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

- Benchmarks live in `benchmarks/<Project>.Benchmarks` and use
  [BenchmarkDotNet](https://benchmarkdotnet.org/).
- They are not part of the test run and never run in CI: a shared runner is too noisy for a
  meaningful result.
- Run in `Release`, without a debugger, on an otherwise idle machine, and add
  `[MemoryDiagnoser]` when allocations matter.
- Compare against the current `main`/`dev` baseline in the same session; a number from another
  machine or day is not a comparison.

## Revit-bound code

Revit API calls need a running Revit, so they are measured outside BenchmarkDotNet:

- measure the part that can run headless with BenchmarkDotNet;
- for the rest, run a fixed scenario on a fixed model in the Revit version under test, before
  and after the change;
- record the Revit version, the model, the hardware and the scenario in the pull request.

## Reporting

- A `perf:` commit carries the before and after numbers and the environment in its body.
- Results are reported in the pull request and not committed; benchmark output is generated
  data.
- No thresholds are enforced in CI; a regression is caught in review, not by a red build.

The rest of the workflow is defined by the [development policy](development.md); what must be
tested is in the [testing policy](testing.md).
