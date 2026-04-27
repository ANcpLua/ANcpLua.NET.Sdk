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

    private static readonly (string Name, string Version)[] _externalPackages =
    [
        ("xunit", "2.9.3"),
        ("xunit.v3", "3.2.2"),
        ("xunit.v3.mtp-v2", "3.2.2"),
        ("xunit.runner.visualstudio", "3.1.5"),
        ("Newtonsoft.Json", "13.0.4"),
        ("System.Net.Http", "4.3.4"),
        ("Microsoft.Sbom.Targets", "4.1.5"),
        ("OpenTelemetry", "1.15.0"),
        ("OpenTelemetry.Extensions.Hosting", "1.15.0")
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

                    await PreWarmNuGetCacheAsync();
                    await PreWarmAncpLuaSdksAsync();
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

        await PreWarmNuGetCacheAsync();
        await PreWarmAncpLuaSdksAsync();
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
                               </configuration>
                               """;
            await File.WriteAllTextAsync(warmupDir / "NuGet.config", nugetConfig);

            var packageRefs = string.Join("\n        ",
                _externalPackages.Select(static p =>
                    $"""<PackageReference Include="{p.Name}" Version="{p.Version}" />"""));

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
            if (Directory.Exists(warmupDir))
                Directory.Delete(warmupDir, true);
        }
    }

    /// <summary>
    ///     Sequentially extracts each ANcpLua SDK variant into the global packages folder.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Tests run in parallel and each spawns <c>dotnet build</c> against a project that uses
    ///         <c>&lt;Project Sdk="ANcpLua.NET.Sdk/{version}"&gt;</c>. On a cold cache this triggers
    ///         <c>NuGet.SdkResolver</c> to extract the SDK package on first use. Two parallel processes
    ///         hitting the same uncached SDK can race during extraction on Windows: MSBuild's evaluation
    ///         path-walks the SDK directory while NuGet is still writing files, and the resulting binlog
    ///         is missing late property events even though the build itself succeeds — observed
    ///         specifically on the first <c>NameContainsAnalyzer_AutoDefaultsRoslynVersion</c> run when it
    ///         coincided with another parallel test starting at the same microsecond.
    ///     </para>
    ///     <para>
    ///         This method runs one sequential <c>dotnet restore</c> per SDK variant, forcing each SDK to
    ///         be fully extracted to the shared NUGET_PACKAGES location before any parallel test fires.
    ///     </para>
    /// </remarks>
    private async Task PreWarmAncpLuaSdksAsync()
    {
        foreach (var sdkName in new[] { SdkName, SdkWebName, SdkTestName })
            await WarmSdkAsync(sdkName);
    }

    private async Task WarmSdkAsync(string sdkName)
    {
        var warmupDir = _packageDirectory.FullPath / $"warmup-{sdkName}";
        Directory.CreateDirectory(warmupDir);

        try
        {
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
                               </configuration>
                               """;
            await File.WriteAllTextAsync(warmupDir / "NuGet.config", nugetConfig);

            var csproj = $"""
                          <Project Sdk="{sdkName}/{Version}">
                              <PropertyGroup>
                                  <TargetFramework>net10.0</TargetFramework>
                                  <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
                                  <ANcpLuaSdkSkipCPMEnforcement>true</ANcpLuaSdkSkipCPMEnforcement>
                                  <DisableVersionAnalyzer>true</DisableVersionAnalyzer>
                                  <SkipXunitInjection>true</SkipXunitInjection>
                              </PropertyGroup>
                          </Project>
                          """;
            await File.WriteAllTextAsync(warmupDir / "Warmup.csproj", csproj);

            var psi = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = warmupDir,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.ArgumentList.AddRange("restore");

            var result = await psi.RunAsTaskAsync(CancellationToken.None);
            if (result.ExitCode is not 0)
                Assert.Fail($"SDK pre-warm for {sdkName} failed with exit code {result.ExitCode}. Output: {result.Output}");
        }
        finally
        {
            if (Directory.Exists(warmupDir))
                Directory.Delete(warmupDir, true);
        }
    }

    [GeneratedRegex("<ANcpSdkPackageVersion>[^<]+</ANcpSdkPackageVersion>")]
    private static partial Regex MyRegex();
}
