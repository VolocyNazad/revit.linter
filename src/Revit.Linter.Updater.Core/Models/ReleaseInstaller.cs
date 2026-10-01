using System.Diagnostics.CodeAnalysis;

namespace Revit.Linter.Updater.Core.Models;

/// <summary>Describes a validated unified MSI asset attached to a stable release.</summary>
public sealed class ReleaseInstaller
{
    private const long MaximumInstallerSize = 1024L * 1024 * 1024;

    private ReleaseInstaller(
        StableVersion version,
        string name,
        Uri downloadUri,
        long size,
        string sha256)
    {
        Version = version;
        Name = name;
        DownloadUri = downloadUri;
        Size = size;
        Sha256 = sha256;
    }

    /// <summary>Gets the stable version encoded by the asset name and URL.</summary>
    public StableVersion Version { get; }

    /// <summary>Gets the exact MSI file name.</summary>
    public string Name { get; }

    /// <summary>Gets the official HTTPS GitHub release download URL.</summary>
    public Uri DownloadUri { get; }

    /// <summary>Gets the expected file size in bytes.</summary>
    public long Size { get; }

    /// <summary>Gets the expected lowercase SHA-256 digest without an algorithm prefix.</summary>
    public string Sha256 { get; }

    /// <summary>Validates release asset metadata and creates an installer descriptor.</summary>
    public static bool TryCreate(
        StableVersion version,
        string? name,
        string? downloadUrl,
        long size,
        string? digest,
        [NotNullWhen(true)] out ReleaseInstaller? installer)
        => TryCreate(version, name, downloadUrl, size, digest, out installer, out _);

    internal static bool TryCreate(
        StableVersion version,
        string? name,
        string? downloadUrl,
        long size,
        string? digest,
        [NotNullWhen(true)] out ReleaseInstaller? installer,
        out string? failureReason)
    {
        installer = null;
        failureReason = null;
        string expectedName = $"RevitLinter-{version}.msi";
        string expectedPath = $"/VolocyNazad/revit.linter/releases/download/v{version}/{expectedName}";
        if (!string.Equals(name, expectedName, StringComparison.Ordinal))
        {
            failureReason = "unexpected asset name";
            return false;
        }
        if (size is <= 0 or > MaximumInstallerSize)
        {
            failureReason = "invalid asset size";
            return false;
        }
        if (!TryParseSha256(digest, out string? sha256))
        {
            failureReason = "missing or invalid SHA-256 digest";
            return false;
        }
        if (!Uri.TryCreate(downloadUrl, UriKind.Absolute, out Uri? downloadUri) ||
            downloadUri.Scheme != Uri.UriSchemeHttps ||
            !downloadUri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) ||
            !downloadUri.IsDefaultPort ||
            !string.IsNullOrEmpty(downloadUri.UserInfo) ||
            !string.IsNullOrEmpty(downloadUri.Query) ||
            !string.IsNullOrEmpty(downloadUri.Fragment) ||
            !downloadUri.AbsolutePath.Equals(expectedPath, StringComparison.Ordinal))
        {
            failureReason = "untrusted asset download URL";
            return false;
        }

        installer = new ReleaseInstaller(version, expectedName, downloadUri, size, sha256);
        return true;
    }

    private static bool TryParseSha256(string? digest, [NotNullWhen(true)] out string? sha256)
    {
        sha256 = null;
        const string prefix = "sha256:";
        if (digest is null || !digest.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        string candidate = digest[prefix.Length..];
        if (candidate.Length != 64 || candidate.Any(character => !Uri.IsHexDigit(character)))
            return false;

        sha256 = candidate.ToLowerInvariant();
        return true;
    }
}
