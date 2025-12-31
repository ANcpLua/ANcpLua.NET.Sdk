// ANcpLua.Sdk.Canary - Validates SDK packaging is correct
// If this compiles, the SDK's .props/.targets are importing correctly

using Xunit;
using AwesomeAssertions;

namespace ANcpLua.Sdk.Canary;

/// <summary>
/// These tests validate that the SDK package structure is correct.
/// They should run in &lt;10 seconds and catch packaging errors before full CI.
/// </summary>
public class SdkValidationTests
{
    [Fact]
    public void SdkProps_ImportedSuccessfully()
    {
        // If we got here, the SDK .props imported without MSB4019/MSB4057
        true.Should().BeTrue();
    }

    [Fact]
    public void MtpOutputType_IsExe()
    {
        // MTP requires OutputType=Exe - if this test runs, it's correct
        // (Library OutputType would fail to produce test host)
        true.Should().BeTrue();
    }

    [Fact]
    public void LangVersion_SupportsModernSyntax()
    {
        // Validates LangVersion is correctly set
        // This uses C# 12+ primary constructor syntax
        var record = new TestRecord("value");
        record.Value.Should().Be("value");
    }

    private record TestRecord(string Value);
}
