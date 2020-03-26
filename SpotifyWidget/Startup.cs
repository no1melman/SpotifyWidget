using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using SpotifyAPI.Web;
using SpotifyWidget.Exceptions;
using SpotifyWidget.Spotify;
using StructureMap;

namespace SpotifyWidget
{
    public interface IStartup
    {
        Task Go();
    }

    public class Startup : IStartup, IHandle<ErrorModel>
    {
        private readonly IContainer container;

        public Startup(
            IContainer container)
        {
            this.container = container;

            container.GetInstance<IEventAggregator>().SubscribeOnPublishedThread(this);
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
            var settingsProvider = container.GetInstance<ISettingsProvider>();


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

            // we load these deps down here, because we've modified the settings provider
            // this prevents us getting a stale copy.

            var auth = container.GetInstance<IAuthentication>();
            var webApi = container.GetInstance<IWebApi>();

            // we can try load the spotify stuff

            SpotifyWebAPI newWebApi = null;
            try
            {
                newWebApi = await auth.Initialise();
            }
            catch (SpotifyApplicationException)
            {
                UnsafeShutdown();
                return false;
            }

            webApi.Set(newWebApi);
            var (isGood, error) = await webApi.CheckConnectionIsGood();


            if (!isGood && error.Status != 401)
            {
                UnsafeShutdown();
                return false;
            }

            // if we get a 401
            // force a browser authentication
            if (!isGood && error.Status == 401)
            {
                try
                {
                    newWebApi = await auth.Initialise(true);
                }
                catch (SpotifyApplicationException)
                {
                    UnsafeShutdown();
                    return false;
                }

                webApi.Set(newWebApi);
                var (secondIsGood, secondError) = await webApi.CheckConnectionIsGood();

                // if it fails a second time, regardless, just quit.
                if (!secondIsGood)
                {
                    UnsafeShutdown();
                    return false;
                }
            }

            // we can now save the settings and the spotify connection
            // credentials because it should all have worked by here
            await settingsProvider.SaveSettings();
            return true;
        }

        private void InitiateWorkload()
        {
            var controller = container.GetInstance<IApplicationController>();

            controller.RunWork();
        }

        private void UnsafeShutdown()
        {
            MessageBox.Show("Unable to connect to spotify", "Error Connecting to Spotify",
                MessageBoxButton.OK, MessageBoxImage.Error);

            this.container.GetInstance<IEventAggregator>().PublishOnUIThreadAsync(new ShutdownModel(1));
        }

        public async Task HandleAsync(ErrorModel message, CancellationToken cancellationToken)
        {
            // round two...
            await InitiateSpotify();
        }
    }
}