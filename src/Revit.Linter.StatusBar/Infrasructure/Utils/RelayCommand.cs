using System.Windows.Input;

namespace Revit.Linter.StatusBar.Infrasructure.Utils;

internal sealed class RelayCommand(Action action) : ICommand
{
    private readonly Action action = action;

#pragma warning disable CS0067
    public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067

    public bool CanExecute(object parameter) => true;
    public void Execute(object parameter) => action?.Invoke();
}
