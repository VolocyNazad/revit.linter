using System.ComponentModel;

namespace Revit.Linter.Presentation.ViewModels;

/// <summary>
/// A toggleable pane filter shared by presenter modules.
/// </summary>
/// <typeparam name="TItem">The row type the filter applies to.</typeparam>
public interface IDiagnosticFilter<in TItem> : INotifyPropertyChanged
{
    /// <summary>Gets whether the filter currently lets its items through.</summary>
    bool IsActive { get; }

    /// <summary>Checks whether the row passes the filter, ignoring whether it is active.</summary>
    /// <param name="item">The row to check.</param>
    /// <returns>True when the row matches the filter.</returns>
    bool IsValid(TItem item);
}
