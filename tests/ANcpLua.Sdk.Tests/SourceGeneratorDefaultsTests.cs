using static ANcpLua.Sdk.Tests.Helpers.PackageFixture;

namespace ANcpLua.Sdk.Tests;

/// <summary>
///     Verifies that <c>SourceGenerators.targets</c> auto-defaults
///     <c>SourceGeneratorRoslynVersion</c> to <c>RoslynVersion</c> for detected
///     generator / analyzer projects, pins <c>Microsoft.CodeAnalysis.CSharp</c>
///     via <c>VersionOverride</c>, and honors explicit overrides.
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
    public async Task NameContainsGeneratorUpperCase_AutoDefaultsRoslynVersion()
    {
        await using var project = CreateProject();

        var result = await project
            .WithFilename("Sample.Generator.csproj")
            .WithOutputType(Val.Library)
            .WithTargetFramework(Tfm.NetStandard20)
            .AddSource("Gen.cs", "public class Gen { }")
            .BuildAsync();

        AssertUsesDefaultRoslynVersion(result);
    }

    [Fact]
    public async Task NameContainsGeneratorLowerCase_AutoDefaultsRoslynVersion()
    {
        // qyl-style naming: "qyl.instrumentation.generators" — all lowercase.
        await using var project = CreateProject();

        var result = await project
            .WithFilename("sample.instrumentation.generators.csproj")
            .WithOutputType(Val.Library)
            .WithTargetFramework(Tfm.NetStandard20)
            .AddSource("Gen.cs", "public class Gen { }")
            .BuildAsync();

        AssertUsesDefaultRoslynVersion(result);
    }

    [Fact]
    public async Task NameContainsAnalyzer_AutoDefaultsRoslynVersion()
    {
        await using var project = CreateProject();

        var result = await project
            .WithFilename("Sample.Analyzer.csproj")
            .WithOutputType(Val.Library)
            .WithTargetFramework(Tfm.NetStandard20)
            .AddSource("Analyzer.cs", "public class Analyzer { }")
            .BuildAsync();

        AssertUsesDefaultRoslynVersion(result);
    }

    [Fact]
    public async Task IsSourceGeneratorProperty_AutoDefaultsRoslynVersion()
    {
        await using var project = CreateProject();

        var result = await project
            .WithFilename("Sample.csproj")
            .WithOutputType(Val.Library)
            .WithTargetFramework(Tfm.NetStandard20)
            .WithProperty("IsSourceGenerator", "true")
            .AddSource("Gen.cs", "public class Gen { }")
            .BuildAsync();

        AssertUsesDefaultRoslynVersion(result);
    }

    [Fact]
    public async Task IsRoslynComponentProperty_AutoDefaultsRoslynVersion()
    {
        await using var project = CreateProject();

        var result = await project
            .WithFilename("Sample.csproj")
            .WithOutputType(Val.Library)
            .WithTargetFramework(Tfm.NetStandard20)
            .WithProperty("IsRoslynComponent", "true")
            .AddSource("Gen.cs", "public class Gen { }")
            .BuildAsync();

        AssertUsesDefaultRoslynVersion(result);
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

    [Fact]
    public async Task CentralPackageManagement_UsesVersionOverrideWithoutVersion()
    {
        await using var project = CreateProject();

        project.AddFile("Directory.Packages.props", """
            <Project>
              <PropertyGroup>
                <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
                <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
              </PropertyGroup>
            </Project>
            """);

        var result = await project
            .WithFilename("Sample.Generator.csproj")
            .WithOutputType(Val.Library)
            .WithTargetFramework(Tfm.NetStandard20)
            .WithProperty("ManagePackageVersionsCentrally", "true")
            .AddSource("Gen.cs", "public class Gen { }")
            .BuildAsync();

        result.ShouldSucceed();
        AssertUsesDefaultRoslynVersion(result);
    }

    [Fact]
    public async Task DisableImplicitRoslynPackageReference_SkipsImplicitPackage()
    {
        await using var project = CreateProject();

        var result = await project
            .WithFilename("Sample.Generator.csproj")
            .WithOutputType(Val.Library)
            .WithTargetFramework(Tfm.NetStandard20)
            .WithProperty("DisableImplicitRoslynPackageReference", "true")
            .AddSource("Gen.cs", "public class Gen { }")
            .BuildAsync();

        result.ShouldHavePropertyValue("_IsSourceGeneratorProject", "true");
        result.ShouldHavePropertyValue("SourceGeneratorRoslynVersion", GetRequiredProperty(result, "RoslynVersion"));
        Assert.DoesNotContain(result.GetMsBuildItems("PackageReference"),
            static item => item.Contains("Microsoft.CodeAnalysis.CSharp", StringComparison.Ordinal));
    }

    private static void AssertUsesDefaultRoslynVersion(BuildResult result)
    {
        result.ShouldHavePropertyValue("_IsSourceGeneratorProject", "true");
        result.ShouldHavePropertyValue("SourceGeneratorRoslynVersion", GetRequiredProperty(result, "RoslynVersion"));
        Assert.Contains(result.GetMsBuildItems("PackageReference"),
            static item => item.Contains("Microsoft.CodeAnalysis.CSharp", StringComparison.Ordinal));
    }

    private static string GetRequiredProperty(BuildResult result, string propertyName)
    {
        var value = result.GetMsBuildPropertyValue(propertyName);
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"{propertyName} was not evaluated.");

        return value;
    }
}
