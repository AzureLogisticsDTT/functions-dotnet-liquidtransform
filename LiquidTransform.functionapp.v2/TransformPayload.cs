/*using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json.Linq;
using System.Net.Http;

namespace Centric.Marketing_Loyalty.Product.List.Function
{
    public static class TransformPayload
    {
        [FunctionName("TransformPayload")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            log.LogInformation("TransformPayload has Started");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            JObject data = JsonConvert.DeserializeObject<JObject>(requestBody);
            var instanceId = await starter.StartNewAsync("Orchestra", data);
            log.LogInformation("Started a new process started ");

            string response = await starter.StartNewAsync< HttpRequestMessage>("LiquidTransformer", req);
            starter.CallActivityAsync< HttpRequestMessage>

            return response;
        }
    }
}
*/