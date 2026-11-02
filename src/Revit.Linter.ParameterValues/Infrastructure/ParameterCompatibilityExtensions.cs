namespace Revit.Linter.ParameterValues.Infrastructure;

/// <summary>Isolates the Revit-version differences around parameter metadata.</summary>
internal static class ParameterCompatibilityExtensions
{
    /// <summary>Returns whether the parameter is a yes/no parameter.</summary>
    /// <param name="parameter">The parameter to inspect.</param>
    /// <returns><see langword="true"/> when Revit stores the parameter as a yes/no value.</returns>
    /// <remarks>
    /// Through Revit 2021 the yes/no kind is a <c>ParameterType</c>; Revit 2022 moved it to the data
    /// type identifier and removed the older spelling in later versions.
    /// </remarks>
    public static bool IsYesNo(this Parameter parameter)
    {
#if AFTER2022
        return parameter.Definition.GetDataType().Equals(SpecTypeId.Boolean.YesNo);
#else
        return parameter.Definition.ParameterType == ParameterType.YesNo;
#endif
    }
}