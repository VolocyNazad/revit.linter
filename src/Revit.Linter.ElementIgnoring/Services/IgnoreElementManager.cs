using Autodesk.Revit.DB;
using Revit.Linter.ElementIgnoring.Abstractions.Models;
using Revit.Linter.ElementIgnoring.Abstractions.Services;

namespace Revit.Linter.ElementIgnoring.Services;

internal sealed class IgnoreElementManager : IIgnoreElementDetector, IIgnoreElementProvider
{
    private readonly static Guid _instanceParameterId = new("666a739a-ae5d-48d1-b146-fc0b2d7f5a4b");
    private readonly static Guid _typeParameterId = new("e1c4d22f-9147-49d5-b7cc-6f13b35e4d53");
    private readonly static char _separator = ';';

    public IgnoreElementFeedback Ignore(string code, Element element)
    {
        Parameter? parameter = element.get_Parameter(GetParameterId(element));
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

        if (ContainsCode(line, code))
            return IgnoreElementFeedback.Success();

        parameter.Set(AppendCode(line, code));

        return IgnoreElementFeedback.Success();
    }

    public bool IsElementIgnored(string code, Element element)
    {
        Parameter? parameter = element.get_Parameter(GetParameterId(element));
        if (parameter is null) return false;
        if (parameter.StorageType != StorageType.String) return false;

        string line = parameter.AsString();
        if (line is null) return false;

        bool isIgnore = ContainsCode(line, code);

        return isIgnore;
    }

    private static bool ContainsCode(string line, string code) =>
        line.Split(_separator).Contains(code);

    private static string AppendCode(string line, string code)
    {
        if (string.IsNullOrEmpty(line)) return code;
        return line[^1] == _separator ? line + code : line + _separator + code;
    }

    private static Guid GetParameterId(Element element) =>
        element is ElementType ? _typeParameterId : _instanceParameterId;
}
