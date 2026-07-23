using Revit.Linter.Build.Modules;
using Revit.Linter.Build.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModularPipelines;
using ModularPipelines.Extensions;

Environment.SetEnvironmentVariable("DOTNET_CLI_UI_LANGUAGE", "en");

var builder = Pipeline.CreateBuilder();

builder.Configuration.AddJsonFile("appsettings.json");
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddCommandLine(args);

builder.Services
    .AddOptions<BuildOptions>()
    .Bind(builder.Configuration.GetSection("Build"));
builder.Services
    .AddOptions<PublishOptions>()
    .Bind(builder.Configuration.GetSection("Publish"));

if (args.Contains("publish", StringComparer.OrdinalIgnoreCase))
    builder.Services.AddModule<PublishGithubModule>();
else if (args.Contains("version", StringComparer.OrdinalIgnoreCase))
    builder.Services.AddModule<ResolveVersioningModule>();
else if (args.Contains("pack", StringComparer.OrdinalIgnoreCase))
    builder.Services.AddModule<CreateInstallersModule>();
else
    builder.Services.AddModule<CompileProjectModule>();

await (await builder.BuildAsync()).RunAsync();
