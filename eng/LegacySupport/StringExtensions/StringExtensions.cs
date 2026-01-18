






#if !NETCOREAPP2_1_OR_GREATER && !NETSTANDARD2_1_OR_GREATER

namespace System;

using System.Text;

/// <summary>
/// Provides extension methods for <see cref="string"/> that are missing in older target frameworks.
/// These methods enable CA1307 and CA2249 compliance on netstandard2.0.
/// </summary>
internal static class StringExtensionsPolyfill {
    /// <summary>
    /// Returns a value indicating whether a specified substring occurs within this string,
    /// using the specified comparison rules.
    /// </summary>
    /// <param name="source">The source string.</param>
    /// <param name="value">The string to seek.</param>
    /// <param name="comparisonType">One of the enumeration values that specifies the rules for the search.</param>
    /// <returns>true if the value parameter occurs within this string; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="value"/> is null.</exception>
    public static bool Contains(this string source, string value, StringComparison comparisonType) {
        if (source is null) {
            throw new ArgumentNullException(nameof(source));
        }

        if (value is null) {
            throw new ArgumentNullException(nameof(value));
        }

        return source.IndexOf(value, comparisonType) >= 0;
    }

    /// <summary>
    /// Returns a value indicating whether a specified character occurs within this string.
    /// </summary>
    /// <param name="source">The source string.</param>
    /// <param name="value">The character to seek.</param>
    /// <returns>true if the value parameter occurs within this string; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    public static bool Contains(this string source, char value) {
        if (source is null) {
            throw new ArgumentNullException(nameof(source));
        }

        return source.IndexOf(value) >= 0;
    }

    /// <summary>
    /// Returns a value indicating whether a specified character occurs within this string,
    /// using the specified comparison rules.
    /// </summary>
    /// <param name="source">The source string.</param>
    /// <param name="value">The character to seek.</param>
    /// <param name="comparisonType">One of the enumeration values that specifies the rules for the search.</param>
    /// <returns>true if the value parameter occurs within this string; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    public static bool Contains(this string source, char value, StringComparison comparisonType) {
        if (source is null) {
            throw new ArgumentNullException(nameof(source));
        }

        
        return source.IndexOf(value.ToString(), comparisonType) >= 0;
    }

    /// <summary>
    /// Returns a new string in which all occurrences of a specified string in the current instance
    /// are replaced with another specified string, using the specified comparison type.
    /// </summary>
    /// <param name="source">The source string.</param>
    /// <param name="oldValue">The string to be replaced.</param>
    /// <param name="newValue">The string to replace all occurrences of oldValue.</param>
    /// <param name="comparisonType">One of the enumeration values that specifies the rules for the search.</param>
    /// <returns>A string that is equivalent to the current string except that all instances of oldValue
    /// are replaced with newValue.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="oldValue"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="oldValue"/> is the empty string.</exception>
    public static string Replace(this string source, string oldValue, string? newValue, StringComparison comparisonType) {
        if (source is null) {
            throw new ArgumentNullException(nameof(source));
        }

        if (oldValue is null) {
            throw new ArgumentNullException(nameof(oldValue));
        }

        if (oldValue.Length is 0) {
            throw new ArgumentException("String cannot be of zero length.", nameof(oldValue));
        }


        if (comparisonType is StringComparison.Ordinal) {
            return source.Replace(oldValue, newValue);
        }

        var result = new StringBuilder(source.Length);
        var searchIndex = 0;

        while (searchIndex < source.Length) {
            var matchIndex = source.IndexOf(oldValue, searchIndex, comparisonType);

            if (matchIndex < 0) {
                
                result.Append(source, searchIndex, source.Length - searchIndex);
                break;
            }

            
            if (matchIndex > searchIndex) {
                result.Append(source, searchIndex, matchIndex - searchIndex);
            }

            
            if (newValue is not null) {
                result.Append(newValue);
            }

            searchIndex = matchIndex + oldValue.Length;
        }

        return result.ToString();
    }

    /// <summary>
    /// Reports the zero-based index of the first occurrence of the specified character in this instance.
    /// The search starts at a specified character position and uses the specified comparison rules.
    /// </summary>
    /// <param name="source">The source string.</param>
    /// <param name="value">The character to seek.</param>
    /// <param name="comparisonType">One of the enumeration values that specifies the rules for the search.</param>
    /// <returns>The zero-based index of value if that character is found, or -1 if it is not.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    public static int IndexOf(this string source, char value, StringComparison comparisonType) {
        if (source is null) {
            throw new ArgumentNullException(nameof(source));
        }

        
        
        return source.IndexOf(value.ToString(), comparisonType);
    }
}

#endif
