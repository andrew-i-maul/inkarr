using System;
using System.Net;
using NzbDrone.Core.Exceptions;

namespace NzbDrone.Core.MetadataSource.ComicVine
{
    public class ComicVineException : NzbDroneClientException
    {
        public ComicVineException(string message)
            : base(HttpStatusCode.ServiceUnavailable, message)
        {
        }

        public ComicVineException(string message, params object[] args)
            : base(HttpStatusCode.ServiceUnavailable, message, args)
        {
        }

        public ComicVineException(string message, Exception innerException, params object[] args)
            : base(HttpStatusCode.ServiceUnavailable, message, innerException, args)
        {
        }
    }
}
