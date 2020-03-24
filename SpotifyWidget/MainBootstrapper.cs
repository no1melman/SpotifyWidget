using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyWidget.Exceptions;
using SpotifyWidget.Spotify;
using SpotifyWidget.Views.Main;
using StructureMap;

namespace SpotifyWidget
{
    public class MainBootstrapper : BootstrapperBase
    {
        private IContainer container;

        public MainBootstrapper()
        {
            Initialize();
        }

        protected override void Configure()
        {
            base.Configure();

            container = ConfigureContainer();
        }

        private IContainer ConfigureContainer() =>
            new Container(c =>
            {
                c.Scan(s =>
                {
                    s.TheCallingAssembly();

                    s.WithDefaultConventions();
                });

                c.For<IWindowManager>().Use<WindowManager>();
                c.For<IEventAggregator>().Use<EventAggregator>();
                c.For<ISettingsProvider>().Use<SettingsProvider>().Singleton();
                c.For<IWebApi>().Use<WebApi>().Singleton();
            });

        protected override object GetInstance(Type service, string key)
        {
            return container.GetInstance(service);
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            return container.GetAllInstances(service)
                .Cast<IEnumerable<object>>();
        }

        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            Task.Run(async () =>
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
                }

                webApi.Set(newWebApi);
                var isGood = await webApi.CheckConnectionIsGood();

                if (!isGood)
                {
                    UnsafeShutdown();
                }

                // we can now save the settings and the spotify connection
                // credentials because it should all have worked by here
                await settingsProvider.SaveSettings();
            }).Wait();

            DisplayRootViewFor<IMainWindowViewModel>();
        }

        private void UnsafeShutdown()
        {
            MessageBox.Show("Unable to connect to spotify", "Error Connecting to Spotify",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Shutdown(1);
        }
    }

    public static class StringExtensions
    {
        public static bool IsNullOrEmpty(this string value) => string.IsNullOrEmpty(value);
    }
}