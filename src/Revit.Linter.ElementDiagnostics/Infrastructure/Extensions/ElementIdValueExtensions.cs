using Revit.Sugar;

namespace Revit.Linter.ElementDiagnostics.Infrastructure.Extensions;

/// <summary>Provides a version-proof numeric element identifier.</summary>
internal static class ElementIdValueExtensions
{
    /// <summary>Gets the numeric identifier as a long on every supported Revit version.</summary>
    /// <remarks>
    /// VolocyNazad's <c>Value()</c> widens from <see langword="int"/> to <see langword="long"/>
    /// between Revit versions, so a bare call does not pin the inferred type. The declared return
    /// type keeps comparisons and ordering identical across the supported versions.
    /// </remarks>
    internal static long IdValue(this ElementId id) => id.Value();
}
