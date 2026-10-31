# Localization policy

Paths mentioned in this policy are relative to the repository root unless a Markdown link specifies otherwise.

User-facing text is part of the product contract: it appears in Revit, in the diagnostic report and
in the Undo history. Every translatable string lives in a resource file owned by the module that
shows it, resolves through `LocalizationResourceReader`, and ships in both supported cultures in the
same change that introduces it.

## Languages and cultures

- English is the neutral resource language: the base `Type.resx` holds English text, and the
  localization project declares `<NeutralLanguage>en</NeutralLanguage>` so English is the fallback
  for any UI culture without resources.
- Russian is the only translated culture, shipped as `Type.ru.resx` under the parent `ru` culture. A
  regional UI culture such as `ru-RU` or `ru-KZ` resolves through its parent to `ru`; it does not get
  its own resource file. Do not add a file for a regional variant of an existing language.
- A UI culture without matching resources falls back to the neutral English resource. Keep the
  neutral file English and the Russian text in the satellite; do not put translated text in the
  neutral resource.

## Resource ownership

- A user-facing string lives in a resource file of the project that owns the feature, next to the
  type that reads it. Do not keep strings for another module.
- `Revit.Linter.Localization` aggregates the resources: it embeds every source `.resx` owned by
  feature and presenter projects, and it owns the shared vocabulary resources (severity, target
  type, report filter, help tooltips). Ownership of the text stays with the feature; embedding and
  validation are centralized.
- `Revit.Linter.Localization` also owns `LocalizationResourceReader` and the `*Localizations`
  helpers. Consumers resolve strings through those types, not by opening a resource file or
  constructing a `ResourceManager`.

## Resource files and build

- Name the neutral resource `Type.resx` and the Russian one `Type.ru.resx`, and place both next to
  `Type.cs`. Nesting (`Type.resx` under `Type.cs`, `Type.ru.resx` under `Type.resx`) is configured
  once in the solution-root `.filenesting.json`; a new resource file or language needs no nesting
  change.
- A project other than `Revit.Linter.Localization` does not list `.resx` files: the root
  `Directory.Build.targets` removes them from `EmbeddedResource` and adds them as `None` there.
  `Revit.Linter.Localization` is the only project that lists and embeds them, each with a
  `LogicalName` that is its runtime base name; keep the file name and the `LogicalName` in step when
  a resource file moves.
- A project that generates typed localized properties sets
  `<UsesLocalizationGenerator>true</UsesLocalizationGenerator>`. The same targets file passes its
  neutral `*.resx` files to the generator as `AdditionalFiles` and excludes `*.ru.resx`; do not pass
  satellites or other translated files to the generator.
- In `Revit.Linter.Localization`, declare every new source `.resx` and its `ru` counterpart as an
  `EmbeddedResource`. The aggregation project is the registration point that the localization tests
  compare against; a resource file that is not declared there is not embedded and not tested.

## Resolving strings at runtime

- Resolve user-facing strings through `LocalizationResourceReader` with the fully qualified resource
  base name, for example `Revit.Linter.Localization.<Module>.<Type>`. Use `GetString` for a required
  string, `TryGetString` when absence is meaningful and `GetAllStrings` for enumeration. Most modules
  wrap the reader in a `*Localizations` helper; use that helper where it exists.
- The reader walks the current UI culture and each parent culture, then the invariant neutral
  resource. A missing key returns the key itself from `GetString` and `false` from `TryGetString`.
  Do not rely on the echoed key as user text: a missing translation is a defect to fix, not a
  fallback to display.
- The generated typed properties are thin wrappers over the reader. Apply
  `[GenerateLocalizedProperties]` to the partial type whose resource file is named after the type and
  whose namespace matches the nested base name; the generator builds the base name from the class
  namespace and name, so a renamed type or resource file changes the runtime lookup.
- `GlobalStringLocalizer` is the `IStringLocalizer<GlobalLocalizations>` implementation and delegates
  to the reader for the global resource. Do not add a parallel lookup path with its own
  `ResourceManager` or culture handling.

## User-facing names

- Revit transaction and transaction-group names are user-facing: Revit shows them in the Undo history
  and in the status bar while the transaction runs. Resolve them through
  `LocalizationResourceReader` or the owning module's `*Localizations` helper, never as hard-coded
  English text.
- A name created only by a test fixture or a benchmark is not user-facing and is exempt.
- The rule covers every transaction and transaction group a user can see or undo, including the
  temporary transactions of visualization and accent operations.

## Key and placeholder parity

- Every key in the neutral resource has the same key in the Russian resource, and the other way
  round. Add, rename or remove a key in both files in the same change.
- Each key carries the same set of placeholders in both cultures. Translate the text, not the
  placeholder names; a placeholder the translation does not use, or invents, is a parity failure.
- Keys are stable identifiers: do not translate a key, and rename it together with every consumer in
  the same change.

## Validation

- `VolocyNazad.ResxAnalyzer` runs during regular builds of `Revit.Linter.Localization` and reports
  `RESX001`-`RESX007`: missing, extra and duplicate keys, mismatched composite-format placeholders,
  empty translations, missing culture files, and satellites without a neutral resource. Keep the
  analyzer referenced by the aggregation project only; adding it globally would analyse incomplete
  per-project subsets and report missing culture files that are intentionally owned and embedded
  elsewhere.
- The analyzer complements the localization tests; it does not replace them. The tests verify key and
  placeholder parity for every source `.resx`, require every source resource to be declared by the
  aggregation project, and load all declared neutral resources plus the `ru` satellite from the final
  published artifacts.
- `RESX008` is intentionally disabled: existing keys use both `PascalCase` and `snake_case`. Enable a
  key naming convention only as a separately planned migration that updates the existing keys and
  their consumers together.
- A localization build warning or a failing localization test is fixed in the resource files or the
  aggregation declaration; do not suppress it.

## Satellites and publishing

- Localization satellites stay outside ILRepack. `Revit.Linter.Localization.dll` is listed in
  `RepackBinariesExcludes`, so the localization assembly and its `ru` satellite are not merged into
  the add-in.
- Keep `Revit.Linter.Localization.dll` excluded from repacking and ship its
  `ru/Revit.Linter.Localization.resources.dll` satellite next to it. Do not fold the resources into
  the merged add-in or move them to another project; the reader resolves them from the localization
  assembly.
- Every final ILRepack output is verified to load all declared neutral resources and the Russian
  satellite before MSI creation. A missing, misnamed or merged satellite fails the release build.
- The reader resolves `ru-RU` and `ru-KZ` through the `ru` parent culture and an unrelated culture
  through English. Keep this precedence when a culture is added or a resource file moves.
- The artifact test asserts that the published localization assembly carries `en` as its neutral
  resource language; keep `<NeutralLanguage>en</NeutralLanguage>` in the localization project.

## Adding strings

- A new user-facing string arrives with both cultures in the same change: add the key to the neutral
  `Type.resx`, add its Russian translation to `Type.ru.resx`, declare both files in
  `Revit.Linter.Localization.csproj`, and update the code that consumes the key.
- Add the string to the resource of the module that shows it. Do not put feature text into the shared
  vocabulary resources or into `GlobalLocalizations`.
- A new built-in diagnostic gets its description and messages in both languages in the same change,
  as [adding a built-in diagnostic](../diagnostics/adding.md) requires.
- When a string becomes unused, remove the key from both files and every consumer in the same change.
- Design-time data resolves localized text through the reader with the same base name the view model
  uses instead of copying literals into the design class; the full design-time rule belongs to the
  [presentation policy](presentation.md).

The rest of the contributor workflow is defined by the [development policy](development.md).
