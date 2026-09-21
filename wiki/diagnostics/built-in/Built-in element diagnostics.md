---
lang: en
---

> Language: **English** · [[Встроенные проверки элементов|Русский]]

| ID           | Description                                                                                     | Level      | Example message                                                                     |
| :----------- | :---------------------------------------------------------------------------------------------- | :--------- | :---------------------------------------------------------------------------------- |
| **SHRD003**  | Checks family instances for the absence of mirroring.                                           | ⚠️ Warning | `Instance 'Окно_Стандарт' (ID:99887) is mirrored. (12 ms)`                          |
| **SHRD004**  | Checks whether views are placed on sheets.                                                      | ⚠️ Warning | `View 'Разрез А-А' (ID:55443) is not placed on a sheet. (9 ms)`                     |
| **SHRD005**  | Checks whether a wall has a top attachment set.                                                 | ⚠️ Warning | `Wall 'Стена_Внешняя' (ID:33221) has no top attachment. (7 ms)`                     |
| **SHRD006**  | Checks line-based elements for placement coordinate tolerance.                                  | ⚠️ Warning | `Element 'Ось 1' (ID:77665) has invalid coordinates. (14 ms)`                       |
| **SHRD007**  | Checks line-based elements for length value tolerance.                                          | ⚠️ Warning | `Element 'Балка_01' (ID:44332) has an invalid length. (11 ms)`                      |
| **SHRD008**  | Checks levels for height value tolerance.                                                       | ⚠️ Warning | `Level 'Уровень 2' (ID:55667) has an invalid height. (5 ms)`                        |
| **SHRD009*** | Checks floors for placement coordinate tolerance.                                               | ⚠️ Warning | `Floor 'Плита_01' (ID:88990) has invalid coordinates. (13 ms)`                      |
| **SHRD0010** | Checks family instances for placement height tolerance.                                         | ⚠️ Warning | `Instance 'Светильник_01' (ID:22334) has an invalid height. (10 ms)`                |
| **SHRD0011** | Checks whether parameter elements are used.                                                     | ⚠️ Warning | `Parameter 'Заказчик_ДопПоле' (ID:44556) is not used. (8 ms)`                       |
| **ARCH004**  | Checks walls for height value tolerance.                                                        | ⚠️ Warning | `Wall 'Стена_Внутренняя' (ID:77889) has an invalid height. (8 ms)`                  |
| **ARCH001**  | Checks whether a room is placed.                                                                | ❌ Error    | `Room 'Комната 101' (ID:11223) is not placed. (6 ms)`                               |
| **ARCH002**  | Checks whether a room is enclosed.                                                              | ❌ Error    | `Room 'Комната 102' (ID:33445) is not enclosed. (7 ms)`                             |
| **ARCH003**  | Checks whether a room is redundant.                                                             | ❌ Error    | `Room 'Комната 103' (ID:55667) is redundant. (5 ms)`                                |
| **SYST001**  | Checks pipe, duct, cable tray, conduit, and custom family instances for unconnected connectors. | 💬 Message | `Element 'Труба-01' (ID:12345) has unconnected connectors. (15 ms)`                 |
| **SHRD001**  | Checks families for usage in the document.                                                      | 💬 Message | `Family 'Вентилятор_Старый' (ID:67890) is not used. (8 ms)`                         |
| **SHRD002**  | Checks family types for usage in the document.                                                  | 💬 Message | `Type 'Дверь_1200x2400' (ID:11223) is not used. (6 ms)`                             |
| **SHRD0012** | Checks whether a family instance is associated with the nearest level.                          | 💬 Message | `Instance 'Колонна_01' (ID:66778) is not associated with the nearest level. (9 ms)` |

> \* `SHRD009` is only available in Revit versions **AFTER2023**.

---

See [[Diagnostic severity|severity levels]] for the meaning of each result level.
