---
name: sdk-integration-testing-net10
description: |
  .NET 10 SDK-driven integration testing infrastructure. Use when:
  (1) Setting up integration tests with zero boilerplate
  (2) SDK auto-injects WebApplicationFactory + MTP v2 configuration
  (3) Questions about automatic Program.cs visibility in .NET 10
  (4) ASP0027 analyzer fires (redundant Program declaration)
  (5) UseKestrel() + StartServer() for real HTTP stack
  (6) Migrating test infrastructure to .NET 10 LTS (2026-ready)
---

```yaml
metadata:
  target: ".NET 10 LTS (2026-ready)"
  philosophy: "SDK owns everything, zero manual configuration"
  last_updated: "2025-12-31"

ecosystem:
  runtime:
    dotnet:
      version: "10"
      lts: true
      lts_since: "2025-11-11"
      target_framework: "net10.0"
    csharp:
      version: "14"

packages:
  testing:
    xunit.v3.mtp-v2:
      version: "3.2.1"
      purpose: "xUnit v3 with MTP v2 adapter"
    Meziantou.Xunit.v3.ParallelTestFramework:
      version: "1.0.6"
      purpose: "Parallel test execution"
    GitHubActionsTestLogger:
      version: "3.0.1"
      purpose: "CI test output formatting"
    Microsoft.Extensions.Diagnostics.Testing:
      version: "10.1.0"
      purpose: "FakeLogger, FakeTimeProvider"
    AwesomeAssertions:
      version: "9.3.0"
      purpose: "Fluent assertions (Apache 2.0 fork)"
    AwesomeAssertions.Analyzers:
      version: "9.0.8"
      purpose: "Best practices analyzers"

  integration_testing:
    Microsoft.AspNetCore.Mvc.Testing:
      version: "10.0.1"
      purpose: "WebApplicationFactory"
      injected_when: "Test project references *.Web.csproj"

sdk_design:
  principle: "Test projects use ANcpLua.NET.Sdk, NOT ANcpLua.NET.Sdk.Web"

  detection_heuristics:
    is_test_project:
      - "Folder path contains 'Tests' or 'Test'"
      - "Project name ends with '.Tests' or '.Test'"
      - "Has PackageReference to xunit.*"
    is_integration_test:
      - "ProjectReference to *.Web.csproj or *.Api.csproj"
      - "Folder path contains 'Integration' or 'E2E'"
      - "Has PackageReference to Mvc.Testing"

  auto_injection:
    all_test_projects:
      properties:
        OutputType: "Exe"
        TestingPlatformDotnetTestSupport: "true"
        UseMicrosoftTestingPlatformRunner: "true"
      packages:
        - "xunit.v3.mtp-v2"
        - "Meziantou.Xunit.v3.ParallelTestFramework"

    integration_test_projects:
      packages:
        - "Microsoft.AspNetCore.Mvc.Testing"
      properties:
        NoDefaultLaunchSettingsFile: "true"

net10_automatic_behavior:
  program_visibility:
    description: "Source generator makes Program public automatically"
    result: "WebApplicationFactory<Program> works without declaration"
    analyzer:
      id: "ASP0027"
      severity: "error"
      message: "Remove redundant public partial class Program { }"

  banned_patterns:
    - pattern: "public partial class Program { }"
      reason: "Redundant in .NET 10"
      analyzer_id: "ASP0027"
    - pattern: "InternalsVisibleTo.*Program"
      reason: "Program is public by default"
    - pattern: "Microsoft.NET.Test.Sdk"
      reason: "MTP replaces VSTest"
    - pattern: "FluentAssertions"
      reason: "Use AwesomeAssertions"

webapplicationfactory:
  testserver:
    method: "CreateClient()"
    use_when: "Basic HTTP assertions"

  real_kestrel:
    method: "UseKestrel(0) + StartServer()"
    use_when:
      - "SSE streaming"
      - "WebSockets"
      - "HTTP/2"
      - "Playwright/Selenium"
    pattern: |
      await using var app = factory
          .UseKestrel(0)
          .StartServer();

      using var client = app.CreateClient();

mtp_cli:
  net10_native: true
  separator_required: false
  examples:
    all_tests: "dotnet test"
    filter_method: "dotnet test --filter-method \"*Pattern*\""
    filter_class: "dotnet test --filter-class \"ClassName\""
    list_tests: "dotnet test --list-tests"
    trx_report: "dotnet test --report-trx"

csproj_templates:
  unit_tests: |
    <!-- Unit tests - no web reference -->
    <Project Sdk="ANcpLua.NET.Sdk">
      <ItemGroup>
        <ProjectReference Include="..\..\src\MyLib\MyLib.csproj" />
      </ItemGroup>
    </Project>

  integration_tests: |
    <!-- Integration tests - references web project -->
    <!-- SDK auto-injects Mvc.Testing -->
    <Project Sdk="ANcpLua.NET.Sdk">
      <ItemGroup>
        <ProjectReference Include="..\..\src\MyApi\MyApi.csproj" />
      </ItemGroup>
    </Project>

test_patterns:
  standard: |
    public sealed class ApiTests(WebApplicationFactory<Program> factory)
        : IClassFixture<WebApplicationFactory<Program>>
    {
        [Fact]
        public async Task Get_ReturnsSuccess()
        {
            var client = factory.CreateClient();
            var response = await client.GetAsync("/api/values");
            response.Should().BeSuccessful();
        }
    }

  real_kestrel: |
    [Fact]
    public async Task Sse_Streams()
    {
        await using var app = factory
            .UseKestrel(0)
            .StartServer();

        using var client = app.CreateClient();
        await using var stream = await client.GetStreamAsync("/events");
    }

  custom_factory: |
    public sealed class CustomFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IEmailService>();
                services.AddSingleton<IEmailService, FakeEmailService>();
            });
        }
    }

troubleshooting:
  asp0027:
    error: "ASP0027"
    fix: "Delete public partial class Program { }"

  type_load_exception:
    error: "Could not load type 'IDataConsumer'"
    cause: "xunit.v3 instead of xunit.v3.mtp-v2"
    fix: "Switch to xunit.v3.mtp-v2"

  exit_code_5:
    error: "Exit code 5"
    cause: "VSTest CLI syntax"
    fix: "Use --filter-method not --filter"

ground_truth:
  must_not_claim:
    - ".NET 10 is preview"
    - "public partial class Program { } is required"
    - "Use -- separator with dotnet test"
    - "Test projects need Microsoft.NET.Sdk.Web"
  must_assert:
    - ".NET 10 is LTS"
    - "Program visibility is automatic"
    - "Test projects use ANcpLua.NET.Sdk (not Web)"
    - "MTP is native (no separator)"
```