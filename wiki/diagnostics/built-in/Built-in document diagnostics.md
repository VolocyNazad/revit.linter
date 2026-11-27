---
lang: en
---

> Language: **English** · [[Встроенные проверки документа|Русский]]

**Identifier convention:** `DOC` — diagnostics related to the document as a whole; `RVT` — checks driven by Revit itself (warnings, Performance Adviser, Purge Unused).

| ID         | Description                                     | Level      | Example message                                                             |
| :--------- | :------------------------------------------- | :----------- | :--------------------------------------------------------------------------- |
| **DOC001** | Checks whether the document has a starting view set (projects only). | 💬 Message | `Document 'Проект_А.rvt' has no starting view. (10 ms)` |
| **DOC002** | Checks whether family lookup tables are referenced by formulas or by the lookup table name of a family type (family documents only). | ⚠️ Warning | `Lookup table 'Sizes' in family 'Fitting.rfa' is not referenced by any formula and is not named by any family type. (10 ms)` |
| **DOC003** | Checks whether a preview view is set for the document (projects and families). | 💬 Message | `Document 'Model.rvt' has no preview view. (10 ms)` |
| **DOC004** | Checks whether the document has several project parameters with the same name (projects only). | ⚠️ Warning | `2 project parameters share the name 'Марка'. Their definitions differ, so the Properties palette and schedules show separate fields with one name. Categories bound to more than one definition: “Стены”. (10 ms)` |
| **DOC005** | Checks whether family formulas and the lookup table name of a family type reference lookup tables that exist in the family (family documents only). | ⚠️ Warning | `Formula of parameter 'L' in family 'Fitting.rfa' references lookup table 'Sizes', which is not in the family. (10 ms)` |
| **DOC006** | Checks whether family formulas reference columns that exist in the lookup table (family documents only). | ⚠️ Warning | `Formula of parameter 'L' in family 'Fitting.rfa' references column 'Z1', which is not in lookup table 'Sizes'. (10 ms)` |
| **RVT002** | Checks the document with the built-in Revit Performance Adviser rules (projects and families). | ⚠️ Warning | `Performance Adviser reported a problem in document 'Проект_А.rvt'. (10 ms)` |
| **RVT001** | Checks for Revit warnings (projects only). | ⚠️ Warning | `Warnings were found in document 'Проект_А.rvt' (10 ms)` |

`DOC002` has a fix that deletes every unused lookup table. It asks for confirmation before changing the family.

---

See [[Diagnostic severity|severity levels]] for the meaning of each result level.
