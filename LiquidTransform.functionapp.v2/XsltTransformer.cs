using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Xsl;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;

namespace LiquidTransform.functionapp.v3
{
    public static class XsltTransformer
    {
        [FunctionName("XsltTransformer")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestMessage req,
            ILogger log)
        {
            log.LogInformation("HTTP Trigger has received a request for LiquidTransformer");
            string xslttransformfilename;
            string inputBlob;
            try
            {
                xslttransformfilename = req.Headers.GetValues("Filename").First();
                log.LogInformation(filename);
                var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
                var blobClient = storageAccount.CreateCloudBlobClient();
                var container = blobClient.GetContainerReference("liquid-transforms").GetBlockBlobReference(filename);
                inputBlob = await container.DownloadTextAsync();
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                return req.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message);
            }
            var request = req.Content.ReadAsStringAsync();
            log.LogInformation("C# HTTP trigger function processed a request.");
            log.LogInformation("File to be requested is " + xslttransformfilename );
            if (inputBlob == null)
            {
                if (xslttransformfilename == "" || xslttransformfilename == null)
                    return req.CreateErrorResponse(HttpStatusCode.BadRequest, "No Header Parameter for XSLT Transform");
                log.LogError("inputBlob null");
                return req.CreateErrorResponse(HttpStatusCode.NotFound, "XSLT transform not found");
            }


            var sr = new StreamReader(inputBlob);
            var xsltTransform = sr.ReadToEnd();

            string output = string.Empty;
            log.LogInformation("Incoming XML:\n" + await request);
            using (StringReader srt = new StringReader(xsltTransform)) // xslInput is a string that contains xsl
            using (StringReader sri = new StringReader(await request)) // xmlInput is a string that contains xml
            {
                using (XmlReader xrt = XmlReader.Create(srt))
                using (XmlReader xri = XmlReader.Create(sri))
                {
                    XslCompiledTransform xslt = new XslCompiledTransform();
                    xslt.Load(xrt);
                    using (StringWriter sw = new StringWriter())
                    using (XmlWriter xwo = XmlWriter.Create(sw, xslt.OutputSettings)) // use OutputSettings of xsl, so it can be output as HTML
                    {
                        xslt.Transform(xri, xwo);
                        output = sw.ToString();
                    }
                }
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(output, Encoding.UTF8,"application/xml")
            };
        }
    }
}

