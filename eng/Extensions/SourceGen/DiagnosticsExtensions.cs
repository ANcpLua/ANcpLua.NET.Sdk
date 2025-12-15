// ANcpLua.NET.Sdk - Source Generator Helpers

#if ANCPLUA_SOURCEGEN_HELPERS
using Microsoft.CodeAnalysis;

namespace ANcpLua.SourceGen;

/// <summary>
/// Extension methods for reporting diagnostics in incremental generators.
/// </summary>
internal static class DiagnosticsExtensions
{
    /// <summary>
    /// Registers an output node to report diagnostics.
    /// </summary>
    /// <param name="context">The generator initialization context.</param>
    /// <param name="diagnostics">The diagnostics provider.</param>
    public static void ReportDiagnostics(
        this IncrementalGeneratorInitializationContext context,
        IncrementalValuesProvider<DiagnosticInfo> diagnostics)
    {
        context.RegisterSourceOutput(diagnostics, static (context, diagnostic) =>
        {
            context.ReportDiagnostic(diagnostic.ToDiagnostic());
        });
    }
}

#endif