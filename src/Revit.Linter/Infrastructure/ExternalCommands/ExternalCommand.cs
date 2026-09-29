using Toolkit.AssemblyResolver;
#if NET
using System.Runtime.Loader;
#endif

namespace Revit.Linter.Infrastructure.ExternalCommands;

/// <summary>
/// Provides the Revit external-command entry point and exposes the active command context to derived commands.
/// </summary>
public abstract class ExternalCommand : IExternalCommand
{
    /// <summary>Gets the Revit UI application for the active command.</summary>
    public UIApplication Application { get; private set; } = null!;

    /// <summary>Gets the active Revit view for the command.</summary>
    public View View { get; private set; } = null!;

    /// <summary>Gets the journal data supplied by Revit.</summary>
    public IDictionary<string, string> JournalData { get; private set; } = null!;

    /// <summary>Gets or sets the error message returned to Revit when the command fails.</summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>Gets the element set used to report command failures to Revit.</summary>
    public ElementSet ElementSet { get; private set; } = null!;

    /// <summary>Gets or sets the command result returned to Revit.</summary>
    public Result Result { get; set; } = Result.Succeeded;

    /// <inheritdoc />
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

    /// <summary>Executes the command after the active Revit context has been initialized.</summary>
    public abstract void Execute();
}
