namespace Revit.Linter.ElementAccentor.Abstractions.Models;

/// <summary>
/// Identifies an operation that can participate in an element visualization pipeline.
/// </summary>
public enum AccentElementsType
{
    /// <summary>Adjusts a three-dimensional view section box around the elements.</summary>
    CutViewByElements,

    /// <summary>Temporarily isolates the elements in the active view.</summary>
    IsolateElementsOnView,

    /// <summary>Selects the elements in the Revit user interface.</summary>
    SelectElements,

    /// <summary>Moves the active view to show the elements.</summary>
    ShowElements
}
