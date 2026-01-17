using System.Collections.Concurrent;
using System.Formats.Tar;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Meziantou.Framework;
using Meziantou.Framework.Threading;
using Microsoft.Deployment.DotNet.Releases;

namespace ANcpLua.Sdk.Tests.Helpers;

public static class DotNetSdkHelpers
{
    private static readonly HttpClient _httpClient = new();
    private static readonly ConcurrentDictionary<NetSdkVersion, FullPath> _values = new();
    private static readonly KeyedAsyncLock<NetSdkVersion> _keyedAsyncLock = new();

    public static async Task<FullPath> Get(NetSdkVersion version)
    {
        if (_values.TryGetValue(version, out var result))
            return result;

        using (await _keyedAsyncLock.LockAsync(version))
        {
            if (_values.TryGetValue(version, out result))
                return result;

            var versionString = version switch
            {
                NetSdkVersion.Net100 => "10.0",
                _ => throw new NotSupportedException()
            };

            var products = await ProductCollection.GetAsync();
            var product = products.Single(a => a.ProductName == ".NET" && a.ProductVersion == versionString);
            var releases = await product.GetReleasesAsync();
            var latestRelease = releases.Single(r => r.Version == product.LatestReleaseVersion);
            var latestSdk = latestRelease.Sdks.MaxBy(static sdk => sdk.Version)
                            ?? throw new InvalidOperationException($"No SDK found for version {product.LatestReleaseVersion}");

            var runtimeIdentifier = RuntimeInformation.RuntimeIdentifier;
            var file = latestSdk.Files.Single(f =>
                f.Rid == runtimeIdentifier && f.Name is { } n && Path.GetExtension(n) is ".zip" or ".gz");
            var fileName = file.Name ?? throw new InvalidOperationException("File name was null after filter");
            var finalFolderPath = FullPath.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) / "ANcpLua" /
                                  "dotnet" / latestSdk.Version.ToString();
            var finalDotnetPath = finalFolderPath / (OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");
            if (File.Exists(finalDotnetPath))
            {
                _values[version] = finalDotnetPath;
                return finalDotnetPath;
            }

            var tempFolder = FullPath.GetTempPath() / "dotnet" / Guid.NewGuid().ToString("N");

            var bytes = await _httpClient.GetByteArrayAsync(file.Address);
            if (Path.GetExtension(fileName) is ".zip")
            {
                using var ms = new MemoryStream(bytes);
                var zip = new ZipArchive(ms);
                await zip.ExtractToDirectoryAsync(tempFolder, true);
            }
            else
            {
                using var ms = new MemoryStream(bytes);
                await using var gz = new GZipStream(ms, CompressionMode.Decompress);
                await using var tar = new TarReader(gz);
                while (await tar.GetNextEntryAsync() is { } entry)
                {
                    var destinationPath = tempFolder / entry.Name;
                    switch (entry.EntryType)
                    {
                        case TarEntryType.Directory:
                            Directory.CreateDirectory(destinationPath);
                            break;
                        case TarEntryType.RegularFile:
                        {
                            var directoryPath = Path.GetDirectoryName(destinationPath);
                            if (directoryPath != null)
                                Directory.CreateDirectory(directoryPath);

                            var entryStream = entry.DataStream;
                            await using var outputStream = File.Create(destinationPath);
                            if (entryStream is not null) await entryStream.CopyToAsync(outputStream);

                            break;
                        }
                    }
                }
            }

            if (!OperatingSystem.IsWindows())
            {
                var tempDotnetPath = tempFolder / "dotnet";

                Console.WriteLine("Updating permissions of " + tempDotnetPath);
                File.SetUnixFileMode(tempDotnetPath,
                    UnixFileMode.UserRead | UnixFileMode.UserExecute | UnixFileMode.GroupRead |
                    UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);

                foreach (var cscPath in Directory.GetFiles(tempFolder, "csc", SearchOption.AllDirectories))
                {
                    Console.WriteLine("Updating permissions of " + cscPath);
                    File.SetUnixFileMode(cscPath,
                        UnixFileMode.UserRead | UnixFileMode.UserExecute | UnixFileMode.GroupRead |
                        UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
                }
            }

            finalFolderPath.CreateParentDirectory();

            Directory.Move(tempFolder, finalFolderPath);

            Assert.True(File.Exists(finalDotnetPath));
            _values[version] = finalDotnetPath;
            return finalDotnetPath;
        }
    }
}
