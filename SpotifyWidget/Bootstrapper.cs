using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using Serilog;
using SpotifyWidget.Spotify;
using SpotifyWidget.Views.Main;
using StructureMap;

namespace SpotifyWidget
{
    public class Bootstrapper : BootstrapperBase, IHandle<ShutdownModel>
    {
        private IContainer container;

        public Bootstrapper()
        {
            Initialize();
        }

        protected override void Configure()
        {
            base.Configure();

            Log.Logger.Information("Configuring the Container");
            
            container = ConfigureContainer();

            container.GetInstance<IEventAggregator>().SubscribeOnUIThread(this);

            var everything = container.WhatDoIHave();
        }

        private IContainer ConfigureContainer() =>
            new Container(c =>
            {
                c.Scan(s =>
                {
                    s.TheCallingAssembly();

                    s.AddAllTypesOf<IWorkload>();

                    s.ConnectImplementationsToTypesClosing(typeof(IHandle<>));
                    
                    s.WithDefaultConventions(); // ISomething => Something, .Transient()
                });

                c.For<IWindowManager>().Use<WindowManager>();
                c.For<IEventAggregator>().Use<EventAggregator>().Singleton();
                c.For<IReAuthenticationEventAggregator>().Use<ReAuthenticationEventAggregator>().Singleton();

                c.For<ISettingsProvider>().Use<SettingsProvider>().Singleton();
                
                c.For<IWebApi>()
                    .Use<WebApi>()
                    .Singleton()
                    .DecorateWith((ctx, inner) => new NotifyWebApi(inner, ctx.GetInstance<IEventAggregator>(), ctx.GetInstance<ILogger>()));
                
                c.For<IApplicationController>().Use<ApplicationController>().Singleton();
                // because this handles events, it needs to exist forever...
                c.For<IStartup>().Use<Startup>().Singleton();
                c.For<ILogger>().Use(Log.Logger);
            });

        protected override object GetInstance(Type service, string key) => container.GetInstance(service);

        protected override IEnumerable<object> GetAllInstances(Type service) =>
            container.GetAllInstances(service)
                .Cast<IEnumerable<object>>();

        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            Log.Logger.Information("Authorising with Spotify");

            Task.Run(async () => await container.GetInstance<IStartup>().Go()).Wait();

            DisplayRootViewFor<IMainWindowViewModel>();
        }

        public Task HandleAsync(ShutdownModel message, CancellationToken cancellationToken)
        {
            // base on message exit code, either we show error boxes
            // or we cleanly shutdown
            //MessageBox.Show("Unable to connect to spotify", "Error Connecting to Spotify",
            //    MessageBoxButton.OK, MessageBoxImage.Error);

            Application.Current.Shutdown(message.ExitCode);

            return Task.CompletedTask;
        }
    }

    public class ShutdownModel
    {
        public ShutdownModel(int exitCode)
        {
            ExitCode = exitCode;
        }

        public int ExitCode { get; }
    }

    public static class StringExtensions
    {
        public static bool IsNullOrEmpty(this string value) => string.IsNullOrEmpty(value);
    }
}