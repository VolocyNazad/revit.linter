# Testing policy

Paths mentioned in this policy are relative to the repository root unless a Markdown link specifies otherwise.

Tests are the evidence that a change works, and on which Revit versions. The repository
separates tests by what they need to run: a headless process, a running Revit, or a person
in front of Revit.

## Test kinds

| Kind | Project suffix | Runs in | May touch |
| --- | --- | --- | --- |
| Headless | `*.Tests` | a normal process, including a hosted runner | pure code, temporary files, Revit API types that do not need a running Revit |
| Revit integration | `*.RevitTests` | a machine with the Revit version under test installed and licensed | `Document`, transactions, collectors, selection, services that need a live API context |
| Manual smoke | - | Revit, by a person | ribbon, windows, dialogs, add-in loading and unloading |

The suffix is a contract: CI and the release checklist select tests by it, and a project
without `*.Tests` or `*.RevitTests` is not a test project. Prefer the product prefix as well
(`<Product>.Tests`, `<Product>.RevitTests`), but the suffix is what matters.

## Revit versions

Code that compiles differently per Revit version (`VERSION_2025`, `IS2025`, `BEFORE2026`, ...)
or calls the Revit API must be verified on every supported version. The supported set is the
`Release_*` configurations of the solution; today that is `Release_2021.1.100`,
`Release_2022.1.80`, `Release_2023.1.90`, `Release_2024.3.60`, `Release_2025.4.60`,
`Release_2026.4.10` and `Release_2027.3.0`.

- Build and headless tests run on every supported configuration in CI, for every pull request.
- `*.RevitTests` run outside CI. CI compiles them; they are executed on a machine with Revit
  installed.
- Before a release tag the full matrix is executed: every `*.RevitTests` project on every
  supported version, with the results recorded in the release checklist.
- A pull request that changes version-specific code runs those tests on the versions it
  touches; the full matrix is still required before the release.
- Every `#if` branch is behaviour: a test that compiles only one branch does not cover the
  other.
- A repository that does not compile against the Revit API is exempt from the version matrix,
  not from testing.

## New Revit version

Adding support is one change: the configuration in the solution, the SDK support, the CI
matrix entry, the tests, and a `*.RevitTests` run on that version. Until then the version is
not supported and must not be published.

## What needs a test

- A bug fix comes with a regression test that fails without the fix.
- A behaviour change comes with a test of the new behaviour, not only proof that nothing
  crashes.
- MSBuild and SDK logic is tested on a temporary project.
- Analyzers and generators are tested through the diagnostic or the generated code.
- Revit-bound logic is covered by `*.RevitTests`; a headless test is added wherever the logic
  can be detached from `Document`.

Documentation, formatting, dependency updates and purely visual XAML changes do not need a
test, but the build must pass.

## Isolation and determinism

- Headless tests do not open Revit, do not use the network and do not write into the
  repository; temporary data goes to a temporary directory.
- `*.RevitTests` create their document from a template and close it in the fixture; they never
  depend on a document the user has open.
- No dialogs and no waiting for input in any test.
- A test does not depend on execution order, locale, registry state or installed add-ins,
  except for the Revit version it targets.

## Running the tests

Headless: `dotnet test tests/<Project>.Tests/<Project>.Tests.csproj --configuration
Release_<version>`, or the built executable, which is what CI uses.

Revit integration: on a machine with the required Revit version installed and licensed:

```powershell
dotnet test tests/<Project>.RevitTests/<Project>.RevitTests.csproj --configuration Release_<version>
```

Record the version and the result in the pull request or the release checklist.

## Manual smoke check

For a change that affects the user interface or the add-in lifecycle, check in Revit: the
add-in loads, the ribbon entry appears, the main scenario works, a second load and unload does
not fail, and the Revit journal has no exceptions. Run it on at least one supported version,
and on every version whose code path changed.

## Flaky and skipped tests

A test that fails intermittently is fixed or quarantined with an issue and a date. A skipped
test has a reason and an issue; an unexplained skip is a defect.

## Coverage

Every behaviour a consumer of the library or add-in can observe has a test. No numeric
threshold is enforced: a percentage that moves without a behaviour change is not a goal.

The workflow around tests - branches, commit messages and the changelog - is defined by the
[development policy](development.md).
