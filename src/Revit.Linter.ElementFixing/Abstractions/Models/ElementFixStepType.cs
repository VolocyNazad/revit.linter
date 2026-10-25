namespace Revit.Linter.ElementFixing.Abstractions.Models;

/// <summary>Identifies the kind of one configured fix step.</summary>
/// <remarks>
/// The configuration file spells the step type as text; the fixing module parses that text into this
/// enumeration once, so executors and pipeline validation share one closed set of step kinds.
/// </remarks>
internal enum ElementFixStepType
{
    Delete,
}
