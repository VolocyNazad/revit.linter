namespace Revit.Linter.ConfigurationPath.Abstractions.Models;

/// <summary>
/// Diagnostic configuration file envelope: the file format version plus the diagnostic rules.
/// </summary>
/// <typeparam name="TRule">The rule type of one diagnostic module.</typeparam>
/// <remarks>
/// Files written before the envelope carry a bare rule list and read as version 0.
/// Version 1 is the first enveloped format: a mapping with <c>version</c> and <c>rules</c>.
/// A newer version means the file was written for a newer product; readers refuse such files
/// so future compatibility checks have a reliable signal.
/// </remarks>
public sealed class ConfigurationFile<TRule>
{
    /// <summary>Gets the file format version; 0 for legacy files with a bare rule list.</summary>
    public int Version { get; init; }

    /// <summary>Gets the diagnostic rules in file order.</summary>
    public List<TRule> Rules { get; init; } = [];
}
