using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Revit.Linter.Updater.Core.Models;
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
    public async Task GetLatestAsync_ValidInstallerAsset_ReturnsValidatedInstaller()
    {
        const string body = """
            {
              "tag_name": "v2.3.4",
              "html_url": "https://github.com/VolocyNazad/revit.linter/releases/tag/v2.3.4",
              "draft": false,
              "prerelease": false,
              "assets": [{
                "name": "RevitLinter-2.3.4.msi",
                "browser_download_url": "https://github.com/VolocyNazad/revit.linter/releases/download/v2.3.4/RevitLinter-2.3.4.msi",
                "size": 4096,
                "digest": "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
              }]
            }
            """;
        var client = CreateClient(new StubHandler(HttpStatusCode.OK, body));

        ReleaseInfo? release = await client.GetLatestAsync(TestContext.Current.CancellationToken);

        ReleaseInstaller installer = Assert.IsType<ReleaseInstaller>(release?.Installer);
        Assert.Equal("RevitLinter-2.3.4.msi", installer.Name);
        Assert.Equal(4096, installer.Size);
    }

    [Theory]
    [InlineData("RevitLinter-2.3.4-x64.msi", "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "does not contain expected installer asset")]
    [InlineData("RevitLinter-2.3.4.msi", "sha256:invalid", "missing or invalid SHA-256 digest")]
    public async Task GetLatestAsync_UntrustedInstallerAsset_LogsReasonAndDoesNotExposeInstaller(
        string name,
        string digest,
        string expectedLog)
    {
        string body = $$"""
            {
              "tag_name": "v2.3.4",
              "html_url": "https://github.com/VolocyNazad/revit.linter/releases/tag/v2.3.4",
              "draft": false,
              "prerelease": false,
              "assets": [{
                "name": "{{name}}",
                "browser_download_url": "https://github.com/VolocyNazad/revit.linter/releases/download/v2.3.4/{{name}}",
                "size": 4096,
                "digest": "{{digest}}"
              }]
            }
            """;
        var logger = new RecordingLogger();
        var client = CreateClient(new StubHandler(HttpStatusCode.OK, body), logger: logger);

        ReleaseInfo? release = await client.GetLatestAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(release);
        Assert.Null(release.Installer);
        Assert.Contains(logger.Messages, message => message.Contains(expectedLog, StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetLatestAsync_NotFound_ReturnsNull()
    {
        var client = CreateClient(new StubHandler(HttpStatusCode.NotFound, "{}"));

        var release = await client.GetLatestAsync(TestContext.Current.CancellationToken);

        Assert.Null(release);
    }

    [Fact]
    public async Task GetLatestAsync_UsesConfiguredEndpoint()
    {
        var handler = new StubHandler(HttpStatusCode.NotFound, "{}");
        var endpoint = new Uri("https://updates.example.test/releases/latest");
        var client = CreateClient(handler, endpoint);

        await client.GetLatestAsync(TestContext.Current.CancellationToken);

        Assert.Equal(endpoint, handler.RequestUri);
    }

    [Theory]
    [InlineData("relative/path")]
    [InlineData("http://updates.example.test/releases/latest")]
    public void Constructor_NonHttpsAbsoluteEndpoint_Throws(string endpoint)
    {
        Assert.Throws<ArgumentException>(() => CreateClient(
            new StubHandler(HttpStatusCode.OK, "{}"),
            new Uri(endpoint, UriKind.RelativeOrAbsolute)));
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

    private static GitHubReleaseClient CreateClient(
        HttpMessageHandler handler,
        Uri? endpoint = null,
        ILogger<GitHubReleaseClient>? logger = null) =>
        new(
            new HttpClient(handler),
            endpoint ?? UpdaterConfiguration.DefaultReleaseApiUri,
            logger ?? NullLogger<GitHubReleaseClient>.Instance);

    private sealed class RecordingLogger : ILogger<GitHubReleaseClient>
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Messages.Add(formatter(state, exception));

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }

    private sealed class StubHandler(HttpStatusCode statusCode, string content) : HttpMessageHandler
    {
        public string UserAgent { get; private set; } = string.Empty;
        public string Accept { get; private set; } = string.Empty;
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            UserAgent = request.Headers.UserAgent.ToString();
            Accept = request.Headers.Accept.ToString();
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            });
        }
    }
}
