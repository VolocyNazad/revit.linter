using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.Modules;
using ModularPipelines.Options;

namespace Revit.Linter.Build.Modules;

[DependsOn<ResolveVersioningModule>]
[DependsOn<ResolveConfigurationsModule>]
public sealed class CompileProjectModule : Module<CompiledRevitTarget[]>
{
    protected override async Task<CompiledRevitTarget[]?> ExecuteAsync(
        IModuleContext context,
        CancellationToken cancellationToken)
    {
        var versioning = (await context.GetModule<ResolveVersioningModule>()).ValueOrDefault!;
        var configurations = (await context.GetModule<ResolveConfigurationsModule>()).ValueOrDefault!;
        var targets = new List<CompiledRevitTarget>(configurations.Length);

        foreach (var configuration in configurations)
        {
            // Every project keeps one project.assets.json regardless of the configuration, so a previous
            // restore for another Revit version can leave the wrong target framework behind. Restore for the
            // configuration being compiled first, then build without the implicit restore.
            await context.SubModule($"{configuration.Name} restore", async () =>
                await context.Shell.Command.ExecuteCommandLineTool(
                    new GenericCommandLineToolOptions("dotnet")
                    {
                        Arguments =
                        [
                            "restore",
                            BuildPaths.AddInProject,
                            "--nologo",
                            $"-p:Configuration={configuration.Name}",
                            "-p:Platform=x64"
                        ]
                    },
                    cancellationToken: cancellationToken));

            await context.SubModule(configuration.Name, async () =>
                await context.Shell.Command.ExecuteCommandLineTool(
                    new GenericCommandLineToolOptions("dotnet")
                    {
                        Arguments =
                        [
                            "build",
                            BuildPaths.AddInProject,
                            "--configuration", configuration.Name,
                            "--no-restore",
                            "--nologo",
                            "--consoleLoggerParameters:ErrorsOnly;Summary",
                            "-p:Platform=x64",
                            "-p:TreatWarningsAsErrors=true",
                            $"-p:Version={versioning.Version}"
                        ]
                    },
                    cancellationToken: cancellationToken));

            targets.Add(new CompiledRevitTarget(
                configuration.RevitVersion,
                BuildPaths.GetAddInOutput(configuration.Name)));
        }

        return targets.ToArray();
    }
}

public sealed record CompiledRevitTarget(int RevitVersion, string Directory);
