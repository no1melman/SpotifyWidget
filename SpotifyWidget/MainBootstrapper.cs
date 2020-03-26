using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using SpotifyWidget.Spotify;
using SpotifyWidget.Views.Main;
using StructureMap;

namespace SpotifyWidget
{
    public class MainBootstrapper : BootstrapperBase, IHandle<ShutdownModel>
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

                    s.WithDefaultConventions();
                });

                c.For<IWindowManager>().Use<WindowManager>();
                c.For<IEventAggregator>().Use<EventAggregator>();
                c.For<ISettingsProvider>().Use<SettingsProvider>().Singleton();
                c.For<IWebApi>().Use<WebApi>().Singleton();
                c.For<IApplicationController>().Use<ApplicationController>().Singleton();
                c.For<IEventAggregator>().Use<EventAggregator>().Singleton();

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
                var startup = new Startup(container);
                await startup.Go();
            }).Wait();

            DisplayRootViewFor<IMainWindowViewModel>();
        }

        public Task HandleAsync(ShutdownModel message, CancellationToken cancellationToken)
        {
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