namespace Revit.Linter.Infrastructure.Services;

using System.IO;

/// <summary>Stores one disposable tutorial copy until its dedicated Revit command consumes it.</summary>
internal sealed class TutorialSampleOpenRequest
{
    private readonly object _syncRoot = new();
    private string? _path;
    private string? _openedPath;
    private int? _closingDocumentId;

    public string? Set(string path)
    {
        lock (_syncRoot)
        {
            string? replacedPath = _path;
            _path = path;
            return replacedPath;
        }
    }

    public bool TryTake(out string? path)
    {
        lock (_syncRoot)
        {
            path = _path;
            _path = null;
            return path is not null;
        }
    }

    public void TrackOpened(string path)
    {
        lock (_syncRoot) _openedPath = path;
    }

    public void TrackClosing(int documentId, string path)
    {
        lock (_syncRoot)
        {
            if (_openedPath is not null
                && string.Equals(Path.GetFullPath(_openedPath), Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase))
                _closingDocumentId = documentId;
        }
    }

    public bool TryReleaseClosed(int documentId, out string? path)
    {
        lock (_syncRoot)
        {
            if (_closingDocumentId != documentId)
            {
                path = null;
                return false;
            }

            path = _openedPath;
            _openedPath = null;
            _closingDocumentId = null;
            return path is not null;
        }
    }
}
