using CommunityToolkit.Mvvm.Input;

namespace Revit.Linter.DiagnosticListPresenter.DesignData;

/// <summary>Provides the sample column visibility popup of <see cref="Views.DiagnosticListView"/> in the XAML designer.</summary>
internal sealed partial class TableColumnsDesignData(IEnumerable<TableColumnDesignData> columns)
{
    /// <summary>Gets the sample data columns in display order.</summary>
    public IReadOnlyList<TableColumnDesignData> Columns { get; } = [.. columns];

    /// <summary>Gets the command that shows every column.</summary>
    [RelayCommand]
    private void ShowAll()
    {
        foreach (TableColumnDesignData column in Columns) column.IsVisible = true;
    }
}
