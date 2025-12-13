// Copyright (c) ANcpLua. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Shared.Comparers;

/// <summary>
/// A singleton ordinal string comparer for consistent, allocation-free comparisons.
/// </summary>
internal sealed class StringOrdinalComparer : IComparer<string>, IEqualityComparer<string>
{
    /// <summary>Gets the singleton instance.</summary>
    public static readonly StringOrdinalComparer Instance = new();

    private StringOrdinalComparer() { }

    /// <inheritdoc />
    public int Compare(string? x, string? y) => StringComparer.Ordinal.Compare(x, y);

    /// <inheritdoc />
    public bool Equals(string? x, string? y) => StringComparer.Ordinal.Equals(x, y);

    /// <inheritdoc />
    public int GetHashCode(string obj) => StringComparer.Ordinal.GetHashCode(obj);
}

/// <summary>
/// A singleton ordinal case-insensitive string comparer.
/// </summary>
internal sealed class StringOrdinalIgnoreCaseComparer : IComparer<string>, IEqualityComparer<string>
{
    /// <summary>Gets the singleton instance.</summary>
    public static readonly StringOrdinalIgnoreCaseComparer Instance = new();

    private StringOrdinalIgnoreCaseComparer() { }

    /// <inheritdoc />
    public int Compare(string? x, string? y) => StringComparer.OrdinalIgnoreCase.Compare(x, y);

    /// <inheritdoc />
    public bool Equals(string? x, string? y) => StringComparer.OrdinalIgnoreCase.Equals(x, y);

    /// <inheritdoc />
    public int GetHashCode(string obj) => StringComparer.OrdinalIgnoreCase.GetHashCode(obj);
}
