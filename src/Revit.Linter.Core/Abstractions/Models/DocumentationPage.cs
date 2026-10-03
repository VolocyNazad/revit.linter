using System.Globalization;

namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>
/// Identifies a page of the user documentation by its English and Russian Wiki page names.
/// </summary>
/// <param name="EnglishPageName">The Wiki page name of the English page, for example <c>Quick start</c>.</param>
/// <param name="RussianPageName">The Wiki page name of the Russian counterpart.</param>
/// <remarks>
/// Page names are the Markdown file names under <c>wiki/</c> without the extension. Renaming a Wiki page
/// requires updating the code that refers to it.
/// </remarks>
public sealed record DocumentationPage(string EnglishPageName, string RussianPageName)
{
#pragma warning disable S1075 // This is the intentional, stable documentation destination.
    private const string WikiRootUrl = "https://github.com/VolocyNazad/revit.linter/wiki/";
#pragma warning restore S1075

    /// <summary>Gets the address of the page in the language of the current UI culture.</summary>
    /// <returns>The absolute HTTPS address of the page.</returns>
    public string GetUrl() => GetUrl(CultureInfo.CurrentUICulture);

    /// <summary>Gets the address of the page in the language of the specified culture.</summary>
    /// <param name="culture">The culture that selects the language.</param>
    /// <returns>The absolute HTTPS address of the page.</returns>
    /// <remarks>
    /// Russian cultures get the Russian page; every other culture gets the English one. Spaces become
    /// hyphens, as GitHub Wiki addresses pages, and the remaining characters are percent-encoded.
    /// </remarks>
    public string GetUrl(CultureInfo culture)
    {
        string pageName = string.Equals(culture.TwoLetterISOLanguageName, "ru", StringComparison.OrdinalIgnoreCase)
            ? RussianPageName
            : EnglishPageName;
        return WikiRootUrl + Uri.EscapeDataString(pageName.Replace(' ', '-'));
    }
}
