// ============================================================================
// CRITICAL: ALL SDK BRANDING STRINGS MUST COME FROM HERE
//
// DO NOT hardcode "Meziantou", "meziantou", or any legacy names anywhere.
// This file is the SINGLE SOURCE OF TRUTH for all branding.
//
// History: 7 hours lost debugging because of hardcoded legacy names (2024-12-16)
// ============================================================================

namespace ANcpLua.Sdk.Tests.Infrastructure;

/// <summary>
///     Single source of truth for all SDK branding strings.
///     NEVER hardcode these values - always reference this class.
/// </summary>
public static class SdkBrandingConstants
{
    public const string Author = "ANcpLua";
    public const string SdkMetadataKey = "ANcpLua.Sdk.Name";

    // ServiceDefaults
    public const string ServiceDefaultsNamespace = "ANcpSdk.AspNetCore.ServiceDefaults";
    public const string ServiceDefaultsOptionsType = "ANcpSdkServiceDefaultsOptions";
    public const string ConventionsMethod = "UseANcpSdkConventions";

    // ═══════════════════════════════════════════════════════════════════════
    // LEGACY NAMES - FOR DETECTION/MIGRATION ONLY - DO NOT USE IN NEW CODE!
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    ///     Legacy names that should NEVER appear in code. Used for validation only.
    ///     If you see these in a PR review, REJECT IT.
    /// </summary>
    public static readonly string[] LegacyNamesToBlock =
    [
        "Meziantou",
        "meziantou",
        "MEZIANTOU",
        "UseMeziantouConventions",
        "MeziantouServiceDefaultsOptions",
        "Meziantou.AspNetCore.ServiceDefaults",
        "Meziantou.Sdk.Name"
    ];

    // Test Code Snippets
    public static string ServiceDefaultsUsing => $"using {ServiceDefaultsNamespace};";

    public static string WebAppWithConventions => $"""
                                                   {ServiceDefaultsUsing}
                                                   var builder = WebApplication.CreateBuilder();
                                                   builder.{ConventionsMethod}();
                                                   """;

    public static string WebAppCheckServiceDefaults => $"""
                                                        {ServiceDefaultsUsing}
                                                        var builder = WebApplication.CreateBuilder();
                                                        var app = builder.Build();
                                                        return app.Services.GetService<{ServiceDefaultsOptionsType}>() is not null ? 0 : 1;
                                                        """;
}