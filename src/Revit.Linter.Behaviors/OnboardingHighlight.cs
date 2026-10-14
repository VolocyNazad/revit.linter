using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using Revit.Linter.Localization;

namespace Revit.Linter.Behaviors;

/// <summary>Highlights registered controls that match the active onboarding key.</summary>
/// <remarks>
/// Registration uses weak references and only affects controls owned by Revit Linter. The original WPF effect is
/// restored when the key changes, the control unloads or highlighting stops.
/// </remarks>
public static class OnboardingHighlight
{
    // Matches the spotlight fallback, so the button frame and the glow stay one color outside the theme dictionaries.
    private static readonly Color AccentFallback = Color.FromRgb(43, 123, 179);
    private static readonly List<WeakReference<FrameworkElement>> Elements = [];
    private static readonly List<SpotlightAdorner> Spotlights = [];
    private static OnboardingHighlightSession? _activeSession;
    private static string? _activeKey;

    /// <summary>Identifies the onboarding action represented by a WPF element.</summary>
    public static readonly DependencyProperty KeyProperty = DependencyProperty.RegisterAttached(
        "Key", typeof(string), typeof(OnboardingHighlight), new PropertyMetadata(null, OnKeyChanged));

    private static readonly DependencyProperty PreviousEffectProperty = DependencyProperty.RegisterAttached(
        "PreviousEffect", typeof(Effect), typeof(OnboardingHighlight));

    private static readonly DependencyProperty PreviousTransformProperty = DependencyProperty.RegisterAttached(
        "PreviousTransform", typeof(Transform), typeof(OnboardingHighlight));

    private static readonly DependencyProperty PreviousTransformOriginProperty = DependencyProperty.RegisterAttached(
        "PreviousTransformOrigin", typeof(Point), typeof(OnboardingHighlight));

    private static readonly DependencyProperty PreviousBorderProperty = DependencyProperty.RegisterAttached(
        "PreviousBorder", typeof(BorderSnapshot), typeof(OnboardingHighlight));

    /// <summary>Gets the onboarding key attached to an element.</summary>
    public static string? GetKey(DependencyObject element) => (string?)element.GetValue(KeyProperty);

    /// <summary>Sets the onboarding key attached to an element.</summary>
    public static void SetKey(DependencyObject element, string? value) => element.SetValue(KeyProperty, value);

    /// <summary>Identifies whether a highlighted element also breathes with a scale pulse.</summary>
    /// <remarks>Only genuine buttons take an element glow: other surfaces pulse in scale and halo only.</remarks>
    public static readonly DependencyProperty ScalePulseProperty = DependencyProperty.RegisterAttached(
        "ScalePulse", typeof(bool), typeof(OnboardingHighlight), new PropertyMetadata(true, OnScalePulseChanged));

    /// <summary>Gets whether the element scales while highlighted.</summary>
    public static bool GetScalePulse(DependencyObject element) => (bool)element.GetValue(ScalePulseProperty);

    /// <summary>Sets whether the element scales while highlighted.</summary>
    public static void SetScalePulse(DependencyObject element, bool value) => element.SetValue(ScalePulseProperty, value);

    /// <summary>Starts an exclusively owned highlight session and ends the previous session, if any.</summary>
    public static OnboardingHighlightSession StartSession()
    {
        _activeSession?.Dispose();
        OnboardingHighlightSession session = new();
        _activeSession = session;
        return session;
    }

    internal static void SetActiveKey(OnboardingHighlightSession session, string? key)
    {
        if (!ReferenceEquals(session, _activeSession)) return;
        SetActiveKeyCore(key);
    }

    internal static void EndSession(OnboardingHighlightSession session)
    {
        if (!ReferenceEquals(session, _activeSession)) return;
        _activeSession = null;
        SetActiveKeyCore(null);
    }

    private static void SetActiveKeyCore(string? key)
    {
        _activeKey = key;
        for (int index = Elements.Count - 1; index >= 0; index--)
        {
            if (!Elements[index].TryGetTarget(out FrameworkElement? element))
            {
                Elements.RemoveAt(index);
                continue;
            }

            UpdateHighlight(element);
        }

        UpdateSpotlights();
    }

