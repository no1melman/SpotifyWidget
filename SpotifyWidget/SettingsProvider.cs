using System;
using System.CodeDom;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace SpotifyWidget
{
    public interface ISettingsProvider : ISettingsModel
    {
        Task SaveSettings();
        Task ReadSettings();
    }

    public class SettingsProvider : ISettingsProvider
    {
        private readonly string filePath;
        private readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        private SettingsModel settings;

        public SettingsProvider()
        {
            settings = new SettingsModel();
            var workingDir = Environment.CurrentDirectory;
            filePath = Path.Join(workingDir, "settings.json");
        }

        public string AccessToken
        {
            get => settings.AccessToken;
            set => settings.AccessToken = value;
        }

        public string TokenType
        {
            get => settings.TokenType;
            set => settings.TokenType = value;
        }

        public async Task SaveSettings()
        {
            await using var fileStream = new FileStream(this.filePath, FileMode.OpenOrCreate,
                FileAccess.ReadWrite, FileShare.ReadWrite);
            await using var writeStream = new StreamWriter(fileStream, Encoding.UTF8);
            await writeStream.WriteAsync(JsonConvert.SerializeObject(settings, Formatting.Indented, jsonSettings));
        }

        public Task ReadSettings() => AssertSettings();

        private async Task InnerReadSettings()
        {
            await using var fileStream = new FileStream(this.filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var readStream = new StreamReader(fileStream, Encoding.UTF8);
            var result = await readStream.ReadToEndAsync().ConfigureAwait(false);
            this.settings = JsonConvert.DeserializeObject<SettingsModel>(result);
        }

        private async Task AssertSettings()
        {
            var settingsExist = true;
            try
            {
                await InnerReadSettings();
            }
            catch (FileNotFoundException agex)
            {
                settingsExist = false;
            }

            if (settingsExist)
            {
                return;
            }
            
            // we should just write some
            await SaveSettings();
        }
    }

    public interface ISettingsModel
    {
        string AccessToken { get; set; }
        string TokenType { get; set; }
    }

    public class SettingsModel : ISettingsModel
    {
        public string AccessToken { get; set; } = "";
        public string TokenType { get; set; } = "";
    }
}