using Toolkit.AssemblyResolver;
#if NET
using System.Runtime.Loader;
#endif

namespace Revit.Linter.Infrastructure.ExternalApplications;

public abstract class ExternalApplication : IExternalApplication
{
    public UIControlledApplication Application { get; private set; } = null!;
    public Result Result { get; set; } = Result.Succeeded;

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
    public abstract void OnStartup();

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

    public virtual void OnShutdown() { }
}