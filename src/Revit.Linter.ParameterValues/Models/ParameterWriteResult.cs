namespace Revit.Linter.ParameterValues.Models;

/// <summary>Reports the outcome of writing a value to a parameter.</summary>
/// <remarks>
/// A rejected write leaves the parameter unchanged. <see cref="Failure"/> keeps the exception thrown by
/// Revit, so a caller can log the original reason next to the stable <see cref="Error"/> code.
/// </remarks>
public readonly struct ParameterWriteResult
{
    private ParameterWriteResult(bool success, ParameterValueError? error, Exception? failure)
    {
        Success = success;
        Error = error;
        Failure = failure;
    }

    /// <summary>Gets the result of a write that stored the new value.</summary>
    public static ParameterWriteResult Written { get; } = new(true, null, null);

    /// <summary>Gets whether the parameter received the new value.</summary>
    public bool Success { get; }

    /// <summary>Gets the reason of a rejected write, or <see langword="null"/> when the write succeeded.</summary>
    public ParameterValueError? Error { get; }

    /// <summary>Gets the exception Revit threw while writing, or <see langword="null"/> when it threw none.</summary>
    public Exception? Failure { get; }

    /// <summary>Creates a rejected write result.</summary>
    /// <param name="error">The reason of the rejection.</param>
    /// <param name="failure">The exception Revit threw, or <see langword="null"/> when the write was rejected before the call.</param>
    /// <returns>The rejected result.</returns>
    public static ParameterWriteResult Rejected(ParameterValueError error, Exception? failure = null) =>
        new(false, error, failure);
}