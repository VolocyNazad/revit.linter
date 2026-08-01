namespace Revit.Linter.Diagnostic.Infrastructure.Exceptions;

public class DuplicateDiagnosticIdException(string code)
    : Exception($"Diagnostic code '{code}' is duplicated");
