namespace ANcpLua.Sdk.Tests;

public sealed class BannedApiTests(PackageFixture fixture) : SdkTestBase(fixture)
{
    private const string Rs0030 = "RS0030";

    [Fact]
    public async Task Detect_WhenArgumentNullExceptionThrowIfNullUsed_DoesNotReportRs0030()
    {
        var result = await BuildLibraryAsync("""
            namespace Consumer;
            internal class AllowedUsage
            {
                public void Validate(object? value)
                {
                    ArgumentNullException.ThrowIfNull(value);
                }
            }
            """);

        result.ShouldSucceed().ShouldNotHaveWarning(Rs0030);
    }

    [Fact]
    public async Task Detect_WhenArgumentExceptionThrowIfNullOrWhiteSpaceUsed_DoesNotReportRs0030()
    {
        var result = await BuildLibraryAsync("""
            namespace Consumer;
            internal class AllowedUsage
            {
                public void Validate(string? value)
                {
                    ArgumentException.ThrowIfNullOrWhiteSpace(value);
                }
            }
            """);

        result.ShouldSucceed().ShouldNotHaveWarning(Rs0030);
    }

    [Fact]
    public async Task Detect_WhenArgumentOutOfRangeExceptionThrowIfNegativeUsed_DoesNotReportRs0030()
    {
        var result = await BuildLibraryAsync("""
            namespace Consumer;
            internal class AllowedUsage
            {
                public void Validate(int value)
                {
                    ArgumentOutOfRangeException.ThrowIfNegative(value);
                }
            }
            """);

        result.ShouldSucceed().ShouldNotHaveWarning(Rs0030);
    }

    [Fact]
    public async Task Detect_WhenDateTimeNowUsed_ReportsRs0030()
    {
        var result = await BuildLibraryAsync("""
            using System;
            namespace Consumer;
            internal class BannedUsage
            {
                public DateTime GetTime() => DateTime.Now;
            }
            """);

        result.ShouldSucceed().ShouldHaveWarning(Rs0030);
    }

    [Fact]
    public async Task Detect_WhenEnumerableAnyWithPredicateUsed_DoesNotReportRs0030()
    {
        var result = await BuildLibraryAsync("""
            using System.Collections.Generic;
            using System.Linq;
            namespace Consumer;
            internal class AllowedUsage
            {
                public bool HasEven(List<int> numbers) => numbers.Any(n => n % 2 == 0);
            }
            """);

        result.ShouldSucceed().ShouldNotHaveWarning(Rs0030);
    }

    [Fact]
    public async Task Detect_WhenListExistsUsed_DoesNotReportRs0030()
    {
        var result = await BuildLibraryAsync("""
            using System.Collections.Generic;
            namespace Consumer;
            internal class AllowedUsage
            {
                public bool HasEven(List<int> numbers) => numbers.Exists(n => n % 2 == 0);
            }
            """);

        result.ShouldSucceed().ShouldNotHaveWarning(Rs0030);
    }

    [Fact]
    public async Task Detect_WhenListFindUsed_DoesNotReportRs0030()
    {
        var result = await BuildLibraryAsync("""
            using System.Collections.Generic;
            namespace Consumer;
            internal class AllowedUsage
            {
                public int FindEven(List<int> numbers) => numbers.Find(n => n % 2 == 0);
            }
            """);

        result.ShouldSucceed().ShouldNotHaveWarning(Rs0030);
    }

    [Fact]
    public async Task Detect_WhenTimeProviderGetUtcNowUsed_DoesNotReportRs0030()
    {
        var result = await BuildLibraryAsync("""
            using System;
            namespace Consumer;
            internal class AllowedUsage
            {
                public DateTimeOffset GetTime() => TimeProvider.System.GetUtcNow();
            }
            """);

        result.ShouldSucceed().ShouldNotHaveWarning(Rs0030);
    }
}
