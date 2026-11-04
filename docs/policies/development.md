# Development policy

Paths mentioned in this policy are relative to the repository root unless a Markdown link specifies otherwise.

Read when: you start a change; it owns the asking rule, the stack priority and the code basics.

The [repository guide](../repository.md) stack is the default; the usage in the affected part of the
repository comes first. Never substitute an alternative silently — if it seems better, ask the user,
explain the reason, and wait for confirmation.

If a change affects the folder structure or the technology stack (a new/removed project, a new
dependency, a version bump worth recording, a new convention) — update the relevant policy and
[repository guide](../repository.md) in the same change.

Before changing or writing tests, ask the user first — clarify what and how it should be covered (or
that the change is trivial enough to need no clarification).

Do not `git push` — commit locally and leave the push to the user unless they explicitly ask.

## Where the rules live

The [policy index](README.md) is the full list. The policies a change usually touches:

| Topic | Policy |
| --- | --- |
| Branch, base branch, merge strategy | [Branching policy](branching.md) |
| Commit message | [Commit message policy](commits.md) |
| `CHANGELOG.md` | [Changelog policy](changelog.md) |
| Pull request, review, required checks | [Pull request policy](pull-requests.md) |
| Tests and Revit version matrix | [Testing policy](testing.md) |
| Benchmarks | [Benchmark policy](benchmarks.md) |
| Release, tags, versioning, MSI | [Release policy](releases.md) |
| Logging and failure reporting | [Logging policy](logging.md) |
| Localized resources and cultures | [Localization policy](localization.md) |
| WPF views, view models, design-time data | [Presentation policy](presentation.md) |
| User-visible migrations | [Migration policy](migrations.md) |
| .NET SDK, build units, packages, warnings | [Build and toolchain policy](build.md) |
| Solution items, validator, formatting | [Repository maintenance policy](repository-maintenance.md) |
| Contributor documentation and XML docs | [Documentation policy](documentation.md) |
| Published user documentation | [Wiki policy](wiki.md) |

Module boundaries, abstractions and dependency injection follow the stable rules in
[architecture decisions](../architecture.md). When a change introduces or revises a fundamental
architectural decision, update that document in the same change.

## Code basics

Do not invent abbreviations in identifiers, file names or documentation. Use complete words such as `ViewModel` and `Configuration`; established terms such as `API`, `WPF`, `XML`, `Guid`, `Id` and `Uri` remain valid.

Use `global using` only when most files in that project need the namespace. Keep specialized namespaces local, and do not repeat namespaces already supplied by `ImplicitUsings`. Apply this rule to new and touched code; clean up existing files only within the task scope.

Keep [CHANGELOG.md](../../CHANGELOG.md) in the state the [changelog policy](changelog.md) requires:
regular development leaves `## [Unreleased]` empty, a change carries its entry as a fragment in
`changelog.d/`, and the release section is composed when a release is prepared.
