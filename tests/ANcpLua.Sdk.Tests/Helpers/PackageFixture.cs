using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ANcpLua.Sdk.Tests.Helpers;
using Meziantou.Framework;
using Xunit;

[assembly: AssemblyFixture(typeof(PackageFixture))]

namespace ANcpLua.Sdk.Tests.Helpers;

public sealed class PackageFixture : IAsyncLifetime
{
    public const string SdkName = "ANcpLua.NET.Sdk";

    private readonly TemporaryDirectory _packageDirectory = TemporaryDirectory.Create();

    public FullPath PackageDirectory => _packageDirectory.FullPath;

    public string Version { get; } = Environment.GetEnvironmentVariable("PACKAGE_VERSION") ?? "999.9.9";

    public async ValueTask InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("CI") != null)
        {
            if (Environment.GetEnvironmentVariable("NUGET_DIRECTORY") is { } path)
            {
                var files = Directory.GetFiles(path, "*.nupkg", SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    foreach (var file in files)
                    {
                        File.Copy(file, _packageDirectory.FullPath / Path.GetFileName(file));
                    }

                    return;
                }

                Assert.Fail("No file found in " + path);
            }

            Assert.Fail("NuGetDirectory environment variable not set");
        }

        // Build NuGet packages
        var buildFiles = Directory.GetFiles(PathHelpers.GetRootDirectory() / "src", "*.csproj").Select(FullPath.FromPath);
        Assert.NotEmpty(buildFiles);
        await Parallel.ForEachAsync(buildFiles, async (nuspecPath, _) =>
        {
            var psi = new ProcessStartInfo("dotnet");
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.ArgumentList.AddRange(["pack", nuspecPath, "-p:NuspecProperties=version=" + Version, "--output", _packageDirectory.FullPath]);
            var result = await psi.RunAsTaskAsync();
            if (result.ExitCode != 0)
            {
                Assert.Fail($"NuGet pack failed with exit code {result.ExitCode}. Output: {result.Output}");
            }
        });
    }

    public async ValueTask DisposeAsync()
    {
        await _packageDirectory.DisposeAsync();
    }
}
