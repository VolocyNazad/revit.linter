using CommunityToolkit.Mvvm.ComponentModel;
using Revit.Linter.FixReportPresenter.Abstractions;
using System.Collections.ObjectModel;

namespace Revit.Linter.FixReportPresenter.ViewModels;

[XamlConstructor]
internal sealed partial class FixReportViewModel : ObservableObject, IFixReportSender
{
    [ObservableProperty]
    public partial ObservableCollection<FixReportItemViewModel> Collection { get; private set; }  = null!;

    public Task Send(IEnumerable<IFixReportInfo> content, CancellationToken cancellationToken = default)
    {
        Collection = new(content.Select(i => new FixReportItemViewModel() { Message = i.Message }).ToList());

        return Task.CompletedTask;
    }
}
