# Logging policy

Paths mentioned in this policy are relative to the repository root unless a Markdown link specifies otherwise.

Logs are the first evidence in a support case and in a failed diagnostic: which build is installed,
which checks are active, and how a run ended. A change that adds an operation, a refusal or a failure
path updates the logging as part of the same change instead of leaving the diagnosis to a later
debugging session.

## Where logging is configured

- Configure Serilog once, in the application composition root where the host is built. Modules,
  presenters and services receive `ILogger<T>` from the Microsoft logging abstractions and never
  create a sink, logger or level filter of their own.
- The runtime configuration sets `Information` as the minimum level and writes a rolling file in
  every build; the console and debug sinks are added in a Debug build only. Do not compensate for a
  missing sink inside a module.
- Use the static `Serilog.Log` logger only where the host logger cannot be resolved: the fatal
  startup path and a shutdown failure after the host has been disposed. Everywhere else resolve
  `ILogger<T>` through dependency injection.
- Enrichment adds environment and Revit context - the document, model path and add-in identity - to
  every event. Add operation-specific context through the structured message properties instead of
  composing it into the message text. The logging stack is listed in the
  [repository guide](../repository.md).

## Levels

| Level | Use |
| --- | --- |
| `Information` | Lifecycle events and user-meaningful operations: startup with the product version and Revit version, shutdown, diagnostic runs, configuration loads, report exports and applied fixes. |
| `Debug` | Routine per-element details, collected element counts, query cache statistics and per-diagnostic durations. A duration line stays `Debug` for a fast diagnostic and is promoted to `Information` for a slow one. |
| `Warning` | Expected refusals and validation failures: invalid configuration, an unsupported state, or an operation declined before it changes the model. |
| `Error` | Data, configuration, file-system and unexpected Revit API failures, logged with the exception instance. |
| `Fatal` | A startup failure that leaves the add-in unusable. |

A run logs its own start and completion once, not the elements it visited. Do not choose a level by
how interesting a message looks: user-meaningful operations are `Information`, and the details used
only while investigating stay at `Debug`.

## Log an exception once

- Log an exception at the layer where its meaning is first understood, and pass the exception
  instance as the logger overload's first argument so the stack trace is recorded.
- Callers that only surface the message stay silent. Do not log the same exception again in a view
  model, presenter, command or event handler on the way up.
- A failure that is translated into user-visible feedback is not logged again where the feedback is
  shown.
- A caught exception that is rethrown leaves logging to the layer that handles it.
- Inside a diagnostic run, the first failure of a diagnostic ends that diagnostic's remaining
  elements and is reported once; the same exception is not written once per element.

## Silent fallbacks

- Keep documented benign fallbacks, `Try*` probes and cancellation used as control flow silent.
- Silence is a decision: put a code comment at the probe or catch block explaining why the failure is
  expected and what the fallback does. A silent fallback without that comment is indistinguishable
  from a swallowed defect.
- Do not turn an unexpected failure into a silent fallback to keep the log clean. A protected read
  that intentionally treats an unreadable element as clean says so in the code.

## Structured messages

- Use structured message templates with named properties, for example
  `"Diagnostic {DiagnosticCode} failed"`, and pass the values as arguments. The properties remain
  queryable in the log.
- Never interpolate values into a template: no string interpolation, concatenation or
  `string.Format` for a logging call.
- Choose stable, meaningful property names and keep the message a sentence that describes the
  operation while the properties supply the variable parts. Use a format specifier only where the
  rendered precision matters, as in `{ElapsedMilliseconds:F0}` or `{CheckMilliseconds:F0}`.
- Pass the exception through the exception overload, not as a `{Exception}` property.

## Privacy and size

- Log the context that identifies the operation: document title, model path, diagnostic code,
  operation and counts.
- Do not log secrets such as credentials or tokens, and do not log entire element payloads or
  collections. Element details belong to the diagnostic report, not to the log.
- Prefer counts, identifiers and titles over serialized objects: a property that renders a whole
  object makes the line large and hard to query.

## Failed diagnostics

- A diagnostic that throws is isolated by the diagnostic service. The failure is logged at `Error`
  once, published to the report as an error under the diagnostic's code, and the remaining
  diagnostics still run.
- The log receives the exception instance; the report receives the failure message under the
  diagnostic's code. Do not put a stack trace into the user-visible report.
- Do not catch a diagnostic failure just to keep running inside that diagnostic: let the diagnostic
  service isolate it. A per-element protected read that treats an unreadable element as clean is a
  documented fallback, not a replacement for that isolation.

## Temporary debug logging

- When a check misbehaves in Revit, start from the logs: which build is installed, which check is
  active and what the report contains. See [adding a built-in diagnostic](../diagnostics/adding.md).
- Adding temporary detailed logging inside the check is allowed while the failure is being
  diagnosed: log what the code sees, what it compares and what it decides on.
- Remove the temporary logging once the failure is fixed, before the change is finished. The removal
  belongs to the same change, not to a follow-up.

## Where the logs are

- The file sink writes under `%LOCALAPPDATA%\Volocy\Revit.Linter\logs\`. Files roll daily and at
  20 MB, and the newest 14 files are retained.
- Startup and shutdown are recorded, fatal failures are captured, and the log is flushed when the
  add-in host shuts down. Development builds additionally write to the console and the debug output;
  those sinks never replace the file.
- The log directory is per-user state, not part of the repository.

The rest of the contributor workflow is defined by the [development policy](development.md).
