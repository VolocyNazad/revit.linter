namespace Revit.Linter.ElementVisualization.Services;

/// <summary>Identifies the kind of one configured visualization step.</summary>
/// <remarks>
/// The configuration file spells the step type as text; the factory parses that text into this
/// enumeration once, so the dispatch on the step behavior is checked by the compiler.
/// </remarks>
internal enum ElementVisualizationStepType
{
    Show,
    Select,
    Isolate,
    Cut,
    OverrideElements,
    OverrideFilter,
    DetailLevel,
    DisplayStyle,
    SetCategoryVisibility,
}
