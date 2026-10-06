namespace Revit.Linter.Core.Abstractions;

/// <summary>
/// Single source of product-wide identity: vendor, product and repository names,
/// GitHub destinations, registry policy location and updater synchronization name.
/// </summary>
/// <remarks>
/// Canonical source compiled into every build unit (add-in, updater, installer) via source link,
/// so renaming the vendor, product or repository requires editing this file only.
/// Path composition stays with the owning module (<c>ConfigurationPathUtils</c> for user
/// configurations, <c>UpdaterPaths</c> for the updater); this type holds segments only.
/// </remarks>
public static class ProductIdentity
{
    /// <summary>Gets the per-user data folder segment under <c>LocalApplicationData</c>.</summary>
    public const string CompanyDirectoryName = "Volocy";

    /// <summary>Gets the product folder segment under the company folder.</summary>
    public const string ProductDirectoryName = "Revit.Linter";

    /// <summary>Gets the vendor used for the per-user installation directory and MSI manufacturer.</summary>
    public const string InstallVendor = "VolocyNazad";

    /// <summary>Gets the add-in and MSI product name.</summary>
    public const string AddInName = "Revit.Linter";

    /// <summary>Gets the standalone updater executable file name.</summary>
    public const string UpdaterExecutableName = "Revit.Linter.Updater.exe";

    /// <summary>Gets the process name of the updater without extension.</summary>
    public const string UpdaterProcessName = "Revit.Linter.Updater";

    /// <summary>Gets the MSI release asset file name prefix.</summary>
    public const string MsiAssetPrefix = "RevitLinter-";

    /// <summary>Gets the MSI release asset file extension.</summary>
    public const string MsiAssetExtension = ".msi";

    /// <summary>Gets the user documents folder segment for version-specific configurations.</summary>
    public const string DocumentsFolderName = "Revit Linter";

    /// <summary>Gets the ValueStore folder segment under the product data folder.</summary>
    public const string SettingsFolderName = "settings";

    /// <summary>Gets the log folder segment under the product data folder.</summary>
    public const string LogsFolderName = "logs";

    /// <summary>Gets the updater folder segment under the product data folder.</summary>
    public const string UpdaterFolderName = "updater";

    /// <summary>Gets the GitHub organization that owns the repository.</summary>
    public const string GitHubOrganization = "VolocyNazad";

    /// <summary>Gets the GitHub repository name.</summary>
    public const string GitHubRepository = "revit.linter";

    /// <summary>Gets the public GitHub host.</summary>
    public const string GitHubHost = "github.com";

    /// <summary>Gets the trusted suffix for GitHub release asset hosts.</summary>
    public const string GitHubAssetHostSuffix = ".githubusercontent.com";

    /// <summary>Gets the updater registry policy subkey below <c>HKLM</c> and <c>HKCU</c>.</summary>
    public const string PolicySubKey = @"Software\Policies\Volocy\Revit.Linter\Updater";

    /// <summary>Gets the updater single-instance synchronization base name.</summary>
    public const string UpdaterMutexName = "Volocy.Revit.Linter.Updater";

    /// <summary>Gets the manual-check event suffix appended to the updater synchronization name.</summary>
    public const string UpdaterManualCheckEventSuffix = ".CheckNow";

    /// <summary>Gets the tutorial session mutex prefix for temporary sample copies.</summary>
    public const string TutorialSessionMutexPrefix = "RevitLinterTutorial_";

    /// <summary>Gets the tutorial folder segment below the temporary product folder.</summary>
    public const string TutorialFolderName = "Tutorial";

    /// <summary>Gets the absolute HTTPS address of the GitHub repository.</summary>
    public static string RepositoryUrl => $"https://github.com/{GitHubOrganization}/{GitHubRepository}";

    /// <summary>Gets the absolute HTTPS address of the issue tracker.</summary>
    public static string SupportUrl => $"{RepositoryUrl}/issues";

    /// <summary>Gets the absolute HTTPS address of the documentation Wiki root.</summary>
    public static string DocumentationUrl => $"{RepositoryUrl}/wiki";

    /// <summary>Gets the absolute HTTPS address of the Wiki page root with trailing slash.</summary>
    public static string WikiRootUrl => $"{RepositoryUrl}/wiki/";

    /// <summary>Gets the absolute HTTPS address of the releases page.</summary>
    public static string ReleasesUrl => $"{RepositoryUrl}/releases";

    /// <summary>Gets the absolute HTTPS address of the author's sponsorship page.</summary>
    public static string SponsorUrl => $"https://github.com/sponsors/{GitHubOrganization}";

    /// <summary>Gets the default GitHub API endpoint for the latest stable release.</summary>
    public static string DefaultReleaseApiUrl =>
        $"https://api.github.com/repos/{GitHubOrganization}/{GitHubRepository}/releases/latest";

    /// <summary>Gets the trusted URL path prefix for release pages.</summary>
    public static string RepositoryReleasePath => $"/{GitHubOrganization}/{GitHubRepository}/releases/";

    /// <summary>Builds the expected MSI asset file name for a stable version.</summary>
    /// <param name="version">The stable version text, for example <c>2.0.0</c>.</param>
    /// <returns>The exact release asset name.</returns>
    public static string GetMsiAssetName(string version) => $"{MsiAssetPrefix}{version}{MsiAssetExtension}";

    /// <summary>Builds the expected absolute asset download path for a stable version.</summary>
    /// <param name="version">The stable version text, for example <c>2.0.0</c>.</param>
    /// <param name="assetName">The exact MSI asset file name.</param>
    /// <returns>The absolute URL path without host.</returns>
    public static string GetMsiAssetPath(string version, string assetName) =>
        $"/{GitHubOrganization}/{GitHubRepository}/releases/download/v{version}/{assetName}";

    /// <summary>Qualifies a synchronization name with the local Windows session namespace.</summary>
    /// <param name="name">The application-specific object name.</param>
    /// <returns>The session-qualified object name.</returns>
    public static string GetLocalMutexName(string name) => $@"Local\{name}";
}
