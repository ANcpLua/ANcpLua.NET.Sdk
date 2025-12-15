using System.Diagnostics;
using ANcpLua.Sdk.Tests.Helpers;
using ANcpLua.Sdk.Tests.Infrastructure;
using Meziantou.Framework;

[assembly: AssemblyFixture(typeof(PackageFixture))]

namespace ANcpLua.Sdk.Tests.Helpers;

public class PackageFixture : IAsyncLifetime
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
                    foreach (var file in files) File.Copy(file, _packageDirectory.FullPath / Path.GetFileName(file));

                    return;
                }

                Assert.Fail("No file found in " + path);
            }

            Assert.Fail("NuGetDirectory environment variable not set");
        }

        var repoRoot = RepositoryRoot.Locate();
        var buildFiles = Directory
            .GetFiles(repoRoot["src"], "*.csproj")
            .Select(FullPath.FromPath)
            .ToList();
        Assert.NotEmpty(buildFiles);
        await Parallel.ForEachAsync(buildFiles, async (nuspecPath, _) =>
        {
            var psi = new ProcessStartInfo("dotnet")
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.ArgumentList.AddRange("pack", nuspecPath, "-p:NuspecProperties=version=" + Version, "--output",
                _packageDirectory.FullPath);
            var result = await psi.RunAsTaskAsync(_);
            if (result.ExitCode != 0)
                Assert.Fail($"NuGet pack failed with exit code {result.ExitCode}. Output: {result.Output}");
        });
    }

    public virtual async ValueTask DisposeAsync()
    {
        await _packageDirectory.DisposeAsync();
    }
}

public sealed record NuGetReference(string Name, string Version);

public enum NetSdkVersion
{
    Net100 = 10
}