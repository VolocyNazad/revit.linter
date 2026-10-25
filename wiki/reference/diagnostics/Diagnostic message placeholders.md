---
lang: en
---

> Language: **English** · [[Подстановки сообщений|Русский]]

The following placeholders can be used in message templates:

|Placeholder|Description|Available in diagnostics|
|---|---|---|
|`{elementName}`|The name of the element in the model.|`Element`|
|`{elementId}`|The unique identifier of the element in the document.|`Element`|
|`{intersection.elementNames}`|Comma-separated names of every element the target intersects, in the same order as `{intersection.elementIds}`. Linked elements appear as `Name [Link]`.|`Element` (collision rules)|
|`{intersection.elementIds}`|Comma-separated identifiers of every element the target intersects, in the same order as `{intersection.elementNames}`.|`Element` (collision rules)|
|`{intersection.count}`|The number of elements the target intersects.|`Element` (collision rules)|
|`{documentTitle}`|The title of the document.|`Document`|
|`{details}`|Additional details of the finding, such as the description of a Revit warning or a parameter problem.|`Document`|
|`{duration}`|The execution time of the check in milliseconds.|`Element`, `Document`|

Unknown placeholders are kept verbatim. Use `{{` and `}}` for literal braces.