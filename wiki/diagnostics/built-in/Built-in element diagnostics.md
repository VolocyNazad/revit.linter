---
lang: en
---

> Language: **English** · [[Встроенные проверки элементов|Русский]]

| ID           | Description                                                                                     | Level      | Example message                                                                     |
| :----------- | :---------------------------------------------------------------------------------------------- | :--------- | :---------------------------------------------------------------------------------- |
| **SHRD003**  | Checks family instances for the absence of mirroring.                                           | ⚠️ Warning | `Instance 'Окно_Стандарт' (ID:99887) is mirrored. (12 ms)`                          |
| **SHRD004**  | Checks whether views are placed on sheets (projects only).                                                      | ⚠️ Warning | `View 'Разрез А-А' (ID:55443) is not placed on a sheet. (9 ms)`                     |
| **SHRD005**  | Checks whether a wall has a top attachment set (projects only).                                                 | ⚠️ Warning | `Wall 'Стена_Внешняя' (ID:33221) has no top attachment. (7 ms)`                     |
| **SHRD006**  | Checks line-based elements for placement coordinate tolerance (projects only).                                  | ⚠️ Warning | `Element 'Ось 1' (ID:77665) has invalid coordinates. (14 ms)`                       |
| **SHRD007**  | Checks line-based elements for length value tolerance (projects only).                                          | ⚠️ Warning | `Element 'Балка_01' (ID:44332) has an invalid length. (11 ms)`                      |
| **SHRD008**  | Checks levels for height value tolerance (projects only).                                                       | ⚠️ Warning | `Level 'Уровень 2' (ID:55667) has an invalid height. (5 ms)`                        |
| **SHRD009*** | Checks floors for placement coordinate tolerance (projects only).                                               | ⚠️ Warning | `Floor 'Плита_01' (ID:88990) has invalid coordinates. (13 ms)`                      |
| **SHRD0010** | Checks family instances for placement height tolerance (projects only).                                         | ⚠️ Warning | `Instance 'Светильник_01' (ID:22334) has an invalid height. (10 ms)`                |
| **SHRD0011** | Checks whether parameter elements are used (projects only).                                                     | ⚠️ Warning | `Parameter 'Заказчик_ДопПоле' (ID:44556) is not used. (8 ms)`                       |
| **ARCH004**  | Checks walls for height value tolerance (projects only).                                                        | ⚠️ Warning | `Wall 'Стена_Внутренняя' (ID:77889) has an invalid height. (8 ms)`                  |
| **ARCH001**  | Checks whether a room is placed (projects only).                                                                | ❌ Error    | `Room 'Комната 101' (ID:11223) is not placed. (6 ms)`                               |
| **ARCH002**  | Checks whether a room is enclosed (projects only).                                                              | ❌ Error    | `Room 'Комната 102' (ID:33445) is not enclosed. (7 ms)`                             |
| **ARCH003**  | Checks whether a room is redundant (projects only).                                                             | ❌ Error    | `Room 'Комната 103' (ID:55667) is redundant. (5 ms)`                                |
| **SYST001**  | Checks pipe, duct, cable tray, conduit, and custom family instances for unconnected connectors (projects only). | 💬 Message | `Element 'Труба-01' (ID:12345) has unconnected connectors. (15 ms)`                 |
| **SHRD001**  | Checks families for usage in the document.                                                      | 💬 Message | `Family 'Вентилятор_Старый' (ID:67890) is not used. (8 ms)`                         |
| **SHRD002**  | Checks family types for usage in the document.                                                  | 💬 Message | `Type 'Дверь_1200x2400' (ID:11223) is not used. (6 ms)`                             |
| **SHRD0012** | Checks whether a family instance is associated with the nearest level (projects only).                          | 💬 Message | `Instance 'Колонна_01' (ID:66778) is not associated with the nearest level. (9 ms)` |
| **SHRD0013** | Checks for model lines.                                                                         | 💬 Message | `Model line 'Линия_01' (ID:99005) exists in the model. (5 ms)` |
| **SHRD0014** | Checks for detail lines.                                                                        | 💬 Message | `Detail line 'Линия_02' (ID:99006) exists in the model. (5 ms)` |
| **SHRD0015** | Checks for text notes.                                                                          | 💬 Message | `Text note 'Примечание_01' (ID:99007) exists in the model. (5 ms)` |
| **SHRD0016** | Checks for imported objects.                                                                    | 💬 Message | `Imported object 'Импорт_01' (ID:99008) exists in the model. (5 ms)` |
| **SHRD0017** | Checks sheets for placed views and schedules (projects only).                                   | ⚠️ Warning | `Sheet 'А-101' (ID:99009) is empty. (5 ms)` |
| **SHRD0018** | Checks whether views placed on sheets have a view template (projects only).                     | ⚠️ Warning | `View 'Разрез Б-Б' (ID:99010) is placed on a sheet without a view template. (6 ms)` |
| **SHRD0019** | Checks whether sheets have a title block (projects only).                                       | ❌ Error    | `Sheet 'А-101' (ID:99009) has no title block. (6 ms)` |
| **SHRD0020** | Checks that sheets have no more than one title block (projects only).                           | ❌ Error    | `Sheet 'А-101' (ID:99009) has multiple title blocks. (6 ms)` |
| **SHRD0021** | Checks for groups nested inside other groups.                                                   | ⚠️ Warning | `Group 'Группа 2' (ID:99011) is nested inside another group. (5 ms)` |
| **SHRD0022** | Checks whether materials are used in the document.                              | 💬 Message | `Material 'Бетон_01' (ID:99012) is not used. (5 ms)` |
| **SHRD0023** | Checks whether profile family types are used in the document (projects only).                   | 💬 Message | `Profile type 'Профиль_01' (ID:99013) is not used. (5 ms)` |
| **SHRD0024** | Checks whether reference planes carry locked or EQ dimensions (family documents only).              | 💬 Message | `Reference plane 'Опорная плоскость' (ID:99001) has no locked or EQ dimensions. (5 ms)` |
| **SHRD0025** | Checks whether family type text parameters are filled (family documents only).                  | 💬 Message | `Family 'Шкаф' (ID:99002) has unfilled type parameters: T1: Description, Model. (5 ms)` |
| **SHRD0026** | Checks whether family types duplicate each other (family documents only).                       | ⚠️ Warning | `Family 'Шкаф' (ID:99002) has duplicate types: T1 = T2. (6 ms)` |
| **SHRD0027** | Checks whether groups exist in the document.                                        | 💬 Message | `Group 'Группа 1' (ID:99003) exists in the document. (5 ms)` |
| **SHRD0028** | Checks whether family symbols duplicate each other in the same family.                                    | ⚠️ Warning | `Family symbol 'Дверь_1200' (ID:99004) has identical parameter values with 'Дверь_1200_2'. (6 ms)` |
| **SHRD0029** | Checks whether reference lines carry locked or EQ dimensions (family documents only).                     | 💬 Message | `Reference line 'Опорная линия' (ID:99014) has no locked or EQ dimensions. (5 ms)` |
| **RVT003** | Checks for unused elements that Purge Unused can delete. | 💬 Message | `Element 'Стена_01' (ID:99015) is not used and can be purged. (5 ms)` |

> \* `SHRD009` is only available in Revit versions **AFTER2023**.

---

See [[Diagnostic severity|severity levels]] for the meaning of each result level.
