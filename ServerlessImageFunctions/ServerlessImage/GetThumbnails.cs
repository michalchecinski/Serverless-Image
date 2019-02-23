using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using Microsoft.Extensions.Configuration;

namespace ServerlessImage
{
    public static class GetThumbnails
    {
        [FunctionName("GetThumbnails")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "thumbnails")] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var config = new ConfigurationBuilder()
                     .SetBasePath(context.FunctionAppDirectory)
                     .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                     .AddEnvironmentVariables()
                     .Build();

            string storageConnectionString = config["Storage"];

            // Retrieve the storage account
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            // Create the blob client, for use in obtaining references to blob storage containers
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            // Get container reference
            var container = blobClient.GetContainerReference("thumbs");

            var files = await BlobHelpers.BlobsList(container);
            var data = JsonConvert.SerializeObject(files);

            return new OkObjectResult(data);
        }
    }
}
