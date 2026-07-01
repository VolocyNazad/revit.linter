using System.Diagnostics.CodeAnalysis;

namespace Revit.Linter.ElementAccentor.Infrastructure.Factories;

[SuppressMessage("SonarAnalyzer", "S101", Justification = "XYZ is coordinate system")]
public static class XYZFactory
{
    public static XYZ XYZ(params double[] array)
    {
        return array.Length != 3
            ? throw new ArgumentException("A vector can only be constructed based on an array of 3 points", nameof(array))
            : new XYZ(array[0], array[1], array[2]);
    }
}