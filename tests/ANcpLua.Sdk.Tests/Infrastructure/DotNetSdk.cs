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
                NetSdkVersion.Net100 => "10.0",
                _ => throw new NotSupportedException($"SDK version {version} is not supported")
            };

            var products = await ProductCollection.GetAsync().ConfigureAwait(false);
            var product = products.Single(p => p.ProductName == ".NET" && p.ProductVersion == versionString);
            var releases = await product.GetReleasesAsync().ConfigureAwait(false);
            var latestRelease = releases.Single(r => r.Version == product.LatestReleaseVersion);
            var latestSdk = latestRelease.Sdks.MaxBy(static sdk => sdk.Version)
                            ?? throw new InvalidOperationException($"No SDK found for .NET {versionString}");

            var runtimeIdentifier = RuntimeInformation.RuntimeIdentifier;
            var file = latestSdk.Files.Single(f =>
                f.Rid == runtimeIdentifier && Path.GetExtension(f.Name) is ".zip" or ".gz");
            var finalFolderPath = FullPath.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) /
                                  "ANcpLua" / "dotnet" / latestSdk.Version.ToString();
            var finalDotnetPath = finalFolderPath / (OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");
            if (File.Exists(finalDotnetPath))
                return s_resolved[version] = finalDotnetPath;

            var tempFolder = FullPath.GetTempPath() / "dotnet" / Guid.NewGuid().ToString("N");
            var bytes = await s_httpClient.GetByteArrayAsync(file.Address).ConfigureAwait(false);
            await ExtractAsync(file.Name, bytes, tempFolder).ConfigureAwait(false);
            MakeUnixExecutables(tempFolder);

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

        await using var gz = new GZipStream(ms, CompressionMode.Decompress);
        await using var tar = new TarReader(gz);
        while (await tar.GetNextEntryAsync().ConfigureAwait(false) is { } entry)
        {
            var destinationPath = destination / entry.Name;
            switch (entry.EntryType)
            {
                case TarEntryType.Directory:
                    Directory.CreateDirectory(destinationPath);
                    break;
                case TarEntryType.RegularFile:
                    if (Path.GetDirectoryName(destinationPath) is { } parentDir)
                        Directory.CreateDirectory(parentDir);
                    await using (var outputStream = File.Create(destinationPath))
                        if (entry.DataStream is { } entryStream)
                            await entryStream.CopyToAsync(outputStream).ConfigureAwait(false);
                    break;
            }
        }
    }

    private static void MakeUnixExecutables(FullPath folder)
    {
        if (OperatingSystem.IsWindows())
            return;

        const UnixFileMode Executable =
            UnixFileMode.UserRead | UnixFileMode.UserExecute | UnixFileMode.GroupRead |
            UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute;

        File.SetUnixFileMode(folder / "dotnet", Executable);
        foreach (var cscPath in Directory.GetFiles(folder, "csc", SearchOption.AllDirectories))
            File.SetUnixFileMode(cscPath, Executable);
    }
}
