namespace Revit.Linter.ElementAccentor.Infrastructure.Factories;

public static class XYZFactory
{
    public static XYZ XYZ(params double[] array)
    {
        return array.Length != 3
            ? throw new ArgumentException("A vector can only be constructed based on an array of 3 points", nameof(array))
            : new XYZ(array[0], array[1], array[2]);
    }
}