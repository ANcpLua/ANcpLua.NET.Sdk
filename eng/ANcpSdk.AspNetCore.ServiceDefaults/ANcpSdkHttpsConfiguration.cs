namespace ANcpSdk.AspNetCore.ServiceDefaults;

public sealed class ANcpSdkHttpsConfiguration
{
    /// <summary>
    ///     Gets or sets a value indicating whether HTTPS redirection is enabled.
    ///     <value>The default value is <see langword="true"/>.</value>
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether HTTP Strict Transport Security (HSTS) is enabled.
    ///     <para>
    ///         HSTS is only enabled in non-development environments.
    ///     </para>
    ///     <value>The default value is <see langword="true"/>.</value>
    /// </summary>
    public bool HstsEnabled { get; set; } = true;
}
