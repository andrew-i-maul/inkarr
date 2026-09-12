using System.Net;
using Inkarr.Http.Exceptions;

namespace Inkarr.Http.REST
{
    public class BadRequestException : ApiException
    {
        public BadRequestException(object content = null)
            : base(HttpStatusCode.BadRequest, content)
        {
        }
    }
}
