namespace Revit.Linter.ElementAccentor.Abstractions.Services;

/// <summary>
/// Owns the state captured by an element accent operation and can restore it.
/// </summary>
public interface IElementAccentSession : IDisposable
{
    /// <summary>Restores the state that preceded the accent operation.</summary>
    void Restore();
}
