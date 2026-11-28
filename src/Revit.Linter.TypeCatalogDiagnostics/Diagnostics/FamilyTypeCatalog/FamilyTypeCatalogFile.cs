namespace Revit.Linter.TypeCatalogDiagnostics.Diagnostics.FamilyTypeCatalog;

internal sealed class FamilyTypeCatalogFile
{
    private readonly HashSet<string> _parameterNames;
    private readonly HashSet<string> _typeNames;

    public FamilyTypeCatalogFile(
        string fileName,
        IReadOnlyList<string> parameterNames,
        IReadOnlyList<string> typeNames)
    {
        FileName = fileName;
        ParameterNames = parameterNames;
        TypeNames = typeNames;
        // Revit reads catalog parameter names case-sensitively, so a column with different casing
        // does not fill the parameter.
        _parameterNames = new HashSet<string>(parameterNames, StringComparer.Ordinal);
        _typeNames = new HashSet<string>(typeNames, StringComparer.OrdinalIgnoreCase);
    }

    public string FileName { get; }

    public IReadOnlyList<string> ParameterNames { get; }

    public IReadOnlyList<string> TypeNames { get; }

    public bool Contains(string typeName) => _typeNames.Contains(typeName);

    public bool ContainsParameter(string parameterName) => _parameterNames.Contains(parameterName);
}
