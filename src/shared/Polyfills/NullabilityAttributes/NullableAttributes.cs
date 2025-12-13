// Copyright (c) ANcpLua. All rights reserved.
// Licensed under the MIT License.

#pragma warning disable CA1019, RCS1251, IDE0300

namespace System.Diagnostics.CodeAnalysis;

#if !NETCOREAPP3_1_OR_GREATER

/// <summary>Specifies that null is allowed as an input even if the corresponding type disallows it.</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property, Inherited = false)]
[ExcludeFromCodeCoverage]
internal sealed class AllowNullAttribute : Attribute { }

/// <summary>Specifies that null is disallowed as an input even if the corresponding type allows it.</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property, Inherited = false)]
[ExcludeFromCodeCoverage]
internal sealed class DisallowNullAttribute : Attribute { }

/// <summary>Specifies that an output may be null even if the corresponding type disallows it.</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
[ExcludeFromCodeCoverage]
internal sealed class MaybeNullAttribute : Attribute { }

/// <summary>Specifies that an output will not be null even if the corresponding type allows it.</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
[ExcludeFromCodeCoverage]
internal sealed class NotNullAttribute : Attribute { }

/// <summary>Specifies that when a method returns <see cref="ReturnValue"/>, the parameter may be null.</summary>
[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
[ExcludeFromCodeCoverage]
internal sealed class MaybeNullWhenAttribute : Attribute
{
    public MaybeNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;
    public bool ReturnValue { get; }
}

/// <summary>Specifies that when a method returns <see cref="ReturnValue"/>, the parameter will not be null.</summary>
[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
[ExcludeFromCodeCoverage]
internal sealed class NotNullWhenAttribute : Attribute
{
    public NotNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;
    public bool ReturnValue { get; }
}

/// <summary>Specifies that the output will be non-null if the named parameter is non-null.</summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
[ExcludeFromCodeCoverage]
internal sealed class NotNullIfNotNullAttribute : Attribute
{
    public NotNullIfNotNullAttribute(string parameterName) => ParameterName = parameterName;
    public string ParameterName { get; }
}

/// <summary>Applied to a method that will never return under any circumstance.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
[ExcludeFromCodeCoverage]
internal sealed class DoesNotReturnAttribute : Attribute { }

/// <summary>Specifies that the method will not return if the associated Boolean parameter is passed the specified value.</summary>
[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
[ExcludeFromCodeCoverage]
internal sealed class DoesNotReturnIfAttribute : Attribute
{
    public DoesNotReturnIfAttribute(bool parameterValue) => ParameterValue = parameterValue;
    public bool ParameterValue { get; }
}

#endif
