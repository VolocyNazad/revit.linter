using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Revit.Linter.Benchmark.Benchmarks;
using Nice3point.BenchmarkDotNet.Revit;
using BenchmarkDotNet.Exporters.Json;

var benchmarkDirectory = FindBenchmarkDirectory();
var artifactsPath = Path.Combine(benchmarkDirectory, "BenchmarkDotNet.Artifacts");

var configuration = ManualConfig.Create(DefaultConfig.Instance)
    .WithArtifactsPath(artifactsPath)
    .AddJob(Job.Default.WithCurrentConfiguration())
    .AddDiagnoser(MemoryDiagnoser.Default)
    .AddExporter(CsvExporter.Default)
    .AddExporter(CsvMeasurementsExporter.Default)
    .AddExporter(JsonExporter.Default)
    .AddExporter(MarkdownExporter.GitHub);


BenchmarkRunner.Run<ElementGeometryWithSolidsBenchmarks>(configuration);
BenchmarkRunner.Run<ElementGeometryNoSolidsBenchmarks>(configuration);

static string FindBenchmarkDirectory()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "Revit.Linter.Benchmark.csproj")))
            return directory.Parent?.FullName
                   ?? throw new DirectoryNotFoundException("benchmark directory was not found.");

        directory = directory.Parent;
    }

    throw new DirectoryNotFoundException("Revit.Linter.Benchmark.csproj was not found.");
}
