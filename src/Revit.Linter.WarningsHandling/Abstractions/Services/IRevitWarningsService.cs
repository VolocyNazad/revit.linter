using Autodesk.Revit.DB;
using Revit.Linter.WarningsHandling.Abstractions.Models;

namespace Revit.Linter.WarningsHandling.Abstractions.Services;

public interface IRevitWarningsService
{
    WarningsServiceResult Execute(Document document);
}
