using ANcpLua.Extensions.FakeLogger;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace ANcpLua.Sdk.Tests;

/// <summary>
/// Tests for the FakeLoggerExtensions helper methods.
/// </summary>
public sealed class FakeLoggerExtensionsTests
{
    [Fact]
    public void GetFullLoggerText_ReturnsFormattedLogs()
    {
        // Arrange
        var collector = new FakeLogCollector();
        var logger = new FakeLogger<FakeLoggerExtensionsTests>(collector);

        logger.LogInformation("First message");
        logger.LogWarning("Second message");
        logger.LogError("Third message");

        // Act
        var result = collector.GetFullLoggerText();

        // Assert
        Assert.Contains("Information - First message", result);
        Assert.Contains("Warning - Second message", result);
        Assert.Contains("Error - Third message", result);
    }

    [Fact]
    public void GetFullLoggerText_WithCustomFormatter_UsesFormatter()
    {
        // Arrange
        var collector = new FakeLogCollector();
        var logger = new FakeLogger<FakeLoggerExtensionsTests>(collector);

        logger.LogInformation("Test message");

        // Act
        var result = collector.GetFullLoggerText(record => $"[{record.Level}] {record.Message}");

        // Assert
        Assert.Contains("[Information] Test message", result);
    }

    [Fact]
    public void GetFullLoggerText_EmptyCollector_ReturnsEmptyString()
    {
        // Arrange
        var collector = new FakeLogCollector();

        // Act
        var result = collector.GetFullLoggerText();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task WaitForLogAsync_ConditionMet_ReturnsTrue()
    {
        // Arrange
        var collector = new FakeLogCollector();
        var logger = new FakeLogger<FakeLoggerExtensionsTests>(collector);

        logger.LogInformation("Expected message");

        // Act
        var result = await collector.WaitForLogAsync(
            logs => logs.Any(l => l.Message.Contains("Expected")),
            timeout: TimeSpan.FromSeconds(1));

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task WaitForLogAsync_ConditionNotMet_ReturnsFalse()
    {
        // Arrange
        var collector = new FakeLogCollector();
        var logger = new FakeLogger<FakeLoggerExtensionsTests>(collector);

        logger.LogInformation("Other message");

        // Act
        var result = await collector.WaitForLogAsync(
            logs => logs.Any(l => l.Message.Contains("NotFound")),
            timeout: TimeSpan.FromMilliseconds(100));

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task WaitForLogAsync_DelayedLog_WaitsAndReturnsTrue()
    {
        // Arrange
        var collector = new FakeLogCollector();
        var logger = new FakeLogger<FakeLoggerExtensionsTests>(collector);

        // Start background task to add log after delay
        _ = Task.Run(async () =>
        {
            await Task.Delay(50);
            logger.LogInformation("Delayed message");
        });

        // Act
        var result = await collector.WaitForLogAsync(
            logs => logs.Any(l => l.Message.Contains("Delayed")),
            timeout: TimeSpan.FromSeconds(2));

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task WaitForLogCountAsync_ReachesExpectedCount_ReturnsTrue()
    {
        // Arrange
        var collector = new FakeLogCollector();
        var logger = new FakeLogger<FakeLoggerExtensionsTests>(collector);

        logger.LogInformation("Message 1");
        logger.LogInformation("Message 2");
        logger.LogWarning("Warning message");

        // Act
        var result = await collector.WaitForLogCountAsync(
            log => log.Level == LogLevel.Information,
            expectedCount: 2,
            timeout: TimeSpan.FromSeconds(1));

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task WaitForLogCountAsync_NotEnoughLogs_ReturnsFalse()
    {
        // Arrange
        var collector = new FakeLogCollector();
        var logger = new FakeLogger<FakeLoggerExtensionsTests>(collector);

        logger.LogInformation("Only one message");

        // Act
        var result = await collector.WaitForLogCountAsync(
            log => log.Level == LogLevel.Information,
            expectedCount: 5,
            timeout: TimeSpan.FromMilliseconds(100));

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task WaitForLogAsync_CancellationRequested_ReturnsFalse()
    {
        // Arrange
        var collector = new FakeLogCollector();
        using var cts = new CancellationTokenSource();

        // Cancel immediately
        await cts.CancelAsync();

        // Act - The method catches OperationCanceledException and returns the condition result
        var result = await collector.WaitForLogAsync(
            logs => false, // Never true
            timeout: TimeSpan.FromSeconds(10),
            cancellationToken: cts.Token);

        // Assert
        Assert.False(result);
    }
}
