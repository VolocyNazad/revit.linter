using Revit.Linter.ElementAccentor.Abstractions.Models;

namespace Revit.Linter.ElementAccentor.Abstractions.Services;

/// <summary>
/// Shortcuts for one-shot element operations shared by presenter modules.
/// </summary>
public static class AccentElementsServiceExtensions
{
    /// <summary>Selects one element in the document.</summary>
    /// <param name="services">The registered accent services.</param>
    /// <param name="document">The active Revit document.</param>
    /// <param name="elementId">The element to select.</param>
    /// <returns><see langword="true"/> when the element was selected.</returns>
    /// <remarks>Requires a registered <c>SelectElements</c> service; throws otherwise.</remarks>
    public static bool SelectElement(
        this IEnumerable<IAccentElementsService> services, Document document, ElementId elementId)
    {
        return services
            .First(service => service.Type == AccentElementsType.SelectElements)
            .RunOnce(document, elementId);
    }
}
