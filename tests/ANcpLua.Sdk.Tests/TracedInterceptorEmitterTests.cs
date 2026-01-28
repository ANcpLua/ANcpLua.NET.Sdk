using System.Collections.Immutable;
using ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister.Emitters;
using ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ANcpLua.Sdk.Tests;

public sealed class TracedInterceptorEmitterTests
{
    private static InterceptableLocation CreateFakeLocation()
    {
        // Create a minimal syntax tree and semantic model for testing
        var tree = CSharpSyntaxTree.ParseText("""
            class C
            {
                void M() { Test(); }
                void Test() { }
            }
            """);

        var compilation = CSharpCompilation.Create("Test",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        var semanticModel = compilation.GetSemanticModel(tree);
        var root = tree.GetRoot();
        var invocation = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax>()
            .First();

        return semanticModel.GetInterceptableLocation(invocation)!;
    }

    /// <summary>
    /// Bug 1: ActivitySource name collision when sanitized names are identical.
    /// "MyApp.Orders" → "MyApp_Orders"
    /// "MyApp_Orders" → "MyApp_Orders"  (collision!)
    /// </summary>
    [Fact]
    public void ActivitySourceNameCollision_SanitizedNamesDuplicate_GeneratesUniqueFieldNames()
    {
        // Arrange - two different ActivitySource names that sanitize to the same field name
        var location = CreateFakeLocation();

        var invocations = new[]
        {
            new TracedInvocationInfo(
                OrderKey: "1",
                ActivitySourceName: "MyApp.Orders",  // Sanitizes to MyApp_Orders
                SpanName: "ProcessOrder",
                SpanKind: "Internal",
                ContainingTypeName: "OrderDemo.ServiceA",
                MethodName: "MethodA",
                IsStatic: false,
                IsAsync: false,
                ReturnTypeName: "void",
                ParameterTypes: [],
                ParameterNames: [],
                TracedTags: [],
                TypeParameters: [],
                InterceptableLocation: location),
            new TracedInvocationInfo(
                OrderKey: "2",
                ActivitySourceName: "MyApp_Orders",  // Also sanitizes to MyApp_Orders - COLLISION!
                SpanName: "SubmitOrder",
                SpanKind: "Internal",
                ContainingTypeName: "OrderDemo.ServiceB",
                MethodName: "MethodB",
                IsStatic: false,
                IsAsync: false,
                ReturnTypeName: "void",
                ParameterTypes: [],
                ParameterNames: [],
                TracedTags: [],
                TypeParameters: [],
                InterceptableLocation: location)
        }.ToImmutableArray();

        // Act
        var result = TracedInterceptorEmitter.Emit(invocations);

        // Assert - should NOT have duplicate field names
        // First: MyApp_Orders = new("MyApp.Orders")
        // Second: MyApp_Orders_1 = new("MyApp_Orders")  - must be unique!
        Assert.Contains("MyApp_Orders = new(\"MyApp.Orders\")", result);
        Assert.Contains("MyApp_Orders_1 = new(\"MyApp_Orders\")", result);

        // Verify no duplicate field declarations
        var fieldCount = result.Split("internal static readonly").Length - 1;
        Assert.Equal(2, fieldCount);
    }

    /// <summary>
    /// Bug 2: Return type generic arguments are not fully qualified.
    /// Task<OrderDemo.Order> → global::System.Threading.Tasks.Task<OrderDemo.Order>
    ///                                                          ^^^^^^^^^^^^^^^^
    ///                                                          Missing global::!
    /// </summary>
    [Fact]
    public void GenericReturnType_InnerTypeNotQualified_ShouldBeFullyQualified()
    {
        // Arrange
        var location = CreateFakeLocation();

        var invocations = new[]
        {
            new TracedInvocationInfo(
                OrderKey: "1",
                ActivitySourceName: "OrderService",
                SpanName: "GetOrder",
                SpanKind: "Internal",
                ContainingTypeName: "OrderDemo.OrderService",
                MethodName: "GetOrderAsync",
                IsStatic: false,
                IsAsync: true,
                ReturnTypeName: "System.Threading.Tasks.Task<OrderDemo.Order>",
                ParameterTypes: ["string"],
                ParameterNames: ["orderId"],
                TracedTags: [],
                TypeParameters: [],
                InterceptableLocation: location)
        }.ToImmutableArray();

        // Act
        var result = TracedInterceptorEmitter.Emit(invocations);

        // Assert - the inner type should also be fully qualified with global::
        // Expected: global::System.Threading.Tasks.Task<global::OrderDemo.Order>
        // Current bug: global::System.Threading.Tasks.Task<OrderDemo.Order>
        Assert.Contains("global::OrderDemo.Order", result);
        Assert.DoesNotContain("<OrderDemo.Order>", result);  // Should not have unqualified inner type
    }

    /// <summary>
    /// Test nested generic types are fully qualified.
    /// Dictionary<string, OrderDemo.Order> → global::System.Collections.Generic.Dictionary<global::System.String, global::OrderDemo.Order>
    /// </summary>
    [Fact]
    public void NestedGenericReturnType_AllTypesFullyQualified()
    {
        // Arrange
        var location = CreateFakeLocation();

        var invocations = new[]
        {
            new TracedInvocationInfo(
                OrderKey: "1",
                ActivitySourceName: "OrderService",
                SpanName: "GetOrders",
                SpanKind: "Internal",
                ContainingTypeName: "OrderDemo.OrderService",
                MethodName: "GetOrdersAsync",
                IsStatic: false,
                IsAsync: true,
                ReturnTypeName: "System.Threading.Tasks.Task<System.Collections.Generic.Dictionary<string, OrderDemo.Order>>",
                ParameterTypes: [],
                ParameterNames: [],
                TracedTags: [],
                TypeParameters: [],
                InterceptableLocation: location)
        }.ToImmutableArray();

        // Act
        var result = TracedInterceptorEmitter.Emit(invocations);

        // Assert - all types in the generic chain should be qualified
        Assert.Contains("global::System.Threading.Tasks.Task", result);
        Assert.Contains("global::System.Collections.Generic.Dictionary", result);
        Assert.Contains("global::System.String", result);
        Assert.Contains("global::OrderDemo.Order", result);
    }

    [Fact]
    public void StaticMethod_NoThisParameter()
    {
        // Arrange
        var location = CreateFakeLocation();

        var invocations = new[]
        {
            new TracedInvocationInfo(
                OrderKey: "1",
                ActivitySourceName: "Utilities",
                SpanName: "Calculate",
                SpanKind: "Internal",
                ContainingTypeName: "Utils.Calculator",
                MethodName: "Add",
                IsStatic: true,
                IsAsync: false,
                ReturnTypeName: "int",
                ParameterTypes: ["int", "int"],
                ParameterNames: ["a", "b"],
                TracedTags: [],
                TypeParameters: [],
                InterceptableLocation: location)
        }.ToImmutableArray();

        // Act
        var result = TracedInterceptorEmitter.Emit(invocations);

        // Assert - static methods should not have 'this' parameter
        Assert.DoesNotContain("this global::", result);
        Assert.Contains("global::Utils.Calculator.Add(a, b)", result);
    }

    [Fact]
    public void GenericMethod_SingleTypeParameter_GeneratesCorrectSignature()
    {
        // Arrange
        var location = CreateFakeLocation();

        var invocations = new[]
        {
            new TracedInvocationInfo(
                OrderKey: "1",
                ActivitySourceName: "MyApp.Generic",
                SpanName: "GetValue",
                SpanKind: "Internal",
                ContainingTypeName: "GenericService",
                MethodName: "GetValue",
                IsStatic: false,
                IsAsync: false,
                ReturnTypeName: "T",
                ParameterTypes: ["string"],
                ParameterNames: ["key"],
                TracedTags: [new TracedTagInfo("key", "key", SkipIfNull: true, IsNullable: true)],
                TypeParameters: [new TypeParameterInfo("T", "where T : new()")],
                InterceptableLocation: location)
        }.ToImmutableArray();

        // Act
        var result = TracedInterceptorEmitter.Emit(invocations);

        // Assert - should have type parameter and constraints
        Assert.Contains("Intercept_Traced_0<T>", result);
        Assert.Contains("where T : new()", result);
        Assert.Contains("@this.GetValue<T>(key)", result);
        // T should NOT be global:: prefixed since it's a type parameter
        Assert.DoesNotContain("global::T", result);
    }

    [Fact]
    public void GenericMethod_MultipleTypeParameters_GeneratesCorrectSignature()
    {
        // Arrange
        var location = CreateFakeLocation();

        var invocations = new[]
        {
            new TracedInvocationInfo(
                OrderKey: "1",
                ActivitySourceName: "MyApp.Generic",
                SpanName: "Transform",
                SpanKind: "Internal",
                ContainingTypeName: "GenericService",
                MethodName: "Transform",
                IsStatic: false,
                IsAsync: false,
                ReturnTypeName: "TResult",
                ParameterTypes: ["TInput"],
                ParameterNames: ["input"],
                TracedTags: [],
                TypeParameters:
                [
                    new TypeParameterInfo("TInput", null),
                    new TypeParameterInfo("TResult", "where TResult : class")
                ],
                InterceptableLocation: location)
        }.ToImmutableArray();

        // Act
        var result = TracedInterceptorEmitter.Emit(invocations);

        // Assert
        Assert.Contains("Intercept_Traced_0<TInput, TResult>", result);
        Assert.Contains("where TResult : class", result);
        Assert.Contains("TInput input", result);  // Parameter type is type parameter
        Assert.Contains("@this.Transform<TInput, TResult>(input)", result);
        // Neither TInput nor TResult should be global:: prefixed
        Assert.DoesNotContain("global::TInput", result);
        Assert.DoesNotContain("global::TResult", result);
    }

    [Fact]
    public void GenericMethod_AsyncWithTypeParameter_GeneratesCorrectCode()
    {
        // Arrange
        var location = CreateFakeLocation();

        var invocations = new[]
        {
            new TracedInvocationInfo(
                OrderKey: "1",
                ActivitySourceName: "MyApp.Generic",
                SpanName: "GetAsync",
                SpanKind: "Internal",
                ContainingTypeName: "GenericService",
                MethodName: "GetAsync",
                IsStatic: false,
                IsAsync: true,
                ReturnTypeName: "System.Threading.Tasks.Task<T>",
                ParameterTypes: ["string"],
                ParameterNames: ["id"],
                TracedTags: [],
                TypeParameters: [new TypeParameterInfo("T", "where T : class, new()")],
                InterceptableLocation: location)
        }.ToImmutableArray();

        // Act
        var result = TracedInterceptorEmitter.Emit(invocations);

        // Assert
        Assert.Contains("async global::System.Threading.Tasks.Task<T> Intercept_Traced_0<T>", result);
        Assert.Contains("where T : class, new()", result);
        Assert.Contains("await @this.GetAsync<T>(id)", result);
    }

    [Fact]
    public void GenericMethod_StaticWithTypeParameter_GeneratesCorrectCode()
    {
        // Arrange
        var location = CreateFakeLocation();

        var invocations = new[]
        {
            new TracedInvocationInfo(
                OrderKey: "1",
                ActivitySourceName: "MyApp.Utilities",
                SpanName: "Parse",
                SpanKind: "Internal",
                ContainingTypeName: "Utils.Parser",
                MethodName: "Parse",
                IsStatic: true,
                IsAsync: false,
                ReturnTypeName: "T",
                ParameterTypes: ["string"],
                ParameterNames: ["json"],
                TracedTags: [],
                TypeParameters: [new TypeParameterInfo("T", null)],
                InterceptableLocation: location)
        }.ToImmutableArray();

        // Act
        var result = TracedInterceptorEmitter.Emit(invocations);

        // Assert - static method call should include type parameter
        Assert.Contains("global::Utils.Parser.Parse<T>(json)", result);
        Assert.DoesNotContain("this global::", result);  // No 'this' for static
    }
}
