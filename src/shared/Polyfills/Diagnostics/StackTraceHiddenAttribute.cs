// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Source: https://github.com/Tenacom/PolyKit

#if !NET6_0_OR_GREATER

namespace System.Diagnostics;

/// <summary>
/// Types and methods attributed with StackTraceHidden will be omitted from the stack trace text.
/// </summary>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Struct,
    Inherited = false)]
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
internal sealed class StackTraceHiddenAttribute : Attribute
{
}

#endif
