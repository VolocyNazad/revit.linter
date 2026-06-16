using System.Diagnostics;
using WixToolset.Dtf.WindowsInstaller;

namespace Revit.Linter.Installer;

public static class CustomActions
{
    private const string RevitVersion = "2021";
    private const string AddinName = "Revit.Linter";
    private const string Vendor = "VolocyNazad";


    [CustomAction]
    public static ActionResult CreateManifest(Session session)
    {
        try {
            string filePath = GetManifestPath();
            ExternalApplicationDefinition definition = new()
            {
                Name = AddinName,
                FullClassName = $"{AddinName}.InitExternalApplication",
                Assembly = $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\{AddinName}\{RevitVersion}\sources\{AddinName}.dll",
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
            string filePath = GetManifestPath();
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

    private static string GetManifestPath()
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string directoryPath = Path.Combine(appDataPath, "Autodesk", "Revit", "Addins", RevitVersion);
        string filePath = Path.Combine(directoryPath, $"{AddinName}.addin");
        return filePath;
    }
}