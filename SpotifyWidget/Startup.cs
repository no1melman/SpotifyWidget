using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using Serilog;
using SpotifyWidget.Exceptions;
using SpotifyWidget.Spotify;

namespace SpotifyWidget
{
    public interface IStartup
    {
        Task Go();
    }

    public class Startup : IStartup, IHandle<ErrorModel>, IHandle<ReAuthenticateMessage>
    {
        private readonly ISettingsProvider settingsProvider;
        private readonly IWebApi webApi;
        private readonly IApplicationController applicationController;
        private readonly ILogger logger;
        private readonly IReAuthenticationEventAggregator reAuthenticationEventAggregator;

        public Startup(
            ISettingsProvider settingsProvider,
            IEventAggregator eventAggregator,
            IWebApi webApi,
            IApplicationController applicationController,
            ILogger logger,
            IReAuthenticationEventAggregator reAuthenticationEventAggregator)
        {
            this.settingsProvider = settingsProvider;
            this.webApi = webApi;
            this.applicationController = applicationController;
            this.logger = logger;
            this.reAuthenticationEventAggregator = reAuthenticationEventAggregator;

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
            // lets get the settings out
            await this.settingsProvider.ReadSettings();

            // lets now authenticate
            var success = true;
            try
            {
                await webApi.Authenticate();
            }
            catch (SpotifyWebApiException e)
            {
                this.logger.Error(e, "Unable to Authenticate with Spotify");
                success = false;
            }

            return success;
        }

        private void InitiateWorkload() => applicationController.RunWork();

        public async Task HandleAsync(ErrorModel message, CancellationToken cancellationToken)
        {
            this.logger.Warning("Spotify Api returned an error {@Error}", message);

            if (message.Status == 401)
            {
                // this reauth makes sure we wait for an allotted time and that we don't keep retrying forever
                this.reAuthenticationEventAggregator.SendAuthenticationEvent();
                return;
            }

            // well, we've logged the error... I guess we just back off right?
            await Task.Delay(10000, cancellationToken);
        }

        public Task HandleAsync(ReAuthenticateMessage message, CancellationToken cancellationToken) => this.InitiateSpotify();
    }
}