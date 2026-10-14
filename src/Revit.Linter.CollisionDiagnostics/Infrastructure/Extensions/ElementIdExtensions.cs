using Revit.Sugar;

namespace Revit.Linter.CollisionDiagnostics.Infrastructure.Extensions;

/// <summary>Provides a version-proof numeric element identifier.</summary>
internal static class ElementIdExtensions
{
    /// <summary>Gets the numeric identifier as a long on every supported Revit version.</summary>
    /// <remarks>
    /// VolocyNazad's <c>Value()</c> widens from <see langword="int"/> to <see langword="long"/>
    /// between Revit versions, so bare calls break generic inference (dictionaries, grouping)
    /// on older versions. The declared return type pins the inference without conditional
    /// compilation at call sites.
    /// </remarks>
    internal static long IdValue(this ElementId id) => id.Value();
}
