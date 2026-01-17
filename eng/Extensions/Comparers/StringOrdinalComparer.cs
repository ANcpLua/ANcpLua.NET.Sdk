


using System;
using System.Collections.Generic;

namespace ANcpLua.NET.Sdk.Shared.Extensions.Comparers;

/// <summary>
///     A singleton ordinal string comparer implementing both <see cref="IComparer{T}"/>
///     and <see cref="IEqualityComparer{T}"/> for consistent, allocation-free comparisons.
/// </summary>
/// <remarks>
///     <para>
///         This class delegates all comparison operations to <see cref="StringComparer.Ordinal"/>,
///         providing a unified type that implements both comparison interfaces. This is useful
///         in scenarios requiring a single comparer instance for sorting and equality checks.
///     </para>
///     <para>
///         Ordinal comparison performs a byte-by-byte comparison of the string's UTF-16 code units,
///         making it case-sensitive and culture-insensitive. This is the fastest string comparison
///         method and is ideal for internal identifiers, file paths, and dictionary keys.
///     </para>
/// </remarks>
internal sealed class StringOrdinalComparer : IComparer<string>, IEqualityComparer<string>
{
    /// <summary>
    ///     Gets the singleton instance of <see cref="StringOrdinalComparer"/>.
    /// </summary>
    /// <value>The shared, thread-safe comparer instance.</value>
    public static readonly StringOrdinalComparer Instance = new();

    private StringOrdinalComparer()
    {
    }

    /// <inheritdoc />
    /// <remarks>Delegates to <see cref="StringComparer.Ordinal"/>.</remarks>
    public int Compare(string? x, string? y)
    {
        return StringComparer.Ordinal.Compare(x, y);
    }

    /// <inheritdoc />
    /// <remarks>Delegates to <see cref="StringComparer.Ordinal"/>.</remarks>
    public bool Equals(string? x, string? y)
    {
        return StringComparer.Ordinal.Equals(x, y);
    }

    /// <inheritdoc />
    /// <remarks>Delegates to <see cref="StringComparer.Ordinal"/>.</remarks>
    public int GetHashCode(string obj)
    {
        return StringComparer.Ordinal.GetHashCode(obj);
    }
}

/// <summary>
///     A singleton ordinal case-insensitive string comparer implementing both <see cref="IComparer{T}"/>
///     and <see cref="IEqualityComparer{T}"/> for consistent, allocation-free comparisons.
/// </summary>
/// <remarks>
///     <para>
///         This class delegates all comparison operations to <see cref="StringComparer.OrdinalIgnoreCase"/>,
///         providing a unified type that implements both comparison interfaces.
///     </para>
///     <para>
///         Ordinal case-insensitive comparison performs case-folding using the invariant culture's
///         uppercase mapping before comparing. This is suitable for case-insensitive identifiers
///         and file paths on case-insensitive file systems (e.g., Windows).
///     </para>
/// </remarks>
internal sealed class StringOrdinalIgnoreCaseComparer : IComparer<string>, IEqualityComparer<string>
{
    /// <summary>
    ///     Gets the singleton instance of <see cref="StringOrdinalIgnoreCaseComparer"/>.
    /// </summary>
    /// <value>The shared, thread-safe comparer instance.</value>
    public static readonly StringOrdinalIgnoreCaseComparer Instance = new();

    private StringOrdinalIgnoreCaseComparer()
    {
    }

    /// <inheritdoc />
    /// <remarks>Delegates to <see cref="StringComparer.OrdinalIgnoreCase"/>.</remarks>
    public int Compare(string? x, string? y)
    {
        return StringComparer.OrdinalIgnoreCase.Compare(x, y);
    }

    /// <inheritdoc />
    /// <remarks>Delegates to <see cref="StringComparer.OrdinalIgnoreCase"/>.</remarks>
    public bool Equals(string? x, string? y)
    {
        return StringComparer.OrdinalIgnoreCase.Equals(x, y);
    }

    /// <inheritdoc />
    /// <remarks>Delegates to <see cref="StringComparer.OrdinalIgnoreCase"/>.</remarks>
    public int GetHashCode(string obj)
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(obj);
    }
}