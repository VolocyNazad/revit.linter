using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;

namespace Revit.Linter.Sandbox.TemplateScan;

/// <summary>
/// One-off Revit 2021 scan: reads a queue of .rte templates on Idling, dumps every project parameter
/// binding to JSON in the configured output directory and writes a done marker.
/// </summary>
/// <remarks>
/// Revit is started with the first template of the queue as the command-line document; the
/// <c>DocumentOpened</c> event of that document supplies the full <see cref="Application"/>, which is
/// then used to create a detached project from every remaining template. The queue file path comes
/// from the <c>REVIT_LINTER_TEMPLATE_SCAN</c> environment variable; when it is not set, the file is read
/// from <c>%TEMP%\revit-linter-template-scan\scan.txt</c>. The file format is one
/// <c>out=&lt;directory&gt;</c> line, an optional <c>packs=&lt;directory&gt;</c> line with the generated
/// check fragments, and <c>&lt;discipline&gt;|&lt;absolute .rte path&gt;</c> lines.
/// </remarks>
public sealed class TemplateScanApplication : IExternalApplication
{
    private const string QueueEnvironmentVariable = "REVIT_LINTER_TEMPLATE_SCAN";
    private const string QueueFolderName = "revit-linter-template-scan";
    private const string QueueFileName = "scan.txt";
    private const int DocumentWaitTicks = 600;

    private readonly List<string> _log = [];
    private UIControlledApplication? _uiApplication;
    private Application? _application;
    private Document? _openedDocument;
    private string? _outputDirectory;
    private string? _packsDirectory;
    private List<TemplateQueueItem>? _queue;
    private int _index;
    private int _idleTicks;
    private bool _finished;

    public Result OnStartup(UIControlledApplication application)
    {
        _uiApplication = application;
        application.ControlledApplication.DocumentOpened += OnDocumentOpened;
        application.Idling += OnIdling;
        return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication application) => Result.Succeeded;

    private void OnDocumentOpened(object? sender, DocumentOpenedEventArgs args)
    {
        if (_application is not null || args.Document is not { } document)
            return;

        _application = document.Application;
        _openedDocument = document;
        Log("application captured from the opened document: " + document.Title);
    }

    private void OnIdling(object? sender, Autodesk.Revit.UI.Events.IdlingEventArgs args)
    {
        if (_finished)
            return;

        _idleTicks++;
        try
        {
            if (_queue is null && !LoadQueue())
                return;

            if (_application is null)
            {
                if (_idleTicks > DocumentWaitTicks)
                    Finish("no-document: Revit did not open the first template");
                return;
            }

            if (_index == 0 && _openedDocument is not null)
            {
                Document document = _openedDocument;
                _openedDocument = null;
                TemplateQueueItem first = _queue![0];
                _index = 1;
                Log($"scan 1/{_queue.Count} (already opened): {first.Discipline}");
                Write(first, TemplateScanner.Read(document, first, _packsDirectory));
                return;
            }

            if (_index >= _queue!.Count)
            {
                Finish("complete");
                return;
            }

            TemplateQueueItem item = _queue[_index];
            _index++;
            Log($"scan {_index}/{_queue.Count}: {item.Discipline} -> {item.Path}");
            Write(item, TemplateScanner.OpenAndRead(_application, item, _packsDirectory));
        }
        catch (Exception exception)
        {
            Log($"fatal: {exception.GetType().Name}: {exception.Message}");
            Finish("failed: " + exception.GetType().Name);
        }
    }

    private void Write(TemplateQueueItem item, TemplateSnapshot snapshot)
    {
        string file = Path.Combine(_outputDirectory!, $"{_index:00}-{item.Discipline}.json");
        File.WriteAllText(file, snapshot.ToJson());
        Log($"done {item.Discipline}: bindings={snapshot.Bindings.Count}, " +
            $"parameterElements={snapshot.ParameterElements.Count}, " +
            $"projectInformationParameters={snapshot.ProjectInformationParameters.Count}, " +
            $"error={snapshot.Error ?? "none"}, closeError={snapshot.CloseError ?? "none"}, " +
            $"verificationChecked={snapshot.VerificationChecked}, " +
            $"verificationProblems={snapshot.Verification.Count}");
        foreach (string problem in snapshot.Verification)
            Log("  verification: " + problem);
    }

    private bool LoadQueue()
    {
        string path = Environment.GetEnvironmentVariable(QueueEnvironmentVariable)
                      ?? Path.Combine(Path.GetTempPath(), QueueFolderName, QueueFileName);

        if (!File.Exists(path))
        {
            Finish("queue-not-found: " + path);
            return false;
        }

        List<TemplateQueueItem> queue = [];
        string? outputDirectory = null;
        string? packsDirectory = null;
        foreach (string rawLine in File.ReadAllLines(path))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                continue;

            if (line.StartsWith("out=", StringComparison.OrdinalIgnoreCase))
            {
                outputDirectory = line.Substring(4).Trim();
                continue;
            }

            if (line.StartsWith("packs=", StringComparison.OrdinalIgnoreCase))
            {
                packsDirectory = line.Substring(6).Trim();
                continue;
            }

            int separator = line.IndexOf('|');
            if (separator <= 0)
                continue;

            queue.Add(new TemplateQueueItem(
                line.Substring(0, separator).Trim(),
                line.Substring(separator + 1).Trim()));
        }

        if (outputDirectory is null || queue.Count == 0)
        {
            Finish("queue-invalid: " + path);
            return false;
        }

        _outputDirectory = outputDirectory;
        _packsDirectory = packsDirectory;
        Directory.CreateDirectory(outputDirectory);
        _queue = queue;
        Log($"queue loaded from {path}: {queue.Count} template(s), output {outputDirectory}, " +
            $"packs {packsDirectory ?? "none"}");
        return true;
    }

    private void Finish(string status)
    {
        if (_finished)
            return;

        _finished = true;
        if (_uiApplication is not null)
        {
            _uiApplication.ControlledApplication.DocumentOpened -= OnDocumentOpened;
            _uiApplication.Idling -= OnIdling;
        }

        Log("finished: " + status);

        if (_outputDirectory is not null)
        {
            File.WriteAllLines(Path.Combine(_outputDirectory, "_scan.log"), _log);
            File.WriteAllText(Path.Combine(_outputDirectory, "_done.txt"), status);
        }
    }

    private void Log(string message)
    {
        _log.Add(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + message);
        if (_outputDirectory is not null)
            File.WriteAllLines(Path.Combine(_outputDirectory, "_scan.log"), _log);
    }
}
