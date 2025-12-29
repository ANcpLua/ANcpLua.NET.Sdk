// Copyright (c) ANcpLua. All rights reserved.

namespace ANcpLua.Sdk.Tests;

/// <summary>
///     Tests for C# 14 Extension Member ThrowPolyfills.
///     These tests verify that the polyfill APIs work correctly on .NET 10+
///     (where the native APIs exist, so we're testing compatibility).
/// </summary>
public class ThrowPolyfillsTests
{
    [Fact]
    public void ThrowIfNull_WithNonNullValue_DoesNotThrow()
    {
        // Arrange
        object value = "test";

        // Act & Assert - should not throw
        ArgumentNullException.ThrowIfNull(value);
    }

    [Fact]
    public void ThrowIfNull_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange
        object? value = null;

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => ArgumentNullException.ThrowIfNull(value));
        Assert.Equal("value", ex.ParamName);
    }

    [Fact]
    public void ThrowIfNull_WithCustomParamName_UsesProvidedName()
    {
        // Arrange
        object? myParameter = null;

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            ArgumentNullException.ThrowIfNull(myParameter));
        Assert.Equal("myParameter", ex.ParamName);
    }

    [Fact]
    public void ThrowIfNullOrEmpty_WithValidString_DoesNotThrow()
    {
        // Arrange
        const string value = "test";

        // Act & Assert - should not throw
        ArgumentException.ThrowIfNullOrEmpty(value);
    }

    [Fact]
    public void ThrowIfNullOrEmpty_WithNullString_ThrowsArgumentNullException()
    {
        // Arrange
        string? value = null;

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            ArgumentException.ThrowIfNullOrEmpty(value));
        Assert.Equal("value", ex.ParamName);
    }

    [Fact]
    public void ThrowIfNullOrEmpty_WithEmptyString_ThrowsArgumentException()
    {
        // Arrange
        const string value = "";

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            ArgumentException.ThrowIfNullOrEmpty(value));
        Assert.Equal("value", ex.ParamName);
    }

    [Fact]
    public void ThrowIfNullOrWhiteSpace_WithValidString_DoesNotThrow()
    {
        // Arrange
        const string value = "test";

        // Act & Assert - should not throw
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
    }

    [Fact]
    public void ThrowIfNullOrWhiteSpace_WithWhitespaceString_ThrowsArgumentException()
    {
        // Arrange
        const string value = "   ";

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            ArgumentException.ThrowIfNullOrWhiteSpace(value));
        Assert.Equal("value", ex.ParamName);
    }

    [Fact]
    public void ThrowIfNullOrWhiteSpace_WithNullString_ThrowsArgumentNullException()
    {
        // Arrange
        string? value = null;

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            ArgumentException.ThrowIfNullOrWhiteSpace(value));
        Assert.Equal("value", ex.ParamName);
    }

    [Fact]
    public void ThrowIfNegative_WithPositiveInt_DoesNotThrow()
    {
        // Arrange
        const int value = 5;

        // Act & Assert - should not throw
        ArgumentOutOfRangeException.ThrowIfNegative(value);
    }

    [Fact]
    public void ThrowIfNegative_WithZero_DoesNotThrow()
    {
        // Arrange
        const int value = 0;

        // Act & Assert - should not throw (zero is not negative)
        ArgumentOutOfRangeException.ThrowIfNegative(value);
    }

    [Fact]
    public void ThrowIfNegative_WithNegativeInt_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        const int value = -1;

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            ArgumentOutOfRangeException.ThrowIfNegative(value));
        Assert.Equal("value", ex.ParamName);
        Assert.Equal(-1, ex.ActualValue);
    }

    [Fact]
    public void ThrowIfNegative_WithNegativeLong_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        const long value = -100L;

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            ArgumentOutOfRangeException.ThrowIfNegative(value));
        Assert.Equal("value", ex.ParamName);
    }

    [Fact]
    public void ThrowIfNegative_WithNegativeDouble_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        const double value = -0.5;

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            ArgumentOutOfRangeException.ThrowIfNegative(value));
        Assert.Equal("value", ex.ParamName);
    }

    [Fact]
    public void ThrowIfZero_WithNonZeroValue_DoesNotThrow()
    {
        // Arrange
        const int value = 1;

        // Act & Assert - should not throw
        ArgumentOutOfRangeException.ThrowIfZero(value);
    }

    [Fact]
    public void ThrowIfZero_WithZero_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        const int value = 0;

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            ArgumentOutOfRangeException.ThrowIfZero(value));
        Assert.Equal("value", ex.ParamName);
        Assert.Equal(0, ex.ActualValue);
    }

    [Fact]
    public void ThrowIfNegativeOrZero_WithPositiveValue_DoesNotThrow()
    {
        // Arrange
        const int value = 1;

        // Act & Assert - should not throw
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
    }

    [Fact]
    public void ThrowIfNegativeOrZero_WithZero_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        const int value = 0;

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value));
        Assert.Equal("value", ex.ParamName);
    }

    [Fact]
    public void ThrowIfNegativeOrZero_WithNegativeValue_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        const int value = -5;

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value));
        Assert.Equal("value", ex.ParamName);
    }

    [Fact]
    public void ThrowIfGreaterThan_WithValueLessThanLimit_DoesNotThrow()
    {
        // Arrange
        const int value = 5;
        const int limit = 10;

        // Act & Assert - should not throw
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, limit);
    }

    [Fact]
    public void ThrowIfGreaterThan_WithValueEqualToLimit_DoesNotThrow()
    {
        // Arrange
        const int value = 10;
        const int limit = 10;

        // Act & Assert - should not throw (equal is okay)
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, limit);
    }

    [Fact]
    public void ThrowIfGreaterThan_WithValueGreaterThanLimit_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        const int value = 15;
        const int limit = 10;

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, limit));
        Assert.Equal("value", ex.ParamName);
    }

    [Fact]
    public void ThrowIfLessThan_WithValueGreaterThanLimit_DoesNotThrow()
    {
        // Arrange
        const int value = 15;
        const int limit = 10;

        // Act & Assert - should not throw
        ArgumentOutOfRangeException.ThrowIfLessThan(value, limit);
    }

    [Fact]
    public void ThrowIfLessThan_WithValueEqualToLimit_DoesNotThrow()
    {
        // Arrange
        const int value = 10;
        const int limit = 10;

        // Act & Assert - should not throw (equal is okay)
        ArgumentOutOfRangeException.ThrowIfLessThan(value, limit);
    }

    [Fact]
    public void ThrowIfLessThan_WithValueLessThanLimit_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        const int value = 5;
        const int limit = 10;

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            ArgumentOutOfRangeException.ThrowIfLessThan(value, limit));
        Assert.Equal("value", ex.ParamName);
    }

    [Fact]
    public void ThrowIf_WithFalseCondition_DoesNotThrow()
    {
        // Arrange
        const bool isDisposed = false;
        var instance = new object();

        // Act & Assert - should not throw
        ObjectDisposedException.ThrowIf(isDisposed, instance);
    }

    [Fact]
    public void ThrowIf_WithTrueCondition_ThrowsObjectDisposedException()
    {
        // Arrange
        const bool isDisposed = true;
        var instance = new object();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() =>
            ObjectDisposedException.ThrowIf(isDisposed, instance));
    }

    [Fact]
    public void ThrowIf_WithType_ThrowsObjectDisposedException()
    {
        // Arrange
        const bool isDisposed = true;

        // Act & Assert
        var ex = Assert.Throws<ObjectDisposedException>(() =>
            ObjectDisposedException.ThrowIf(isDisposed, typeof(string)));
        Assert.Contains("String", ex.ObjectName);
    }

    [Fact]
    public void ThrowIfNull_CapturesCorrectParameterName()
    {
        // Arrange
        object? myVerySpecificParameterName = null;

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            ArgumentNullException.ThrowIfNull(myVerySpecificParameterName));

        // The CallerArgumentExpression should capture the variable name
        Assert.Equal("myVerySpecificParameterName", ex.ParamName);
    }

    [Fact]
    public void ThrowIfNegative_CapturesCorrectParameterName()
    {
        // Arrange
        const int userCount = -1;

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            ArgumentOutOfRangeException.ThrowIfNegative(userCount));

        Assert.Equal("userCount", ex.ParamName);
    }

}