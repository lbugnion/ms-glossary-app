using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace WordsOfTheDayApp.Model
{
    public class NotificationService
    {
        private const string NotificationsUrl = "https://notificationsendpoint.azurewebsites.net/api/send";

        public static async Task Notify(
            string title,
            string message,
            ILogger log)
        {
            var json = $"{{\"title\":\"{title}\",\"body\": \"{message}\",\"channel\":\"WordsOfTheDay\"}}";
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
            log.LogInformation(result);
        }
    }
}