using static ANcpLua.Sdk.Tests.Helpers.PackageFixture;

namespace ANcpLua.Sdk.Tests;

/// <summary>
///     Comprehensive test matrix for MTP (Microsoft Testing Platform) detection.
///     All test projects use MTP - VSTest is deprecated on .NET 10+.
///     SDK supports:
///     - TUnit (always MTP)
///     - xunit.v3.mtp-v1 / xunit.v3.mtp-v2 (explicit MTP)
///     - NUnit with EnableNUnitRunner=true (explicit opt-in)
///     - MSTest with EnableMSTestRunner=true (explicit opt-in)
/// </summary>
public sealed class MtpDetectionNet100Tests(PackageFixture fixture)
    : MtpDetectionTests(fixture, NetSdkVersion.Net100);

public abstract class MtpDetectionTests(
    PackageFixture fixture,
    NetSdkVersion dotnetSdkVersion)
{
    private static readonly NuGetReference[] _xUnit3MtpV1Packages =
        [new("xunit.v3.mtp-v1", "3.2.1")];

    private static readonly NuGetReference[] _xUnit3MtpV2Packages =
        [new("xunit.v3.mtp-v2", "3.2.1")];

    private static readonly NuGetReference[] _nUnitMtpPackages =
        [new("NUnit", "4.3.2"), new("NUnit3TestAdapter", "5.0.0")];

    private static readonly NuGetReference[] _msTestMtpPackages =
        [new("MSTest.TestFramework", "3.8.3"), new("MSTest.TestAdapter", "3.8.3")];

    private static readonly NuGetReference[] _tUnitPackages =
        [new("TUnit", "0.18.0")];

    private readonly NetSdkVersion _dotnetSdkVersion = dotnetSdkVersion;
    private readonly PackageFixture _fixture = fixture;

    private SdkProjectBuilder CreateProject(string? sdkName = null) =>
        SdkProjectBuilder.Create(_fixture, SdkImportStyle.ProjectElement, sdkName ?? SdkTestName)
            .WithDotnetSdkVersion(_dotnetSdkVersion);

    [Fact]
    public async Task XUnit3MtpV2_IsMTP()
    {
        await using var project = CreateProject(SdkName);

        foreach (var pkg in _xUnit3MtpV2Packages)
            project.WithPackage(pkg.Name, pkg.Version);

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

        result.ShouldHavePropertyValue("IsTestProject", "true");
        result.ShouldHavePropertyValue("UseMicrosoftTestingPlatform", "true");
        result.ShouldHavePropertyValue("OutputType", "exe");
        result.ShouldHavePropertyValue("TestingPlatformDotnetTestSupport", "true");
    }

    [Fact]
    public async Task XUnit3MtpV2_DoesNotInjectMTPExtensions()
    {
        await using var project = CreateProject();

        foreach (var pkg in _xUnit3MtpV2Packages)
            project.WithPackage(pkg.Name, pkg.Version);

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
        Assert.DoesNotContain(items,
            static i => i.Contains("Microsoft.Testing.Extensions.CrashDump", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(items,
            static i => i.Contains("Microsoft.Testing.Extensions.TrxReport", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(items,
            static i => i.Contains("Microsoft.Testing.Extensions.HangDump", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task XUnit3MtpV2_UsesNativeTrxOption()
    {
        await using var project = CreateProject();

        foreach (var pkg in _xUnit3MtpV2Packages)
            project.WithPackage(pkg.Name, pkg.Version);

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

        var cliArgs = result.GetMsBuildPropertyValue("TestingPlatformCommandLineArguments");
        Assert.Contains("--report-xunit-trx", cliArgs);
        Assert.DoesNotContain("--report-trx ", cliArgs);
        Assert.DoesNotContain("--crashdump", cliArgs);
        Assert.DoesNotContain("--hangdump", cliArgs);
    }

    [Fact]
    public async Task XUnit3MtpV1_IsMTP()
    {
        await using var project = CreateProject();

        foreach (var pkg in _xUnit3MtpV1Packages)
            project.WithPackage(pkg.Name, pkg.Version);

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

        result.ShouldHavePropertyValue("UseMicrosoftTestingPlatform", "true");
        result.ShouldHavePropertyValue("OutputType", "exe");
    }

    [Fact]
    public async Task NUnit_WithEnableNUnitRunner_IsMTP()
    {
        await using var project = CreateProject(SdkName);

        foreach (var pkg in _nUnitMtpPackages)
            project.WithPackage(pkg.Name, pkg.Version);

        var result = await project
            .WithFilename("Sample.Tests.csproj")
            .WithTargetFramework("net10.0")
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

        result.ShouldHavePropertyValue("UseMicrosoftTestingPlatform", "true");
        result.ShouldHavePropertyValue("OutputType", "exe");
    }

    [Fact]
    public async Task MSTest_WithEnableMSTestRunner_IsMTP()
    {
        await using var project = CreateProject(SdkName);

        foreach (var pkg in _msTestMtpPackages)
            project.WithPackage(pkg.Name, pkg.Version);

        var result = await project
            .WithFilename("Sample.Tests.csproj")
            .WithTargetFramework("net10.0")
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

        result.ShouldHavePropertyValue("UseMicrosoftTestingPlatform", "true");
        result.ShouldHavePropertyValue("OutputType", "exe");
    }

    [Fact]
    public async Task TUnit_IsMTP()
    {
        await using var project = CreateProject(SdkName);

        foreach (var pkg in _tUnitPackages)
            project.WithPackage(pkg.Name, pkg.Version);

        var result = await project
            .WithFilename("Sample.Tests.csproj")
            .WithTargetFramework("net10.0")
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

        result.ShouldHavePropertyValue("IsTestProject", "true");
        result.ShouldHavePropertyValue("UseMicrosoftTestingPlatform", "true");
        result.ShouldHavePropertyValue("OutputType", "exe");
    }

    [Fact]
    public async Task MTP_DoesNotInjectMicrosoftNETTestSdk()
    {
        await using var project = CreateProject(SdkName);

        foreach (var pkg in _tUnitPackages)
            project.WithPackage(pkg.Name, pkg.Version);

        var result = await project
            .WithFilename("Sample.Tests.csproj")
            .WithTargetFramework("net10.0")
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
        Assert.DoesNotContain(items,
            static i => i.Contains("Microsoft.NET.Test.Sdk", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task MTP_InjectsMTPExtensions()
    {
        await using var project = CreateProject(SdkName);

        foreach (var pkg in _tUnitPackages)
            project.WithPackage(pkg.Name, pkg.Version);

        var result = await project
            .WithFilename("Sample.Tests.csproj")
            .WithTargetFramework("net10.0")
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
        Assert.Contains(items,
            static i => i.Contains("Microsoft.Testing.Extensions.CrashDump", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(items,
            static i => i.Contains("Microsoft.Testing.Extensions.TrxReport", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SafetyGuard_WarnsWhenMTPWithLibraryOutputType()
    {
        await using var project = CreateProject();

        foreach (var pkg in _xUnit3MtpV2Packages)
            project.WithPackage(pkg.Name, pkg.Version);

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
    public async Task ExplicitProperty_UseMicrosoftTestingPlatform_EnablesMTP()
    {
        await using var project = CreateProject();

        foreach (var pkg in _xUnit3MtpV2Packages)
            project.WithPackage(pkg.Name, pkg.Version);

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

        result.ShouldHavePropertyValue("UseMicrosoftTestingPlatform", "true");
        result.ShouldHavePropertyValue("OutputType", "exe");
    }
}
