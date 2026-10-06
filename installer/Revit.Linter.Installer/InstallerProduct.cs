using Revit.Linter.Core.Abstractions;

namespace Revit.Linter.Installer;

/// <summary>Single owner of installer product identity shared by Program and custom actions.</summary>
internal static class InstallerProduct
{
    /// <summary>Gets the add-in and MSI product name.</summary>
    internal const string AddInName = ProductIdentity.AddInName;

    /// <summary>Gets the MSI manufacturer and per-user installation vendor.</summary>
    internal const string Vendor = ProductIdentity.InstallVendor;

    /// <summary>Gets the updater executable file name.</summary>
    internal const string UpdaterExecutableName = ProductIdentity.UpdaterExecutableName;

    /// <summary>Gets the updater process name without extension.</summary>
    internal const string UpdaterProcessName = ProductIdentity.UpdaterProcessName;

    /// <summary>Gets the stable MSI upgrade code.</summary>
    internal const string UpgradeCode = "ed6109bc-3ea6-4fe3-a1ab-e31a7db46ac1";

    /// <summary>Gets the payload folder segment for per-Revit-version add-in builds.</summary>
    internal const string AddinsFolderName = "Addins";

    /// <summary>Gets the payload folder segment for add-in binaries below a version folder.</summary>
    internal const string SourcesFolderName = "sources";

    /// <summary>Gets the MSI property that carries the installation directory.</summary>
    internal const string InstallDirectoryProperty = "INSTALLDIR";

    /// <summary>Gets the MSI property that carries the semicolon-separated Revit versions.</summary>
    internal const string RevitVersionsProperty = "REVIT_VERSIONS";

    /// <summary>Gets the MSI release asset file name prefix.</summary>
    internal const string MsiAssetPrefix = ProductIdentity.MsiAssetPrefix;

    /// <summary>Gets the per-user installation directory MSI property value.</summary>
    internal static string InstallDirectory => $@"%LocalAppDataFolder%\Programs\{Vendor}\{AddInName}";

    /// <summary>Gets the documentation help link.</summary>
    internal static string HelpLink => ProductIdentity.RepositoryUrl;

    /// <summary>Gets the per-user Revit add-in manifest path for a Revit version.</summary>
    /// <param name="revitVersion">The Revit release year, for example <c>2025</c>.</param>
    /// <returns>The absolute manifest file path.</returns>
    internal static string GetManifestPath(string revitVersion)
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string directoryPath = Path.Combine(appDataPath, "Autodesk", "Revit", "Addins", revitVersion);
        return Path.Combine(directoryPath, $"{AddInName}.addin");
    }

    /// <summary>Gets the MSI asset file name for a product version.</summary>
    /// <param name="version">The product version.</param>
    /// <returns>The exact MSI file name.</returns>
    internal static string GetMsiFileName(Version version) =>
        $"{ProductIdentity.MsiAssetPrefix}{version.Major}.{version.Minor}.{version.Build}{ProductIdentity.MsiAssetExtension}";

    /// <summary>Gets the MSI output file name without extension for a product version.</summary>
    /// <param name="version">The product version.</param>
    /// <returns>The output file name consumed by WixSharp.</returns>
    internal static string GetMsiOutFileName(Version version) =>
        $"{ProductIdentity.MsiAssetPrefix}{version.Major}.{version.Minor}.{version.Build}";
}
