// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Source: https://github.com/Tenacom/PolyKit

#if !NETCOREAPP3_0_OR_GREATER && !NETSTANDARD2_1_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System
{
    /// <summary>
    ///     Represent a range that has start and end indexes.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal readonly struct Range : IEquatable<Range>
    {
        public Index Start { get; }
        public Index End { get; }

        public Range(Index start, Index end)
        {
            Start = start;
            End = end;
        }

        public override bool Equals(object? obj) => obj is Range other && other.Start.Equals(Start) && other.End.Equals(End);
        public bool Equals(Range other) => other.Start.Equals(Start) && other.End.Equals(End);
        public override int GetHashCode() => (Start.GetHashCode() * 31) + End.GetHashCode();
        public override string ToString() => Start + ".." + End;

        public static Range StartAt(Index start) => new(start, Index.End);
        public static Range EndAt(Index end) => new(Index.Start, end);
        public static Range All => new(Index.Start, Index.End);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (int Offset, int Length) GetOffsetAndLength(int length)
        {
            var start = Start.GetOffset(length);
            var end = End.GetOffset(length);
            if ((uint)end > (uint)length || (uint)start > (uint)end) throw new ArgumentOutOfRangeException(nameof(length));
            return (start, end - start);
        }
    }
}
#endif