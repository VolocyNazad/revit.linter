# Benchmark policy

Paths mentioned in this policy are relative to the repository root unless a Markdown link specifies otherwise.

Performance work is measured, not estimated. A claim about speed is backed by numbers from a
repeatable run on a stated environment.

## Kinds

| Kind | Project | Runs in | May touch |
| --- | --- | --- | --- |
| Headless | `benchmarks/<Project>.Benchmarks` | a normal process, with BenchmarkDotNet | pure code, collections, parsing, serialization, start-up of components that do not call the Revit API |
| Revit | `benchmarks/<Project>.RevitBenchmarks` | Revit, through Nice3point.BenchmarkDotNet.Revit | Revit API, `Document`, collectors, transactions |

The suffix follows the same idea as the test policy: the project name says whether a Revit
process is required. A project without `Benchmarks` or `RevitBenchmarks` is not a benchmark
project.

## When

- a `perf:` change;
- a change to a hot path: document queries, element loops, collectors, serialization or
  start-up;
- a user-visible claim about speed or memory in a pull request, the changelog or the
  documentation.

## Headless benchmarks

Code that does not need a running Revit is benchmarked with plain
[BenchmarkDotNet](https://benchmarkdotnet.org/):

- add `[MemoryDiagnoser]` when allocations matter and run in `Release`, without a debugger,
  on an otherwise idle machine;
- run with `dotnet run -c Release --project benchmarks/<Project>.Benchmarks -- --filter
  "*<Benchmarks>*"`;
- a headless benchmark that compiles differently per Revit version runs on each of those
  configurations, so the numbers match the shipped code;
- compare against the current `main`/`dev` baseline in the same session.

## Revit benchmarks

Code that calls the Revit API is benchmarked with
[Nice3point.BenchmarkDotNet.Revit](https://www.nuget.org/packages/Nice3point.BenchmarkDotNet.Revit),
which runs the benchmark inside Revit on its single-threaded API context:

- benchmark classes derive from `RevitApiBenchmark`; the package is referenced as
  `Version="$(MajorRevitVersion).*"`, so each configuration gets the package built for its Revit
  year;
- open and close documents in `OnGlobalSetup` and `OnGlobalCleanup`; `[GlobalSetup]` and
  `[GlobalCleanup]` cannot be applied twice;
- run with the solution configuration instead of the default `Release`: use
  `Job.Default.WithCurrentConfiguration()` and
  `dotnet run -c Release_<version> -- --filter "*<Benchmarks>*"`;
- a licensed Revit of the matching version must be installed. The package starts it with the
  US English interface and the default installation path; the `RevitLanguage` and
  `RevitInstallationPath` attributes override both.

## Not in CI

Neither kind runs in CI. A hosted runner has no Revit, and a shared runner is too noisy for a
meaningful number. Benchmarks are run locally, before and after the change.

## Revit versions

Revit benchmarks use the same `Release_*` configurations as the tests, so a run always targets
one Revit version and one Revit process.

- A number from one version does not transfer to another: a claim that spans versions is
  measured on each of them.
- A pull request that changes version-specific code benchmarks the versions it touches; a
  claim about the product as a whole benchmarks every supported version.
- Record the Revit version, the model, the hardware and the scenario with the result.

## Reporting

- A `perf:` commit carries the before and after numbers and the environment in its body.
- Results are reported in the pull request and not committed; benchmark output is generated
  data.
- No thresholds are enforced in CI; a regression is caught in review, not by a red build.

The rest of the workflow is defined by the [development policy](development.md); what must be
tested is in the [testing policy](testing.md).
