---
aliases:
  - Formula Language Syntax
tags:
  - documentation
  - formulas
lang: en
---

> Language: **English** · [[Синтаксис формул|Русский]]

## Values

| Type | Syntax | Examples |
| --- | --- | --- |
| Number (`double`) | integer or decimal notation with a point | `0`, `-3`, `12.5` |
| String (`string`) | single quotes | `'Wall 01'`, `''` |
| Boolean (`bool`) | `true` or `false` | `true` |
| Empty value | `null` | `null` |
| Number π | `pi` | `2 * pi` |

### String escaping

`\'`, `\\`, `\n`, `\r`, `\t`, `\f`, and `\b` are supported. An unknown escape sequence is preserved unchanged.

```text
'it\'s'       // it's
'a\\b'        // a\b
'line1\nline2'
```

## Operators

Operators are listed from highest to lowest precedence.

| Precedence | Operators | Purpose |
| ---: | --- | --- |
| 1 | `^` | Exponentiation |
| 2 | `*`, `/`, `%` | Multiplication, division, remainder |
| 3 | `+`, `-` | Addition/concatenation, subtraction |
| 4 | `!` | Logical NOT |
| 5 | `>`, `>=`, `<`, `<=` | Numeric comparison |
| 6 | `==`, `!=` | Equality and inequality |
| 7 | `&` | Logical AND |
| 8 | `\|` | Logical OR |

```text
2 + 3 * 4              // 14
(2 + 3) * 4            // 20
true | false & false   // true
!(1 > 2)               // true
```

`+` adds numbers and concatenates strings. If at least one operand is a string, the other is converted to a string. When adding a string to `null`, the empty value becomes an empty string; `null + null` returns `null`.

Numbers in `==` and `!=` are compared with a tolerance of about `1e-9`. The `&` and `|` operators evaluate both operands.

> [!note]
> Division follows `double` behavior: for example, `1 / 0` returns positive infinity.

Formula usage: [[User diagnostics|user diagnostics]], [[Collision diagnostics|collision diagnostics]], [[Project parameter diagnostics|project parameter diagnostics]]. See also: [[Formula functions|Functions]], [[Formula Revit|Revit objects]].
