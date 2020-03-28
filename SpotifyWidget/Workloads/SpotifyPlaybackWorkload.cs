using System.Threading.Tasks;
using Caliburn.Micro;
using SpotifyWidget.Spotify;

namespace SpotifyWidget.Workloads
{
    public class SpotifyPlaybackWorkload : IWorkload
    {
        private readonly IWebApi webApi;
        private readonly IEventAggregator eventAggregator;

        public SpotifyPlaybackWorkload(
            IWebApi webApi,
            IEventAggregator eventAggregator)
        {
            this.webApi = webApi;
            this.eventAggregator = eventAggregator;
        }

        // makes this work continuously loop
        public int Delay => 5000;
        
        public async Task DoWork()
        {
            var (error, playbackModel) = await this.webApi.GetPlayback();

            if (error == null)
            {
                // we have playback!! better let the view know
                await eventAggregator.PublishOnBackgroundThreadAsync(playbackModel);
            }

            // any errors will be caught and propagated by the NotifyWebApiHandler.
        }
    }
}