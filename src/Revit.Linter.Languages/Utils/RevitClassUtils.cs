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
    private static readonly Type[] Types =
    [
        typeof(FamilyInstance),
        typeof(Pipe),
        typeof(GenericZone),
        typeof(Railing),
        typeof(ConduitSettings),
        typeof(DuctSettings),
        typeof(Hub)
    ];

    public static Type GetType(string typeName)
    {
        Type[] types = Types;
        foreach (Type type in types)
        {
            Type? type2 = Type.GetType(type.AssemblyQualifiedName.Replace(type.Name, typeName));
            if (type2 != null) return type2;
        }

        throw new RevitTypeNotFoundException(typeName);
    }
}

