using System.Xml.Linq;

namespace Revit.Linter.Installer;

internal sealed partial class MultiAddInManifestGenerator
{
    public static void CreateManifests(string toFilePath, params ExternalApplicationDefinition[] addIns)
    {
        XElement revitAddInsElement = new("RevitAddIns");

        foreach (ExternalApplicationDefinition addIn in addIns)
        {
            XElement addInElement = new("AddIn",
                new XAttribute("Type", addIn.Type),
                new XElement("Name", addIn.Name),
                new XElement("Assembly", addIn.Assembly),
                new XElement("FullClassName", addIn.FullClassName),
                new XElement("AddInId", addIn.AddInId),
                new XElement("VendorId", addIn.VendorId),
                new XElement("VendorDescription", addIn.VendorDescription)
            );

            revitAddInsElement.Add(addInElement);
        }

        XDocument doc = new(
            new("1.0", "utf-8", null),
            revitAddInsElement
        );

        using StreamWriter writer = new(toFilePath);
        doc.Save(writer);
    }
}
