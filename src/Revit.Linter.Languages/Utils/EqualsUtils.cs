namespace Revit.Linter.Languages.Utils;

internal static class EqualsUtils
{
    private const double Tolerance = 0.000000001;

    public static bool InternalEquals(object left, object right) => left switch
    {
        null when right is string => false,
        double leftDouble when right is double rightDouble => Math.Abs(leftDouble - rightDouble) < Tolerance,
        string leftString when right is string rightString => leftString == rightString,
        bool leftBool when right is bool rightBool => leftBool == rightBool,
        _ => false,
    };

    public static bool InternalNotEquals(object left, object right) => left switch
    {
        null when right is string => true,
        double leftDouble when right is double rightDouble => Math.Abs(leftDouble - rightDouble) > Tolerance,
        string leftString when right is string rightString => leftString != rightString,
        bool leftBool when right is bool rightBool => leftBool != rightBool,
        _ => false,
    };
}
