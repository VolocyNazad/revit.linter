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

        if (File.Exists(location))
            location = Path.GetDirectoryName(location);

        if (!Directory.Exists(location)) return;

        string? target = Directory.GetFiles(location)
            .Where(i => i.Contains(".dll"))
            .Where(i => !i.Contains(".config"))
            .FirstOrDefault(i => Path.GetFileNameWithoutExtension(i) == targetName);

        if (target != null) Assembly.LoadFrom(target);
    }
}