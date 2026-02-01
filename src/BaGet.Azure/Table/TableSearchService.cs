using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BaGet.Core;
using BaGet.Protocol.Models;
using Azure;
using Azure.Data.Tables;

namespace BaGet.Azure
{
    public class TableSearchService : ISearchService
    {
        private readonly TableClient _table;
        private readonly ISearchResponseBuilder _responseBuilder;

        public TableSearchService(
            TableClient tableClient,
            ISearchResponseBuilder responseBuilder)
        {
            _table = tableClient ?? throw new ArgumentNullException(nameof(tableClient));
            _responseBuilder = responseBuilder ?? throw new ArgumentNullException(nameof(responseBuilder));
        }

        public async Task<SearchResponse> SearchAsync(
            SearchRequest request,
            CancellationToken cancellationToken)
        {
            var results = await SearchAsync(
                request.Query,
                request.Skip,
                request.Take,
                request.IncludePrerelease,
                request.IncludeSemVer2,
                cancellationToken);

            return _responseBuilder.BuildSearch(results);
        }

        public async Task<AutocompleteResponse> AutocompleteAsync(
            AutocompleteRequest request,
            CancellationToken cancellationToken)
        {
            var results = await SearchAsync(
                request.Query,
                request.Skip,
                request.Take,
                request.IncludePrerelease,
                request.IncludeSemVer2,
                cancellationToken);

            var packageIds = results.Select(p => p.PackageId).ToList();

            return _responseBuilder.BuildAutocomplete(packageIds);
        }

        public Task<AutocompleteResponse> ListPackageVersionsAsync(
            VersionsRequest request,
            CancellationToken cancellationToken)
        {
            // TODO: Support versions autocomplete.
            var response = _responseBuilder.BuildAutocomplete(new List<string>());
            return Task.FromResult(response);
        }

        public Task<DependentsResponse> FindDependentsAsync(
            string packageId,
            CancellationToken cancellationToken)
        {
            var response = _responseBuilder.BuildDependents(new List<PackageDependent>());
            return Task.FromResult(response);
        }

        private async Task<List<PackageRegistration>> SearchAsync(
            string searchText,
            int skip,
            int take,
            bool includePrerelease,
            bool includeSemVer2,
            CancellationToken cancellationToken)
        {
            var filter = GenerateSearchFilter(searchText, includePrerelease, includeSemVer2);

            var query = _table.QueryAsync<PackageEntity>(
                filter: filter,
                maxPerPage: skip + take,
                cancellationToken: cancellationToken);

            var results = new List<PackageEntity>();
            await foreach (var entity in query)
            {
                results.Add(entity);
            }

            return results
                .GroupBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
                .Select(g => new PackageRegistration(
                    g.Key,
                    g.Select(e => e.AsPackage()).ToList()
                ))
                .Skip(skip)
                .Take(take)
                .ToList();
        }

        private string GenerateSearchFilter(string searchText, bool includePrerelease, bool includeSemVer2)
        {
            var filters = new List<string>();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var prefix = searchText.TrimEnd().Split(' ').Last();
                var upperBound = prefix + "~"; // simple lexicographical upper bound

                filters.Add($"PartitionKey ge '{prefix}' and PartitionKey le '{upperBound}'");
            }

            filters.Add("Listed eq true");

            if (!includePrerelease)
            {
                filters.Add("IsPrerelease eq false");
            }

            if (!includeSemVer2)
            {
                filters.Add("SemVerLevel eq 0");
            }

            return string.Join(" and ", filters);
        }
    }
}
