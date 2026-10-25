using System.Text;
using Revit.Linter.DiagnosticConfiguration.Infrastructure;

namespace Revit.Linter.DiagnosticConfiguration.Tests;

public sealed class DiagnosticConfigurationFileWriterTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(), nameof(DiagnosticConfigurationFileWriterTests), Guid.NewGuid().ToString("N"));

    public DiagnosticConfigurationFileWriterTests() => Directory.CreateDirectory(_directory);

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
            // Best effort cleanup of a test directory.
        }
    }

    [Fact]
    public void WriteAtomically_preserves_the_byte_order_mark()
    {
        string path = Path.Combine(_directory, "diagnostics.config.yaml");
        File.WriteAllText(path, "version: 2\nrules: []\n", new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        DiagnosticConfigurationFileWriter.WriteAtomically(path, "version: 2\nrules: []\n# changed\n");

        byte[] bytes = File.ReadAllBytes(path);
        Assert.True(bytes.Length >= 3);
        Assert.Equal(0xEF, bytes[0]);
        Assert.Equal(0xBB, bytes[1]);
        Assert.Equal(0xBF, bytes[2]);
        Assert.Contains("# changed", File.ReadAllText(path), StringComparison.Ordinal);
    }

    [Fact]
    public void WriteAtomically_writes_a_new_file_without_a_byte_order_mark()
    {
        string path = Path.Combine(_directory, "nested", "diagnostics.config.yaml");

        DiagnosticConfigurationFileWriter.WriteAtomically(path, "version: 2\nrules: []\n");

        byte[] bytes = File.ReadAllBytes(path);
        Assert.True(bytes.Length > 0);
        Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);
    }
}
