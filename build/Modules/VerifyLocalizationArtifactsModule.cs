using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.Modules;
using ModularPipelines.Options;

namespace Revit.Linter.Build.Modules;

[DependsOn<CompileProjectModule>]
public sealed class VerifyLocalizationArtifactsModule : Module
{
    protected override async Task ExecuteModuleAsync(
        IModuleContext context,
        CancellationToken cancellationToken)
    {
        var targets = (await context.GetModule<CompileProjectModule>()).ValueOrDefault!;

        foreach (var target in targets)
        {
            string configuration = new DirectoryInfo(target.Directory).Parent!.Name;
            await context.SubModule(configuration, async () =>
                await context.Shell.Command.ExecuteCommandLineTool(
                    new GenericCommandLineToolOptions("dotnet")
                    {
                        Arguments =
                        [
                            "test",
                            BuildPaths.LocalizationArtifactTestsProject,
                            "--configuration", configuration,
                            "--nologo"
                        ]
                    },
                    cancellationToken: cancellationToken));
        }
    }
}
