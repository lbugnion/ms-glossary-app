using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Net.Http;
using Microsoft.AspNetCore.Http.Extensions;
using WordsOfTheDayApp.Model;

namespace WordsOfTheDayApp
{
    public static class Cleanup
    {
        [FunctionName("Cleanup")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Function, 
                "get", 
                Route = null)] 
            HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Cleanup was called");

            var account = CloudStorageAccount.Parse(
                Environment.GetEnvironmentVariable(Constants.AzureWebJobsStorageVariableName));

            var blobClient = account.CreateCloudBlobClient();
            var markdownContainer = blobClient.GetContainerReference(
                Environment.GetEnvironmentVariable(Constants.TopicsUploadContainerVariableName));
            var newMarkdownContainer = blobClient.GetContainerReference(
                Environment.GetEnvironmentVariable(Constants.TopicsContainerVariableName));
            var settingsContainer = blobClient.GetContainerReference(
                Environment.GetEnvironmentVariable(Constants.SettingsContainerVariableName));

            BlobContinuationToken continuationToken = null;
            var result = string.Empty;

            do
            {
                var response = await newMarkdownContainer.ListBlobsSegmentedAsync(continuationToken);
                continuationToken = response.ContinuationToken;

                foreach (CloudBlockBlob blob in response.Results)
                {
                    await blob.DeleteAsync();
                }
            }
            while (continuationToken != null);

            do
            {
                var response = await settingsContainer.ListBlobsSegmentedAsync(continuationToken);
                continuationToken = response.ContinuationToken;

                foreach (CloudBlockBlob blob in response.Results)
                {
                    await blob.DeleteAsync();
                }
            }
            while (continuationToken != null);

            do
            {
                var response = await markdownContainer.ListBlobsSegmentedAsync(continuationToken);
                continuationToken = response.ContinuationToken;
                var client = new HttpClient();
                var thisUrl = req.GetEncodedUrl();
                var updateUrl = thisUrl.Replace("/Cleanup", "/UpdateMarkdownHttp?name={0}");

                foreach (CloudBlockBlob blob in response.Results)
                {
                    // Regenerate the markdown to trigger new execution
                    // We do this via HTTP call to the corresponding function
                    // to take advantage of scaling.
                    var request = new HttpRequestMessage(HttpMethod.Get, string.Format(updateUrl, Path.GetFileNameWithoutExtension(blob.Name)));
                    
                    // TODO REPLACE
                    //request.Headers.Add("x-functions-key", Environment.GetEnvironmentVariable("UpdateHttpFunctionCode"));

                    var updateResponse = await client.SendAsync(request);
                    result += await updateResponse.Content.ReadAsStringAsync() + Environment.NewLine;
                    log.LogInformation(result);
                }
            }
            while (continuationToken != null);

            return new OkObjectResult(result);
        }
    }
}
