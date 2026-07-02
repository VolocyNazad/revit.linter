
## 📋 Пользовательские проверки

Список и поведение проверок задается через конфигурационный файл с расширением `.yaml`, который должен быть расположен по указанному в соглашении [[diagnostic-configuration-path-convension|пути]]. Наименование файла должно быть `config.yaml`.


---

**Пример конфигурации:**
```yml
- code: "CSTM001"
  description: "custom"
  message: "Для элемента с именем '{elementName}' и идентификатором '{elementId}' неверно указаны параметры 'Марка' и(или) 'Комментарии'. Время выполнения '{duration}' мс."
  isObsolete: true
  takeDocument: "!property('IsFamilyDocument')"
  take: "instance and builtincategory('OST_Walls')"
  check: "parameter('Комментарии') == 'Ура!' & parameter('Марка') == 'Ура!'"
- code: "CSTM002"
  description: "custom"
  message: "Для элемента с именем '{elementName}' и идентификатором '{elementId}' неверно указано имя"
  severity: "Message"
  isActive: false
  isObsolete: true
  obsoleteDescription: "Какой-то текст, описывающий причину устаревания проверки"
  takeDocument: "true"
  take: "instance and builtincategory('OST_Walls')"
  check: "property('Name') == '1'"
- code: "CSTM003"
  ...
```

---

**Параметры:**

| Поле                  | Тип              | Тип вводимых данных | Тип возвращаемых данных формулой | Назначение                                                                                                            |
| --------------------- | ---------------- | ------------------- | -------------------------------- | --------------------------------------------------------------------------------------------------------------------- |
| `code`                | строка           | `string`            | `-`                              | Уникальный идентификатор проверки                                                                                     |
| `description`         | строка           | `string`            | `-`                              | Описание проверки                                                                                                     |
| `message`             | строка           | `string`            | `-`                              | Шаблон сообщения об ошибке. Доступные переменные: `{elementName}`, `{elementId}`, `{duration}`                        |
| `severity`            | перечисление     | `string`            | `-`                              | [[diagnostic-severity\|Уровень серьёзности]] <br>(по умолчанию: `Message`) (не обязательно)                           |
| `isActive`            | булевое значение | `bool`              | `-`                              | Активна ли проверка (по умолчанию: `true`) (не обязательно)                                                           |
| `isObsolete`          | булевое значение | `bool`              | `-`                              | [[obsolete-diagnostic\|Устарела ли проверка]] (по умолчанию: `false`) (не обязательно)                                |
| `obsoleteDescription` | строка           | `string`            | `-`                              | [[obsolete-diagnostic\|Описание причины устаревания проверки]] (отображается при `isObsolete: true`) (не обязательно) |
| `takeDocument`        | строка           | `string`            | `bool`                           | Условие фильтрации документов для проверки                                                                            |
| `take`                | строка           | `string`            | `ElementFilter`                  | Условие фильтрации элементов для проверки                                                                             |
| `check`               | строка           | `string`            | `bool`                           | Условие проверки (если условие истинно, то элемент считается валидным)                                                |


---

**Примечания:**
- Если не указывать поле `severity`, оно будет равно `Message`
- Если не указывать поле `isActive`, оно будет равно `true`
- Если не указывать поле `isObsolete`, оно будет равно `false`