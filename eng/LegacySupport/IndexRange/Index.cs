// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Source: https://github.com/Tenacom/PolyKit

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#if !NETCOREAPP3_0_OR_GREATER && !NETSTANDARD2_1_OR_GREATER
namespace System;

/// <summary>
///     Represent a type can be used to index a collection either from the start or the end.
/// </summary>
[ExcludeFromCodeCoverage]
internal readonly struct Index : IEquatable<Index>
{
    private readonly int _value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Index(int value, bool fromEnd = false)
    {
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "Non-negative number required.");

        _value = fromEnd ? ~value : value;
    }

    private Index(int value)
    {
        _value = value;
    }

    public static Index Start => new(0);
    public static Index End => new(~0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Index FromStart(int value)
    {
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "Non-negative number required.");
        return new Index(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Index FromEnd(int value)
    {
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "Non-negative number required.");
        return new Index(~value);
    }

    public int Value => _value < 0 ? ~_value : _value;
    public bool IsFromEnd => _value < 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetOffset(int length)
    {
        var offset = _value;
        if (IsFromEnd) offset += length + 1;
        return offset;
    }

    public override bool Equals(object? obj)
    {
        return obj is Index other && _value == other._value;
    }

    public bool Equals(Index other)
    {
        return _value == other._value;
    }

    public override int GetHashCode()
    {
        return _value;
    }

    public static implicit operator Index(int value)
    {
        return FromStart(value);
    }

    public override string ToString()
    {
        return IsFromEnd ? "^" + Value : ((uint)Value).ToString();
    }
}
#endif