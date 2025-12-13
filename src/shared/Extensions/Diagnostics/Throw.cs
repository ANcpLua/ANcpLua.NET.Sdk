// Copyright (c) ANcpLua. All rights reserved.
// Licensed under the MIT License.

#pragma warning disable IDE0005

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.Shared.Diagnostics;

/// <summary>
/// Defines static methods used to throw exceptions.
/// </summary>
/// <remarks>
/// The main purpose is to reduce code size, improve performance, and standardize exception messages.
/// </remarks>
[ExcludeFromCodeCoverage]
internal static partial class Throw
{
    #region For Object

    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> if the specified argument is <see langword="null"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNull]
    public static T IfNull<T>([NotNull] T argument, [CallerArgumentExpression(nameof(argument))] string paramName = "")
    {
        if (argument is null)
        {
            ArgumentNullException(paramName);
        }

        return argument;
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> if the specified argument is <see langword="null"/>,
    /// or <see cref="ArgumentException"/> if the specified member is <see langword="null"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNull]
    public static TMember IfNullOrMemberNull<TParameter, TMember>(
        [NotNull] TParameter argument,
        [NotNull] TMember member,
        [CallerArgumentExpression(nameof(argument))] string paramName = "",
        [CallerArgumentExpression(nameof(member))] string memberName = "")
    {
        if (argument is null)
        {
            ArgumentNullException(paramName);
        }

        if (member is null)
        {
            ArgumentException(paramName, $"Member {memberName} of {paramName} is null");
        }

        return member;
    }

    #endregion

    #region For String

    /// <summary>
    /// Throws either an <see cref="ArgumentNullException"/> or an <see cref="ArgumentException"/>
    /// if the specified string is <see langword="null"/> or whitespace respectively.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNull]
    public static string IfNullOrWhitespace([NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string paramName = "")
    {
        if (string.IsNullOrWhiteSpace(argument))
        {
            if (argument is null)
            {
                ArgumentNullException(paramName);
            }
            else
            {
                ArgumentException(paramName, "Argument is whitespace");
            }
        }

        return argument;
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> if the string is <see langword="null"/>,
    /// or <see cref="ArgumentException"/> if it is empty.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNull]
    public static string IfNullOrEmpty([NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string paramName = "")
    {
        if (string.IsNullOrEmpty(argument))
        {
            if (argument is null)
            {
                ArgumentNullException(paramName);
            }
            else
            {
                ArgumentException(paramName, "Argument is an empty string");
            }
        }

        return argument;
    }

    #endregion

    #region For Enums

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/> if the enum value is not valid.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T IfOutOfRange<T>(T argument, [CallerArgumentExpression(nameof(argument))] string paramName = "")
        where T : struct, Enum
    {
#if NET5_0_OR_GREATER
        if (!Enum.IsDefined(argument))
#else
        if (!Enum.IsDefined(typeof(T), argument))
#endif
        {
            ArgumentOutOfRangeException(paramName, $"{argument} is an invalid value for enum type {typeof(T)}");
        }

        return argument;
    }

    #endregion

    #region For Collections

    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> if the collection is <see langword="null"/>,
    /// or <see cref="ArgumentException"/> if it is empty.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNull]
    [ExcludeFromCodeCoverage]
    public static IEnumerable<T> IfNullOrEmpty<T>([NotNull] IEnumerable<T>? argument, [CallerArgumentExpression(nameof(argument))] string paramName = "")
    {
        if (argument is null)
        {
            ArgumentNullException(paramName);
        }
        else
        {
            switch (argument)
            {
                case ICollection<T> { Count: 0 }:
                case IReadOnlyCollection<T> { Count: 0 }:
                    ArgumentException(paramName, "Collection is empty");
                    break;
                default:
                    using (IEnumerator<T> enumerator = argument.GetEnumerator())
                    {
                        if (!enumerator.MoveNext())
                        {
                            ArgumentException(paramName, "Collection is empty");
                        }
                    }
                    break;
            }
        }

        return argument;
    }

    #endregion

    #region Exceptions

    [DoesNotReturn]
#if !NET6_0_OR_GREATER
    [MethodImpl(MethodImplOptions.NoInlining)]
#endif
    public static void ArgumentNullException(string paramName)
        => throw new ArgumentNullException(paramName);

    [DoesNotReturn]
#if !NET6_0_OR_GREATER
    [MethodImpl(MethodImplOptions.NoInlining)]
#endif
    public static void ArgumentNullException(string paramName, string? message)
        => throw new ArgumentNullException(paramName, message);

    [DoesNotReturn]
#if !NET6_0_OR_GREATER
    [MethodImpl(MethodImplOptions.NoInlining)]
#endif
    public static void ArgumentOutOfRangeException(string paramName)
        => throw new ArgumentOutOfRangeException(paramName);

    [DoesNotReturn]
#if !NET6_0_OR_GREATER
    [MethodImpl(MethodImplOptions.NoInlining)]
#endif
    public static void ArgumentOutOfRangeException(string paramName, string? message)
        => throw new ArgumentOutOfRangeException(paramName, message);

    [DoesNotReturn]
#if !NET6_0_OR_GREATER
    [MethodImpl(MethodImplOptions.NoInlining)]
#endif
    public static void ArgumentOutOfRangeException(string paramName, object? actualValue, string? message)
        => throw new ArgumentOutOfRangeException(paramName, actualValue, message);

    [DoesNotReturn]
#if !NET6_0_OR_GREATER
    [MethodImpl(MethodImplOptions.NoInlining)]
#endif
    public static void ArgumentException(string paramName, string? message)
        => throw new ArgumentException(message, paramName);

    [DoesNotReturn]
#if !NET6_0_OR_GREATER
    [MethodImpl(MethodImplOptions.NoInlining)]
#endif
    public static void InvalidOperationException(string message)
        => throw new InvalidOperationException(message);

    #endregion

    #region For Integer

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IfLessThan(int argument, int min, [CallerArgumentExpression(nameof(argument))] string paramName = "")
    {
        if (argument < min)
            ArgumentOutOfRangeException(paramName, argument, $"Argument less than minimum value {min}");
        return argument;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IfGreaterThan(int argument, int max, [CallerArgumentExpression(nameof(argument))] string paramName = "")
    {
        if (argument > max)
            ArgumentOutOfRangeException(paramName, argument, $"Argument greater than maximum value {max}");
        return argument;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IfOutOfRange(int argument, int min, int max, [CallerArgumentExpression(nameof(argument))] string paramName = "")
    {
        if (argument < min || argument > max)
            ArgumentOutOfRangeException(paramName, argument, $"Argument not in the range [{min}..{max}]");
        return argument;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IfZero(int argument, [CallerArgumentExpression(nameof(argument))] string paramName = "")
    {
        if (argument == 0)
            ArgumentOutOfRangeException(paramName, "Argument is zero");
        return argument;
    }

    #endregion

    #region For Long

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long IfLessThan(long argument, long min, [CallerArgumentExpression(nameof(argument))] string paramName = "")
    {
        if (argument < min)
            ArgumentOutOfRangeException(paramName, argument, $"Argument less than minimum value {min}");
        return argument;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long IfGreaterThan(long argument, long max, [CallerArgumentExpression(nameof(argument))] string paramName = "")
    {
        if (argument > max)
            ArgumentOutOfRangeException(paramName, argument, $"Argument greater than maximum value {max}");
        return argument;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long IfZero(long argument, [CallerArgumentExpression(nameof(argument))] string paramName = "")
    {
        if (argument == 0L)
            ArgumentOutOfRangeException(paramName, "Argument is zero");
        return argument;
    }

    #endregion

    #region For Double

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double IfLessThan(double argument, double min, [CallerArgumentExpression(nameof(argument))] string paramName = "")
    {
        // NaN-safe comparison
        if (!(argument >= min))
            ArgumentOutOfRangeException(paramName, argument, $"Argument less than minimum value {min}");
        return argument;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double IfGreaterThan(double argument, double max, [CallerArgumentExpression(nameof(argument))] string paramName = "")
    {
        // NaN-safe comparison
        if (!(argument <= max))
            ArgumentOutOfRangeException(paramName, argument, $"Argument greater than maximum value {max}");
        return argument;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double IfOutOfRange(double argument, double min, double max, [CallerArgumentExpression(nameof(argument))] string paramName = "")
    {
        // NaN-safe comparison
        if (!(min <= argument && argument <= max))
            ArgumentOutOfRangeException(paramName, argument, $"Argument not in the range [{min}..{max}]");
        return argument;
    }

    #endregion
}
