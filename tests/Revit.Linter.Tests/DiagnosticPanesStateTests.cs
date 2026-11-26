using Revit.Linter.Infrastructure.Services;

namespace Revit.Linter.Tests;

public sealed class DiagnosticPanesStateTests
{
    private static readonly bool[] Open = [true, true, true, true];
    private static readonly bool[] Closed = [false, false, false, false];

    [Fact]
    public void Remembered_panes_survive_the_state_without_documents()
    {
        var state = new DiagnosticPanesState();
        state.Remember(Open, documentIsOpen: true);
        state.Remember(Closed, documentIsOpen: false);

        IReadOnlyList<bool>? shown = state.Shown;

        Assert.NotNull(shown);
        Assert.Equal(Open, shown);
    }

    [Fact]
    public void Panes_hidden_while_a_document_is_open_are_remembered_as_hidden()
    {
        var state = new DiagnosticPanesState();
        state.Remember(Open, documentIsOpen: true);
        state.Remember(Closed, documentIsOpen: true);

        IReadOnlyList<bool>? shown = state.Shown;

        Assert.NotNull(shown);
        Assert.Equal(Closed, shown);
    }

    [Fact]
    public void Panes_opened_without_a_document_are_remembered()
    {
        var state = new DiagnosticPanesState();
        state.Remember(Closed, documentIsOpen: false);
        state.Remember(Open, documentIsOpen: false);

        IReadOnlyList<bool>? shown = state.Shown;

        Assert.NotNull(shown);
        Assert.Equal(Open, shown);
    }

    [Fact]
    public void Nothing_is_remembered_before_the_first_switch()
    {
        var state = new DiagnosticPanesState();

        Assert.Null(state.Shown);
    }
}
