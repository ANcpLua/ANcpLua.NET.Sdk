using ANcpLua.Sdk.Tests.Helpers;
using ANcpLua.Sdk.Tests.Infrastructure;

namespace ANcpLua.Sdk.Tests;

public class PolyfillActivationTests(PackageFixture fixture, ITestOutputHelper testOutputHelper)
{
    private readonly PackageFixture _fixture = fixture;
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

    public static TheoryData<IPolyfillCase> PolyfillCases =>
    [
        new PolyfillCase<TrimAttributesFile>(TargetFrameworks.NetStandard20),
        new PolyfillCase<NullabilityAttributesFile>(TargetFrameworks.NetStandard20),
        new PolyfillCase<IsExternalInitFile>(TargetFrameworks.NetStandard20),
        new PolyfillCase<RequiredMemberFile>(TargetFrameworks.NetStandard20),
        new PolyfillCase<CompilerFeatureRequiredFile>(TargetFrameworks.NetStandard20),
        new PolyfillCase<CallerArgumentExpressionFile>(TargetFrameworks.NetStandard20),
        new PolyfillCase<UnreachableExceptionFile>(TargetFrameworks.NetStandard20),
        new PolyfillCase<ExperimentalAttributeFile>(TargetFrameworks.NetStandard20),
        new PolyfillCase<IndexRangeFile>(TargetFrameworks.NetStandard20),
        new PolyfillCase<ParamCollectionFile>(TargetFrameworks.Net80),
        new PolyfillCase<StackTraceHiddenFile>(TargetFrameworks.NetStandard20),
        new PolyfillCase<LockFile>(TargetFrameworks.Net80),
        new PolyfillCase<TimeProviderFile>(TargetFrameworks.NetStandard20),
        new PolyfillCase<ThrowFile>(TargetFrameworks.NetStandard20),
        new PolyfillCase<StringOrdinalComparerFile>(TargetFrameworks.NetStandard20),
        new PolyfillCase<DiagnosticClassesFile>(TargetFrameworks.NetStandard20)
    ];

    [Theory]
    [MemberData(nameof(PolyfillCases))]
    public async Task Polyfill_Activation_Positive(IPolyfillCase testCase)
    {
        await testCase.RunPositive(_fixture, _testOutputHelper);
    }

    [Theory]
    [MemberData(nameof(PolyfillCases))]
    public async Task Polyfill_Activation_Negative(IPolyfillCase testCase)
    {
        await testCase.RunNegative(_fixture, _testOutputHelper);
    }
}
