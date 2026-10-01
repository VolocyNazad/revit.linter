using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Revit.Linter.Updater.Core.Abstractions;
using Revit.Linter.Updater.Core.Models;

namespace Revit.Linter.Updater.Core.Services;

/// <summary>Reads the latest stable release from the public GitHub releases API.</summary>
public sealed class GitHubReleaseClient : IGitHubReleaseClient
{
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

        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("GitHub release request returned HTTP {StatusCode}", (int)response.StatusCode);
            throw new HttpRequestException(
                $"GitHub returned HTTP {(int)response.StatusCode}.", null, response.StatusCode);
        }

        await using Stream content = await response.Content.ReadAsStreamAsync(cancellationToken);
        using JsonDocument document = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);
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

    private static ReleaseInstaller? TryReadInstaller(JsonElement root, StableVersion version)
    {
        if (!root.TryGetProperty("assets", out JsonElement assets) || assets.ValueKind != JsonValueKind.Array)
            return null;

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
            return ReleaseInstaller.TryCreate(version, name, downloadUrl, size, digest, out ReleaseInstaller? installer)
                ? installer
                : null;
        }

        return null;
    }
}
