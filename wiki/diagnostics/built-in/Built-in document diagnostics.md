---
lang: en
---

> Language: **English** · [[Встроенные проверки документа|Русский]]

**Identifier convention:** `DOC` — diagnostics related to the document as a whole; `RVT` — checks driven by Revit itself (warnings, Performance Adviser, Purge Unused).

| ID         | Description                                     | Level      | Example message                                                             |
| :--------- | :------------------------------------------- | :----------- | :--------------------------------------------------------------------------- |
| **DOC001** | Checks whether the document has a starting view set (projects only). | 💬 Message | `Document 'Проект_А.rvt' has no starting view. (10 ms)` |
| **DOC002** | Checks whether family lookup tables are referenced by formulas (family documents only). | ⚠️ Warning | `Lookup table 'Sizes' in family 'Fitting.rfa' is not referenced by any formula. (10 ms)` |
| **DOC003** | Checks whether a preview view is set for the document (projects and families). | 💬 Message | `Document 'Model.rvt' has no preview view. (10 ms)` |
| **DOC004** | Checks whether the document has several project parameters with the same name (projects only). | ⚠️ Warning | `2 project parameters share the name 'Марка'. Their definitions differ, so the Properties palette and schedules show separate fields with one name. Categories bound to more than one definition: “Стены”. (10 ms)` |
| **DOC005** | Checks whether family formulas reference a lookup table that exists in the family (family documents only). | ⚠️ Warning | `Formula of parameter 'L' in family 'Fitting.rfa' references lookup table 'Sizes', which is not in the family. (10 ms)` |
| **DOC006** | Checks whether family formulas reference columns that exist in the lookup table (family documents only). | ⚠️ Warning | `Formula of parameter 'L' in family 'Fitting.rfa' references column 'Z1', which is not in lookup table 'Sizes'. (10 ms)` |
| **DOC007** | Checks whether a size lookup formula uses a table or column name that cannot be determined statically (family documents only). | 💬 Message | `The lookup table name of the formula of parameter 'L' in family 'Fitting.rfa' cannot be determined statically. The table checks cannot verify this formula; check it by hand. (10 ms)` |
| **RVT002** | Checks the document with the built-in Revit Performance Adviser rules (projects and families). | ⚠️ Warning | `Performance Adviser reported a problem in document 'Проект_А.rvt'. (10 ms)` |
| **RVT001** | Checks for Revit warnings (projects only). | ⚠️ Warning | `Warnings were found in document 'Проект_А.rvt' (10 ms)` |

`DOC002` has a fix that deletes every unused lookup table. It is offered only when every table name in formulas is a literal: a table name from a parameter can change in a project, so the family document cannot prove the table is unused. The fix asks for confirmation before changing the family.

> **Note:** `DOC005` and `DOC006` read a table or column name written in the formula or supplied by a text parameter, type or instance. For an instance parameter they read the family default value; an instance can override it. They do not evaluate an expression such as `IF(...)` written inside the formula; `DOC007` reports it. An expression in a separate text parameter is read through its computed value.

---

See [[Diagnostic severity|severity levels]] for the meaning of each result level.
