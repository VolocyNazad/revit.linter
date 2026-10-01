using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Revit.Linter.Updater.Core.Abstractions;
using Revit.Linter.Updater.Core.Models;

namespace Revit.Linter.Updater.Core.Services;

/// <summary>Reads the latest stable release from the public GitHub releases API.</summary>
/// <remarks>
/// Requests a pinned GitHub API version and rejects response bodies larger than one megabyte.
/// Redirect behavior is owned by the injected HTTP transport.
/// </remarks>
public sealed class GitHubReleaseClient : IGitHubReleaseClient
{
    private const int MaximumResponseSize = 1024 * 1024;
    private readonly HttpClient _httpClient;
    private readonly Uri _latestReleaseUri;
    private readonly ILogger<GitHubReleaseClient> _logger;

    /// <summary>Creates a GitHub release client for an absolute HTTPS endpoint.</summary>
    public GitHubReleaseClient(
        HttpClient httpClient,
        Uri latestReleaseUri,
        ILogger<GitHubReleaseClient> logger)
    {
        if (!latestReleaseUri.IsAbsoluteUri || latestReleaseUri.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException("Release API endpoint must be an absolute HTTPS URI.", nameof(latestReleaseUri));

        _httpClient = httpClient;
        _latestReleaseUri = latestReleaseUri;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ReleaseInfo?> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, _latestReleaseUri);
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Revit.Linter.Updater", "1.0"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");

        using HttpResponseMessage response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("GitHub release request returned HTTP {StatusCode}", (int)response.StatusCode);
            throw new HttpRequestException(
                $"GitHub returned HTTP {(int)response.StatusCode}.", null, response.StatusCode);
        }

        using JsonDocument document = await ReadDocumentAsync(response.Content, cancellationToken);
        JsonElement root = document.RootElement;

        string? tag = root.TryGetProperty("tag_name", out JsonElement tagElement)
            ? tagElement.GetString()
            : null;
        string? page = root.TryGetProperty("html_url", out JsonElement pageElement)
            ? pageElement.GetString()
            : null;
        bool draft = root.TryGetProperty("draft", out JsonElement draftElement) && draftElement.GetBoolean();
        bool prerelease = root.TryGetProperty("prerelease", out JsonElement prereleaseElement) && prereleaseElement.GetBoolean();

        if (draft || prerelease)
            return null;
        if (!StableVersion.TryParse(tag, out StableVersion version) ||
            !Uri.TryCreate(page, UriKind.Absolute, out Uri? releasePage) ||
            releasePage.Scheme != Uri.UriSchemeHttps)
        {
            throw new JsonException("GitHub release response contains invalid tag_name or html_url.");
        }

        ReleaseInstaller? installer = TryReadInstaller(root, version);
        return new ReleaseInfo(version, releasePage, installer);
    }

    private static async Task<JsonDocument> ReadDocumentAsync(
        HttpContent content,
        CancellationToken cancellationToken)
    {
        if (content.Headers.ContentLength is > MaximumResponseSize)
            throw new InvalidDataException("GitHub release response exceeds the allowed size.");

        await using Stream input = await content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();
        byte[] chunk = new byte[81920];
        int totalBytes = 0;
        int bytesRead;
        while ((bytesRead = await input.ReadAsync(chunk, cancellationToken)) > 0)
        {
            totalBytes += bytesRead;
            if (totalBytes > MaximumResponseSize)
                throw new InvalidDataException("GitHub release response exceeds the allowed size.");
            await buffer.WriteAsync(chunk.AsMemory(0, bytesRead), cancellationToken);
        }

        buffer.Position = 0;
        return await JsonDocument.ParseAsync(buffer, cancellationToken: cancellationToken);
    }

    private ReleaseInstaller? TryReadInstaller(JsonElement root, StableVersion version)
    {
        if (!root.TryGetProperty("assets", out JsonElement assets) || assets.ValueKind != JsonValueKind.Array)
        {
            _logger.LogWarning(
                "GitHub release {ReleaseVersion} does not contain an assets array",
                version);
            return null;
        }

        string expectedName = $"RevitLinter-{version}.msi";
        foreach (JsonElement asset in assets.EnumerateArray())
        {
            string? name = asset.TryGetProperty("name", out JsonElement nameElement)
                ? nameElement.GetString()
                : null;
            if (!string.Equals(name, expectedName, StringComparison.Ordinal))
                continue;

            string? downloadUrl = asset.TryGetProperty("browser_download_url", out JsonElement urlElement)
                ? urlElement.GetString()
                : null;
            string? digest = asset.TryGetProperty("digest", out JsonElement digestElement)
                ? digestElement.GetString()
                : null;
            long size = asset.TryGetProperty("size", out JsonElement sizeElement) && sizeElement.TryGetInt64(out long value)
                ? value
                : 0;
            if (ReleaseInstaller.TryCreate(
                    version,
                    name,
                    downloadUrl,
                    size,
                    digest,
                    out ReleaseInstaller? installer,
                    out string? failureReason))
            {
                return installer;
            }

            _logger.LogWarning(
                "Ignored installer asset {InstallerName} for release {ReleaseVersion}: {FailureReason}",
                expectedName,
                version,
                failureReason);
            return null;
        }

        _logger.LogWarning(
            "GitHub release {ReleaseVersion} does not contain expected installer asset {InstallerName}",
            version,
            expectedName);
        return null;
    }
}
