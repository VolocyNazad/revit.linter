using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Revit.Linter.ParameterValues.Models;
using Revit.Linter.ParameterValues.Services;

namespace Revit.Linter.ParameterValues.RevitTests;

public sealed class ParameterValueStorageTests : RevitApiTest
{
    [Test]
    public async Task Read_returns_null_for_missing_parameter() =>
        await Assert.That(ParameterValueConverter.Read(null)).IsNull();

    [Test]
    public async Task FromStorage_returns_text_for_string_storage()
    {
        object? value = ParameterValueConverter.FromStorage(StorageType.String, "checked", null);

        await Assert.That(value).IsEqualTo("checked");
    }

    [Test]
    public async Task FromStorage_returns_null_for_string_parameter_without_value() =>
        await Assert.That(ParameterValueConverter.FromStorage(StorageType.String, null, null)).IsNull();

    [Test]
    public async Task FromStorage_returns_number_for_integer_storage()
    {
        object? value = ParameterValueConverter.FromStorage(StorageType.Integer, 3, null);

        await Assert.That(value).IsEqualTo(3d);
    }

    [Test]
    public async Task TryToStorage_rejects_empty_value()
    {
        bool converted = ParameterValueConverter.TryToStorage(
            null, StorageType.String, null, false, out object? storageValue, out ParameterValueError? error);

        await Assert.That(converted).IsFalse();
        await Assert.That(storageValue).IsNull();
        await Assert.That(error).IsEqualTo(ParameterValueError.NullValue);
    }

    [Test]
    public async Task TryToStorage_returns_text_for_string_storage()
    {
        bool converted = ParameterValueConverter.TryToStorage(
            "checked", StorageType.String, null, false, out object? storageValue, out ParameterValueError? error);

        await Assert.That(converted).IsTrue();
        await Assert.That(storageValue).IsEqualTo("checked");
        await Assert.That(error).IsNull();
    }

    [Test]
    [Arguments(12.5, "12.5")]
    [Arguments(3.0, "3")]
    [Arguments(-0.25, "-0.25")]
    public async Task TryToStorage_formats_number_in_invariant_culture_for_string_storage(
        double value, string expected)
    {
        bool converted = ParameterValueConverter.TryToStorage(
            value, StorageType.String, null, false, out object? storageValue, out ParameterValueError? error);

        await Assert.That(converted).IsTrue();
        await Assert.That(storageValue).IsEqualTo(expected);
        await Assert.That(error).IsNull();
    }

    [Test]
    [Arguments(1.5, 2)]
    [Arguments(2.5, 3)]
    [Arguments(-1.5, -2)]
    [Arguments(-2.5, -3)]
    [Arguments(0.4, 0)]
    public async Task TryToStorage_rounds_number_away_from_zero_for_integer_storage(
        double value, int expected)
    {
        bool converted = ParameterValueConverter.TryToStorage(
            value, StorageType.Integer, null, false, out object? storageValue, out ParameterValueError? error);

        await Assert.That(converted).IsTrue();
        await Assert.That(storageValue).IsEqualTo(expected);
        await Assert.That(error).IsNull();
    }

    [Test]
    [Arguments(true, 1)]
    [Arguments(false, 0)]
    public async Task TryToStorage_normalizes_boolean_for_yes_no_storage(bool value, int expected)
    {
        bool converted = ParameterValueConverter.TryToStorage(
            value, StorageType.Integer, null, true, out object? storageValue, out ParameterValueError? error);

        await Assert.That(converted).IsTrue();
        await Assert.That(storageValue).IsEqualTo(expected);
        await Assert.That(error).IsNull();
    }

    [Test]
    [Arguments(5.0, 1)]
    [Arguments(0.0, 0)]
    [Arguments(-2.5, 1)]
    public async Task TryToStorage_normalizes_number_for_yes_no_storage(double value, int expected)
    {
        bool converted = ParameterValueConverter.TryToStorage(
            value, StorageType.Integer, null, true, out object? storageValue, out ParameterValueError? error);

        await Assert.That(converted).IsTrue();
        await Assert.That(storageValue).IsEqualTo(expected);
        await Assert.That(error).IsNull();
    }

    [Test]
    public async Task TryToStorage_rejects_boolean_for_plain_integer_storage()
    {
        bool converted = ParameterValueConverter.TryToStorage(
            true, StorageType.Integer, null, false, out _, out ParameterValueError? error);

        await Assert.That(converted).IsFalse();
        await Assert.That(error).IsEqualTo(ParameterValueError.TypeMismatch);
    }

    [Test]
    public async Task TryToStorage_rejects_text_for_numeric_storage()
    {
        bool converted = ParameterValueConverter.TryToStorage(
            "1.5", StorageType.Integer, null, false, out _, out ParameterValueError? error);

        await Assert.That(converted).IsFalse();
        await Assert.That(error).IsEqualTo(ParameterValueError.TypeMismatch);
    }

    [Test]
    [Arguments("Integer")]
    [Arguments("Double")]
    public async Task TryToStorage_rejects_non_finite_number(string storageTypeName)
    {
        StorageType storageType = (StorageType)Enum.Parse(typeof(StorageType), storageTypeName);
        bool converted = ParameterValueConverter.TryToStorage(
            double.NaN, storageType, null, false, out _, out ParameterValueError? error);

        await Assert.That(converted).IsFalse();
        await Assert.That(error).IsEqualTo(ParameterValueError.NotFinite);
    }

    [Test]
    [Arguments(3e9)]
    [Arguments(double.MaxValue)]
    [Arguments(double.MinValue)]
    public async Task TryToStorage_rejects_number_outside_integer_range(double value)
    {
        bool converted = ParameterValueConverter.TryToStorage(
            value, StorageType.Integer, null, false, out _, out ParameterValueError? error);

        await Assert.That(converted).IsFalse();
        await Assert.That(error).IsEqualTo(ParameterValueError.OutOfRange);
    }

    [Test]
    public async Task TryToStorage_rejects_storage_without_value()
    {
        bool converted = ParameterValueConverter.TryToStorage(
            1.0, StorageType.None, null, false, out _, out ParameterValueError? error);

        await Assert.That(converted).IsFalse();
        await Assert.That(error).IsEqualTo(ParameterValueError.UnsupportedStorageType);
    }

    [Test]
    public async Task Describe_returns_text_for_every_error()
    {
        foreach (ParameterValueError error in Enum.GetValues(typeof(ParameterValueError)))
        {
            string description = ParameterValueErrors.Describe(error);
            await Assert.That(string.IsNullOrWhiteSpace(description)).IsFalse();
        }
    }
}