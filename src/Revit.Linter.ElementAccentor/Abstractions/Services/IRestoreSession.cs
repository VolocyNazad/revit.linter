namespace Revit.Linter.ElementAccentor.Abstractions.Services;

/// <summary>
/// Owns the state captured by an element accent operation and can restore it.
/// </summary>
/// <remarks>
/// The session captures the preceding view and selection state, not element identity: it holds
/// no elements, identifiers, or documents. Restoring therefore works the same whatever target
/// the operation was applied to, including a link instance standing in for linked elements.
/// </remarks>
public interface IRestoreSession : IDisposable
{
    /// <summary>Restores the state that preceded the accent operation.</summary>
    void Restore();
}
