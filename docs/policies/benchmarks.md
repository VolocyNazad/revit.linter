# Benchmark policy

Paths mentioned in this policy are relative to the repository root unless a Markdown link specifies otherwise.

Performance work is measured, not estimated. A claim about speed is backed by numbers from a
repeatable run on a stated environment.

## When

- a `perf:` change;
- a change to a hot path: document queries, element loops, collectors, serialization or
  start-up;
- a user-visible claim about speed or memory in a pull request, the changelog or the
  documentation.

## How

Benchmarks live in `benchmarks/<Project>.Benchmarks` and use
[BenchmarkDotNet](https://benchmarkdotnet.org/) together with
[Nice3point.BenchmarkDotNet.Revit](https://www.nuget.org/packages/Nice3point.BenchmarkDotNet.Revit),
which runs them inside Revit on its single-threaded API context.

- Benchmark classes derive from `RevitApiBenchmark`; the package is referenced as
  `Version="$(MajorRevitVersion).*"`, so each configuration gets the package built for its Revit
  year.
- Open and close documents in `OnGlobalSetup` and `OnGlobalCleanup`; `[GlobalSetup]` and
  `[GlobalCleanup]` cannot be applied twice.
- Run with the solution configuration instead of the default `Release`: use
  `Job.Default.WithCurrentConfiguration()` and
  `dotnet run -c Release_<version> -- --filter "*<Benchmarks>*"`.
- A licensed Revit of the matching version must be installed. The package starts it with the
  US English interface and the default installation path; the `RevitLanguage` and
  `RevitInstallationPath` attributes override both.
- Benchmarks are not part of the test run and never run in CI: a hosted runner has no Revit
  and is too noisy for a meaningful result.
- Run on an otherwise idle machine, add `[MemoryDiagnoser]` when allocations matter, and
  compare against the current `main`/`dev` baseline in the same session.

## Revit versions

Benchmarks use the same `Release_*` configurations as the tests, so a run always targets one
Revit version and one Revit process.

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
