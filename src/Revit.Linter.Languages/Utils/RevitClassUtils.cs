using Autodesk.Revit.DB.Analysis;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Revit.Linter.Languages.Exceptions;

namespace Revit.Linter.Languages.Utils;

public static class RevitClassUtils
{
    private static readonly Type[] NamespaceTypes =
    [
        typeof(FamilyInstance),
        typeof(Pipe),
        typeof(GenericZone),
        typeof(Railing),
        typeof(ConduitSettings),
        typeof(DuctSettings),
        typeof(Hub)
    ];

    private static readonly IReadOnlyDictionary<string, Type> Types = CreateTypes();

    public static Type GetType(string typeName)
    {
        if (Types.TryGetValue(typeName, out Type? type)) return type;

        throw new RevitTypeNotFoundException(typeName);
    }

    private static IReadOnlyDictionary<string, Type> CreateTypes()
    {
        Dictionary<string, Type> types = new(StringComparer.Ordinal);
        Type[] exportedTypes = typeof(Element).Assembly.GetExportedTypes();

        foreach (Type namespaceType in NamespaceTypes)
        {
            foreach (Type type in exportedTypes)
            {
                if (type.Namespace == namespaceType.Namespace && !types.ContainsKey(type.Name))
                {
                    types.Add(type.Name, type);
                }
            }
        }

        return types;
    }
}

