---
lang: en
---

> Language: **English** · [[Встроенные проверки документа|Русский]]

**Identifier convention:** `DOC` — diagnostics related to the document as a whole; `RVT` — checks driven by Revit itself (warnings, Performance Adviser, Purge Unused).

| ID         | Description                                     | Level      | Example message                                                             |
| :--------- | :------------------------------------------- | :----------- | :--------------------------------------------------------------------------- |
| **DOC001** | Checks whether the document has a starting view set (projects only). | 💬 Message | `Document named 'Проект_А.rvt' has no starting view set. (10 ms)` |
| **DOC002** | Checks whether family size lookup tables are referenced by family types (family documents only). | 💬 Message | `Size lookup table 'Sizes' in family 'Fitting.rfa' is not referenced by any family type. (10 ms)` |
| **DOC003** | Checks whether a preview view is set for the document (projects and families). | 💬 Message | `Document 'Model.rvt' has no preview view. (10 ms)` |
| **RVT001** | Checks for Revit warnings (projects only). | ⚠️ Warning | `Warnings were found in document 'Проект_А.rvt' (10 ms)` |

---

See [[Diagnostic severity|severity levels]] for the meaning of each result level.
