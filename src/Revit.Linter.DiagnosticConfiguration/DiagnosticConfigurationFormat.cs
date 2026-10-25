namespace Revit.Linter.DiagnosticConfiguration;

/// <summary>
/// Defines the diagnostic configuration file format shared by the add-in and the diagnostics editor.
/// </summary>
/// <remarks>
/// Version 2 is the single-file format with a per-rule <c>type</c> discriminator. A file without the
/// version entry is not read; a file with a newer version than the running product is refused when it
/// is loaded, so compatibility checks have a reliable signal. The header is written into a newly
/// created configuration file so the format is explained even before a rule exists.
/// </remarks>
public static class DiagnosticConfigurationFormat
{
    /// <summary>Gets the configuration file name used by the current format.</summary>
    public const string FileName = "diagnostics.config.yaml";

    /// <summary>Gets the configuration file format version this product reads.</summary>
    public const int SupportedVersion = 2;

    private static readonly string[] HeaderLines =
    [
        "# Revit Linter diagnostics configuration (format version 2).",
        "# This single file holds every configured check. The \"type\" field selects the module:",
        "#   custom    - a user-defined element check;",
        "#   collision - a check that compares elements with each other;",
        "#   parameter - a project-parameter check;",
        "#   builtIn   - an override of a check that ships with the product. Such an entry may",
        "#               contain only code, isActive, severity and isHidden.",
        "#",
        "# Settings changed in the Diagnostics pane are written back to this file; comments and",
        "# the order of the rules are preserved. A rule with isHidden: true is not shown in the",
        "# interface, does not run and does not appear in reports.",
        "#",
        "# File format version for compatibility checks.",
        string.Empty,
        "version: 2",
        "rules: []",
    ];

    /// <summary>Gets the header written into a newly created configuration file.</summary>
    public static readonly string Header = string.Join(Environment.NewLine, HeaderLines) + Environment.NewLine;
}
