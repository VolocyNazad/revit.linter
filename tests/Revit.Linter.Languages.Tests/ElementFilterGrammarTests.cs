using Revit.Linter.Languages.Languages;
using StringToExpression;
using StringToExpression.GrammerDefinitions;

namespace Revit.Linter.Languages.Tests;

public sealed class ElementFilterGrammarTests
{
    [Fact]
    public void Element_filter_grammar_builds()
    {
        GrammerDefinition[] definitions = LanguageDefinitions.CreateElementFilter();
        _ = new Language(definitions);

        Assert.NotEmpty(definitions);
    }
}
