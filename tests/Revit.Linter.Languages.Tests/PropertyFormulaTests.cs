namespace Revit.Linter.Languages.Tests;

public sealed class PropertyFormulaTests
{
    private readonly TestTarget _target = new()
    {
        Name = "Wall 01",
        Count = 3,
        IsActive = true,
        NullableValue = null,
        BaseName = "Base",
    };

    [Theory]
    [InlineData("property('Name')", "Wall 01")]
    [InlineData("property('BaseName')", "Base")]
    [InlineData("property(if(true, 'Name', 'BaseName'))", "Wall 01")]
    [InlineData("property('Name') + ' checked'", "Wall 01 checked")]
    [InlineData("tolower(property('Name'))", "wall 01")]
    public void String_property_can_be_used_in_formula(string formula, string expected)
    {
        string result = FormulaCompiler.Evaluate<TestTarget, string>(formula, _target);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("property('Count') == 3", true)]
    [InlineData("property('Count') > 2", true)]
    [InlineData("property('IsActive') == true", true)]
    [InlineData("property('NullableValue') == null", true)]
    [InlineData("isnull(property('Missing'))", true)]
    [InlineData("isnull(property('name'))", true)]
    public void Property_can_participate_in_logical_formula(string formula, bool expected)
    {
        bool result = FormulaCompiler.Evaluate<TestTarget, bool>(formula, _target);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Formula_is_compiled_once_and_evaluated_for_different_targets()
    {
        Func<TestTarget, string> formula = FormulaCompiler.Compile<TestTarget, string>("property('Name')");
        TestTarget anotherTarget = new() { Name = "Door 01" };

        string first = formula.Invoke(_target);
        string second = formula.Invoke(anotherTarget);

        Assert.Equal("Wall 01", first);
        Assert.Equal("Door 01", second);
    }

    [Fact]
    public void Property_declared_only_on_runtime_type_is_available()
    {
        Func<BaseTarget, string> formula = FormulaCompiler.Compile<BaseTarget, string>("property('Name')");
        BaseTarget target = new TestTarget { Name = "Wall 01" };

        string result = formula.Invoke(target);

        Assert.Equal("Wall 01", result);
    }

    private class BaseTarget
    {
        public string BaseName { get; init; } = string.Empty;
    }

    private sealed class TestTarget : BaseTarget
    {
        public string Name { get; init; } = string.Empty;
        public double Count { get; init; }
        public bool IsActive { get; init; }
        public string? NullableValue { get; init; }
    }
}