    private static void OnKeyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is not FrameworkElement element) return;
        element.Loaded -= Element_Loaded;
        element.Unloaded -= Element_Unloaded;
        element.Loaded += Element_Loaded;
        element.Unloaded += Element_Unloaded;
        Register(element);
        UpdateHighlight(element);
        UpdateSpotlights();
    }

    private static void OnScalePulseChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is not FrameworkElement element) return;
        UpdateHighlight(element);
        UpdateSpotlights();
    }

    private static void Element_Loaded(object sender, RoutedEventArgs args)
    {
        if (sender is not FrameworkElement element) return;
        Register(element);
        UpdateHighlight(element);
        UpdateSpotlights();
    }

    private static void Element_Unloaded(object sender, RoutedEventArgs args)
    {
        if (sender is not FrameworkElement element) return;
        Restore(element);
        UpdateSpotlights();
    }

    private static void Register(FrameworkElement element)
    {
        if (Elements.Any(reference => reference.TryGetTarget(out FrameworkElement? target)
            && ReferenceEquals(target, element))) return;
        Elements.Add(new(element));
    }

    private static void UpdateHighlight(FrameworkElement element)
    {
        if (!IsActive(element))
        {
            Restore(element);
            return;
        }

        UpdateButtonFrame(element);
        if (!IsButton(element))
        {
            // Rows, text boxes, check boxes and other non-button surfaces must not take an element
            // effect: on their transparent backgrounds a DropShadowEffect samples the inner text
            // itself and draws accent shadows around every glyph. They keep the scale pulse and the
            // spotlight halo, which is pure geometry in the adorner, so the text never casts a glow.
            RestoreEffectAndTransform(element);
            if (SystemParameters.ClientAreaAnimation && GetScalePulse(element))
                StartScalePulse(element);
            return;
        }

        if (element.ReadLocalValue(PreviousEffectProperty) == DependencyProperty.UnsetValue)
            element.SetValue(PreviousEffectProperty, element.Effect);

        // A small radius keeps the per-frame software render cheap: large blurs stutter,
        // most visibly on buttons inside virtualized grid rows.
        // Icon-only buttons stay on the element effect: their silhouette is the icon itself.
        DropShadowEffect effect = new()
        {
            BlurRadius = 12,
            Color = AccentFallback,
            Direction = 0,
            Opacity = 1,
            ShadowDepth = 0
        };
        element.Effect = effect;
        if (SystemParameters.ClientAreaAnimation)
        {
            DoubleAnimation pulse = new(0.7, 1, TimeSpan.FromMilliseconds(800))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            effect.BeginAnimation(DropShadowEffect.OpacityProperty, pulse);

            if (!GetScalePulse(element))
            {
                RestoreTransform(element);
                return;
            }

            StartScalePulse(element);
        }
    }

    private static void StartScalePulse(FrameworkElement element)
    {
        if (element.ReadLocalValue(PreviousTransformProperty) == DependencyProperty.UnsetValue)
        {
            element.SetValue(PreviousTransformProperty, element.RenderTransform);
            element.SetValue(PreviousTransformOriginProperty, element.RenderTransformOrigin);
        }

        element.RenderTransformOrigin = new(0.5, 0.5);
        ScaleTransform scale = new(1, 1);
        element.RenderTransform = scale;
        DoubleAnimation grow = new(1, 1.12, TimeSpan.FromMilliseconds(800))
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever
        };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, grow);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, grow);
    }

    private static bool IsActive(FrameworkElement element)
        => element.IsLoaded
           && element.IsVisible
           && !string.IsNullOrEmpty(_activeKey)
           && string.Equals(GetKey(element), _activeKey, StringComparison.Ordinal);

    private static void UpdateSpotlights()
    {
        foreach (SpotlightAdorner spotlight in Spotlights) spotlight.Dispose();
        Spotlights.Clear();

        foreach (IGrouping<FrameworkElement, FrameworkElement> group in Elements
                     .Select(reference => reference.TryGetTarget(out FrameworkElement? element) ? element : null)
                     .Where(element => element is not null && IsActive(element))
                     .Cast<FrameworkElement>()
                     .Select(element => (Element: element, Scope: FindScope(element)))
                     .Where(item => item.Scope is not null)
                     .GroupBy(item => item.Scope!, item => item.Element))
        {
            AdornerLayer? layer = AdornerLayer.GetAdornerLayer(group.Key);
            if (layer is null) continue;

            SpotlightAdorner spotlight = new(group.Key, group.ToArray());
            layer.Add(spotlight);
            Spotlights.Add(spotlight);
        }
    }

    private static FrameworkElement? FindScope(FrameworkElement element)
    {
        FrameworkElement? scope = null;
        DependencyObject? current = element;
        while (current is not null)
        {
            if (current is UserControl userControl) scope = userControl;
            current = VisualTreeHelper.GetParent(current);
        }

        return scope;
    }

    private static void Restore(FrameworkElement element)
    {
        RestoreEffectAndTransform(element);
        RestoreButtonFrame(element);
    }

    private static void RestoreEffectAndTransform(FrameworkElement element)
    {
        if (element.ReadLocalValue(PreviousEffectProperty) != DependencyProperty.UnsetValue)
        {
            element.Effect = (Effect?)element.GetValue(PreviousEffectProperty);
            element.ClearValue(PreviousEffectProperty);
        }

        RestoreTransform(element);
    }

    private static void RestoreTransform(FrameworkElement element)
    {
        if (element.ReadLocalValue(PreviousTransformProperty) == DependencyProperty.UnsetValue) return;
        element.RenderTransform = (Transform)element.GetValue(PreviousTransformProperty);
        element.RenderTransformOrigin =
            (Point)element.GetValue(PreviousTransformOriginProperty);
        element.ClearValue(PreviousTransformProperty);
        element.ClearValue(PreviousTransformOriginProperty);
    }

    // The icon buttons are flat and transparent until they are pointed at, so a highlighted button takes an
    // accent frame of its own: inside the dimmed pane the spotlight outline alone leaves the icon floating.
    private static void UpdateButtonFrame(FrameworkElement element)
    {
        if (element is not Control control || !IsButton(element)) return;

        if (control.ReadLocalValue(PreviousBorderProperty) == DependencyProperty.UnsetValue)
        {
            control.SetValue(PreviousBorderProperty, new BorderSnapshot(
                control.BorderBrush,
                control.BorderThickness,
                control.ReadLocalValue(Control.BorderBrushProperty) != DependencyProperty.UnsetValue,
                control.ReadLocalValue(Control.BorderThicknessProperty) != DependencyProperty.UnsetValue));
        }

        control.BorderBrush = element.TryFindResource("LinterAccentBrush") as Brush
            ?? new SolidColorBrush(AccentFallback);
        control.BorderThickness = new(1);
    }

    private static void RestoreButtonFrame(FrameworkElement element)
    {
        if (element is not Control control) return;
        if (control.ReadLocalValue(PreviousBorderProperty) is not BorderSnapshot snapshot) return;

        if (snapshot.HasLocalBorderBrush) control.SetValue(Control.BorderBrushProperty, snapshot.BorderBrush);
        else control.ClearValue(Control.BorderBrushProperty);

        if (snapshot.HasLocalBorderThickness) control.SetValue(Control.BorderThicknessProperty, snapshot.BorderThickness);
        else control.ClearValue(Control.BorderThicknessProperty);

        control.ClearValue(PreviousBorderProperty);
    }

    // Check boxes and radio buttons derive from ToggleButton: the element glow is only for genuine buttons.
    private static bool IsButton(FrameworkElement element)
        => (element is Button or ToggleButton) && element is not CheckBox and not RadioButton;

    // Remembers the border a highlighted button carried before the onboarding frame replaced it.
    private sealed record BorderSnapshot(
        Brush? BorderBrush,
        Thickness BorderThickness,
        bool HasLocalBorderBrush,
        bool HasLocalBorderThickness);
}

