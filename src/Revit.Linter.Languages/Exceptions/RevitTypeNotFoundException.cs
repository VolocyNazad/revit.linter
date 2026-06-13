namespace Revit.Linter.Languages.Exceptions;

public class RevitTypeNotFoundException : Exception
{
    public string TypeName { get; }

    public RevitTypeNotFoundException(string typeName)
        : base("Type '" + typeName + "'not fount in Revit API")
    {
        TypeName = typeName;
    }

    public RevitTypeNotFoundException(string typeName, Exception innerException)
        : base("Type '" + typeName + "'not fount in Revit API", innerException)
    {
        TypeName = typeName;
    }
}

