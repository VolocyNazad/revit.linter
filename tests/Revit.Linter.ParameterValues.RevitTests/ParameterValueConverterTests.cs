using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.ParameterValues.Models;
using Revit.Linter.ParameterValues.Services;
using TUnit.Core.Executors;

namespace Revit.Linter.ParameterValues.RevitTests;

public sealed class ParameterValueConverterTests : RevitApiTest
{
    private Document? _document;
    private Wall? _wall;
    private Level? _level;

    private Document CurrentDocument => _document!;
    private Wall TargetWall => _wall!;
    private Level TargetLevel => _level!;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateModel()
    {
        _document = Application.NewProjectDocument(UnitSystem.Metric);

        using Transaction transaction = new(_document, "Seed parameter value tests");
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
    public void CloseModel() => _document?.Close(false);

    [Test]
    public async Task Read_returns_string_parameter_value()
    {
        object? value = ParameterValueConverter.Read(
            TargetWall.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS));

        await Assert.That(value).IsEqualTo("checked");
    }

    [Test]
    public async Task Read_returns_double_parameter_in_project_unit()
    {
        Parameter parameter = TargetWall.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH);
        double expected = UnitUtils.ConvertFromInternalUnits(
            parameter.AsDouble(), parameter.GetUnitTypeId());

        object? value = ParameterValueConverter.Read(parameter);

        await Assert.That(value).IsEqualTo(expected);
    }

    [Test]
    public async Task Read_returns_element_id_parameter_as_value()
    {
        object? value = ParameterValueConverter.Read(
            TargetWall.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT));

        await Assert.That(value).IsEqualTo(TargetLevel.Id);
    }

    [Test]
    public async Task Read_returns_null_for_missing_parameter() =>
        await Assert.That(ParameterValueConverter.Read(null)).IsNull();

    [Test]
    public async Task TryWrite_writes_string_parameter()
    {
        using (Transaction transaction = new(CurrentDocument, "Write string parameter"))
        {
            transaction.Start();
            ParameterWriteResult result = ParameterValueConverter.TryWrite(
                TargetWall.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS), "fixed");
            transaction.Commit();

            await Assert.That(result.Success).IsTrue();
            await Assert.That(result.Error).IsNull();
        }

        await Assert.That(
            TargetWall.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).AsString())
            .IsEqualTo("fixed");
    }

    [Test]
    public async Task TryWrite_converts_double_value_to_project_units()
    {
        using (Transaction transaction = new(CurrentDocument, "Write double parameter"))
        {
            transaction.Start();
            ParameterWriteResult result = ParameterValueConverter.TryWrite(
                TargetWall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET), 1.5);
            transaction.Commit();

            await Assert.That(result.Success).IsTrue();
        }

        Parameter parameter = TargetWall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET);
        double expected = UnitUtils.ConvertToInternalUnits(1.5, parameter.GetUnitTypeId());
        await Assert.That(parameter.AsDouble()).IsEqualTo(expected);
    }

    [Test]
    public async Task TryWrite_normalizes_boolean_for_yes_no_parameter()
    {
        Parameter? parameter = TargetLevel.get_Parameter(BuiltInParameter.LEVEL_IS_BUILDING_STORY);
        await Assert.That(parameter).IsNotNull();
        await Assert.That(parameter.StorageType).IsEqualTo(StorageType.Integer);

        using (Transaction transaction = new(CurrentDocument, "Write yes/no parameter"))
        {
            transaction.Start();
            ParameterWriteResult result = ParameterValueConverter.TryWrite(parameter, true);
            transaction.Commit();

            await Assert.That(result.Success).IsTrue();
        }

        await Assert.That(
            TargetLevel.get_Parameter(BuiltInParameter.LEVEL_IS_BUILDING_STORY).AsInteger())
            .IsEqualTo(1);
    }

    [Test]
    public async Task TryWrite_writes_element_id_parameter()
    {
        using (Transaction transaction = new(CurrentDocument, "Write element id parameter"))
        {
            transaction.Start();
            ParameterWriteResult result = ParameterValueConverter.TryWrite(
                TargetWall.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT), TargetLevel.Id);
            transaction.Commit();

            await Assert.That(result.Success).IsTrue();
        }

        await Assert.That(
            TargetWall.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT).AsElementId())
            .IsEqualTo(TargetLevel.Id);
    }

    [Test]
    public async Task TryWrite_returns_missing_parameter_for_null_parameter()
    {
        ParameterWriteResult result = ParameterValueConverter.TryWrite(null, "value");

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error).IsEqualTo(ParameterValueError.MissingParameter);
    }

    [Test]
    public async Task TryWrite_returns_null_value_and_keeps_parameter()
    {
        ParameterWriteResult result = ParameterValueConverter.TryWrite(
            TargetWall.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS), null);

        await Assert.That(result.Error).IsEqualTo(ParameterValueError.NullValue);
        await Assert.That(
            TargetWall.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).AsString())
            .IsEqualTo("checked");
    }

    [Test]
    public async Task TryWrite_returns_type_mismatch_for_text_in_double_parameter()
    {
        ParameterWriteResult result = ParameterValueConverter.TryWrite(
            TargetWall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET), "text");

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error).IsEqualTo(ParameterValueError.TypeMismatch);
    }

    [Test]
    public async Task TryWrite_returns_read_only_for_computed_parameter()
    {
        ParameterWriteResult result = ParameterValueConverter.TryWrite(
            TargetWall.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED), 1.0);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error).IsEqualTo(ParameterValueError.ReadOnly);
    }

    [Test]
    public async Task TryWrite_returns_invalid_element_id_for_invalid_id()
    {
        ParameterWriteResult result = ParameterValueConverter.TryWrite(
            TargetWall.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT),
            ElementId.InvalidElementId);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error).IsEqualTo(ParameterValueError.InvalidElementId);
    }

    [Test]
    public async Task TryWrite_returns_foreign_element_id_for_missing_element_id()
    {
#if AFTER2024
        ElementId missingId = new(999999L);
#else
        ElementId missingId = new(999999);
#endif
        await Assert.That(CurrentDocument.GetElement(missingId)).IsNull();

        ParameterWriteResult result = ParameterValueConverter.TryWrite(
            TargetWall.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT), missingId);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error).IsEqualTo(ParameterValueError.ForeignElementId);
    }
}