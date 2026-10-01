using Microsoft.Extensions.Options;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.Modules;
using ModularPipelines.Options;
using Revit.Linter.Build.Options;
using Shouldly;

namespace Revit.Linter.Build.Modules;

/// <summary>Publishes the shared updater with its own .NET runtime for the per-user installer.</summary>
[DependsOn<ResolveVersioningModule>]
public sealed class PublishUpdaterModule(IOptions<BuildOptions> options) : Module<string>
{
    protected override async Task<string?> ExecuteAsync(
        IModuleContext context,
        CancellationToken cancellationToken)
    {
        var versioning = (await context.GetModule<ResolveVersioningModule>()).ValueOrDefault!;
        string outputDirectory = Path.GetFullPath(options.Value.OutputDirectory, BuildPaths.Root);
        string publishDirectory = BuildPaths.GetUpdaterPublishDirectory(outputDirectory);
        Directory.CreateDirectory(publishDirectory);

        await context.Shell.Command.ExecuteCommandLineTool(
            new GenericCommandLineToolOptions("dotnet")
            {
                Arguments =
                [
                    "publish",
                    BuildPaths.UpdaterProject,
                    "--configuration", "Release_2025.0.0",
                    "--runtime", "win-x64",
                    "--self-contained", "true",
                    "--output", publishDirectory,
                    "--nologo",
                    "--consoleLoggerParameters:ErrorsOnly;Summary",
                    "-p:Platform=x64",
                    $"-p:Version={versioning.Version}"
                ]
            }, cancellationToken: cancellationToken);

        File.Exists(Path.Combine(publishDirectory, "Revit.Linter.Updater.exe"))
            .ShouldBeTrue($"Published updater was not found: {publishDirectory}");
        return publishDirectory;
    }
}
