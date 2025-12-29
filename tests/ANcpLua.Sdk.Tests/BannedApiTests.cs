using ANcpLua.Sdk.Tests.Helpers;
using ANcpLua.Sdk.Tests.Infrastructure;

namespace ANcpLua.Sdk.Tests;

public class BannedApiTests(PackageFixture fixture, ITestOutputHelper testOutputHelper)
{
    private const string BannedApiDiagnosticId = "RS0030";

    [Fact]
    public async Task ArgumentNullException_ThrowIfNull_Is_Banned()
    {
        await using var project =
            new ProjectBuilder(fixture, testOutputHelper, SdkImportStyle.SdkElement, PackageFixture.SdkName);

        project.AddCsprojFile([
            (MsBuildProperties.TargetFramework, TargetFrameworks.Net100),
            (MsBuildProperties.OutputType, MsBuildValues.Library)
        ]);

        project.AddFile("BannedUsage.cs", """
                                          namespace Consumer;

                                          internal class BannedUsage
                                          {
                                              public void Validate(object? value)
                                              {
                                                  ArgumentNullException.ThrowIfNull(value);
                                              }
                                          }
                                          """);

        var result = await project.BuildAndGetOutput();

        Assert.True(result.HasWarning(BannedApiDiagnosticId),
            $"Expected {BannedApiDiagnosticId} warning for ArgumentNullException.ThrowIfNull. Output: {result.ProcessOutput}");
    }

    [Fact]
    public async Task ArgumentException_ThrowIfNullOrWhiteSpace_Is_Banned()
    {
        await using var project =
            new ProjectBuilder(fixture, testOutputHelper, SdkImportStyle.SdkElement, PackageFixture.SdkName);

        project.AddCsprojFile([
            (MsBuildProperties.TargetFramework, TargetFrameworks.Net100),
            (MsBuildProperties.OutputType, MsBuildValues.Library)
        ]);

        project.AddFile("BannedUsage.cs", """
                                          namespace Consumer;

                                          internal class BannedUsage
                                          {
                                              public void Validate(string? value)
                                              {
                                                  ArgumentException.ThrowIfNullOrWhiteSpace(value);
                                              }
                                          }
                                          """);

        var result = await project.BuildAndGetOutput();

        Assert.True(result.HasWarning(BannedApiDiagnosticId),
            $"Expected {BannedApiDiagnosticId} warning for ArgumentException.ThrowIfNullOrWhiteSpace. Output: {result.ProcessOutput}");
    }

    [Fact]
    public async Task DateTime_Now_Is_Banned()
    {
        await using var project =
            new ProjectBuilder(fixture, testOutputHelper, SdkImportStyle.SdkElement, PackageFixture.SdkName);

        project.AddCsprojFile([
            (MsBuildProperties.TargetFramework, TargetFrameworks.Net100),
            (MsBuildProperties.OutputType, MsBuildValues.Library)
        ]);

        project.AddFile("BannedUsage.cs", """
                                          using System;
                                          namespace Consumer;

                                          internal class BannedUsage
                                          {
                                              public DateTime GetTime() => DateTime.Now;
                                          }
                                          """);

        var result = await project.BuildAndGetOutput();

        Assert.True(result.HasWarning(BannedApiDiagnosticId),
            $"Expected {BannedApiDiagnosticId} warning for DateTime.Now. Output: {result.ProcessOutput}");
    }

    [Fact]
    public async Task Enumerable_Any_With_Predicate_Is_Banned()
    {
        await using var project =
            new ProjectBuilder(fixture, testOutputHelper, SdkImportStyle.SdkElement, PackageFixture.SdkName);

        project.AddCsprojFile([
            (MsBuildProperties.TargetFramework, TargetFrameworks.Net100),
            (MsBuildProperties.OutputType, MsBuildValues.Library)
        ]);

        project.AddFile("BannedUsage.cs", """
                                          using System.Collections.Generic;
                                          using System.Linq;
                                          namespace Consumer;

                                          internal class BannedUsage
                                          {
                                              public bool HasEven(List<int> numbers) => numbers.Any(n => n % 2 == 0);
                                          }
                                          """);

        var result = await project.BuildAndGetOutput();

        Assert.True(result.HasWarning(BannedApiDiagnosticId),
            $"Expected {BannedApiDiagnosticId} warning for Enumerable.Any with predicate. Output: {result.ProcessOutput}");
    }

