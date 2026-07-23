using System.IO;
using System.Reflection;

namespace Revit.Linter.Infrastructure.Services;

public static class AssemblyLoadService
{
    private static readonly IEnumerable<string> Troubled = [
            "Microsoft.Xaml.Behaviors",
            "MaterialDesignThemes.Wpf",
            "MaterialDesignColors",
        ];
    public static void LoadAssemblies()
    {
        foreach (var name in Troubled)
        {
            LoadAssembly(name);
        }
    }
    private static void LoadAssembly(string targetName)
    {
        string? location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        if (!Directory.Exists(location)) return;

        string? target = Directory.GetFiles(location)
            .Where(i => string.Equals(Path.GetExtension(i), ".dll", StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault(i => Path.GetFileNameWithoutExtension(i) == targetName);

        if (target == null) return;

        AssemblyName targetAssemblyName = AssemblyName.GetAssemblyName(target);
        bool isLoaded = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetName())
            .Any(assemblyName => AssemblyName.ReferenceMatchesDefinition(assemblyName, targetAssemblyName));

        if (!isLoaded) Assembly.LoadFrom(target);
    }
}
