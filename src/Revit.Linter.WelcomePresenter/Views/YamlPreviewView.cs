using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Revit.Linter.WelcomePresenter.Views;

/// <summary>Displays a compact, read-only YAML sample with theme-aware syntax colors.</summary>
internal sealed class YamlPreviewView : FlowDocumentScrollViewer
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(YamlPreviewView),
        new FrameworkPropertyMetadata(string.Empty, OnTextChanged));

    public YamlPreviewView()
    {
        IsSelectionEnabled = true;
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
        Document = CreateDocument();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    private static void OnTextChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        YamlPreviewView view = (YamlPreviewView)sender;
        view.Document = CreateDocument(args.NewValue as string);
    }

    private static FlowDocument CreateDocument(string? text = null)
    {
        Paragraph paragraph = new() { Margin = new(0) };
        string[] lines = (text ?? string.Empty).Replace("\r\n", "\n").Split('\n');

        for (int index = 0; index < lines.Length; index++)
        {
            AppendLine(paragraph, lines[index]);
            if (index < lines.Length - 1) paragraph.Inlines.Add(new LineBreak());
        }

        return new FlowDocument(paragraph)
        {
            FontFamily = new("Consolas"),
            FontSize = 12,
            PageWidth = 650,
            PagePadding = new(0),
        };
    }

    private static void AppendLine(Paragraph paragraph, string line)
    {
        int commentIndex = FindUnquoted(line, '#');
        string content = commentIndex >= 0 ? line.Substring(0, commentIndex) : line;
        int separatorIndex = FindUnquoted(content, ':');

        if (separatorIndex < 0)
        {
            paragraph.Inlines.Add(new Run(content));
        }
        else
        {
            int keyStart = content.TakeWhile(character => char.IsWhiteSpace(character) || character == '-').Count();
            paragraph.Inlines.Add(new Run(content.Substring(0, keyStart)));
            paragraph.Inlines.Add(CreateColoredRun(
                content.Substring(keyStart, separatorIndex - keyStart), "LinterAccentBrush"));
            paragraph.Inlines.Add(new Run(":"));
            AppendValue(paragraph, content.Substring(separatorIndex + 1));
        }

        if (commentIndex >= 0)
            paragraph.Inlines.Add(CreateColoredRun(
                line.Substring(commentIndex), "LinterSecondaryForegroundBrush"));
    }

    private static void AppendValue(Paragraph paragraph, string value)
    {
        int firstQuoteIndex = value.IndexOfAny(['\'', '"']);
        if (firstQuoteIndex < 0)
        {
            paragraph.Inlines.Add(new Run(value));
            return;
        }

        paragraph.Inlines.Add(new Run(value.Substring(0, firstQuoteIndex)));
        paragraph.Inlines.Add(CreateColoredRun(
            value.Substring(firstQuoteIndex), "LinterSeverityWarningBrush"));
    }

    private static Run CreateColoredRun(string text, string resourceKey)
    {
        Run run = new(text);
        run.SetResourceReference(TextElement.ForegroundProperty, resourceKey);
        return run;
    }

    private static int FindUnquoted(string text, char character)
    {
        char quote = '\0';
        for (int index = 0; index < text.Length; index++)
        {
            if (quote == '\0' && text[index] is '\'' or '"')
                quote = text[index];
            else if (quote == text[index])
                quote = '\0';

            if (quote == '\0' && text[index] == character) return index;
        }

        return -1;
    }
}
