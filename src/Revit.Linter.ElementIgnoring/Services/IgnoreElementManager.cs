using Autodesk.Revit.DB;
using Revit.Linter.ElementIgnoring.Abstractions.Models;
using Revit.Linter.ElementIgnoring.Abstractions.Services;

namespace Revit.Linter.ElementIgnoring.Services;

internal sealed class IgnoreElementManager : IIgnoreElementDetector, IIgnoreElementProvider
{
    private readonly static Guid _parameterId = new("666a739a-ae5d-48d1-b146-fc0b2d7f5a4b");
    private readonly static char _separator = ';';

    public IgnoreElementFeedback Ignore(string code, Element element)
    {
        Parameter? parameter = element.get_Parameter(_parameterId);
        if (parameter is null) 
            return IgnoreElementFeedback.Failed("Parameter not found.");
        if (parameter.StorageType != StorageType.String) 
            return IgnoreElementFeedback.Failed("Parameter storage type not string.");
        if (parameter.IsReadOnly) 
            return IgnoreElementFeedback.Failed("Parameter is readonly.");

        string line = parameter.AsString();
        if (line is null || line == string.Empty) {
            parameter.Set(code);
            return IgnoreElementFeedback.Success();
        }

        if (line[-1] != _separator) line += _separator;

        line += code;
        parameter.Set(line);

        return IgnoreElementFeedback.Success();
    }

    public bool IsIgnoreElement(string code, Element element)
    {
        Parameter? parameter = element.get_Parameter(_parameterId);
        if (parameter is null) return false;
        if (parameter.StorageType != StorageType.String) return false;

        string line = parameter.AsString();
        if (line is null) return false;

        bool isIgnore = line.Split(_separator).Any(i => i == code);

        return isIgnore;
    }
}
