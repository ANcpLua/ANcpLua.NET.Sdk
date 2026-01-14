using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister;

/// <summary>
/// Intercepts WebApplicationBuilder.Build() calls to auto-register ANcpSdk service defaults.
/// </summary>
/// <remarks>
/// See: https://github.com/dotnet/roslyn/blob/main/docs/features/interceptors.md
/// </remarks>
[Generator]
public sealed class ServiceDefaultsSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var hasServiceDefaults = context.CompilationProvider
            .Select(HasServiceDefaultsType)
            .WithTrackingName(TrackingNames.ServiceDefaultsAvailable);

        var interceptionCandidates = context.SyntaxProvider
            .CreateSyntaxProvider(IsPotentialBuildCall, TransformToBuildInterception)
            .SelectMany(AsSingletonOrEmpty)
            .WithTrackingName(TrackingNames.InterceptionCandidates)
            .Collect()
            .WithTrackingName(TrackingNames.CollectedBuildCalls);

        context.RegisterSourceOutput(
            interceptionCandidates.Combine(hasServiceDefaults),
            EmitInterceptors);
    }

    #region Pipeline Predicates

    private static bool HasServiceDefaultsType(Compilation compilation, CancellationToken _)
    {
        return compilation.GetTypeByMetadataName(MetadataNames.ServiceDefaultsClass) is not null;
    }

    private static bool IsPotentialBuildCall(SyntaxNode node, CancellationToken _)
    {
        return node.IsKind(SyntaxKind.InvocationExpression);
    }

    private static ImmutableArray<InterceptionData> AsSingletonOrEmpty(
        InterceptionData? item,
        CancellationToken _)
    {
        return item is { } data
            ? [data]
            : [];
    }

    #endregion

    #region Semantic Transform

    private static InterceptionData? TransformToBuildInterception(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        if (!TryGetBuildInvocation(context, cancellationToken, out var invocation))
            return null;

        if (!IsWebApplicationBuilderBuild(invocation, context.SemanticModel.Compilation))
            return null;

        var interceptLocation = GetInterceptableLocation(context, cancellationToken);
        if (interceptLocation is null)
            return null;

        return CreateInterceptionData(context.Node, interceptLocation);
    }

    private static bool TryGetBuildInvocation(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken,
        out IInvocationOperation invocation)
    {
        invocation = null!;

        if (context.SemanticModel.GetOperation(context.Node, cancellationToken)
            is not IInvocationOperation op)
            return false;

        invocation = op;
        return true;
    }

    private static bool IsWebApplicationBuilderBuild(
        IInvocationOperation invocation,
        Compilation compilation)
    {
        if (invocation.TargetMethod.Name != MethodNames.Build)
            return false;

        var webAppBuilderType = compilation.GetTypeByMetadataName(MetadataNames.WebApplicationBuilder);
        return SymbolEqualityComparer.Default.Equals(
            invocation.TargetMethod.ContainingType,
            webAppBuilderType);
    }

    private static InterceptableLocation? GetInterceptableLocation(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        return context.SemanticModel.GetInterceptableLocation(
            (InvocationExpressionSyntax)context.Node,
            cancellationToken);
    }

    private static InterceptionData CreateInterceptionData(
        SyntaxNode node,
        InterceptableLocation location)
    {
        return new InterceptionData
        {
            OrderKey = FormatLocationKey(node),
            Kind = InterceptionMethodKind.Build,
            InterceptableLocation = location
        };
    }

    private static string FormatLocationKey(SyntaxNode node)
    {
        var span = node.GetLocation().GetLineSpan();
        var start = span.StartLinePosition;
        return $"{node.SyntaxTree.FilePath}:{start.Line}:{start.Character}";
    }

    #endregion

    #region Code Generation

    private static void EmitInterceptors(
        SourceProductionContext context,
        (ImmutableArray<InterceptionData> Candidates, bool HasServiceDefaults) source)
    {
        if (!source.HasServiceDefaults)
            return;

        var sourceCode = BuildInterceptorsSource(source.Candidates);
        context.AddSource(OutputFileNames.Interceptors, SourceText.From(sourceCode, Encoding.UTF8));
    }

    private static string BuildInterceptorsSource(ImmutableArray<InterceptionData> candidates)
    {
        var sb = new StringBuilder();

        AppendFileHeader(sb);
        AppendInterceptsLocationAttribute(sb);
        AppendInterceptorsClassOpen(sb);
        AppendInterceptorMethods(sb, candidates);
        AppendInterceptorsClassClose(sb);

        return sb.ToString();
    }

    private static void AppendFileHeader(StringBuilder sb)
    {
        sb.AppendLine(SourceTemplates.AutoGeneratedHeader);
        sb.AppendLine(SourceTemplates.PragmaDisable);
        sb.AppendLine();
    }

    private static void AppendInterceptsLocationAttribute(StringBuilder sb)
    {
        sb.AppendLine(SourceTemplates.InterceptsLocationAttribute);
    }

    private static void AppendInterceptorsClassOpen(StringBuilder sb)
    {
        sb.AppendLine(SourceTemplates.InterceptorsNamespaceOpen);
    }

    private static void AppendInterceptorMethods(
        StringBuilder sb,
        ImmutableArray<InterceptionData> candidates)
    {
        var orderedCandidates = candidates
            .OrderBy(static c => c.OrderKey, StringComparer.Ordinal);

        var index = 0;
        foreach (var candidate in orderedCandidates)
        {
            if (candidate.Kind is InterceptionMethodKind.Build)
            {
                AppendBuildInterceptor(sb, candidate, index);
            }

            index++;
        }
    }

    private static void AppendBuildInterceptor(
        StringBuilder sb,
        InterceptionData candidate,
        int index)
    {
        var displayLocation = candidate.InterceptableLocation.GetDisplayLocation();
        var interceptAttribute = candidate.InterceptableLocation.GetInterceptsLocationAttributeSyntax();

        sb.AppendLine($$"""
                // Intercepted call at {{displayLocation}}
                {{interceptAttribute}}
                public static global::{{MetadataNames.WebApplication}} {{MethodNames.InterceptBuildPrefix}}{{index}}(
                    this global::{{MetadataNames.WebApplicationBuilder}} builder)
                {
                    builder.{{MethodNames.TryUseConventions}}();
                    var app = builder.{{MethodNames.Build}}();
                    app.{{MethodNames.MapDefaultEndpoints}}();
                    return app;
                }
        """);
    }

    private static void AppendInterceptorsClassClose(StringBuilder sb)
    {
        sb.AppendLine(SourceTemplates.InterceptorsNamespaceClose);
    }

    #endregion

    #region Constants

    /// <summary>Fully-qualified metadata names for type lookups.</summary>
    private static class MetadataNames
    {
        public const string WebApplicationBuilder = "Microsoft.AspNetCore.Builder.WebApplicationBuilder";
        public const string WebApplication = "Microsoft.AspNetCore.Builder.WebApplication";
        public const string ServiceDefaultsClass = "ANcpSdk.AspNetCore.ServiceDefaults.ANcpSdkServiceDefaults";
    }

    /// <summary>Method names used in interception and generated code.</summary>
    private static class MethodNames
    {
        public const string Build = "Build";
        public const string TryUseConventions = "TryUseANcpSdkConventions";
        public const string MapDefaultEndpoints = "MapANcpSdkDefaultEndpoints";
        public const string InterceptBuildPrefix = "Intercept_Build";
    }

    /// <summary>Tracking names for incremental generator debugging.</summary>
    private static class TrackingNames
    {
        public const string ServiceDefaultsAvailable = nameof(ServiceDefaultsAvailable);
        public const string InterceptionCandidates = nameof(InterceptionCandidates);
        public const string CollectedBuildCalls = nameof(CollectedBuildCalls);
    }

    /// <summary>Output file names for generated source.</summary>
    private static class OutputFileNames
    {
        public const string Interceptors = "Intercepts.g.cs";
    }

    /// <summary>Source code templates for generated output.</summary>
    private static class SourceTemplates
    {
        public const string AutoGeneratedHeader = "// <auto-generated/>";
        public const string PragmaDisable = "#pragma warning disable";

        public const string InterceptsLocationAttribute = """
            namespace System.Runtime.CompilerServices
            {
                [global::System.AttributeUsage(global::System.AttributeTargets.Method, AllowMultiple = true)]
                file sealed class InterceptsLocationAttribute(int version, string data) : global::System.Attribute;
            }
            """;

        public const string InterceptorsNamespaceOpen = """
            namespace ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister
            {
                using ANcpSdk.AspNetCore.ServiceDefaults;

                file static partial class Interceptors
                {
            """;

        public const string InterceptorsNamespaceClose = """
                }
            }
            """;
    }

    #endregion
}
