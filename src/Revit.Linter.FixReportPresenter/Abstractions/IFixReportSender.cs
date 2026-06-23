namespace Revit.Linter.FixReportPresenter.Abstractions;

public interface IFixReportSender
{
    Task Send(IEnumerable<IFixReportInfo> content, CancellationToken cancellationToken = default);
}