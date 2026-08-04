using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Globalization;
using System.Text;
using Xunit;

namespace Revit.Linter.Localization.Generator.Tests;

public sealed class LocalizationPropertiesGeneratorTests
{
    [Fact]
    public void Linked_resource_is_resolved_from_localization_assembly()
    {
        CultureInfo previousCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ru-RU");
            Assert.Equal("Отчеты:", LocalizationResourceReader.GetString(
                "Revit.Linter.Localization.DiagnosticReportPresenter.ViewModels.DiagnosticReportViewModel",
                "reports_text"));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousCulture;
        }
    }

    [Fact]
    public void Attribute_generates_property_for_each_resource_key()
    {
        const string source = """
            namespace Sample;
            [Revit.Linter.Localization.GenerateLocalizedProperties]
            internal partial class SampleViewModel
            {
                private Microsoft.Extensions.Localization.IStringLocalizerFactory _localizerFactory = null!;
            }
            """;
        const string resource = """
            <root>
              <data name="reports_text"><value>Reports:</value></data>
              <data name="severity_header"><value>Severity</value></data>
            </root>
            """;

        CSharpParseOptions parseOptions = new(LanguageVersion.CSharp12);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(
            source,
            parseOptions,
            cancellationToken: cancellationToken);
        CSharpCompilation compilation = CSharpCompilation.Create(
            "Tests",
            [syntaxTree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        AdditionalText additionalText = new InMemoryAdditionalText("SampleViewModel.resx", resource);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new LocalizationPropertiesGenerator().AsSourceGenerator()],
            [additionalText],
            parseOptions);

        driver = driver.RunGenerators(compilation, cancellationToken);

        string generated = string.Join(
            Environment.NewLine,
            driver.GetRunResult().GeneratedTrees.Select(tree => tree.ToString()));
        Assert.Contains("public string ReportsText", generated, StringComparison.Ordinal);
        Assert.Contains("public string SeverityHeader", generated, StringComparison.Ordinal);
        Assert.Contains("GetLocalizedString(\"reports_text\")", generated, StringComparison.Ordinal);
    }

    private sealed class InMemoryAdditionalText(string path, string content) : AdditionalText
    {
        public override string Path { get; } = path;
        public override SourceText GetText(CancellationToken cancellationToken = default) =>
            SourceText.From(content, Encoding.UTF8);
    }
}
