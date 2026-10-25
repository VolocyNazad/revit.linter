namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>Identifies one problem found in a diagnostic configuration file.</summary>
public enum DiagnosticConfigurationFileProblemCode
{
    /// <summary>The file exists but has no YAML document.</summary>
    FileEmpty,

    /// <summary>The file declares a format version newer than this product supports.</summary>
    VersionNewer,

    /// <summary>The file declares a format version this product does not support.</summary>
    VersionUnsupported,

    /// <summary>The file could not be read; the problem arguments contain the failure details.</summary>
    FileUnreadable,

    /// <summary>The rule list contains an empty entry that was ignored.</summary>
    EmptyRule,
}

/// <summary>Describes one configuration file problem without formatting it for the user.</summary>
/// <param name="Code">The stable problem code.</param>
/// <param name="FileName">The file name shown in the message.</param>
/// <param name="Arguments">The values referenced by the problem, in the order the message expects them.</param>
/// <remarks>
/// The code and arguments let the host format the same problem in the user's language. The configuration
/// service keeps a plain English description as well for logs and legacy callers.
/// </remarks>
public sealed record DiagnosticConfigurationFileProblem(
    DiagnosticConfigurationFileProblemCode Code,
    string FileName,
    IReadOnlyList<string> Arguments);
