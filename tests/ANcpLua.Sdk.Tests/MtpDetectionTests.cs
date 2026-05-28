using static ANcpLua.Sdk.Tests.Infrastructure.PackageFixture;

namespace ANcpLua.Sdk.Tests;

public sealed class MtpDetectionNet100Tests(PackageFixture fixture)
    : MtpDetectionTests(fixture, NetSdkVersion.Net100);

public abstract class MtpDetectionTests(PackageFixture fixture, NetSdkVersion dotnetSdkVersion)
    : SdkTestBase(fixture)
{
    private static readonly NuGetReference[] s_xUnit3MtpV1Packages = [new("xunit.v3.mtp-v1", "3.2.1")];
    private static readonly NuGetReference[] s_xUnit3MtpV2Packages = [new("xunit.v3.mtp-v2", "3.2.1")];
    private static readonly NuGetReference[] s_nUnitMtpPackages = [new("NUnit", "4.3.2"), new("NUnit3TestAdapter", "5.0.0")];
    private static readonly NuGetReference[] s_msTestMtpPackages = [new("MSTest.TestFramework", "3.8.3"), new("MSTest.TestAdapter", "3.8.3")];
    private static readonly NuGetReference[] s_tUnitPackages = [new("TUnit", "0.18.0")];

    private static readonly string[] s_recordedProperties =
    [
        "IsTestProject",
        "UseMicrosoftTestingPlatform",
        "OutputType",
        "TestingPlatformDotnetTestSupport",
        "TestingPlatformCommandLineArguments"
    ];

    private SdkProjectBuilder CreateProject(string? sdkName = null) =>
        CreateProject(SdkImportStyle.ProjectElement, sdkName);

    private SdkProjectBuilder CreateProject(SdkImportStyle style, string? sdkName = null) =>
        CreateProject(style, sdkName ?? SdkTestName, dotnetSdkVersion, s_recordedProperties);

    private static void AddPackages(SdkProjectBuilder project, NuGetReference[] packages)
    {
        foreach (var package in packages)
            project.WithPackage(package.Name, package.Version);
    }

    private static void AssertProducesExecutable(SdkProjectBuilder project)
    {
        var runtimeConfigs = Directory
            .EnumerateFiles(project.RootFolder, "Sample.Tests.runtimeconfig.json", SearchOption.AllDirectories)
            .ToArray();
        Assert.NotEmpty(runtimeConfigs);
    }

    [Fact]
    public async Task Detect_WhenXUnit3MtpV2PackagePresent_RecognizesAsMtp()
    {
        await using var project = CreateProject(SdkName);
        AddPackages(project, s_xUnit3MtpV2Packages);

        var result = await project
            .WithFilename("Sample.Tests.csproj")
            .WithProperty("IsTestProject", "true")
            .AddSource("Tests.cs", """
                public class SampleTests
                {
                    [Fact]
                    public void Test1() => Assert.True(true);
                }
                """)
            .BuildAsync();

        result.ShouldHaveRecordedProperty("IsTestProject", "true");
        result.ShouldHaveRecordedProperty("UseMicrosoftTestingPlatform", "true");
        result.ShouldHaveRecordedProperty("OutputType", "exe");
        result.ShouldHaveRecordedProperty("TestingPlatformDotnetTestSupport", "true");
    }

    [Fact]
    public async Task Build_WhenXUnit3MtpV2_DoesNotInjectMtpExtensions()
    {
        await using var project = CreateProject();
        AddPackages(project, s_xUnit3MtpV2Packages);

        var result = await project
            .WithFilename("Sample.Tests.csproj")
            .AddSource("Tests.cs", """
                public class SampleTests
                {
                    [Fact]
                    public void Test1() => Assert.True(true);
                }
                """)
            .BuildAsync();

        var items = result.GetMsBuildItems("PackageReference");
        Assert.DoesNotContain(items, static i => i.Contains("Microsoft.Testing.Extensions.CrashDump", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(items, static i => i.Contains("Microsoft.Testing.Extensions.TrxReport", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(items, static i => i.Contains("Microsoft.Testing.Extensions.HangDump", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Build_WhenXUnit3MtpV2_UsesNativeTrxCliArguments()
    {
        await using var project = CreateProject();
        AddPackages(project, s_xUnit3MtpV2Packages);

        var result = await project
            .WithFilename("Sample.Tests.csproj")
            .AddSource("Tests.cs", """
                public class SampleTests
                {
                    [Fact]
                    public void Test1() => Assert.True(true);
                }
                """)
            .BuildAsync();

        var cliArgs = result.GetRecordedProperty("TestingPlatformCommandLineArguments");
        Assert.Contains("--report-xunit-trx", cliArgs, StringComparison.Ordinal);
        Assert.DoesNotContain("--report-trx", cliArgs, StringComparison.Ordinal);
        Assert.DoesNotContain("--crashdump", cliArgs, StringComparison.Ordinal);
        Assert.DoesNotContain("--hangdump", cliArgs, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Detect_WhenXUnit3MtpV1PackagePresent_RecognizesAsMtp()
    {
        await using var project = CreateProject();
        AddPackages(project, s_xUnit3MtpV1Packages);

        var result = await project
            .WithFilename("Sample.Tests.csproj")
            .AddSource("Tests.cs", """
                public class SampleTests
                {
                    [Fact]
                    public void Test1() => Assert.True(true);
                }
                """)
            .BuildAsync();

        result.ShouldHaveRecordedProperty("UseMicrosoftTestingPlatform", "true");
        result.ShouldHaveRecordedProperty("OutputType", "exe");
    }

    [Fact]
    public async Task Detect_WhenNUnitWithEnableNUnitRunner_RecognizesAsMtp()
    {
        await using var project = CreateProject(SdkName);
        AddPackages(project, s_nUnitMtpPackages);

        var result = await project
            .WithFilename("Sample.Tests.csproj")
            .WithTargetFramework(Tfm.Net100)
            .WithProperty("IsTestProject", "true")
            .WithProperty("EnableNUnitRunner", "true")
            .AddSource("Tests.cs", """
                using NUnit.Framework;

                [TestFixture]
                public class SampleTests
                {
                    [Test]
                    public void Test1() => Assert.That(true, Is.True);
                }
                """)
            .BuildAsync();

        result.ShouldHaveRecordedProperty("UseMicrosoftTestingPlatform", "true");
        result.ShouldHaveRecordedProperty("OutputType", "exe");
    }

    [Fact]
    public async Task Detect_WhenMSTestWithEnableMSTestRunner_RecognizesAsMtp()
    {
        await using var project = CreateProject(SdkName);
        AddPackages(project, s_msTestMtpPackages);

        var result = await project
            .WithFilename("Sample.Tests.csproj")
            .WithTargetFramework(Tfm.Net100)
            .WithProperty("IsTestProject", "true")
            .WithProperty("EnableMSTestRunner", "true")
            .AddSource("Tests.cs", """
                using Microsoft.VisualStudio.TestTools.UnitTesting;

                [TestClass]
                public class SampleTests
                {
                    [TestMethod]
                    public void Test1() => Assert.IsTrue(true);
                }
                """)
            .BuildAsync();

        result.ShouldHaveRecordedProperty("UseMicrosoftTestingPlatform", "true");
        result.ShouldHaveRecordedProperty("OutputType", "exe");
    }

    [Fact]
    public async Task Detect_WhenTUnitPackagePresent_RecognizesAsMtp()
    {
        await using var project = CreateProject(SdkName);
        AddPackages(project, s_tUnitPackages);

        var result = await project
            .WithFilename("Sample.Tests.csproj")
            .WithTargetFramework(Tfm.Net100)
            .WithProperty("IsTestProject", "true")
            .WithProperty("SkipXunitInjection", "true")
            .AddSource("Tests.cs", """
                using TUnit.Core;

                public class SampleTests
                {
                    [Test]
                    public async Task Test1() => await Assert.That(true).IsTrue();
                }
                """)
            .BuildAsync();

        result.ShouldHaveRecordedProperty("IsTestProject", "true");
        result.ShouldHaveRecordedProperty("UseMicrosoftTestingPlatform", "true");
        result.ShouldHaveRecordedProperty("OutputType", "exe");
    }

    [Fact]
    public async Task Build_WhenMtpEnabled_DoesNotInjectMicrosoftNetTestSdk()
    {
        await using var project = CreateProject(SdkName);
        AddPackages(project, s_tUnitPackages);

        var result = await project
            .WithFilename("Sample.Tests.csproj")
            .WithTargetFramework(Tfm.Net100)
            .WithProperty("IsTestProject", "true")
            .WithProperty("SkipXunitInjection", "true")
            .AddSource("Tests.cs", """
                using TUnit.Core;
                public class SampleTests
                {
                    [Test]
                    public async Task Test1() => await Assert.That(true).IsTrue();
                }
                """)
            .BuildAsync();

        Assert.DoesNotContain(result.GetMsBuildItems("PackageReference"),
            static i => i.Contains("Microsoft.NET.Test.Sdk", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Build_WhenMtpEnabled_InjectsMtpExtensions()
    {
        await using var project = CreateProject(SdkName);
        AddPackages(project, s_tUnitPackages);

        var result = await project
            .WithFilename("Sample.Tests.csproj")
            .WithTargetFramework(Tfm.Net100)
            .WithProperty("IsTestProject", "true")
            .WithProperty("SkipXunitInjection", "true")
            .AddSource("Tests.cs", """
                using TUnit.Core;
                public class SampleTests
                {
                    [Test]
                    public async Task Test1() => await Assert.That(true).IsTrue();
                }
                """)
            .BuildAsync();

        var items = result.GetMsBuildItems("PackageReference");
        Assert.Contains(items, static i => i.Contains("Microsoft.Testing.Extensions.CrashDump", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(items, static i => i.Contains("Microsoft.Testing.Extensions.TrxReport", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Validate_WhenMtpEnabledWithLibraryOutput_WarnsOrErrors()
    {
        await using var project = CreateProject();
        AddPackages(project, s_xUnit3MtpV2Packages);

        var result = await project
            .WithFilename("Sample.Tests.csproj")
            .WithProperty("UseMicrosoftTestingPlatform", "true")
            .WithOutputType("Library")
            .AddSource("Tests.cs", """
                public class SampleTests
                {
                    [Fact]
                    public void Test1() => Assert.True(true);
                }
                """)
            .BuildAsync();

        Assert.True(
            result.OutputContains("ANCPSDK001") ||
            result.OutputContains("MTP is enabled") ||
            result.OutputContains("test projects must be executable"),
            "Should warn/error about MTP with Library OutputType");
    }

    [Fact]
    public async Task Enable_WhenUseMicrosoftTestingPlatformPropertyTrue_EnablesMtp()
    {
        await using var project = CreateProject();
        AddPackages(project, s_xUnit3MtpV2Packages);

        var result = await project
            .WithFilename("Sample.Tests.csproj")
            .WithProperty("UseMicrosoftTestingPlatform", "true")
            .AddSource("Tests.cs", """
                public class SampleTests
                {
                    [Fact]
                    public void Test1() => Assert.True(true);
                }
                """)
            .BuildAsync();

        result.ShouldHaveRecordedProperty("UseMicrosoftTestingPlatform", "true");
        result.ShouldHaveRecordedProperty("OutputType", "exe");
    }

    [Fact]
    public async Task Build_WhenTUnitWithoutOutputType_ProducesExecutable()
    {
        await using var project = CreateProject();
        AddPackages(project, s_tUnitPackages);

        var result = await project
            .WithFilename("Sample.Tests.csproj")
            .OmitOutputType()
            .WithProperty("SkipXunitInjection", "true")
            .AddSource("Tests.cs", """
                using TUnit.Core;
                public class SampleTests
                {
                    [Test]
                    public async Task Test1() => await Assert.That(true).IsTrue();
                }
                """)
            .BuildAsync();

        result.ShouldHaveRecordedProperty("OutputType", "exe");
        result.ShouldHaveRecordedProperty("UseMicrosoftTestingPlatform", "true");
        AssertProducesExecutable(project);
    }

    [Fact]
    public async Task Build_WhenNUnitWithoutOutputType_ProducesExecutable()
    {
        await using var project = CreateProject();
        AddPackages(project, s_nUnitMtpPackages);

        var result = await project
            .WithFilename("Sample.Tests.csproj")
            .OmitOutputType()
            .WithProperty("EnableNUnitRunner", "true")
            .AddSource("Tests.cs", """
                using NUnit.Framework;

                [TestFixture]
                public class SampleTests
                {
                    [Test]
                    public void Test1() => Assert.That(true, Is.True);
                }
                """)
            .BuildAsync();

        result.ShouldHaveRecordedProperty("OutputType", "exe");
        result.ShouldHaveRecordedProperty("UseMicrosoftTestingPlatform", "true");
        AssertProducesExecutable(project);
    }

    [Theory]
    [InlineData(SdkImportStyle.ProjectElement)]
    [InlineData(SdkImportStyle.SdkElement)]
    [InlineData(SdkImportStyle.SdkElementDirectoryBuildProps)]
    public async Task Build_WhenMtpWithoutOutputTypeAcrossImportStyles_ProducesExecutable(SdkImportStyle style)
    {
        await using var project = CreateProject(style);
        AddPackages(project, s_xUnit3MtpV2Packages);

        var result = await project
            .WithFilename("Sample.Tests.csproj")
            .OmitOutputType()
            .AddSource("Tests.cs", """
                public class SampleTests
                {
                    [Fact]
                    public void Test1() => Assert.True(true);
                }
                """)
            .BuildAsync();

        result.ShouldHaveRecordedProperty("OutputType", "exe");
        AssertProducesExecutable(project);
    }

    [Theory]
    [InlineData(SdkImportStyle.ProjectElement)]
    [InlineData(SdkImportStyle.SdkElement)]
    [InlineData(SdkImportStyle.SdkElementDirectoryBuildProps)]
    public async Task Build_WhenExplicitLibraryOutputAcrossImportStyles_ConsumerSettingWinsSdkDefault(SdkImportStyle style)
    {
        await using var project = CreateProject(style);

        var result = await project
            .WithFilename("Sample.Tests.csproj")
            .WithOutputType("Library")
            .WithProperty("UseMicrosoftTestingPlatform", "false")
            .WithProperty("SkipXunitInjection", "true")
            .AddSource("Lib.cs", "namespace Sample.Tests { public class C { } }")
            .BuildAsync();

        result.ShouldHaveRecordedProperty("OutputType", "library");

        var runtimeConfigs = Directory
            .EnumerateFiles(project.RootFolder, "Sample.Tests.runtimeconfig.json", SearchOption.AllDirectories)
            .ToArray();
        Assert.Empty(runtimeConfigs);
    }

    [Fact]
    public async Task Build_WhenMSTestWithoutOutputType_ProducesExecutable()
    {
        await using var project = CreateProject();
        AddPackages(project, s_msTestMtpPackages);

        var result = await project
            .WithFilename("Sample.Tests.csproj")
            .OmitOutputType()
            .WithProperty("EnableMSTestRunner", "true")
            .AddSource("Tests.cs", """
                using Microsoft.VisualStudio.TestTools.UnitTesting;

                [TestClass]
                public class SampleTests
                {
                    [TestMethod]
                    public void Test1() => Assert.IsTrue(true);
                }
                """)
            .BuildAsync();

        result.ShouldHaveRecordedProperty("OutputType", "exe");
        result.ShouldHaveRecordedProperty("UseMicrosoftTestingPlatform", "true");
        AssertProducesExecutable(project);
    }
}
