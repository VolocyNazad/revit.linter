namespace Revit.Linter.Behaviors;

/// <summary>
/// Exposes the diagnostic code of a grid row item for highlight matching.
/// </summary>
public interface IHasDiagnosticCode
{
    /// <summary>
    /// Gets the diagnostic code of the row item.
    /// </summary>
    string Code { get; }
}
