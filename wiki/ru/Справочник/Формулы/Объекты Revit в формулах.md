---
tags:
  - documentation
  - formulas
  - revit
lang: ru
---

> Язык: [[Formula Revit|English]] · **Русский**

## Свойства и методы

`property(name)` считывает общедоступное свойство текущего объекта. В `takeDocument` текущий объект — `Autodesk.Revit.DB.Document`; в `check` это проверяемый `Autodesk.Revit.DB.Element` его фактического типа.

```text
!property('IsFamilyDocument')
property('Name') == 'Wall 01'
property('Width') > 0
```

`method(name)` вызывает общедоступный метод без параметров. Метод должен возвращать значение и не должен быть обобщённым.

```text
!isnull(method('GetWarnings'))
!isnull(method('GetTypeId'))
```

Если подходящее свойство или метод не найдены, возвращается `null`. Имя чувствительно к регистру. Имя можно вычислить по другой формуле: `property(if(true, 'Name', 'Id'))`.

## Параметры элемента

`parameter(elementDefiner, identifier)` доступен только в формулах элементов. Первый аргумент задаёт источник параметра: `me` означает текущий элемент, `type` — его тип, `host` — его хост и так далее. Если источник содержит несколько элементов, используется первый. Идентификатор параметра определяется в следующем порядке:

1. имя перечисления `BuiltInParameter`;
2. GUID общего параметра;
3. отображаемое имя параметра (`LookupParameter`).

```text
parameter(me, 'ALL_MODEL_INSTANCE_COMMENTS') == 'checked'
parameter(type, 'ALL_MODEL_TYPE_COMMENTS') == 'checked'
!isnullorempty(parameter(me, 'Марка'))
isnull(parameter(me, 'Missing parameter'))
```

Наличие параметра и заданного значения можно проверить следующими функциями:

```text
hasparameter('Марка', me)
hasparametervalue('Марка', me)
```


| Тип хранилища Revit | Значение формулы |
| --- | --- |
| `String` | `string` |
| `Integer` | `double` |
| `Double` | `double` в проектной единице параметра, преобразованный из внутренних единиц |
| `ElementId` | `ElementId` |

При отсутствии параметра возвращается `null`. Значение параметра `Double` преобразуется из внутренних единиц Revit в единицу этого параметра, то есть читается в той единице, которую Revit показывает для него: параметр длины — в единице длины проекта, параметр площади, объёма или угла — в соответствующей единице проекта, безразмерный параметр — обычным числом. Значения `Integer` возвращаются числами без преобразования, значения `String` и `ElementId` — как есть.

> [!warning]
> `property(name)` и `method(name)` возвращают сырые значения Revit API во внутренних единицах: футы для длин и радианы для углов. Не смешивайте их с `parameter(...)` в одном выражении: `parameter(me, 'WALL_USER_HEIGHT_PARAM') + property('Height')` сложит проектные единицы (миллиметры в метрическом проекте) с футами.

## Фильтры элементов

| Выражение | Результат |
| --- | --- |
| `instance` | Экземпляры, но не типы |
| `type` | Типы элементов |
| `room` | Помещения |
| `all` | Все элементы |
| `empty` | Пустая выборка |

| Функция | Назначение | Пример |
| --- | --- | --- |
| `builtincategory(name)` | Фильтр по `BuiltInCategory` | `builtincategory('OST_Walls')` |
| `class(name)` | Фильтр по имени класса Revit API | `class('Wall')` |

Фильтры объединяются словами `and` и `or`; `and` имеет более высокий приоритет. Круглые скобки поддерживаются.

Листовые фильтры отрицаются префиксом `!` через инвертированные нативные перегрузки: `!type`, `!instance`, `!all`, `!empty`, `!builtincategory(name)` и `!class(name)`. Составное отрицание вида `!(A and B)` не поддерживается — перепишите его через Де Моргана (`!A or !B`).

```text
instance and builtincategory('OST_Walls')
builtincategory('OST_Walls') or builtincategory('OST_Levels')
instance and (class('Wall') or class('Floor'))
instance and !builtincategory('OST_CurtainWallMullions')
```

> [!warning]
> В формулах фильтрации используются слова `and` и `or`. Логические формулы в `takeDocument` и `check` используют символы `&` и `|`.

Использование формулы: [[Пользовательские проверки]], [[Проверки коллизий]], [[Проверки параметров проекта]]. См. также: [[Синтаксис формул]], [[Функции формул]].
