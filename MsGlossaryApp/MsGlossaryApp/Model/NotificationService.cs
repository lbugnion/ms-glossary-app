using Microsoft.Extensions.Logging;
using MsGlossaryApp.DataModel;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace MsGlossaryApp.Model
{
    public class NotificationService
    {
        private const string NotificationsUrl = "https://notificationsendpoint.azurewebsites.net/api/send";

        public static async Task Notify(
            string title,
            string message,
            ILogger log = null)
        {
            log?.LogInformation("In NotificationService.Notify");

            message = message.Replace("\"", "\\\"");

            var json = $"{{\"title\":\"{title}\",\"body\": \"{message}\",\"channel\":\"MSGlossary\"}}";
            var client = new HttpClient();
            var content = new StringContent(json);

            var request = new HttpRequestMessage(HttpMethod.Post, NotificationsUrl);

            var code = Environment.GetEnvironmentVariable(Constants.NotifyFunctionCodeVariableName);

            if (string.IsNullOrEmpty(code))
            {
                log?.LogError("Function code not found in NotificationService.Notify");
                return;
            }
            request.Headers.Add("x-functions-key", code);
            request.Content = content;
            var response = await client.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();
            log?.LogDebug(result);
        }
    }
}