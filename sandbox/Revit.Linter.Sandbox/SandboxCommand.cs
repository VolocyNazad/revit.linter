using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Diagnostics;

namespace Revit.Linter.Sandbox;

[Transaction(TransactionMode.Manual)]
public class SandboxCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
#if DEBUG
        if (!Debugger.IsAttached)
            Debugger.Launch();
#endif

        using var dialog = new TaskDialog("Revit.Linter.Sandbox");
        dialog.MainInstruction = "Выполнена команда входа";
        dialog.MainContent = $"Пользователь: {Environment.UserName}";
        dialog.Show();

        return Result.Succeeded;
    }
}
