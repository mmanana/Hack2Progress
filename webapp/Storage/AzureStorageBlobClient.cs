using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace webapp.Storage
{
    public class AzureStorageBlobClient
    {
        private CloudBlobClient _blobClient;

        public AzureStorageBlobClient(string storageConnectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            _blobClient = storageAccount.CreateCloudBlobClient();
        }

        public async Task AddFileAsync(string containerName, string fileName, byte[] buffer)
        {
            CloudBlobContainer container = _blobClient.GetContainerReference(containerName);
            await container.CreateIfNotExistsAsync();
            CloudBlockBlob blobRef = container.GetBlockBlobReference(fileName);

            var blobRequestOptions = new BlobRequestOptions()
            {
                RetryPolicy = new ExponentialRetry(TimeSpan.FromSeconds(1), 5)
            };

            await blobRef.UploadFromByteArrayAsync(buffer, 0, buffer.Length, null, blobRequestOptions, null);
        }

        public async Task<IEnumerable<string>> GetUriBlobsNewestFirst(string containerName)
        {
            var blobs = new List<CloudBlob>();

            CloudBlobContainer container = _blobClient.GetContainerReference(containerName);
            BlobContinuationToken blobContinuationToken = null;
            do
            {
                var results = await container.ListBlobsSegmentedAsync(null, blobContinuationToken);
                // Get the value of the continuation token returned by the listing call.
                blobContinuationToken = results.ContinuationToken;
                foreach (CloudBlob item in results.Results.OfType<CloudBlob>())
                {
                    blobs.Add(item);
                }
            } while (blobContinuationToken != null); // Loop while the continuation token is not null.

            return blobs
                .OrderByDescending(pi => pi.Properties.Created)
                .Select(pi => pi.Uri.AbsoluteUri);
        }
    }
}
