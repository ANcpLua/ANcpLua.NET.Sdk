// Copyright (c) Microsoft. All rights reserved.
// Polyfill for diagnostic classes on legacy TFMs
// Note: CallerArgumentExpressionAttribute is in a separate file (LanguageFeatures/CallerArgumentExpressionAttribute.cs)

#if NETSTANDARD2_0 || NET472

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Struct |
        AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property |
        AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Delegate,
        Inherited = false, AllowMultiple = false)]
    internal sealed class ExcludeFromCodeCoverageAttribute : Attribute
    {
        public string? Justification { get; set; }
    }
}

#endif
