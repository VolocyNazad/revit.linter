using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Revit.Linter.ThemeManaging.Controls;

/// <summary>
/// Draws one of the add-in icons in a single color.
/// </summary>
/// <remarks>
/// The icon is scaled to the smaller side of the element and centered in it. Its color is the inherited text
/// color, so an icon placed in a button follows the foreground of that button unless
/// <see cref="Foreground"/> is set on the icon. Without an explicit size the icon measures 16 by 16.
/// </remarks>
public sealed class PathIcon : FrameworkElement
{
    /// <summary>
    /// Identifies the <see cref="Kind"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty KindProperty = DependencyProperty.Register(
        nameof(Kind),
        typeof(IconKind),
        typeof(PathIcon),
        new FrameworkPropertyMetadata(default(IconKind), FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>
    /// Identifies the <see cref="Foreground"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ForegroundProperty = TextElement.ForegroundProperty.AddOwner(
        typeof(PathIcon),
        new FrameworkPropertyMetadata(
            SystemColors.ControlTextBrush,
            FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

    private const double DesignSize = 24;
    private const double DefaultSize = 16;

    /// <summary>
    /// Gets or sets the icon to draw.
    /// </summary>
    public IconKind Kind
    {
        get => (IconKind)GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    /// <summary>
    /// Gets or sets the brush the icon is filled with.
    /// </summary>
    public Brush Foreground
    {
        get => (Brush)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize) => new(DefaultSize, DefaultSize);

    /// <inheritdoc />
    protected override void OnRender(DrawingContext drawingContext)
    {
        Geometry? geometry = IconGeometries.Find(Kind);
        double side = Math.Min(RenderSize.Width, RenderSize.Height);
        if (geometry is null || side <= 0) return;

        double scale = side / DesignSize;
        drawingContext.PushTransform(new TranslateTransform(
            (RenderSize.Width - side) / 2, (RenderSize.Height - side) / 2));
        drawingContext.PushTransform(new ScaleTransform(scale, scale));
        drawingContext.DrawGeometry(Foreground, null, geometry);
        drawingContext.Pop();
        drawingContext.Pop();
    }
}
