using System.Text;

namespace Revit.Linter.DiagnosticConfiguration.Infrastructure;

/// <summary>Writes diagnostic configuration text without losing the file's byte order mark.</summary>
/// <remarks>
/// The write is atomic through a temporary file and, where the file system supports it,
/// <see cref="File.Replace(string, string, string?)"/>. A restricted file system that refuses the atomic
/// replacement falls back to a direct write so the user's change is not lost at the cost of a short window
/// without atomicity. The existing byte order mark is preserved; a new file is written without one.
/// </remarks>
public static class DiagnosticConfigurationFileWriter
{
    /// <summary>Writes configuration text to the path.</summary>
    /// <param name="path">The configuration file path.</param>
    /// <param name="text">The complete configuration file content.</param>
    /// <exception cref="InvalidOperationException"><paramref name="path"/> has no directory.</exception>
    public static void WriteAtomically(string path, string text)
    {
        byte[] existing = File.Exists(path) ? File.ReadAllBytes(path) : [];
        bool hasBom = existing.Length >= 3
                      && existing[0] == 0xEF && existing[1] == 0xBB && existing[2] == 0xBF;
        string directory = Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException($"The configuration file path '{path}' has no directory.");
        Directory.CreateDirectory(directory);
        string temporaryPath = Path.Combine(directory, $"{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllText(temporaryPath, text, new UTF8Encoding(hasBom));
            if (File.Exists(path))
                File.Replace(temporaryPath, path, destinationBackupFileName: null);
            else
                File.Move(temporaryPath, path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // File.Replace is the atomic path; a restricted file system can refuse it. Falling back to a
            // direct write keeps the user's change, at the cost of a short window without atomicity.
            try
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
            catch (Exception cleanupException) when (cleanupException is IOException or UnauthorizedAccessException)
            {
                // The temporary file is left for the file system's cleanup; the change itself still lands.
            }

            File.WriteAllText(path, text, new UTF8Encoding(hasBom));
        }
    }
}
