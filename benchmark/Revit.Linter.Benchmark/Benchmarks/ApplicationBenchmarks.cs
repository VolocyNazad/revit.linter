using Autodesk.Revit.DB;
using BenchmarkDotNet.Attributes;
using Nice3point.BenchmarkDotNet.Revit;

namespace Revit.Linter.Benchmark.Benchmarks;

/// <summary>
/// Benchmarks Revit application-level operations. No document is required.
/// </summary>
public class ApplicationBenchmarks : RevitApiBenchmark
{
    [Benchmark]
    public XYZ NewXyz()
    {
        return new XYZ(3, 4, 5);
    }

    [Benchmark]
    public XYZ CreateNewXyz()
    {
        return Application.Create.NewXYZ(3, 4, 5);
    }
}