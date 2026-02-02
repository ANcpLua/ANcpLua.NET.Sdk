using ANcpLua.Roslyn.Utilities.Testing.Aot;

namespace AotTesting.Sample;

/// <summary>
/// Sample AOT/Trim tests demonstrating the testing infrastructure.
/// These tests are compiled to native AOT or trimmed executables and run as processes.
/// </summary>
public class SampleAotTests
{
    /// <summary>
    /// Simple AOT test that verifies basic AOT compilation works.
    /// Returns 100 to indicate success (by convention).
    /// </summary>
    [AotTest]
    public static int BasicAotTest()
    {
        // Simple test: verify we can run basic operations in AOT
        var list = new List<int> { 1, 2, 3 };
        var sum = list.Sum();

        if (sum != 6)
        {
            Console.Error.WriteLine($"FAIL: Expected sum 6, got {sum}");
            return 1;
        }

        Console.WriteLine("PASS: BasicAotTest - AOT compilation works correctly");
        return 100; // Success exit code
    }

    /// <summary>
    /// Simple trim test that verifies trimming preserves essential types.
    /// </summary>
    [TrimTest(TrimMode = TrimMode.Full)]
    public static int BasicTrimTest()
    {
        // Verify essential types are preserved after trimming
        var type = typeof(List<string>);
        if (type is null)
        {
            Console.Error.WriteLine("FAIL: List<string> type was trimmed");
            return 2;
        }

        // Test basic string operations
        var text = "Hello, Trimmed World!";
        if (!text.Contains("Trimmed"))
        {
            Console.Error.WriteLine("FAIL: String.Contains not working");
            return 3;
        }

        Console.WriteLine("PASS: BasicTrimTest - Trimming preserves essential types");
        return 100;
    }

    /// <summary>
    /// AOT test with disabled feature switch for JSON reflection.
    /// </summary>
    [AotTest(DisabledFeatureSwitches = [FeatureSwitches.JsonReflection])]
    public static int AotTestWithDisabledJsonReflection()
    {
        // This test runs with JSON reflection disabled
        // System.Text.Json source generation should still work

        Console.WriteLine("PASS: AotTestWithDisabledJsonReflection - Feature switch applied");
        return 100;
    }

    /// <summary>
    /// Trim test with partial trim mode (less aggressive).
    /// </summary>
    [TrimTest(TrimMode = TrimMode.Partial)]
    public static int PartialTrimTest()
    {
        // Partial trim mode is less aggressive, preserving more types
        var dateTime = DateTime.UtcNow;
        if (dateTime == default)
        {
            Console.Error.WriteLine("FAIL: DateTime.UtcNow returned default");
            return 4;
        }

        Console.WriteLine($"PASS: PartialTrimTest - DateTime works: {dateTime}");
        return 100;
    }

    /// <summary>
    /// Test using TrimAssert to verify types are preserved.
    /// </summary>
    [TrimTest(TrimMode = TrimMode.Full)]
    public static int TrimAssertPreservedTest()
    {
        // Verify List<int> is preserved (it should be, since we use it above)
        TrimAssert.TypePreserved("System.Collections.Generic.List`1", "System.Private.CoreLib");

        Console.WriteLine("PASS: TrimAssertPreservedTest - TrimAssert.TypePreserved works");
        return 100;
    }
}
