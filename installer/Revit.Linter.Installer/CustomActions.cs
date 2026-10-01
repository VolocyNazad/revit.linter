using WixToolset.Dtf.WindowsInstaller;

namespace Revit.Linter.Installer;

public static class CustomActions
{
    private const string AddInName = "Revit.Linter";
    private const string Vendor = "VolocyNazad";


    [CustomAction]
    public static ActionResult CreateManifests(Session session)
    {
        try
        {
            foreach (string revitVersion in GetRevitVersions(session))
            {
                string filePath = GetManifestPath(revitVersion);
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
            session.Log($"Failed to create manifests: {ex}");
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

    private static string GetManifestPath(string revitVersion)
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string directoryPath = Path.Combine(appDataPath, "Autodesk", "Revit", "Addins", revitVersion);
        string filePath = Path.Combine(directoryPath, $"{AddInName}.addin");
        return filePath;
    }
}
