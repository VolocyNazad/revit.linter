using Revit.Linter.Core.Abstractions;

namespace Revit.Linter.Updater.Core.Services;

/// <summary>Composes the per-user updater directories from product identity segments.</summary>
/// <remarks>
/// Single owner of <c>%LOCALAPPDATA%\Volocy\Revit.Linter\updater\</c> layout
/// (<c>logs/</c>, <c>state.json</c>, <c>downloads/</c>). Accepts the local-application-data
/// root explicitly so tests can substitute a temporary folder.
/// </remarks>
public static class UpdaterPaths
{
    /// <summary>Gets the updater home directory below the product data folder.</summary>
    /// <param name="localApplicationData">The <c>LocalApplicationData</c> folder path.</param>
    /// <returns>The updater home directory path.</returns>
    public static string GetUpdaterDirectory(string localApplicationData) => Path.Combine(
        localApplicationData,
        ProductIdentity.CompanyDirectoryName,
        ProductIdentity.ProductDirectoryName,
        ProductIdentity.UpdaterFolderName);

    /// <summary>Gets the updater home directory for the current user.</summary>
    /// <returns>The updater home directory path.</returns>
    public static string GetUpdaterDirectory() => GetUpdaterDirectory(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

    /// <summary>Gets the updater log file path template (rolling daily).</summary>
    /// <param name="updaterDirectory">The updater home directory path.</param>
    /// <returns>The Serilog rolling file path.</returns>
    public static string GetLogPath(string updaterDirectory) => Path.Combine(
        updaterDirectory, ProductIdentity.LogsFolderName, "updater-.log");

    /// <summary>Gets the per-user JSON state file path.</summary>
    /// <param name="updaterDirectory">The updater home directory path.</param>
    /// <returns>The state file path.</returns>
    public static string GetStatePath(string updaterDirectory) => Path.Combine(updaterDirectory, "state.json");

    /// <summary>Gets the verified installer download directory.</summary>
    /// <param name="updaterDirectory">The updater home directory path.</param>
    /// <returns>The downloads directory path.</returns>
    public static string GetDownloadDirectory(string updaterDirectory) =>
        Path.Combine(updaterDirectory, "downloads");
}
