using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Models;
using SpotifyWidget.Exceptions;

namespace SpotifyWidget.Spotify
{
    public interface IWebApi
    {
        void Set(SpotifyWebAPI api);

        Task<(ErrorModel, PlaybackModel)> GetPlayback();

        Task<(bool, ErrorModel)> CheckConnectionIsGood();
    }

    public class WebApi : IWebApi
    {
        private SpotifyWebAPI spotifyWebApi;

        public void Set(SpotifyWebAPI api)
        {
            spotifyWebApi = api ?? throw new ArgumentNullException(nameof(api), "Please specify a Web Api");
        }

        public SpotifyWebAPI Get()
        {
            return spotifyWebApi ?? throw new SpotifyApplicationException("You've not initialised the Web Api");
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

        private object EnsureWebApi()
        {
            return spotifyWebApi ?? throw new Exception("You've not initialised the Web Api");
        }
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
    }

    public class ErrorModel
    {
        public int Status { get; set; }
        public string Message { get; set; }
    }
}