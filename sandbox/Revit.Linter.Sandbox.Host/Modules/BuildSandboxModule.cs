using Microsoft.Extensions.Logging;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace Revit.Linter.Sandbox.Host.Modules;

[DependsOn<StopRevitModule>]
public sealed class BuildSandboxModule(LaunchOptions options) : Module<CommandResult>
{
    private readonly LaunchOptions _options = options;

    protected override async Task<CommandResult?> ExecuteAsync(
        IModuleContext context,
        CancellationToken cancellationToken)
    {
        string root = SandboxPaths.FindRoot();
        string project = Path.Combine(root, "Revit.Linter.Sandbox", "Revit.Linter.Sandbox.csproj");

        context.Logger.LogInformation("Building sandbox: {Project}", project);

        var result = await context.DotNet().Build(new DotNetBuildOptions
        {
            ProjectSolution = project,
            Configuration = _options.Configuration,
            Nologo = true,
            Properties = [new KeyValue("Platform", "x64")],
        }, cancellationToken: cancellationToken);

        return result;
    }
}