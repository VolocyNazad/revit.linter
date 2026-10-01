using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Revit.Linter.Updater.Core.Services;

namespace Revit.Linter.Updater.Core.Tests;

public sealed class GitHubReleaseClientTests
{
    [Fact]
    public async Task GetLatestAsync_StableRelease_ReturnsReleaseAndSendsUserAgent()
    {
        var handler = new StubHandler(HttpStatusCode.OK,
            """{"tag_name":"v2.3.4","html_url":"https://github.com/VolocyNazad/revit.linter/releases/tag/v2.3.4","draft":false,"prerelease":false}""");
        var client = CreateClient(handler);

        var release = await client.GetLatestAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(release);
        Assert.Equal("2.3.4", release.Version.ToString());
        Assert.Contains("Revit.Linter.Updater", handler.UserAgent);
        Assert.Contains("application/vnd.github+json", handler.Accept);
    }

    [Fact]
    public async Task GetLatestAsync_NotFound_ReturnsNull()
    {
        var client = CreateClient(new StubHandler(HttpStatusCode.NotFound, "{}"));

        var release = await client.GetLatestAsync(TestContext.Current.CancellationToken);

        Assert.Null(release);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task GetLatestAsync_NonStableRelease_ReturnsNull(bool draft, bool prerelease)
    {
        var body = $$"""{"tag_name":"v2.3.4","html_url":"https://example.com/release","draft":{{draft.ToString().ToLowerInvariant()}},"prerelease":{{prerelease.ToString().ToLowerInvariant()}}}""";
        var client = CreateClient(new StubHandler(HttpStatusCode.OK, body));

        var release = await client.GetLatestAsync(TestContext.Current.CancellationToken);

        Assert.Null(release);
    }

    [Fact]
    public async Task GetLatestAsync_ErrorResponse_ThrowsWithStatusCode()
    {
        var client = CreateClient(new StubHandler(HttpStatusCode.TooManyRequests, "{}"));

        HttpRequestException exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => client.GetLatestAsync(TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.TooManyRequests, exception.StatusCode);
    }

    [Theory]
    [InlineData("release", "https://example.com/release")]
    [InlineData("v2.3.4", "http://example.com/release")]
    [InlineData("v2.3.4", "not-a-url")]
    public async Task GetLatestAsync_InvalidReleaseData_ThrowsJsonException(string tag, string page)
    {
        string body = $$"""{"tag_name":"{{tag}}","html_url":"{{page}}","draft":false,"prerelease":false}""";
        var client = CreateClient(new StubHandler(HttpStatusCode.OK, body));

        await Assert.ThrowsAnyAsync<System.Text.Json.JsonException>(
            () => client.GetLatestAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetLatestAsync_MalformedJson_ThrowsJsonException()
    {
        var client = CreateClient(new StubHandler(HttpStatusCode.OK, "{ invalid"));

        await Assert.ThrowsAnyAsync<System.Text.Json.JsonException>(
            () => client.GetLatestAsync(TestContext.Current.CancellationToken));
    }

    private static GitHubReleaseClient CreateClient(HttpMessageHandler handler) =>
        new(new HttpClient(handler), NullLogger<GitHubReleaseClient>.Instance);

    private sealed class StubHandler(HttpStatusCode statusCode, string content) : HttpMessageHandler
    {
        public string UserAgent { get; private set; } = string.Empty;
        public string Accept { get; private set; } = string.Empty;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            UserAgent = request.Headers.UserAgent.ToString();
            Accept = request.Headers.Accept.ToString();
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            });
        }
    }
}
