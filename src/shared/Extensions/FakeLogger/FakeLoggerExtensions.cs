using System.Text;
using Microsoft.Extensions.Logging.Testing;

namespace ANcpLua.Extensions.FakeLogger;

/// <summary>
///     Extension methods for <see cref="FakeLogCollector" /> to simplify log assertions in tests.
/// </summary>
/// <remarks>
///     <para>
///         These extensions provide convenient ways to retrieve formatted log output and
///         asynchronously wait for specific log conditions during testing scenarios.
///     </para>
/// </remarks>
public static class FakeLoggerExtensions
{
    /// <summary>
    ///     Retrieves all collected log entries as a single formatted string.
    /// </summary>
    /// <param name="source">The <see cref="FakeLogCollector" /> containing the log records.</param>
    /// <param name="formatter">
    ///     An optional function to format each <see cref="FakeLogRecord" />.
    ///     If <c>null</c>, defaults to "{Level} - {Message}" format.
    /// </param>
    /// <returns>
    ///     A string containing all log entries, each on a separate line, formatted according to the specified formatter.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method takes a snapshot of the current log state, so logs added after
    ///         the call begins will not be included in the result.
    ///     </para>
    /// </remarks>
    public static string GetFullLoggerText(
        this FakeLogCollector source,
        Func<FakeLogRecord, string>? formatter = null)
    {
        StringBuilder sb = new();
        var snapshot = source.GetSnapshot();
        formatter ??= record => $"{record.Level} - {record.Message}";

        foreach (var record in snapshot) sb.AppendLine(formatter(record));

        return sb.ToString();
    }

    /// <summary>
    ///     Asynchronously waits for a log condition to be satisfied within a specified timeout.
    /// </summary>
    /// <param name="source">The <see cref="FakeLogCollector" /> to monitor.</param>
    /// <param name="condition">
    ///     A predicate that receives the current snapshot of log records and returns <c>true</c>
    ///     when the expected condition is met.
    /// </param>
    /// <param name="timeout">
    ///     The maximum time to wait for the condition. Defaults to 5 seconds if not specified.
    /// </param>
    /// <param name="pollInterval">
    ///     The interval between condition checks. Defaults to 25 milliseconds if not specified.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the wait operation.</param>
    /// <returns>
    ///     <c>true</c> if the condition was satisfied within the timeout; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         The method performs a final condition check after timeout to handle race conditions
    ///         where the condition becomes true just as the timeout expires.
    ///     </para>
    /// </remarks>
    public static async Task<bool> WaitForLogAsync(
        this FakeLogCollector source,
        Func<IReadOnlyList<FakeLogRecord>, bool> condition,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
    {
        timeout ??= TimeSpan.FromSeconds(5);
        pollInterval ??= TimeSpan.FromMilliseconds(25);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout.Value);

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                if (condition(source.GetSnapshot())) return true;

                await Task.Delay(pollInterval.Value, cts.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
        }

        return condition(source.GetSnapshot());
    }

    /// <summary>
    ///     Asynchronously waits until a specified number of log entries matching a predicate have been collected.
    /// </summary>
    /// <param name="source">The <see cref="FakeLogCollector" /> to monitor.</param>
    /// <param name="predicate">A function to test each log record for a match.</param>
    /// <param name="expectedCount">The minimum number of matching log entries to wait for.</param>
    /// <param name="timeout">
    ///     The maximum time to wait for the expected count. Defaults to 5 seconds if not specified.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the wait operation.</param>
    /// <returns>
    ///     <c>true</c> if at least <paramref name="expectedCount" /> matching entries were found
    ///     within the timeout; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This is a convenience wrapper around <see cref="WaitForLogAsync" /> for the common
    ///         pattern of waiting for a specific number of log entries.
    ///     </para>
    /// </remarks>
    public static Task<bool> WaitForLogCountAsync(
        this FakeLogCollector source,
        Func<FakeLogRecord, bool> predicate,
        int expectedCount,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        return source.WaitForLogAsync(
            logs => logs.Count(predicate) >= expectedCount,
            timeout,
            cancellationToken: cancellationToken);
    }
}
