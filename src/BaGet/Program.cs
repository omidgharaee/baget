using System;
using BaGet;
using BaGet.Core;
using BaGet.Web;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

// ----------------------
// Top-level statements
// ----------------------

// Builder
var builder = WebApplication.CreateBuilder(args);

// Configuration root
var root = Environment.GetEnvironmentVariable("BAGET_CONFIG_ROOT");
if (!string.IsNullOrEmpty(root))
{
    builder.Configuration.SetBasePath(root);
}

// Register services (equivalent to ConfigureServices)
builder.Services.AddTransient<IConfigureOptions<CorsOptions>, ConfigureBaGetOptions>();
builder.Services.AddTransient<IConfigureOptions<FormOptions>, ConfigureBaGetOptions>();
builder.Services.AddTransient<IConfigureOptions<ForwardedHeadersOptions>, ConfigureBaGetOptions>();
builder.Services.AddTransient<IConfigureOptions<IISServerOptions>, ConfigureBaGetOptions>();
builder.Services.AddTransient<IValidateOptions<BaGetOptions>, ConfigureBaGetOptions>();

builder.Services.AddBaGetOptions<IISServerOptions>(nameof(IISServerOptions));
builder.Services.AddBaGetWebApplication(app =>
{
    // Add database providers
    app.AddAzureTableDatabase();
    app.AddMySqlDatabase();
    app.AddPostgreSqlDatabase();
    app.AddSqliteDatabase();
    app.AddSqlServerDatabase();

    // Add storage providers
    app.AddFileStorage();
    app.AddAliyunOssStorage();
    app.AddAwsS3Storage();
    app.AddAzureBlobStorage();
    app.AddGoogleCloudStorage();

    // Add search providers
    app.AddAzureSearch();
});

// Register subsystem services
builder.Services.AddScoped(DependencyInjectionExtensions.GetServiceFromProviders<IContext>);
builder.Services.AddTransient(DependencyInjectionExtensions.GetServiceFromProviders<IStorageService>);
builder.Services.AddTransient(DependencyInjectionExtensions.GetServiceFromProviders<IPackageDatabase>);
builder.Services.AddTransient(DependencyInjectionExtensions.GetServiceFromProviders<ISearchService>);
builder.Services.AddTransient(DependencyInjectionExtensions.GetServiceFromProviders<ISearchIndexer>);

// Razor runtime compilation
builder.Services.AddSingleton<IConfigureOptions<MvcRazorRuntimeCompilationOptions>, ConfigureRazorRuntimeCompilation>();

// Cors
builder.Services.AddCors();

// Register ValidateStartupOptions to avoid InvalidOperationException
builder.Services.AddSingleton<ValidateStartupOptions>();

// Configure Kestrel
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = null;

    if (builder.Environment.IsProduction())
    {
        options.ListenAnyIP(80);
    }
});

var host = builder.Build();

// Validate startup options
if (!host.ValidateStartupOptions())
{
    return;
}

// ----------------------
// CommandLineApp
// ----------------------
var appCli = new CommandLineApplication
{
    Name = "baget",
    Description = "A light-weight NuGet service"
};

appCli.HelpOption(inherited: true);

appCli.Command("import", import =>
{
    import.Command("downloads", downloads =>
    {
        downloads.OnExecuteAsync(async cancellationToken =>
        {
            using var scope = host.Services.CreateScope();
            var importer = scope.ServiceProvider.GetRequiredService<DownloadsImporter>();

            await importer.ImportAsync(cancellationToken);
        });
    });
});

appCli.Option("--urls", "The URLs that BaGet should bind to.", CommandOptionType.SingleValue);

appCli.OnExecuteAsync(async cancellationToken =>
{
    // Run migrations
    await host.RunMigrationsAsync(cancellationToken);

    // Run web app
    var app = host;

    var options = host.Services.GetRequiredService<IOptions<BaGetOptions>>().Value;

    if (builder.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        app.UseStatusCodePages();
    }

    app.UseForwardedHeaders();
    app.UsePathBase(options.PathBase);

    app.UseStaticFiles();
    app.UseRouting();

    app.UseCors(ConfigureBaGetOptions.CorsPolicy);
    app.UseOperationCancelledMiddleware();

    var baget = new BaGetEndpointBuilder();

    baget.MapEndpoints(app);

    await app.RunAsync(cancellationToken);
});

await appCli.ExecuteAsync(args);
