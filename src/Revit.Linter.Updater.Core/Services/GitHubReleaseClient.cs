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
    private static readonly Uri LatestReleaseUri = new(
        "https://api.github.com/repos/VolocyNazad/revit.linter/releases/latest");
    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubReleaseClient> _logger;

    /// <summary>Creates a GitHub release client.</summary>
    public GitHubReleaseClient(HttpClient httpClient, ILogger<GitHubReleaseClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ReleaseInfo?> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseUri);
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

        return new ReleaseInfo(version, releasePage);
    }
}
