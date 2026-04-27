using System.Diagnostics;
using System.Text.RegularExpressions;
using Meziantou.Framework;

[assembly: AssemblyFixture(typeof(PackageFixture))]

namespace ANcpLua.Sdk.Tests.Helpers;

public partial class PackageFixture : IAsyncLifetime
{
    public const string SdkName = "ANcpLua.NET.Sdk";
    public const string SdkWebName = "ANcpLua.NET.Sdk.Web";
    public const string SdkTestName = "ANcpLua.NET.Sdk.Test";

    private readonly TemporaryDirectory _packageDirectory = TemporaryDirectory.Create();

    public FullPath PackageDirectory => _packageDirectory.FullPath;

    public string Version { get; } = Environment.GetEnvironmentVariable("PACKAGE_VERSION") ?? "999.9.9";

    public async ValueTask InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("CI") is not null)
        {
            if (Environment.GetEnvironmentVariable("NUGET_DIRECTORY") is { } path)
            {
                var files = Directory.GetFiles(path, "*.nupkg", SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    foreach (var file in files)
                        File.Copy(file, _packageDirectory.FullPath / Path.GetFileName(file), true);

                    return;
                }

                Assert.Fail("No file found in " + path);
            }

            Assert.Fail("NuGetDirectory environment variable not set");
        }

        var repoRoot = RepositoryRoot.Locate();

        var versionPropsPath = repoRoot["src"] / "Build" / "Common" / "Version.props";
        var existingContent = await File.ReadAllTextAsync(versionPropsPath);
        var updatedContent = MyRegex().Replace(existingContent, $"<ANcpSdkPackageVersion>{Version}</ANcpSdkPackageVersion>");
        await File.WriteAllTextAsync(versionPropsPath, updatedContent);

        var buildFiles = Directory
            .GetFiles(repoRoot["src"], "*.csproj")
            .Select(FullPath.FromPath)
            .ToList();

        Assert.NotEmpty(buildFiles);
        await Parallel.ForEachAsync(buildFiles, async (nuspecPath, t) =>
        {
            var psi = new ProcessStartInfo("dotnet")
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.ArgumentList.AddRange("pack", nuspecPath, "-c", "Release",
                "-p:Version=" + Version, "-p:NuspecProperties=version=" + Version,
                "--output",
                _packageDirectory.FullPath);
            var result = await psi.RunAsTaskAsync(t);
            if (result.ExitCode is not 0)
                Assert.Fail($"NuGet pack failed with exit code {result.ExitCode}. Output: {result.Output}");
        });
    }

    public virtual async ValueTask DisposeAsync()
    {
        await _packageDirectory.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    [GeneratedRegex("<ANcpSdkPackageVersion>[^<]+</ANcpSdkPackageVersion>")]
    private static partial Regex MyRegex();
}
