using ANcpLua.Sdk.Tests.Infrastructure;
using Xunit.Sdk;

namespace ANcpLua.Sdk.Tests.Helpers;

public static class MsbuildShould
{
    public static async Task ShouldGenerate<TFile>(this string expectedSource, PackageFixture packages,
        params string[] msbuildPropsXml)
        where TFile : IInjectedFile
    {
        await ShouldGenerateSnapshot(
            expectedSource,
            packages,
            TFile.RepoRelativePath,
            TFile.InjectPropertyName,
            msbuildPropsXml);
    }

    public static async Task ShouldGenerate<TFile>(this string expectedSource, PackageFixture fixture, string propsXml)
        where TFile : IInjectedFile
    {
        await ShouldGenerateFunctional(
            expectedSource, fixture, TFile.RepoRelativePath, TFile.InjectPropertyName, propsXml);
    }

    private static async Task ShouldGenerateSnapshot(
        string expectedSource, PackageFixture fixture, string repoRelativePath, string injectPropertyName,
        string[] msbuildPropsXml)
    {
        var props = MsBuildPropertyBuilder.FromXmlSnippets(msbuildPropsXml);
        props[injectPropertyName] = MsBuildValues.True;

        var propsDict = props.ToDictionary(k => k.Key, v => v.Value);
        var result = await MsbuildScenario.BuildAsync(fixture, propsDict);

        if (result.ExitCode is not 0)
            throw new XunitException(
                $"Build failed (ExitCode={result.ExitCode}). Output:\n{string.Join("\n", result.ProcessOutput.Select(l => l.Text))}");

        var compileItems = result.GetMsBuildItems("Compile");
        var expectedPathSuffix = Path.GetFileName(repoRelativePath);

        if (!compileItems.Any(p => p.EndsWith(expectedPathSuffix, StringComparison.OrdinalIgnoreCase)))
            throw new XunitException(
                $"Expected Compile to include '{expectedPathSuffix}' (from '{repoRelativePath}'), but it didn't. Found items:\n{string.Join("\n", compileItems)}");

        if (!string.IsNullOrWhiteSpace(expectedSource))
        {
            var repoRoot = RepositoryRoot.Locate();
            var fullPath = repoRoot[repoRelativePath];

            var actual = (await File.ReadAllTextAsync(fullPath)).ReplaceLineEndings("\n");
            var expected = expectedSource.ReplaceLineEndings("\n");

            if (!string.Equals(actual, expected, StringComparison.Ordinal)) Assert.Equal(expected, actual);
        }
    }

    private static async Task ShouldGenerateFunctional(
        string userCode,
        PackageFixture fixture,
        string repoRelativePath,
        string injectPropertyName,
        string propsXml)
    {
        var props = MsBuildPropertyBuilder.FromXmlSnippets(propsXml);
        props[injectPropertyName] = MsBuildValues.True;
        props.TryAdd(MsBuildProperties.OutputType, MsBuildValues.Library);

        var output = TestContext.Current.TestOutputHelper!;

        await using var project =
            new ProjectBuilder(fixture, output, SdkImportStyle.SdkElement, PackageFixture.SdkName);

        var propsArray = props
            .Where(kv => kv.Value is not null)
            .Select(kv => (kv.Key, kv.Value!))
            .ToArray();

        project.AddCsprojFile(propsArray);

        if (!string.IsNullOrWhiteSpace(userCode)) project.AddFile("UserCode.cs", userCode);

        var result = await project.BuildAndGetOutput();

        if (result.ExitCode is not 0)
        {
            var errors = result.SarifFile?.AllResults().Where(r => r.Level == "error").Select(r => r.ToString()) ?? [];
            throw new XunitException($"Build failed (ExitCode={result.ExitCode}). Errors: {string.Join("; ", errors)}");
        }

        var compileItems = result.GetMsBuildItems("Compile");
        var expectedEnd = Path.GetFileName(repoRelativePath);

        if (!compileItems.Any(p => p.EndsWith(expectedEnd, StringComparison.OrdinalIgnoreCase)))
            throw new XunitException(
                $"Expected Compile items to include '{expectedEnd}', but it was not found.\n" +
                $"Available items:\n{string.Join("\n", compileItems.Take(10))}...");
    }

    internal static class MsbuildScenario
    {
        public static async Task<BuildResult> BuildAsync(PackageFixture fixture, Dictionary<string, string?> props)
        {
            var output = TestContext.Current.TestOutputHelper!;
            await using var project =
                new ProjectBuilder(fixture, output, SdkImportStyle.SdkElement, PackageFixture.SdkName);

            var propsArray = props
                .Where(kv => kv.Value is not null)
                .Select(kv => (kv.Key, kv.Value!))
                .ToArray();

            project.AddCsprojFile(propsArray);

            project.AddFile("Program.cs", """
                                          using System;
                                          Console.WriteLine("Hello");
                                          """);

            return await project.BuildAndGetOutput();
        }
    }
}