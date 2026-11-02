namespace Revit.Linter.ParameterValues.Services;

/// <summary>Finds the parameter of an element by the identifier spelled in diagnostic configuration.</summary>
/// <remarks>
/// The identifier is resolved in this order: a <see cref="BuiltInParameter"/> name, a shared parameter
/// GUID, then the parameter display name through <c>Element.LookupParameter</c>. The display name is
/// localized, so a configuration that uses it matches only a Revit interface in that language; prefer
/// <see cref="BuiltInParameter"/> names or shared parameter GUIDs.
/// </remarks>
public static class ParameterIdentifierResolver
{
    /// <summary>Returns the kind of an identifier: <see cref="BuiltInParameter"/>, <see cref="Guid"/> or <see cref="string"/>.</summary>
    /// <param name="identifier">The identifier spelled in configuration.</param>
    /// <returns>The type that drives the resolution of the identifier.</returns>
    public static Type GetIdentifierType(string identifier)
    {
        if (Enum.TryParse(identifier, out BuiltInParameter _))
            return typeof(BuiltInParameter);

        if (Guid.TryParse(identifier, out Guid _))
            return typeof(Guid);

        return typeof(string);
    }

    /// <summary>Finds the parameter by a <see cref="BuiltInParameter"/> name.</summary>
    /// <param name="element">The element that owns the parameter, or <see langword="null"/>.</param>
    /// <param name="identifier">The built-in parameter identifier.</param>
    /// <returns>The parameter, or <see langword="null"/> when the element does not have it.</returns>
    public static Parameter? GetParameter(Element? element, BuiltInParameter identifier) =>
        element?.get_Parameter(identifier);

    /// <summary>Finds the parameter by a shared parameter GUID.</summary>
    /// <param name="element">The element that owns the parameter, or <see langword="null"/>.</param>
    /// <param name="identifier">The shared parameter identifier.</param>
    /// <returns>The parameter, or <see langword="null"/> when the element does not have it.</returns>
    public static Parameter? GetParameter(Element? element, Guid identifier) =>
        element?.get_Parameter(identifier);

    /// <summary>Finds the parameter by its display name.</summary>
    /// <param name="element">The element that owns the parameter, or <see langword="null"/>.</param>
    /// <param name="identifier">The display name of the parameter.</param>
    /// <returns>The parameter, or <see langword="null"/> when the element does not have it.</returns>
    public static Parameter? GetParameter(Element? element, string identifier) =>
        element?.LookupParameter(identifier);

    /// <summary>Resolves a configuration identifier whose kind is not known in advance.</summary>
    /// <param name="element">The element that owns the parameter, or <see langword="null"/>.</param>
    /// <param name="identifier">The identifier spelled in configuration.</param>
    /// <returns>The parameter, or <see langword="null"/> when the element does not have it.</returns>
    /// <remarks>Use this overload when the identifier is computed by a formula at run time.</remarks>
    public static Parameter? Resolve(Element? element, string identifier)
    {
        if (element is null) return null;

        if (Enum.TryParse(identifier, out BuiltInParameter builtInParameter))
            return element.get_Parameter(builtInParameter);

        if (Guid.TryParse(identifier, out Guid guid))
            return element.get_Parameter(guid);

        return element.LookupParameter(identifier);
    }
}