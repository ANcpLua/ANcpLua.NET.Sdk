// Copyright (c) Microsoft. All rights reserved.
// Polyfill for trimming attributes on legacy TFMs

#if NETSTANDARD2_0 || NET472

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class, Inherited = false)]
    internal sealed class RequiresUnreferencedCodeAttribute : Attribute
    {
        public RequiresUnreferencedCodeAttribute(string message) => Message = message;
        public string Message { get; }
        public string? Url { get; set; }
    }

    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    internal sealed class DynamicallyAccessedMembersAttribute : Attribute
    {
        public DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberTypes memberTypes) => MemberTypes = memberTypes;
        public DynamicallyAccessedMemberTypes MemberTypes { get; }
    }

    [Flags]
    internal enum DynamicallyAccessedMemberTypes
    {
        None = 0,
        PublicParameterlessConstructor = 0x0001,
        PublicConstructors = 0x0003,
        NonPublicConstructors = 0x0004,
        PublicMethods = 0x0008,
        NonPublicMethods = 0x0010,
        PublicFields = 0x0020,
        NonPublicFields = 0x0040,
        PublicNestedTypes = 0x0080,
        NonPublicNestedTypes = 0x0100,
        PublicProperties = 0x0200,
        NonPublicProperties = 0x0400,
        PublicEvents = 0x0800,
        NonPublicEvents = 0x1000,
        Interfaces = 0x2000,
        All = ~None
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
    internal sealed class RequiresDynamicCodeAttribute : Attribute
    {
        public RequiresDynamicCodeAttribute(string message) => Message = message;
        public string Message { get; }
        public string? Url { get; set; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter |
        AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Method |
        AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct,
        Inherited = false)]
    internal sealed class DynamicDependencyAttribute : Attribute
    {
        public DynamicDependencyAttribute(string memberSignature) => MemberSignature = memberSignature;
        public DynamicDependencyAttribute(string memberSignature, Type type) { MemberSignature = memberSignature; Type = type; }
        public DynamicDependencyAttribute(string memberSignature, string typeName, string assemblyName) { MemberSignature = memberSignature; TypeName = typeName; AssemblyName = assemblyName; }
        public DynamicDependencyAttribute(DynamicallyAccessedMemberTypes memberTypes, Type type) { MemberTypes = memberTypes; Type = type; }
        public DynamicDependencyAttribute(DynamicallyAccessedMemberTypes memberTypes, string typeName, string assemblyName) { MemberTypes = memberTypes; TypeName = typeName; AssemblyName = assemblyName; }
        public string? MemberSignature { get; }
        public DynamicallyAccessedMemberTypes MemberTypes { get; }
        public Type? Type { get; }
        public string? TypeName { get; }
        public string? AssemblyName { get; }
        public string? Condition { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    internal sealed class MemberNotNullAttribute : Attribute
    {
        public MemberNotNullAttribute(string member) => Members = new[] { member };
        public MemberNotNullAttribute(params string[] members) => Members = members;
        public string[] Members { get; }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    internal sealed class MemberNotNullWhenAttribute : Attribute
    {
        public MemberNotNullWhenAttribute(bool returnValue, string member) { ReturnValue = returnValue; Members = new[] { member }; }
        public MemberNotNullWhenAttribute(bool returnValue, params string[] members) { ReturnValue = returnValue; Members = members; }
        public bool ReturnValue { get; }
        public string[] Members { get; }
    }
}

#endif
