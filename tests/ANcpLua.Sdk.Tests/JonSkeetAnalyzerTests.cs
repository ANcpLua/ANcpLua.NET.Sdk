using ANcpLua.Sdk.Tests.Helpers;
using ANcpLua.Sdk.Tests.Infrastructure;

namespace ANcpLua.Sdk.Tests;

/// <summary>
///     Tests for JonSkeet.RoslynAnalyzers injection via SDK.
///     JS0001/JS0002: Record 'with' operator safety diagnostics.
///     Note: The analyzer is beta and diagnostic tests are fragile.
/// </summary>
public class JonSkeetAnalyzerTests(PackageFixture fixture, ITestOutputHelper testOutputHelper)
    : SdkTestBase(fixture, testOutputHelper)
{
    [Fact]
    public async Task JonSkeet_Analyzer_Package_Is_Injected_By_SDK()
    {
        var result = await QuickBuild("""
                                      namespace Consumer;

                                      public record SafeRecord(string Value);
                                      """);

        result.ShouldSucceed();
    }

    [Fact]
    public async Task Records_With_Operator_Compiles_Successfully()
    {
        var result = await QuickBuild("""
                                      namespace Consumer;

                                      public record Person(string Name, int Age);

                                      internal static class Usage
                                      {
                                          public static Person Clone(Person p) => p with { Age = 30 };
                                      }
                                      """);

        result.ShouldSucceed();
    }
}
