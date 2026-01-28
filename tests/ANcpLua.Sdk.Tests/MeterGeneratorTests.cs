using System.Collections.Immutable;
using ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister.Emitters;
using ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister.Models;

namespace ANcpLua.Sdk.Tests;

public sealed class MeterGeneratorTests
{
    [Fact]
    public void Counter_WithAllParameters_GeneratesCorrectCode()
    {
        // Arrange
        var meters = new[]
        {
            new MeterClassInfo(
                OrderKey: "test",
                Namespace: "TestNamespace",
                ClassName: "OrderMetrics",
                MeterName: "OrderService",
                MeterVersion: null,
                Methods:
                [
                    new MetricMethodInfo(
                        MethodName: "RecordOrder",
                        Kind: MetricKind.Counter,
                        MetricName: "orders.created",
                        Unit: "{order}",
                        Description: "Orders created",
                        ValueTypeName: null,
                        Tags: [new MetricTagInfo("status", "status", "string")])
                ])
        }.ToImmutableArray();

        // Act
        var result = MeterEmitter.Emit(meters);

        // Assert
        Assert.Contains("CreateCounter<long>(\"orders.created\", \"{order}\", \"Orders created\")", result);
        Assert.Contains("_ordersCreated.Add(1, new KeyValuePair<string, object?>(\"status\", status))", result);
    }

    [Fact]
    public void Counter_WithOnlyName_GeneratesCorrectCode()
    {
        // Arrange
        var meters = new[]
        {
            new MeterClassInfo(
                OrderKey: "test",
                Namespace: "TestNamespace",
                ClassName: "SimpleMetrics",
                MeterName: "SimpleService",
                MeterVersion: null,
                Methods:
                [
                    new MetricMethodInfo(
                        MethodName: "RecordOrder",
                        Kind: MetricKind.Counter,
                        MetricName: "orders.created",
                        Unit: null,
                        Description: null,
                        ValueTypeName: null,
                        Tags: [])
                ])
        }.ToImmutableArray();

        // Act
        var result = MeterEmitter.Emit(meters);

        // Assert
        Assert.Contains("CreateCounter<long>(\"orders.created\")", result);  // Clean: no trailing nulls
        Assert.Contains("_ordersCreated.Add(1)", result);
    }

    [Fact]
    public void Histogram_WithValue_GeneratesCorrectCode()
    {
        // Arrange
        var meters = new[]
        {
            new MeterClassInfo(
                OrderKey: "test",
                Namespace: "TestNamespace",
                ClassName: "DurationMetrics",
                MeterName: "TimingService",
                MeterVersion: null,
                Methods:
                [
                    new MetricMethodInfo(
                        MethodName: "Record",
                        Kind: MetricKind.Histogram,
                        MetricName: "duration",
                        Unit: "ms",
                        Description: null,
                        ValueTypeName: "double",
                        Tags: [new MetricTagInfo("type", "type", "string")])
                ])
        }.ToImmutableArray();

        // Act
        var result = MeterEmitter.Emit(meters);

        // Assert
        Assert.Contains("CreateHistogram<double>(\"duration\", \"ms\")", result);  // Clean: no trailing null
        Assert.Contains("_duration.Record(value, new KeyValuePair<string, object?>(\"type\", type))", result);
    }

    [Fact]
    public void MultipleTags_GeneratesTagsArray()
    {
        // Arrange
        var meters = new[]
        {
            new MeterClassInfo(
                OrderKey: "test",
                Namespace: "TestNamespace",
                ClassName: "RequestMetrics",
                MeterName: "HttpService",
                MeterVersion: null,
                Methods:
                [
                    new MetricMethodInfo(
                        MethodName: "Record",
                        Kind: MetricKind.Counter,
                        MetricName: "requests",
                        Unit: null,
                        Description: null,
                        ValueTypeName: null,
                        Tags:
                        [
                            new MetricTagInfo("m", "method", "string"),
                            new MetricTagInfo("p", "path", "string")
                        ])
                ])
        }.ToImmutableArray();

        // Act
        var result = MeterEmitter.Emit(meters);

        // Assert
        Assert.Contains("var tags = new KeyValuePair<string, object?>[]", result);
        Assert.Contains("new(\"method\", m)", result);
        Assert.Contains("new(\"path\", p)", result);
        Assert.Contains("_requests.Add(1, tags)", result);
    }

    [Fact]
    public void MeterWithVersion_IncludesVersionInConstructor()
    {
        // Arrange
        var meters = new[]
        {
            new MeterClassInfo(
                OrderKey: "test",
                Namespace: "TestNamespace",
                ClassName: "VersionedMetrics",
                MeterName: "VersionedService",
                MeterVersion: "1.0.0",
                Methods:
                [
                    new MetricMethodInfo(
                        MethodName: "Count",
                        Kind: MetricKind.Counter,
                        MetricName: "events",
                        Unit: null,
                        Description: null,
                        ValueTypeName: null,
                        Tags: [])
                ])
        }.ToImmutableArray();

        // Act
        var result = MeterEmitter.Emit(meters);

        // Assert
        Assert.Contains("new Meter(\"VersionedService\", \"1.0.0\")", result);
    }

    [Fact]
    public void Counter_WithDescriptionOnly_GeneratesNullUnit()
    {
        // Arrange - Unit=null, Description="Test description"
        var meters = new[]
        {
            new MeterClassInfo(
                OrderKey: "test",
                Namespace: "TestNamespace",
                ClassName: "DescMetrics",
                MeterName: "DescService",
                MeterVersion: null,
                Methods:
                [
                    new MetricMethodInfo(
                        MethodName: "RecordEvent",
                        Kind: MetricKind.Counter,
                        MetricName: "events",
                        Unit: null,
                        Description: "Test description",
                        ValueTypeName: null,
                        Tags: [])
                ])
        }.ToImmutableArray();

        // Act
        var result = MeterEmitter.Emit(meters);

        // Assert - Should have null for unit but description set
        Assert.Contains("CreateCounter<long>(\"events\", null, \"Test description\")", result);
    }
}
