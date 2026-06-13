using Toolkit.AssemblyResolver;
#if NET
using System.Runtime.Loader;
#endif

namespace Revit.Linter.Infrastructure.ExternalCommands;

public abstract class ExternalCommand : IExternalCommand
{
    public UIApplication Application { get; private set; } = null!;
    public View View { get; private set; } = null!;
    public IDictionary<string, string> JournalData { get; private set; } = null!;
    public string ErrorMessage { get; set; } = string.Empty;
    public ElementSet ElementSet { get; private set; } = null!;
    public Result Result { get; set; } = Result.Succeeded;


    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        ElementSet = elements;
        ErrorMessage = message;
        Application = commandData.Application;
        View = commandData.View;
        JournalData = commandData.JournalData;

        var currentType = GetType();
#if NET
        if (AssemblyLoadContext.GetLoadContext(currentType.Assembly) == AssemblyLoadContext.Default)
        {
            using (ResolveHelper.BeginAssemblyResolveScope(currentType))
            {
                Execute();
            }
        }
        else
        {
            Execute();
        }
#else
        using (ResolveHelper.BeginAssemblyResolveScope(currentType))
        {
            Execute();
        }
#endif

        message = ErrorMessage;
        return Result;
    }

    public abstract void Execute();
}