using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using MsGlossaryApp.DataModel;

namespace MsGlossaryApp.Model
{
    public static class HttpRequestExtensions
    {
        public static (string userEmail, 
                       string fileName,
                       string hash,
                       string commitMessage) GetUserInfoFromHeaders(
            this HttpRequest request)
        {
            var success = request.Headers.TryGetValue(
                Constants.UserEmailHeaderKey,
                out StringValues userEmailValues);

            if (!success
                || userEmailValues.Count == 0)
            {
                return (null, null, null, null);
            }

            success = request.Headers.TryGetValue(
                Constants.FileNameHeaderKey,
                out StringValues fileNameValues);

            if (!success
                || fileNameValues.Count == 0)
            {
                return (userEmailValues[0]?.Trim(), null, null, null);
            }

            success = request.Headers.TryGetValue(
                Constants.HashHeaderKey,
                out StringValues hashValues);

            if (!success
                || hashValues.Count == 0)
            {
                return (userEmailValues[0]?.Trim(), fileNameValues[0]?.Trim(), null, null);
            }

            success = request.Headers.TryGetValue(
                Constants.HashHeaderKey,
                out StringValues commitMessageValues);

            if (!success
                || commitMessageValues.Count == 0)
            {
                return (userEmailValues[0]?.Trim(), fileNameValues[0]?.Trim(), hashValues[0].Trim(), null);
            }

            return (userEmailValues[0]?.Trim(), fileNameValues[0]?.Trim(), hashValues[0].Trim(), commitMessageValues[0]?.Trim());
        }
    }
}