namespace Revit.Linter.Presentation.Models;

/// <summary>
/// Orders diagnostic codes the way a reader does: text is compared as text, and every run of digits is
/// compared as a number.
/// </summary>
/// <remarks>
/// A WPF grid sorts a column by the value of its <c>SortMemberPath</c> property, so a row exposes this key
/// instead of the code text. A plain text comparison puts <c>CSTM10</c> before <c>CSTM2</c>.
/// </remarks>
public readonly struct DiagnosticCodeSortKey
    : IComparable, IComparable<DiagnosticCodeSortKey>, IEquatable<DiagnosticCodeSortKey>
{
    /// <summary>Initializes a sort key for a diagnostic code.</summary>
    /// <param name="code">The diagnostic code.</param>
    public DiagnosticCodeSortKey(string code) => Code = code ?? throw new ArgumentNullException(nameof(code));

    /// <summary>Gets the diagnostic code this key orders.</summary>
    public string Code { get; }

    /// <summary>Compares two diagnostic codes, treating every run of digits as a number.</summary>
    /// <param name="first">The first code, or <see langword="null"/>.</param>
    /// <param name="second">The second code, or <see langword="null"/>.</param>
    /// <returns>
    /// A negative number when <paramref name="first"/> comes first, zero when the codes are equal, and a
    /// positive number otherwise. A missing code comes before any code.
    /// </returns>
    public static int Compare(string? first, string? second)
    {
        if (ReferenceEquals(first, second)) return 0;
        if (first is null) return -1;
        if (second is null) return 1;

        int result = CompareWithNumbers(first, second);

        // Codes that differ only in leading zeros or letter case compare as one number. The raw text
        // keeps their order fixed instead of leaving it to the sort algorithm.
        return result != 0 ? result : StringComparer.Ordinal.Compare(first, second);
    }

    /// <inheritdoc />
    public int CompareTo(DiagnosticCodeSortKey other) => Compare(Code, other.Code);

    /// <inheritdoc />
    public int CompareTo(object? obj) => obj switch
    {
        null => 1,
        DiagnosticCodeSortKey other => CompareTo(other),
        _ => throw new ArgumentException($"The object must be a {nameof(DiagnosticCodeSortKey)}.", nameof(obj)),
    };

    /// <inheritdoc />
    public bool Equals(DiagnosticCodeSortKey other) => string.Equals(Code, other.Code, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is DiagnosticCodeSortKey other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Code);

    /// <summary>Checks whether two keys hold the same code.</summary>
    /// <param name="left">The first key.</param>
    /// <param name="right">The second key.</param>
    /// <returns><see langword="true"/> when both keys hold the same code.</returns>
    public static bool operator ==(DiagnosticCodeSortKey left, DiagnosticCodeSortKey right) => left.Equals(right);

    /// <summary>Checks whether two keys hold different codes.</summary>
    /// <param name="left">The first key.</param>
    /// <param name="right">The second key.</param>
    /// <returns><see langword="true"/> when the keys hold different codes.</returns>
    public static bool operator !=(DiagnosticCodeSortKey left, DiagnosticCodeSortKey right) => !left.Equals(right);

    /// <summary>Checks whether the code of the first key comes before the code of the second.</summary>
    /// <param name="left">The first key.</param>
    /// <param name="right">The second key.</param>
    /// <returns><see langword="true"/> when the first code comes first.</returns>
    public static bool operator <(DiagnosticCodeSortKey left, DiagnosticCodeSortKey right) => left.CompareTo(right) < 0;

    /// <summary>Checks whether the code of the first key comes after the code of the second.</summary>
    /// <param name="left">The first key.</param>
    /// <param name="right">The second key.</param>
    /// <returns><see langword="true"/> when the first code comes last.</returns>
    public static bool operator >(DiagnosticCodeSortKey left, DiagnosticCodeSortKey right) => left.CompareTo(right) > 0;

    /// <summary>Checks whether the code of the first key comes before the code of the second or is equal to it.</summary>
    /// <param name="left">The first key.</param>
    /// <param name="right">The second key.</param>
    /// <returns><see langword="true"/> when the first code does not come after the second.</returns>
    public static bool operator <=(DiagnosticCodeSortKey left, DiagnosticCodeSortKey right) => left.CompareTo(right) <= 0;

    /// <summary>Checks whether the code of the first key comes after the code of the second or is equal to it.</summary>
    /// <param name="left">The first key.</param>
    /// <param name="right">The second key.</param>
    /// <returns><see langword="true"/> when the first code does not come before the second.</returns>
    public static bool operator >=(DiagnosticCodeSortKey left, DiagnosticCodeSortKey right) => left.CompareTo(right) >= 0;

    private static int CompareWithNumbers(string first, string second)
    {
        int firstIndex = 0;
        int secondIndex = 0;
        while (firstIndex < first.Length && secondIndex < second.Length)
        {
            if (IsDigit(first[firstIndex]) && IsDigit(second[secondIndex]))
            {
                int numberResult = CompareNumber(first, ref firstIndex, second, ref secondIndex);
                if (numberResult != 0) return numberResult;
                continue;
            }

            int charResult = char.ToUpperInvariant(first[firstIndex])
                .CompareTo(char.ToUpperInvariant(second[secondIndex]));
            if (charResult != 0) return charResult;

            firstIndex++;
            secondIndex++;
        }

        // The code that runs out first comes first, so CSTM precedes CSTM1.
        return (first.Length - firstIndex).CompareTo(second.Length - secondIndex);
    }

    private static int CompareNumber(string first, ref int firstIndex, string second, ref int secondIndex)
    {
        int firstStart = firstIndex;
        while (firstIndex < first.Length && IsDigit(first[firstIndex])) firstIndex++;

        int secondStart = secondIndex;
        while (secondIndex < second.Length && IsDigit(second[secondIndex])) secondIndex++;

        // Leading zeros do not change the value, so 001 and 1 are the same number.
        while (firstStart < firstIndex - 1 && first[firstStart] == '0') firstStart++;
        while (secondStart < secondIndex - 1 && second[secondStart] == '0') secondStart++;

        int firstLength = firstIndex - firstStart;
        int secondLength = secondIndex - secondStart;
        if (firstLength != secondLength) return firstLength.CompareTo(secondLength);

        // Equal lengths mean the digits compare in order, and the longer number never overflows.
        for (int offset = 0; offset < firstLength; offset++)
        {
            int digitResult = first[firstStart + offset].CompareTo(second[secondStart + offset]);
            if (digitResult != 0) return digitResult;
        }

        return 0;
    }

    private static bool IsDigit(char value) => value is >= '0' and <= '9';
}
