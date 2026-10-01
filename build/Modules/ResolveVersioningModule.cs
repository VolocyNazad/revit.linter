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
        string version;

        if (string.IsNullOrWhiteSpace(options.Value.Version))
        {
            var gitVersion = await context.Git().Versioning.GetGitVersioningInformation();
            version = gitVersion.SemVer
                ?? throw new InvalidOperationException("GitVersion did not produce a semantic version.");
        }
        else
        {
            version = options.Value.Version;
        }

        string productVersion = GetProductVersion(version);
        var result = new ResolveVersioningResult
        {
            Version = version,
            ProductVersion = productVersion
        };

        context.Summary.KeyValue("Build", "Version", result.Version);
        context.Summary.KeyValue("Build", "MSI product version", result.ProductVersion);
        return result;
    }

    private static string GetProductVersion(string semanticVersion)
    {
        int suffixIndex = semanticVersion.IndexOfAny(['-', '+']);
        string value = suffixIndex < 0 ? semanticVersion : semanticVersion[..suffixIndex];
        if (!Version.TryParse(value, out Version? version) || version.Build < 0 || version.Revision >= 0)
        {
            throw new InvalidOperationException(
                $"Version '{semanticVersion}' cannot be converted to an MSI product version. " +
                "Expected semantic version major.minor.patch with an optional prerelease or build suffix.");
        }

        return $"{version.Major}.{version.Minor}.{version.Build}";
    }
}

public sealed record ResolveVersioningResult
{
    public required string Version { get; init; }

    public required string ProductVersion { get; init; }
}
