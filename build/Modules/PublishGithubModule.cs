using Microsoft.Extensions.Options;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.Modules;
using ModularPipelines.Options;
using Revit.Linter.Build.Options;
using Shouldly;

namespace Revit.Linter.Build.Modules;

[DependsOn<CreateInstallersModule>]
[DependsOn<ResolveVersioningModule>]
public sealed class PublishGithubModule(
    IOptions<BuildOptions> buildOptions,
    IOptions<PublishOptions> publishOptions) : Module
{
    protected override async Task ExecuteModuleAsync(
        IModuleContext context,
        CancellationToken cancellationToken)
    {
        var versioning = (await context.GetModule<ResolveVersioningModule>()).ValueOrDefault!;
        Version.TryParse(versioning.Version, out _)
            .ShouldBeTrue($"GitHub releases require a stable version, but GitVersion produced '{versioning.Version}'");

        string outputDirectory = Path.GetFullPath(buildOptions.Value.OutputDirectory, BuildPaths.Root);
        string[] installers = Directory.GetFiles(
            outputDirectory,
            $"RevitLinter-{versioning.Version}-rvt*.msi");
        installers.ShouldNotBeEmpty("No MSI installers were found to publish");

        var arguments = new List<string>
        {
            "release", "create", $"v{versioning.Version}",
            "--verify-tag",
            "--generate-notes",
            "--title", $"Revit.Linter {versioning.Version}"
        };

        if (publishOptions.Value.Draft)
            arguments.Add("--draft");

        arguments.AddRange(installers);
        await context.Shell.Command.ExecuteCommandLineTool(
            new GenericCommandLineToolOptions("gh") { Arguments = arguments },
            new CommandExecutionOptions { WorkingDirectory = BuildPaths.Root },
            cancellationToken: cancellationToken);
    }
}
