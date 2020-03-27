using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using SpotifyWidget.Spotify;

namespace SpotifyWidget
{
    public interface IStartup
    {
        Task Go();
    }

    public class Startup : IStartup, IHandle<ErrorModel>
    {
        private readonly ISettingsProvider settingsProvider;
        private readonly IWebApi webApi;
        private readonly IApplicationController applicationController;

        public Startup(
            ISettingsProvider settingsProvider,
            IEventAggregator eventAggregator,
            IWebApi webApi,
            IApplicationController applicationController)
        {
            this.settingsProvider = settingsProvider;
            this.webApi = webApi;
            this.applicationController = applicationController;

            eventAggregator.SubscribeOnPublishedThread(this);
        }

        public async Task Go()
        {
            var success = await InitiateSpotify();
            
            if (success)
            { 
                InitiateWorkload();
            }
        }

        private async Task<bool> InitiateSpotify()
        {
            var settingsExist = true;
            try
            {
                await settingsProvider.ReadSettings();
            }
            catch (FileNotFoundException agex)
            {
                settingsExist = false;
            }

            if (!settingsExist)
            {
                // we should just write some
                await settingsProvider.SaveSettings();
            }

            await webApi.ReAuthorise();

            return true;
        }

        private void InitiateWorkload() => applicationController.RunWork();

        public async Task HandleAsync(ErrorModel message, CancellationToken cancellationToken) => await InitiateSpotify();
    }
}