using System.Windows.Media;

namespace Revit.Linter.ThemeManaging.Controls;

/// <summary>
/// Holds the outlines of the icons, drawn on a 24 by 24 grid.
/// </summary>
/// <remarks>
/// The outlines are taken from Material Design Icons by Pictogrammers, licensed under the Apache License 2.0.
/// </remarks>
internal static class IconGeometries
{
    private static readonly Dictionary<IconKind, Geometry> Geometries = new()
    {
        [IconKind.ApplicationOutline] = Parse("M21 2H3C1.9 2 1 2.9 1 4V20C1 21.1 1.9 22 3 22H21C22.1 22 23 21.1 23 20V4C23 2.9 22.1 2 21 2M21 20H3V6H21V20Z"),
        [IconKind.CheckboxMultipleBlankOutline] = Parse("M20,16V4H8V16H20M22,16A2,2 0 0,1 20,18H8C6.89,18 6,17.1 6,16V4C6,2.89 6.89,2 8,2H20A2,2 0 0,1 22,4V16M16,20V22H4A2,2 0 0,1 2,20V7H4V20H16Z"),
        [IconKind.CheckboxMultipleOutline] = Parse("M20,2H8A2,2 0 0,0 6,4V16A2,2 0 0,0 8,18H20A2,2 0 0,0 22,16V4A2,2 0 0,0 20,2M20,16H8V4H20V16M16,20V22H4A2,2 0 0,1 2,20V7H4V20H16M18.53,8.06L17.47,7L12.59,11.88L10.47,9.76L9.41,10.82L12.59,14L18.53,8.06Z"),
        [IconKind.ChevronDown] = Parse("M7.41,8.58L12,13.17L16.59,8.58L18,10L12,16L6,10L7.41,8.58Z"),
        [IconKind.ChevronUp] = Parse("M7.41,15.41L12,10.83L16.59,15.41L18,14L12,8L6,14L7.41,15.41Z"),
        [IconKind.Export] = Parse("M23,12L19,8V11H10V13H19V16M1,18V6C1,4.89 1.9,4 3,4H15A2,2 0 0,1 17,6V9H15V6H3V18H15V15H17V18A2,2 0 0,1 15,20H3A2,2 0 0,1 1,18Z"),
        [IconKind.Eye] = Parse("M12,9A3,3 0 0,0 9,12A3,3 0 0,0 12,15A3,3 0 0,0 15,12A3,3 0 0,0 12,9M12,17A5,5 0 0,1 7,12A5,5 0 0,1 12,7A5,5 0 0,1 17,12A5,5 0 0,1 12,17M12,4.5C7,4.5 2.73,7.61 1,12C2.73,16.39 7,19.5 12,19.5C17,19.5 21.27,16.39 23,12C21.27,7.61 17,4.5 12,4.5Z"),
        [IconKind.Filter] = Parse("M14,12V19.88C14.04,20.18 13.94,20.5 13.71,20.71C13.32,21.1 12.69,21.1 12.3,20.71L10.29,18.7C10.06,18.47 9.96,18.16 10,17.87V12H9.97L4.21,4.62C3.87,4.19 3.95,3.56 4.38,3.22C4.57,3.08 4.78,3 5,3V3H19V3C19.22,3 19.43,3.08 19.62,3.22C20.05,3.56 20.13,4.19 19.79,4.62L14.03,12H14Z"),
        [IconKind.HelpCircleOutline] = Parse("M11,18H13V16H11V18M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,20C7.59,20 4,16.41 4,12C4,7.59 7.59,4 12,4C16.41,4 20,7.59 20,12C20,16.41 16.41,20 12,20M12,6A4,4 0 0,0 8,10H10A2,2 0 0,1 12,8A2,2 0 0,1 14,10C14,12 11,11.75 11,15H13C13,12.75 16,12.5 16,10A4,4 0 0,0 12,6Z"),
        [IconKind.Lightbulb] = Parse("M12,2A7,7 0 0,0 5,9C5,11.38 6.19,13.47 8,14.74V17A1,1 0 0,0 9,18H15A1,1 0 0,0 16,17V14.74C17.81,13.47 19,11.38 19,9A7,7 0 0,0 12,2M9,21A1,1 0 0,0 10,22H14A1,1 0 0,0 15,21V20H9V21Z"),
        [IconKind.Magnify] = Parse("M9.5,3A6.5,6.5 0 0,1 16,9.5C16,11.11 15.41,12.59 14.44,13.73L14.71,14H15.5L20.5,19L19,20.5L14,15.5V14.71L13.73,14.44C12.59,15.41 11.11,16 9.5,16A6.5,6.5 0 0,1 3,9.5A6.5,6.5 0 0,1 9.5,3M9.5,5C7,5 5,7 5,9.5C5,12 7,14 9.5,14C12,14 14,12 14,9.5C14,7 12,5 9.5,5Z"),
        [IconKind.Pause] = Parse("M14,19H18V5H14M6,19H10V5H6V19Z"),
        [IconKind.Play] = Parse("M8,5.14V19.14L19,12.14L8,5.14Z"),
        [IconKind.SwapHorizontal] = Parse("M21,9L17,5V8H10V10H17V13M7,11L3,15L7,19V16H14V14H7V11Z"),
    };

    /// <summary>
    /// Finds the frozen outline of an icon.
    /// </summary>
    /// <param name="kind">The icon to find.</param>
    /// <returns>The outline, or <see langword="null"/> when the icon has none.</returns>
    public static Geometry? Find(IconKind kind)
        => Geometries.TryGetValue(kind, out Geometry? geometry) ? geometry : null;

    private static Geometry Parse(string data)
    {
        Geometry geometry = Geometry.Parse(data);
        geometry.Freeze();
        return geometry;
    }
}
