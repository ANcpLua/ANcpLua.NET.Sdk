// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Source: https://github.com/Tenacom/PolyKit

#if !NETCOREAPP3_0_OR_GREATER && !NETSTANDARD2_1_OR_GREATER

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System;

/// <summary>
/// Represent a type can be used to index a collection either from the start or the end.
/// </summary>
/// <remarks>
/// Index is used by the C# compiler to support the index syntax.
/// <code>
/// int[] someArray = new int[5] { 1, 2, 3, 4, 5 };
/// int lastElement = someArray[^1]; // lastElement = 5
/// </code>
/// </remarks>
[ExcludeFromCodeCoverage]
internal readonly struct Index : IEquatable<Index>
{
    private readonly int _value;

    /// <summary>
    /// Construct an Index using a value and indicating if the index is from the start or from the end.
    /// </summary>
    /// <param name="value">The index value. it has to be zero or positive number.</param>
    /// <param name="fromEnd">Indicating if the index is from the start or from the end.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Index(int value, bool fromEnd = false)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Non-negative number required.");
        }

        _value = fromEnd ? ~value : value;
    }

    private Index(int value) => _value = value;

    /// <summary>Create an Index pointing at first element.</summary>
    public static Index Start => new(0);

    /// <summary>Create an Index pointing at beyond last element.</summary>
    public static Index End => new(~0);

    /// <summary>Create an Index from the start at the position indicated by the value.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Index FromStart(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Non-negative number required.");
        }

        return new Index(value);
    }

    /// <summary>Create an Index from the end at the position indicated by the value.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Index FromEnd(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Non-negative number required.");
        }

        return new Index(~value);
    }

    /// <summary>Gets the index value.</summary>
    public int Value => _value < 0 ? ~_value : _value;

    /// <summary>Indicates whether the index is from the start or the end.</summary>
    public bool IsFromEnd => _value < 0;

    /// <summary>Calculate the offset from the start using the giving collection length.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetOffset(int length)
    {
        var offset = _value;
        if (IsFromEnd)
        {
            offset += length + 1;
        }

        return offset;
    }

    /// <inheritdoc />
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is Index other && _value == other._value;

    /// <inheritdoc />
    public bool Equals(Index other) => _value == other._value;

    /// <inheritdoc />
    public override int GetHashCode() => _value;

    /// <summary>Converts an integer number to an Index.</summary>
    public static implicit operator Index(int value) => FromStart(value);

    /// <inheritdoc />
    public override string ToString() => IsFromEnd ? "^" + Value.ToString() : ((uint)Value).ToString();
}

#endif
