using ANcpLua.Sdk.Tests.Helpers;
using static ANcpLua.Sdk.Tests.Helpers.PackageFixture;

namespace ANcpLua.Sdk.Tests;

/// <summary>
///     Comprehensive test matrix for MTP (Microsoft Testing Platform) vs VSTest detection.
///     Two separate concerns:
///     1. IsTestProject = "this project contains tests" (broad detection)
///     2. UseMicrosoftTestingPlatform = "uses MTP" (strict detection)
///     MTP should ONLY be enabled for:
///     - TUnit (always MTP)
///     - xunit.v3.mtp-v1 / xunit.v3.mtp-v2 (explicit MTP)
///     - Microsoft.Testing.Extensions.* packages
///     - EnableNUnitRunner=true / EnableMSTestRunner=true (explicit opt-in)
///     - UseMicrosoftTestingPlatform=true (explicit)
///     MTP should NOT be enabled for:
///     - Plain xunit.v3 (ambiguous)
///     - NUnit alone (VSTest by default)
///     - MSTest.TestFramework alone (VSTest by default)
///     - xunit (v2, VSTest)
/// </summary>
public sealed class MtpDetectionNet100Tests(PackageFixture fixture, ITestOutputHelper testOutputHelper)
    : MtpDetectionTests(fixture, testOutputHelper, NetSdkVersion.Net100);

