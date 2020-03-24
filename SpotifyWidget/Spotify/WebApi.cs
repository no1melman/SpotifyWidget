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

        Task<PlaybackModel> GetPlayback();

        Task<bool> CheckConnectionIsGood();
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

        public async Task<PlaybackModel> GetPlayback()
        {
            try
            {
                EnsureWebApi();

                var playbackContext = await spotifyWebApi.GetPlaybackAsync();

                return new PlaybackModel(playbackContext.IsPlaying, playbackContext?.Item);
            }
            catch (SpotifyApplicationException ex)
            { 
                return null;
            }
        }

        public async Task<bool> CheckConnectionIsGood()
        {
            EnsureWebApi();

            var playbackContext = await spotifyWebApi.GetPlaybackAsync();

            return !playbackContext.HasError();
        }

        private object EnsureWebApi()
        {
            return spotifyWebApi ?? throw new Exception("You've not initialised the Web Api");
        }
    }

    public class PlaybackModel
    {
        public PlaybackModel(bool isPlaying, FullTrack playbackContextItem)
        {
            if (playbackContextItem == null)
            {
                throw new SpotifyApplicationException("Unable to retrieve playback");
            }

            if (playbackContextItem.HasError())
            {
                throw new SpotifyApplicationException("Unable to retrieve playback");
            }

            if (playbackContextItem.Album.HasError())
            {
                throw new SpotifyApplicationException("Unable to retrieve album");
            }

            if (playbackContextItem.Artists.All(x => x.HasError()))
            {
                throw new SpotifyApplicationException("Couldn't retrieve any artists");
            }

            this.IsPlaying = isPlaying;
            this.Album = playbackContextItem.Album.Name;
            this.Artists = string.Join(", ", playbackContextItem.Artists.Where(x => !x.HasError()).Select(x => x.Name));
            this.Name = playbackContextItem.Name;
            this.Image = playbackContextItem.Album.Images.Where(x => x.Url != null).Select(x => x.Url);
        }

        public bool IsPlaying { get; }
        public string Album { get; }
        public string Name { get; }
        public string Artists { get; set; }

        public IEnumerable<string> Image { get; set; }
    }
}