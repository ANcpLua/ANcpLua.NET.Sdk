
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.Testing;

namespace ANcpLua.Testing;

/// <summary>
///     Extensions for <see cref="FakeLogCollector"/> to help with testing logging.
/// </summary>
public static class FakeLoggerExtensions
{
    /// <summary>
    ///     Gets the full text of all logged messages.
    /// </summary>
    /// <param name="source">The log collector.</param>
    /// <param name="formatter">Optional formatter for each log record.</param>
    /// <returns>A string containing all log messages.</returns>
    public static string GetFullLoggerText(
        this FakeLogCollector source,
        Func<FakeLogRecord, string>? formatter = null)
    {
        var sb = new StringBuilder();
        formatter ??= record => $"{record.Level} - {record.Message}";
        foreach (var record in source.GetSnapshot())
            sb.AppendLine(formatter(record));
        return sb.ToString();
    }

    /// <summary>
    ///     Waits asynchronously until a condition over the logged messages is met.
    /// </summary>
    /// <param name="source">The log collector.</param>
    /// <param name="condition">A predicate that checks the list of log records.</param>
    /// <param name="timeout">The maximum time to wait. Defaults to 5 seconds.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task that returns <see langword="true"/> if the condition was met, or <see langword="false"/> if timed out.</returns>
    public static async Task<bool> WaitForLogAsync(
        this FakeLogCollector source,
        Func<IReadOnlyList<FakeLogRecord>, bool> condition,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        timeout ??= TimeSpan.FromSeconds(5);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout.Value);

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                if (condition(source.GetSnapshot())) return true;
                await Task.Delay(25, cts.Token);
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { }

        return condition(source.GetSnapshot());
    }

    /// <summary>
    ///     Waits asynchronously until a specific number of logs matching a predicate are found.
    /// </summary>
    /// <param name="source">The log collector.</param>
    /// <param name="predicate">A predicate to filter log records.</param>
    /// <param name="expectedCount">The number of matching logs to wait for.</param>
    /// <param name="timeout">The maximum time to wait.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task that returns <see langword="true"/> if the count was reached, or <see langword="false"/> if timed out.</returns>
    public static Task<bool> WaitForLogCountAsync(
        this FakeLogCollector source,
        Func<FakeLogRecord, bool> predicate,
        int expectedCount,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        return source.WaitForLogAsync(
            logs => logs.Count(predicate) >= expectedCount,
            timeout,
            ct);
    }
}
