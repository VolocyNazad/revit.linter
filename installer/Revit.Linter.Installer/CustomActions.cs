using WixToolset.Dtf.WindowsInstaller;

namespace Revit.Linter.Installer;

public static class CustomActions
{
    private const string AddInName = "Revit.Linter";
    private const string Vendor = "VolocyNazad";


    [CustomAction]
    public static ActionResult CreateManifest(Session session)
    {
        try {
            string revitVersion = GetRevitVersion(session);
            string filePath = GetManifestPath(revitVersion);
            ExternalApplicationDefinition definition = new()
            {
                Name = AddInName,
                FullClassName = $"{AddInName}.InitExternalApplication",
                Assembly = $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\{AddInName}\{revitVersion}\sources\{AddInName}.dll",
                VendorId = Vendor,
                VendorDescription = Vendor,
            };
            MultiAddInManifestGenerator.CreateManifests(filePath, definition);
            return ActionResult.Success;
        } catch (Exception ex) {
            session.Log($"Failed to create manifest: {ex.Message}");
            return ActionResult.Failure;
        }
    }

    [CustomAction]
    public static ActionResult RemoveManifest(Session session)
    {
        try
        {
            string filePath = GetManifestPath(GetRevitVersion(session));
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                session.Log($"Manifest removed successfully from: {filePath}");
            }
            else
                session.Log($"Manifest not found at: {filePath}");

            return ActionResult.Success;
        }
        catch (Exception ex)
        {
            session.Log($"Failed to remove manifest: {ex.Message}");
            return ActionResult.Failure;
        }
    }

    private static string GetRevitVersion(Session session)
    {
        string revitVersion = session["REVIT_VERSION"];
        if (string.IsNullOrWhiteSpace(revitVersion))
            throw new InvalidOperationException("MSI property REVIT_VERSION is not set.");

        return revitVersion;
    }

    private static string GetManifestPath(string revitVersion)
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string directoryPath = Path.Combine(appDataPath, "Autodesk", "Revit", "Addins", revitVersion);
        string filePath = Path.Combine(directoryPath, $"{AddInName}.addin");
        return filePath;
    }
}
