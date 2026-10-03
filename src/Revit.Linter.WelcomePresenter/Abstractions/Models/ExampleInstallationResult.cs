namespace Revit.Linter.WelcomePresenter.Abstractions.Models;

/// <summary>
/// Describes where one example configuration file was written.
/// </summary>
/// <param name="FileName">The configuration file name required by its diagnostic module.</param>
/// <param name="Path">The full path the example was written to.</param>
/// <param name="PlacedAside">
/// <see langword="true"/> when a non-empty user file already existed, so the example was written to the
/// examples subfolder instead of replacing it.
/// </param>
public sealed record InstalledExampleFile(string FileName, string Path, bool PlacedAside);

/// <summary>
/// Describes the outcome of installing the example configuration files.
/// </summary>
/// <param name="Files">The written files, in installation order.</param>
public sealed record ExampleInstallationResult(IReadOnlyList<InstalledExampleFile> Files);
