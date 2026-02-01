using System;
using BaGet.Core;
using BaGet.Web;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

// ----------------------
// Top-level statements
// ----------------------

var builder = WebApplication.CreateBuilder(args);

var root = Environment.GetEnvironmentVariable("BAGET_CONFIG_ROOT");
if (!string.IsNullOrEmpty(root))
{
    builder.Configuration.SetBasePath(root);
}

builder.WebHost.ConfigureKestrel(options =>
{
    // Remove upload limit
    options.Limits.MaxRequestBodySize = null;

    if (builder.Environment.IsProduction())
    {
        options.ListenAnyIP(80);
    }
});

var host = builder.Build();

if (!host.ValidateStartupOptions())
{
    return;
}

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
    await host.RunMigrationsAsync(cancellationToken);

    await host.RunAsync(cancellationToken);
});

await appCli.ExecuteAsync(args);
