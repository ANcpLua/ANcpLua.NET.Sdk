using ANcpLua.Sdk.Tests.Helpers;
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
public sealed class MtpDetectionNet100Tests(PackageFixture fixture, ITestOutputHelper testOutputHelper)
    : MtpDetectionTests(fixture, testOutputHelper, NetSdkVersion.Net100);

public abstract class MtpDetectionTests(
    PackageFixture fixture,
    ITestOutputHelper testOutputHelper,
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
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

    private ProjectBuilder CreateProjectBuilder(string defaultSdkName = SdkTestName)
    {
        var builder = new ProjectBuilder(_fixture, _testOutputHelper, SdkImportStyle.ProjectElement, defaultSdkName);
        builder.SetDotnetSdkVersion(_dotnetSdkVersion);
        return builder;
    }

    [Fact]
    public async Task XUnit3MtpV2_IsMTP()
    {
        await using var project = CreateProjectBuilder(SdkName);
        project.AddCsprojFile(
            filename: "Sample.Tests.csproj",
            properties: [("IsTestProject", "true")],
            nuGetPackages: [.. _xUnit3MtpV2Packages]
        );

        project.AddFile("Tests.cs", """
                                    public class SampleTests
                                    {
                                        [Fact]
                                        public void Test1() => Assert.True(true);
                                    }
                                    """);

        var data = await project.BuildAndGetOutput();

        data.AssertMsBuildPropertyValue("IsTestProject", "true");

        data.AssertMsBuildPropertyValue("UseMicrosoftTestingPlatform", "true");

        data.AssertMsBuildPropertyValue("OutputType", "exe");

        data.AssertMsBuildPropertyValue("TestingPlatformDotnetTestSupport", "true");
    }

    [Fact]
    public async Task XUnit3MtpV2_DoesNotInjectMTPExtensions()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile(
            filename: "Sample.Tests.csproj",
            nuGetPackages: [.. _xUnit3MtpV2Packages]
        );

        project.AddFile("Tests.cs", """
                                    public class SampleTests
                                    {
                                        [Fact]
                                        public void Test1() => Assert.True(true);
                                    }
                                    """);

        var data = await project.BuildAndGetOutput();

        var items = data.GetMsBuildItems("PackageReference");
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
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile(
            filename: "Sample.Tests.csproj",
            nuGetPackages: [.. _xUnit3MtpV2Packages]
        );

        project.AddFile("Tests.cs", """
                                    public class SampleTests
                                    {
                                        [Fact]
                                        public void Test1() => Assert.True(true);
                                    }
                                    """);

        var data = await project.BuildAndGetOutput();

        var cliArgs = data.GetMsBuildPropertyValue("TestingPlatformCommandLineArguments");
        Assert.Contains("--report-xunit-trx", cliArgs);
        Assert.DoesNotContain("--report-trx ", cliArgs);
        Assert.DoesNotContain("--crashdump", cliArgs);
        Assert.DoesNotContain("--hangdump", cliArgs);
    }

    [Fact]
    public async Task XUnit3MtpV1_IsMTP()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile(
            filename: "Sample.Tests.csproj",
            nuGetPackages: [.. _xUnit3MtpV1Packages]
        );

        project.AddFile("Tests.cs", """
                                    public class SampleTests
                                    {
                                        [Fact]
                                        public void Test1() => Assert.True(true);
                                    }
                                    """);

        var data = await project.BuildAndGetOutput();

        data.AssertMsBuildPropertyValue("UseMicrosoftTestingPlatform", "true");
        data.AssertMsBuildPropertyValue("OutputType", "exe");
    }

    [Fact]
    public async Task NUnit_WithEnableNUnitRunner_IsMTP()
    {
        await using var project = CreateProjectBuilder(SdkName);
        project.AddCsprojFile(
            filename: "Sample.Tests.csproj",
            properties: [("TargetFramework", "net10.0"), ("IsTestProject", "true"), ("EnableNUnitRunner", "true")],
            nuGetPackages: [.. _nUnitMtpPackages]
        );

        project.AddFile("Tests.cs", """
                                    using NUnit.Framework;

                                    [TestFixture]
                                    public class SampleTests
                                    {
                                        [Test]
                                        public void Test1() => Assert.That(true, Is.True);
                                    }
                                    """);

        var data = await project.BuildAndGetOutput();

        data.AssertMsBuildPropertyValue("UseMicrosoftTestingPlatform", "true");
        data.AssertMsBuildPropertyValue("OutputType", "exe");
    }

    [Fact]
    public async Task MSTest_WithEnableMSTestRunner_IsMTP()
    {
        await using var project = CreateProjectBuilder(SdkName);
        project.AddCsprojFile(
            filename: "Sample.Tests.csproj",
            properties: [("TargetFramework", "net10.0"), ("IsTestProject", "true"), ("EnableMSTestRunner", "true")],
            nuGetPackages: [.. _msTestMtpPackages]
        );

        project.AddFile("Tests.cs", """
                                    using Microsoft.VisualStudio.TestTools.UnitTesting;

                                    [TestClass]
                                    public class SampleTests
                                    {
                                        [TestMethod]
                                        public void Test1() => Assert.IsTrue(true);
                                    }
                                    """);

        var data = await project.BuildAndGetOutput();

        data.AssertMsBuildPropertyValue("UseMicrosoftTestingPlatform", "true");
        data.AssertMsBuildPropertyValue("OutputType", "exe");
    }

    [Fact]
    public async Task TUnit_IsMTP()
    {
        await using var project = CreateProjectBuilder(SdkName);
        project.AddCsprojFile(
            filename: "Sample.Tests.csproj",
            properties: [("TargetFramework", "net10.0"), ("IsTestProject", "true"), ("SkipXunitInjection", "true")],
            nuGetPackages: [.. _tUnitPackages]
        );

        project.AddFile("Tests.cs", """
                                    using TUnit.Core;

                                    public class SampleTests
                                    {
                                        [Test]
                                        public async Task Test1() => await Assert.That(true).IsTrue();
                                    }
                                    """);

        var data = await project.BuildAndGetOutput();

        data.AssertMsBuildPropertyValue("IsTestProject", "true");

        data.AssertMsBuildPropertyValue("UseMicrosoftTestingPlatform", "true");
        data.AssertMsBuildPropertyValue("OutputType", "exe");
    }

    [Fact]
    public async Task MTP_DoesNotInjectMicrosoftNETTestSdk()
    {
        await using var project = CreateProjectBuilder(SdkName);
        project.AddCsprojFile(
            filename: "Sample.Tests.csproj",
            properties: [("TargetFramework", "net10.0"), ("IsTestProject", "true"), ("SkipXunitInjection", "true")],
            nuGetPackages: [.. _tUnitPackages]
        );

        project.AddFile("Tests.cs", """
                                    using TUnit.Core;
                                    public class SampleTests
                                    {
                                        [Test]
                                        public async Task Test1() => await Assert.That(true).IsTrue();
                                    }
                                    """);

        var data = await project.BuildAndGetOutput();

        var items = data.GetMsBuildItems("PackageReference");
        Assert.DoesNotContain(items,
            static i => i.Contains("Microsoft.NET.Test.Sdk", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task MTP_InjectsMTPExtensions()
    {
        await using var project = CreateProjectBuilder(SdkName);
        project.AddCsprojFile(
            filename: "Sample.Tests.csproj",
            properties: [("TargetFramework", "net10.0"), ("IsTestProject", "true"), ("SkipXunitInjection", "true")],
            nuGetPackages: [.. _tUnitPackages]
        );

        project.AddFile("Tests.cs", """
                                    using TUnit.Core;
                                    public class SampleTests
                                    {
                                        [Test]
                                        public async Task Test1() => await Assert.That(true).IsTrue();
                                    }
                                    """);

        var data = await project.BuildAndGetOutput();

        var items = data.GetMsBuildItems("PackageReference");
        Assert.Contains(items,
            static i => i.Contains("Microsoft.Testing.Extensions.CrashDump", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(items,
            static i => i.Contains("Microsoft.Testing.Extensions.TrxReport", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SafetyGuard_WarnsWhenMTPWithLibraryOutputType()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile(
            filename: "Sample.Tests.csproj",
            properties: [("UseMicrosoftTestingPlatform", "true"), ("OutputType", "Library")],
            nuGetPackages: [.. _xUnit3MtpV2Packages]
        );

        project.AddFile("Tests.cs", """
                                    public class SampleTests
                                    {
                                        [Fact]
                                        public void Test1() => Assert.True(true);
                                    }
                                    """);

        var data = await project.BuildAndGetOutput();

        Assert.True(
            data.OutputContains("ANCPSDK001") ||
            data.OutputContains("MTP is enabled") ||
            data.OutputContains("test projects must be executable"),
            "Should warn/error about MTP with Library OutputType");
    }

    [Fact]
    public async Task ExplicitProperty_UseMicrosoftTestingPlatform_EnablesMTP()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile(
            filename: "Sample.Tests.csproj",
            properties: [("UseMicrosoftTestingPlatform", "true")],
            nuGetPackages: [.. _xUnit3MtpV2Packages]
        );

        project.AddFile("Tests.cs", """
                                    public class SampleTests
                                    {
                                        [Fact]
                                        public void Test1() => Assert.True(true);
                                    }
                                    """);

        var data = await project.BuildAndGetOutput();

        data.AssertMsBuildPropertyValue("UseMicrosoftTestingPlatform", "true");
        data.AssertMsBuildPropertyValue("OutputType", "exe");
    }
}
