using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Logging;

namespace BaGet.Azure
{
    public class AzureSearchBatchIndexer
    {
        public const int MaxBatchSize = 1000;

        private readonly SearchClient _indexClient;
        private readonly ILogger<AzureSearchBatchIndexer> _logger;

        public AzureSearchBatchIndexer(
            SearchClient indexClient,
            ILogger<AzureSearchBatchIndexer> logger)
        {
            _indexClient = indexClient ?? throw new ArgumentNullException(nameof(indexClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task IndexAsync(
            IReadOnlyList<IndexDocumentsAction<PackageDocument>> batch,
            CancellationToken cancellationToken)
        {
            if (batch.Count > MaxBatchSize)
                throw new ArgumentException($"Batch cannot have more than {MaxBatchSize} elements", nameof(batch));

            try
            {
                var indexBatch = IndexDocumentsBatch.Create<PackageDocument>(batch.ToArray());

                var response = await _indexClient.IndexDocumentsAsync(
                    indexBatch,
                    cancellationToken: cancellationToken);

                _logger.LogInformation(
                    "Pushed batch of {DocumentCount} documents",
                    batch.Count);

                if (response?.Value?.Results?.Any(r => !r.Succeeded) == true)
                {
                    throw new InvalidOperationException("Failed to push batch of documents");
                }
            }
            catch (RequestFailedException ex)
                when (ex.Status == (int)HttpStatusCode.RequestEntityTooLarge && batch.Count > 1)
            {
                var half = batch.Count / 2;
                var halfA = batch.Take(half).ToList();
                var halfB = batch.Skip(half).ToList();

                _logger.LogWarning(
                    ex,
                    "The request body for a batch of {BatchSize} was too large. " +
                    "Splitting into two batches of size {HalfA} and {HalfB}.",
                    batch.Count,
                    halfA.Count,
                    halfB.Count);

                await IndexAsync(halfA, cancellationToken);
                await IndexAsync(halfB, cancellationToken);
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Search indexing failed");
                throw;
            }
        }
    }
}
