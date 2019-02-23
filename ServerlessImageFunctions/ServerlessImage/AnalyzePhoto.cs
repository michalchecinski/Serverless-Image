using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.ContentModerator;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.CognitiveServices.ContentModerator;
using Microsoft.CognitiveServices.ContentModerator.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace ServerlessImage
{
    public static class AnalyzePhoto
    {
        [FunctionName("AnalyzePhoto")]
        public static async Task RunAsync([ServiceBusTrigger("analyze", Connection = "ServiceBus")]string myQueueItem, ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {myQueueItem}");
            var targetContainer = "review";

            try
            {
                if (await IsAllowed(myQueueItem))
                {
                    targetContainer = "accepted";
                }
            }
            catch (Exception ex)
            {
                log.LogError($"Unable to add image to list. Caught {ex.GetType().FullName}: {ex.Message}");
            }

            string storageConnectionString = Environment.GetEnvironmentVariable("Storage");

            // Retrieve the storage account
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            // Create the blob client, for use in obtaining references to blob storage containers
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            await BlobHelpers.CopyFile(blobClient, "input", targetContainer, Path.GetFileName(myQueueItem));
        }

        readonly static ContentModeratorClient ContentModeratorClient =
           new ContentModeratorClient(new ApiKeyServiceClientCredentials(Environment.GetEnvironmentVariable("CMSubscriptionKey")))
           {
               Endpoint = "https://westeurope.api.cognitive.microsoft.com/"
           };

        static async Task<bool> IsAllowed(string imageUrl)
        {
            var url = new BodyModel("URL", imageUrl.Trim());
            var moderation = await ContentModeratorClient.ImageModeration
                .EvaluateUrlInputAsync("application/json", url);
            if (moderation.IsImageAdultClassified.GetValueOrDefault() ||
                moderation.IsImageRacyClassified.GetValueOrDefault())
                return false;

            return true;
        }
    }
}
