using static ANcpLua.Sdk.Tests.Helpers.PackageFixture;

namespace ANcpLua.Sdk.Tests;

/// <summary>
///     Verifies that <c>SourceGenerators.props</c> auto-defaults
///     <c>SourceGeneratorRoslynVersion</c> to 5.0.0 for detected generator /
///     analyzer projects, pins <c>Microsoft.CodeAnalysis.CSharp</c> via
///     <c>VersionOverride</c>, and honors explicit overrides.
/// </summary>
public sealed class SourceGeneratorDefaultsNet100Tests(PackageFixture fixture)
    : SourceGeneratorDefaultsTests(fixture, NetSdkVersion.Net100);

public abstract class SourceGeneratorDefaultsTests(
    PackageFixture fixture,
    NetSdkVersion dotnetSdkVersion)
{
    private SdkProjectBuilder CreateProject(string sdkName = SdkName) =>
        SdkProjectBuilder.Create(fixture, SdkImportStyle.ProjectElement, sdkName)
            .WithDotnetSdkVersion(dotnetSdkVersion);

    [Fact]
    public async Task NameContainsGeneratorUpperCase_AutoDefaultsRoslynTo500()
    {
        await using var project = CreateProject();

        var result = await project
            .WithFilename("Sample.Generator.csproj")
            .WithOutputType(Val.Library)
            .WithTargetFramework(Tfm.NetStandard20)
            .AddSource("Gen.cs", "public class Gen { }")
            .BuildAsync();

        result.ShouldHavePropertyValue("_IsSourceGeneratorProject", "true");
        result.ShouldHavePropertyValue("SourceGeneratorRoslynVersion", "5.0.0");
    }

    [Fact]
    public async Task NameContainsGeneratorLowerCase_AutoDefaultsRoslynTo500()
    {
        // qyl-style naming: "qyl.instrumentation.generators" — all lowercase.
        await using var project = CreateProject();

        var result = await project
            .WithFilename("sample.instrumentation.generators.csproj")
            .WithOutputType(Val.Library)
            .WithTargetFramework(Tfm.NetStandard20)
            .AddSource("Gen.cs", "public class Gen { }")
            .BuildAsync();

        result.ShouldHavePropertyValue("_IsSourceGeneratorProject", "true");
        result.ShouldHavePropertyValue("SourceGeneratorRoslynVersion", "5.0.0");
    }

    [Fact]
    public async Task NameContainsAnalyzer_AutoDefaultsRoslynTo500()
    {
        await using var project = CreateProject();

        var result = await project
            .WithFilename("Sample.Analyzer.csproj")
            .WithOutputType(Val.Library)
            .WithTargetFramework(Tfm.NetStandard20)
            .AddSource("Analyzer.cs", "public class Analyzer { }")
            .BuildAsync();

        result.ShouldHavePropertyValue("_IsSourceGeneratorProject", "true");
        result.ShouldHavePropertyValue("SourceGeneratorRoslynVersion", "5.0.0");
    }

    [Fact]
    public async Task RegularLibrary_DoesNotAutoPinRoslyn()
    {
        await using var project = CreateProject();

        var result = await project
            .WithFilename("Sample.csproj")
            .WithOutputType(Val.Library)
            .AddSource("Sample.cs", "public class Sample { }")
            .BuildAsync();

        result.ShouldHavePropertyValue("_IsSourceGeneratorProject", null);
        result.ShouldHavePropertyValue("SourceGeneratorRoslynVersion", null);
    }

    [Fact]
    public async Task ExplicitVersion_WinsOverAutoDefault()
    {
        await using var project = CreateProject();

        var result = await project
            .WithFilename("Sample.Generator.csproj")
            .WithOutputType(Val.Library)
            .WithTargetFramework(Tfm.NetStandard20)
            .WithProperty("SourceGeneratorRoslynVersion", "4.11.0")
            .AddSource("Gen.cs", "public class Gen { }")
            .BuildAsync();

        result.ShouldHavePropertyValue("_IsSourceGeneratorProject", "true");
        result.ShouldHavePropertyValue("SourceGeneratorRoslynVersion", "4.11.0");
    }
}
