using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using WordsOfTheDayApp.Model;

namespace WordsOfTheDayApp
{
    public static class ReplaceKeywords
    {
        [FunctionName("ReplaceKeywords")]
        public static void Run(
            [QueueTrigger(
                Constants.QueueName, 
                Connection = Constants.AzureWebJobsStorage)]
            string myQueueItem, 
            ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {myQueueItem}");
        }
    }
}