internal sealed class SpotlightAdorner : Adorner, IDisposable
{
    private const double TargetPadding = 7;
    private const double LabelGap = 12;

    /// <summary>Identifies the pulsing opacity of the halo drawn around the targets.</summary>
    /// <remarks>
    /// The halo is pure geometry around the target rectangles, so unlike an element
    /// <see cref="System.Windows.Media.Effects.DropShadowEffect"/> it never samples the inner text.
    /// </remarks>
    public static readonly DependencyProperty GlowOpacityProperty = DependencyProperty.Register(
        nameof(GlowOpacity),
        typeof(double),
        typeof(SpotlightAdorner),
        new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Gets or sets the pulsing opacity of the halo drawn around the targets.</summary>
    public double GlowOpacity
    {
        get => (double)GetValue(GlowOpacityProperty);
        set => SetValue(GlowOpacityProperty, value);
    }

    private readonly IReadOnlyList<FrameworkElement> _targets;
    private readonly Border _label;
    private Rect _labelRect;
    private Rect _primaryTargetRect;
    private bool _disposed;

    public SpotlightAdorner(FrameworkElement scope, IReadOnlyList<FrameworkElement> targets) : base(scope)
    {
        _targets = targets;
        IsHitTestVisible = false;
        _label = new Border
        {
            Background = GetBrush(scope, "LinterAccentBrush", Color.FromRgb(43, 123, 179)),
            CornerRadius = new(3),
            Padding = new(8, 4, 8, 4),
            Child = new TextBlock
            {
                Foreground = GetBrush(scope, "LinterAccentForegroundBrush", Colors.White),
                FontFamily = SystemFonts.MessageFontFamily,
                FontSize = SystemFonts.MessageFontSize,
                FontWeight = FontWeights.SemiBold,
                Text = LocalizationResourceReader.GetString(
                    "Revit.Linter.Localization.Behaviors.OnboardingHighlight", "nextStep_text"),
            },
        };
        AddVisualChild(_label);
        scope.LayoutUpdated += Scope_LayoutUpdated;
        scope.Unloaded += Scope_Unloaded;
        if (SystemParameters.ClientAreaAnimation)
        {
            DoubleAnimation glowPulse = new(0.45, 1, TimeSpan.FromMilliseconds(800))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
            };
            BeginAnimation(GlowOpacityProperty, glowPulse);
        }
    }

