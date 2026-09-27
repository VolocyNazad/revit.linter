using Microsoft.Extensions.Localization;
using Revit.Linter.Localization;
using System.Globalization;

namespace Revit.Linter;

internal sealed class GlobalStringLocalizer : IStringLocalizer<GlobalLocalizations>
{
    private const string ResourceBaseName = "Revit.Linter.Localization.GlobalLocalizations";

    public LocalizedString this[string name] => CreateLocalizedString(name);

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            LocalizedString localizedString = CreateLocalizedString(name);
            string value = string.Format(CultureInfo.CurrentCulture, localizedString.Value, arguments);
            return new LocalizedString(name, value, localizedString.ResourceNotFound, ResourceBaseName);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
        LocalizationResourceReader.GetAllStrings(ResourceBaseName, includeParentCultures)
            .Select(resource => new LocalizedString(resource.Key, resource.Value, false, ResourceBaseName));

    private static LocalizedString CreateLocalizedString(string name)
    {
        bool found = LocalizationResourceReader.TryGetString(ResourceBaseName, name, out string value);
        return new LocalizedString(name, value, !found, ResourceBaseName);
    }
}
