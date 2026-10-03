using Revit.Linter.Localization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Shapes;

namespace Revit.Linter.Behaviors;

/// <summary>
/// Gives a control a ribbon-like tooltip: a title, a description, an optional note and, when a help command is
/// available, a hint that F1 opens the documentation.
/// </summary>
/// <remarks>
/// Setting <see cref="TitleProperty"/> replaces the control's <see cref="FrameworkElement.ToolTip"/>; clearing
/// it removes the tooltip again. The tooltip is also shown for a disabled control, so it can explain why the
/// control is unavailable. <see cref="HelpCommandProperty"/> is inherited: set it once on the root of a view and
/// every tooltip below it offers F1. The tooltip keeps the surrounding Material Design tooltip style.
/// </remarks>
public static class HelpToolTip
{
    private const double MaximumWidth = 320;
    private const int ShowDurationMilliseconds = 60000;

    private static readonly object OwnedMarker = new();

    /// <summary>Identifies the attached title, shown in bold on the first line.</summary>
    public static readonly DependencyProperty TitleProperty = DependencyProperty.RegisterAttached(
        "Title", typeof(string), typeof(HelpToolTip), new PropertyMetadata(null, OnContentChanged));

    /// <summary>Identifies the attached description, shown below the title.</summary>
    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.RegisterAttached(
        "Description", typeof(string), typeof(HelpToolTip), new PropertyMetadata(null, OnContentChanged));

    /// <summary>Identifies the attached note for changing details such as the duration of the last run.</summary>
    public static readonly DependencyProperty NoteProperty = DependencyProperty.RegisterAttached(
        "Note", typeof(string), typeof(HelpToolTip), new PropertyMetadata(null, OnContentChanged));

