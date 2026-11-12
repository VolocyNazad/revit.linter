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
