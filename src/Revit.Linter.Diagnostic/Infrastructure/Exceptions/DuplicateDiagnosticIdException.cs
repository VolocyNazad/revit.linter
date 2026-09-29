namespace Revit.Linter.Diagnostic.Infrastructure.Exceptions;

/// <summary>
/// Represents an error caused by registering more than one diagnostic with the same code.
/// </summary>
/// <param name="code">The duplicated diagnostic code.</param>
public class DuplicateDiagnosticIdException(string code)
    : Exception($"Diagnostic code '{code}' is duplicated");
