using Meziantou.Framework;
using Xunit;

[assembly: AssemblyFixture(typeof(ANcpLua.Sdk.Tests.Infrastructure.PackageFixture))]

namespace ANcpLua.Sdk.Tests.Infrastructure;

public sealed class PackageFixture : IAsyncLifetime
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
            await CopyPrebuiltPackagesAsync();
            return;
        }

        var repoRoot = RepositoryRoot.Locate();
        var projects = Directory.GetFiles(repoRoot["src"], "*.csproj").Select(FullPath.FromPath).ToList();
        Assert.NotEmpty(projects);

        await Parallel.ForEachAsync(projects, async (project, cancellationToken) =>
        {
            var result = await ProcessWrapper.Create("dotnet")
                .WithArguments(
                    "pack", project, "-c", "Release",
                    "-p:Version=" + Version, "-p:NuspecProperties=version=" + Version,
                    "--output", _packageDirectory.FullPath)
                .WithValidation(ProcessValidationMode.None)
                .ExecuteBufferedAsync(cancellationToken);

            if (!result.ExitCode.IsSuccess)
                Assert.Fail($"NuGet pack failed with exit code {result.ExitCode}. Output: {result.Output}");
        });
    }

    public ValueTask DisposeAsync() => _packageDirectory.DisposeAsync();

    private ValueTask CopyPrebuiltPackagesAsync()
    {
        if (Environment.GetEnvironmentVariable("NUGET_DIRECTORY") is not { } path)
        {
            Assert.Fail("NUGET_DIRECTORY environment variable not set");
            return ValueTask.CompletedTask;
        }

        var files = Directory.GetFiles(path, "*.nupkg", SearchOption.AllDirectories);
        if (files.Length is 0)
            Assert.Fail("No file found in " + path);

        foreach (var file in files)
            File.Copy(file, _packageDirectory.FullPath / Path.GetFileName(file), true);

        return ValueTask.CompletedTask;
    }
}
