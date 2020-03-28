using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Caliburn.Micro;
using Serilog;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Models;
using SpotifyWidget.Exceptions;

namespace SpotifyWidget.Spotify
{
    public interface IWebApi
    {
        /// <summary>
        /// Authenticates the WebApi specifically through our <see cref="IAuthentication">Authentication</see> module
        /// </summary>
        /// <exception cref="SpotifyWebApiException">Thrown if Browser Authentication fails</exception>
        /// <returns>Whether the Authentication was successful and if not, it will return an error object</returns>
        Task<(bool, ErrorModel)> Authenticate();

        Task<(ErrorModel, PlaybackModel)> GetPlayback();

        Task<(bool, ErrorModel)> CheckConnectionIsGood();
    }

    public class WebApi : IWebApi
    {
        private readonly IAuthentication authentication;
        private readonly ISettingsProvider settingsProvider;

        private SpotifyWebAPI spotifyWebApi;

        public WebApi(
            IAuthentication authentication,
            ISettingsProvider settingsProvider)
        {
            this.authentication = authentication;
            this.settingsProvider = settingsProvider;
        }

        /// <inheritdoc cref="IWebApi.Authenticate"/>
        public async Task<(bool, ErrorModel)> Authenticate()
        {
            spotifyWebApi = await authentication.Initialise();

            var (isGood, error) = await CheckConnectionIsGood();


            if (!isGood && error.Status != 401)
            {
                // don't think we can recover from this here, just need to report on
                // using the false literal because then I can always be sure it sends failure.
                return (false, error);
            }

            // if we get a 401
            // force a browser authentication
            if (!isGood && error.Status == 401)
            {
                spotifyWebApi = await authentication.Initialise(true);

                var (secondIsGood, secondError) = await CheckConnectionIsGood();

                // if it fails a second time, regardless, just quit.
                if (!secondIsGood)
                {
                    // if we fail a second time, again, not sure what we can do here
                    // so letting the error bubble up
                    return (false, secondError);
                }
            }

            // we can now save the settings and the spotify connection
            // credentials because it should all have worked by here
            await settingsProvider.SaveSettings();
            return (true, null);
        }
        
        public async Task<(ErrorModel, PlaybackModel)> GetPlayback()
        {
            EnsureWebApi();

            var playbackContext = await spotifyWebApi.GetPlaybackAsync();
            
            return (GetError(playbackContext),
                new PlaybackModel(playbackContext?.IsPlaying ?? false, playbackContext?.Item));
        }

        public async Task<(bool, ErrorModel)> CheckConnectionIsGood()
        {
            EnsureWebApi();

            var playbackContext = await spotifyWebApi.GetPlaybackAsync();

            return (!playbackContext.HasError(), GetError(playbackContext));
        }

        private ErrorModel GetError(BasicModel core)
            =>
                core?.HasError() ?? true
                    ? new ErrorModel
                    {
                        Message = core?.Error?.Message,
                        Status = core?.Error?.Status ?? -1
                    }
                    : null;

        private object EnsureWebApi() => spotifyWebApi ?? throw new Exception("You've not initialised the Web Api");
    }

    public class NotifyWebApi : IWebApi
    {
        private readonly IWebApi webApi;
        private readonly ILogger logger;
        private readonly Func<object, Task> publishEvent;

        public NotifyWebApi(
            IWebApi webApi,
            IEventAggregator eventAggregator,
            ILogger logger)
        {
            this.webApi = webApi;
            this.logger = logger;
            publishEvent = eventAggregator.PublishOnCurrentThreadAsync;
        }

        public async Task<(bool, ErrorModel)> Authenticate()
        {
            var (success, error) = await webApi.Authenticate();

            if (!success)
            {
                this.logger.Warning("Spotify encountered an error :: Authenticate");
                await publishEvent(error ?? new ErrorModel {Message = "Error from :: Authenticate"});
            }

            return (success, error);
        }

        public async Task<(ErrorModel, PlaybackModel)> GetPlayback()
        {
            var (error, playback) = await webApi.GetPlayback();

            if (error == null) return (error, playback);

            this.logger.Warning("Spotify encountered an error :: GetPlayback");
            await publishEvent(error);

            return (error, playback);
        }

        public Task<(bool, ErrorModel)> CheckConnectionIsGood() => this.webApi.CheckConnectionIsGood();
    }

    public class PlaybackModel
    {
        public PlaybackModel(bool isPlaying, FullTrack playbackContextItem)
        {
            this.IsPlaying = isPlaying;
            this.Album = playbackContextItem?.Album?.Name;
            this.Artists = playbackContextItem?.Artists == null ? "" : string.Join(", ", playbackContextItem.Artists.Where(x => !x.HasError()).Select(x => x.Name));
            this.Name = playbackContextItem?.Name ?? null;
            this.Image = playbackContextItem?.Album?.Images?.Where(x => x.Url != null).Select(x => x.Url);
        }

        public bool IsPlaying { get; }
        public string Album { get; }
        public string Name { get; }
        public string Artists { get; set; }

        public IEnumerable<string> Image { get; set; }
        public string Playlist { get; set; }
    }

    public class ErrorModel
    {
        public int Status { get; set; }
        public string Message { get; set; }
    }
}