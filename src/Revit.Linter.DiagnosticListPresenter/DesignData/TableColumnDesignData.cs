namespace Revit.Linter.DiagnosticListPresenter.DesignData;

/// <summary>Provides one sample column of the column visibility popup.</summary>
internal sealed class TableColumnDesignData(string title)
{
    /// <summary>Gets the localized column title.</summary>
    public string Title { get; } = title;

    /// <summary>Gets or sets whether the table shows the column.</summary>
    public bool IsVisible { get; set; } = true;
}
