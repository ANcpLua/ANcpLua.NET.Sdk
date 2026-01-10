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
    // Package references for each scenario - note: renovate will update these
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

    private ProjectBuilder CreateProjectBuilder(string defaultSdkName = SdkTestName)
    {
        var builder = new ProjectBuilder(fixture, testOutputHelper, SdkImportStyle.ProjectElement, defaultSdkName);
        builder.SetDotnetSdkVersion(dotnetSdkVersion);
        return builder;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // A) xUnit v3 + MTP v2 (explicit)
    // Expected: IsTestProject=true, UseMicrosoftTestingPlatform=true, OutputType=Exe
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task XUnit3MtpV2_IsMTP()
    {
        // Use base SDK and manually add xunit to test MTP detection
        // (Test SDK auto-injects xunit which would cause duplicates)
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

        // Assert test project detection
        data.AssertMsBuildPropertyValue("IsTestProject", "true");

        // Assert MTP IS enabled
        data.AssertMsBuildPropertyValue("UseMicrosoftTestingPlatform", "true");

        // Assert OutputType is Exe (MTP requirement)
        data.AssertMsBuildPropertyValue("OutputType", "exe");

        // Assert TestingPlatformDotnetTestSupport is enabled
        data.AssertMsBuildPropertyValue("TestingPlatformDotnetTestSupport", "true");
    }

    [Fact]
    public async Task XUnit3MtpV2_DoesNotInjectMTPExtensions()
    {
        // xunit.v3.mtp has its own native MTP implementation - SDK should NOT inject
        // Microsoft.Testing.Extensions packages (they use different CLI options)
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

        // xunit.v3.mtp should NOT have Microsoft.Testing.Extensions injected
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
        // xunit.v3.mtp uses --report-xunit-trx, NOT --report-trx
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

        // Verify the correct xunit.v3 native option is used
        var cliArgs = data.GetMsBuildPropertyValue("TestingPlatformCommandLineArguments");
        Assert.Contains("--report-xunit-trx", cliArgs);
        Assert.DoesNotContain("--report-trx ", cliArgs); // Note: space to avoid matching --report-xunit-trx
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

        // Assert MTP IS enabled for mtp-v1 as well
        data.AssertMsBuildPropertyValue("UseMicrosoftTestingPlatform", "true");
        data.AssertMsBuildPropertyValue("OutputType", "exe");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // B) NUnit with MTP (explicit opt-in via EnableNUnitRunner)
    // Expected: IsTestProject=true, UseMicrosoftTestingPlatform=true
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task NUnit_WithEnableNUnitRunner_IsMTP()
    {
        // Use base SDK (not Test SDK) because NUnit doesn't need xunit auto-injection
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

        // Assert MTP IS enabled with explicit opt-in
        data.AssertMsBuildPropertyValue("UseMicrosoftTestingPlatform", "true");
        data.AssertMsBuildPropertyValue("OutputType", "exe");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // C) MSTest with MTP (explicit opt-in via EnableMSTestRunner)
    // Expected: IsTestProject=true, UseMicrosoftTestingPlatform=true
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task MSTest_WithEnableMSTestRunner_IsMTP()
    {
        // Use base SDK (not Test SDK) because MSTest doesn't need xunit auto-injection
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

        // Assert MTP IS enabled with explicit opt-in
        data.AssertMsBuildPropertyValue("UseMicrosoftTestingPlatform", "true");
        data.AssertMsBuildPropertyValue("OutputType", "exe");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // D) TUnit (always MTP)
    // Expected: IsTestProject=true, UseMicrosoftTestingPlatform=true
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task TUnit_IsMTP()
    {
        // Use base SDK with TUnit - skip xunit injection to avoid conflicts
        await using var project = CreateProjectBuilder(SdkName);
        project.AddCsprojFile(
            filename: "Sample.Tests.csproj",
            properties: [("TargetFramework", "net10.0"), ("IsTestProject", "true"), ("SkipXunitInjection", "true")],
            nuGetPackages: [.. _tUnitPackages]
        );

        // TUnit 0.18+ requires await on assertions
        project.AddFile("Tests.cs", """
                                    using TUnit.Core;

                                    public class SampleTests
                                    {
                                        [Test]
                                        public async Task Test1() => await Assert.That(true).IsTrue();
                                    }
                                    """);

        var data = await project.BuildAndGetOutput();

        // Assert test project detection
        data.AssertMsBuildPropertyValue("IsTestProject", "true");

        // Assert MTP IS enabled (TUnit is always MTP)
        data.AssertMsBuildPropertyValue("UseMicrosoftTestingPlatform", "true");
        data.AssertMsBuildPropertyValue("OutputType", "exe");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PACKAGE INJECTION VERIFICATION
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task MTP_DoesNotInjectMicrosoftNETTestSdk()
    {
        // Use TUnit with base SDK - skip xunit injection to avoid conflicts
        await using var project = CreateProjectBuilder(SdkName);
        project.AddCsprojFile(
            filename: "Sample.Tests.csproj",
            properties: [("TargetFramework", "net10.0"), ("IsTestProject", "true"), ("SkipXunitInjection", "true")],
            nuGetPackages: [.. _tUnitPackages]
        );

        // TUnit 0.18+ requires await on assertions
        project.AddFile("Tests.cs", """
                                    using TUnit.Core;
                                    public class SampleTests
                                    {
                                        [Test]
                                        public async Task Test1() => await Assert.That(true).IsTrue();
                                    }
                                    """);

        var data = await project.BuildAndGetOutput();

        // MTP projects should NOT have Microsoft.NET.Test.Sdk
        var items = data.GetMsBuildItems("PackageReference");
        Assert.DoesNotContain(items,
            static i => i.Contains("Microsoft.NET.Test.Sdk", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task MTP_InjectsMTPExtensions()
    {
        // Use TUnit with base SDK - skip xunit injection to avoid conflicts
        await using var project = CreateProjectBuilder(SdkName);
        project.AddCsprojFile(
            filename: "Sample.Tests.csproj",
            properties: [("TargetFramework", "net10.0"), ("IsTestProject", "true"), ("SkipXunitInjection", "true")],
            nuGetPackages: [.. _tUnitPackages]
        );

        // TUnit 0.18+ requires await on assertions
        project.AddFile("Tests.cs", """
                                    using TUnit.Core;
                                    public class SampleTests
                                    {
                                        [Test]
                                        public async Task Test1() => await Assert.That(true).IsTrue();
                                    }
                                    """);

        var data = await project.BuildAndGetOutput();

        // MTP projects should have MTP extensions injected (except xunit.v3.mtp which has its own implementation)
        var items = data.GetMsBuildItems("PackageReference");
        Assert.Contains(items,
            static i => i.Contains("Microsoft.Testing.Extensions.CrashDump", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(items,
            static i => i.Contains("Microsoft.Testing.Extensions.TrxReport", StringComparison.OrdinalIgnoreCase));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SAFETY GUARD TESTS
    // ═══════════════════════════════════════════════════════════════════════

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

        // Should emit warning/error about Library OutputType
        // Either our SDK warning (ANCPSDK001) or xunit.v3's own error message
        Assert.True(
            data.OutputContains("ANCPSDK001") ||
            data.OutputContains("MTP is enabled") ||
            data.OutputContains("test projects must be executable"),
            "Should warn/error about MTP with Library OutputType");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // EXPLICIT OPT-IN VIA PROPERTY
    // ═══════════════════════════════════════════════════════════════════════

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

        // Explicit property should enable MTP
        data.AssertMsBuildPropertyValue("UseMicrosoftTestingPlatform", "true");
        data.AssertMsBuildPropertyValue("OutputType", "exe");
    }
}
