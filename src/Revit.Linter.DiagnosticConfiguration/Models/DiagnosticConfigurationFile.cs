using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.DiagnosticConfiguration.Models;

/// <summary>
/// Diagnostic configuration file envelope: the file format version plus the diagnostic rules.
/// </summary>
/// <remarks>
/// Version 2 is the single-file format with a per-rule <c>type</c> discriminator. Files written before
/// that format are not read; they are left untouched and the product shows a migration notice.
/// A newer version means the file was written for a newer product; readers refuse such files so future
/// compatibility checks have a reliable signal.
/// </remarks>
public sealed class DiagnosticConfigurationFile
{
    /// <summary>Gets the file format version.</summary>
    public int Version { get; init; }

    /// <summary>Gets the configuration entries in file order.</summary>
    public List<DiagnosticRule> Rules { get; init; } = [];
}
