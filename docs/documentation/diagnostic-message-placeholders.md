## 📌 Message Placeholders

The following placeholders can be used in message templates:

|Placeholder|Description|Available in diagnostics|
|---|---|---|
|`{elementName}`|The name of the element in the model.|`Element`|
|`{elementId}`|The unique identifier of the element in the document.|`Element`|
|`{documentTitle}`|The title of the document.|`Document`|
|`{duration}`|The execution time of the check in milliseconds.|`Element`, `Document`|

Unknown placeholders are kept verbatim. Use `{{` and `}}` for literal braces.