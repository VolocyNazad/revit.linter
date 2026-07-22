using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core.Executors;

namespace Revit.Linter.Languages.RevitTests;

public sealed class ElementFormulaTests : RevitApiTest
{
    private Document? _document;
    private Wall? _wall;
    private Level? _level;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateModel()
    {
        _document = Application.NewProjectDocument(UnitSystem.Metric);

        using Transaction transaction = new(_document, "Seed language tests");
        transaction.Start();
        _level = Level.Create(_document, 0);
        _wall = Wall.Create(
            _document,
            Line.CreateBound(XYZ.Zero, new XYZ(10, 0, 0)),
            _level.Id,
            false);
        _wall.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).Set("checked");
        transaction.Commit();
    }

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseModel()
    {
        _document?.Close(false);
    }

    [Test]
    public async Task Property_declared_on_concrete_wall_type_is_available_from_element_expression()
    {
        Func<Element, bool> formula =
            PropertyFormulaCompiler.CompileElement<bool>("property('Width') > 0");

        bool result = formula.Invoke(_wall!);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Parameterless_element_method_is_available()
    {
        Func<Element, bool> formula =
            PropertyFormulaCompiler.CompileElement<bool>("!isnull(method('GetTypeId'))");

        bool result = formula.Invoke(_wall!);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Dynamically_selected_element_method_is_available()
    {
        Func<Element, bool> formula = PropertyFormulaCompiler.CompileElement<bool>(
            "!isnull(method(if(true, 'GetTypeId', 'Missing')))");

        bool result = formula.Invoke(_wall!);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Builtin_string_parameter_is_available()
    {
        Func<Element, bool> formula = PropertyFormulaCompiler.CompileElement<bool>(
            "parameter('ALL_MODEL_INSTANCE_COMMENTS') == 'checked'");

        bool result = formula.Invoke(_wall!);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Dynamically_selected_parameter_identifier_is_available()
    {
        Func<Element, bool> formula = PropertyFormulaCompiler.CompileElement<bool>(
            "parameter(if(true, 'ALL_MODEL_INSTANCE_COMMENTS', 'INVALID')) == 'checked'");

        bool result = formula.Invoke(_wall!);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Missing_parameter_returns_null()
    {
        Func<Element, bool> formula =
            PropertyFormulaCompiler.CompileElement<bool>("isnull(parameter('Missing parameter'))");

        bool result = formula.Invoke(_wall!);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Double_parameter_is_converted_to_language_number()
    {
        Func<Element, bool> formula =
            PropertyFormulaCompiler.CompileElement<bool>("parameter('WALL_BASE_OFFSET') == 0");

        bool result = formula.Invoke(_wall!);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task ElementId_parameter_is_returned_as_value()
    {
        Func<Element, bool> formula =
            PropertyFormulaCompiler.CompileElement<bool>("!isnull(parameter('WALL_BASE_CONSTRAINT'))");

        bool result = formula.Invoke(_wall!);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Builtin_category_filter_matches_wall_only()
    {
        ElementFilter filter = PropertyFormulaCompiler.CompileElementFilter("builtincategory('OST_Walls')");

        bool matchesWall = filter.PassesFilter(_wall!);
        bool matchesLevel = filter.PassesFilter(_level!);

        await Assert.That(matchesWall).IsTrue();
        await Assert.That(matchesLevel).IsFalse();
    }

    [Test]
    public async Task Class_filter_matches_wall_only()
    {
        ElementFilter filter = PropertyFormulaCompiler.CompileElementFilter("class('Wall')");

        bool matchesWall = filter.PassesFilter(_wall!);
        bool matchesLevel = filter.PassesFilter(_level!);

        await Assert.That(matchesWall).IsTrue();
        await Assert.That(matchesLevel).IsFalse();
    }

    [Test]
    public async Task Instance_and_type_filters_distinguish_wall_from_wall_type()
    {
        Element wallType = _document!.GetElement(_wall!.GetTypeId());
        ElementFilter instanceFilter = PropertyFormulaCompiler.CompileElementFilter("instance");
        ElementFilter typeFilter = PropertyFormulaCompiler.CompileElementFilter("type");

        await Assert.That(instanceFilter.PassesFilter(_wall)).IsTrue();
        await Assert.That(instanceFilter.PassesFilter(wallType)).IsFalse();
        await Assert.That(typeFilter.PassesFilter(_wall)).IsFalse();
        await Assert.That(typeFilter.PassesFilter(wallType)).IsTrue();
    }

    [Test]
    public async Task Logical_and_combines_element_filters()
    {
        Element wallType = _document!.GetElement(_wall!.GetTypeId());
        ElementFilter filter = PropertyFormulaCompiler.CompileElementFilter(
            "instance and builtincategory('OST_Walls')");

        await Assert.That(filter.PassesFilter(_wall)).IsTrue();
        await Assert.That(filter.PassesFilter(wallType)).IsFalse();
        await Assert.That(filter.PassesFilter(_level!)).IsFalse();
    }

    [Test]
    public async Task Logical_or_combines_element_filters()
    {
        ElementFilter filter = PropertyFormulaCompiler.CompileElementFilter(
            "builtincategory('OST_Walls') or builtincategory('OST_Levels')");

        await Assert.That(filter.PassesFilter(_wall!)).IsTrue();
        await Assert.That(filter.PassesFilter(_level!)).IsTrue();
    }

    [Test]
    public async Task All_and_empty_filters_have_opposite_results()
    {
        ElementFilter allFilter = PropertyFormulaCompiler.CompileElementFilter("all");
        ElementFilter emptyFilter = PropertyFormulaCompiler.CompileElementFilter("empty");

        await Assert.That(allFilter.PassesFilter(_wall!)).IsTrue();
        await Assert.That(emptyFilter.PassesFilter(_wall!)).IsFalse();
    }
}