    protected override int VisualChildrenCount => 1;

    protected override Visual GetVisualChild(int index)
        => index == 0 ? _label : throw new ArgumentOutOfRangeException(nameof(index));

    protected override Size MeasureOverride(Size constraint)
    {
        _label.Measure(new(double.PositiveInfinity, double.PositiveInfinity));
        return AdornedElement.RenderSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        IReadOnlyList<Rect> targetRects = GetTargetRects();
        _primaryTargetRect = targetRects.FirstOrDefault();
        _labelRect = PlaceLabel(finalSize, _primaryTargetRect, _label.DesiredSize);
        _label.Arrange(_labelRect);
        return finalSize;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        IReadOnlyList<Rect> targetRects = GetTargetRects();
        if (targetRects.Count == 0) return;

        GeometryGroup mask = new() { FillRule = FillRule.EvenOdd };
        mask.Children.Add(new RectangleGeometry(new(RenderSize)));
        foreach (Rect targetRect in targetRects)
            mask.Children.Add(new RectangleGeometry(targetRect, 4, 4));

        drawingContext.DrawGeometry(
            GetBrush((FrameworkElement)AdornedElement, "LinterSpotlightDimBrush", Color.FromArgb(176, 0, 0, 0)),
            null,
            mask);
        Brush accent = GetBrush((FrameworkElement)AdornedElement, "LinterAccentBrush", Color.FromRgb(43, 123, 179));
        // Soft halo around each target: geometry only, so the text inside a transparent
        // row or box never casts accent shadows the way an element effect would.
        double glow = GlowOpacity;
        foreach (Rect targetRect in targetRects)
        {
            drawingContext.DrawRoundedRectangle(null, HaloPen(accent, 9, 0.14 * glow), targetRect, 4, 4);
            drawingContext.DrawRoundedRectangle(null, HaloPen(accent, 6, 0.22 * glow), targetRect, 4, 4);
        }

        Pen outline = new(accent, 4);
        foreach (Rect targetRect in targetRects)
            drawingContext.DrawRoundedRectangle(null, outline, targetRect, 4, 4);

        DrawConnector(drawingContext, accent);
    }

    private static Pen HaloPen(Brush accent, double thickness, double opacity)
    {
        Brush halo = accent.Clone();
        halo.Opacity *= opacity;
        halo.Freeze();
        Pen pen = new(halo, thickness);
        pen.Freeze();
        return pen;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        BeginAnimation(GlowOpacityProperty, null);
        FrameworkElement scope = (FrameworkElement)AdornedElement;
        scope.LayoutUpdated -= Scope_LayoutUpdated;
        scope.Unloaded -= Scope_Unloaded;
        AdornerLayer.GetAdornerLayer(scope)?.Remove(this);
    }

