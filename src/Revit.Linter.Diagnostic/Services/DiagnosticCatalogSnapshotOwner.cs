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

        DisposeComponents(Snapshot.ElementDiagnostics, Snapshot.DocumentDiagnostics);
    }

    /// <summary>Disposes the components owned by registrations that will never reach a snapshot.</summary>
    /// <remarks>
    /// Snapshot creation owns every component returned by the registration providers. When creation
    /// fails - a provider throws or the aggregated result fails validation - the collected
    /// registrations have no owner yet, so the factory disposes their components here instead of
    /// leaking them.
    /// </remarks>
    public static void DisposeComponents(
        IEnumerable<ElementDiagnosticRegistration> elementDiagnostics,
        IEnumerable<DocumentDiagnosticRegistration> documentDiagnostics)
    {
        var disposed = new HashSet<object>(ReferenceComparer.Instance);
        foreach (object component in GetOwnedComponents(elementDiagnostics, documentDiagnostics))
        {
            if (component is IDisposable disposable && disposed.Add(component))
                disposable.Dispose();
        }
    }

    private static IEnumerable<object> GetOwnedComponents(
        IEnumerable<ElementDiagnosticRegistration> elementDiagnostics,
        IEnumerable<DocumentDiagnosticRegistration> documentDiagnostics)
    {
        foreach (ElementDiagnosticRegistration registration in elementDiagnostics)
        {
            yield return registration.Diagnostic;
            yield return registration.Filter;
            yield return registration.DocumentFilter;
            foreach (IElementFix fix in registration.Fixes) yield return fix;
            foreach (IElementVisualizationPipeline pipeline in registration.VisualizationPipelines)
                yield return pipeline;
        }

        foreach (DocumentDiagnosticRegistration registration in documentDiagnostics)
        {
            yield return registration.Diagnostic;
            yield return registration.Filter;
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
