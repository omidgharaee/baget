using System;
using Azure;
using Azure.Data.Tables;
using BaGet.Core;

namespace BaGet.Azure
{
    public class PackageEntity : ITableEntity, IDownloadCount, IListed
    {
        // ITableEntity requires PartitionKey, RowKey, Timestamp, ETag
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string Id { get; set; }
        public string NormalizedVersion { get; set; }
        public string OriginalVersion { get; set; }
        public string Authors { get; set; }
        public string Description { get; set; }
        public long Downloads { get; set; }
        public bool HasReadme { get; set; }
        public bool HasEmbeddedIcon { get; set; }
        public bool IsPrerelease { get; set; }
        public string Language { get; set; }
        public bool Listed { get; set; }
        public string MinClientVersion { get; set; }
        public DateTime Published { get; set; }
        public bool RequireLicenseAcceptance { get; set; }
        public int SemVerLevel { get; set; }
        public string ReleaseNotes { get; set; }
        public string Summary { get; set; }
        public string Title { get; set; }
        public string IconUrl { get; set; }
        public string LicenseUrl { get; set; }
        public string ProjectUrl { get; set; }
        public string RepositoryUrl { get; set; }
        public string RepositoryType { get; set; }
        public string Tags { get; set; }
        public string Dependencies { get; set; }
        public string PackageTypes { get; set; }
        public string TargetFrameworks { get; set; }
    }

    public class PackageListingEntity : ITableEntity, IListed
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public bool Listed { get; set; }
    }

    public class PackageDownloadsEntity : ITableEntity, IDownloadCount
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public long Downloads { get; set; }
    }

    internal interface IListed
    {
        bool Listed { get; set; }
    }

    public interface IDownloadCount
    {
        long Downloads { get; set; }
    }
}
