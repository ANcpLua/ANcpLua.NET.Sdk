using System.Diagnostics;
using System.Text.RegularExpressions;
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

    // External package versions used by tests - keep in sync with test code
    private static readonly (string Name, string Version)[] ExternalPackages =
    [
        ("xunit", "2.9.3"),
        ("xunit.v3", "3.2.1"),
        ("xunit.v3.mtp-v2", "3.2.1"),
        ("xunit.runner.visualstudio", "3.1.5"),
        ("Newtonsoft.Json", "13.0.4"),
        ("System.Net.Http", "4.3.4"),
        ("Microsoft.Sbom.Targets", "4.1.5"),
        ("OpenTelemetry", "1.14.0"),
        ("OpenTelemetry.Extensions.Hosting", "1.14.0")
    ];

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

        // Update only the version in existing Version.props (preserve all other content)
        var versionPropsPath = repoRoot["src"] / "common" / "Version.props";
        var existingContent = await File.ReadAllTextAsync(versionPropsPath);
        var updatedContent = Regex.Replace(
            existingContent,
            @"<ANcpSdkPackageVersion>[^<]+</ANcpSdkPackageVersion>",
            $"<ANcpSdkPackageVersion>{Version}</ANcpSdkPackageVersion>");
        await File.WriteAllTextAsync(versionPropsPath, updatedContent);

        var buildFiles = Directory
            .GetFiles(repoRoot["src"], "*.csproj")
            .Select(FullPath.FromPath)
            .ToList();

        // Also include ANcpSdk.AspNetCore.ServiceDefaults packages from eng/ directory
        // These need to be built first because they have IncludeBuildOutput=false and manually include the Release DLL
        var engProjects = new[]
        {
            repoRoot["eng"] / "ANcpSdk.AspNetCore.ServiceDefaults" / "ANcpSdk.AspNetCore.ServiceDefaults.csproj", repoRoot["eng"] / "ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister" /
                                                                                                                  "ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister.csproj"
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
            if (buildResult.ExitCode is not 0)
                Assert.Fail($"Build failed with exit code {buildResult.ExitCode}. Output: {buildResult.Output}");
        }

        buildFiles.AddRange(engProjects);

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

        // Pre-warm NuGet cache to avoid race conditions in parallel tests
        await PreWarmNuGetCacheAsync();
    }

    public virtual async ValueTask DisposeAsync()
    {
        await _packageDirectory.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Pre-warms the NuGet cache by restoring all external packages used by tests.
    ///     This prevents race conditions when parallel tests try to download the same packages.
    /// </summary>
    private async Task PreWarmNuGetCacheAsync()
    {
        var warmupDir = _packageDirectory.FullPath / "warmup";
        Directory.CreateDirectory(warmupDir);

        try
        {
            // Create NuGet.config pointing to our shared package folder
            var nugetConfig = $"""
                               <configuration>
                                   <config>
                                       <add key="globalPackagesFolder" value="{_packageDirectory.FullPath}/packages" />
                                   </config>
                                   <packageSources>
                                       <clear />
                                       <add key="TestSource" value="{_packageDirectory.FullPath}" />
                                       <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                                   </packageSources>
                                   <packageSourceMapping>
                                       <packageSource key="TestSource">
                                           <package pattern="ANcpLua.*" />
                                           <package pattern="ANcpSdk.*" />
                                       </packageSource>
                                       <packageSource key="nuget.org">
                                           <package pattern="*" />
                                       </packageSource>
                                   </packageSourceMapping>
                               </configuration>
                               """;
            await File.WriteAllTextAsync(warmupDir / "NuGet.config", nugetConfig);

            // Build package references XML
            var packageRefs = string.Join("\n        ",
                ExternalPackages.Select(static p =>
                    $"""<PackageReference Include="{p.Name}" Version="{p.Version}" />"""));

            // Create warmup project that references all external packages
            var csproj = $"""
                          <Project Sdk="Microsoft.NET.Sdk">
                              <Sdk Name="Microsoft.Sbom.Targets" Version="4.1.5" />
                              <PropertyGroup>
                                  <TargetFramework>net10.0</TargetFramework>
                                  <GenerateSBOM>false</GenerateSBOM>
                              </PropertyGroup>
                              <ItemGroup>
                                  {packageRefs}
                              </ItemGroup>
                          </Project>
                          """;
            await File.WriteAllTextAsync(warmupDir / "warmup.csproj", csproj);

            // Restore packages (sequential, one-time)
            var psi = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = warmupDir,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.ArgumentList.AddRange("restore", "--no-cache");

            var result = await psi.RunAsTaskAsync(CancellationToken.None);
            if (result.ExitCode is not 0)
                Assert.Fail($"NuGet cache pre-warm failed with exit code {result.ExitCode}. Output: {result.Output}");
        }
        finally
        {
            // Cleanup warmup directory (packages stay in globalPackagesFolder)
            if (Directory.Exists(warmupDir))
                Directory.Delete(warmupDir, true);
        }
    }
}

public sealed record NuGetReference(string Name, string Version);

public enum NetSdkVersion
{
    Net100 = 10
}
