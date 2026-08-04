namespace Revit.Linter.DialogPresenter.Abstractions;

public interface IDialog
{
    Task Show(DialogRequest request, CancellationToken cancellationToken = default);

    Task Show(object content, CancellationToken cancellationToken = default);
}
