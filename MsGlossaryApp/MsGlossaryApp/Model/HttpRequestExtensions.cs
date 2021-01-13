using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using MsGlossaryApp.DataModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace MsGlossaryApp.Model
{
    public static class HttpRequestExtensions
    {
        public static (string userEmail, string fileName) GetUserInfoFromHeaders(
            this HttpRequest request)
        {
            StringValues userEmailValues;
            var success = request.Headers.TryGetValue(
                Constants.UserEmailHeaderKey, 
                out userEmailValues);

            if (!success
                || userEmailValues.Count == 0)
            {
                return (null, null);
            }

            StringValues fileNameValues;
            success = request.Headers.TryGetValue(
                Constants.FileNameHeaderKey, 
                out fileNameValues);

            if (!success
                || fileNameValues.Count == 0)
            {
                return (userEmailValues[0], null);
            }

            return (userEmailValues[0], fileNameValues[0]);
        }
    }
}
