// ANcpLua.Sdk.Canary - netstandard2.0 Polyfill Validation
// Validates that polyfills compile correctly on legacy TFMs

#if NETSTANDARD2_0

using Microsoft.Shared.Diagnostics;

namespace ANcpLua.Sdk.Canary;

/// <summary>
/// Validates SDK polyfills work on netstandard2.0.
/// This file compiles (no tests run) - compilation success = validation pass.
/// </summary>
internal static class PolyfillValidation
{
    // ═══════════════════════════════════════════════════════════════════════
    // Index polyfill validation (Range slicing requires RuntimeHelpers.GetSubArray)
    // ═══════════════════════════════════════════════════════════════════════
    public static void ValidateIndex()
    {
        var arr = new[] { 1, 2, 3, 4, 5 };

        // Index from start
        _ = arr[0];

        // Index from end (requires polyfill)
        _ = arr[^1];

        // Index type usage
        System.Index idx = ^2;
        _ = arr[idx];
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Nullable attributes polyfill validation
    // ═══════════════════════════════════════════════════════════════════════
    public static bool TryGetValue([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? value)
    {
        value = "test";
        return true;
    }

    public static void RequiresNotNull([System.Diagnostics.CodeAnalysis.NotNull] string? value)
    {
        if (value is null)
            throw new System.ArgumentNullException(nameof(value));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CallerArgumentExpression polyfill validation
    // ═══════════════════════════════════════════════════════════════════════
    public static void ValidateArg(
        bool condition,
        [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(condition))] string? expression = null)
    {
        if (!condition)
            throw new System.ArgumentException($"Condition failed: {expression}");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // IsExternalInit polyfill validation (init accessors)
    // ═══════════════════════════════════════════════════════════════════════
    public static void ValidateInitAccessor()
    {
        _ = new InitPropertyTest { Value = "test" };
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Required member polyfill validation
    // ═══════════════════════════════════════════════════════════════════════
    public static void ValidateRequiredMember()
    {
        _ = new RequiredMemberTest { Name = "test" };
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Throw helpers validation
    // ═══════════════════════════════════════════════════════════════════════
    public static void ValidateThrowHelpers()
    {
        string? value = "test";
        Throw.IfNull(value);
        Throw.IfNullOrEmpty(value);
        Throw.IfNullOrWhitespace(value);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // StackTraceHidden polyfill validation
    // ═══════════════════════════════════════════════════════════════════════
    [System.Diagnostics.StackTraceHidden]
    public static void HiddenMethod() { }
}

// Non-nested types to avoid CA1034
internal sealed class InitPropertyTest
{
    public string Value { get; init; } = "";
}

internal sealed class RequiredMemberTest
{
    public required string Name { get; set; }
}

#endif
