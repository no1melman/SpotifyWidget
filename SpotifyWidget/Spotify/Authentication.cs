﻿using System;
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
        Task<SpotifyWebAPI> Initialise();
    }

    public class Authentication : IAuthentication
    {
        private readonly ISettingsProvider settingsProvider;
        private ImplicitGrantAuth implicitGrantAuth;
        
        public Authentication(
            IConfiguration configuration,
            ISettingsProvider settingsProvider)
        {
            this.settingsProvider = settingsProvider;
            implicitGrantAuth = new ImplicitGrantAuth(configuration.SpotifyApiKey, "http://localhost:4002",
                "http://localhost:4002",
                Scope.UserReadPlaybackState);
        }


        public async Task<SpotifyWebAPI> Initialise()
        {
            if (settingsProvider.TokenType.IsNullOrEmpty() || settingsProvider.AccessToken.IsNullOrEmpty())
            {
                return await InitialiseFromBrowser();
            }

            return InitialiseFromSettings();
        }

        private SpotifyWebAPI InitialiseFromSettings()
        {
            try
            {
                return new SpotifyWebAPI
                {
                    AccessToken = this.settingsProvider.AccessToken,
                    TokenType = this.settingsProvider.TokenType
                };
            }
            catch (SpotifyWebApiException ex)
            {
                throw new SpotifyApplicationException("Unable to connect to ", ex);
            }
        }

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
            catch (SpotifyWebApiException ex)
            {
                throw new SpotifyApplicationException("Unable to connect to ", ex);
            }
            finally
            {
                implicitGrantAuth.AuthReceived -= OnImplicitGrantAuthOnAuthReceived;
            }
        }
    }
}