using System;
using BaGet.Azure;
using BaGet.Core;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Storage.Blobs;
using Azure.Data.Tables;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace BaGet
{
    public static class AzureApplicationExtensions
    {
        // ---------------- TABLE STORAGE ----------------

        public static BaGetApplication AddAzureTableDatabase(this BaGetApplication app)
        {
            app.Services.AddBaGetOptions<AzureTableOptions>(nameof(BaGetOptions.Database));

            app.Services.AddTransient<TablePackageDatabase>();
            app.Services.AddTransient<TableOperationBuilder>();
            app.Services.AddTransient<TableSearchService>();
            app.Services.TryAddTransient<IPackageDatabase>(p => p.GetRequiredService<TablePackageDatabase>());
            app.Services.TryAddTransient<ISearchService>(p => p.GetRequiredService<TableSearchService>());
            app.Services.TryAddTransient<ISearchIndexer>(p => p.GetRequiredService<NullSearchIndexer>());

            app.Services.AddSingleton(p =>
            {
                var options = p.GetRequiredService<IOptions<AzureTableOptions>>().Value;
                return new TableServiceClient(options.TableName);
            });

            app.Services.AddTransient(p =>
            {
                var options = p.GetRequiredService<IOptionsSnapshot<AzureTableOptions>>().Value;
                var client = p.GetRequiredService<TableServiceClient>();
                return client.GetTableClient(options.TableName);
            });

            app.Services.AddProvider<IPackageDatabase>((p, config) =>
            {
                if (!config.HasDatabaseType("AzureTable")) return null;
                return p.GetRequiredService<TablePackageDatabase>();
            });

            app.Services.AddProvider<ISearchService>((p, config) =>
            {
                if (!config.HasSearchType("Database")) return null;
                if (!config.HasDatabaseType("AzureTable")) return null;
                return p.GetRequiredService<TableSearchService>();
            });

            app.Services.AddProvider<ISearchIndexer>((p, config) =>
            {
                if (!config.HasSearchType("Database")) return null;
                if (!config.HasDatabaseType("AzureTable")) return null;
                return p.GetRequiredService<NullSearchIndexer>();
            });

            return app;
        }

        // ---------------- BLOB STORAGE ----------------

        public static BaGetApplication AddAzureBlobStorage(this BaGetApplication app)
        {
            app.Services.AddBaGetOptions<AzureBlobStorageOptions>(nameof(BaGetOptions.Storage));
            app.Services.AddTransient<BlobStorageService>();
            app.Services.TryAddTransient<IStorageService>(p => p.GetRequiredService<BlobStorageService>());

            app.Services.AddSingleton(p =>
            {
                var options = p.GetRequiredService<IOptions<AzureBlobStorageOptions>>().Value;
                return new BlobServiceClient(options.ConnectionString);
            });

            app.Services.AddTransient(p =>
            {
                var options = p.GetRequiredService<IOptionsSnapshot<AzureBlobStorageOptions>>().Value;
                var service = p.GetRequiredService<BlobServiceClient>();
                return service.GetBlobContainerClient(options.Container);
            });

            app.Services.AddProvider<IStorageService>((p, config) =>
            {
                if (!config.HasStorageType("AzureBlobStorage")) return null;
                return p.GetRequiredService<BlobStorageService>();
            });

            return app;
        }

        // ---------------- AZURE SEARCH ----------------

        public static BaGetApplication AddAzureSearch(this BaGetApplication app)
        {
            app.Services.AddBaGetOptions<AzureSearchOptions>(nameof(BaGetOptions.Search));

            app.Services.AddTransient<AzureSearchBatchIndexer>();
            app.Services.AddTransient<AzureSearchService>();
            app.Services.AddTransient<AzureSearchIndexer>();
            app.Services.AddTransient<IndexActionBuilder>();

            app.Services.TryAddTransient<ISearchService>(p => p.GetRequiredService<AzureSearchService>());
            app.Services.TryAddTransient<ISearchIndexer>(p => p.GetRequiredService<AzureSearchIndexer>());

            app.Services.AddSingleton(p =>
            {
                var options = p.GetRequiredService<IOptions<AzureSearchOptions>>().Value;
                return new SearchClient(
                    new Uri(options.AccountName),
                    PackageDocument.IndexName,
                    new AzureKeyCredential(options.ApiKey));
            });

            app.Services.AddSingleton(p =>
            {
                var options = p.GetRequiredService<IOptions<AzureSearchOptions>>().Value;
                return new SearchIndexClient(
                    new Uri(options.AccountName),
                    new AzureKeyCredential(options.ApiKey));
            });

            app.Services.AddProvider<ISearchService>((p, config) =>
            {
                if (!config.HasSearchType("AzureSearch")) return null;
                return p.GetRequiredService<AzureSearchService>();
            });

            app.Services.AddProvider<ISearchIndexer>((p, config) =>
            {
                if (!config.HasSearchType("AzureSearch")) return null;
                return p.GetRequiredService<AzureSearchIndexer>();
            });

            return app;
        }
    }
}
