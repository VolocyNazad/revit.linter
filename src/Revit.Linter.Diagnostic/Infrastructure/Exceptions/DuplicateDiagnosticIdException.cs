namespace Revit.Linter.Diagnostic.Infrastructure.Exceptions;

public class DuplicateDiagnosticIdException() : Exception($"{nameof(ElementDiagnosticId)}(s) duplicated");
