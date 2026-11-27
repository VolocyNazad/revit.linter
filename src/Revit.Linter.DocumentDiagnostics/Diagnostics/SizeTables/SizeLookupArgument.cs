namespace Revit.Linter.DocumentDiagnostics.Diagnostics.SizeTables;

/// <summary>One table or column argument of a <c>size_lookup</c> call.</summary>
/// <param name="Literal">The quoted value, or <see langword="null"/> when the argument is not a literal.</param>
/// <param name="ParameterName">The family parameter that supplies the value, or <see langword="null"/>.</param>
internal sealed record SizeLookupArgument(string? Literal, string? ParameterName)
{
    /// <summary>Gets an argument that is neither a literal nor a single family parameter.</summary>
    public static SizeLookupArgument Unresolved { get; } = new(null, null);

    /// <summary>Gets a value indicating whether the argument names a literal or a family parameter.</summary>
    public bool IsResolved => Literal is not null || ParameterName is not null;
}
