namespace Revit.Linter.Core.Abstractions.Services;

/// <summary>Exposes the configuration file parsing failure that the application should show the user.</summary>
public interface IDiagnosticConfigurationParseErrorSource
{
    /// <summary>Gets the last parsing failure, or <see langword="null"/> when every readable file parsed.</summary>
    Exception? CurrentError { get; }

    /// <summary>Occurs when <see cref="CurrentError"/> changes.</summary>
    event EventHandler? Changed;
}
