using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Revit.Linter.ReportMessaging.Tests;

public sealed class ReportMessageViewTests
{
    [Fact]
    public void Parts_are_rendered_with_view_typography()
    {
        RunInSta(() =>
        {
            FontFamily fontFamily = new("Arial");
            ReportMessageView view = new()
            {
                FontFamily = fontFamily,
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                Parts = [new(ReportInlineType.Text, "Message")]
            };

            Paragraph paragraph = Assert.IsType<Paragraph>(view.Document.Blocks.FirstBlock);
            Run run = Assert.IsType<Run>(paragraph.Inlines.FirstInline);

            Assert.Equal("Message", run.Text);
            Assert.Equal(fontFamily, run.FontFamily);
            Assert.Equal(17, run.FontSize);
            Assert.Equal(FontWeights.Bold, run.FontWeight);
        });
    }

    [Fact]
    public void Link_uses_command_parameter_and_tooltip()
    {
        RunInSta(() =>
        {
            object parameter = new();
            TestCommand command = new();
            ReportMessageView view = new()
            {
                LinkCommand = command,
                ToolTipFormat = "Show {0}",
                Parts = [new(ReportInlineType.Hyperlink, "42", parameter)]
            };

            Paragraph paragraph = Assert.IsType<Paragraph>(view.Document.Blocks.FirstBlock);
            Hyperlink hyperlink = Assert.IsType<Hyperlink>(paragraph.Inlines.FirstInline);

            Assert.Same(command, hyperlink.Command);
            Assert.Same(parameter, hyperlink.CommandParameter);
            Assert.Equal("Show 42", hyperlink.ToolTip);
            Assert.Equal("42", Assert.IsType<Run>(hyperlink.Inlines.FirstInline).Text);
        });
    }

    [Fact]
    public void Message_is_left_aligned()
    {
        RunInSta(() =>
        {
            ReportMessageView view = new()
            {
                Parts = [new(ReportInlineType.Text, "Message")]
            };

            Paragraph paragraph = Assert.IsType<Paragraph>(view.Document.Blocks.FirstBlock);

            Assert.Equal(TextAlignment.Left, view.Document.TextAlignment);
            Assert.Equal(TextAlignment.Left, paragraph.TextAlignment);
        });
    }

    [Fact]
    public void Obsolete_message_is_struck_through()
    {
        RunInSta(() =>
        {
            ReportMessageView view = new()
            {
                IsObsolete = true,
                Parts = [new(ReportInlineType.Text, "Obsolete")]
            };

            Paragraph paragraph = Assert.IsType<Paragraph>(view.Document.Blocks.FirstBlock);

            Assert.NotEmpty(paragraph.TextDecorations);
        });
    }

    [Fact]
    public void Document_carries_the_view_foreground()
    {
        RunInSta(() =>
        {
            SolidColorBrush foreground = new(Colors.White);
            ReportMessageView view = new()
            {
                Foreground = foreground,
                Parts = [new(ReportInlineType.Text, "Message")]
            };

            Paragraph paragraph = Assert.IsType<Paragraph>(view.Document.Blocks.FirstBlock);
            Run run = Assert.IsType<Run>(paragraph.Inlines.FirstInline);

            Assert.Same(foreground, view.Document.Foreground);
            Assert.Same(foreground, run.Foreground);
            Assert.NotEqual(DependencyProperty.UnsetValue, view.Document.ReadLocalValue(TextElement.ForegroundProperty));
        });
    }

    [Fact]
    public void Document_takes_the_foreground_it_inherits_when_the_view_joins_the_tree()
    {
        RunInSta(() =>
        {
            ReportMessageView view = new()
            {
                Parts = [new(ReportInlineType.Text, "Message")]
            };
            Grid host = new();
            host.SetValue(TextElement.ForegroundProperty, Brushes.White);

            host.Children.Add(view);

            Assert.Same(Brushes.White, view.Foreground);
            Assert.Same(Brushes.White, view.Document.Foreground);
            Assert.NotEqual(DependencyProperty.UnsetValue, view.Document.ReadLocalValue(TextElement.ForegroundProperty));
        });
    }

    private static void RunInSta(Action action)
    {
        Exception? exception = null;
        Thread thread = new(() =>
        {
            try
            {
                action();
            }
            catch (Exception caught)
            {
                exception = caught;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception is not null)
            ExceptionDispatchInfo.Capture(exception).Throw();
    }

    private sealed class TestCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add { /* The test command has a constant CanExecute result. */ }
            remove { /* No handlers are retained because the result never changes. */ }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) { }
    }
}
