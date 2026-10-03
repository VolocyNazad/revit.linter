using Revit.Linter.Core.Abstractions.Models;
using System.Globalization;

namespace Revit.Linter.Core.Tests;

public sealed class DocumentationPageTests
{
    private static readonly DocumentationPage Page = new("Quick start", "Быстрый старт");

    [Theory]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    [InlineData("")]
    public void Non_russian_cultures_get_the_english_page(string cultureName)
    {
        string url = Page.GetUrl(CultureInfo.GetCultureInfo(cultureName));

        Assert.Equal("https://github.com/VolocyNazad/revit.linter/wiki/Quick-start", url);
    }

    [Theory]
    [InlineData("ru")]
    [InlineData("ru-RU")]
    public void Russian_cultures_get_the_percent_encoded_russian_page(string cultureName)
    {
        string url = Page.GetUrl(CultureInfo.GetCultureInfo(cultureName));

        Assert.Equal(
            "https://github.com/VolocyNazad/revit.linter/wiki/"
            + "%D0%91%D1%8B%D1%81%D1%82%D1%80%D1%8B%D0%B9-%D1%81%D1%82%D0%B0%D1%80%D1%82",
            url);
    }

    [Fact]
    public void Every_space_becomes_a_hyphen()
    {
        DocumentationPage page = new("Diagnostic configuration path button", "Кнопка папки конфигурации");

        Assert.EndsWith("/Diagnostic-configuration-path-button", page.GetUrl(CultureInfo.InvariantCulture));
    }
}
