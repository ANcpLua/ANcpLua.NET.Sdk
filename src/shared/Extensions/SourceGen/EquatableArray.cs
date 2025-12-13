// Copyright (c) ANcpLua. All rights reserved.
// Licensed under the MIT License.

#if ANCPLUA_SOURCEGEN_HELPERS

using System.Collections.Immutable;

namespace ANcpLua.SourceGen;

/// <summary>
/// An equatable wrapper for <see cref="ImmutableArray{T}"/> for use in source generators.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
internal readonly record struct EquatableArray<T>(ImmutableArray<T> Items)
    where T : IEquatable<T>
{
    /// <summary>An empty array.</summary>
    public static readonly EquatableArray<T> Empty = new(ImmutableArray<T>.Empty);

    /// <summary>Gets a value indicating whether the array is default or empty.</summary>
    public bool IsDefaultOrEmpty => Items.IsDefaultOrEmpty;

    /// <summary>Gets the number of elements in the array.</summary>
    public int Length => Items.IsDefault ? 0 : Items.Length;

    /// <inheritdoc />
    public bool Equals(EquatableArray<T> other)
    {
        if (Items.IsDefault && other.Items.IsDefault)
        {
            return true;
        }

        if (Items.IsDefault || other.Items.IsDefault)
        {
            return false;
        }

        return Items.AsSpan().SequenceEqual(other.Items.AsSpan());
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        if (Items.IsDefault)
        {
            return 0;
        }

        unchecked
        {
            var hash = 17;
            foreach (var item in Items)
            {
                hash = (hash * 31) + (item?.GetHashCode() ?? 0);
            }

            return hash;
        }
    }
}

#endif
