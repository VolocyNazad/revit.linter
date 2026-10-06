---
lang: en
---

> Language: **English** · [[Создание первого правила|Русский]]

# Create your first rule

This example reports doors whose **Mark** parameter is empty.

## 1. Open the configuration folder

On the **Diagnostics** ribbon tab, press [[Diagnostic configuration path button|Open configuration folder]]. Each Revit version has its own folder.

Create `config.yaml`, or open the existing file. If you loaded the welcome examples, use them as a starting point instead of replacing the file.

## 2. Add the rule

```yaml
- code: "CSTM201"
  description: "Doors without a mark"
  message: "'{elementName}' ({elementId}) has an empty mark."
  severity: "Warning"
  takeDocument: "!property('IsFamilyDocument')"
  take: "instance and builtincategory('OST_Doors')"
  check: "!isnullorempty(parameter(me, 'ALL_MODEL_MARK'))"
```

`take` selects door instances. `check` returns `true` for a door with a mark, so a door with an empty mark appears in the report. The built-in parameter name keeps the rule independent of the Revit interface language.

## 3. Check the result

Save the file, return to Revit, enable **CSTM201** in [[Diagnostics pane|Diagnostics]], and press **Run** in [[Diagnostic reports pane|Diagnostic results]]. Test the rule on a disposable model or copy first.

If the rule does not appear, check the exact file name and folder, then see [[Troubleshooting]]. For more fields and formula options, continue with [[User diagnostics]] and [[Formula syntax]].

[[Quick start|← Quick start]]
