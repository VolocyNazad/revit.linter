**Identifier convention:**
`SYST` — system diagnostics, `SHRD` — shared element diagnostics, `ARCH` — architectural diagnostics, `DOC` — diagnostics related to the document as a whole.

The number `001`…`999` is unique within its prefix.

Each diagnostic's identifier must be unique

## 🏷️ Identifier Prefixes

|Prefix|Category|Diagnostic type|
|:--|:--|:--|
|`SYST`|System diagnostics (connectors, links)|`ElementDiagnosticId`|
|`SHRD`|Shared element diagnostics (families, views, levels)|`ElementDiagnosticId`|
|`ARCH`|Architectural diagnostics (rooms, walls)|`ElementDiagnosticId`|
|`DOC`|Document diagnostics (starting view, etc.)|`DocumentDiagnosticId`|
