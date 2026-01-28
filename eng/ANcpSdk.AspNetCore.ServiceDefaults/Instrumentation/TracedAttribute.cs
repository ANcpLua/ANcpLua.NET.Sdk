namespace ANcpSdk.AspNetCore.ServiceDefaults.Instrumentation;

/// <summary>
///     Marks a method for automatic OpenTelemetry tracing instrumentation.
/// </summary>
/// <remarks>
///     <para>
///         Methods decorated with this attribute will be intercepted at compile time
///         to automatically create a span around the method execution.
///     </para>
///     <para>
///         Example usage:
///         <code>
/// [Traced("MyApp.Orders")]
/// public async Task&lt;Order&gt; ProcessOrder(
///     [TracedTag("order.id")] string orderId,
///     [TracedTag("order.amount")] decimal amount)
/// {
///     // Method body - automatically wrapped in a span
/// }
/// </code>
///     </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class TracedAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TracedAttribute" /> class.
    /// </summary>
    /// <param name="activitySourceName">
    ///     The name of the ActivitySource to use for creating spans.
    ///     This should match a registered ActivitySource in your application.
    /// </param>
    public TracedAttribute(string activitySourceName) =>
        ActivitySourceName = activitySourceName ?? throw new ArgumentNullException(nameof(activitySourceName));

    /// <summary>
    ///     Gets the name of the ActivitySource to use for creating spans.
    /// </summary>
    public string ActivitySourceName { get; }

    /// <summary>
    ///     Gets or sets the span name. If not specified, defaults to the method name.
    /// </summary>
    public string? SpanName { get; set; }

    /// <summary>
    ///     Gets or sets the span kind. Defaults to <see cref="System.Diagnostics.ActivityKind.Internal" />.
    /// </summary>
    public System.Diagnostics.ActivityKind Kind { get; set; } = System.Diagnostics.ActivityKind.Internal;
}
