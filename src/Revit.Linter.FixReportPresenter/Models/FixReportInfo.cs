using Revit.Linter.FixReportPresenter.Abstractions;

namespace Revit.Linter.FixReportPresenter.Models
{
    public sealed class FixReportInfo : IFixReportInfo
    {
        public required string Message { get; init; }
    }
}
