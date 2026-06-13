namespace Revit.Linter.Installer;

internal sealed class ExternalApplicationDefinition
{
    public string Type { get; set; } = "Application";
    public required string Name { get; init; }
    public required string Assembly { get; init; }
    public required string FullClassName { get; init; }
    public string AddInId { get; init; } = Guid.NewGuid().ToString();
    public required string VendorId { get; init; }
    public required string VendorDescription { get; init; }
}