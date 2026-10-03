using Autodesk.Revit.DB;
using Revit.Linter.DocumentQueries.Abstractions.Models;
using Revit.Linter.DocumentQueries.Abstractions.Services;
using Revit.Linter.ElementIgnoring.Abstractions.Models;
using Revit.Linter.ElementIgnoring.Abstractions.Services;

namespace Revit.Linter.ElementIgnoring.Services;

internal sealed class IgnoreElementManager(IDocumentQueryService documentQueries)
    : IIgnoreElementDetector, IIgnoreElementProvider
{
    private const string IgnoreStateQuery = "element-ignoring:ignore-state";

    private readonly static Guid _instanceParameterId = new("666a739a-ae5d-48d1-b146-fc0b2d7f5a4b");
    private readonly static Guid _typeParameterId = new("e1c4d22f-9147-49d5-b7cc-6f13b35e4d53");
    private readonly static char _separator = ';';

    public IgnoreElementFeedback Ignore(string code, Element element)
    {
        Parameter? parameter = element.get_Parameter(GetParameterId(element));
        if (parameter is null) 
            return IgnoreElementFeedback.Failed(ElementIgnoringLocalizations.GetString("parameterNotFound_message"));
        if (parameter.StorageType != StorageType.String) 
            return IgnoreElementFeedback.Failed(ElementIgnoringLocalizations.GetString("parameterStorageType_message"));
        if (parameter.IsReadOnly) 
            return IgnoreElementFeedback.Failed(ElementIgnoringLocalizations.GetString("parameterReadOnly_message"));

        string line = parameter.AsString();
        if (line is null || line == string.Empty) {
            parameter.Set(code);
            ForgetCodes(element);
            return IgnoreElementFeedback.Success();
        }

        if (ContainsCode(line, code))
            return IgnoreElementFeedback.Success();

        parameter.Set(AppendCode(line, code));
        ForgetCodes(element);

        return IgnoreElementFeedback.Success();
    }

    // A diagnostic run asks this for every relevant element of every diagnostic. Reading the ignore
    // parameter through the Revit API each time dominated the run, so the answer is assembled from data
    // kept in the transaction cache: whether the document has the ignore parameters at all, and the codes
    // of each element, read once and shared by all diagnostics.
    public bool IsElementIgnored(string code, Element element)
        => GetState(element.Document).IsIgnored(code, element);

    public IIgnoredElements GetIgnoredElements(Document document) => GetState(document);

    private IgnoreState GetState(Document document)
        => documentQueries.GetOrCreate(
            DocumentQueryKey.Create(document, IgnoreStateQuery),
            () => new IgnoreState(
                SharedParameterElement.Lookup(document, _instanceParameterId) is not null,
                SharedParameterElement.Lookup(document, _typeParameterId) is not null));

    private static string[]? ReadCodes(Element element)
    {
        Parameter? parameter = element.get_Parameter(GetParameterId(element));
        if (parameter is null || parameter.StorageType != StorageType.String) return null;

        return parameter.AsString()?.Split(_separator);
    }

    // The cache is invalidated only when the transaction that changed the parameter is committed, so the
    // element written by Ignore is dropped here to keep a check made inside that transaction correct.
    private void ForgetCodes(Element element)
    {
        IgnoreState state = GetState(element.Document);
        state.CodesByElement.Remove(element.Id);
        if (element is ElementType)
            state.HasTypeParameter = true;
        else
            state.HasInstanceParameter = true;
    }

    private static bool ContainsCode(string line, string code) =>
        line.Split(_separator).Contains(code);

    private static string AppendCode(string line, string code)
    {
        if (string.IsNullOrEmpty(line)) return code;
        return line[line.Length - 1] == _separator ? line + code : line + _separator + code;
    }

    private static Guid GetParameterId(Element element) =>
        element is ElementType ? _typeParameterId : _instanceParameterId;

    private sealed class IgnoreState(bool hasInstanceParameter, bool hasTypeParameter) : IIgnoredElements
    {
        public bool HasInstanceParameter { get; set; } = hasInstanceParameter;
        public bool HasTypeParameter { get; set; } = hasTypeParameter;
        public Dictionary<ElementId, string[]?> CodesByElement { get; } = [];

        public bool IsIgnored(string code, Element element)
        {
            if (element is ElementType ? !HasTypeParameter : !HasInstanceParameter) return false;

            ElementId elementId = element.Id;
            if (!CodesByElement.TryGetValue(elementId, out string[]? codes))
            {
                codes = ReadCodes(element);
                CodesByElement[elementId] = codes;
            }

            return codes is not null && Array.IndexOf(codes, code) >= 0;
        }
    }
}
