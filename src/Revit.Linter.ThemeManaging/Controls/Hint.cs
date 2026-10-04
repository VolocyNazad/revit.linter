using System.Windows;

namespace Revit.Linter.ThemeManaging.Controls;

/// <summary>
/// Attaches a hint to a text box: the text shown inside the box while it is empty.
/// </summary>
/// <remarks>
/// The hint is drawn by the text box templates of the shared theme resources; a text box with another
/// template ignores it.
/// </remarks>
public static class Hint
{
    /// <summary>
    /// Identifies the <c>Hint.Text</c> attached property.
    /// </summary>
    public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
        "Text",
        typeof(string),
        typeof(Hint),
        new FrameworkPropertyMetadata(null));

    /// <summary>
    /// Gets the hint of a text box.
    /// </summary>
    /// <param name="element">The text box.</param>
    /// <returns>The hint, or <see langword="null"/> when none is set.</returns>
    public static string? GetText(DependencyObject element) => (string?)element.GetValue(TextProperty);

    /// <summary>
    /// Sets the hint of a text box.
    /// </summary>
    /// <param name="element">The text box.</param>
    /// <param name="value">The hint, or <see langword="null"/> to show none.</param>
    public static void SetText(DependencyObject element, string? value) => element.SetValue(TextProperty, value);
}
