using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.Diagnostic.Infrastructure.Exceptions;

internal class DuplicateDiagnosticIdException() : Exception($"{nameof(ElementDiagnosticId)}(s) duplicated");
