using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using MsGlossaryApp.DataModel;

namespace MsGlossaryApp.Model
{
    public static class HttpRequestExtensions
    {
        public static (string userEmail, string fileName) GetUserInfoFromHeaders(
            this HttpRequest request)
        {
            var success = request.Headers.TryGetValue(
                Constants.UserEmailHeaderKey,
                out StringValues userEmailValues);

            if (!success
                || userEmailValues.Count == 0)
            {
                return (null, null);
            }

            success = request.Headers.TryGetValue(
                Constants.FileNameHeaderKey,
                out StringValues fileNameValues);

            if (!success
                || fileNameValues.Count == 0)
            {
                return (userEmailValues[0], null);
            }

            return (userEmailValues[0], fileNameValues[0]);
        }
    }
}