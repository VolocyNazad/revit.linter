using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Revit.Linter.FixReportPresenter.ViewModels;

internal sealed partial class FixReportItemViewModel : ObservableObject
{
    public required string Code { get; set; }
    public required string Template { get; init; }
    public required Dictionary<string, object> Args { get; init; }
    public required Action<int> AccentElementDelegate { get; init; }
    public required DateTime Created { get; init; }

    public FlowDocument Message => CreateFlowDocument();

    private FlowDocument CreateFlowDocument()
    {
        FlowDocument flowDocument = new()
        {
            PagePadding = new(0)
        };
        Paragraph paragraph = new()
        {
            Margin = new(0)
        };

        var parts = ParseTemplate(Template, Args);

        CreateInlinesFromParts(parts, paragraph);

        flowDocument.Blocks.Add(paragraph);

        paragraph.FontSize = 13;
        paragraph.FontFamily = new("Segoe UI");
        paragraph.FontWeight = FontWeights.Regular;

        return flowDocument;
    }
    private static List<TextPart> ParseTemplate(string text, Dictionary<string, object> args)
    {
        List<TextPart> parts = [];

        int currentIndex = 0;
        int nextPlaceholderIndex;

        while ((nextPlaceholderIndex = text.IndexOf('{', currentIndex)) != -1)
        {
            int endPlaceholderIndex = text.IndexOf('}', nextPlaceholderIndex);

            if (endPlaceholderIndex == -1) break;

            if (nextPlaceholderIndex > currentIndex)
            {
                string textBefore = text[currentIndex..nextPlaceholderIndex];
                if (!string.IsNullOrEmpty(textBefore))
                    parts.Add(new(InlineType.Run, textBefore));
            }

            string key = text.Substring(nextPlaceholderIndex + 1, endPlaceholderIndex - nextPlaceholderIndex - 1);

            if (args.TryGetValue(key, out object? value))
            {
                var valueParts = CreateTextPartsFromValue(value);
                parts.AddRange(valueParts);
            }
            else
                parts.Add(new(InlineType.Run, $"{key}"));

            currentIndex = endPlaceholderIndex + 1;
        }

        if (currentIndex < text.Length)
        {
            string remainingText = text[currentIndex..];
            if (!string.IsNullOrEmpty(remainingText))
                parts.Add(new(InlineType.Run, remainingText));
        }

        return parts;
    }
    private static IEnumerable<TextPart> CreateTextPartsFromValue(object value)
    {
        List<TextPart> parts = [];

        switch (value)
        {
            case IEnumerable<ElementId> elementIds:
                {
                    var idsList = elementIds.ToList();

                    if (!idsList.Any()) return parts;

                    for (int i = 0; i < idsList.Count; i++)
                    {
                        parts.Add(new(InlineType.Hyperlink, idsList[i].ToString()));

                        if (i < idsList.Count - 1) parts.Add(new(InlineType.Run, ", "));
                    }

                    break;
                }

            case ElementId elementId:
                parts.Add(new(InlineType.Hyperlink, elementId.ToString()));
                break;
            default:
                parts.Add(new(InlineType.Run, value?.ToString() ?? string.Empty));
                break;
        }

        return parts;
    }
    private void CreateInlinesFromParts(List<TextPart> parts, Paragraph paragraph)
    {
        foreach (var part in parts)
        {
            switch (part.Type)
            {
                case InlineType.Run:
                    Run run = new(part.Text);
                    paragraph.Inlines.Add(run);
                    break;

                case InlineType.Hyperlink:
                    var hyperlink = CreateHyperlink(part.Text);
                    paragraph.Inlines.Add(hyperlink);
                    break;
            }
        }
    }
    private Hyperlink CreateHyperlink(string elementId)
    {
        var hyperlink = new Hyperlink(new Run(elementId))
        {
            Tag = elementId,
            Foreground = Brushes.Blue,
            TextDecorations = TextDecorations.Underline,
            Cursor = System.Windows.Input.Cursors.Hand,
            ToolTip = $"Показать элемент {elementId} в модели",
        };

        hyperlink.Click += (s, e) =>
        {
            OnElementNavigationRequested(int.Parse((string)hyperlink.Tag));
            e.Handled = true;
        };

        return hyperlink;
    }

    private void OnElementNavigationRequested(int elementId) => AccentElementDelegate(elementId);

    enum InlineType
    {
        Run,
        Hyperlink
    }
    sealed record TextPart(InlineType Type, string Text);
}
