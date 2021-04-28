using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using DotLiquid;
using System.Text;
using System;
using Microsoft.Extensions.Logging;
using System.Xml.Linq;

namespace LiquidTransform.functionapp.v3
{
    public static class LiquidTransformer
    {
        /// <summary>
        /// Converts Json to XML using a Liquid mapping. The filename of the liquid map needs to be provided in the path. 
        /// The tranformation is executed with the HTTP request body as input.
        /// </summary>
        /// <param name="req"></param>
        /// <param name="inputBlob"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("LiquidTransformer")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "liquidtransformer/{liquidtransformfilename}")] HttpRequestMessage req,
            [Blob("liquid-transforms/{liquidtransformfilename}", FileAccess.Read)] Stream inputBlob,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            //HttpClient client = new HttpClient();
            //var responseString = await client.GetStringAsync("https://azurelogisticsdtt.blob.core.windows.net/liquid-transforms/teste.liquid");
            //log.LogInformation(responseString);


            if (inputBlob == null)
            {
                log.LogError("inputBlob null");
                return req.CreateErrorResponse(HttpStatusCode.NotFound, "Liquid transform not found");
            }

            // This indicates the response content type. If set to application/json it will perform additional formatting
            // Otherwise the Liquid transform is returned unprocessed.
            string requestContentType = req.Content.Headers.ContentType.MediaType;
            string responseContentType = req.Headers.Accept.FirstOrDefault().MediaType;

            // Load the Liquid transform in a string
            var sr = new StreamReader(inputBlob);
            var liquidTransform = sr.ReadToEnd();

            var contentReader = ContentFactory.GetContentReader(requestContentType);
            var contentWriter = ContentFactory.GetContentWriter(responseContentType);

            Hash inputHash;

            log.LogInformation("Parsing Request...\n");

            try
            {
                inputHash = await contentReader.ParseRequestAsync(req.Content);

            }
            catch (Exception ex)
            {
                log.LogError(ex.Message, ex);
                return req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error parsing request body", ex);
            }
            log.LogInformation("Done!\n");


            // Register the Liquid custom filter extensions
            Template.RegisterFilter(typeof(CustomFilters));

            // Execute the Liquid transform
            Template template;
            log.LogInformation("Parsing Template.....");

            try
            {
                template = Template.Parse(liquidTransform);
                //template = Template.Parse(responseString);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message, ex);
                return req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error parsing Liquid template", ex);
            }
            log.LogInformation("Done!\n");


            string output = string.Empty;

            log.LogInformation("Rendering Output...\n");


            try
            {
                output = template.Render(inputHash);        
                switch (responseContentType)
                {
                    case "application/xml":
                        var xDoc = XDocument.Parse(output);
                        output = xDoc.ToString();
                        break;
                    case "application/json":
                        dynamic parsedJson = JsonConvert.DeserializeObject(output);
                        output = JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message, ex);
                return req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error rendering Liquid template", ex);
            }
            log.LogInformation("Done");


            if (template.Errors != null && template.Errors.Count > 0)
            {
                log.LogInformation("but it has errors...\n");

                if (template.Errors[0].InnerException != null)
                {
                    return req.CreateErrorResponse(HttpStatusCode.InternalServerError, $"Error rendering Liquid template: {template.Errors[0].Message}", template.Errors[0].InnerException);
                }
                else
                {
                    log.LogInformation($"Warning rendering Liquid template: {template.Errors[0].Message}");
                    //return req.CreateErrorResponse(HttpStatusCode.InternalServerError, $"Error rendering Liquid template: {template.Errors[0].Message}");
                }
            }

            log.LogInformation("Writing Response!!\n");
            try
            {
                var content = contentWriter.CreateResponse(output);
                log.LogInformation("Request Complete!\n");
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = content
                };
            }
            catch (Exception ex)
            {
                log.LogInformation("Failed to Write Response.\n");
                // Just log the error, and return the Liquid output without parsing
                log.LogError(ex.Message, ex);

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(output, Encoding.UTF8, responseContentType)
                };
            }
        }
    }
}
