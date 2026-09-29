using Toolkit.AssemblyResolver;
#if NET
using System.Runtime.Loader;
#endif

namespace Revit.Linter.Infrastructure.ExternalApplications;

/// <summary>
/// Provides the Revit application entry-point lifecycle and assembly-resolution scope.
/// </summary>
public abstract class ExternalApplication : IExternalApplication
{
    /// <summary>Gets the controlled Revit application for the active lifecycle callback.</summary>
    public UIControlledApplication Application { get; private set; } = null!;

    /// <summary>Gets or sets the result returned to Revit after the current lifecycle callback.</summary>
    public Result Result { get; set; } = Result.Succeeded;

    /// <inheritdoc />
    public Result OnStartup(UIControlledApplication application)
    {
        Application = application;

        var currentType = GetType();
#if NET
        if (AssemblyLoadContext.GetLoadContext(currentType.Assembly) == AssemblyLoadContext.Default)
        {
            using (ResolveHelper.BeginAssemblyResolveScope(currentType))
            {
                OnStartup();
            }
        }
        else
        {
            OnStartup();
        }
#else
        using (ResolveHelper.BeginAssemblyResolveScope(currentType))
        {
            OnStartup();
        }
#endif

        return Result;
    }
    /// <summary>Runs add-in-specific startup after the Revit application context is initialized.</summary>
    public abstract void OnStartup();

    /// <inheritdoc />
    public Result OnShutdown(UIControlledApplication application)
    {
        var currentType = GetType();
#if NET
        if (AssemblyLoadContext.GetLoadContext(currentType.Assembly) == AssemblyLoadContext.Default)
        {
            using (ResolveHelper.BeginAssemblyResolveScope(currentType))
            {
                OnShutdown();
            }
        }
        else
        {
            OnShutdown();
        }
#else
        using (ResolveHelper.BeginAssemblyResolveScope(currentType))
        {
            OnShutdown();
        }
#endif

        return Result.Succeeded;
    }

    /// <summary>Runs add-in-specific shutdown before the application context is released.</summary>
    public virtual void OnShutdown() { }
}
