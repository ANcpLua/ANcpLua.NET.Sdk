using ANcpLua.Sdk.Tests.Helpers;
using ANcpLua.Sdk.Tests.Infrastructure;

namespace ANcpLua.Sdk.Tests;

public class BannedApiTests(PackageFixture fixture, ITestOutputHelper testOutputHelper)
{
    private const string BannedApiDiagnosticId = "RS0030";

    [Fact]
    public async Task DateTime_Now_Reports_Warning()
    {
        await using var project =
            new ProjectBuilder(fixture, testOutputHelper, SdkImportStyle.SdkElement, PackageFixture.SdkName);

        project.AddCsprojFile([
            (MsBuildProperties.TargetFramework, TargetFrameworks.Net100),
            (MsBuildProperties.OutputType, MsBuildValues.Library)
        ]);

        project.AddFile("DateTimeUsage.cs", """
                                            using System;
                                            namespace Consumer;

                                            internal class DateTimeUsage
                                            {
                                                public DateTime GetTime() => DateTime.Now;
                                            }
                                            """);

        var result = await project.BuildAndGetOutput();

        Assert.True(result.HasWarning(BannedApiDiagnosticId),
            $"Expected {BannedApiDiagnosticId} warning for DateTime.Now usage. Output: {result.ProcessOutput}");
    }

    [Fact]
    public async Task ArgumentNullException_ThrowIfNull_Reports_Warning()
    {
        await using var project =
            new ProjectBuilder(fixture, testOutputHelper, SdkImportStyle.SdkElement, PackageFixture.SdkName);

        project.AddCsprojFile([
            (MsBuildProperties.TargetFramework, TargetFrameworks.Net100),
            (MsBuildProperties.OutputType, MsBuildValues.Library)
        ]);

        project.AddFile("ArgumentNullUsage.cs", """
                                                using System;
                                                namespace Consumer;

                                                internal class ArgumentNullUsage
                                                {
                                                    public void Check(object o)
                                                    {
                                                        ArgumentNullException.ThrowIfNull(o);
                                                    }
                                                }
                                                """);

        var result = await project.BuildAndGetOutput();

        Assert.True(result.HasWarning(BannedApiDiagnosticId),
            $"Expected {BannedApiDiagnosticId} warning for ArgumentNullException.ThrowIfNull usage. Output: {result.ProcessOutput}");
    }

    [Fact]
    public async Task Enumerable_Any_WithPredicate_Reports_Warning()
    {
        await using var project =
            new ProjectBuilder(fixture, testOutputHelper, SdkImportStyle.SdkElement, PackageFixture.SdkName);

        project.AddCsprojFile([
            (MsBuildProperties.TargetFramework, TargetFrameworks.Net100),
            (MsBuildProperties.OutputType, MsBuildValues.Library)
        ]);

        project.AddFile("EnumerableUsage.cs", """
                                              using System;
                                              using System.Collections.Generic;
                                              using System.Linq;

                                              namespace Consumer;

                                              internal class EnumerableUsage
                                              {
                                                  public bool Check(List<int> list)
                                                  {
                                                      return list.Any(x => x > 5);
                                                  }
                                              }
                                              """);

        var result = await project.BuildAndGetOutput();

        Assert.True(result.HasWarning(BannedApiDiagnosticId),
            $"Expected {BannedApiDiagnosticId} warning for Enumerable.Any(predicate) usage. Output: {result.ProcessOutput}");
    }

    [Fact]
    public async Task Enumerable_FirstOrDefault_WithPredicate_Reports_Warning()
    {
        await using var project =
            new ProjectBuilder(fixture, testOutputHelper, SdkImportStyle.SdkElement, PackageFixture.SdkName);

        project.AddCsprojFile([
            (MsBuildProperties.TargetFramework, TargetFrameworks.Net100),
            (MsBuildProperties.OutputType, MsBuildValues.Library)
        ]);

        project.AddFile("FirstOrDefaultUsage.cs", """
                                                  using System;
                                                  using System.Collections.Generic;
                                                  using System.Linq;

                                                  namespace Consumer;

                                                  internal class FirstOrDefaultUsage
                                                  {
                                                      public int GetFirst(List<int> list)
                                                      {
                                                          return list.FirstOrDefault(x => x > 5);
                                                      }
                                                  }
                                                  """);

        var result = await project.BuildAndGetOutput();

        Assert.True(result.HasWarning(BannedApiDiagnosticId),
            $"Expected {BannedApiDiagnosticId} warning for Enumerable.FirstOrDefault(predicate) usage. Output: {result.ProcessOutput}");
    }

    [Fact]
    public async Task List_Exists_Does_Not_Report_Warning()
    {
        await using var project =
            new ProjectBuilder(fixture, testOutputHelper, SdkImportStyle.SdkElement, PackageFixture.SdkName);

        project.AddCsprojFile([
            (MsBuildProperties.TargetFramework, TargetFrameworks.Net100),
            (MsBuildProperties.OutputType, MsBuildValues.Library)
        ]);

        project.AddFile("ListExistsUsage.cs", """
                                              using System;
                                              using System.Collections.Generic;

                                              namespace Consumer;

                                              internal class ListExistsUsage
                                              {
                                                  public bool Check(List<int> list)
                                                  {
                                                      // Recommended alternative to Any(predicate) for List<T>
                                                      return list.Exists(x => x > 5);
                                                  }
                                              }
                                              """);

        var result = await project.BuildAndGetOutput();

        Assert.False(result.HasWarning(BannedApiDiagnosticId),
            $"Did not expect {BannedApiDiagnosticId} warning for List.Exists usage. Output: {result.ProcessOutput}");
    }
}