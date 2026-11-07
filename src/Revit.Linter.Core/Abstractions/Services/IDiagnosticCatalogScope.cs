namespace Revit.Linter.Core.Abstractions.Services;

/// <summary>Narrows the published diagnostics to a single check for a guided session.</summary>
/// <remarks>
/// The scope is session state, not configuration: it changes which checks the catalog publishes without
/// writing anything into the configuration files, and the catalog refreshes itself when the scope changes.
/// A session that needs only its own check, such as the practical tour, focuses that check and clears the
/// scope when it ends, so the user's own rules and their states are never touched.
/// </remarks>
public interface IDiagnosticCatalogScope
{
    /// <summary>Occurs after the focused check changed.</summary>
    event EventHandler? Changed;

    /// <summary>Gets the code of the only published check, or <see langword="null"/> when every check is published.</summary>
    string? FocusedDiagnosticCode { get; }

    /// <summary>Publishes only the check with the given code until the scope is cleared.</summary>
    /// <param name="diagnosticCode">The code of the check the session works with.</param>
    void Focus(string diagnosticCode);

    /// <summary>Publishes every check again.</summary>
    void Clear();
}
