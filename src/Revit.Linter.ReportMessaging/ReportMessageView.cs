using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace Revit.Linter.ReportMessaging;

public sealed class ReportMessageView : FlowDocumentScrollViewer
{
    public static readonly DependencyProperty PartsProperty = DependencyProperty.Register(
        nameof(Parts),
        typeof(IEnumerable<ReportTextPart>),
        typeof(ReportMessageView),
        new PropertyMetadata(null, OnMessagePropertyChanged));

    public static readonly DependencyProperty LinkCommandProperty = DependencyProperty.Register(
        nameof(LinkCommand),
        typeof(ICommand),
        typeof(ReportMessageView),
        new PropertyMetadata(null, OnMessagePropertyChanged));

    public static readonly DependencyProperty ToolTipFormatProperty = DependencyProperty.Register(
        nameof(ToolTipFormat),
        typeof(string),
        typeof(ReportMessageView),
        new PropertyMetadata(null, OnMessagePropertyChanged));

    public static readonly DependencyProperty IsObsoleteProperty = DependencyProperty.Register(
        nameof(IsObsolete),
        typeof(bool),
        typeof(ReportMessageView),
        new PropertyMetadata(false, OnMessagePropertyChanged));

    private static readonly Thickness NoPadding = new(0);

    public IEnumerable<ReportTextPart>? Parts
    {
        get => (IEnumerable<ReportTextPart>?)GetValue(PartsProperty);
        set => SetValue(PartsProperty, value);
    }

    public ICommand? LinkCommand
    {
        get => (ICommand?)GetValue(LinkCommandProperty);
        set => SetValue(LinkCommandProperty, value);
    }

    public string? ToolTipFormat
    {
        get => (string?)GetValue(ToolTipFormatProperty);
        set => SetValue(ToolTipFormatProperty, value);
    }

    public bool IsObsolete
    {
        get => (bool)GetValue(IsObsoleteProperty);
        set => SetValue(IsObsoleteProperty, value);
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == FontFamilyProperty || e.Property == FontSizeProperty || e.Property == FontWeightProperty)
            BuildDocument();
    }

    private static void OnMessagePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        => ((ReportMessageView)sender).BuildDocument();

    private void BuildDocument()
    {
        FlowDocument document = new()
        {
            FontFamily = FontFamily,
            FontSize = FontSize,
            FontWeight = FontWeight,
            PagePadding = NoPadding
        };
        Paragraph paragraph = new()
        {
            FontFamily = FontFamily,
            FontSize = FontSize,
            FontWeight = FontWeight,
            Margin = NoPadding
        };

        foreach (ReportTextPart part in Parts ?? [])
            paragraph.Inlines.Add(CreateInline(part));

        if (IsObsolete)
            paragraph.TextDecorations = TextDecorations.Strikethrough;

        document.Blocks.Add(paragraph);
        Document = document;
    }

    private Inline CreateInline(ReportTextPart part)
    {
        Run run = new(part.Text)
        {
            FontFamily = FontFamily,
            FontSize = FontSize,
            FontWeight = FontWeight
        };

        return part.Type switch
        {
            ReportInlineType.Text => run,
            ReportInlineType.Hyperlink => new Hyperlink(run)
            {
                Command = LinkCommand,
                CommandParameter = part.Data,
                FontFamily = FontFamily,
                FontSize = FontSize,
                FontWeight = FontWeight,
                ToolTip = ToolTipFormat is null
                    ? null
                    : string.Format(CultureInfo.CurrentCulture, ToolTipFormat, part.Text)
            },
            _ => throw new ArgumentOutOfRangeException(nameof(part), part.Type, "Unsupported report inline type.")
        };
    }
}
