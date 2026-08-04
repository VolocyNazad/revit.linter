namespace Revit.Linter.Diagnostic.Services;

using System.Runtime.CompilerServices;

internal sealed class DiagnosticCatalogSnapshotOwner : IDisposable
{
    private bool _disposed;

    public DiagnosticCatalogSnapshotOwner(DiagnosticCatalogSnapshot snapshot)
    {
        Snapshot = snapshot;
    }

    public DiagnosticCatalogSnapshot Snapshot { get; }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        var disposed = new HashSet<object>(ReferenceComparer.Instance);
        foreach (object component in GetOwnedComponents())
        {
            if (component is IDisposable disposable && disposed.Add(component))
                disposable.Dispose();
        }
    }

    private IEnumerable<object> GetOwnedComponents()
    {
        foreach (ElementDiagnosticRegistration registration in Snapshot.ElementDiagnostics)
        {
            yield return registration.Diagnostic;
            yield return registration.Filter;
            yield return registration.DocumentFilter;
            yield return registration.Override;
            foreach (IElementFix fix in registration.Fixes) yield return fix;
        }

        foreach (DocumentDiagnosticRegistration registration in Snapshot.DocumentDiagnostics)
        {
            yield return registration.Diagnostic;
            yield return registration.Filter;
            yield return registration.Override;
            foreach (IDocumentFix fix in registration.Fixes) yield return fix;
        }
    }

    private sealed class ReferenceComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceComparer Instance = new();

        bool IEqualityComparer<object>.Equals(object? x, object? y) => ReferenceEquals(x, y);

        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }
}