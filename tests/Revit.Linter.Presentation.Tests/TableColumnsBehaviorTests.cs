using System.Runtime.ExceptionServices;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Revit.Linter.Presentation.Models;
using Revit.Linter.Presentation.Services;
using Revit.Linter.Presentation.ViewModels;
using Toolkit.ValueStore.Abstractions;

namespace Revit.Linter.Presentation.Tests;

/// <summary>
/// Covers the attached properties that apply the saved column state to a real table: a table without
/// saved widths keeps the widths it declares, a saved width is applied, and a finished header drag stores
/// the width the user chose.
/// </summary>
public sealed class TableColumnsBehaviorTests
{
    [Fact]
    public void A_table_opens_with_the_saved_width_and_stores_a_dragged_width()
    {
        RunInSta(() =>
        {
            TestStore store = new()
            {
                CurrentValue = new TableColumnsSettings
                {
                    Widths =
                    [
                        new TableColumnWidth
                        {
                            Table = TableColumnIds.Diagnostics,
                            Column = TableColumnKeys.Code,
                            Pixels = 180,
                        },
                    ],
                },
            };
            using TableColumnsService service = new(store);
            using TableColumnsViewModel viewModel = new(
                TableColumnIds.Diagnostics,
                service,
                [new(TableColumnKeys.Code, () => "Code")]);

            DataGrid grid = new() { AutoGenerateColumns = false };
            DataGridTemplateColumn code = new() { Width = DataGridLength.Auto };
            TableColumns.SetKey(code, TableColumnKeys.Code);
            DataGridTemplateColumn description = new()
            {
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
            };
            TableColumns.SetKey(description, TableColumnKeys.Description);
            grid.Columns.Add(code);
            grid.Columns.Add(description);

            TableColumns.SetSource(grid, viewModel);

            Assert.True(code.Width.IsAbsolute);
            Assert.Equal(180d, code.Width.Value);

            code.Width = new DataGridLength(260);
            grid.RaiseEvent(new DragCompletedEventArgs(0, 0, canceled: false)
            {
                RoutedEvent = Thumb.DragCompletedEvent,
            });

            Assert.Equal(260d, service.GetWidth(TableColumnIds.Diagnostics, TableColumnKeys.Code));
        });
    }

    [Fact]
    public void A_table_without_saved_widths_keeps_the_widths_it_declares()
    {
        RunInSta(() =>
        {
            using TableColumnsService service = new(new TestStore());
            using TableColumnsViewModel viewModel = new(
                TableColumnIds.Diagnostics,
                service,
                [new(TableColumnKeys.Code, () => "Code")]);

            DataGrid grid = new() { AutoGenerateColumns = false };
            DataGridTemplateColumn code = new() { Width = DataGridLength.Auto };
            TableColumns.SetKey(code, TableColumnKeys.Code);
            DataGridTemplateColumn description = new()
            {
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
            };
            TableColumns.SetKey(description, TableColumnKeys.Description);
            grid.Columns.Add(code);
            grid.Columns.Add(description);

            TableColumns.SetSource(grid, viewModel);

            Assert.True(code.Width.IsAuto);
            Assert.True(description.Width.IsStar);
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

    private sealed class TestStore : IValueStore<TableColumnsSettings>
    {
        private readonly List<Action<TableColumnsSettings>> _listeners = [];

        public TableColumnsSettings CurrentValue { get; set; } = new();

        public IDisposable OnChange(Action<TableColumnsSettings> listener)
        {
            _listeners.Add(listener);
            return new Subscription(_listeners, listener);
        }

        public void Update(Action<TableColumnsSettings> change)
        {
            change(CurrentValue);
            foreach (Action<TableColumnsSettings> listener in _listeners) listener(CurrentValue);
        }

        private sealed class Subscription(List<Action<TableColumnsSettings>> listeners, Action<TableColumnsSettings> listener)
            : IDisposable
        {
            public void Dispose() => listeners.Remove(listener);
        }
    }
}
