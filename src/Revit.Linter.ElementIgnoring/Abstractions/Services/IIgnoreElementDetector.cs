using Autodesk.Revit.DB;

namespace Revit.Linter.ElementIgnoring.Abstractions.Services;

public interface IIgnoreElementDetector
{
    bool IsIgnoreElement(string code, Element element);
}