using System.IO;
using System.Reflection;

namespace Revit.Linter.Infrastructure.Services;

internal static class AssemblyLoadService
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

        // Revit does not probe the add-in directory reliably, so these WPF dependencies must be
        // loaded from the exact deployment path before their types are requested.
#pragma warning disable S3885
        if (!isLoaded) Assembly.LoadFrom(target);
#pragma warning restore S3885
    }
}
