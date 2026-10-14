using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Revit.Linter.DocumentQueries.Abstractions.Models;
using Revit.Linter.DocumentQueries.Abstractions.Services;
using Revit.Linter.ElementIgnoring.Abstractions.Models;
using Revit.Linter.ElementIgnoring.Abstractions.Services;
using System.Text.Json;

namespace Revit.Linter.ElementIgnoring.Services;

internal sealed class IgnoreElementManager(
    IDocumentQueryService documentQueries,
    ILogger<IgnoreElementManager>? logger = null)
    : IIgnoreElementDetector, IIgnoreElementProvider, IIgnoreListManager
{
    private const string IgnoreStateQuery = "element-ignoring:ignore-state";
    private const string VersionField = "SchemaVersion";
    private const string PayloadField = "Payload";
    private const int SchemaVersion = 2;

    private static readonly Guid SchemaId = new("b7c9d2e4-1f3a-4b5c-8d6e-0f1a2b3c4d5e");
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly ILogger<IgnoreElementManager> _logger = logger ?? NullLogger<IgnoreElementManager>.Instance;

    public IgnoreElementFeedback Ignore(string code, Element element)
    {
        if (element is null || string.IsNullOrWhiteSpace(code))
            return Failed("storageFailed_message");
        return AddToIgnoreList(code, [element]);
    }

    public IgnoreElementFeedback IgnoreMany(string code, IEnumerable<Element> elements)
    {
        if (elements is null || string.IsNullOrWhiteSpace(code))
            return Failed("storageFailed_message");
        return AddToIgnoreList(code, elements.Where(i => i is not null).ToArray());
    }

    public IgnoreElementFeedback IgnoreDocument(string code, Document document)
    {
        if (document is null || string.IsNullOrWhiteSpace(code))
            return Failed("storageFailed_message");
        if (!IsWritableDocument(document))
            return Failed("foreignDocument_message");
        try
        {
            List<StoredIgnore> stored = ReadEntries(document);
            if (stored.Any(i => i.Scope == IgnoreScope.Document && string.Equals(i.Code, code, StringComparison.Ordinal)))
                return IgnoreElementFeedback.Success();
            stored.Add(new StoredIgnore { Id = 0, Code = code, UniqueId = string.Empty, Scope = IgnoreScope.Document });
            WriteEntries(document, stored);
            GetState(document).Invalidate();
            _logger.LogInformation(
                "Ignored diagnostic {DiagnosticCode} for the whole document {DocumentTitle}",
                code, document.Title);
            return IgnoreElementFeedback.Success();
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to ignore diagnostic {DiagnosticCode} for the whole document {DocumentTitle}",
                code, document.Title);
            return Failed("storageFailed_message");
        }
    }

    // A diagnostic run asks this for every relevant element of every diagnostic. Reading the ignore
    // storage through the Revit API each time would dominate the run, so the answer is assembled once
    // per document and kept in the transaction cache.
    public bool IsElementIgnored(string code, Element element)
    {
        try
        {
            return GetState(element.Document).IsIgnored(code, element);
        }
        catch (Exception exception)
        {
            // A detector failure must never hide or invent findings silently: report no ignore and
            // let the diagnostic evaluate the element normally.
            _logger.LogError(exception, "Failed to check the ignore list for diagnostic {DiagnosticCode}", code);
            return false;
        }
    }

    public bool IsDocumentIgnored(string code, Document document)
    {
        try
        {
            return GetState(document).IsDocumentIgnored(code);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to check the ignore list for diagnostic {DiagnosticCode}", code);
            return false;
        }
    }

    public IIgnoredElements GetIgnoredElements(Document document) => GetState(document);

    public IReadOnlyList<IgnoreListEntry> GetEntries(Document document)
    {
        List<StoredIgnore> stored = LoadSnapshot(document);
        List<IgnoreListEntry> result = new(stored.Count);
        foreach (StoredIgnore entry in stored)
        {
            if (entry.Scope == IgnoreScope.Document)
            {
                result.Add(new IgnoreListEntry(null, entry.Code, string.Empty, true));
                continue;
            }
            result.Add(new IgnoreListEntry(entry.Id, entry.Code, entry.UniqueId, FindElement(document, entry.Id) is not null));
        }
        return result;
    }

    public bool Remove(string code, Element element)
    {
        if (element is null || string.IsNullOrWhiteSpace(code)) return false;
        Document? document = OwnDocument(element);
        if (document is null) return false;
        try
        {
            List<StoredIgnore> stored = ReadEntries(document);
            int beforePurge = stored.Count;
            stored.RemoveAll(i => i.Scope == IgnoreScope.Element && FindElement(document, i.Id) is null);
            bool purged = stored.Count != beforePurge;
            long idValue = ElementIdValue(element.Id);
            int removed = stored.RemoveAll(i => i.Scope == IgnoreScope.Element
                && i.Id == idValue
                && string.Equals(i.Code, code, StringComparison.Ordinal));
            if (removed == 0 && !purged) return false;
            WriteEntries(document, stored);
            GetState(document).Invalidate();
            _logger.LogInformation(
                "Removed the ignore entry of element {ElementId} for diagnostic {DiagnosticCode} in {DocumentTitle}",
                idValue, code, document.Title);
            return true;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to remove the ignore entry of an element for diagnostic {DiagnosticCode} in {DocumentTitle}",
                code, document.Title);
            return false;
        }
    }

    public bool RemoveDocument(string code, Document document)
    {
        if (document is null || string.IsNullOrWhiteSpace(code)) return false;
        if (!IsWritableDocument(document)) return false;
        try
        {
            List<StoredIgnore> stored = ReadEntries(document);
            int beforePurge = stored.Count;
            stored.RemoveAll(i => i.Scope == IgnoreScope.Element && FindElement(document, i.Id) is null);
            bool purged = stored.Count != beforePurge;
            int removed = stored.RemoveAll(i => i.Scope == IgnoreScope.Document
                && string.Equals(i.Code, code, StringComparison.Ordinal));
            if (removed == 0 && !purged) return false;
            WriteEntries(document, stored);
            GetState(document).Invalidate();
            _logger.LogInformation(
                "Removed the document-level ignore entry for diagnostic {DiagnosticCode} in {DocumentTitle}",
                code, document.Title);
            return true;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to remove the document-level ignore entry for diagnostic {DiagnosticCode} in {DocumentTitle}",
                code, document.Title);
            return false;
        }
    }

    public int RemoveMissing(Document document)
    {
        if (document is null) return 0;
        try
        {
            List<StoredIgnore> stored = ReadEntries(document);
            int before = stored.Count;
            stored.RemoveAll(i => i.Scope == IgnoreScope.Element && FindElement(document, i.Id) is null);
            int removed = before - stored.Count;
            if (removed == 0) return 0;
            WriteEntries(document, stored);
            GetState(document).Invalidate();
            _logger.LogInformation(
                "Removed {EntryCount} missing ignore entries in {DocumentTitle}", removed, document.Title);
            return removed;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception, "Failed to remove missing ignore entries in {DocumentTitle}", document.Title);
            return 0;
        }
    }

    public int Clear(Document document, string? code = null)
    {
        if (document is null) return 0;
        try
        {
            List<StoredIgnore> stored = ReadEntries(document);
            int before = stored.Count;
            stored.RemoveAll(i => i.Scope == IgnoreScope.Element && FindElement(document, i.Id) is null);
            if (code is null)
                stored.Clear();
            else
                stored.RemoveAll(i => string.Equals(i.Code, code, StringComparison.Ordinal));
            int removed = before - stored.Count;
            if (removed == 0) return 0;
            WriteEntries(document, stored);
            GetState(document).Invalidate();
            _logger.LogInformation(
                "Cleared {EntryCount} ignore entries in {DocumentTitle}", removed, document.Title);
            return removed;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception, "Failed to clear ignore entries in {DocumentTitle}", document.Title);
            return 0;
        }
    }

    private IgnoreElementFeedback AddToIgnoreList(string code, IReadOnlyList<Element> targets)
    {
        if (targets.Count == 0) return IgnoreElementFeedback.Success();
        if (targets.Any(i => i is DataStorage))
            return Failed("unsupportedElement_message");
        Document? document = OwnDocument(targets[0]);
        if (document is null || targets.Any(i => !document.Equals(OwnDocument(i))))
            return Failed("foreignDocument_message");
        try
        {
            List<StoredIgnore> stored = ReadEntries(document);
            // Missing entries are dropped on every write, so no explicit cleanup pass is needed.
            int beforePurge = stored.Count;
            stored.RemoveAll(i => i.Scope == IgnoreScope.Element && FindElement(document, i.Id) is null);
            bool changed = stored.Count != beforePurge;
            foreach (Element target in targets)
            {
                long idValue = ElementIdValue(target.Id);
                StoredIgnore? existing = stored.Find(
                    i => i.Id == idValue && string.Equals(i.Code, code, StringComparison.Ordinal));
                if (existing is null)
                {
                    stored.Add(new StoredIgnore { Id = idValue, Code = code, UniqueId = target.UniqueId });
                    changed = true;
                }
                else if (!string.Equals(existing.UniqueId, target.UniqueId, StringComparison.Ordinal))
                {
                    // The identifier was reused by another element: adopt it for the new one.
                    existing.UniqueId = target.UniqueId;
                    changed = true;
                }
            }

            if (changed)
            {
                WriteEntries(document, stored);
                // The cache is invalidated only when the transaction that changed the storage is
                // committed, so the state is dropped (never patched) to keep a check made inside that
                // transaction correct. A rollback leaves the dropped state behind: the next check
                // reloads the reverted storage, so rollbacks stay correct by construction.
                GetState(document).Invalidate();
            }

            _logger.LogInformation(
                "Ignored {ElementCount} elements for diagnostic {DiagnosticCode} in {DocumentTitle}",
                targets.Count, code, document.Title);
            return IgnoreElementFeedback.Success();
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to ignore {ElementCount} elements for diagnostic {DiagnosticCode} in {DocumentTitle}",
                targets.Count, code, document.Title);
            return Failed("storageFailed_message");
        }
    }

    private static IgnoreElementFeedback Failed(string messageKey)
        => IgnoreElementFeedback.Failed(ElementIgnoringLocalizations.GetString(messageKey));

    private IgnoreState GetState(Document document)
        => documentQueries.GetOrCreate(
            DocumentQueryKey.Create(document, IgnoreStateQuery),
            () => new IgnoreState(document, LoadSnapshot(document), LoadSnapshot));

    // A corrupt payload cannot be parsed back, so it is reported once and treated as empty. Anything
    // else (no Revit context, a closed document) is not a data problem and must surface to the caller:
    // the provider turns it into a failure instead of overwriting the stored list with an empty one.
    private List<StoredIgnore> ReadEntries(Document document)
    {
        Schema schema = EnsureSchema();
        DataStorage? storage = FindStorage(document, schema.GUID);
        if (storage is null) return [];
        Entity entity = storage.GetEntity(schema);
        if (!entity.IsValid()) return [];
        if (entity.Get<int>(VersionField) > SchemaVersion)
        {
            _logger.LogError(
                "The ignore list of {DocumentTitle} was written by a newer product version and will be treated as empty",
                document.Title);
            return [];
        }
        string? payload = entity.Get<string>(PayloadField);
        if (string.IsNullOrEmpty(payload)) return [];
        try
        {
            List<StoredIgnore>? stored = JsonSerializer.Deserialize<List<StoredIgnore>>(payload, JsonOptions);
            return stored?.Where(i => i is not null && !string.IsNullOrWhiteSpace(i.Code)).ToList() ?? [];
        }
        catch (JsonException exception)
        {
            _logger.LogError(
                exception, "The ignore list of {DocumentTitle} is corrupted and will be treated as empty",
                document.Title);
            return [];
        }
    }

    // The detector must never fail a diagnostic run: an unreadable list behaves like an empty one.
    private List<StoredIgnore> LoadSnapshot(Document document)
    {
        try
        {
            return ReadEntries(document);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception, "Failed to read the ignore list of {DocumentTitle}", document.Title);
            return [];
        }
    }

    private static void WriteEntries(Document document, List<StoredIgnore> stored)
    {
        Schema schema = EnsureSchema();
        DataStorage? storage = FindStorage(document, schema.GUID);
        if (stored.Count == 0)
        {
            if (storage is not null) document.Delete(storage.Id);
            return;
        }

        storage ??= DataStorage.Create(document);
        Entity entity = new(schema);
        entity.Set(VersionField, SchemaVersion);
        entity.Set(PayloadField, JsonSerializer.Serialize(stored, JsonOptions));
        storage.SetEntity(entity);
    }

    private static Schema EnsureSchema()
    {
        Schema? schema = Schema.Lookup(SchemaId);
        if (schema is not null) return schema;
        using SchemaBuilder builder = new(SchemaId);
        builder.SetSchemaName("RevitLinter_IgnoreList");
        builder.SetVendorId("Volocy");
        builder.SetDocumentation("Elements ignored by Revit Linter diagnostics, one entry per ignored element and diagnostic code.");
        builder.SetReadAccessLevel(AccessLevel.Public);
        builder.SetWriteAccessLevel(AccessLevel.Public);
        builder.AddSimpleField(VersionField, typeof(int));
        builder.AddSimpleField(PayloadField, typeof(string));
        return builder.Finish();
    }

    private static DataStorage? FindStorage(Document document, Guid schemaId)
        => new FilteredElementCollector(document)
            .OfClass(typeof(DataStorage))
            .WherePasses(new ExtensibleStorageFilter(schemaId))
            .Cast<DataStorage>()
            .FirstOrDefault();

    private static Element? FindElement(Document document, long idValue)
    {
        try
        {
            return document.GetElement(ToElementId(idValue));
        }
        catch
        {
            return null;
        }
    }

    // Only elements of a writable host document can be ignored: entries of linked documents would land
    // in storage the user cannot modify, and the ignore storage itself is not a diagnosable element.
    private static Document? OwnDocument(Element element)
    {
        try
        {
            return IsWritableDocument(element.Document) ? element.Document : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsWritableDocument(Document? document)
    {
        try
        {
            return document is not null && document.IsValidObject && !document.IsLinked;
        }
        catch
        {
            return false;
        }
    }

    private static long ElementIdValue(ElementId id) =>
#if BEFORE2024
        id.IntegerValue;
#else
        id.Value;
#endif

    private static ElementId ToElementId(long value) =>
#if BEFORE2024
        new ElementId((int)value);
#else
        new ElementId(value);
#endif

    private sealed class StoredIgnore
    {
        public long Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string UniqueId { get; set; } = string.Empty;
        public IgnoreScope Scope { get; set; } = IgnoreScope.Element;
    }

    private enum IgnoreScope
    {
        Element,
        Document
    }

    private sealed class IgnoreState : IIgnoredElements
    {
        private readonly Document _document;
        private readonly Func<Document, List<StoredIgnore>> _reload;
        private Dictionary<long, Dictionary<string, string>> _codes;
        private HashSet<string> _documentCodes;
        private bool _invalidated;

        public IgnoreState(
            Document document,
            List<StoredIgnore> snapshot,
            Func<Document, List<StoredIgnore>> reload)
        {
            _document = document;
            _reload = reload;
            _codes = ToLookup(snapshot, out HashSet<string> documentCodes);
            _documentCodes = documentCodes;
        }

        public void Invalidate()
        {
            _codes.Clear();
            _documentCodes.Clear();
            _invalidated = true;
        }

        public bool IsIgnored(string code, Element element)
        {
            if (IsDocumentIgnored(code)) return true;
            if (!element.Document.Equals(_document)) return false;
            EnsureLoaded();
            if (!_codes.TryGetValue(ElementIdValue(element.Id), out Dictionary<string, string>? byCode))
                return false;
            if (!byCode.TryGetValue(code, out string? uniqueId)) return false;
            if (string.IsNullOrEmpty(uniqueId)) return true;
            try
            {
                return string.Equals(uniqueId, element.UniqueId, StringComparison.Ordinal);
            }
            catch
            {
                return true;
            }
        }

        public bool IsDocumentIgnored(string code)
        {
            EnsureLoaded();
            return _documentCodes.Contains(code);
        }

        private void EnsureLoaded()
        {
            if (!_invalidated) return;
            // Reloaded after a write inside the same transaction: the storage read sees the
            // uncommitted change, and after a rollback it sees the reverted storage.
            _codes = ToLookup(_reload(_document), out HashSet<string> documentCodes);
            _documentCodes = documentCodes;
            _invalidated = false;
        }

        private static Dictionary<long, Dictionary<string, string>> ToLookup(
            List<StoredIgnore> snapshot, out HashSet<string> documentCodes)
        {
            Dictionary<long, Dictionary<string, string>> result = [];
            documentCodes = new HashSet<string>(StringComparer.Ordinal);
            foreach (StoredIgnore entry in snapshot)
            {
                if (entry.Scope == IgnoreScope.Document)
                {
                    documentCodes.Add(entry.Code);
                    continue;
                }
                if (!result.TryGetValue(entry.Id, out Dictionary<string, string>? byCode))
                {
                    byCode = new Dictionary<string, string>(StringComparer.Ordinal);
                    result[entry.Id] = byCode;
                }
                byCode[entry.Code] = entry.UniqueId;
            }
            return result;
        }
    }
}
