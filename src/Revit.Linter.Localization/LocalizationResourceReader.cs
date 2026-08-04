using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace Revit.Linter.Localization;

public static class LocalizationResourceReader
{
    private static readonly Assembly ResourceAssembly = typeof(LocalizationAssembly).Assembly;
    private static readonly ConcurrentDictionary<(string BaseName, string Culture), IReadOnlyDictionary<string, string>> Resources = new();

    public static string GetString(string baseName, string key)
    {
        for (CultureInfo culture = CultureInfo.CurrentUICulture; !string.IsNullOrEmpty(culture.Name); culture = culture.Parent)
        {
            if (GetResources(baseName, culture).TryGetValue(key, out string? value)) return value;
        }

        return GetResources(baseName, CultureInfo.InvariantCulture).TryGetValue(key, out string? fallback)
            ? fallback
            : key;
    }

    private static IReadOnlyDictionary<string, string> GetResources(string baseName, CultureInfo culture) =>
        Resources.GetOrAdd((baseName, culture.Name), static item => ReadResources(item.BaseName, item.Culture));

    private static IReadOnlyDictionary<string, string> ReadResources(string baseName, string cultureName)
    {
        Assembly assembly = ResourceAssembly;
        if (!string.IsNullOrEmpty(cultureName))
        {
            try
            {
                assembly = ResourceAssembly.GetSatelliteAssembly(CultureInfo.GetCultureInfo(cultureName));
            }
            catch (FileNotFoundException)
            {
                return new Dictionary<string, string>();
            }
        }

        using Stream? stream = assembly.GetManifestResourceStream($"{baseName}.resources");
        if (stream is null) return new Dictionary<string, string>();

        using ResourceReader reader = new(stream);
        Dictionary<string, string> values = new(StringComparer.Ordinal);
        foreach (System.Collections.DictionaryEntry entry in reader)
        {
            if (entry.Key is string key && entry.Value is string value) values[key] = value;
        }
        return values;
    }
}
