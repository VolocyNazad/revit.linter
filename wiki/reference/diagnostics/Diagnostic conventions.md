---
lang: en
---

> Language: **English** · [[Соглашения диагностики|Русский]]

**Identifier convention:**
`SYST` — system diagnostics, `SHRD` — shared element diagnostics, `ARCH` — architectural diagnostics, `DOC` — diagnostics related to the document as a whole, `RVT` — checks driven by Revit itself.

The number has three or four digits and is unique within its prefix.

Each diagnostic's identifier must be unique

## 🏷️ Identifier Prefixes

|Prefix|Category|Diagnostic type|Discipline tag|
|:---|:---|:---|:---|
|`SYST`|System diagnostics for engineering systems (ducts, pipes, cable trays, electrical, plumbing)|`ElementDiagnosticId`|`Systems`|
|`SHRD`|Shared element diagnostics (families, views, levels, links)|`ElementDiagnosticId`|`Shared`|
|`ARCH`|Architectural diagnostics (rooms, walls)|`ElementDiagnosticId`|`Architecture`|
|`DOC`|Document diagnostics (starting view, etc.)|`DocumentDiagnosticId`|-|
|`RVT`|Checks driven by Revit itself (warnings, Performance Adviser, Purge Unused)|`ElementDiagnosticId` and `DocumentDiagnosticId`|-|

`RVT001` and `RVT002` are document diagnostics; `RVT003` is an element diagnostic.

The product puts the discipline tag on a built-in diagnostic. A configured rule writes it in its own `tags`, in the language of the configuration file; see [[Diagnostic tags]].
