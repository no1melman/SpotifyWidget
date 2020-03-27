using System.Threading.Tasks;
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
        private readonly IWebApi webApi;
        private readonly IEventAggregator eventAggregator;

        public SettingsViewModel(
            IWebApi webApi,
            IEventAggregator eventAggregator)
        {
            this.webApi = webApi;
            this.eventAggregator = eventAggregator;
        }


        public async Task ConnectToSpotify()
        {
            try
            {
                await this.webApi.ReAuthorise();
            }
            catch (SpotifyApplicationException ex)
            {
                await eventAggregator.PublishOnUIThreadAsync(new ShutdownModel(50));
                await this.TryCloseAsync();
            }
        }
    }


}