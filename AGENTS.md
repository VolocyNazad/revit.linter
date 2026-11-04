# AGENTS.md

This repository keeps its rules and facts in documents, not in this file. Load only what your change
needs: the table routes you, and each document says what it owns.

| Your change touches | Read first | Then, only when the change crosses that area |
| --- | --- | --- |
| Anything | [Development policy](docs/policies/development.md) — asking before a change, code basics | The [policy index](docs/policies/README.md): pick the one policy that covers the area |
| Project structure, dependencies, technology stack, build, installer, updater | [Repository guide](docs/repository.md) — the section for that area | [Architecture decisions](docs/architecture.md) when module boundaries, references or host seams change |
| A built-in diagnostic | [Adding a built-in diagnostic](docs/diagnostics/adding.md) (Russian) | [Testing policy](docs/policies/testing.md) when the change needs a test |
| User-visible text or behaviour | The English page under `wiki/`, then its `wiki/ru/` mirror after the English edit is final | [Wiki policy](docs/policies/wiki.md) for page, link and publication rules |
| Release, version, MSI, `CHANGELOG.md` | [Release policy](docs/policies/releases.md) and [changelog policy](docs/policies/changelog.md) | [Build and toolchain policy](docs/policies/build.md) |
| A pull request | [Pull request policy](docs/policies/pull-requests.md) | [Commit message policy](docs/policies/commits.md) and [branching policy](docs/policies/branching.md) |

## How to load context

- Open sections, not whole files. The [repository guide](docs/repository.md) and [architecture
  decisions](docs/architecture.md) are references for the area you touch; reading them end to end is
  not required.
- Documents under [docs/policies/](docs/policies/) are binding rules; the [policy
  index](docs/policies/README.md) says what each one owns. Read the policy that covers your area and
  the [development policy](docs/policies/development.md), not all of them.
- Follow the owning link instead of loading every document: rules are in the policies, facts about
  the repository in the [repository guide](docs/repository.md), decisions in [architecture
  decisions](docs/architecture.md), product scope in [docs/FEATURES.md](docs/FEATURES.md), vocabulary
  in [GLOSSARY.md](GLOSSARY.md).
- User documentation is bilingual. Read and edit the English page first, then bring the `wiki/ru/`
  mirror in step; do not read both pages before the English text is final.
- Do not read generated or ignored directories for context: `output/`, `benchmark/BenchmarkDotNet.Artifacts/`,
  `bin/`, `obj/`, `.vs/`, `.idea/`, `TestResults/`.
- Verify before finishing: [scripts/Validate-Repository.ps1](scripts/Validate-Repository.ps1) checks
  links, anchors, required files and solution items; [scripts/Verify-Release.ps1](scripts/Verify-Release.ps1)
  checks the `CHANGELOG.md` structure.
