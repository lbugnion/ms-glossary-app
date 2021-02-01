using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using MsGlossaryApp.Model;
using Microsoft.Azure.Cosmos.Table;
using MsGlossaryApp.DataModel;
using System.Net;
using MsGlossaryApp.Model.Pass;

namespace MsGlossaryApp
{
    public static class AddPassword
    {
        [FunctionName("AddPassword")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Function, 
                "post", 
                Route = "add-p")] HttpRequest req,
            ILogger log)
        {
            log?.LogInformation("-> AddPassword");

            var (userEmail, fileName, _) = req.GetUserInfoFromHeaders();

            log?.LogDebug($"User email {userEmail}");
            log?.LogDebug($"Original fileName {fileName}");

            if (string.IsNullOrEmpty(userEmail))
            {
                log?.LogError("No user email found in header");
                return new BadRequestObjectResult("No user email found in header");
            }

            if (string.IsNullOrEmpty(fileName))
            {
                log?.LogError("No file name found in header");
                return new BadRequestObjectResult("No file name found in header");
            }

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var passInfo = JsonConvert.DeserializeObject<PassInfo>(requestBody);

            if (string.IsNullOrEmpty(passInfo.NewHash))
            {
                log?.LogError("No NewHash found");
                return new BadRequestObjectResult("Incomplete request");
            }

            var storageAccount = CloudStorageAccount.Parse(
                Environment.GetEnvironmentVariable(Constants.AzureWebJobsStorageVariableName));

            var tableClient = storageAccount.CreateCloudTableClient(
                new TableClientConfiguration());

            var passEntity = new PassEntity
            {
                PartitionKey = userEmail.ToLower(),
                RowKey = fileName.ToLower(),
                Hash = passInfo.NewHash
            };

            CloudTable table;

            try
            {
                table = tableClient.GetTableReference("login");
                await table.CreateIfNotExistsAsync();
            }
            catch (Exception ex)
            {
                log?.LogError(ex, "Error interacting with table");
                return new UnprocessableEntityObjectResult($"We had an issue, please contact support");
            }

            // Try to retrieve an existing entity
            var retrieveOperation = TableOperation.Retrieve<PassEntity>(
                passEntity.PartitionKey, passEntity.RowKey);

            TableResult retrieveResult;
            PassEntity existingPass;

            try
            {
                retrieveResult = await table.ExecuteAsync(retrieveOperation);
                existingPass = retrieveResult.Result as PassEntity;
            }
            catch (Exception ex)
            {
                log?.LogError(ex, "Error retrieving existing entity");
                return new UnprocessableEntityObjectResult($"We had an issue, please contact support");
            }

            if (existingPass == null)
            {
                // No password defined yet for this user / filename
                var insertOperation = TableOperation.Insert(passEntity);

                TableResult insertResult;

                try
                {
                    insertResult = await table.ExecuteAsync(insertOperation);
                }
                catch (Exception ex)
                {
                    log?.LogError(ex, "Error inserting new entity");
                    return new UnprocessableEntityObjectResult($"We had an issue, please contact support");
                }
            }
            else
            {
                // Verify the old password
                if (passInfo.OldHash != existingPass.Hash)
                {
                    var result = new PassResult
                    {
                        ErrorMessage = "Incorrect password"
                    };

                    return new BadRequestObjectResult(result);
                }

                // Replace password
                passEntity.Hash = passInfo.NewHash;
                var insertOperation = TableOperation.InsertOrMerge(passEntity);

                TableResult insertResult;

                try
                {
                    insertResult = await table.ExecuteAsync(insertOperation);
                }
                catch (Exception ex)
                {
                    log?.LogError(ex, "Error inserting new entity");
                    return new UnprocessableEntityObjectResult($"We had an issue, please contact support");
                }
            }

            return new OkObjectResult("OK");
        }
    }
}
