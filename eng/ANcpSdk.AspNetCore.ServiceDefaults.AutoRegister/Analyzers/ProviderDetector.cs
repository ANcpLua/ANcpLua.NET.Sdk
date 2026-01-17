using System.Collections.Immutable;
using ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister.Models;
using Microsoft.CodeAnalysis;

namespace ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister.Analyzers;

/// <summary>
///     Detects instrumentation provider packages from compilation references.
/// </summary>
/// <remarks>
///     Uses <see cref="ProviderRegistry" /> as the Single Source of Truth for provider definitions.
/// </remarks>
internal static class ProviderDetector
{
    /// <summary>
    ///     Detects all instrumentation providers referenced by the compilation.
    /// </summary>
    public static ImmutableArray<ProviderInfo> DetectProviders(Compilation compilation)
    {
        var referencedAssemblies = compilation.ReferencedAssemblyNames
            .Select(static a => a.Name)
            .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

        var providers = ImmutableArray.CreateBuilder<ProviderInfo>();

        foreach (var definition in ProviderRegistry.AllProviders)
            if (referencedAssemblies.Contains(definition.AssemblyName))
                providers.Add(new ProviderInfo(
                    Category: definition.Category,
                    ProviderId: definition.ProviderId,
                    AssemblyName: definition.AssemblyName,
                    PrimaryTypeName: definition.PrimaryTypeName));

        return providers.ToImmutable();
    }

    /// <summary>
    ///     Checks if any GenAI providers are detected.
    /// </summary>
    public static bool HasGenAiProviders(Compilation compilation)
    {
        var referencedAssemblies = compilation.ReferencedAssemblyNames
            .Select(static a => a.Name)
            .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

        return ProviderRegistry.GenAiProviders.Any(p => referencedAssemblies.Contains(p.AssemblyName));
    }

    /// <summary>
    ///     Checks if any database providers are detected.
    /// </summary>
    public static bool HasDatabaseProviders(Compilation compilation)
    {
        var referencedAssemblies = compilation.ReferencedAssemblyNames
            .Select(static a => a.Name)
            .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

        return ProviderRegistry.DatabaseProviders.Any(p => referencedAssemblies.Contains(p.AssemblyName));
    }

    /// <summary>
    ///     Gets the provider ID for a database system based on type name.
    /// </summary>
    public static string? GetDatabaseProviderId(string typeName)
    {
        return ProviderRegistry.DatabaseProviders
            .Where(p => typeName.Contains(p.TypeContains, StringComparison.OrdinalIgnoreCase))
            .Select(static p => p.ProviderId)
            .FirstOrDefault();
    }

    /// <summary>
    ///     Gets the provider ID for a GenAI system based on type name.
    /// </summary>
    public static string? GetGenAiProviderId(string typeName)
    {
        return ProviderRegistry.GenAiProviders
            .Where(p => typeName.Contains(p.TypeContains, StringComparison.OrdinalIgnoreCase))
            .Select(static p => p.ProviderId)
            .FirstOrDefault();
    }
}
