using System.Net.Http.Headers;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Revit.Linter.Updater.Core.Models;

namespace Revit.Linter.Updater.Core.Services;

/// <summary>Downloads and verifies a unified MSI release asset without launching it.</summary>
public sealed class InstallerDownloadService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InstallerDownloadService> _logger;

    /// <summary>Creates a verified installer downloader.</summary>
    public InstallerDownloadService(
        HttpClient httpClient,
        ILogger<InstallerDownloadService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>Downloads an installer to the supplied directory after size and SHA-256 verification.</summary>
    /// <remarks>
    /// Data is written to a uniquely named partial file and moved to its final name only after every
    /// check succeeds. Partial or mismatched files are deleted. This method never launches the MSI.
    /// </remarks>
    public async Task<string> DownloadAsync(
        ReleaseInstaller installer,
        string destinationDirectory,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(destinationDirectory);
        string finalPath = Path.Combine(destinationDirectory, installer.Name);
        string partialPath = Path.Combine(destinationDirectory, $".{installer.Name}.{Guid.NewGuid():N}.partial");

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, installer.DownloadUri);
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Revit.Linter.Updater", "1.0"));
            using HttpResponseMessage response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            response.EnsureSuccessStatusCode();

            if (response.Content.Headers.ContentLength is { } contentLength &&
                contentLength != installer.Size)
            {
                throw new InvalidDataException(
                    $"Installer content length {contentLength} does not match expected size {installer.Size}.");
            }

            long bytesWritten;
            string actualDigest;
            await using (Stream input = await response.Content.ReadAsStreamAsync(cancellationToken))
            await using (var output = new FileStream(
                             partialPath,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             bufferSize: 81920,
                             FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                byte[] buffer = new byte[81920];
                bytesWritten = 0;
                int bytesRead;
                while ((bytesRead = await input.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    bytesWritten += bytesRead;
                    if (bytesWritten > installer.Size)
                        throw new InvalidDataException("Installer exceeds its declared size.");

                    hash.AppendData(buffer, 0, bytesRead);
                    await output.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                }
                await output.FlushAsync(cancellationToken);
                actualDigest = Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
            }

            if (bytesWritten != installer.Size)
                throw new InvalidDataException("Installer size does not match release metadata.");

            if (!CryptographicOperations.FixedTimeEquals(
                    Convert.FromHexString(actualDigest),
                    Convert.FromHexString(installer.Sha256)))
            {
                throw new InvalidDataException("Installer SHA-256 does not match release metadata.");
            }

            File.Move(partialPath, finalPath, overwrite: true);
            _logger.LogInformation(
                "Downloaded and verified installer {InstallerName} ({InstallerSize} bytes)",
                installer.Name,
                installer.Size);
            return finalPath;
        }
        catch
        {
            try
            {
                File.Delete(partialPath);
            }
            catch (Exception cleanupException) when (
                cleanupException is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(cleanupException, "Failed to delete partial installer {PartialPath}", partialPath);
            }
            throw;
        }
    }
}
