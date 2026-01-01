// ANcpLua.Sdk.Canary - Fast SDK Validation Tests
// Run time: <10 seconds | Catches: packaging errors, import failures, MTP issues

#if NET10_0_OR_GREATER

using Microsoft.Shared.Diagnostics;

namespace ANcpLua.Sdk.Canary;

/// <summary>
///     Fast validation tests that catch SDK packaging errors before CI.
///     If these pass locally, the SDK package structure is correct.
/// </summary>
public class SdkStructureTests
{
    [Fact]
    public void Sdk_Props_Imported() =>
        // If we reach here, SDK .props imported without MSB4019/MSB4057
        true.Should().BeTrue();

    [Fact]
    public void Sdk_Targets_Imported() =>
        // If we reach here, SDK .targets imported without errors
        true.Should().BeTrue();

    [Fact]
    public void MTP_OutputType_IsExe() =>
        // MTP requires Exe - if test runs, OutputType is correct
        // (Library would fail: "No test host process")
        true.Should().BeTrue();
}

/// <summary>
///     Validates MTP detection and package injection.
/// </summary>
public class MtpDetectionTests
{
    [Fact]
    public void XunitV3MtpV2_Detected() =>
        // xunit.v3.mtp-v2 should trigger UseMicrosoftTestingPlatform=true
        // Evidence: this test is running (MTP host works)
        true.Should().BeTrue();

    [Fact]
    public void AwesomeAssertions_Available()
    {
        // Validates AwesomeAssertions package injection
        var value = 42;
        value.Should().Be(42);
        value.Should().BeGreaterThan(0);
        value.Should().BeLessThan(100);
    }
}

/// <summary>
///     Validates C# language features work correctly.
/// </summary>
public class LanguageFeatureTests
{
    [Fact]
    public void CSharp14_PrimaryConstructors()
    {
        // C# 12+ primary constructor syntax
        var record = new TestRecord("test");
        record.Value.Should().Be("test");
    }

    [Fact]
    public void CSharp14_CollectionExpressions()
    {
        // C# 12+ collection expressions
        int[] numbers = [1, 2, 3];
        numbers.Should().HaveCount(3);
    }

    [Fact]
    public void CSharp14_PatternMatching()
    {
        // C# 11+ list patterns
        int[] arr = [1, 2, 3];
        var result = arr switch
        {
            [1, 2, 3] => "matched",
            _ => "no match"
        };
        result.Should().Be("matched");
    }

    private record TestRecord(string Value);
}

/// <summary>
///     Validates Throw helpers are injected.
/// </summary>
public class ThrowHelperTests
{
    [Fact]
    public void Throw_IfNull_Works()
    {
        var value = "not null";
        // This should NOT throw
        var action = () => Throw.IfNull(value);
        action.Should().NotThrow();
    }

    [Fact]
    public void Throw_IfNull_ThrowsOnNull()
    {
        string? value = null;
        var action = () => Throw.IfNull(value);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Throw_IfNullOrEmpty_Works()
    {
        var action = () => Throw.IfNullOrEmpty("valid");
        action.Should().NotThrow();
    }
}

#endif
