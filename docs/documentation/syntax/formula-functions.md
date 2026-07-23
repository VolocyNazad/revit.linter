---
aliases:
  - Функции языка формул
tags:
  - documentation
  - formulas
---

# Функции общего назначения

Эти функции доступны в формулах документа и элемента: `takeDocument`, `check`, `groupBy`, а также `take` модуля проверок параметров проекта.

## Логические функции

| Функция | Результат | Пример |
| --- | --- | --- |
| `if(condition, whenTrue, whenFalse)` | Одно из двух значений | `if(true, 'yes', 'no')` → `'yes'` |
| `isnull(value)` | Значение равно `null` | `isnull(property('Missing'))` |
| `isdouble(value)` | Значение является числом | `isdouble(1)` |
| `isstring(value)` | Значение является строкой | `isstring('text')` |
| `isbool(value)` | Значение является `bool` | `isbool(false)` |
| `isnullorempty(value)` | Строка пустая или равна `null` | `isnullorempty('')` |

## Строковые функции

Проверки регистра (`contains`, `startwith`, `endwith`) чувствительны к регистру.

| Функция | Результат | Пример |
| --- | --- | --- |
| `str(value)` | Строковое представление в invariant culture | `str(12.5)` → `'12.5'` |
| `contains(text, part)` | Содержит ли строка фрагмент | `contains('Revit Linter', 'Linter')` |
| `startwith(text, prefix)` | Начинается ли строка с фрагмента | `startwith('Revit', 'Rev')` |
| `endwith(text, suffix)` | Заканчивается ли строка фрагментом | `endwith('Revit', 'vit')` |
| `tolower(text)` | Нижний регистр | `tolower('ReViT')` → `'revit'` |
| `toupper(text)` | Верхний регистр | `toupper('ReViT')` → `'REVIT'` |
| `totitle(text)` | Регистр заголовка | `totitle('revit linter')` → `'Revit Linter'` |
| `tosentence(text)` | Регистр предложения | `tosentence('revit linter')` → `'Revit linter'` |

## Арифметические функции

| Функция | Назначение | Пример |
| --- | --- | --- |
| `roundup(number)` | Округление вверх | `roundup(1.1)` → `2` |
| `rounddown(number)` | Округление вниз | `rounddown(1.9)` → `1` |
| `round(number, digits)` | Округление от нуля до указанного числа знаков | `round(1.25, 1)` → `1.3` |
| `sqrt(number)` | Квадратный корень | `sqrt(81)` → `9` |
| `sin(number)` | Синус, аргумент в радианах | `sin(0)` → `0` |
| `cos(number)` | Косинус, аргумент в радианах | `cos(0)` → `1` |
| `tan(number)` | Тангенс, аргумент в радианах | `tan(0)` → `0` |
| `num(text)` | Преобразование строки в число в invariant culture | `num('12.5')` → `12.5` |

## Дата и время

`now(format)` возвращает локальные дату и время в формате .NET.

```text
now('yyyy-MM-dd')
'Проверено: ' + now('dd.MM.yyyy HH:mm')
```

Использование формул: [[user-diagnostics|пользовательские проверки]], [[collision-diagnostics|проверки коллизий]], [[project-parameter-diagnostics|проверки параметров проекта]]. См. также: [[formula-syntax|Синтаксис]], [[formula-revit|Объекты Revit]].
