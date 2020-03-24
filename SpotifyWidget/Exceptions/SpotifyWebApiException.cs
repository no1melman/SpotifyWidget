using System;

namespace SpotifyWidget.Exceptions
{
    public class SpotifyWebApiException : Exception
    {
        public string Description { get; }

        public SpotifyWebApiException(string message, string description) : base(message)
        {
            Description = description;
        }
    }
}