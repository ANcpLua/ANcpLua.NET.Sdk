using ANcpLua.Sdk.Tests.Helpers;
using ANcpLua.Sdk.Tests.Infrastructure;

namespace ANcpLua.Sdk.Tests;

public class AnalyzerTests(PackageFixture fixture, ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task QYL0001_LockKeyword_Reports_Warning()
    {
        await using var project =
            new ProjectBuilder(fixture, testOutputHelper, SdkImportStyle.SdkElement, PackageFixture.SdkName);

        project.AddCsprojFile([
            (MsBuildProperties.TargetFramework, TargetFrameworks.Net100),
            (MsBuildProperties.OutputType, MsBuildValues.Library)
        ]);

        project.AddFile("LockUsage.cs", """
                                        namespace Consumer;

                                        internal class LockUsage
                                        {
                                            private readonly object _syncRoot = new();

                                            public void DoWork()
                                            {
                                                lock (_syncRoot)
                                                {
                                                    // some work
                                                }
                                            }
                                        }
                                        """);

        var result = await project.BuildAndGetOutput();

        Assert.True(result.HasWarning("QYL0001"),
            $"Expected QYL0001 warning for lock keyword usage. Output: {result.ProcessOutput}");
    }

    [Fact]
    public async Task QYL0001_LockKeyword_No_Warning_When_Not_Used()
    {
        await using var project =
            new ProjectBuilder(fixture, testOutputHelper, SdkImportStyle.SdkElement, PackageFixture.SdkName);

        project.AddCsprojFile([
            (MsBuildProperties.TargetFramework, TargetFrameworks.Net100),
            (MsBuildProperties.OutputType, MsBuildValues.Library)
        ]);

        project.AddFile("NoLockUsage.cs", """
                                          namespace Consumer;

                                          internal class NoLockUsage
                                          {
                                              private readonly System.Threading.Lock _lock = new();

                                              public void DoWork()
                                              {
                                                  using (_lock.EnterScope())
                                                  {
                                                      // some work
                                                  }
                                              }
                                          }
                                          """);

        var result = await project.BuildAndGetOutput();

        Assert.False(result.HasWarning("QYL0001"),
            $"Did not expect QYL0001 warning when using Lock.EnterScope(). Output: {result.ProcessOutput}");
    }

    [Theory]
    [InlineData("gen_ai.system", "gen_ai.provider.name")]
    [InlineData("gen_ai.usage.prompt_tokens", "gen_ai.usage.input_tokens")]
    [InlineData("gen_ai.usage.completion_tokens", "gen_ai.usage.output_tokens")]
    [InlineData("gen_ai.request.max_tokens", "gen_ai.request.max_output_tokens")]
    public async Task QYL0002_DeprecatedGenAiAttribute_Reports_Warning(string deprecatedAttribute, string _)
    {
        await using var project =
            new ProjectBuilder(fixture, testOutputHelper, SdkImportStyle.SdkElement, PackageFixture.SdkName);

        project.AddCsprojFile([
            (MsBuildProperties.TargetFramework, TargetFrameworks.Net100),
            (MsBuildProperties.OutputType, MsBuildValues.Library)
        ]);

        project.AddFile("DeprecatedAttribute.cs", $$"""
                                                    namespace Consumer;

                                                    internal class DeprecatedAttribute
                                                    {
                                                        public string GetAttribute() => "{{deprecatedAttribute}}";
                                                    }
                                                    """);

        var result = await project.BuildAndGetOutput();

        Assert.True(result.HasWarning("QYL0002"),
            $"Expected QYL0002 warning for deprecated attribute '{deprecatedAttribute}'. Output: {result.ProcessOutput}");
    }

    [Theory]
    [InlineData("gen_ai.provider.name")]
    [InlineData("gen_ai.usage.input_tokens")]
    [InlineData("gen_ai.usage.output_tokens")]
    [InlineData("gen_ai.request.max_output_tokens")]
    public async Task QYL0002_CurrentGenAiAttribute_No_Warning(string currentAttribute)
    {
        await using var project =
            new ProjectBuilder(fixture, testOutputHelper, SdkImportStyle.SdkElement, PackageFixture.SdkName);

        project.AddCsprojFile([
            (MsBuildProperties.TargetFramework, TargetFrameworks.Net100),
            (MsBuildProperties.OutputType, MsBuildValues.Library)
        ]);

        project.AddFile("CurrentAttribute.cs", $$"""
                                                 namespace Consumer;

                                                 internal class CurrentAttribute
                                                 {
                                                     public string GetAttribute() => "{{currentAttribute}}";
                                                 }
                                                 """);

        var result = await project.BuildAndGetOutput();

        Assert.False(result.HasWarning("QYL0002"),
            $"Did not expect QYL0002 warning for current attribute '{currentAttribute}'. Output: {result.ProcessOutput}");
    }

    [Fact]
    public async Task QYL0002_UnrelatedString_No_Warning()
    {
        await using var project =
            new ProjectBuilder(fixture, testOutputHelper, SdkImportStyle.SdkElement, PackageFixture.SdkName);

        project.AddCsprojFile([
            (MsBuildProperties.TargetFramework, TargetFrameworks.Net100),
            (MsBuildProperties.OutputType, MsBuildValues.Library)
        ]);

        project.AddFile("UnrelatedString.cs", """
                                              namespace Consumer;

                                              internal class UnrelatedString
                                              {
                                                  public string GetValue() => "hello world";
                                              }
                                              """);

        var result = await project.BuildAndGetOutput();

        Assert.False(result.HasWarning("QYL0002"),
            $"Did not expect QYL0002 warning for unrelated string. Output: {result.ProcessOutput}");
    }
}