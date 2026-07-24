---
aliases:
  - Формулы Revit
  - Фильтры элементов Revit
tags:
  - documentation
  - formulas
  - revit
---

# Объекты Revit и фильтры элементов

## Свойства и методы

`property(name)` читает публичное свойство текущего объекта. В `takeDocument` текущий объект — `Autodesk.Revit.DB.Document`, в `check` — проверяемый `Autodesk.Revit.DB.Element` фактического типа.

```text
!property('IsFamilyDocument')
property('Name') == 'Wall 01'
property('Width') > 0
```

`method(name)` вызывает публичный метод без параметров. Метод должен возвращать значение и не должен быть обобщённым (`generic`).

```text
!isnull(method('GetWarnings'))
!isnull(method('GetTypeId'))
```

Если свойство или подходящий метод не найден, возвращается `null`. Имя чувствительно к регистру. Имя можно вычислить другой формулой: `property(if(true, 'Name', 'Id'))`.

## Параметры элемента

`parameter(elementDefiner, identifier)` доступна только в формулах элемента. Первый аргумент выбирает источник параметра: `me` означает текущий элемент, `type` — его тип, `host` — основу и так далее. Если определитель возвращает несколько элементов, используется первый. Идентификатор разрешается в следующем порядке:

1. имя перечисления `BuiltInParameter`;
2. GUID общего параметра;
3. отображаемое имя параметра (`LookupParameter`).

```text
parameter(me, 'ALL_MODEL_INSTANCE_COMMENTS') == 'checked'
parameter(type, 'ALL_MODEL_TYPE_COMMENTS') == 'checked'
!isnullorempty(parameter(me, 'Марка'))
isnull(parameter(me, 'Missing parameter'))
```

Проверить наличие параметра или заполненного значения можно функциями:

```text
hasparameter('Марка', me)
hasparametervalue('Марка', me)
```


| StorageType Revit | Значение в формуле |
| --- | --- |
| `String` | `string` |
| `Integer` | `double` |
| `Double` | `double`, преобразованный из внутренних единиц в единицы проекта |
| `ElementId` | `ElementId` |

Отсутствующий параметр возвращает `null`. Сейчас величины `Double` преобразуются с настройками единиц длины документа, поэтому формулы для иных физических величин следует проверять отдельно.

## Фильтры элементов

| Выражение | Что пропускает |
| --- | --- |
| `instance` | Экземпляры, но не типы |
| `type` | Типы элементов |
| `room` | Помещения |
| `all` | Все элементы |
| `empty` | Ни одного элемента |

| Функция | Назначение | Пример |
| --- | --- | --- |
| `builtincategory(name)` | Фильтр по `BuiltInCategory` | `builtincategory('OST_Walls')` |
| `class(name)` | Фильтр по имени класса Revit API | `class('Wall')` |

Фильтры объединяются словами `and` и `or`; `and` имеет более высокий приоритет. Поддерживаются скобки.

```text
instance and builtincategory('OST_Walls')
builtincategory('OST_Walls') or builtincategory('OST_Levels')
instance and (class('Wall') or class('Floor'))
```

> [!warning]
> В формулах фильтрации используются слова `and` и `or`. В логических формулах `takeDocument` и `check` используются символы `&` и `|`.

Использование формул: [[user-diagnostics|пользовательские проверки]], [[collision-diagnostics|проверки коллизий]], [[project-parameter-diagnostics|проверки параметров проекта]]. См. также: [[formula-syntax|Синтаксис]], [[formula-functions|Функции]].
