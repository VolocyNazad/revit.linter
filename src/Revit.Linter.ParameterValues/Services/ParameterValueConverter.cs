using System.Globalization;
using Revit.Linter.ParameterValues.Infrastructure;
using Revit.Linter.ParameterValues.Models;

namespace Revit.Linter.ParameterValues.Services;

/// <summary>Converts parameter values between the Revit storage model and the formula value model.</summary>
/// <remarks>
/// The formula value model is the one the diagnostic configuration formulas use: a string parameter is a
/// <see cref="string"/>, an integer parameter is a <see cref="double"/>, a double parameter is a
/// <see cref="double"/> in the project unit of that parameter, and an element identifier parameter is an
/// <see cref="ElementId"/>. Reading never changes a value. Writing applies the inverse conversion and
/// refuses a value it cannot store instead of writing a default.
/// </remarks>
public static class ParameterValueConverter
{
    /// <summary>Reads the value of a parameter in the formula value model.</summary>
    /// <param name="parameter">The parameter to read, or <see langword="null"/> for a missing parameter.</param>
    /// <returns>The value, or <see langword="null"/> when the parameter is missing or has no value.</returns>
    /// <exception cref="InvalidOperationException">The parameter has no value storage type.</exception>
    /// <remarks>
    /// A <c>Double</c> value is converted from Revit internal units to the unit of the parameter, the same
    /// way the formulas compare it.
    /// </remarks>
    public static object? Read(Parameter? parameter)
    {
        if (parameter is null) return null;

        return parameter.StorageType switch
        {
            StorageType.String => FromStorage(StorageType.String, parameter.AsString(), null),
            StorageType.Integer => FromStorage(StorageType.Integer, parameter.AsInteger(), null),
            StorageType.Double => FromStorage(StorageType.Double, parameter.AsDouble(), parameter.GetUnitTypeId()),
            StorageType.ElementId => FromStorage(StorageType.ElementId, parameter.AsElementId(), null),
            _ => throw new InvalidOperationException(
                "A parameter without a value storage type has no readable value."),
        };
    }

    /// <summary>Converts one raw storage value into the formula value model.</summary>
    /// <param name="storageType">The storage type of the parameter.</param>
    /// <param name="rawValue">The value returned by the matching <c>Parameter.As</c> method.</param>
    /// <param name="unitTypeId">The unit of the parameter; required for <see cref="StorageType.Double"/>.</param>
    /// <returns>The value in the formula value model.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="unitTypeId"/> is missing for a double value.</exception>
    /// <exception cref="ArgumentException">The raw value does not fit the storage type.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The storage type has no formula value.</exception>
    public static object? FromStorage(StorageType storageType, object? rawValue, ForgeTypeId? unitTypeId)
    {
        switch (storageType)
        {
            case StorageType.String when rawValue is null or string:
                return rawValue;

            case StorageType.Integer when rawValue is int integerValue:
                return Convert.ToDouble(integerValue);

            case StorageType.Double when rawValue is double doubleValue:
                if (unitTypeId is null)
                    throw new ArgumentNullException(
                        nameof(unitTypeId), "A double value requires the unit of its parameter.");
                return UnitUtils.ConvertFromInternalUnits(doubleValue, unitTypeId);

            case StorageType.ElementId when rawValue is null or ElementId:
                return rawValue;

            case StorageType.None:
                throw new ArgumentOutOfRangeException(
                    nameof(storageType), storageType, "The storage type has no formula value.");

            default:
                throw new ArgumentException(
                    $"A value of type '{rawValue?.GetType().Name ?? "null"}' does not fit storage type '{storageType}'.",
                    nameof(rawValue));
        }
    }

    /// <summary>Converts a formula value into the raw storage value of a parameter.</summary>
    /// <param name="value">The value to convert; <see langword="null"/> is rejected.</param>
    /// <param name="storageType">The storage type of the destination parameter.</param>
    /// <param name="unitTypeId">The unit of the destination parameter; required for <see cref="StorageType.Double"/>.</param>
    /// <param name="isYesNo">Whether the destination integer parameter is a yes/no parameter.</param>
    /// <param name="storageValue">The value ready for the matching <c>Parameter.Set</c> overload.</param>
    /// <param name="error">The reason of a rejected conversion, or <see langword="null"/> when it succeeded.</param>
    /// <returns><see langword="true"/> when the value converts; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="unitTypeId"/> is missing for a double value.</exception>
    /// <remarks>
    /// A number reaches a string parameter in the invariant culture, the same way the formula
    /// <c>str</c> function formats it. Text never reaches a numeric parameter: parsing text needs an
    /// explicit <c>num</c> call in the formula. A yes/no parameter accepts a boolean or any finite number,
    /// which it normalizes to 0 or 1.
    /// </remarks>
    public static bool TryToStorage(
        object? value,
        StorageType storageType,
        ForgeTypeId? unitTypeId,
        bool isYesNo,
        out object? storageValue,
        out ParameterValueError? error)
    {
        storageValue = null;
        error = null;

        if (value is null)
        {
            error = ParameterValueError.NullValue;
            return false;
        }

        switch (storageType)
        {
            case StorageType.String:
                return TryToString(value, out storageValue, out error);

            case StorageType.Integer:
                return TryToInteger(value, isYesNo, out storageValue, out error);

            case StorageType.Double:
                return TryToDouble(value, unitTypeId, out storageValue, out error);

            case StorageType.ElementId:
                return TryToElementId(value, out storageValue, out error);

            default:
                error = ParameterValueError.UnsupportedStorageType;
                return false;
        }
    }

