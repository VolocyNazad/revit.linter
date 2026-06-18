namespace Revit.Linter.DialogPresenter.Abstractions;

public interface IDialog
{
    Task Show(object content, CancellationToken cancellationToken = default);
}
