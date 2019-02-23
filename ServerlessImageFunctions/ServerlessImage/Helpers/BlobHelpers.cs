using Microsoft.AspNetCore.Http;
using Microsoft.WindowsAzure.Storage.Blob;
using ServerlessImage.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerlessImage
{
    public class BlobHelpers
    {
        public static async Task<string> UploadFileToBlob(IFormFile file, CloudBlobContainer container)
        {
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);

                await container.CreateIfNotExistsAsync();

                CloudBlockBlob blockBlob = container.GetBlockBlobReference(file.FileName);

                // Upload the file
                memoryStream.Seek(0, SeekOrigin.Begin);
                await blockBlob.UploadFromStreamAsync(memoryStream);
                return blockBlob.Uri.ToString();
            }
        }

        public static async Task<List<BlobFile>> BlobsList(CloudBlobContainer cloudBlobContainer)
        {
            List<BlobFile> blobsList = new List<BlobFile>();

            BlobContinuationToken blobContinuationToken = null;
            do
            {
                var results = await cloudBlobContainer.ListBlobsSegmentedAsync(null, blobContinuationToken);
                blobContinuationToken = results.ContinuationToken;

                foreach (IListBlobItem item in results.Results)
                {
                    var itemUri = item.Uri.ToString();

                    var itemName = Path.GetFileName(itemUri);
                    CloudBlockBlob blobData = cloudBlobContainer.GetBlockBlobReference(itemName);

                    // Construct the SAS URL for blob
                    var itemUrl = itemUri + GetBlobSasToken(cloudBlobContainer, itemName, SharedAccessBlobPermissions.Read);

                    blobsList.Add(new BlobFile(itemUrl, itemName));
                }
            } while (blobContinuationToken != null);

            return blobsList;
        }

        public static string GetBlobSasToken(CloudBlobContainer container, string blobName, SharedAccessBlobPermissions permissions, string policyName = null)
        {
            string sasBlobToken;

            // Get a reference to a blob within the container.
            // Note that the blob may not exist yet, but a SAS can still be created for it.
            CloudBlockBlob blob = container.GetBlockBlobReference(blobName);

            if (policyName == null)
            {
                var adHocSas = CreateAdHocSasPolicy(permissions);

                // Generate the shared access signature on the blob, setting the constraints directly on the signature.
                sasBlobToken = blob.GetSharedAccessSignature(adHocSas);
            }
            else
            {
                // Generate the shared access signature on the blob. In this case, all of the constraints for the
                // shared access signature are specified on the container's stored access policy.
                sasBlobToken = blob.GetSharedAccessSignature(null, policyName);
            }

            return sasBlobToken;
        }

        public static async Task CopyFile(CloudBlobClient cloudBlobClient, string sourceContainerName, string targetContainerName, string blobName)
        {
            CloudBlobContainer sourceContainer = cloudBlobClient.GetContainerReference(sourceContainerName);
            CloudBlobContainer targetContainer = cloudBlobClient.GetContainerReference(targetContainerName);
            await targetContainer.CreateIfNotExistsAsync();
            CloudBlockBlob sourceBlob = sourceContainer.GetBlockBlobReference(blobName);
            CloudBlockBlob targetBlob = targetContainer.GetBlockBlobReference(blobName);
            await targetBlob.StartCopyAsync(sourceBlob);
        }

        private static SharedAccessBlobPolicy CreateAdHocSasPolicy(SharedAccessBlobPermissions permissions)
        {
            // Create a new access policy and define its constraints.
            // Note that the SharedAccessBlobPolicy class is used both to define the parameters of an ad-hoc SAS, and 
            // to construct a shared access policy that is saved to the container's shared access policies. 

            return new SharedAccessBlobPolicy()
            {
                // Set start time to five minutes before now to avoid clock skew.
                SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-5),
                SharedAccessExpiryTime = DateTime.UtcNow.AddDays(2),
                Permissions = permissions
            };
        }

    }
}
