using System.Threading.Tasks;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using SpotifyWidget.Exceptions;

namespace SpotifyWidget.Spotify
{
    public interface IAuthentication
    {
        /// <summary>
        /// Initialises the WebApi object with authorisation credentials. It will attempt to read from settings first (unless overridden) then try Browser Authentication.
        /// </summary>
        /// <param name="forceAuth">Used to force Browser Authentication and ignore settings file</param>
        /// <exception cref="SpotifyWebApiException">Thrown when browser authentication fails</exception>
        /// <returns>A fully initialised WebApi object with User Authorisation</returns>
        Task<SpotifyWebAPI> Initialise(bool forceAuth = false);
    }

    /// <inheritdoc cref="IAuthentication"/>
    public class Authentication : IAuthentication
    {
        private readonly ISettingsProvider settingsProvider;
        private readonly ImplicitGrantAuth implicitGrantAuth;
        
        public Authentication(
            ISettingsProvider settingsProvider)
        {
            this.settingsProvider = settingsProvider;
            this.implicitGrantAuth = new ImplicitGrantAuth("2ee62a35a2ec45a4a7fe26b81f7f3681", "http://localhost:4002",
                "http://localhost:4002",
                Scope.UserReadPlaybackState);
        }
        
        public async Task<SpotifyWebAPI> Initialise(bool forceAuth = false)
        {
            if (forceAuth || settingsProvider.TokenType.IsNullOrEmpty() || settingsProvider.AccessToken.IsNullOrEmpty())
            {
                return await InitialiseFromBrowser();
            }

            return InitialiseFromSettings();
        }

        private SpotifyWebAPI InitialiseFromSettings() =>
            new SpotifyWebAPI
            {
                AccessToken = this.settingsProvider.AccessToken,
                TokenType = this.settingsProvider.TokenType
            };

        private async Task<SpotifyWebAPI> InitialiseFromBrowser()
        {
            var source = new TaskCompletionSource<Token>();

            void OnImplicitGrantAuthOnAuthReceived(object sender, Token payload)
            {
                implicitGrantAuth.Stop();

                if (payload.HasError())
                {
                    source.SetException(new SpotifyWebApiException(payload.Error, payload.ErrorDescription));
                    return;
                }

                source.SetResult(payload);
            }

            implicitGrantAuth.AuthReceived += OnImplicitGrantAuthOnAuthReceived;

            implicitGrantAuth.Start();
            implicitGrantAuth.OpenBrowser();

            try
            {
                var token = await source.Task;

                settingsProvider.TokenType = token.TokenType;
                settingsProvider.AccessToken = token.AccessToken;

                return new SpotifyWebAPI
                {
                    AccessToken = token.AccessToken,
                    TokenType = token.TokenType
                };
            }
            finally
            {
                implicitGrantAuth.AuthReceived -= OnImplicitGrantAuthOnAuthReceived;
            }
        }
    }
}