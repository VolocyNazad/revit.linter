using Autodesk.Revit.DB;
using Revit.Linter.ElementIgnoring.Abstractions.Models;

namespace Revit.Linter.ElementIgnoring.Abstractions.Services;

public interface IIgnoreElementProvider
{
    IgnoreElementFeedback Ignore(string code, Element element);
}