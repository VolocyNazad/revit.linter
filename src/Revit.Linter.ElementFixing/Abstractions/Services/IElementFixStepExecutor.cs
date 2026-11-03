using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.ElementFixing.Abstractions.Models;

namespace Revit.Linter.ElementFixing.Abstractions.Services;

/// <summary>Creates the executable step of one configured step kind.</summary>
internal interface IElementFixStepExecutor
{
    /// <summary>Gets the step kind this executor handles.</summary>
    ElementFixStepType Type { get; }

    /// <summary>Creates the executable step for one configured definition.</summary>
    /// <param name="step">The validated step definition with its step-specific fields.</param>
    /// <returns>The executable step.</returns>
    /// <remarks>
    /// The configuration load path calls this method once per configured step, so an implementation
    /// compiles its configuration there instead of repeating the work for every fix execution.
    /// </remarks>
    IElementFixStep Create(ElementFixStepDefinition step);
}