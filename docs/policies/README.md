# Repository policies

Read the policy for your area before changing it; [development policy](development.md) is the entry
point and links the rest.

Facts belong in the [repository guide](../repository.md), architecture decisions in
[architecture decisions](../architecture.md), user-visible behaviour in [product scope](../FEATURES.md),
and future work in [TODO.md](../../TODO.md).

## Conventions

- Policies are written in English.
- Every policy has a `Read when:` line after the paths sentence.
- Every policy starts with the sentence "Paths mentioned in this policy are relative to the
  repository root unless a Markdown link specifies otherwise.".
- Policy links are relative Markdown links; an anchor must point at an existing heading, checked by
  `scripts/Validate-Repository.ps1`.
- A policy ends by pointing to the [development policy](development.md).
- A policy owns exactly one subject. Rules owned by another policy are linked, not repeated.

## Policy index

| Policy | Covers | Enforced by |
| --- | --- | --- |
| [Development policy](development.md) | Entry point: repository rules, asking before a change, code basics, links to every policy | Review |
| [Branching policy](branching.md) | Branch kinds, base branches, merge strategy, protected branches | Review; branch protection is configured in GitHub, not in the repository |
| [Commit message policy](commits.md) | Conventional Commits, types, scope, body and footers | Review |
| [Changelog policy](changelog.md) | `CHANGELOG.md` format, entry modes, release section, release history window and archive | `scripts/Verify-Release.ps1` in `.github/workflows/repository-policy.yml` and `.github/workflows/publish-release.yml`; the same script also checks the release history window and the archive |
| [Testing policy](testing.md) | Test kinds, Revit version matrix, what needs a test, determinism, running tests | `.github/workflows/build-and-test.yml`, `scripts/Invoke-HeadlessTests.ps1` and the matrix resolved from `Directory.Build.props`; Revit integration runs are manual |
| [Benchmark policy](benchmarks.md) | Benchmark kinds, when to measure, reporting | Review; benchmarks deliberately do not run in CI |
| [Wiki policy](wiki.md) | Wiki as a mirror of `wiki/`, publication rules | `.github/workflows/publish-wiki.yml`, `scripts/Prepare-Wiki.ps1` |
| [Release policy](releases.md) | Release channels, tags, versioning, release metadata, MSI artifacts, publish automation | `.github/workflows/publish-release.yml` and `scripts/Verify-Release.ps1` check the tag and the changelog |
| [Logging policy](logging.md) | Log levels, one log per failure, structured messages, privacy, failed diagnostics | Review |
| [Localization policy](localization.md) | Neutral language and cultures, resource ownership, RESX validation, satellites | `VolocyNazad.ResxAnalyzer` (`RESX001`-`RESX007`) and the localization tests in the release build |
| [Presentation policy](presentation.md) | WPF views and view models, design-time data, shared styles and resources, help helpers | `Revit.Linter.Architecture.Tests` for composition; the rest is review |
| [Migration policy](migrations.md) | Migration lifecycle, version window, data safety, removal gate | Review; the registry in `docs/migrations/` is the source of truth for entries |
| [Build and toolchain policy](build.md) | .NET SDK selection, build units, central packages, Revit version constants, warnings, Roslyn baseline | `.github/workflows/build-and-test.yml` (warnings as errors); the release build treats warnings as errors |
| [Repository maintenance policy](repository-maintenance.md) | Solution items, required root files, repository validation, formatting baseline | `scripts/Validate-Repository.ps1` through `.github/workflows/repository-policy.yml`; the validator also checks action pins and the Revit version matrix |
| [Documentation policy](documentation.md) | Document genres, single source of truth, updates in the same change, XML documentation, language, links, glossary | Review; the repository validator checks links and anchors |
| [Pull request policy](pull-requests.md) | Source branch and target, required checks, review, author checklist | Review; `CI gate` in `.github/workflows/build-and-test.yml` and `.github/workflows/repository-policy.yml` are the required checks; branch protection is configured in GitHub |

## Adding or changing a policy

1. Add or edit the file in `docs/policies/`. Keep one subject per policy.
2. Add the row to the [policy index](#policy-index) above.
3. Add the file to the `/docs/policies/` solution folder in `Revit.Linter.slnx`, or the validator
   fails.
4. Link the policy from [development policy](development.md),
   [CONTRIBUTING.md](../../CONTRIBUTING.md), the pull request template or the area guide; keep no
   second copy of the full list.
5. Run `./scripts/Validate-Repository.ps1`.

When an automated check replaces a policy rule, say so in the "Enforced by" column. Fix a policy that
is no longer enforced instead of leaving a dead rule.
