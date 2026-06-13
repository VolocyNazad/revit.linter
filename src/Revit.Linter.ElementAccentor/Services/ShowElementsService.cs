using Microsoft.Extensions.Logging;
using Revit.Linter.ElementAccentor.Abstractions.Models;
using Revit.Linter.ElementAccentor.Abstractions.Services;

namespace Revit.Linter.ElementAccentor.Services;

internal sealed class ShowElementsService(ILogger<SelectElementsService> logger) : IAccentElementsService
{
    public AccentElementsType Type => AccentElementsType.ShowElements;

    public bool Execute(Document document, params ElementId[] elementIds)
    {
        new UIDocument(document).ShowElements(elementIds);

        logger.LogInformation("Elements showed.");

        return true;
    }
}
