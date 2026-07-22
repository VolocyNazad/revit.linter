namespace Revit.Linter.Languages.Tests;

public sealed class MethodFormulaTests
{
    [Theory]
    [InlineData("method('GetName')", "Wall 01")]
    [InlineData("method(if(true, 'GetName', 'GetBaseName'))", "Wall 01")]
    [InlineData("method(if(false, 'GetName', 'GetBaseName'))", "Base")]
    [InlineData("method('GetName') + ' checked'", "Wall 01 checked")]
    [InlineData("tolower(method('GetName'))", "wall 01")]
    public void Parameterless_method_can_be_used_in_formula(string formula, string expected)
    {
        string result = FormulaCompiler.Evaluate<TestTarget, string>(formula, new TestTarget());
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("method('GetCount') == 3", true)]
    [InlineData("method('IsActive') == true", true)]
    [InlineData("isnull(method('Missing'))", true)]
    [InlineData("isnull(method('WithParameter'))", true)]
    [InlineData("isnull(method('VoidMethod'))", true)]
    [InlineData("isnull(method('GenericMethod'))", true)]
    [InlineData("isnull(method('getname'))", true)]
    [InlineData("isnull(method('GetNullableValue'))", true)]
    public void Method_can_participate_in_logical_formula(string formula, bool expected)
    {
        bool result = FormulaCompiler.Evaluate<TestTarget, bool>(formula, new TestTarget());
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Method_declared_only_on_runtime_type_is_available()
    {
        Func<BaseTarget, string> formula = FormulaCompiler.Compile<BaseTarget, string>("method('GetName')");

        string result = formula.Invoke(new TestTarget());

        Assert.Equal("Wall 01", result);
    }

    [Fact]
    public void Formula_is_compiled_once_and_evaluated_for_different_targets()
    {
        Func<TestTarget, string> formula = FormulaCompiler.Compile<TestTarget, string>("method('GetName')");

        string first = formula.Invoke(new TestTarget { Name = "Wall 01" });
        string second = formula.Invoke(new TestTarget { Name = "Door 01" });

        Assert.Equal("Wall 01", first);
        Assert.Equal("Door 01", second);
    }

    [Fact]
    public void Runtime_method_name_is_resolved_for_each_target()
    {
        Func<BaseTarget, string> formula = FormulaCompiler.Compile<BaseTarget, string>(
            "method(if(property('UseDerivedName'), 'GetName', 'GetBaseName'))");

        string baseName = formula.Invoke(new BaseTarget { BaseName = "Base", UseDerivedName = false });
        string derivedName = formula.Invoke(new TestTarget { Name = "Wall 01", UseDerivedName = true });

        Assert.Equal("Base", baseName);
        Assert.Equal("Wall 01", derivedName);
    }

    public class BaseTarget
    {
        public string BaseName { get; init; } = "Base";
        public bool UseDerivedName { get; init; }
        public string GetBaseName() => BaseName;
    }

    public sealed class TestTarget : BaseTarget
    {
        public string Name { get; init; } = "Wall 01";
        public double Count { get; init; } = 3;
        public bool Active { get; init; } = true;
        public string GetName() => Name;
        public double GetCount() => Count;
        public bool IsActive() => Active;
        public string WithParameter(string value) => value + Name;
        public void VoidMethod() => GC.KeepAlive(this);
        public T GenericMethod<T>()
        {
            GC.KeepAlive(Name);
            return default!;
        }

        public string? GetNullableValue()
        {
            GC.KeepAlive(Name);
            return null;
        }
    }
}
