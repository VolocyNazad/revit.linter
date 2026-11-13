namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>Defines one colour step of the findings heat map.</summary>
/// <param name="Red">The red channel from 0 to 255.</param>
/// <param name="Green">The green channel from 0 to 255.</param>
/// <param name="Blue">The blue channel from 0 to 255.</param>
public readonly record struct FindingHeatmapColor(byte Red, byte Green, byte Blue);

/// <summary>Describes a fixed range of failed checks of one element and its legend label.</summary>
/// <param name="MinimumCount">The smallest number of failed checks in the band.</param>
/// <param name="MaximumCount">The largest number of failed checks in the band; <see cref="int.MaxValue"/> for the open band.</param>
/// <param name="Label">The user-facing label of the band, such as <c>3–4</c>.</param>
public readonly record struct FindingHeatmapCountBand(int MinimumCount, int MaximumCount, string Label);

/// <summary>
/// Maps the most severe finding of an element and its number of distinct checks to a fixed colour step.
/// </summary>
/// <remarks>
/// The heat map uses one hue per severity and a brightness that grows with the number of failed checks.
/// The brightness ranges of the severities do not overlap, so any error stays brighter than any warning
/// and any warning stays brighter than any message. The table is shared by the Revit overrides and the
/// WPF legend, so the two never drift apart.
/// </remarks>
public static class FindingHeatmapScale
{
    /// <summary>Gets the fixed count bands, from the fewest checks to the open top band.</summary>
    public static IReadOnlyList<FindingHeatmapCountBand> CountBands { get; } =
    [
        new(1, 1, "1"),
        new(2, 2, "2"),
        new(3, 4, "3–4"),
        new(5, 8, "5–8"),
        new(9, 16, "9–16"),
        new(17, int.MaxValue, "17+"),
    ];

    // The hues follow the severities of the diagnostic report; the 3D view shades the surfaces, so the
    // colours stay more saturated than the text colours of the light palette.
    private static readonly FindingHeatmapColor ErrorColor = new(211, 47, 47);
    private static readonly FindingHeatmapColor WarningColor = new(224, 138, 0);
    private static readonly FindingHeatmapColor MessageColor = new(130, 160, 180);

    // Brightness factors per severity. The ranges are disjoint on purpose, so a single error is brighter
    // than any number of warnings and a warning is brighter than any number of messages.
    private const double ErrorMinimumBrightness = 0.80;
    private const double ErrorMaximumBrightness = 1.00;
    private const double WarningMinimumBrightness = 0.52;
    private const double WarningMaximumBrightness = 0.75;
    private const double MessageMinimumBrightness = 0.30;
    private const double MessageMaximumBrightness = 0.48;

    /// <summary>Returns the zero-based count band of a number of failed checks.</summary>
    /// <param name="count">The number of distinct checks an element fails.</param>
    /// <returns>The index of the band in <see cref="CountBands"/>; a count below one uses the first band.</returns>
    public static int GetLevel(int count)
    {
        for (int index = CountBands.Count - 1; index > 0; index--)
        {
            if (count >= CountBands[index].MinimumCount) return index;
        }

        return 0;
    }

    /// <summary>Returns the heat map colour of an element's most severe findings.</summary>
    /// <param name="severity">The most severe severity of the element's findings.</param>
    /// <param name="count">The number of distinct checks of that severity.</param>
    /// <returns>The colour step that the Revit override and the legend swatch use.</returns>
    public static FindingHeatmapColor GetColor(DiagnosticSeverity severity, int count)
    {
        FindingHeatmapColor baseColor = severity switch
        {
            DiagnosticSeverity.Error => ErrorColor,
            DiagnosticSeverity.Warning => WarningColor,
            _ => MessageColor,
        };
        (double minimum, double maximum) = severity switch
        {
            DiagnosticSeverity.Error => (ErrorMinimumBrightness, ErrorMaximumBrightness),
            DiagnosticSeverity.Warning => (WarningMinimumBrightness, WarningMaximumBrightness),
            _ => (MessageMinimumBrightness, MessageMaximumBrightness),
        };

        int level = GetLevel(count);
        double levelShare = CountBands.Count == 1 ? 0.0 : level / (double)(CountBands.Count - 1);
        double brightness = minimum + ((maximum - minimum) * levelShare);
        return new FindingHeatmapColor(
            ScaleChannel(baseColor.Red, brightness),
            ScaleChannel(baseColor.Green, brightness),
            ScaleChannel(baseColor.Blue, brightness));
    }

    private static byte ScaleChannel(byte channel, double brightness) =>
        (byte)Math.Min(byte.MaxValue, Math.Round(channel * brightness, MidpointRounding.AwayFromZero));
}
