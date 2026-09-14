using Microsoft.Extensions.Logging;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.Modules;
using Revit.Linter.Sandbox.Host.Models;

namespace Revit.Linter.Sandbox.Host.Modules;

[DependsOn<BuildSandboxModule>]
public sealed class CreateManifestModule : Module<ManifestInfo>
{
    private readonly LaunchOptions _options;

    public CreateManifestModule(LaunchOptions options)
    {
        _options = options;
    }

    protected override async Task<ManifestInfo?> ExecuteAsync(
        IModuleContext context,
        CancellationToken cancellationToken)
    {
        string root = SandboxPaths.FindRoot();
        string assemblyPath = SandboxPaths.FindSandboxAssembly(root, _options.Configuration);
        string manifestPath = SandboxPaths.FindManifestPath(_options.MajorVersion);

        Directory.CreateDirectory(Path.GetDirectoryName(manifestPath)!);
        await File.WriteAllTextAsync(manifestPath, BuildManifest(assemblyPath), cancellationToken);

        context.Logger.LogInformation("Addin manifest created: {ManifestPath}", manifestPath);
        return new ManifestInfo(assemblyPath, manifestPath, _options.MajorVersion.ToString());
    }

    private static string BuildManifest(string assemblyPath) => $"""
        <?xml version="1.0" encoding="utf-8"?>
        <RevitAddIns>
          <AddIn Type="Command">
            <Name>Revit.Linter.Sandbox</Name>
            <Assembly>{assemblyPath}</Assembly>
            <AddInId>{Guid.NewGuid():D}</AddInId>
            <FullClassName>Revit.Linter.Sandbox.SandboxCommand</FullClassName>
            <VendorId>VolocyNazad</VendorId>
            <VendorDescription>VolocyNazad</VendorDescription>
          </AddIn>
        </RevitAddIns>
        """;
}