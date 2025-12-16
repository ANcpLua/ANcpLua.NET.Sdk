using System.Text;
using Microsoft.Extensions.Logging.Testing;

namespace ANcpLua.Extensions.FakeLogger;

public static class FakeLoggerExtensions
{
    public static string GetFullLoggerText(
        this FakeLogCollector source,
        Func<FakeLogRecord, string>? formatter = null)
    {
        StringBuilder sb = new();
        IReadOnlyList<FakeLogRecord> snapshot = source.GetSnapshot();
        formatter ??= record => $"{record.Level} - {record.Message}";

        foreach (var record in snapshot) sb.AppendLine(formatter(record));

        return sb.ToString();
    }

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