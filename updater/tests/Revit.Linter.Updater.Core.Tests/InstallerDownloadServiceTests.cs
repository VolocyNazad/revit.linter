using System.Net;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging.Abstractions;
using Revit.Linter.Updater.Core.Models;
using Revit.Linter.Updater.Core.Services;

namespace Revit.Linter.Updater.Core.Tests;

public sealed class InstallerDownloadServiceTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(), nameof(InstallerDownloadServiceTests), Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task DownloadAsync_ValidContent_WritesFinalInstaller()
    {
        byte[] content = "verified msi content"u8.ToArray();
        ReleaseInstaller installer = CreateInstaller(content);
        var service = CreateService(content);

        string path = await service.DownloadAsync(
            installer, _directory, TestContext.Current.CancellationToken);

        Assert.Equal(installer.Name, Path.GetFileName(path));
        Assert.Equal(content, await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
        Assert.Empty(Directory.GetFiles(_directory, "*.partial"));
    }

    [Fact]
    public async Task DownloadAsync_DigestMismatch_DeletesPartialFile()
    {
        byte[] expected = "expected"u8.ToArray();
        byte[] actual = "tampered"u8.ToArray();
        ReleaseInstaller installer = CreateInstaller(expected, actual.Length);
        var service = CreateService(actual);

        await Assert.ThrowsAsync<InvalidDataException>(() => service.DownloadAsync(
            installer, _directory, TestContext.Current.CancellationToken));

        Assert.Empty(Directory.GetFiles(_directory));
    }

    [Fact]
    public async Task DownloadAsync_ContentLengthMismatch_DoesNotCreateFile()
    {
        byte[] content = "short"u8.ToArray();
        ReleaseInstaller installer = CreateInstaller(content, content.Length + 1);
        var service = CreateService(content);

        await Assert.ThrowsAsync<InvalidDataException>(() => service.DownloadAsync(
            installer, _directory, TestContext.Current.CancellationToken));

        Assert.Empty(Directory.GetFiles(_directory));
    }

    [Fact]
    public async Task DownloadAsync_TrustedHttpsRedirect_DownloadsInstaller()
    {
        byte[] content = "redirected content"u8.ToArray();
        ReleaseInstaller installer = CreateInstaller(content);
        var handler = new RedirectHandler(
            new Uri("https://release-assets.githubusercontent.com/example/installer.msi"),
            content);
        var service = new InstallerDownloadService(
            new HttpClient(handler),
            NullLogger<InstallerDownloadService>.Instance);

        string path = await service.DownloadAsync(
            installer, _directory, TestContext.Current.CancellationToken);

        Assert.Equal(content, await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
        Assert.Equal(2, handler.RequestUris.Count);
    }

    [Fact]
    public async Task DownloadAsync_UntrustedRedirect_RejectsAndLeavesNoFiles()
    {
        byte[] content = "redirected content"u8.ToArray();
        ReleaseInstaller installer = CreateInstaller(content);
        var handler = new RedirectHandler(new Uri("https://example.com/installer.msi"), content);
        var service = new InstallerDownloadService(
            new HttpClient(handler),
            NullLogger<InstallerDownloadService>.Instance);

        await Assert.ThrowsAsync<HttpRequestException>(() => service.DownloadAsync(
            installer, _directory, TestContext.Current.CancellationToken));

        Assert.Empty(Directory.GetFiles(_directory));
        Assert.Single(handler.RequestUris);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    private static InstallerDownloadService CreateService(byte[] content) =>
        new(
            new HttpClient(new ContentHandler(content)),
            NullLogger<InstallerDownloadService>.Instance);

    private static ReleaseInstaller CreateInstaller(byte[] digestContent, int? declaredSize = null)
    {
        var version = new StableVersion(2, 0, 0);
        string digest = "sha256:" + Convert.ToHexString(SHA256.HashData(digestContent));
        Assert.True(ReleaseInstaller.TryCreate(
            version,
            "RevitLinter-2.0.0.msi",
            "https://github.com/VolocyNazad/revit.linter/releases/download/v2.0.0/RevitLinter-2.0.0.msi",
            declaredSize ?? digestContent.Length,
            digest,
            out ReleaseInstaller? installer));
        return Assert.IsType<ReleaseInstaller>(installer);
    }

    private sealed class ContentHandler(byte[] content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(content)
            });
    }

    private sealed class RedirectHandler(Uri redirectUri, byte[] content) : HttpMessageHandler
    {
        public List<Uri> RequestUris { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUris.Add(Assert.IsType<Uri>(request.RequestUri));
            if (RequestUris.Count == 1)
            {
                var redirect = new HttpResponseMessage(HttpStatusCode.Redirect);
                redirect.Headers.Location = redirectUri;
                return Task.FromResult(redirect);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(content)
            });
        }
    }
}
