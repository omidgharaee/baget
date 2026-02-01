using System;
using System.ComponentModel.DataAnnotations;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace BaGet.Azure
{
    public class PackageDocument : KeyedDocument
    {
        public const string IndexName = "packages";

        [SearchableField(IsFilterable = true, IsSortable = true)]
        public string Id { get; set; }

        /// <summary>
        /// The package's full version after normalization (SemVer 2.0.0 supported)
        /// </summary>
        [SearchableField(IsFilterable = true, IsSortable = true)]
        public string Version { get; set; }

        [SearchableField]
        public string Description { get; set; }

        public string[] Authors { get; set; }

        public bool HasEmbeddedIcon { get; set; }

        public string IconUrl { get; set; }

        public string LicenseUrl { get; set; }

        public string ProjectUrl { get; set; }

        public DateTimeOffset Published { get; set; }

        [SearchableField]
        public string Summary { get; set; }

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string[] Tags { get; set; }

        [SearchableField]
        public string Title { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public long TotalDownloads { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public int DownloadsMagnitude { get; set; }

        /// <summary>
        /// All normalized versions
        /// </summary>
        public string[] Versions { get; set; }

        public string[] VersionDownloads { get; set; }

        [SearchableField(AnalyzerName = ExactMatchCustomAnalyzer.Name)]
        public string[] Dependencies { get; set; }

        [SearchableField(AnalyzerName = ExactMatchCustomAnalyzer.Name)]
        public string[] PackageTypes { get; set; }

        [SearchableField(AnalyzerName = ExactMatchCustomAnalyzer.Name)]
        public string[] Frameworks { get; set; }

        [SimpleField(IsFilterable = true)]
        public string SearchFilters { get; set; }
    }

    public class KeyedDocument : IKeyedDocument
    {
        [Key]
        [SimpleField(IsKey = true)]
        public string Key { get; set; }
    }

    public interface IKeyedDocument
    {
        string Key { get; set; }
    }
}
