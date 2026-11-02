namespace Revit.Linter.ParameterValues.Models;

/// <summary>Names the reason a parameter value could not be read or written.</summary>
/// <remarks>
/// The codes do not depend on the user interface language. A caller logs them through
/// <see cref="ParameterValueErrors.Describe(ParameterValueError)"/> and compares them in tests.
/// </remarks>
public enum ParameterValueError
{
    /// <summary>The parameter does not exist for the element.</summary>
    MissingParameter,

    /// <summary>The parameter is read-only.</summary>
    ReadOnly,

    /// <summary>The parameter has no value storage or a storage type this module does not convert.</summary>
    UnsupportedStorageType,

    /// <summary>The value is empty while the parameter requires one.</summary>
    NullValue,

    /// <summary>The value type does not fit the storage type of the parameter.</summary>
    TypeMismatch,

    /// <summary>The number is not finite and cannot be stored.</summary>
    NotFinite,

    /// <summary>The number does not fit the storage type after conversion.</summary>
    OutOfRange,

    /// <summary>The element identifier is invalid.</summary>
    InvalidElementId,

    /// <summary>The element identifier does not belong to the document of the parameter.</summary>
    ForeignElementId,

    /// <summary>Revit rejected the value and the parameter kept its previous value.</summary>
    SetRejected,
}