    /// <summary>Identifies the inherited command executed when F1 is pressed while a tooltip is open.</summary>
    public static readonly DependencyProperty HelpCommandProperty = DependencyProperty.RegisterAttached(
        "HelpCommand", typeof(ICommand), typeof(HelpToolTip),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, OnContentChanged));

    /// <summary>Gets the tooltip title of the element.</summary>
    /// <param name="element">The element that owns the tooltip.</param>
    /// <returns>The title, or <see langword="null"/> when the element has no help tooltip.</returns>
    public static string? GetTitle(DependencyObject element) => (string?)element.GetValue(TitleProperty);

    /// <summary>Sets the tooltip title of the element.</summary>
    /// <param name="element">The element that owns the tooltip.</param>
    /// <param name="value">The title; <see langword="null"/> or an empty string removes the tooltip.</param>
    public static void SetTitle(DependencyObject element, string? value) => element.SetValue(TitleProperty, value);

    /// <summary>Gets the tooltip description of the element.</summary>
    /// <param name="element">The element that owns the tooltip.</param>
    /// <returns>The description, or <see langword="null"/> when none is set.</returns>
    public static string? GetDescription(DependencyObject element) => (string?)element.GetValue(DescriptionProperty);

    /// <summary>Sets the tooltip description of the element.</summary>
    /// <param name="element">The element that owns the tooltip.</param>
    /// <param name="value">The description; <see langword="null"/> or an empty string hides the line.</param>
    public static void SetDescription(DependencyObject element, string? value) =>
        element.SetValue(DescriptionProperty, value);

    /// <summary>Gets the tooltip note of the element.</summary>
    /// <param name="element">The element that owns the tooltip.</param>
    /// <returns>The note, or <see langword="null"/> when none is set.</returns>
    public static string? GetNote(DependencyObject element) => (string?)element.GetValue(NoteProperty);

    /// <summary>Sets the tooltip note of the element.</summary>
    /// <param name="element">The element that owns the tooltip.</param>
    /// <param name="value">The note; <see langword="null"/> or an empty string hides the line.</param>
    public static void SetNote(DependencyObject element, string? value) => element.SetValue(NoteProperty, value);

    /// <summary>Gets the command executed when F1 is pressed while the element's tooltip is open.</summary>
    /// <param name="element">The element that owns the tooltip, or one of its ancestors.</param>
    /// <returns>The command, or <see langword="null"/> when F1 help is not offered.</returns>
    public static ICommand? GetHelpCommand(DependencyObject element) => (ICommand?)element.GetValue(HelpCommandProperty);

    /// <summary>Sets the command executed when F1 is pressed while a tooltip below the element is open.</summary>
    /// <param name="element">The element, normally the root of a view.</param>
    /// <param name="value">The command, executed without a parameter.</param>
    public static void SetHelpCommand(DependencyObject element, ICommand? value) =>
        element.SetValue(HelpCommandProperty, value);

    private static void OnContentChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        // The inherited help command reaches every element of a view; only the ones with a title own a tooltip.
        if (sender is not FrameworkElement element) return;

        string? title = GetTitle(element);
        ToolTip? owned = element.ToolTip is ToolTip { Tag: var tag } existing && ReferenceEquals(tag, OwnedMarker)
            ? existing
            : null;

        if (string.IsNullOrEmpty(title))
        {
            if (owned is not null) element.ClearValue(FrameworkElement.ToolTipProperty);
            return;
        }

        // The tooltip only has to exist; its content is read from the attached properties when it opens.
        if (owned is not null) return;

        element.ToolTip = CreateToolTip(element);
        ToolTipService.SetShowOnDisabled(element, true);
        ToolTipService.SetShowDuration(element, ShowDurationMilliseconds);
    }

    private static ToolTip CreateToolTip(FrameworkElement element)
    {
        ToolTip toolTip = new() { Tag = OwnedMarker };

        // The content is built just before the tooltip opens. Buttons in report rows are created by the
        // thousand, while only the one under the pointer ever shows its tooltip, and the note may change.
        element.ToolTipOpening += (_, _) =>
        {
            if (!ReferenceEquals(element.ToolTip, toolTip)) return;
            toolTip.Content = CreateContent(
                GetTitle(element) ?? string.Empty,
                GetDescription(element),
                GetNote(element),
                GetHelpCommand(element) is not null);
        };

        toolTip.Opened += (_, _) =>
        {
            if (GetHelpCommand(element) is null) return;
            HelpKeyHook.Install(() =>
            {
                toolTip.IsOpen = false;
                ICommand? command = GetHelpCommand(element);
                if (command?.CanExecute(null) == true) command.Execute(null);
            });
        };
        toolTip.Closed += (_, _) => HelpKeyHook.Uninstall();
        return toolTip;
    }

    private static StackPanel CreateContent(string title, string? description, string? note, bool offersHelp)
    {
        StackPanel panel = new() { MaxWidth = MaximumWidth };
        panel.Children.Add(new TextBlock
        {
            Text = title,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
        });

        if (!string.IsNullOrEmpty(description))
            panel.Children.Add(new TextBlock
            {
                Text = description,
                Margin = new Thickness(0, 4, 0, 0),
                TextWrapping = TextWrapping.Wrap,
            });

        if (!string.IsNullOrEmpty(note))
            panel.Children.Add(new TextBlock
            {
                Text = note,
                Margin = new Thickness(0, 6, 0, 0),
                Opacity = 0.8,
                TextWrapping = TextWrapping.Wrap,
            });

        if (!offersHelp) return panel;

        // The divider takes the tooltip's own text color, so it follows the theme without naming a brush.
        Rectangle divider = new() { Height = 1, Margin = new Thickness(0, 8, 0, 6), Opacity = 0.3 };
        divider.SetBinding(Shape.FillProperty, new Binding
        {
            Path = new PropertyPath(Control.ForegroundProperty),
            RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(ToolTip), 1),
        });
        panel.Children.Add(divider);
        panel.Children.Add(new TextBlock
        {
            Text = HelpToolTipLocalizations.GetString("pressF1_text"),
            Opacity = 0.8,
            TextWrapping = TextWrapping.Wrap,
        });
        return panel;
    }
}
