namespace Revit.Linter.ElementIgnoring.Abstractions.Models;

public sealed record IgnoreElementFeedback(IgnoreElementResult Result, string? Message = null)
{
    public static IgnoreElementFeedback Success() => new(IgnoreElementResult.Success);
    public static IgnoreElementFeedback Failed(string message) => new(IgnoreElementResult.Failed, message);
}
