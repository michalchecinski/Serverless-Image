using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage.Queue;
using ServerlessImage.Helpers;

namespace ServerlessImage
{
    public static class FileUpload
    {
        private static readonly List<string> FileTypes = new List<string> { "png", "jpg", "jpeg", "bmp" };
        private static readonly string FileTypesJson = JsonConvert.SerializeObject(FileTypes);


        [FunctionName("FileUpload")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "photo")] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var config = new ConfigurationBuilder()
                     .SetBasePath(context.FunctionAppDirectory)
                     .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                     .AddEnvironmentVariables()
                     .Build();

            string storageConnectionString = config["Storage"];

            try
            {
                var file = req.Form.Files[0];
                string folderName = "input";

                var url = "";
                
                if (file.Length > 0)
                {
                    var ext = Path.GetExtension(file.FileName).ToLower().Replace(".","");
                    if(!FileTypes.Contains(ext))
                    {
                        return new BadRequestObjectResult("Valid filetypes are only: " + FileTypesJson);
                    }

                    // Retrieve the storage account
                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
                    // Create the blob client, for use in obtaining references to blob storage containers
                    CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                    // Get container reference
                    var container = blobClient.GetContainerReference(folderName);

                    url = await BlobHelpers.UploadFileToBlob(file, container);
                    //url += BlobHelpers.GetBlobSasToken(container, Path.GetFileName(url), SharedAccessBlobPermissions.Read);

                    await  ServiceBusQueueHelpers.SendMessageAsync("analyze", url, config["ServiceBus"]);
                }
                
                return new OkObjectResult("Upload Successful.");
            }
            catch (System.Exception ex)
            {
                return new BadRequestObjectResult("Upload Failed: " + ex.Message);
            }
        }
    }
}