    private IReadOnlyList<Rect> GetTargetRects()
    {
        FrameworkElement scope = (FrameworkElement)AdornedElement;
        Rect scopeRect = new(scope.RenderSize);
        List<Rect> result = [];
        foreach (FrameworkElement target in _targets)
        {
            if (!target.IsVisible || target.ActualWidth <= 0 || target.ActualHeight <= 0) continue;
            try
            {
                GeneralTransform transform = target.TransformToAncestor(scope);
                Rect rect = transform.TransformBounds(new(new Point(), target.RenderSize));
                rect.Inflate(TargetPadding, TargetPadding);
                rect.Intersect(scopeRect);
                if (!rect.IsEmpty) result.Add(rect);
            }
            catch (InvalidOperationException)
            {
                // A virtualized row may leave the visual tree between layout and rendering.
            }
        }

        return result;
    }

    private static Rect PlaceLabel(Size available, Rect target, Size label)
    {
        if (target.IsEmpty) return Rect.Empty;
        double x;
        double y;
        if (available.Width - target.Right >= label.Width + LabelGap)
        {
            x = target.Right + LabelGap;
            y = target.Top + (target.Height - label.Height) / 2;
        }
        else if (target.Left >= label.Width + LabelGap)
        {
            x = target.Left - LabelGap - label.Width;
            y = target.Top + (target.Height - label.Height) / 2;
        }
        else if (available.Height - target.Bottom >= label.Height + LabelGap)
        {
            x = target.Left + (target.Width - label.Width) / 2;
            y = target.Bottom + LabelGap;
        }
        else if (target.Top >= label.Height + LabelGap)
        {
            x = target.Left + (target.Width - label.Width) / 2;
            y = target.Top - LabelGap - label.Height;
        }
        else
        {
            x = target.Left + TargetPadding;
            y = target.Top + TargetPadding;
        }

        x = Math.Max(0, Math.Min(x, available.Width - label.Width));
        y = Math.Max(0, Math.Min(y, available.Height - label.Height));
        return new(new(x, y), label);
    }

    private void DrawConnector(DrawingContext drawingContext, Brush accent)
    {
        if (_labelRect.IsEmpty || _primaryTargetRect.IsEmpty) return;
        Point from;
        Point to;
        if (_labelRect.Left >= _primaryTargetRect.Right)
        {
            from = new(_labelRect.Left, _labelRect.Top + _labelRect.Height / 2);
            to = new(_primaryTargetRect.Right, _primaryTargetRect.Top + _primaryTargetRect.Height / 2);
        }
        else if (_labelRect.Right <= _primaryTargetRect.Left)
        {
            from = new(_labelRect.Right, _labelRect.Top + _labelRect.Height / 2);
            to = new(_primaryTargetRect.Left, _primaryTargetRect.Top + _primaryTargetRect.Height / 2);
        }
        else if (_labelRect.Top >= _primaryTargetRect.Bottom)
        {
            from = new(_labelRect.Left + _labelRect.Width / 2, _labelRect.Top);
            to = new(_primaryTargetRect.Left + _primaryTargetRect.Width / 2, _primaryTargetRect.Bottom);
        }
        else if (_labelRect.Bottom <= _primaryTargetRect.Top)
        {
            from = new(_labelRect.Left + _labelRect.Width / 2, _labelRect.Bottom);
            to = new(_primaryTargetRect.Left + _primaryTargetRect.Width / 2, _primaryTargetRect.Top);
        }
        else
        {
            return;
        }

        drawingContext.DrawLine(new(accent, 2), from, to);
    }

    private static Brush GetBrush(FrameworkElement element, string key, Color fallback)
        => element.TryFindResource(key) as Brush ?? new SolidColorBrush(fallback);

    private void Scope_LayoutUpdated(object? sender, EventArgs args)
    {
        IReadOnlyList<Rect> targetRects = GetTargetRects();
        _primaryTargetRect = targetRects.FirstOrDefault();
        _labelRect = PlaceLabel(RenderSize, _primaryTargetRect, _label.DesiredSize);
        _label.Arrange(_labelRect);
        InvalidateVisual();
    }

    private void Scope_Unloaded(object sender, RoutedEventArgs args) => Dispose();
}

/// <summary>Owns one temporary onboarding-highlight lifetime.</summary>
public sealed class OnboardingHighlightSession : IDisposable
{
    private bool _disposed;

    internal OnboardingHighlightSession()
    {
    }

    /// <summary>Highlights controls registered for the supplied key, or clears emphasis when it is null.</summary>
    public void SetActiveKey(string? key)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(OnboardingHighlightSession));
        OnboardingHighlight.SetActiveKey(this, key);
    }

    /// <summary>Ends this session and restores every effect it applied.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        OnboardingHighlight.EndSession(this);
    }
}
