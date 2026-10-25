namespace Revit.Linter.WelcomePresenter.Abstractions.Models;

/// <summary>Describes what happened to one example configuration file during installation.</summary>
public enum ExampleInstallationOutcome
{
    /// <summary>The example file was created.</summary>
    Installed,

    /// <summary>The example file already contained the shipped example, so it was not rewritten.</summary>
    Unchanged,

    /// <summary>The example file was replaced; the previous content is kept in <see cref="InstalledExampleFile.BackupPath"/>.</summary>
    Replaced,
}

/// <summary>
/// Describes where one example configuration file was written and what happened to an existing file.
/// </summary>
/// <param name="FileName">The configuration file name required by its diagnostic module.</param>
/// <param name="Path">The full path the example was written to, inside the examples subfolder.</param>
public sealed record InstalledExampleFile(string FileName, string Path)
{
    /// <summary>Gets what happened to the file.</summary>
    /// <remarks>
    /// The default is <see cref="ExampleInstallationOutcome.Installed"/>, which describes a fresh installation.
    /// A repeated installation of identical content reports <see cref="ExampleInstallationOutcome.Unchanged"/>
    /// so the user is not told that something was written when the file stayed byte-identical.
    /// </remarks>
    public ExampleInstallationOutcome Outcome { get; init; } = ExampleInstallationOutcome.Installed;

    /// <summary>Gets the path of the backup copy created before a replacement, when one was made.</summary>
    /// <value>The backup path for a replaced non-empty file; otherwise, <see langword="null"/>.</value>
    public string? BackupPath { get; init; }
}

/// <summary>
/// Describes the outcome of installing the example configuration files.
/// </summary>
/// <param name="Files">The written files, in installation order.</param>
public sealed record ExampleInstallationResult(IReadOnlyList<InstalledExampleFile> Files);
