using Microsoft.Extensions.Logging;
using Revit.Linter.ElementAccentor.Abstractions.Models;
using Revit.Linter.ElementAccentor.Abstractions.Services;

namespace Revit.Linter.ElementAccentor.Services;

internal sealed class SelectElementsService(ILogger<SelectElementsService> logger) : IAccentElementsService
{
    public AccentElementsType Type => AccentElementsType.SelectElements;

    public bool Execute(Document document, params ElementId[] elementIds)
    {
        new UIDocument(document).Selection.SetElementIds(elementIds);

        logger.LogInformation("Elements selected.");

        return true;
    }
}
