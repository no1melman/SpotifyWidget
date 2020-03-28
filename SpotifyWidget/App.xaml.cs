using System.Windows;
using Serilog;
using Serilog.Core;

namespace SpotifyWidget
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Log.Logger = new LoggerConfiguration().WriteTo.Async(a => a.RollingFile("log-{HalfHour}.txt", outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")).CreateLogger();

            Log.Logger.Information("Starting up...");

            InitializeComponent();
        }
    }
}
