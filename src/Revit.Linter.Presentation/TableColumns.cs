using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Revit.Linter.Presentation.ViewModels;
using VolocyNazad.Wpf.DependencyProperties;

namespace Revit.Linter.Presentation;

/// <summary>
/// Applies the remembered column visibility to a table.
/// </summary>
/// <remarks>
/// Set <see cref="SourceProperty"/> on the <see cref="DataGrid"/> to its pane view model and
/// <see cref="KeyProperty"/> on each data column. A column without a key, such as an action icon column,
/// is never hidden. When the stretching column is hidden, the next visible data column takes the leftover
/// space until the stretching column returns. Hiding a column does not change the current sort.
/// </remarks>
[GenerateAttachedDependencyProperty<TableColumnsViewModel>(
    "Source",
    IsNullable = true,
    OnChanged = nameof(OnSourceChanged),
    XmlDocumentation = "Identifies the column visibility of a table.",
    GetterXmlDocumentation = "<summary>Gets the column visibility applied to a table.</summary>",
    SetterXmlDocumentation = "<summary>Sets the column visibility applied to a table.</summary>")]
[GenerateAttachedDependencyProperty<string>(
    "Key",
    IsNullable = true,
    XmlDocumentation = "Identifies the stored key of a hideable table column.",
    GetterXmlDocumentation = "<summary>Gets the stored key of a table column, or null for a column that is never hidden.</summary>",
    SetterXmlDocumentation = "<summary>Sets the stored key of a table column; a column without a key is never hidden.</summary>")]
public static partial class TableColumns
{
    private static readonly ConditionalWeakTable<DataGrid, TableState> States = new();

    private static void OnSourceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is not DataGrid grid) return;

        TableState state = States.GetValue(grid, _ => new TableState());
        state.Detach();

        if (args.NewValue is TableColumnsViewModel source)
        {
            state.Attach(source, grid);
        }

        Apply(grid);
    }

    private static void Grid_Loaded(object sender, RoutedEventArgs args)
    {
        if (sender is DataGrid grid) Apply(grid);
    }

    private static void Apply(DataGrid grid)
    {
        if (!States.TryGetValue(grid, out TableState? state) || state.Source is null) return;

        // Restore the column that took the leftover space before deciding again from the user's widths.
        if (state.Promoted is { } promoted)
        {
            promoted.Width = state.PromotedWidth;
            state.Promoted = null;
        }

        DataGridColumn[] keyed = grid.Columns
            .Where(column => !string.IsNullOrEmpty(GetKey(column)))
            .ToArray();
        if (keyed.Length == 0) return;

        foreach (DataGridColumn column in keyed)
        {
            column.Visibility = state.Source.IsVisible(GetKey(column)!) ? Visibility.Visible : Visibility.Collapsed;
        }

        DataGridColumn[] visible = keyed.Where(column => column.Visibility == Visibility.Visible).ToArray();
        if (visible.Length == 0 || visible.Any(column => column.Width.IsStar)) return;

        DataGridColumn? hiddenStretch = keyed.FirstOrDefault(column =>
            column.Visibility != Visibility.Visible && column.Width.IsStar);
        DataGridColumn target = (hiddenStretch is null ? null : NextVisible(keyed, hiddenStretch)) ?? visible[visible.Length - 1];

        state.Promoted = target;
        state.PromotedWidth = target.Width;
        target.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
    }

    private static DataGridColumn? NextVisible(DataGridColumn[] columns, DataGridColumn reference)
    {
        int index = Array.IndexOf(columns, reference);
        for (int next = index + 1; next < columns.Length; next++)
        {
            if (columns[next].Visibility == Visibility.Visible) return columns[next];
        }

        return null;
    }

    private sealed class TableState
    {
        private EventHandler? _changed;

        public TableColumnsViewModel? Source { get; private set; }

        public DataGridColumn? Promoted { get; set; }

        public DataGridLength PromotedWidth { get; set; }

        public void Attach(TableColumnsViewModel source, DataGrid grid)
        {
            Source = source;
            _changed = (_, _) => Apply(grid);
            source.VisibilityChanged += _changed;
            grid.Loaded -= Grid_Loaded;
            grid.Loaded += Grid_Loaded;
        }

        public void Detach()
        {
            if (Source is not null && _changed is not null)
                Source.VisibilityChanged -= _changed;

            // The promoted column belongs to the table, not to the visibility model: give its own width
            // back even when the table is detached from the model mid-session.
            if (Promoted is { } promoted) promoted.Width = PromotedWidth;

            Source = null;
            _changed = null;
            Promoted = null;
        }
    }
}
