using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
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

    public class MainWindowViewModel : Screen, IMainWindowViewModel, IHandle<PlaybackModel>
    {
        private readonly IWebApi webApi;
        private readonly IWindowManager windowManager;
        private readonly ISettingsViewModel settingsViewModel;

        public MainWindowViewModel(
            IWebApi webApi,
            IWindowManager windowManager,
            ISettingsViewModel settingsViewModel,
            IEventAggregator eventAggregator)
        {
            this.webApi = webApi;
            this.windowManager = windowManager;
            this.settingsViewModel = settingsViewModel;

            eventAggregator.SubscribeOnUIThread(this);
        }

        protected override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            var (error, playbackModel) = await webApi.GetPlayback();

            if (error == null && playbackModel != null)
            {
                SetAllTheThings(playbackModel);
            }

            await base.OnActivateAsync(cancellationToken);
        }

        protected override void OnViewLoaded(object view)
        {
            NotifyAllTheThings();
            base.OnViewLoaded(view);
        }

        public string Name { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public bool IsPlaying { get; set; }
        public BitmapImage Bitmap { get; set; }
        public bool TitleBarVisibility { get; set; } = true;
        
        public void CloseWindow()
        {
            Application.Current.Shutdown(0);
        }

        public void HideTitleBar()
        {
            this.TitleBarVisibility = false;
            NotifyOfPropertyChange(() => TitleBarVisibility);
        }

        public void KeyEvent(ActionExecutionContext aec)
        {
            if (!(aec.EventArgs is KeyEventArgs kargs)) return;
            if (kargs.Key != Key.V || kargs.KeyboardDevice.Modifiers != ModifierKeys.Shift) return;
            if (TitleBarVisibility == true) return;
            
            TitleBarVisibility = true;
            NotifyOfPropertyChange(() => TitleBarVisibility);
        }

        public async Task OpenSettings()
        {
            var showDialogAsync = await this.windowManager.ShowDialogAsync(settingsViewModel);
        }

        private void NotifyAllTheThings()
        {
            this.NotifyOfPropertyChange(() => Name);
            this.NotifyOfPropertyChange(() => Artist);
            this.NotifyOfPropertyChange(() => Album);
            this.NotifyOfPropertyChange(() => IsPlaying);
            this.NotifyOfPropertyChange(() => Bitmap);
        }

        private void SetAllTheThings(PlaybackModel playbackModel)
        {
            // there is only context data when playing
            if (playbackModel.IsPlaying)
            {
                this.Name = playbackModel.Name;
                this.Artist = playbackModel.Artists;
                this.Album = playbackModel.Album;
                this.Bitmap = new BitmapImage(new Uri(playbackModel.Image.FirstOrDefault()));
            }
            
            this.IsPlaying = playbackModel.IsPlaying;
        }

        public Task HandleAsync(PlaybackModel message, CancellationToken cancellationToken)
        {
            SetAllTheThings(message);

            NotifyAllTheThings();

            return Task.CompletedTask;
        }
    }
}