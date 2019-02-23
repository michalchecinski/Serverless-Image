using System;
using System.IO;
using ImageResizer;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace ServerlessImage
{
    public static class ResizePhoto
    {
        [FunctionName("ResizePhoto")]
        public static async System.Threading.Tasks.Task Run([BlobTrigger("accepted/{name}", Connection = "Storage")]Stream image, string name, ILogger log)
        {

            var instructions = new Instructions
            {
                Height = 320,
                Width = 200,
                Mode = FitMode.Carve,
                Scale = ScaleMode.Both
            };
            using (Stream imageSmall = new MemoryStream())
            {
                ImageBuilder.Current.Build(new ImageJob(image, imageSmall, instructions));

                // Retrieve the storage account
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("Storage"));
                // Create the blob client, for use in obtaining references to blob storage containers
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                // Get container reference
                var container = blobClient.GetContainerReference("thumbs");
                var blob = container.GetBlockBlobReference(name);

                imageSmall.Seek(0, SeekOrigin.Begin);

                await blob.UploadFromStreamAsync(imageSmall);
            }

            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {image.Length} Bytes");
        }
    }
}
