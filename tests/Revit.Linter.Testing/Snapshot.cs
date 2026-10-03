using System.Runtime.CompilerServices;
using System.Text;

namespace Revit.Linter.Testing;

/// <summary>Compares generated text with an approved snapshot file stored next to the tests.</summary>
/// <remarks>
/// The approved file is <c>{owner}.{test}.verified.{extension}</c>. On a mismatch, or when no approved file
/// exists yet, the actual text is written beside it as <c>*.received.*</c> (ignored by Git) and the check
/// fails. To accept a change, review the received file and rename it over the verified one. Line endings
/// and trailing line breaks are ignored so snapshots survive Git line-ending conversion.
/// </remarks>
public static class Snapshot
{
    /// <summary>Verifies <paramref name="actual"/> against the approved snapshot of the calling test.</summary>
    /// <param name="snapshotDirectory">The snapshot folder, relative to the repository root.</param>
    /// <param name="owner">The name that prefixes the snapshot files, normally the test class name.</param>
    /// <param name="extension">The snapshot file extension without a leading dot.</param>
    /// <param name="actual">The text produced by the code under test.</param>
    /// <param name="testName">The snapshot name; defaults to the calling test method.</param>
    /// <exception cref="SnapshotMismatchException">
    /// The approved snapshot is missing or differs from <paramref name="actual"/>.
    /// </exception>
    public static void Match(
        string snapshotDirectory,
        string owner,
        string extension,
        string actual,
        [CallerMemberName] string testName = "")
    {
        string directory = Path.Combine(RepositoryRoot.Find(), snapshotDirectory);
        string verifiedPath = Path.Combine(directory, $"{owner}.{testName}.verified.{extension}");
        string receivedPath = Path.Combine(directory, $"{owner}.{testName}.received.{extension}");

        string normalizedActual = Normalize(actual);
        string? normalizedVerified = File.Exists(verifiedPath) ? Normalize(File.ReadAllText(verifiedPath)) : null;
        if (string.Equals(normalizedVerified, normalizedActual, StringComparison.Ordinal))
        {
            if (File.Exists(receivedPath))
                File.Delete(receivedPath);
            return;
        }

        Directory.CreateDirectory(directory);
        File.WriteAllText(receivedPath, actual, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        throw new SnapshotMismatchException(normalizedVerified is null
            ? $"No approved snapshot exists. Review '{receivedPath}' and rename it to '{verifiedPath}'."
            : $"The output differs from '{verifiedPath}' at line {FindFirstDifferentLine(normalizedVerified, normalizedActual)}. " +
              $"Review '{receivedPath}' and rename it over the verified file to accept the change.");
    }

    private static string Normalize(string text) =>
        text.Replace("\r\n", "\n").Replace('\r', '\n').TrimEnd('\n');

    private static int FindFirstDifferentLine(string expected, string actual)
    {
        string[] expectedLines = expected.Split('\n');
        string[] actualLines = actual.Split('\n');
        int common = Math.Min(expectedLines.Length, actualLines.Length);
        for (int index = 0; index < common; index++)
        {
            if (!string.Equals(expectedLines[index], actualLines[index], StringComparison.Ordinal))
                return index + 1;
        }

        return common + 1;
    }
}

/// <summary>Thrown when generated text does not match its approved snapshot.</summary>
public sealed class SnapshotMismatchException : Exception
{
    /// <summary>Creates the exception with a message that names the snapshot files to review.</summary>
    /// <param name="message">The description of the mismatch.</param>
    public SnapshotMismatchException(string message) : base(message)
    {
    }
}
