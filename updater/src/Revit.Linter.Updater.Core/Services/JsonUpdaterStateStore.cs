using System.Text.Json;
using Revit.Linter.Updater.Core.Abstractions;
using Revit.Linter.Updater.Core.Models;

namespace Revit.Linter.Updater.Core.Services;

/// <summary>Stores updater state as JSON in the current user's local application data.</summary>
public sealed class JsonUpdaterStateStore : IUpdaterStateStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };
    private readonly string _path;

    /// <summary>Creates a state store for the conventional per-user updater path.</summary>
    public JsonUpdaterStateStore()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Volocy", "Revit.Linter", "updater", "state.json"))
    {
    }

    /// <summary>Creates a state store at an explicit path.</summary>
    /// <param name="path">Full path to the JSON state file.</param>
    public JsonUpdaterStateStore(string path) => _path = path;

    /// <inheritdoc />
    public async Task<UpdaterState> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_path))
            return new UpdaterState();

        await using FileStream stream = File.OpenRead(_path);
        return await JsonSerializer.DeserializeAsync<UpdaterState>(stream, SerializerOptions, cancellationToken)
            ?? new UpdaterState();
    }

    /// <inheritdoc />
    public async Task SaveAsync(UpdaterState state, CancellationToken cancellationToken = default)
    {
        string? directory = Path.GetDirectoryName(_path);
        if (string.IsNullOrEmpty(directory))
            throw new InvalidOperationException("The updater state path has no parent directory.");

        Directory.CreateDirectory(directory);
        string temporaryPath = _path + ".tmp";
        await using (FileStream stream = File.Create(temporaryPath))
            await JsonSerializer.SerializeAsync(stream, state, SerializerOptions, cancellationToken);

        File.Move(temporaryPath, _path, true);
    }
}
