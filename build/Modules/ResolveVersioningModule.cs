using Microsoft.Extensions.Options;
using ModularPipelines.Context;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Modules;
using Revit.Linter.Build.Options;

namespace Revit.Linter.Build.Modules;

public sealed class ResolveVersioningModule(IOptions<PublishOptions> options)
    : Module<ResolveVersioningResult>
{
    protected override async Task<ResolveVersioningResult?> ExecuteAsync(
        IModuleContext context,
        CancellationToken cancellationToken)
    {
        ResolveVersioningResult result;

        if (string.IsNullOrWhiteSpace(options.Value.Version))
        {
            var gitVersion = await context.Git().Versioning.GetGitVersioningInformation();
            result = new ResolveVersioningResult
            {
                Version = gitVersion.SemVer
                    ?? throw new InvalidOperationException("GitVersion did not produce a semantic version.")
            };
        }
        else
        {
            result = new ResolveVersioningResult { Version = options.Value.Version };
        }

        context.Summary.KeyValue("Build", "Version", result.Version);
        return result;
    }
}

public sealed record ResolveVersioningResult
{
    public required string Version { get; init; }
}
