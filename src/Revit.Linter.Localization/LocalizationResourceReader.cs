using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace Revit.Linter.Localization;

/// <summary>
/// Reads localized strings from resources embedded in the localization assembly.
/// </summary>
public static class LocalizationResourceReader
{
    private static readonly Assembly ResourceAssembly = typeof(LocalizationAssembly).Assembly;
    private static readonly ConcurrentDictionary<(string BaseName, string Culture), IReadOnlyDictionary<string, string>> Resources = new();

    /// <summary>
    /// Gets a localized string using the current UI culture and its parent cultures.
    /// </summary>
    /// <param name="baseName">The fully qualified base name of the resource set.</param>
    /// <param name="key">The resource key.</param>
    /// <returns>The localized value, or <paramref name="key"/> when no resource exists.</returns>
    public static string GetString(string baseName, string key)
    {
        _ = TryGetString(baseName, key, out string value);
        return value;
    }

    /// <summary>
    /// Attempts to get a localized string using the current UI culture and its parent cultures.
    /// </summary>
    /// <param name="baseName">The fully qualified base name of the resource set.</param>
    /// <param name="key">The resource key.</param>
    /// <param name="value">The localized value when found; otherwise, <paramref name="key"/>.</param>
    /// <returns><see langword="true"/> when a resource was found; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetString(string baseName, string key, out string value)
    {
        for (CultureInfo culture = CultureInfo.CurrentUICulture; !string.IsNullOrEmpty(culture.Name); culture = culture.Parent)
        {
            if (GetResources(baseName, culture).TryGetValue(key, out string? localizedValue))
            {
                value = localizedValue;
                return true;
            }
        }

        if (GetResources(baseName, CultureInfo.InvariantCulture).TryGetValue(key, out string? fallback))
        {
            value = fallback;
            return true;
        }

        value = key;
        return false;
    }

    /// <summary>
    /// Enumerates the distinct localized strings in a resource set for the current UI culture.
    /// </summary>
    /// <param name="baseName">The fully qualified base name of the resource set.</param>
    /// <param name="includeParentCultures">
    /// A value indicating whether strings from parent cultures are included before invariant-culture fallbacks.
    /// </param>
    /// <returns>The localized key-value pairs, with values from more specific cultures taking precedence.</returns>
    public static IEnumerable<KeyValuePair<string, string>> GetAllStrings(string baseName, bool includeParentCultures)
    {
        HashSet<string> keys = new(StringComparer.Ordinal);
        CultureInfo culture = CultureInfo.CurrentUICulture;
        while (!string.IsNullOrEmpty(culture.Name))
        {
            foreach (KeyValuePair<string, string> resource in GetResources(baseName, culture))
            {
                if (keys.Add(resource.Key)) yield return resource;
            }

            if (!includeParentCultures) break;
            culture = culture.Parent;
        }

        foreach (KeyValuePair<string, string> resource in GetResources(baseName, CultureInfo.InvariantCulture))
        {
            if (keys.Add(resource.Key)) yield return resource;
        }
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
