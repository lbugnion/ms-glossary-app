using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using MsGlossaryApp.DataModel;
using MsGlossaryApp.Model;
using System;
using System.Threading.Tasks;

namespace MsGlossaryApp
{
    public static class UploadFile
    {
        [FunctionName("UploadFile")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "post",
                Route = null)]
            HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var email = req.Query["e"];
            var fileName = req.Query["f"];

            var account = CloudStorageAccount.Parse(
                Environment.GetEnvironmentVariable(
                    Constants.AzureWebJobsStorageVariableName));

            var blobClient = account.CreateCloudBlobClient();
            var blobHelper = new BlobHelper(blobClient, log);
            var imageContainer = blobHelper.GetContainerFromVariable(
                Constants.ImageContainerVariableName);

            var uniqueFileName = $"{email}-{Guid.NewGuid()}-{fileName}";

            var blob = imageContainer.GetBlockBlobReference(uniqueFileName);
            await blob.UploadFromStreamAsync(req.Body);

            return new OkObjectResult(blob.Uri.ToString());
        }
    }
}