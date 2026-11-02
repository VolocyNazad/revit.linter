---
aliases:
  - Revit Formulas
  - Revit Element Filters
tags:
  - documentation
  - formulas
  - revit
lang: en
---

> Language: **English** · [[Объекты Revit в формулах|Русский]]

## Properties and methods

`property(name)` reads a public property of the current object. In `takeDocument`, the current object is `Autodesk.Revit.DB.Document`; in `check`, it is the `Autodesk.Revit.DB.Element` being checked, of its actual type.

```text
!property('IsFamilyDocument')
property('Name') == 'Wall 01'
property('Width') > 0
```

`method(name)` calls a public parameterless method. The method must return a value and must not be generic.

```text
!isnull(method('GetWarnings'))
!isnull(method('GetTypeId'))
```

If no matching property or method is found, `null` is returned. The name is case-sensitive. The name can be computed by another formula: `property(if(true, 'Name', 'Id'))`.

## Element parameters

`parameter(elementDefiner, identifier)` is only available in element formulas. The first argument selects the parameter source: `me` means the current element, `type` its type, `host` its host, and so on. If the definer returns multiple elements, the first one is used. The identifier is resolved in the following order:

1. a `BuiltInParameter` enum name;
2. a shared parameter GUID;
3. the parameter's display name (`LookupParameter`).

```text
parameter(me, 'ALL_MODEL_INSTANCE_COMMENTS') == 'checked'
parameter(type, 'ALL_MODEL_TYPE_COMMENTS') == 'checked'
!isnullorempty(parameter(me, 'Марка'))
isnull(parameter(me, 'Missing parameter'))
```

You can check whether a parameter exists or has a value set using these functions:

```text
hasparameter('Марка', me)
hasparametervalue('Марка', me)
```


| Revit StorageType | Formula value |
| --- | --- |
| `String` | `string` |
| `Integer` | `double` |
| `Double` | `double` in the parameter's project unit, converted from internal units |
| `ElementId` | `ElementId` |

A missing parameter returns `null`. A `Double` parameter is converted from Revit's internal units to the unit of that parameter, so it is read in the unit Revit shows for it: a length parameter in the project length unit, an area, volume or angle parameter in the matching project unit, and a dimensionless parameter as a plain number. `Integer` values are returned as numbers without any conversion, and `String` and `ElementId` values are returned as they are.

> [!warning]
> `property(name)` and `method(name)` return raw Revit API values in internal units: feet for lengths and radians for angles. Do not mix them with `parameter(...)` in one expression: `parameter(me, 'WALL_USER_HEIGHT_PARAM') + property('Height')` adds project units (millimetres in a metric project) to feet.

## Element filters

| Expression | What it matches |
| --- | --- |
| `instance` | Instances, but not types |
| `type` | Element types |
| `room` | Rooms |
| `all` | All elements |
| `empty` | No elements |

| Function | Purpose | Example |
| --- | --- | --- |
| `builtincategory(name)` | Filter by `BuiltInCategory` | `builtincategory('OST_Walls')` |
| `class(name)` | Filter by Revit API class name | `class('Wall')` |

Filters are combined with the words `and` and `or`; `and` has higher precedence. Parentheses are supported.

Leaf filters are negated with a `!` prefix, using the inverted native overloads: `!type`, `!instance`, `!all`, `!empty`, `!builtincategory(name)` and `!class(name)`. Compound negation such as `!(A and B)` is not supported — rewrite it through De Morgan (`!A or !B`).

```text
instance and builtincategory('OST_Walls')
builtincategory('OST_Walls') or builtincategory('OST_Levels')
instance and (class('Wall') or class('Floor'))
instance and !builtincategory('OST_CurtainWallMullions')
```

> [!warning]
> Filtering formulas use the words `and` and `or`. Logical formulas in `takeDocument` and `check` use the symbols `&` and `|`.

Formula usage: [[User diagnostics|user diagnostics]], [[Collision diagnostics|collision diagnostics]], [[Project parameter diagnostics|project parameter diagnostics]]. See also: [[Formula syntax|Syntax]], [[Formula functions|Functions]].
