using Microsoft.Extensions.Logging;
using Revit.Linter.ConfigurationPath;
using Revit.Linter.ConfigurationPath.Abstractions.Services;
using Revit.Linter.DiagnosticConfiguration.Models;
using System.Globalization;
using System.IO;
using System.Text;

namespace Revit.Linter.PracticalTourPresenter.Infrastructure.Services;

/// <summary>
/// Installs and removes the managed practical-tour configuration for the running Revit version.
/// </summary>
/// <remarks>
/// The file is owned by Revit Linter and lives in the <c>tour</c> subfolder, so it never overwrites the
/// user's main configuration.
/// </remarks>
internal sealed class PracticalTourConfigurationInstaller(
    ILogger<PracticalTourConfigurationInstaller> logger,
    IConfigurationPaths paths)
{
    internal const string ExamplesFolderName = ConfigurationPathUtils.ExamplesFolderName;
    internal const string TourFolderName = ConfigurationPathUtils.TourFolderName;
    internal const string PracticalTourFileName = ConfigurationPathUtils.PracticalTourFileName;

    private const string TourDiagnosticCode = "TOUR001";

    private const string ResourcePrefix = "Revit.Linter.PracticalTourPresenter.Examples.";

    /// <summary>Installs the managed practical-tour configuration and removes a superseded copy.</summary>
    /// <returns>The full path of the installed configuration file.</returns>
    public string Install()
    {
        string language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ru" ? "ru" : "en";
        string stalePath = Path.Combine(paths.Directory, ExamplesFolderName, PracticalTourFileName);
        bool hadStaleConfiguration = File.Exists(stalePath);
        string path = Install(paths.Directory, fileName => ReadTemplate(language, fileName));
        if (hadStaleConfiguration)
            logger.LogInformation("Removed the superseded practical-tour configuration at {ConfigurationPath}", stalePath);
        logger.LogInformation("Installed practical-tour diagnostic configuration into {ConfigurationPath}", path);
        return path;
    }

    /// <summary>Removes the managed practical-tour configuration when it is installed.</summary>
    /// <remarks>Called when the tour completes or is dismissed, so no training configuration is left behind.</remarks>
    public void Remove()
    {
        if (Remove(paths.Directory))
            logger.LogInformation("Removed the practical-tour diagnostic configuration.");
    }

    /// <summary>Replaces the managed practical-tour configuration when it is missing from the current format.</summary>
    /// <returns><see langword="true"/> when an outdated file was replaced.</returns>
    /// <remarks>
    /// The file belongs to the product, so a copy written by a previous product version is refreshed from the
    /// embedded template. A file that already declares the current format and the training diagnostic is left
    /// untouched, because the tour writes the user's selection into it.
    /// </remarks>
    public bool RefreshOutdated()
    {
        string path = Path.Combine(paths.Directory, TourFolderName, PracticalTourFileName);
        if (!File.Exists(path)) return false;

        bool isCurrent = ConfigurationPathUtils.TryGetDiagnosticConfiguration(
                             path, out DiagnosticConfigurationFile? file, out _)
                         && file is not null
                         && file.Version == ConfigurationPathUtils.SupportedConfigurationVersion
                         && file.Rules?.Any(rule => string.Equals(rule.Code, TourDiagnosticCode, StringComparison.Ordinal)) == true;
        if (isCurrent) return false;

        Install();
        logger.LogInformation(
            "Refreshed the outdated managed practical-tour configuration at {ConfigurationPath}", path);
        return true;
    }

    /// <summary>Removes the practical-tour configuration from an explicit directory.</summary>
    /// <returns><see langword="true"/> when a file was removed.</returns>
    /// <remarks>Separated from the configuration folder lookup so it can be exercised headlessly.</remarks>
    internal static bool Remove(string targetDirectory)
    {
        string path = Path.Combine(targetDirectory, TourFolderName, PracticalTourFileName);
        if (!File.Exists(path)) return false;
        File.Delete(path);
        return true;
    }

    /// <summary>Installs the practical-tour configuration into an explicit directory from an explicit template source.</summary>
    /// <remarks>Separated from the Revit-version and culture lookup so it can be exercised headlessly.</remarks>
    internal static string Install(string targetDirectory, Func<string, string> readTemplate)
    {
        string tourDirectory = Path.Combine(targetDirectory, TourFolderName);
        Directory.CreateDirectory(tourDirectory);
        string path = Path.Combine(tourDirectory, PracticalTourFileName);
        UTF8Encoding encoding = new(encoderShouldEmitUTF8Identifier: false);
        File.WriteAllText(path, readTemplate(PracticalTourFileName), encoding);
        string stalePath = Path.Combine(targetDirectory, ExamplesFolderName, PracticalTourFileName);
        if (File.Exists(stalePath)) File.Delete(stalePath);
        return path;
    }

    private static string ReadTemplate(string language, string fileName)
    {
        string resourceName = $"{ResourcePrefix}{language}.{fileName}";
        using Stream stream = typeof(PracticalTourConfigurationInstaller).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"The example configuration '{resourceName}' is not embedded.");
        using StreamReader reader = new(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
