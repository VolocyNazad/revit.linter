using Revit.Linter.ElementAccentor.Infrastructure.Factories;
using System.Diagnostics.CodeAnalysis;

namespace Revit.Linter.ElementAccentor.Infrastructure.Implementations;

[SuppressMessage("SonarAnalyzer", "S101", Justification = "XYZ is coordinate system")]
public sealed class SizableBoundingBoxXYZ : BoundingBoxXYZ
{
    public SizableBoundingBoxXYZ(double height = 1, double width = 1, double length = 1)
    {
        _alignments = [Align.Center, Align.Center, Align.Center];
        Height = height;
        Width = width;
        Length = length;
    }

    public SizableBoundingBoxXYZ(BoundingBoxXYZ boundingBox)
    {
        _alignments = [Align.Center, Align.Center, Align.Center];
        NewOrigin(boundingBox.Transform.Origin).NewSize(boundingBox.Min, boundingBox.Max);
    }

    /// <summary> Длина </summary>
    public double Length { get => GetDimension(Dimension.Length); set => SetDimension(Dimension.Length, value); }

    /// <summary> Ширина </summary>
    public double Width { get => GetDimension(Dimension.Width); set => SetDimension(Dimension.Width, value); }

    /// <summary> Высота </summary>
    public double Height { get => GetDimension(Dimension.Height); set => SetDimension(Dimension.Height, value); }

    /// <summary> Центр </summary>
    public XYZ Center => (Min + Max) / 2;

    /// <summary> Точка начала координат ящика </summary>
    public XYZ Origin
    {
        get => Transform.Origin;
        set
        {
            Transform transform = Transform;
            transform.Origin = value;
            Transform = transform;
        }
    }

    private readonly Align[] _alignments;
    /// <summary> Выравнивание </summary>
    /// <param name="dimension"> Габарит </param>
    /// <returns></returns>
    public Align this[Dimension dimension]
    {
        get => _alignments[(int)dimension];
        set => _alignments[(int)dimension] = value;
    }

    /// <summary> Задает выравнивание указанному габариту ящика. </summary>
    /// <param name="dimension"> Тип габарита </param>
    /// <param name="align"> Выравнивание </param>
    /// <returns> Возвращает ссылку на габаритный ящик </returns>
    public SizableBoundingBoxXYZ NewAlign(Dimension dimension, Align align = Align.Center)
    {
        this[dimension] = align;

        return this;
    }

    /// <summary> Задает выравнивание сразу всем габаритам ящика </summary>
    /// <param name="align"> Выравнивание </param>
    /// <returns> Возвращает ссылку на габаритный ящик </returns>
    public SizableBoundingBoxXYZ NewAlign(Align align = Align.Center)
    {
        this[Dimension.Height] = align;
        this[Dimension.Width] = align;
        this[Dimension.Length] = align;

        return this;
    }

    /// <summary> Задает выравнивание сразу всем габаритам ящика </summary>
    /// <param name="heightAlign"> Выравнивание по высоте </param>
    /// <param name="widthAlign"> Выравнивание по ширине </param>
    /// <param name="lengthAlign"> Выравнивание по длине </param>
    /// <returns> Возвращает ссылку на габаритный ящик </returns>
    public SizableBoundingBoxXYZ NewAlign(Align heightAlign, Align widthAlign, Align lengthAlign)
    {
        this[Dimension.Height] = heightAlign;
        this[Dimension.Width] = widthAlign;
        this[Dimension.Length] = lengthAlign;

        return this;
    }

    public SizableBoundingBoxXYZ NewCenter(XYZ center)
    {
        double height = Height;
        double width = Width;
        double length = Length;
        Min = new(center.X - length / 2, center.Y - width / 2, center.Z - height / 2);
        Max = new(center.X + length / 2, center.Y + width / 2, center.Z + height / 2);

        return this;
    }
    public SizableBoundingBoxXYZ NewMin(XYZ min)
    {
        double height = Height;
        double width = Width;
        double length = Length;
        Min = min;
        Max = new(min.X + length, min.Y + width, min.Z + height);

        return this;
    }
    public SizableBoundingBoxXYZ NewMax(XYZ max)
    {
        double height = Height;
        double width = Width;
        double length = Length;
        Max = max;
        Min = new(max.X - length, max.Y - width, max.Z - height);

        return this;
    }
    public SizableBoundingBoxXYZ NewSize(XYZ min, XYZ max)
    {
        Max = max;
        Min = min;

        return this;
    }
    public SizableBoundingBoxXYZ NewOrigin(XYZ origin)
    {
        Origin = origin;

        return this;
    }

    public SizableBoundingBoxXYZ AppendHeight(double height)
    {
        Height += height;

        return this;
    }

    public SizableBoundingBoxXYZ AppendWidth(double width)
    {
        Width += width;

        return this;
    }

    public SizableBoundingBoxXYZ AppendLength(double length)
    {
        Length += length;

        return this;
    }

    public SizableBoundingBoxXYZ NewHeight(double height)
    {
        Height = height;

        return this;
    }

    public SizableBoundingBoxXYZ NewWidth(double width)
    {
        Width = width;

        return this;
    }

    public SizableBoundingBoxXYZ NewLength(double length)
    {
        Length = length;

        return this;
    }

    /// <summary> Получение величины одного из трех габаритов ящика </summary>
    /// <param name="dimension"> Тип габарита </param>
    /// <returns> Возвращает величину одного из габаритов </returns>
    private double GetDimension(Dimension dimension)
    {
        double value = Max[(int)dimension] - Min[(int)dimension];
        return value;
    }

    /// <summary> Изменение величины одного из трех габаритов ящика </summary>
    /// <param name="dimension"> Тип габарита </param>
    /// <param name="value"> Значение </param>
    private void SetDimension(Dimension dimension, double value)
    {
        double minValue;
        minValue = _alignments[(int)dimension] switch
        {
            Align.Start => Min[(int)dimension],
            Align.End => Max[(int)dimension] - value,
            Align.Center => Center[(int)dimension] - value / 2,
            _ => throw new NotImplementedException()
        };

        var minCoordinates = new[] { Min[0], Min[1], Min[2] };
        var maxCoordinates = new[] { Max[0], Max[1], Max[2] };

        minCoordinates[(int)dimension] = minValue;
        maxCoordinates[(int)dimension] = minValue + value;

        Min = XYZFactory.XYZ(minCoordinates);
        Max = XYZFactory.XYZ(maxCoordinates);
    }
}

/// <summary> Габариты </summary>
public enum Dimension
{
    Length,
    Width,
    Height,
}

/// <summary> Выравнивание </summary>
public enum Align
{
    Start,
    Center,
    End,
}