using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using SpotifyWidget.Exceptions;
using SpotifyWidget.Spotify;

namespace SpotifyWidget.Views.Settings
{
    public interface ISettingsViewModel
    {
        Task ConnectToSpotify();
    }

    public class SettingsViewModel : Screen, ISettingsViewModel
    {
        private readonly IAuthentication authentication;
        private readonly IWebApi webApi;
        private readonly IEventAggregator eventAggregator;

        public SettingsViewModel(
            IAuthentication authentication,
            IWebApi webApi,
            IEventAggregator eventAggregator)
        {
            this.authentication = authentication;
            this.webApi = webApi;
            this.eventAggregator = eventAggregator;
        }


        public async Task ConnectToSpotify()
        {
            try
            {
                var spotifyWebApi = await this.authentication.Initialise();

                webApi.Set(spotifyWebApi);
            }
            catch (SpotifyApplicationException ex)
            {
                await eventAggregator.PublishOnBackgroundThreadAsync(ex);
                await this.TryCloseAsync();
            }
        }
    }


}