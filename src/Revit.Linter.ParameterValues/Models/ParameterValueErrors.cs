namespace Revit.Linter.ParameterValues.Models;

/// <summary>Formats a <see cref="ParameterValueError"/> for log messages and diagnostics.</summary>
public static class ParameterValueErrors
{
    /// <summary>Returns the English description of a conversion or write failure.</summary>
    /// <param name="error">The failure code.</param>
    /// <returns>The description used in log messages.</returns>
    public static string Describe(ParameterValueError error) => error switch
    {
        ParameterValueError.MissingParameter => "the parameter does not exist",
        ParameterValueError.ReadOnly => "the parameter is read-only",
        ParameterValueError.UnsupportedStorageType => "the parameter has no supported value storage",
        ParameterValueError.NullValue => "the value is empty",
        ParameterValueError.TypeMismatch => "the value type does not match the parameter storage type",
        ParameterValueError.NotFinite => "the number is not finite",
        ParameterValueError.OutOfRange => "the number is out of range for the parameter storage type",
        ParameterValueError.InvalidElementId => "the element identifier is invalid",
        ParameterValueError.ForeignElementId =>
            "the element identifier does not belong to the document of the parameter",
        ParameterValueError.SetRejected => "Revit rejected the value",
        _ => "the parameter value cannot be converted",
    };
}