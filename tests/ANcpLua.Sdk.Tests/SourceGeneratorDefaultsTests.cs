using static ANcpLua.Sdk.Tests.Infrastructure.PackageFixture;

namespace ANcpLua.Sdk.Tests;

public sealed class SourceGeneratorDefaultsNet100Tests(PackageFixture fixture)
    : SourceGeneratorDefaultsTests(fixture, NetSdkVersion.Net100);

public abstract class SourceGeneratorDefaultsTests(PackageFixture fixture, NetSdkVersion dotnetSdkVersion)
    : SdkTestBase(fixture)
{
    private static readonly string[] s_recordedProperties =
    [
        "_IsSourceGeneratorProject",
        "SourceGeneratorRoslynVersion",
        "RoslynVersion"
    ];

    private SdkProjectBuilder CreateProject(string sdkName = SdkName) =>
        CreateProject(SdkImportStyle.ProjectElement, sdkName, dotnetSdkVersion, s_recordedProperties);

    [Fact]
    public async Task Build_WhenProjectNameContainsGeneratorUpperCase_DefaultsRoslynVersion()
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
    public async Task Build_WhenProjectNameContainsGeneratorLowerCase_DefaultsRoslynVersion()
    {
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
    public async Task Build_WhenProjectNameContainsAnalyzer_DefaultsRoslynVersion()
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
    public async Task Build_WhenIsSourceGeneratorPropertyTrue_DefaultsRoslynVersion()
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
    public async Task Build_WhenIsRoslynComponentPropertyTrue_DefaultsRoslynVersion()
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
    public async Task Build_WhenProjectIsRegularLibrary_DoesNotPinRoslyn()
    {
        await using var project = CreateProject();

        var result = await project
            .WithFilename("Sample.csproj")
            .WithOutputType(Val.Library)
            .AddSource("Sample.cs", "public class Sample { }")
            .BuildAsync();

        result.ShouldHaveRecordedProperty("_IsSourceGeneratorProject", null);
        result.ShouldHaveRecordedProperty("SourceGeneratorRoslynVersion", null);
    }

    [Fact]
    public async Task Build_WhenSourceGeneratorRoslynVersionIsExplicit_UsesExplicitVersion()
    {
        await using var project = CreateProject();

        var result = await project
            .WithFilename("Sample.Generator.csproj")
            .WithOutputType(Val.Library)
            .WithTargetFramework(Tfm.NetStandard20)
            .WithProperty("SourceGeneratorRoslynVersion", "4.11.0")
            .AddSource("Gen.cs", "public class Gen { }")
            .BuildAsync();

        result.ShouldHaveRecordedProperty("_IsSourceGeneratorProject", "true");
        result.ShouldHaveRecordedProperty("SourceGeneratorRoslynVersion", "4.11.0");
    }

    [Fact]
    public async Task Build_WhenCentralPackageManagementEnabled_UsesVersionOverride()
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
    public async Task Build_WhenDisableImplicitRoslynPackageReferenceTrue_SkipsImplicitPackage()
    {
        await using var project = CreateProject();

        var result = await project
            .WithFilename("Sample.Generator.csproj")
            .WithOutputType(Val.Library)
            .WithTargetFramework(Tfm.NetStandard20)
            .WithProperty("DisableImplicitRoslynPackageReference", "true")
            .AddSource("Gen.cs", "public class Gen { }")
            .BuildAsync();

        result.ShouldHaveRecordedProperty("_IsSourceGeneratorProject", "true");
        result.ShouldHaveRecordedProperty("SourceGeneratorRoslynVersion", GetRequiredRecordedProperty(result, "RoslynVersion"));
        Assert.DoesNotContain(result.GetMsBuildItems("PackageReference"),
            static item => item.Contains("Microsoft.CodeAnalysis.CSharp", StringComparison.Ordinal));
    }

    private static void AssertUsesDefaultRoslynVersion(BuildResult result)
    {
        result.ShouldHaveRecordedProperty("_IsSourceGeneratorProject", "true");
        result.ShouldHaveRecordedProperty("SourceGeneratorRoslynVersion", GetRequiredRecordedProperty(result, "RoslynVersion"));
        Assert.Contains(result.GetMsBuildItems("PackageReference"),
            static item => item.Contains("Microsoft.CodeAnalysis.CSharp", StringComparison.Ordinal));
    }

    private static string GetRequiredRecordedProperty(BuildResult result, string propertyName)
    {
        var value = result.GetRecordedProperty(propertyName);
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"{propertyName} was not recorded.");

        return value;
    }
}
