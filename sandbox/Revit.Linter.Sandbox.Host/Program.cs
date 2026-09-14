using Microsoft.Extensions.DependencyInjection;
using ModularPipelines;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.Extensions;
using Revit.Linter.Sandbox.Host;
using Revit.Linter.Sandbox.Host.Modules;

[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows")]

string configuration = SandboxPaths.ParseConfiguration(args);
LaunchOptions options = new(configuration, SandboxPaths.ParseMajorVersion(configuration));

PipelineBuilder builder = Pipeline.CreateBuilder();
builder.Services.RegisterDotNetContext();
builder.Services.AddSingleton(options);
builder.Services.AddModule<StopRevitModule>();
builder.Services.AddModule<BuildSandboxModule>();
builder.Services.AddModule<CreateManifestModule>();
builder.Services.AddModule<LaunchRevitModule>();
builder.Services.AddModule<DeleteManifestModule>();

await (await builder.BuildAsync()).RunAsync();