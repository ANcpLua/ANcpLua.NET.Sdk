using System.Collections.Concurrent;
using System.Formats.Tar;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Meziantou.Framework;
using Microsoft.Deployment.DotNet.Releases;

namespace ANcpLua.Sdk.Tests.Infrastructure;

public static class DotNetSdk
{
    private static readonly HttpClient s_httpClient = new();
    private static readonly ConcurrentDictionary<NetSdkVersion, FullPath> s_resolved = new();
    private static readonly SemaphoreSlim s_downloadGate = new(1, 1);

    public static async Task<FullPath> GetAsync(NetSdkVersion version)
    {
        if (s_resolved.TryGetValue(version, out var cached))
            return cached;

        await s_downloadGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (s_resolved.TryGetValue(version, out cached))
                return cached;

            var versionString = version switch
            {
                NetSdkVersion.Net110 => "11.0",
                _ => throw new NotSupportedException($"SDK version {version} is not supported")
            };

            var products = await ProductCollection.GetAsync().ConfigureAwait(false);
            var product = products.Single(p => p.ProductName == ".NET" && p.ProductVersion == versionString);
            var releases = await product.GetReleasesAsync().ConfigureAwait(false);
            var latestRelease = releases.Single(r => r.Version == product.LatestReleaseVersion);
            var latestSdk = latestRelease.Sdks.MaxBy(static sdk => sdk.Version)
                            ?? throw new InvalidOperationException($"No SDK found for .NET {versionString}");

            var runtimeIdentifier = RuntimeInformation.RuntimeIdentifier;
            // Windows gets both a .zip and a .tar.gz from .NET 11 on; take the native archive per OS.
            var archiveExtension = OperatingSystem.IsWindows() ? ".zip" : ".gz";
            var file = latestSdk.Files.Single(f =>
                f.Rid == runtimeIdentifier && Path.GetExtension(f.Name) == archiveExtension);
            var finalFolderPath = FullPath.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) /
                                  "ANcpLua" / "dotnet" / latestSdk.Version.ToString();
            var finalDotnetPath = finalFolderPath / (OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");
            if (File.Exists(finalDotnetPath))
                return s_resolved[version] = finalDotnetPath;

            var tempFolder = FullPath.GetTempPath() / "dotnet" / Guid.NewGuid().ToString("N");
            var bytes = await s_httpClient.GetByteArrayAsync(file.Address).ConfigureAwait(false);
            await ExtractAsync(file.Name, bytes, tempFolder).ConfigureAwait(false);

            finalFolderPath.CreateParentDirectory();
            Directory.Move(tempFolder, finalFolderPath);

            if (!File.Exists(finalDotnetPath))
                throw new InvalidOperationException($"SDK download failed. Expected dotnet at: {finalDotnetPath}");

            return s_resolved[version] = finalDotnetPath;
        }
        finally
        {
            s_downloadGate.Release();
        }
    }

    private static async Task ExtractAsync(string fileName, byte[] bytes, FullPath destination)
    {
        using var ms = new MemoryStream(bytes);
        if (Path.GetExtension(fileName) is ".zip")
        {
            using var zip = new ZipArchive(ms);
            await zip.ExtractToDirectoryAsync(destination, true).ConfigureAwait(false);
            return;
        }

        // TarFile keeps symbolic links, hard links and Unix file modes. The .NET 11 SDK archives
        // deduplicate Roslyn's assemblies with symlinks (Roslyn/bincore -> sdk root), so an
        // extractor that writes regular files only leaves csc unable to load Microsoft.CodeAnalysis.
        await using var gz = new GZipStream(ms, CompressionMode.Decompress);
        Directory.CreateDirectory(destination);
        await TarFile.ExtractToDirectoryAsync(gz, destination, overwriteFiles: true).ConfigureAwait(false);
    }
}
