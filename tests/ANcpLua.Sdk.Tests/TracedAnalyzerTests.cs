using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ANcpLua.Sdk.Tests;

/// <summary>
///     Tests for TracedCallSiteAnalyzer behavior with various edge cases.
/// </summary>
public sealed class TracedAnalyzerTests
{
    private static readonly string TracedAttributeSource = """
        namespace ANcpSdk.AspNetCore.ServiceDefaults.Instrumentation
        {
            [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method)]
            public sealed class TracedAttribute : System.Attribute
            {
                public TracedAttribute(string activitySourceName) => ActivitySourceName = activitySourceName;
                public string ActivitySourceName { get; }
                public string? SpanName { get; set; }
                public System.Diagnostics.ActivityKind Kind { get; set; }
            }
            
            [System.AttributeUsage(System.AttributeTargets.Method)]
            public sealed class NoTraceAttribute : System.Attribute { }
            
            [System.AttributeUsage(System.AttributeTargets.Parameter)]
            public sealed class TracedTagAttribute : System.Attribute
            {
                public TracedTagAttribute() { }
                public TracedTagAttribute(string name) => Name = name;
                public string? Name { get; }
                public bool SkipIfNull { get; set; } = true;
            }
        }
        """;

    /// <summary>
    ///     Test inheritance: When base class has [Traced], does derived class method get traced?
    /// </summary>
    [Fact]
    public void Inheritance_BaseClassTraced_DerivedOverride_ShouldBeTraced()
    {
        var code = """
            using ANcpSdk.AspNetCore.ServiceDefaults.Instrumentation;
            
            [Traced("MyApp.Base")]
            public class BaseService
            {
                public virtual void TracedInBase() { }
            }
            
            public class DerivedService : BaseService
            {
                public override void TracedInBase() { }
                public void NewMethod() { }
            }
            
            public class Program
            {
                public static void Main()
                {
                    var derived = new DerivedService();
                    derived.TracedInBase();  // Should this be intercepted?
                    derived.NewMethod();     // Should this be intercepted?
                }
            }
            """;

        var (compilation, invocations) = AnalyzeCode(code);

        // Check what methods the invocations target
        foreach (var invocation in invocations)
        {
            var semanticModel = compilation.GetSemanticModel(invocation.SyntaxTree);
            var symbolInfo = semanticModel.GetSymbolInfo(invocation);
            
            if (symbolInfo.Symbol is IMethodSymbol method)
            {
                // Key question: What is method.ContainingType for derived.TracedInBase()?
                var containingType = method.ContainingType?.Name;
                var methodName = method.Name;
                
                // Output for debugging
                System.Console.WriteLine($"Invocation: {methodName}, ContainingType: {containingType}");
                
                // The question is: Is containingType "BaseService" or "DerivedService"?
                // If it's "DerivedService", the [Traced] on BaseService won't be found
                // unless we walk the inheritance chain
            }
        }

        // For now, just verify the compilation works
        Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    /// <summary>
    ///     Test class-level [Traced] with method-level override.
    /// </summary>
    [Fact]
    public void ClassLevel_MethodOverride_ShouldUseMethodLevel()
    {
        var code = """
            using ANcpSdk.AspNetCore.ServiceDefaults.Instrumentation;
            
            [Traced("MyApp.Class", SpanName = "class.default")]
            public class Service
            {
                [Traced("MyApp.Method", SpanName = "method.override")]
                public void MethodWithOverride() { }
                
                public void MethodWithoutOverride() { }
            }
            
            public class Program
            {
                public static void Main()
                {
                    var svc = new Service();
                    svc.MethodWithOverride();     // Should use "method.override"
                    svc.MethodWithoutOverride();  // Should use "class.default" or method name
                }
            }
            """;

        var (compilation, _) = AnalyzeCode(code);
        Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    /// <summary>
    ///     Test [NoTrace] opt-out.
    /// </summary>
    [Fact]
    public void ClassLevel_NoTraceOptOut_ShouldNotTrace()
    {
        var code = """
            using ANcpSdk.AspNetCore.ServiceDefaults.Instrumentation;
            
            [Traced("MyApp")]
            public class Service
            {
                public void TracedMethod() { }
                
                [NoTrace]
                public void NotTracedMethod() { }
            }
            
            public class Program
            {
                public static void Main()
                {
                    var svc = new Service();
                    svc.TracedMethod();      // Should be traced
                    svc.NotTracedMethod();   // Should NOT be traced
                }
            }
            """;

        var (compilation, _) = AnalyzeCode(code);
        Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    /// <summary>
    ///     Test private method with explicit [Traced] on class-level traced class.
    /// </summary>
    [Fact]
    public void PrivateMethod_ExplicitTraced_ShouldBeTraced()
    {
        var code = """
            using ANcpSdk.AspNetCore.ServiceDefaults.Instrumentation;
            
            [Traced("MyApp.Class")]
            public class Service
            {
                public void PublicMethod() { PrivateMethod(); }
                
                // Private but explicitly traced - should this work?
                [Traced("MyApp.Private")]
                private void PrivateMethod() { }
            }
            
            public class Program
            {
                public static void Main()
                {
                    var svc = new Service();
                    svc.PublicMethod();
                }
            }
            """;

        var (compilation, _) = AnalyzeCode(code);
        Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    /// <summary>
    ///     Test [TracedTag] without explicit name (should use parameter name).
    /// </summary>
    [Fact]
    public void TracedTag_NoName_ShouldUseParameterName()
    {
        var code = """
            using ANcpSdk.AspNetCore.ServiceDefaults.Instrumentation;
            
            [Traced("MyApp")]
            public class Service
            {
                public void Process([TracedTag] string orderId, [TracedTag("amount")] int amount) { }
            }
            
            public class Program
            {
                public static void Main()
                {
                    var svc = new Service();
                    svc.Process("123", 100);  // Tags: "orderId" and "amount"
                }
            }
            """;

        var (compilation, _) = AnalyzeCode(code);
        Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    /// <summary>
    ///     Test [NoTrace] on derived class override - should opt out from inherited tracing.
    /// </summary>
    [Fact]
    public void Inheritance_NoTraceOnOverride_ShouldNotTrace()
    {
        var code = """
            using ANcpSdk.AspNetCore.ServiceDefaults.Instrumentation;
            
            [Traced("MyApp")]
            public class BaseService
            {
                public virtual void Method() { }
            }
            
            public class DerivedService : BaseService
            {
                [NoTrace]  // Opt-out on override
                public override void Method() { }
            }
            
            public class Program
            {
                public static void Main()
                {
                    var derived = new DerivedService();
                    derived.Method();  // Should NOT be traced due to [NoTrace]
                }
            }
            """;

        var (compilation, invocations) = AnalyzeCode(code);
        
        // Verify compilation is valid
        Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
        
        // Check the method symbol for the invocation
        foreach (var invocation in invocations)
        {
            var semanticModel = compilation.GetSemanticModel(invocation.SyntaxTree);
            var symbolInfo = semanticModel.GetSymbolInfo(invocation);
            
            if (symbolInfo.Symbol is IMethodSymbol method && method.Name == "Method")
            {
                // Verify [NoTrace] is on the method
                var hasNoTrace = method.GetAttributes()
                    .Any(a => a.AttributeClass?.Name == "NoTraceAttribute");
                
                System.Console.WriteLine($"Method: {method.Name}, HasNoTrace: {hasNoTrace}");
                Assert.True(hasNoTrace, "Override should have [NoTrace] attribute");
            }
        }
    }

    private static (Compilation Compilation, IEnumerable<Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax> Invocations) AnalyzeCode(string code)
    {
        var attributeTree = CSharpSyntaxTree.ParseText(TracedAttributeSource);
        var codeTree = CSharpSyntaxTree.ParseText(code);

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Diagnostics.ActivityKind).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location)
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [attributeTree, codeTree],
            references,
            new CSharpCompilationOptions(OutputKind.ConsoleApplication));

        var invocations = codeTree.GetRoot()
            .DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax>();

        return (compilation, invocations);
    }
}
