using ANcpLua.Sdk.Tests.Helpers;
using ANcpLua.Sdk.Tests.Infrastructure;

namespace ANcpLua.Sdk.Tests;

public class BannedApiTests(PackageFixture fixture, ITestOutputHelper testOutputHelper)
    : SdkTestBase(fixture, testOutputHelper)
{
    private const string RS0030 = "RS0030";

    [Fact]
    public async Task ArgumentNullException_ThrowIfNull_Is_Banned()
    {
        var result = await QuickBuild("""
                                      namespace Consumer;
                                      internal class BannedUsage
                                      {
                                          public void Validate(object? value)
                                          {
                                              ArgumentNullException.ThrowIfNull(value);
                                          }
                                      }
                                      """);

        result.ShouldSucceed().ShouldHaveWarning(RS0030);
    }

    [Fact]
    public async Task ArgumentException_ThrowIfNullOrWhiteSpace_Is_Banned()
    {
        var result = await QuickBuild("""
                                      namespace Consumer;
                                      internal class BannedUsage
                                      {
                                          public void Validate(string? value)
                                          {
                                              ArgumentException.ThrowIfNullOrWhiteSpace(value);
                                          }
                                      }
                                      """);

        result.ShouldSucceed().ShouldHaveWarning(RS0030);
    }

    [Fact]
    public async Task DateTime_Now_Is_Banned()
    {
        var result = await QuickBuild("""
                                      using System;
                                      namespace Consumer;
                                      internal class BannedUsage
                                      {
                                          public DateTime GetTime() => DateTime.Now;
                                      }
                                      """);

        result.ShouldSucceed().ShouldHaveWarning(RS0030);
    }

    // NOTE: Enumerable.Any() and Enumerable.FirstOrDefault() with predicates were previously banned
    // to encourage using List<T>.Exists/Find, but this broke compatibility with xunit.v3 which
    // uses Any() in its auto-generated entry point. See BannedSymbols.txt for details.

    [Fact]
    public async Task Enumerable_Any_With_Predicate_Is_Not_Banned()
    {
        // Any() with predicate is allowed since xunit.v3 source generator uses it
        var result = await QuickBuild("""
                                      using System.Collections.Generic;
                                      using System.Linq;
                                      namespace Consumer;
                                      internal class AllowedUsage
                                      {
                                          public bool HasEven(List<int> numbers) => numbers.Any(n => n % 2 == 0);
                                      }
                                      """);

        result.ShouldSucceed().ShouldNotHaveWarning(RS0030);
    }

    [Fact]
    public async Task List_Exists_Is_Not_Banned()
    {
        var result = await QuickBuild("""
                                      using System.Collections.Generic;
                                      namespace Consumer;
                                      internal class AllowedUsage
                                      {
                                          public bool HasEven(List<int> numbers) => numbers.Exists(n => n % 2 == 0);
                                      }
                                      """);

        result.ShouldSucceed().ShouldNotHaveWarning(RS0030);
    }

    [Fact]
    public async Task List_Find_Is_Not_Banned()
    {
        var result = await QuickBuild("""
                                      using System.Collections.Generic;
                                      namespace Consumer;
                                      internal class AllowedUsage
                                      {
                                          public int FindEven(List<int> numbers) => numbers.Find(n => n % 2 == 0);
                                      }
                                      """);

        result.ShouldSucceed().ShouldNotHaveWarning(RS0030);
    }

    [Fact]
    public async Task TimeProvider_GetUtcNow_Is_Not_Banned()
    {
        var result = await QuickBuild("""
                                      using System;
                                      namespace Consumer;
                                      internal class AllowedUsage
                                      {
                                          public DateTimeOffset GetTime() => TimeProvider.System.GetUtcNow();
                                      }
                                      """);

        result.ShouldSucceed().ShouldNotHaveWarning(RS0030);
    }
}