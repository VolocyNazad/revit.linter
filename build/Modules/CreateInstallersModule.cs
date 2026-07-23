using Revit.Linter.Build.Options;
using Microsoft.Extensions.Options;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Modules;
using ModularPipelines.Options;
using Shouldly;

namespace Revit.Linter.Build.Modules;

[DependsOn<CompileProjectModule>]
[DependsOn<ResolveVersioningModule>]
public sealed class CreateInstallersModule(IOptions<BuildOptions> options) : Module
{
    protected override async Task ExecuteModuleAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var versioning = (await context.GetModule<ResolveVersioningModule>()).ValueOrDefault!;

        await context.DotNet().Build(new DotNetBuildOptions
        {
            ProjectSolution = BuildPaths.InstallerProject,
            Configuration = "Release"
        }, cancellationToken: cancellationToken);

        string installer = BuildPaths.GetInstallerExecutable();
        File.Exists(installer).ShouldBeTrue($"Installer generator was not found: {installer}");

        string outputDirectory = Path.GetFullPath(options.Value.OutputDirectory, BuildPaths.Root);
        Directory.CreateDirectory(outputDirectory);

        var targets = (await context.GetModule<CompileProjectModule>()).ValueOrDefault!;

        foreach (var target in targets)
        {
            Directory.Exists(target.Directory).ShouldBeTrue($"Build output was not found: {target.Directory}");

            await context.Shell.Command.ExecuteCommandLineTool(
                new GenericCommandLineToolOptions(installer)
                {
                    Arguments =
                    [
                        target.RevitVersion.ToString(),
                        versioning.Version,
                        target.Directory,
                        outputDirectory
                    ]
                }, cancellationToken: cancellationToken);

            string msiPath = Path.Combine(
                outputDirectory,
                $"RevitLinter-{versioning.Version}-rvt{target.RevitVersion}.msi");
            File.Exists(msiPath).ShouldBeTrue($"MSI was not created: {msiPath}");
        }
    }
}
