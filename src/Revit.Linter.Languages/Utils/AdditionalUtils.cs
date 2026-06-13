namespace Revit.Linter.Languages.Utils;

internal static class AdditionalUtils
{
    public static object? Add(object? left, object? right) => left switch
    {
        double leftDouble when right is double rightDouble => leftDouble + rightDouble,
        string leftString when right is string rightString => leftString + rightString,
        string leftString when right is double rightDouble => leftString + rightDouble,
        string leftString when right is null => leftString,
        null when right is string rightString => rightString,
        null when right is null => null,
        null => null,
        _ => left.ToString() + right,
    };
}
