using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using BaGet.Core;
using Microsoft.Extensions.Logging;
using NuGet.Versioning;

namespace BaGet.Azure
{
    /// <summary>
    /// Stores the metadata of packages using Azure Table Storage (new Azure.Data.Tables SDK).
    /// </summary>
    public class TablePackageDatabase : IPackageDatabase
    {
        private const int MaxPreconditionFailures = 5;

        private readonly TableOperationBuilder _operationBuilder;
        private readonly TableClient _table;
        private readonly ILogger<TablePackageDatabase> _logger;

        public TablePackageDatabase(
            TableOperationBuilder operationBuilder,
            TableClient table,
            ILogger<TablePackageDatabase> logger)
        {
            _operationBuilder = operationBuilder ?? throw new ArgumentNullException(nameof(operationBuilder));
            _table = table ?? throw new ArgumentNullException(nameof(table));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PackageAddResult> AddAsync(Package package, CancellationToken cancellationToken)
        {
            try
            {
                var entity = _operationBuilder.CreatePackageEntity(package);
                await _table.AddEntityAsync(entity, cancellationToken);
            }
            catch (RequestFailedException e) when (e.Status == 409)
            {
                return PackageAddResult.PackageAlreadyExists;
            }

            return PackageAddResult.Success;
        }

        public async Task AddDownloadAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
        {
            var attempt = 0;
            var partitionKey = id.ToLowerInvariant();
            var rowKey = version.ToNormalizedString().ToLowerInvariant();

            while (true)
            {
                try
                {
                    var response = await _table.GetEntityAsync<PackageDownloadsEntity>(partitionKey, rowKey, cancellationToken: cancellationToken);
                    var entity = response.Value;

                    entity.Downloads += 1;

                    await _table.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace, cancellationToken);
                    return;
                }
                catch (RequestFailedException e) when (e.Status == 412 && attempt < MaxPreconditionFailures)
                {
                    attempt++;
                    _logger.LogWarning(e, $"Retrying due to precondition failure, attempt {attempt} of {MaxPreconditionFailures}");
                }
                catch (RequestFailedException e) when (e.Status == 404)
                {
                    return;
                }
            }
        }

        public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken)
        {
            try
            {
                await foreach (var _ in _table.QueryAsync<PackageEntity>(
                    filter: $"PartitionKey eq '{id.ToLowerInvariant()}'",
                    maxPerPage: 1,
                    cancellationToken: cancellationToken))
                {
                    return true;
                }
            }
            catch (RequestFailedException)
            {
                return false;
            }

            return false;
        }

        public async Task<bool> ExistsAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _table.GetEntityAsync<PackageEntity>(
                    id.ToLowerInvariant(),
                    version.ToNormalizedString().ToLowerInvariant(),
                    cancellationToken: cancellationToken);

                return response.Value != null;
            }
            catch (RequestFailedException e) when (e.Status == 404)
            {
                return false;
            }
        }

        public async Task<IReadOnlyList<Package>> FindAsync(string id, bool includeUnlisted, CancellationToken cancellationToken)
        {
            var filter = $"PartitionKey eq '{id.ToLowerInvariant()}'";

            if (!includeUnlisted)
            {
                filter += $" and Listed eq true";
            }

            var results = new List<Package>();

            await foreach (var entity in _table.QueryAsync<PackageEntity>(filter, cancellationToken: cancellationToken))
            {
                results.Add(entity.AsPackage());
            }

            return results.OrderBy(p => p.Version).ToList();
        }

        public async Task<Package> FindOrNullAsync(string id, NuGetVersion version, bool includeUnlisted, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _table.GetEntityAsync<PackageEntity>(
                    id.ToLowerInvariant(),
                    version.ToNormalizedString().ToLowerInvariant(),
                    cancellationToken: cancellationToken);

                var entity = response.Value;
                if (!includeUnlisted && !entity.Listed)
                {
                    return null;
                }

                return entity.AsPackage();
            }
            catch (RequestFailedException e) when (e.Status == 404)
            {
                return null;
            }
        }

        public async Task<bool> HardDeletePackageAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
        {
            return await TryUpdatePackageAsync(_operationBuilder.HardDeletePackageEntity(id, version), cancellationToken);
        }

        public async Task<bool> RelistPackageAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
        {
            return await TryUpdatePackageAsync(_operationBuilder.RelistPackageEntity(id, version), cancellationToken);
        }

        public async Task<bool> UnlistPackageAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
        {
            return await TryUpdatePackageAsync(_operationBuilder.UnlistPackageEntity(id, version), cancellationToken);
        }

        private async Task<bool> TryUpdatePackageAsync(TableEntity entity, CancellationToken cancellationToken)
        {
            try
            {
                await _table.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Merge, cancellationToken);
                return true;
            }
            catch (RequestFailedException e) when (e.Status == 404)
            {
                return false;
            }
        }
    }
}
