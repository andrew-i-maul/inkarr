using System;

namespace NzbDrone.Common.Exceptions
{
    public class InkarrStartupException : NzbDroneException
    {
        public InkarrStartupException(string message, params object[] args)
            : base("Inkarr failed to start: " + string.Format(message, args))
        {
        }

        public InkarrStartupException(string message)
            : base("Inkarr failed to start: " + message)
        {
        }

        public InkarrStartupException()
            : base("Inkarr failed to start")
        {
        }

        public InkarrStartupException(Exception innerException, string message, params object[] args)
            : base("Inkarr failed to start: " + string.Format(message, args), innerException)
        {
        }

        public InkarrStartupException(Exception innerException, string message)
            : base("Inkarr failed to start: " + message, innerException)
        {
        }

        public InkarrStartupException(Exception innerException)
            : base("Inkarr failed to start: " + innerException.Message)
        {
        }
    }
}
