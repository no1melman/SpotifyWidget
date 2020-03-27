using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Caliburn.Micro;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Models;
using SpotifyWidget.Exceptions;

namespace SpotifyWidget.Spotify
{
    public interface IWebApi
    {
        Task ReAuthorise();

        Task<(ErrorModel, PlaybackModel)> GetPlayback();

        Task<(bool, ErrorModel)> CheckConnectionIsGood();
    }

    public class WebApi : IWebApi
    {
        private readonly IAuthentication authentication;
        private readonly IEventAggregator eventAggregator;
        private readonly ISettingsProvider settingsProvider;
        private SpotifyWebAPI spotifyWebApi;

        public WebApi(
            IAuthentication authentication,
            IEventAggregator eventAggregator,
            ISettingsProvider settingsProvider)
        {
            this.authentication = authentication;
            this.eventAggregator = eventAggregator;
            this.settingsProvider = settingsProvider;
        }

        public async Task ReAuthorise()
        {
            try
            {
                spotifyWebApi = await authentication.Initialise();
            }
            catch (SpotifyApplicationException)
            {
                UnsafeShutdown();
            }

            var (isGood, error) = await CheckConnectionIsGood();


            if (!isGood && error.Status != 401)
            {
                // delay then retry connection
                // UnsafeShutdown();
            }

            // if we get a 401
            // force a browser authentication
            if (!isGood && error.Status == 401)
            {
                try
                {
                    spotifyWebApi = await authentication.Initialise(true);
                }
                catch (SpotifyApplicationException)
                {
                    UnsafeShutdown();
                }

                var (secondIsGood, secondError) = await CheckConnectionIsGood();

                // if it fails a second time, regardless, just quit.
                if (!secondIsGood)
                {
                    // most likely try again in 1 minute
                    UnsafeShutdown();
                }
            }

            // we can now save the settings and the spotify connection
            // credentials because it should all have worked by here
            await settingsProvider.SaveSettings();
        }

        private void UnsafeShutdown()
        {
            eventAggregator.PublishOnUIThreadAsync(new ShutdownModel(1));
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