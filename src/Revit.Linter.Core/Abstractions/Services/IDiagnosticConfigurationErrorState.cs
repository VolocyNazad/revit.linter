namespace Revit.Linter.Core.Abstractions.Services;

/// <summary>
/// Accumulates the configuration problems found while one diagnostic catalog snapshot is built.
/// </summary>
/// <remarks>
/// The catalog calls <see cref="BeginRefresh"/> before the registration providers read the configuration
/// and <see cref="CompleteRefresh"/> after they finish. The configuration service and the providers add
/// the file and rule problems they find, so the user sees one combined message instead of one dialog per
/// provider. A later refresh replaces the accumulated problems.
/// </remarks>
public interface IDiagnosticConfigurationErrorState : IDiagnosticConfigurationErrorSource
{
    /// <summary>Starts a new catalog refresh and forgets the problems of the previous one.</summary>
    void BeginRefresh();

    /// <summary>Replaces the file-level problems and the parsing failure of the current refresh.</summary>
    /// <param name="messages">The user-facing descriptions of unreadable files and invalid rules.</param>
    /// <param name="parseError">The parsing failure to expose separately, or <see langword="null"/>.</param>
    void SetFileErrors(IReadOnlyList<string> messages, Exception? parseError);

    /// <summary>Adds the problems a registration provider found while reading the configuration.</summary>
    /// <param name="messages">The user-facing descriptions of skipped rules.</param>
    void AddRuleErrors(IEnumerable<string> messages);

    /// <summary>Publishes the accumulated problems and raises <see cref="IDiagnosticConfigurationErrorSource.Changed"/> when they changed.</summary>
    void CompleteRefresh();
}