    [Fact]
    public async Task Enumerable_FirstOrDefault_With_Predicate_Is_Banned()
    {
        await using var project =
            new ProjectBuilder(fixture, testOutputHelper, SdkImportStyle.SdkElement, PackageFixture.SdkName);

        project.AddCsprojFile([
            (MsBuildProperties.TargetFramework, TargetFrameworks.Net100),
            (MsBuildProperties.OutputType, MsBuildValues.Library)
        ]);

        project.AddFile("BannedUsage.cs", """
                                          using System.Collections.Generic;
                                          using System.Linq;
                                          namespace Consumer;

                                          internal class BannedUsage
                                          {
                                              public int? FindEven(List<int> numbers) => numbers.FirstOrDefault(n => n % 2 == 0);
                                          }
                                          """);

        var result = await project.BuildAndGetOutput();

        Assert.True(result.HasWarning(BannedApiDiagnosticId),
            $"Expected {BannedApiDiagnosticId} warning for Enumerable.FirstOrDefault with predicate. Output: {result.ProcessOutput}");
    }

    [Fact]
    public async Task List_Exists_Is_Not_Banned()
    {
        await using var project =
            new ProjectBuilder(fixture, testOutputHelper, SdkImportStyle.SdkElement, PackageFixture.SdkName);

        project.AddCsprojFile([
            (MsBuildProperties.TargetFramework, TargetFrameworks.Net100),
            (MsBuildProperties.OutputType, MsBuildValues.Library)
        ]);

        project.AddFile("AllowedUsage.cs", """
                                           using System.Collections.Generic;
                                           namespace Consumer;

                                           internal class AllowedUsage
                                           {
                                               public bool HasEven(List<int> numbers) => numbers.Exists(n => n % 2 == 0);
                                           }
                                           """);

        var result = await project.BuildAndGetOutput();

        Assert.False(result.HasWarning(BannedApiDiagnosticId),
            $"Did not expect {BannedApiDiagnosticId} warning for List.Exists. Output: {result.ProcessOutput}");
    }

    [Fact]
    public async Task List_Find_Is_Not_Banned()
    {
        await using var project =
            new ProjectBuilder(fixture, testOutputHelper, SdkImportStyle.SdkElement, PackageFixture.SdkName);

        project.AddCsprojFile([
            (MsBuildProperties.TargetFramework, TargetFrameworks.Net100),
            (MsBuildProperties.OutputType, MsBuildValues.Library)
        ]);

        project.AddFile("AllowedUsage.cs", """
                                           using System.Collections.Generic;
                                           namespace Consumer;

                                           internal class AllowedUsage
                                           {
                                               public int FindEven(List<int> numbers) => numbers.Find(n => n % 2 == 0);
                                           }
                                           """);

        var result = await project.BuildAndGetOutput();

        Assert.False(result.HasWarning(BannedApiDiagnosticId),
            $"Did not expect {BannedApiDiagnosticId} warning for List.Find. Output: {result.ProcessOutput}");
    }

    [Fact]
    public async Task TimeProvider_GetUtcNow_Is_Not_Banned()
    {
        await using var project =
            new ProjectBuilder(fixture, testOutputHelper, SdkImportStyle.SdkElement, PackageFixture.SdkName);

        project.AddCsprojFile([
            (MsBuildProperties.TargetFramework, TargetFrameworks.Net100),
            (MsBuildProperties.OutputType, MsBuildValues.Library)
        ]);

        project.AddFile("AllowedUsage.cs", """
                                           using System;
                                           namespace Consumer;

                                           internal class AllowedUsage
                                           {
                                               public DateTimeOffset GetTime() => TimeProvider.System.GetUtcNow();
                                           }
                                           """);

        var result = await project.BuildAndGetOutput();

        Assert.False(result.HasWarning(BannedApiDiagnosticId),
            $"Did not expect {BannedApiDiagnosticId} warning for TimeProvider.System.GetUtcNow(). Output: {result.ProcessOutput}");
    }
}