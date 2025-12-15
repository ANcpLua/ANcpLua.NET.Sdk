// Copyright (c) ANcpLua. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;

#if !NET9_0_OR_GREATER

namespace System.Runtime.CompilerServices
{
    /// <summary>
    ///     Indicates that a method will allow a variable number of arguments in its invocation.
    /// </summary>
    /// <remarks>
    ///     This attribute is required for C# 13 params collections feature on older TFMs.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Parameter)]
    [ExcludeFromCodeCoverage]
    internal sealed class ParamCollectionAttribute : Attribute
    {
    }
}

#endif
