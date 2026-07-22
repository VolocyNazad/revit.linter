using Autodesk.Revit.DB;

namespace Revit.Linter.ElementIgnoring.Abstractions.Services;

public interface IIgnoreElementDetector
{
    bool IsElementIgnored(string code, Element element);
}