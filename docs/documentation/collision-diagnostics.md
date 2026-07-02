## 📋 Проверки коллизий

Список и поведение проверок задается через конфигурационный файл с расширением `.yaml`, который должен быть расположен по указанному в соглашении [[diagnostic-configuration-path-convension|пути]]. Наименование файла должно быть `collision.config.yaml`.

---

**Пример конфигурации:**
```yml
- code: "CLSN001"
  description: "custom"
  message: "Элемент с именем '{elementName}' и идентификатором '{elementId}' образует пересечение c элементом '{intersection.elementName}' и идентификатором '{intersection.elementId}'. Время выполнения '{duration}' мс."
  severity: "Warning"
  isActive: true
  takeDocument: "property('Title') != '' & !property('IsFamilyDocument')"
  take: "instance and (class('Pipe') or class('Duct') or class('CableTray') or class('Conduit') or builtincategory('OST_DuctFitting') or builtincategory('OST_PipeFitting') or builtincategory('OST_ConduitFitting') or builtincategory('OST_ConduitFitting'))"
  andTake: "instance and (class('Pipe') or class('Duct') or class('CableTray') or class('Conduit') or builtincategory('OST_DuctFitting') or builtincategory('OST_PipeFitting') or builtincategory('OST_ConduitFitting') or builtincategory('OST_ConduitFitting'))"
  groupBy: "parameter('Комментарии')"
- code: "CLSN002"
...
```

---

**Параметры:**

| Поле                  | Тип              | Тип вводимых данных | Тип возвращаемых данных формулой | Назначение                                                                                                                                                   |
| --------------------- | ---------------- | ------------------- | -------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `code`                | строка           | `string`            | `-`                              | Уникальный идентификатор проверки                                                                                                                            |
| `description`         | строка           | `string`            | `-`                              | Описание проверки                                                                                                                                            |
| `message`             | строка           | `string`            | `-`                              | Шаблон сообщения об ошибке. <br>Доступные переменные: `{elementName}`, `{elementId}`, `{intersection.elementName}`, `{intersection.elementId}`, `{duration}` |
| `severity`            | перечисление     | `string`            | `-`                              | [[diagnostic-severity\|Уровень серьёзности]]<br>(по умолчанию: `Message`) (не обязательно)                                                                   |
| `isActive`            | булевое значение | `bool`              | `-`                              | Активна ли проверка <br>(по умолчанию: `true`) (не обязательно)                                                                                              |
| `isObsolete`          | булевое значение | `bool`              | `-`                              | [[obsolete-diagnostic\|Устарела ли проверка]] <br>(по умолчанию: `false`) (не обязательно)                                                                   |
| `obsoleteDescription` | строка           | `string`            | `-`                              | [[obsolete-diagnostic\|Описание причины устаревания проверки]] <br>(отображается при `isObsolete: true`) (не обязательно)                                    |
| `takeDocument`        | строка           | `string`            | `bool`                           | Условие фильтрации документов для проверки                                                                                                                   |
| `take`                | строка           | `string`            | `ElementFilter`                  | Условие отбора первой группы элементов для проверки коллизий                                                                                                 |
| `andTake`             | строка           | `string`            | `ElementFilter`                  | Условие отбора второй группы элементов для проверки коллизий (с чем проверять пересечения)                                                                   |
| `groupBy`             | строка           | `string`            | `object`                         | Группировка по параметру элемента <br>(поиск коллизий осуществляется внутри групп)                                                                           |

---

**Примечания:**
- Если не указывать поле `severity`, оно будет равно `Message`
- Если не указывать поле `isActive`, оно будет равно `true`
- Если не указывать поле `isObsolete`, оно будет равно `false`