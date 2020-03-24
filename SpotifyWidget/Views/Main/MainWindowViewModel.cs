using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using SpotifyWidget.Spotify;
using SpotifyWidget.Views.Settings;

namespace SpotifyWidget.Views.Main
{
    public interface IMainWindowViewModel
    {
        void CloseWindow();
        Task OpenSettings();
    }

    public class MainWindowViewModel : Screen, IMainWindowViewModel
    {
        private readonly IAuthentication authentication;
        private readonly IWebApi webApi;
        private readonly IWindowManager windowManager;
        private readonly ISettingsViewModel settingsViewModel;

        public MainWindowViewModel(
            IAuthentication authentication,
            IWebApi webApi,
            IWindowManager windowManager,
            ISettingsViewModel settingsViewModel)
        {
            this.authentication = authentication;
            this.webApi = webApi;
            this.windowManager = windowManager;
            this.settingsViewModel = settingsViewModel;
        }

        protected override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            var playbackModel = await webApi.GetPlayback();

            if (playbackModel != null)
            {
                this.Name = playbackModel.Name;
                this.Artist = playbackModel.Artists;
                this.Album = playbackModel.Album;
                this.IsPlaying = playbackModel.IsPlaying;
                this.Bitmap = new BitmapImage(new Uri(playbackModel.Image.FirstOrDefault()));
            }

            await base.OnActivateAsync(cancellationToken);
        }

        protected override void OnViewLoaded(object view)
        {
            this.NotifyOfPropertyChange(() => Name);
            this.NotifyOfPropertyChange(() => Artist);
            this.NotifyOfPropertyChange(() => Album);
            this.NotifyOfPropertyChange(() => IsPlaying);
            this.NotifyOfPropertyChange(() => Bitmap);
            base.OnViewLoaded(view);
        }

        public string Name { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public bool IsPlaying { get; set; }
        public BitmapImage Bitmap { get; set; }
        
        public void CloseWindow()
        {
            Application.Current.Shutdown(0);
        }

        public async Task OpenSettings()
        {
            var showDialogAsync = await this.windowManager.ShowDialogAsync(settingsViewModel);
            
            var playbackModel = await webApi.GetPlayback();

            this.Name = playbackModel.Name;
            this.Artist = playbackModel.Artists;
            this.Album = playbackModel.Album;
            this.IsPlaying = playbackModel.IsPlaying;
            this.Bitmap = new BitmapImage(new Uri(playbackModel.Image.FirstOrDefault()));

            this.NotifyOfPropertyChange(() => Name);
            this.NotifyOfPropertyChange(() => Artist);
            this.NotifyOfPropertyChange(() => Album);
            this.NotifyOfPropertyChange(() => IsPlaying);
            this.NotifyOfPropertyChange(() => Bitmap);
        }
    }
}