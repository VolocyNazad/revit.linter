namespace Revit.Linter.ElementAccentor.Abstractions.Models;

/// <summary>Selects how a category visibility operation treats the listed and the other categories.</summary>
public enum CategoryVisibilityMode
{
    /// <summary>Shows the listed categories and hides every other hideable category.</summary>
    OnlyVisible,

    /// <summary>Hides the listed categories and shows every other hideable category.</summary>
    OnlyHidden,

    /// <summary>Shows the listed categories and leaves the visibility of the other categories unchanged.</summary>
    Show,

    /// <summary>Hides the listed categories and leaves the visibility of the other categories unchanged.</summary>
    Hide,
}
