namespace MyApp.Tests;

public sealed class SmokeTests
{
    [Fact]
    public void EntryPoint_Exists()
    {
        typeof(Program).Should().NotBeNull();
    }
}
