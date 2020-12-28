using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using MsGlossaryApp.DataModel;

namespace MsGlossaryApp
{
    public static class SaveSynopsis
    {
        [FunctionName("SaveSynopsis")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Function, 
                "post", 
                Route = "synopsis")] 
            HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            var synopsis = JsonConvert.DeserializeObject<Term>(requestBody);
            synopsis.Stage = Term.TermStage.Synopsis;

            return new OkObjectResult(synopsis);
        }
    }
}
