namespace Revit.Linter.Core.Abstractions.Services;

/// <summary>Notifies interested components that diagnostic formulas must be recompiled.</summary>
public interface IFormulaCompilationNotifier
{
    /// <summary>Publishes a formula recompilation notification.</summary>
    void Notify();
}
