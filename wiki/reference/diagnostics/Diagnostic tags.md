---
lang: en
---

> Language: **English** · [[Теги диагностик|Русский]]

# Diagnostic tags

A tag is a plain string attached to a diagnostic. Tags name a diagnostic and let you find it; they do not change how it runs. They appear in the **Tags** column of the [[Diagnostics pane]] and the [[Diagnostic reports pane]], are searched by typing `#` before a tag name on either pane, can be clicked to search for the tag, and are written to every exported report.

## Searching by tag

Type `#` before a tag name in the search box. The name must match a tag exactly, ignoring case, and several tags narrow the list together, for example `#Project #Family`. The **Aa** and **ab|** buttons next to the search box do not change the tag search. Text without `#` searches the code and the text of the row: the description on the diagnostics list and the message on the report. Clicking a tag in the **Tags** column puts `#tag` in the search box. A tag whose name contains spaces is written in quotes, for example `#"fire alarm"`.

Two kinds of tags exist.

## User tags

Written by the rule author in the `tags` list of a configuration file and kept as written. A rule without `tags` has none. Use them for your own conventions, such as a discipline or a system.

## System tags

Set by the product. They are always English and look the same in every interface language. A user tag that reuses a system tag name is ignored, so a configuration file cannot fake one.

| Tag | Meaning |
| :--- | :--- |
| `BuiltIn` | The check ships with Revit Linter. |
| `Custom` | A user-defined check from `diagnostics.config.yaml`. |
| `Collision` | A collision check from `diagnostics.config.yaml`. |
| `ParameterProject` | A project parameter check from `diagnostics.config.yaml`. |
| `Example` | The check came from the installed example configurations. |
| `Tour` | The managed practical-tour training check. |
| `Debug` | The check came from a debug configuration. |

`Project` and `Family` are the document kinds a diagnostic applies to. The product sets them on a built-in check from its document filter; on a configured rule they are ordinary tags the author writes in `tags`, because only the rule knows where it applies. They are not reserved.

`Shared`, `Systems` and `Architecture` are the discipline tags. The product sets them on a built-in check from the prefix of its code (`SHRD`, `SYST` or `ARCH`); a check whose code has no such prefix, such as a `DOC` or `RVT` check, gets none. On a configured rule they are ordinary tags the author writes in `tags`, so the installed examples spell them in the language of the configuration file: `Systems` in English and `Системы` in Russian. They are not reserved.

## How the tag list of a diagnostic is built

- A built-in diagnostic gets `BuiltIn`, then its discipline tag when its code has one of the prefixes above, then the document kinds it applies to (`Project`, `Family` or both).
- A rule loaded from a configuration file gets the tag of its declared `type` (`Custom`, `Collision` or `ParameterProject`) and, when the product installed the rule, the provenance tag (`Example`, `Tour` or `Debug`). The author's `tags` from the file follow; the installed examples begin them with the discipline tag of the block.
- The system tags come first in declaration order, then the user tags in the order they are written in the file.

Before the list is built, tags are trimmed, empty tags are dropped and duplicates are removed ignoring case, so `MEP` and `mep` are one tag.

## See also

- [[Diagnostic parameters]] — the `tags` field of a rule.
- [[Diagnostics pane]] — finding diagnostics by tag.
