namespace Revit.Linter.Core.Abstractions.Services;

/// <summary>Narrows the published diagnostics to a single check for a guided session.</summary>
/// <remarks>
/// The scope is session state, not configuration: it changes which checks the catalog publishes without
/// writing anything into the configuration files, and the catalog refreshes itself when the scope changes.
/// A session, such as the practical tour, begins the scope so its managed checks become visible, focuses
/// them while the session works with one check, and ends the scope when it pauses or finishes. Outside a
/// session the managed checks stay hidden, so a paused tour leaves neither its check nor its findings in
/// the user's diagnostics list and runs.
/// </remarks>
public interface IDiagnosticCatalogScope
{
    /// <summary>Occurs after the session state or the focused check changed.</summary>
    event EventHandler? Changed;

    /// <summary>Gets a value indicating whether a guided session is active.</summary>
    bool IsSessionActive { get; }

    /// <summary>Gets the code of the only published check, or <see langword="null"/> when every check is published.</summary>
    string? FocusedDiagnosticCode { get; }

    /// <summary>Makes the managed session checks visible.</summary>
    void BeginSession();

    /// <summary>Publishes every check again and hides the managed session checks.</summary>
    void EndSession();

    /// <summary>Publishes only the check with the given code until the focus is cleared.</summary>
    /// <param name="diagnosticCode">The code of the check the session works with.</param>
    /// <remarks>Focusing starts the session when none is active.</remarks>
    void Focus(string diagnosticCode);

    /// <summary>Publishes every check of the session again without ending it.</summary>
    void Clear();
}
