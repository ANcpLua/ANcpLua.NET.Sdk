using ANcpLua.Sdk.Tests.Helpers;

namespace ANcpLua.Sdk.Tests.Infrastructure;

public interface IPolyfillCase
{
    Task RunPositive(PackageFixture fixture, ITestOutputHelper output);
    Task RunNegative(PackageFixture fixture, ITestOutputHelper output);
}

public sealed class PolyfillCase<TMarker>(string tfm) : IPolyfillCase
    where TMarker : IPolyfillMarker
{
    public async Task RunPositive(PackageFixture fixture, ITestOutputHelper output)
    {
        await using var project =
            new ProjectBuilder(fixture, output, SdkImportStyle.SdkElement, PackageFixture.SdkName);

        var properties = new List<(string, string)>
        {
            (TMarker.InjectPropertyName, MsBuildValues.True),
            (MsBuildProperties.TargetFramework, tfm),
            (MsBuildProperties.OutputType, MsBuildValues.Library)
        };

        if (TMarker.InjectPropertyName is "InjectRequiredMemberOnLegacy" or "InjectCompilerFeatureRequiredOnLegacy")
            properties.Add((MsBuildProperties.LangVersion, MsBuildValues.Latest));

        project.AddCsprojFile(properties.ToArray());

        project.AddFile("Smoke.cs", $$"""
                                      #nullable enable
                                      using System;
                                      namespace Consumer;
                                      internal class Smoke
                                      {
                                          public void Run()
                                          {
                                              {{TMarker.ActivationSnippet}}
                                          }
                                      }
                                      """);

        var result = await project.BuildAndGetOutput();
        Assert.True(result.ExitCode == 0,
            $"Build failed for {TMarker.InjectPropertyName} on {tfm} when expected to succeed. Output: {result.ProcessOutput}");
    }

    public async Task RunNegative(PackageFixture fixture, ITestOutputHelper output)
    {
        if (typeof(TMarker) == typeof(DiagnosticClassesFile)) return;

        await using var project =
            new ProjectBuilder(fixture, output, SdkImportStyle.SdkElement, PackageFixture.SdkName);

        var properties = new List<(string, string)>
        {
            (MsBuildProperties.TargetFramework, tfm),
            (MsBuildProperties.OutputType, MsBuildValues.Library)
        };

        if (TMarker.InjectPropertyName is "InjectRequiredMemberOnLegacy" or "InjectCompilerFeatureRequiredOnLegacy")
            properties.Add((MsBuildProperties.LangVersion, MsBuildValues.Latest));

        project.AddCsprojFile(properties.ToArray());

        project.AddFile("Smoke.cs", $@"
            #nullable enable
            using System;
            namespace Consumer;
            internal class Smoke
            {{
                public void Run()
                {{
                    {TMarker.ActivationSnippet}
                }}
            }}
        ");

        var result = await project.BuildAndGetOutput();
        Assert.True(result.ExitCode != 0,
            $"Build succeeded for {TMarker.InjectPropertyName} on {tfm} when expected to fail without the flag. Output: {result.ProcessOutput}");

        Assert.True(
            result.OutputContains("CS0246") ||
            result.OutputContains("CS0103") ||
            result.OutputContains("CS0234") ||
            result.OutputContains("CS0518") ||
            result.OutputContains("CS1513") ||
            result.OutputContains("CS1022"),
            $"Expected compilation error for missing type {TMarker.ExpectedType}. Output: {result.ProcessOutput}");
    }

    public override string ToString()
    {
        return $"{typeof(TMarker).Name} → {tfm}";
    }
}