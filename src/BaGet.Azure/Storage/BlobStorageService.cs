using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BaGet.Core;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace BaGet.Azure
{
    public class BlobStorageService : IStorageService
    {
        private readonly BlobContainerClient _container;

        public BlobStorageService(BlobContainerClient container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        public async Task<Stream> GetAsync(string path, CancellationToken cancellationToken)
        {
            var blob = _container.GetBlobClient(path);
            var response = await blob.DownloadAsync(cancellationToken);
            return response.Value.Content;
        }

        public Task<Uri> GetDownloadUriAsync(string path, CancellationToken cancellationToken)
        {
            var blob = _container.GetBlobClient(path);

            // Create SAS token valid for 10 minutes
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = blob.BlobContainerName,
                BlobName = blob.Name,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(10)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var uri = blob.GenerateSasUri(sasBuilder);
            return Task.FromResult(uri);
        }

        public async Task<StoragePutResult> PutAsync(
            string path,
            Stream content,
            string contentType,
            CancellationToken cancellationToken)
        {
            var blob = _container.GetBlobClient(path);

            try
            {
                await blob.UploadAsync(
                    content,
                    new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
                    cancellationToken);

                return StoragePutResult.Success;
            }
            catch (RequestFailedException ex) when (ex.ErrorCode == "BlobAlreadyExists")
            {
                // Compare content if blob already exists
                var download = await blob.DownloadAsync(cancellationToken);
                content.Position = 0;

                return content.Matches(download.Value.Content)
                    ? StoragePutResult.AlreadyExists
                    : StoragePutResult.Conflict;
            }
        }

        public async Task DeleteAsync(string path, CancellationToken cancellationToken)
        {
            var blob = _container.GetBlobClient(path);
            await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }
    }
}
