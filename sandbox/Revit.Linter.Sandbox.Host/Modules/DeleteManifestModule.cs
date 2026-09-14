using Microsoft.Extensions.Logging;
using ModularPipelines.Attributes;
using ModularPipelines.Configuration;
using ModularPipelines.Context;
using ModularPipelines.Modules;
using Revit.Linter.Sandbox.Host.Models;

namespace Revit.Linter.Sandbox.Host.Modules;

[DependsOn<CreateManifestModule>]
[DependsOn<LaunchRevitModule>]
public sealed class DeleteManifestModule : Module<object?>
{
    protected override ModuleConfiguration Configure() => ModuleConfiguration.Create()
        .WithAlwaysRun()
        .WithIgnoreFailures()
        .Build();

    protected override async Task<object?> ExecuteAsync(
        IModuleContext context,
        CancellationToken cancellationToken)
    {
        var manifest = (await context.GetModule<CreateManifestModule>()).ValueOrDefault;
        if (manifest is not null)
        {
            if (File.Exists(manifest.ManifestPath))
                File.Delete(manifest.ManifestPath);

            context.Logger.LogInformation("Addin manifest removed: {ManifestPath}", manifest.ManifestPath);
        }

        return null;
    }
}