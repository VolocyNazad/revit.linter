namespace Revit.Linter.ThemeManaging.Controls;

/// <summary>
/// Names the icons the add-in draws with <see cref="PathIcon"/>.
/// </summary>
public enum IconKind
{
    /// <summary>A window frame: the active view.</summary>
    ApplicationOutline,

    /// <summary>Stacked empty check boxes: clear every check box.</summary>
    CheckboxMultipleBlankOutline,

    /// <summary>Stacked checked check boxes: set every check box.</summary>
    CheckboxMultipleOutline,

    /// <summary>A chevron pointing down: next.</summary>
    ChevronDown,

    /// <summary>A chevron pointing up: previous.</summary>
    ChevronUp,

    /// <summary>An arrow leaving a frame: export.</summary>
    Export,

    /// <summary>An eye: show in the model.</summary>
    Eye,

    /// <summary>A funnel: filters.</summary>
    Filter,

    /// <summary>A document with folded corner: open the defining file.</summary>
    FileDocumentOutline,

    /// <summary>A question mark in a circle: documentation.</summary>
    HelpCircleOutline,

    /// <summary>A light bulb: a fix is available.</summary>
    Lightbulb,

    /// <summary>A magnifier: search.</summary>
    Magnify,

    /// <summary>Two bars: pause.</summary>
    Pause,

    /// <summary>A triangle pointing right: run.</summary>
    Play,

    /// <summary>Two opposite arrows: invert.</summary>
    SwapHorizontal,
}
