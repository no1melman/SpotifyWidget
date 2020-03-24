using System;

namespace SpotifyWidget.Exceptions
{
    public class SpotifyApplicationException : Exception
    {
        public SpotifyApplicationException(string message) : base(message)
        {
        }
        public SpotifyApplicationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}