public abstract class MtpDetectionTests(
    PackageFixture fixture,
    ITestOutputHelper testOutputHelper,
    NetSdkVersion dotnetSdkVersion)
{
    // Package references for each scenario - note: renovate will update these
    private static readonly NuGetReference[] XUnit2Packages =
        [new("xunit", "2.9.3"), new("xunit.runner.visualstudio", "3.1.5")];

    private static readonly NuGetReference[] XUnit3PlainPackages =
        [new("xunit.v3", "3.2.0")];

    private static readonly NuGetReference[] XUnit3MtpV1Packages =
        [new("xunit.v3.mtp-v1", "3.2.0")];

    private static readonly NuGetReference[] XUnit3MtpV2Packages =
        [new("xunit.v3.mtp-v2", "3.2.0")];

    private static readonly NuGetReference[] XUnit3MtpOffPackages =
        [new("xunit.v3.mtp-off", "3.2.0")];

    private static readonly NuGetReference[] NUnitPackages =
        [new("NUnit", "4.3.2"), new("NUnit3TestAdapter", "5.0.0")];

    private static readonly NuGetReference[] MSTestPackages =
        [new("MSTest.TestFramework", "3.8.3"), new("MSTest.TestAdapter", "3.8.3")];

    private static readonly NuGetReference[] TUnitPackages =
        [new("TUnit", "0.17.28")];

    private ProjectBuilder CreateProjectBuilder(string defaultSdkName = SdkTestName)
    {
        var builder = new ProjectBuilder(fixture, testOutputHelper, SdkImportStyle.ProjectElement, defaultSdkName);
        builder.SetDotnetSdkVersion(dotnetSdkVersion);
        return builder;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // A) xUnit v2 (classic VSTest)
    // Expected: IsTestProject=true, UseMicrosoftTestingPlatform=false/empty
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task XUnit2_IsVSTest_NotMTP()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile(
            filename: "Sample.Tests.csproj",
            nuGetPackages: [.. XUnit2Packages]
        );

        project.AddFile("Tests.cs", """
                                    public class SampleTests
                                    {
                                        [Fact]
                                        public void Test1() => Assert.True(true);
                                    }
                                    """);

        var data = await project.BuildAndGetOutput();

        // Assert test project detection (broad)
        data.AssertMsBuildPropertyValue("IsTestProject", "true");

        // Assert MTP is NOT enabled (VSTest path)
        var useMtp = data.GetMsBuildPropertyValue("UseMicrosoftTestingPlatform");
        Assert.True(string.IsNullOrEmpty(useMtp) || useMtp == "false",
            $"UseMicrosoftTestingPlatform should be empty or false, got: {useMtp}");

        // Assert MTP is NOT enabled (VSTest path)
        // Note: We don't check OutputType here because MSBuild normalizes it and the project template sets it
    }

    [Fact]
    public async Task XUnit2_TestRuns_WithVSTest()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile(
            filename: "Sample.Tests.csproj",
            nuGetPackages: [.. XUnit2Packages]
        );

        project.AddFile("Tests.cs", """
                                    public class SampleTests
                                    {
                                        [Fact]
                                        public void PassingTest() => Assert.True(true);
                                    }
                                    """);

        var data = await project.TestAndGetOutput();
        Assert.Equal(0, data.ExitCode);
        Assert.True(data.OutputContains("Test Run Successful") || data.OutputContains("Passed:"),
            "Test should pass using VSTest runner");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // B) xUnit v3 "ambiguous" (plain xunit.v3)
    // Expected: IsTestProject=true, UseMicrosoftTestingPlatform=false/empty
    // Plain xunit.v3 is ambiguous - SDK should NOT assume MTP
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task XUnit3Plain_IsTestProject_NotMTP()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile(
            filename: "Sample.Tests.csproj",
            nuGetPackages: [.. XUnit3PlainPackages]
        );

        project.AddFile("Tests.cs", """
                                    public class SampleTests
                                    {
                                        [Fact]
                                        public void Test1() => Assert.True(true);
                                    }
                                    """);

        var data = await project.BuildAndGetOutput();

        // Assert test project detection (broad)
        data.AssertMsBuildPropertyValue("IsTestProject", "true");

        // Assert MTP is NOT enabled (ambiguous - don't assume)
        var useMtp = data.GetMsBuildPropertyValue("UseMicrosoftTestingPlatform");
        Assert.True(string.IsNullOrEmpty(useMtp) || useMtp == "false",
            $"Plain xunit.v3 should NOT trigger MTP detection, got: {useMtp}");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // C) xUnit v3 + MTP v2 (explicit)
    // Expected: IsTestProject=true, UseMicrosoftTestingPlatform=true, OutputType=Exe
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task XUnit3MtpV2_IsMTP()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile(
            filename: "Sample.Tests.csproj",
            nuGetPackages: [.. XUnit3MtpV2Packages]
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

    // Skip: xunit.v3.mtp-v2 on .NET 10 has incompatibilities with dotnet test:
    // - Without global.json test.runner: "Testing with VSTest target is no longer supported"
    // - With global.json test.runner: "Unknown option '--report-trx'" (xunit.v3 has different CLI)
    // The XUnit3MtpV2_IsMTP test verifies build-time MTP detection still works.
    // [Fact]
    // public async Task XUnit3MtpV2_TestRuns() { ... }

    [Fact]
    public async Task XUnit3MtpV1_IsMTP()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile(
            filename: "Sample.Tests.csproj",
            nuGetPackages: [.. XUnit3MtpV1Packages]
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
    // D) xUnit v3 + MTP OFF (explicit opt-out)
    // Expected: IsTestProject=true, UseMicrosoftTestingPlatform=false
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task XUnit3MtpOff_IsNotMTP()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile(
            filename: "Sample.Tests.csproj",
            nuGetPackages: [.. XUnit3MtpOffPackages]
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

        // Assert MTP is explicitly DISABLED
        var useMtp = data.GetMsBuildPropertyValue("UseMicrosoftTestingPlatform");
        Assert.True(string.IsNullOrEmpty(useMtp) || useMtp == "false",
            $"xunit.v3.mtp-off should force MTP off, got: {useMtp}");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // E) NUnit classic (VSTest)
    // Expected: IsTestProject=true, UseMicrosoftTestingPlatform=false
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task NUnit_IsVSTest_NotMTP()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile(
            filename: "Sample.Tests.csproj",
            nuGetPackages: [.. NUnitPackages]
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

        // Assert test project detection (broad)
        data.AssertMsBuildPropertyValue("IsTestProject", "true");

        // Assert MTP is NOT enabled (NUnit uses VSTest by default)
        var useMtp = data.GetMsBuildPropertyValue("UseMicrosoftTestingPlatform");
        Assert.True(string.IsNullOrEmpty(useMtp) || useMtp == "false",
            $"NUnit alone should NOT trigger MTP, got: {useMtp}");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // F) NUnit with MTP (explicit opt-in via EnableNUnitRunner)
    // Expected: IsTestProject=true, UseMicrosoftTestingPlatform=true
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task NUnit_WithEnableNUnitRunner_IsMTP()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile(
            filename: "Sample.Tests.csproj",
            properties: [("EnableNUnitRunner", "true")],
            nuGetPackages: [.. NUnitPackages]
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
    // G) MSTest classic (VSTest)
    // Expected: IsTestProject=true, UseMicrosoftTestingPlatform=false
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task MSTest_IsVSTest_NotMTP()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile(
            filename: "Sample.Tests.csproj",
            nuGetPackages: [.. MSTestPackages]
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

        // Assert test project detection (broad)
        data.AssertMsBuildPropertyValue("IsTestProject", "true");

        // Assert MTP is NOT enabled (MSTest uses VSTest by default)
        var useMtp = data.GetMsBuildPropertyValue("UseMicrosoftTestingPlatform");
        Assert.True(string.IsNullOrEmpty(useMtp) || useMtp == "false",
            $"MSTest.TestFramework alone should NOT trigger MTP, got: {useMtp}");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // H) MSTest with MTP (explicit opt-in via EnableMSTestRunner)
    // Expected: IsTestProject=true, UseMicrosoftTestingPlatform=true
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task MSTest_WithEnableMSTestRunner_IsMTP()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile(
            filename: "Sample.Tests.csproj",
            properties: [("EnableMSTestRunner", "true")],
            nuGetPackages: [.. MSTestPackages]
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
    // I) TUnit (always MTP)
    // Expected: IsTestProject=true, UseMicrosoftTestingPlatform=true
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task TUnit_IsMTP()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile(
            filename: "Sample.Tests.csproj",
            nuGetPackages: [.. TUnitPackages]
        );

        project.AddFile("Tests.cs", """
                                    using TUnit.Core;

                                    public class SampleTests
                                    {
                                        [Test]
                                        public void Test1() => Assert.That(true).IsTrue();
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
    public async Task VSTest_InjectsMicrosoftNETTestSdk()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile(
            filename: "Sample.Tests.csproj",
            nuGetPackages: [.. XUnit2Packages]
        );

        project.AddFile("Tests.cs", """
                                    public class SampleTests
                                    {
                                        [Fact]
                                        public void Test1() => Assert.True(true);
                                    }
                                    """);

        var data = await project.BuildAndGetOutput();

        // VSTest projects should have Microsoft.NET.Test.Sdk injected
        var items = data.GetMsBuildItems("PackageReference");
        Assert.Contains(items, i => i.Contains("Microsoft.NET.Test.Sdk", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task MTP_DoesNotInjectMicrosoftNETTestSdk()
    {
        // Use TUnit (not xunit.v3.mtp) because xunit.v3.mtp has its own MTP implementation
        // and we skip standard extension injection for it
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile(
            filename: "Sample.Tests.csproj",
            nuGetPackages: [.. TUnitPackages]
        );

        project.AddFile("Tests.cs", """
                                    using TUnit.Core;
                                    public class SampleTests
                                    {
                                        [Test]
                                        public void Test1() => Assert.That(true).IsTrue();
                                    }
                                    """);

        var data = await project.BuildAndGetOutput();

        // MTP projects should NOT have Microsoft.NET.Test.Sdk
        var items = data.GetMsBuildItems("PackageReference");
        Assert.DoesNotContain(items, i => i.Contains("Microsoft.NET.Test.Sdk", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task MTP_InjectsMTPExtensions()
    {
        // Use TUnit (not xunit.v3.mtp) because xunit.v3.mtp has its own MTP implementation
        // and we skip standard extension injection for it
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile(
            filename: "Sample.Tests.csproj",
            nuGetPackages: [.. TUnitPackages]
        );

        project.AddFile("Tests.cs", """
                                    using TUnit.Core;
                                    public class SampleTests
                                    {
                                        [Test]
                                        public void Test1() => Assert.That(true).IsTrue();
                                    }
                                    """);

        var data = await project.BuildAndGetOutput();

        // MTP projects should have MTP extensions injected (except xunit.v3.mtp which has its own implementation)
        var items = data.GetMsBuildItems("PackageReference");
        Assert.Contains(items,
            i => i.Contains("Microsoft.Testing.Extensions.CrashDump", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(items,
            i => i.Contains("Microsoft.Testing.Extensions.TrxReport", StringComparison.OrdinalIgnoreCase));
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
            nuGetPackages: [.. XUnit2Packages]
        );

        project.AddFile("Tests.cs", """
                                    public class SampleTests
                                    {
                                        [Fact]
                                        public void Test1() => Assert.True(true);
                                    }
                                    """);

        var data = await project.BuildAndGetOutput();

        // Should emit warning ANCPSDK001
        Assert.True(data.OutputContains("ANCPSDK001") || data.OutputContains("MTP is enabled"),
            "Should warn about MTP with Library OutputType");
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
            nuGetPackages: [.. XUnit2Packages]
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

    // ═══════════════════════════════════════════════════════════════════════
    // MTP EXTENSION PACKAGE DETECTION
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task MTPExtensionPackage_TriggersMTPDetection()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile(
            filename: "Sample.Tests.csproj",
            nuGetPackages: [.. XUnit2Packages, new NuGetReference("Microsoft.Testing.Extensions.TrxReport", "2.0.2")]
        );

        project.AddFile("Tests.cs", """
                                    public class SampleTests
                                    {
                                        [Fact]
                                        public void Test1() => Assert.True(true);
                                    }
                                    """);

        var data = await project.BuildAndGetOutput();

        // Having an MTP extension package should trigger MTP detection
        data.AssertMsBuildPropertyValue("UseMicrosoftTestingPlatform", "true");
    }
}