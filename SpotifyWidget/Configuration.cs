namespace SpotifyWidget
{
    public interface IConfiguration
    {
        string SpotifyApiKey { get; }
    }


    public class Configuration : IConfiguration
    {
        public string SpotifyApiKey => "2ee62a35a2ec45a4a7fe26b81f7f3681";
    }
}