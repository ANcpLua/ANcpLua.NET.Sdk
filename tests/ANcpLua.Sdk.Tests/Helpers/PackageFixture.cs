using System.Diagnostics;
using ANcpLua.Sdk.Tests.Helpers;
using ANcpLua.Sdk.Tests.Infrastructure;
using Meziantou.Framework;

[assembly: AssemblyFixture(typeof(PackageFixture))]

namespace ANcpLua.Sdk.Tests.Helpers;

public class PackageFixture : IAsyncLifetime
{
    public const string SdkName = "ANcpLua.NET.Sdk";
    public const string SdkWebName = "ANcpLua.NET.Sdk.Web";
    public const string SdkTestName = "ANcpLua.NET.Sdk.Test";


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

        // Also include ANcpSdk.AspNetCore.ServiceDefaults packages from eng/ directory
        // These need to be built first because they have IncludeBuildOutput=false and manually include the Release DLL
        var engProjects = new[]
        {
            repoRoot["eng"] / "ANcpSdk.AspNetCore.ServiceDefaults" / "ANcpSdk.AspNetCore.ServiceDefaults.csproj",
            repoRoot["eng"] / "ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister" / "ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister.csproj"
        };

        // Build eng projects first (they need explicit build due to IncludeBuildOutput=false)
        foreach (var engProject in engProjects)
        {
            var buildPsi = new ProcessStartInfo("dotnet")
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            buildPsi.ArgumentList.AddRange("build", engProject, "-c", "Release");
            var buildResult = await buildPsi.RunAsTaskAsync(CancellationToken.None);
            if (buildResult.ExitCode != 0)
                Assert.Fail($"Build failed with exit code {buildResult.ExitCode}. Output: {buildResult.Output}");
        }

        buildFiles.AddRange(engProjects);

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
            psi.ArgumentList.AddRange("pack", nuspecPath, "-c", "Release", "-p:NuspecProperties=version=" + Version, "--output",
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