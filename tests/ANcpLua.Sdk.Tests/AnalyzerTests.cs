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
    [InlineData("gen_ai.system", "gen_ai.provider.name")] // v1.37.0
    [InlineData("gen_ai.usage.prompt_tokens", "gen_ai.usage.input_tokens")] // v1.27.0
    [InlineData("http.method", "http.request.method")] // v1.21.0
    [InlineData("db.statement", "db.query.text")] // v1.25.0
    [InlineData("code.function", "code.function.name")] // v1.30.0
    public async Task QYL0002_DeprecatedAttribute_In_TelemetryContext_Reports_Warning(string deprecatedAttribute, string _)
    {
        await using var project =
            new ProjectBuilder(fixture, testOutputHelper, SdkImportStyle.SdkElement, PackageFixture.SdkName);

        project.AddCsprojFile([
            (MsBuildProperties.TargetFramework, TargetFrameworks.Net100),
            (MsBuildProperties.OutputType, MsBuildValues.Library)
        ]);

        // Test with indexer access on "attributes" dict - triggers telemetry context detection
        project.AddFile("DeprecatedAttribute.cs", $$"""
                                                    using System.Collections.Generic;

                                                    namespace Consumer;

                                                    internal class DeprecatedAttributeUsage
                                                    {
                                                        public void SetTelemetry()
                                                        {
                                                            var attributes = new Dictionary<string, object>();
                                                            attributes["{{deprecatedAttribute}}"] = "value";
                                                        }
                                                    }
                                                    """);

        var result = await project.BuildAndGetOutput();

        Assert.True(result.HasWarning("QYL0002"),
            $"Expected QYL0002 warning for deprecated attribute '{deprecatedAttribute}'. Output: {result.ProcessOutput}");
    }

    [Theory]
    [InlineData("gen_ai.provider.name")] // current GenAI
    [InlineData("http.request.method")] // current HTTP
    [InlineData("db.query.text")] // current DB
    [InlineData("code.function.name")] // current Code
    public async Task QYL0002_CurrentAttribute_No_Warning(string currentAttribute)
    {
        await using var project =
            new ProjectBuilder(fixture, testOutputHelper, SdkImportStyle.SdkElement, PackageFixture.SdkName);

        project.AddCsprojFile([
            (MsBuildProperties.TargetFramework, TargetFrameworks.Net100),
            (MsBuildProperties.OutputType, MsBuildValues.Library)
        ]);

        project.AddFile("CurrentAttribute.cs", $$"""
                                                 using System.Collections.Generic;

                                                 namespace Consumer;

                                                 internal class CurrentAttributeUsage
                                                 {
                                                     public void SetTelemetry()
                                                     {
                                                         var attributes = new Dictionary<string, object>();
                                                         attributes["{{currentAttribute}}"] = "value";
                                                     }
                                                 }
                                                 """);

        var result = await project.BuildAndGetOutput();

        Assert.False(result.HasWarning("QYL0002"),
            $"Did not expect QYL0002 warning for current attribute '{currentAttribute}'. Output: {result.ProcessOutput}");
    }

    [Fact]
    public async Task QYL0002_DeprecatedAttribute_Outside_TelemetryContext_No_Warning()
    {
        await using var project =
            new ProjectBuilder(fixture, testOutputHelper, SdkImportStyle.SdkElement, PackageFixture.SdkName);

        project.AddCsprojFile([
            (MsBuildProperties.TargetFramework, TargetFrameworks.Net100),
            (MsBuildProperties.OutputType, MsBuildValues.Library)
        ]);

        // Deprecated attribute name used outside telemetry context should NOT warn
        project.AddFile("NotTelemetry.cs", """
                                           namespace Consumer;

                                           internal class NotTelemetry
                                           {
                                               // Just a plain string, not in telemetry context
                                               public string GetValue() => "http.method";
                                           }
                                           """);

        var result = await project.BuildAndGetOutput();

        Assert.False(result.HasWarning("QYL0002"),
            $"Did not expect QYL0002 warning outside telemetry context. Output: {result.ProcessOutput}");
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

    [Fact]
    public async Task QYL0003_MissingSchemaUrl_Reports_Info()
    {
        await using var project =
            new ProjectBuilder(fixture, testOutputHelper, SdkImportStyle.SdkElement, PackageFixture.SdkName);

        project.AddCsprojFile([
            (MsBuildProperties.TargetFramework, TargetFrameworks.Net100),
            (MsBuildProperties.OutputType, MsBuildValues.Library)
        ], nuGetPackages: [
            new NuGetReference("OpenTelemetry", "1.10.0"),
            new NuGetReference("OpenTelemetry.Extensions.Hosting", "1.10.0")
        ]);

        project.AddFile("MissingSchemaUrl.cs", """
                                               using OpenTelemetry.Resources;

                                               namespace Consumer;

                                               internal static class OtelConfig
                                               {
                                                   public static void Configure(ResourceBuilder builder)
                                                   {
                                                       // Missing telemetry.schema_url
                                                       builder.ConfigureResource(r => r
                                                           .AddService("my-service"));
                                                   }
                                               }
                                               """);

        var result = await project.BuildAndGetOutput();

        // QYL0003 is Info level, not Warning
        Assert.True(result.HasInfo("QYL0003") || result.HasWarning("QYL0003"),
            $"Expected QYL0003 info/warning for missing schema URL. Output: {result.ProcessOutput}");
    }

    [Fact]
    public async Task QYL0003_WithSchemaUrl_No_Warning()
    {
        await using var project =
            new ProjectBuilder(fixture, testOutputHelper, SdkImportStyle.SdkElement, PackageFixture.SdkName);

        project.AddCsprojFile([
            (MsBuildProperties.TargetFramework, TargetFrameworks.Net100),
            (MsBuildProperties.OutputType, MsBuildValues.Library)
        ], nuGetPackages: [
            new NuGetReference("OpenTelemetry", "1.10.0"),
            new NuGetReference("OpenTelemetry.Extensions.Hosting", "1.10.0")
        ]);

        project.AddFile("WithSchemaUrl.cs", """
                                            using OpenTelemetry.Resources;

                                            namespace Consumer;

                                            internal static class OtelConfigWithSchema
                                            {
                                                public static void Configure(ResourceBuilder builder)
                                                {
                                                    // Has schema URL
                                                    builder.ConfigureResource(r => r
                                                        .AddService("my-service")
                                                        .AddAttributes([
                                                            new("telemetry.schema_url", "https://opentelemetry.io/schemas/1.25.0")
                                                        ]));
                                                }
                                            }
                                            """);

        var result = await project.BuildAndGetOutput();

        Assert.False(result.HasWarning("QYL0003"),
            $"Did not expect QYL0003 warning when schema URL is set. Output: {result.ProcessOutput}");
    }
}
