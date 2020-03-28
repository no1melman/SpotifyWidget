using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Caliburn.Micro;
using Serilog;
using SpotifyWidget.Spotify;

namespace SpotifyWidget.Views.Settings
{
    public interface ISettingsViewModel
    {
        Task ConnectToSpotify();
    }

    public class SettingsViewModel : Screen, ISettingsViewModel
    {
        private readonly IWebApi webApi;
        private readonly ILogger logger;

        public SettingsViewModel(
            IWebApi webApi,
            ILogger logger)
        {
            this.webApi = webApi;
            this.logger = logger;
        }


        public async Task ConnectToSpotify()
        {
            var (success, error) = await this.webApi.Authenticate();

            if (!success)
            {
                this.logger.Warning("Unable to Authenticate :: ConnectToSpotify");
            }
        }

        public void ViewLogs()
        {
            Process.Start(new ProcessStartInfo("explorer", Environment.CurrentDirectory));
        }
    }
}