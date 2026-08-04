using Autodesk.Revit.ApplicationServices;
using System.Globalization;

namespace Revit.Linter.SerilogEnrichers.Utils;

internal static class LanguageTypeExtensions
{
    public static CultureInfo GetCultureInfo(this LanguageType languageType) => (int)languageType switch
    {
        0 => CultureInfo.GetCultureInfo("en-US"),
        1 => CultureInfo.GetCultureInfo("de-DE"),
        2 => CultureInfo.GetCultureInfo("es"),
        3 => CultureInfo.GetCultureInfo("fr-FR"),
        4 => CultureInfo.GetCultureInfo("it-IT"),
        5 => CultureInfo.GetCultureInfo("nl-NL"),
        6 => CultureInfo.GetCultureInfo("zh-CN"),
        7 => CultureInfo.GetCultureInfo("zh-Hant"),
        8 => CultureInfo.GetCultureInfo("ja-JP"),
        9 => CultureInfo.GetCultureInfo("ko-KR"),
        10 => CultureInfo.GetCultureInfo("ru-RU"),
        11 => CultureInfo.GetCultureInfo("cs-CZ"),
        12 => CultureInfo.GetCultureInfo("pl-PL"),
        13 => CultureInfo.GetCultureInfo("hu-HU"),
        14 => CultureInfo.GetCultureInfo("pt-BR"),
        15 => CultureInfo.GetCultureInfo("en-GB"),
        _ => CultureInfo.CurrentCulture,
    };
}