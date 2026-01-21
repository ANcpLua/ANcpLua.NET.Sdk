namespace ANcpSdk.AspNetCore.ServiceDefaults;

/// <summary>
///     Configuration options for Anti-Forgery protection.
/// </summary>
public sealed class ANcpSdkAntiForgeryConfiguration
{
    /// <summary>
    ///     Gets or sets a value indicating whether Anti-Forgery services and middleware are enabled.
    ///     <value>The default value is <see langword="true"/>.</value>
    /// </summary>
    public bool Enabled { get; set; } = true;
}
