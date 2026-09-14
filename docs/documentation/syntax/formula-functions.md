---
aliases:
  - Formula Language Functions
tags:
  - documentation
  - formulas
---

# General-Purpose Functions

These functions are available in document and element formulas: `takeDocument`, `check`, `groupBy`, and also `take` in the project parameter diagnostics module.

## Logical functions

| Function | Result | Example |
| --- | --- | --- |
| `if(condition, whenTrue, whenFalse)` | One of two values | `if(true, 'yes', 'no')` → `'yes'` |
| `isnull(value)` | The value equals `null` | `isnull(property('Missing'))` |
| `isdouble(value)` | The value is a number | `isdouble(1)` |
| `isstring(value)` | The value is a string | `isstring('text')` |
| `isbool(value)` | The value is a `bool` | `isbool(false)` |
| `isempty(value)` | The value is an empty string | `isempty('')` → `true` |
| `isnullorempty(value)` | The string is empty or equals `null` | `isnullorempty('')` |

`isempty` and `isnullorempty` distinguish between an empty string and the absence of a value:

```text
isempty('')             // true
isempty(null)           // false
isnullorempty('')       // true
isnullorempty(null)     // true
```

## String functions

Case checks (`contains`, `startwith`, `endwith`) are case-sensitive.

| Function | Result | Example |
| --- | --- | --- |
| `str(value)` | String representation in invariant culture | `str(12.5)` → `'12.5'` |
| `contains(text, part)` | Whether the string contains a fragment | `contains('Revit Linter', 'Linter')` |
| `startwith(text, prefix)` | Whether the string starts with a fragment | `startwith('Revit', 'Rev')` |
| `endwith(text, suffix)` | Whether the string ends with a fragment | `endwith('Revit', 'vit')` |
| `tolower(text)` | Lowercase | `tolower('ReViT')` → `'revit'` |
| `toupper(text)` | Uppercase | `toupper('ReViT')` → `'REVIT'` |
| `trim(text)` | Removes whitespace at the start and end of a string | `trim('  Revit  ')` → `'Revit'` |
| `replace(text, old, new)` | Replaces all occurrences of a fragment | `replace('Revit Linter', 'Linter', 'Rules')` → `'Revit Rules'` |
| `length(text)` | Returns the length of a string | `length('Revit')` → `5` |
| `substring(text, start, length)` | Returns a fragment of a string | `substring('Revit Linter', 6, 6)` → `'Linter'` |
| `totitle(text)` | Title case | `totitle('revit linter')` → `'Revit Linter'` |
| `tosentence(text)` | Sentence case | `tosentence('revit linter')` → `'Revit linter'` |

`replace` is case-sensitive. In `substring`, the `start` position is zero-based. The position and length must be non-negative integers, and the requested fragment must not go beyond the bounds of the string. If these conditions are violated, formula evaluation fails with an error.

Like other numbers in the language, the result of `length` has type `double`.

## Arithmetic functions

| Function | Purpose | Example |
| --- | --- | --- |
| `roundup(number)` | Round up | `roundup(1.1)` → `2` |
| `rounddown(number)` | Round down | `rounddown(1.9)` → `1` |
| `round(number, digits)` | Round away from zero to the specified number of digits | `round(1.25, 1)` → `1.3` |
| `sqrt(number)` | Square root | `sqrt(81)` → `9` |
| `abs(number)` | Absolute value of a number | `abs(0 - 3)` → `3` |
| `min(a, b)` | The smaller of two numbers | `min(2, 5)` → `2` |
| `max(a, b)` | The larger of two numbers | `max(2, 5)` → `5` |
| `sin(number)` | Sine, argument in radians | `sin(0)` → `0` |
| `cos(number)` | Cosine, argument in radians | `cos(0)` → `1` |
| `tan(number)` | Tangent, argument in radians | `tan(0)` → `0` |
| `num(text)` | Converts a string to a number in invariant culture | `num('12.5')` → `12.5` |

## Date and time

`now(format)` returns the local date and time in .NET format.

```text
now('yyyy-MM-dd')
'Checked: ' + now('dd.MM.yyyy HH:mm')
```

Formula usage: [[user-diagnostics|user diagnostics]], [[collision-diagnostics|collision diagnostics]], [[project-parameter-diagnostics|project parameter diagnostics]]. See also: [[formula-syntax|Syntax]], [[formula-revit|Revit objects]].
