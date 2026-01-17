using ANcpLua.Extensions.FakeLogger;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace ANcpLua.Sdk.Tests;

/// <summary>
///     Tests for the FakeLoggerExtensions helper methods.
/// </summary>
public sealed class FakeLoggerExtensionsTests
{
    [Fact]
    public void GetFullLoggerText_ReturnsFormattedLogs()
    {
        var collector = new FakeLogCollector();
        var logger = new FakeLogger<FakeLoggerExtensionsTests>(collector);

        logger.LogInformation("First message");
        logger.LogWarning("Second message");
        logger.LogError("Third message");

        var result = collector.GetFullLoggerText();

        Assert.Contains("Information - First message", result);
        Assert.Contains("Warning - Second message", result);
        Assert.Contains("Error - Third message", result);
    }

    [Fact]
    public void GetFullLoggerText_WithCustomFormatter_UsesFormatter()
    {
        var collector = new FakeLogCollector();
        var logger = new FakeLogger<FakeLoggerExtensionsTests>(collector);

        logger.LogInformation("Test message");

        var result = collector.GetFullLoggerText(record => $"[{record.Level}] {record.Message}");

        Assert.Contains("[Information] Test message", result);
    }

    [Fact]
    public void GetFullLoggerText_EmptyCollector_ReturnsEmptyString()
    {
        var collector = new FakeLogCollector();

        var result = collector.GetFullLoggerText();

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task WaitForLogAsync_ConditionMet_ReturnsTrue()
    {
        var collector = new FakeLogCollector();
        var logger = new FakeLogger<FakeLoggerExtensionsTests>(collector);

        logger.LogInformation("Expected message");

        var result = await collector.WaitForLogAsync(logs => logs.Any(l => l.Message.Contains("Expected")),
            TimeSpan.FromSeconds(1), cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result);
    }

    [Fact]
    public async Task WaitForLogAsync_ConditionNotMet_ReturnsFalse()
    {
        var collector = new FakeLogCollector();
        var logger = new FakeLogger<FakeLoggerExtensionsTests>(collector);

        logger.LogInformation("Other message");

        var result = await collector.WaitForLogAsync(logs => logs.Any(l => l.Message.Contains("NotFound")),
            TimeSpan.FromMilliseconds(100), cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    [Fact]
    public async Task WaitForLogAsync_DelayedLog_WaitsAndReturnsTrue()
    {
        var collector = new FakeLogCollector();
        var logger = new FakeLogger<FakeLoggerExtensionsTests>(collector);

        _ = Task.Run(async () =>
        {
            await Task.Delay(50);
            logger.LogInformation("Delayed message");
        }, TestContext.Current.CancellationToken);

        var result = await collector.WaitForLogAsync(logs => logs.Any(l => l.Message.Contains("Delayed")),
            TimeSpan.FromSeconds(2), cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result);
    }

    [Fact]
    public async Task WaitForLogCountAsync_ReachesExpectedCount_ReturnsTrue()
    {
        var collector = new FakeLogCollector();
        var logger = new FakeLogger<FakeLoggerExtensionsTests>(collector);

        logger.LogInformation("Message 1");
        logger.LogInformation("Message 2");
        logger.LogWarning("Warning message");

        var result = await collector.WaitForLogCountAsync(log => log.Level == LogLevel.Information, 2,
            TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);

        Assert.True(result);
    }

    [Fact]
    public async Task WaitForLogCountAsync_NotEnoughLogs_ReturnsFalse()
    {
        var collector = new FakeLogCollector();
        var logger = new FakeLogger<FakeLoggerExtensionsTests>(collector);

        logger.LogInformation("Only one message");

        var result = await collector.WaitForLogCountAsync(log => log.Level == LogLevel.Information, 5,
            TimeSpan.FromMilliseconds(100), TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    [Fact]
    public async Task WaitForLogAsync_CancellationRequested_ReturnsFalse()
    {
        var collector = new FakeLogCollector();
        using var cts = new CancellationTokenSource();

        await cts.CancelAsync();

        var result = await collector.WaitForLogAsync(
            _ => false,
            TimeSpan.FromSeconds(10),
            cancellationToken: cts.Token);

        Assert.False(result);
    }
}
