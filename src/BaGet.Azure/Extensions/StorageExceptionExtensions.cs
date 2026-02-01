using System.Net;
using Azure;

namespace BaGet.Azure
{
    internal static class StorageExceptionExtensions
    {
        public static bool IsAlreadyExistsException(this RequestFailedException e)
        {
            return e?.Status == (int)HttpStatusCode.Conflict;
        }

        public static bool IsNotFoundException(this RequestFailedException e)
        {
            return e?.Status == (int)HttpStatusCode.NotFound;
        }

        public static bool IsPreconditionFailedException(this RequestFailedException e)
        {
            return e?.Status == (int)HttpStatusCode.PreconditionFailed;
        }
    }
}
