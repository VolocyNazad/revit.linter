using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

using VolocyNazad.Wpf.DependencyProperties;

namespace Revit.Linter.ReportMessaging;

/// <summary>
/// Renders report message parts as WPF text and command-enabled hyperlinks.
/// </summary>
[GenerateDependencyProperty<IEnumerable<ReportTextPart>>(
    "Parts",
    IsNullable = true,
    OnChanged = nameof(OnMessagePropertyChanged),
    PropertyXmlDocumentation = "Gets or sets the report message parts to render.")]
[GenerateDependencyProperty<ICommand>(
    "LinkCommand",
    IsNullable = true,
    OnChanged = nameof(OnMessagePropertyChanged),
    PropertyXmlDocumentation = "Gets or sets the command invoked by rendered hyperlinks.")]
[GenerateDependencyProperty<string>(
    "ToolTipFormat",
    IsNullable = true,
    OnChanged = nameof(OnMessagePropertyChanged),
    PropertyXmlDocumentation = "Gets or sets the composite format string used for hyperlink tooltips.")]
[GenerateDependencyProperty<ICommand>(
    "ExpanderCommand",
    IsNullable = true,
    OnChanged = nameof(OnMessagePropertyChanged),
    PropertyXmlDocumentation = "Gets or sets the command that reveals a folded message list.")]
[GenerateDependencyProperty<string>(
    "ExpanderToolTip",
    IsNullable = true,
    OnChanged = nameof(OnMessagePropertyChanged),
    PropertyXmlDocumentation = "Gets or sets the tooltip of the control that reveals a folded message list.")]
[GenerateDependencyProperty<bool>(
    "IsObsolete",
    OnChanged = nameof(OnMessagePropertyChanged),
    PropertyXmlDocumentation = "Gets or sets a value indicating whether the message is rendered with a strikethrough.")]
public sealed partial class ReportMessageView : FlowDocumentScrollViewer
{
    private static readonly Thickness NoPadding = new(0);

    /// <inheritdoc />
    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == FontFamilyProperty || e.Property == FontSizeProperty || e.Property == FontWeightProperty)
            BuildDocument();
        else if (e.Property == ForegroundProperty)
            ApplyForeground();
    }

    private static void OnMessagePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        => ((ReportMessageView)sender).BuildDocument();

    /// <summary>
    /// Repeats the foreground of the view on its document, so text formatting never keeps the default color.
    /// </summary>
    /// <remarks>
    /// A document takes the default text color when it is built outside the visual tree and keeps it while
    /// the theme color arrives later. An own value on the document invalidates the formatted text and the
    /// message is drawn with the color the view actually has.
    /// </remarks>
    private void ApplyForeground()
    {
        if (Document is not null)
            Document.Foreground = Foreground;
    }

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

        // Attaching a document applies the defaults of the viewer, so the colors are set afterwards, like
        // the alignment below. The own value keeps the text on the view color while the theme changes and
        // when the document is built before the view joins the tree.
        ApplyForeground();

        // The viewer justifies its document, so the spaces between words stretch as the view changes
        // width. A message reads better left-aligned, with fixed spaces. Both values are set after the
        // document is attached because attaching it is what applies the justification.
        document.TextAlignment = TextAlignment.Left;
        paragraph.TextAlignment = TextAlignment.Left;
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
            ReportInlineType.Expander => new Hyperlink(run)
            {
                Command = ExpanderCommand,
                CommandParameter = part.Data,
                FontFamily = FontFamily,
                FontSize = FontSize,
                FontWeight = FontWeight,
                ToolTip = ExpanderToolTip
            },
            ReportInlineType.LineBreak => new LineBreak(),
            _ => throw new ArgumentOutOfRangeException(nameof(part), part.Type, "Unsupported report inline type.")
        };
    }
}
