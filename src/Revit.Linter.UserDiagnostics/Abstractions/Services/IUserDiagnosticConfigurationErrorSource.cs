namespace Revit.Linter.UserDiagnostics.Abstractions.Services;

/// <summary>
/// Exposes the current user-diagnostic configuration parsing error and reports when it changes.
/// </summary>
public interface IUserDiagnosticConfigurationErrorSource
{
    /// <summary>
    /// Gets the current parsing error, or <see langword="null"/> after a successful configuration load.
    /// </summary>
    Exception? CurrentError { get; }

    /// <summary>
    /// Occurs when the current parsing error is added, replaced, or cleared.
    /// </summary>
    event EventHandler? Changed;
}
