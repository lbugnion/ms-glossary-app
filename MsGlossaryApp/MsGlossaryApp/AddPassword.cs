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
using MsGlossaryApp.DataModel.Pass;
using MsGlossaryApp.Model.Pass;

namespace MsGlossaryApp
{
    public static class AddPassword
    {
        [FunctionName("HandlePassword")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "post",
                Route = "p")] HttpRequest req,
            ILogger log)
        {
            log?.LogInformation("-> HandlePassword");

            var (userEmail, fileName, _, _) = req.GetUserInfoFromHeaders();

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

            log?.LogDebug($"OldHash {passInfo.OldHash}");
            log?.LogDebug($"NewHash {passInfo.NewHash}");

            if (string.IsNullOrEmpty(passInfo.OldHash))
            {
                log?.LogError("No OldHash found");
                return new BadRequestObjectResult("Incomplete request");
            }

            var connectionString = Environment.GetEnvironmentVariable(
                Constants.AzureWebJobsStorageVariableName);
            var handler = new PassHandler(connectionString);
            var result = new PassResult();

            if (string.IsNullOrEmpty(passInfo.NewHash))
            {
                // Verify password

                try
                {
                    var (valid, first) = await handler.Verify(userEmail, fileName, passInfo.OldHash);
                    result.PassOk = valid;
                    result.IsFirstLogin = first;
                }
                catch (Exception ex)
                {
                    log?.LogError(ex, "Error interacting with table");

                    result.PassOk = false;
                    result.ErrorMessage = $"We had an issue, please contact support";

                    return new UnprocessableEntityObjectResult(result);
                }
            }
            else
            {
                // Change password

                try
                {
                    result.PassOk = await handler.Change(userEmail, fileName, passInfo.OldHash, passInfo.NewHash);
                }
                catch (Exception ex)
                {
                    log?.LogError(ex, "Error interacting with table");

                    result.PassOk = false;
                    result.ErrorMessage = $"We had an issue, please contact support";

                    return new UnprocessableEntityObjectResult(result);
                }
            }

            return new OkObjectResult(result);
        }
    }
}
