



using System;
using System.Diagnostics.CodeAnalysis;

#if !NET6_0_OR_GREATER

namespace System.Diagnostics;

/// <summary>
///     Types and methods attributed with StackTraceHidden will be omitted from the stack trace text.
/// </summary>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Struct,
    Inherited = false)]
[ExcludeFromCodeCoverage]
internal sealed class StackTraceHiddenAttribute : Attribute
{
}

#endif