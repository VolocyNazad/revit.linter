using System.Diagnostics;
using WixToolset.Dtf.WindowsInstaller;

namespace Revit.Linter.Installer;

public static class CustomActions
{
    private const string AddInName = "Revit.Linter";
    private const string Vendor = "VolocyNazad";


    [CustomAction]
    public static ActionResult PrepareForFileChanges(Session session)
    {
        try
        {
            StopUpdater(session);
            Process[] revitProcesses = Process.GetProcessesByName("Revit");
            try
            {
                if (revitProcesses.Length == 0)
                    return ActionResult.Success;

                string processIds = string.Join(", ", revitProcesses.Select(process => process.Id));
                session.Log($"Revit file replacement refused because Revit processes are running: {processIds}");
                using var message = new Record(0)
                {
                    FormatString = "Close all Autodesk Revit windows, then run Revit Linter setup again."
                };
                session.Message(InstallMessage.Error, message);
                return ActionResult.Failure;
            }
            finally
            {
                foreach (Process process in revitProcesses)
                    process.Dispose();
            }
        }
        catch (Exception ex)
        {
            session.Log($"Failed to prepare Revit Linter file changes: {ex}");
            return ActionResult.Failure;
        }
    }

    [CustomAction]
    public static ActionResult SynchronizeManifests(Session session)
    {
        try
        {
            foreach (string revitVersion in GetRevitVersions(session))
            {
                string filePath = GetManifestPath(revitVersion);
                if (!IsFeatureSelected(session, revitVersion))
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        session.Log($"Manifest removed for unselected Revit {revitVersion}: {filePath}");
                    }

                    continue;
                }

                string installDirectory = session["INSTALLDIR"];
                if (string.IsNullOrWhiteSpace(installDirectory))
                    throw new InvalidOperationException("MSI property INSTALLDIR is not set.");

                ExternalApplicationDefinition definition = new()
                {
                    Name = AddInName,
                    FullClassName = $"{AddInName}.InitExternalApplication",
                    Assembly = Path.Combine(
                        installDirectory,
                        "Addins", revitVersion, "sources", $"{AddInName}.dll"),
                    VendorId = Vendor,
                    VendorDescription = Vendor,
                };
                MultiAddInManifestGenerator.CreateManifests(filePath, definition);
                session.Log($"Manifest created successfully at: {filePath}");
            }

            return ActionResult.Success;
        }
        catch (Exception ex)
        {
            session.Log($"Failed to synchronize manifests: {ex}");
            return ActionResult.Failure;
        }
    }

    [CustomAction]
    public static ActionResult RemoveManifests(Session session)
    {
        try
        {
            foreach (string revitVersion in GetRevitVersions(session))
            {
                string filePath = GetManifestPath(revitVersion);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    session.Log($"Manifest removed successfully from: {filePath}");
                }
                else
                {
                    session.Log($"Manifest not found at: {filePath}");
                }
            }

            return ActionResult.Success;
        }
        catch (Exception ex)
        {
            session.Log($"Failed to remove manifests: {ex}");
            return ActionResult.Failure;
        }
    }

    private static string[] GetRevitVersions(Session session)
    {
        string value = session["REVIT_VERSIONS"];
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("MSI property REVIT_VERSIONS is not set.");

        return value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static bool IsFeatureSelected(Session session, string revitVersion)
    {
        FeatureInfo feature = session.Features[$"Revit{revitVersion}"];
        return feature.RequestState is InstallState.Local or InstallState.Source ||
               feature.RequestState == InstallState.Unknown &&
               feature.CurrentState is InstallState.Local or InstallState.Source;
    }

    private static void StopUpdater(Session session)
    {
        foreach (Process process in Process.GetProcessesByName("Revit.Linter.Updater"))
        {
            using (process)
            {
                session.Log($"Stopping updater process {process.Id} before changing files");
                process.Kill();
                if (!process.WaitForExit(10_000))
                    throw new InvalidOperationException($"Updater process {process.Id} did not stop in time.");
            }
        }
    }

    private static string GetManifestPath(string revitVersion)
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string directoryPath = Path.Combine(appDataPath, "Autodesk", "Revit", "Addins", revitVersion);
        string filePath = Path.Combine(directoryPath, $"{AddInName}.addin");
        return filePath;
    }
}
