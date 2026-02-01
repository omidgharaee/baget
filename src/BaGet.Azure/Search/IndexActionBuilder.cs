using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BaGet.Core;
using Azure.Search.Documents.Models;

namespace BaGet.Azure
{
    public class IndexActionBuilder
    {
        public virtual IReadOnlyList<IndexDocumentsAction<PackageDocument>> AddPackage(
            PackageRegistration registration)
        {
            return AddOrUpdatePackage(registration, isUpdate: false);
        }

        public virtual IReadOnlyList<IndexDocumentsAction<PackageDocument>> UpdatePackage(
            PackageRegistration registration)
        {
            return AddOrUpdatePackage(registration, isUpdate: true);
        }

        private IReadOnlyList<IndexDocumentsAction<PackageDocument>> AddOrUpdatePackage(
            PackageRegistration registration,
            bool isUpdate)
        {
            var encodedId = EncodePackageId(registration.PackageId.ToLowerInvariant());
            var result = new List<IndexDocumentsAction<PackageDocument>>();

            for (var i = 0; i < 4; i++)
            {
                var includePrerelease = (i & 1) != 0;
                var includeSemVer2 = (i & 2) != 0;
                var searchFilters = (SearchFilters)i;

                var documentKey = $"{encodedId}-{searchFilters}";
                var filtered = registration.Packages.Where(p => p.Listed);

                if (!includePrerelease)
                {
                    filtered = filtered.Where(p => !p.IsPrerelease);
                }

                if (!includeSemVer2)
                {
                    filtered = filtered.Where(p => p.SemVerLevel != SemVerLevel.SemVer2);
                }

                var versions = filtered.OrderBy(p => p.Version).ToList();
                if (versions.Count == 0)
                {
                    if (isUpdate)
                    {
                        var action = IndexDocumentsAction.Delete(
                            new PackageDocument
                            {
                                Key = documentKey
                            });

                        result.Add(action);
                    }

                    continue;
                }

                var latest = versions.Last();
                var dependencies = latest
                    .Dependencies
                    .Select(d => d.Id?.ToLowerInvariant())
                    .Where(d => d != null)
                    .Distinct()
                    .ToArray();

                var document = new PackageDocument
                {
                    Key = documentKey,
                    Id = latest.Id,
                    Version = latest.Version.ToFullString(),
                    Description = latest.Description,
                    Authors = latest.Authors,
                    HasEmbeddedIcon = latest.HasEmbeddedIcon,
                    IconUrl = latest.IconUrlString,
                    LicenseUrl = latest.LicenseUrlString,
                    ProjectUrl = latest.ProjectUrlString,
                    Published = latest.Published,
                    Summary = latest.Summary,
                    Tags = latest.Tags,
                    Title = latest.Title,
                    TotalDownloads = versions.Sum(p => p.Downloads),
                    DownloadsMagnitude = versions.Sum(p => p.Downloads).ToString().Length,
                    Versions = versions.Select(p => p.Version.ToFullString()).ToArray(),
                    VersionDownloads = versions.Select(p => p.Downloads.ToString()).ToArray(),
                    Dependencies = dependencies,
                    PackageTypes = latest.PackageTypes.Select(t => t.Name).ToArray(),
                    Frameworks = latest.TargetFrameworks.Select(f => f.Moniker.ToLowerInvariant()).ToArray(),
                    SearchFilters = searchFilters.ToString()
                };

                result.Add(
                    isUpdate
                        ? IndexDocumentsAction.MergeOrUpload<PackageDocument>(document)
                        : IndexDocumentsAction.Upload<PackageDocument>(document));
            }

            return result;
        }

        private string EncodePackageId(string key)
        {
            var bytes = Encoding.UTF8.GetBytes(key);
            var base64 = Convert.ToBase64String(bytes);

            return base64.Replace('+', '-').Replace('/', '_');
        }
    }
}