    /// <summary>Writes a formula value to a parameter.</summary>
    /// <param name="parameter">The destination parameter, or <see langword="null"/> when it was not found.</param>
    /// <param name="value">The value to write.</param>
    /// <returns>The write result; a rejected result leaves the parameter unchanged.</returns>
    /// <remarks>
    /// The method refuses what it cannot store instead of writing a default: a missing parameter, a
    /// read-only parameter, an empty value and an element identifier outside the parameter document are
    /// reported through <see cref="ParameterWriteResult.Error"/>. A failure reported by Revit keeps its
    /// exception in <see cref="ParameterWriteResult.Failure"/>.
    /// </remarks>
    public static ParameterWriteResult TryWrite(Parameter? parameter, object? value)
    {
        if (parameter is null)
            return ParameterWriteResult.Rejected(ParameterValueError.MissingParameter);

        if (parameter.IsReadOnly)
            return ParameterWriteResult.Rejected(ParameterValueError.ReadOnly);

        StorageType storageType = parameter.StorageType;
        if (storageType == StorageType.None)
            return ParameterWriteResult.Rejected(ParameterValueError.UnsupportedStorageType);

        ForgeTypeId? unitTypeId = storageType == StorageType.Double ? parameter.GetUnitTypeId() : null;
        if (!TryToStorage(value, storageType, unitTypeId, parameter.IsYesNo(), out object? storageValue, out ParameterValueError? error))
            return ParameterWriteResult.Rejected(error!.Value);

        if (storageValue is ElementId elementId && !BelongsToDocument(parameter, elementId))
            return ParameterWriteResult.Rejected(ParameterValueError.ForeignElementId);

        try
        {
            bool isSet = storageType switch
            {
                StorageType.String => parameter.Set((string)storageValue!),
                StorageType.Integer => parameter.Set((int)storageValue!),
                StorageType.Double => parameter.Set((double)storageValue!),
                StorageType.ElementId => parameter.Set((ElementId)storageValue!),
                _ => throw new InvalidOperationException("The storage type has no parameter setter."),
            };
            return isSet
                ? ParameterWriteResult.Written
                : ParameterWriteResult.Rejected(ParameterValueError.SetRejected);
        }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException exception)
        {
            return ParameterWriteResult.Rejected(ParameterValueError.ReadOnly, exception);
        }
        catch (Autodesk.Revit.Exceptions.ApplicationException exception)
        {
            return ParameterWriteResult.Rejected(ParameterValueError.SetRejected, exception);
        }
    }

    private static bool TryToString(
        object value, out object? storageValue, out ParameterValueError? error)
    {
        storageValue = null;
        error = null;

        switch (value)
        {
            case string text:
                storageValue = text;
                return true;
            case double number:
                storageValue = Convert.ToString(number, CultureInfo.InvariantCulture);
                return true;
            default:
                error = ParameterValueError.TypeMismatch;
                return false;
        }
    }

    private static bool TryToInteger(
        object value, bool isYesNo, out object? storageValue, out ParameterValueError? error)
    {
        storageValue = null;
        error = null;

        if (isYesNo)
        {
            switch (value)
            {
                case bool flag:
                    storageValue = flag ? 1 : 0;
                    return true;
                case double yesNoNumber:
                    if (!IsFinite(yesNoNumber))
                    {
                        error = ParameterValueError.NotFinite;
                        return false;
                    }
                    storageValue = yesNoNumber != 0 ? 1 : 0;
                    return true;
                default:
                    error = ParameterValueError.TypeMismatch;
                    return false;
            }
        }

        if (value is not double number)
        {
            error = ParameterValueError.TypeMismatch;
            return false;
        }
        if (!IsFinite(number))
        {
            error = ParameterValueError.NotFinite;
            return false;
        }

        double rounded = Math.Round(number, MidpointRounding.AwayFromZero);
        if (rounded < int.MinValue || rounded > int.MaxValue)
        {
            error = ParameterValueError.OutOfRange;
            return false;
        }

        storageValue = (int)rounded;
        return true;
    }

    private static bool TryToDouble(
        object value, ForgeTypeId? unitTypeId, out object? storageValue, out ParameterValueError? error)
    {
        storageValue = null;
        error = null;

        if (value is not double number)
        {
            error = ParameterValueError.TypeMismatch;
            return false;
        }
        if (!IsFinite(number))
        {
            error = ParameterValueError.NotFinite;
            return false;
        }
        if (unitTypeId is null)
            throw new ArgumentNullException(
                nameof(unitTypeId), "A double value requires the unit of its parameter.");

        double internalValue = UnitUtils.ConvertToInternalUnits(number, unitTypeId);
        if (!IsFinite(internalValue))
        {
            error = ParameterValueError.OutOfRange;
            return false;
        }

        storageValue = internalValue;
        return true;
    }

    private static bool TryToElementId(
        object value, out object? storageValue, out ParameterValueError? error)
    {
        storageValue = null;
        error = null;

        if (value is not ElementId elementId)
        {
            error = ParameterValueError.TypeMismatch;
            return false;
        }
        if (ElementId.InvalidElementId.Equals(elementId))
        {
            error = ParameterValueError.InvalidElementId;
            return false;
        }

        storageValue = elementId;
        return true;
    }

    private static bool BelongsToDocument(Parameter parameter, ElementId elementId)
    {
        Element? owner = parameter.Element;
        if (owner is null) return true;

        Document document = owner.Document;
        if (document is not { IsValidObject: true }) return true;

        try
        {
            return document.GetElement(elementId) is not null;
        }
        catch (Autodesk.Revit.Exceptions.ApplicationException)
        {
            return false;
        }
    }

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}