using System.Text.RegularExpressions;
using System.Xml.Linq;
using ModularPipelines.Context;
using ModularPipelines.Modules;
using Shouldly;

namespace Revit.Linter.Build.Modules;

public sealed partial class ResolveConfigurationsModule : Module<RevitConfiguration[]>
{
    protected override Task<RevitConfiguration[]?> ExecuteAsync(
        IModuleContext context,
        CancellationToken cancellationToken)
    {
        var solution = XDocument.Load(BuildPaths.Solution);
        var configurations = solution
            .Descendants("BuildType")
            .Select(element => element.Attribute("Name")?.Value)
            .Where(name => name is not null)
            .Select(name => Parse(name!))
            .Where(configuration => configuration is not null)
            .Cast<RevitConfiguration>()
            .OrderBy(configuration => configuration.RevitVersion)
            .ToArray();

        configurations.ShouldNotBeEmpty("No Revit Release configurations were found in Revit.Linter.slnx");
        return Task.FromResult<RevitConfiguration[]?>(configurations);
    }

    private static RevitConfiguration? Parse(string configuration)
    {
        var match = ReleaseConfigurationRegex().Match(configuration);
        return match.Success
            ? new RevitConfiguration(configuration, int.Parse(match.Groups["version"].Value))
            : null;
    }

    [GeneratedRegex("^Release_(?<version>\\d{4})\\.", RegexOptions.CultureInvariant)]
    private static partial Regex ReleaseConfigurationRegex();
}

public sealed record RevitConfiguration(string Name, int RevitVersion);
