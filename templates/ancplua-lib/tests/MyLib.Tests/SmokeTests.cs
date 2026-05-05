namespace MyLib.Tests;

public sealed class SmokeTests
{
    [Fact]
    public void Greet_Returns_NonEmpty()
    {
        Greeter.Greet("world").Should().NotBeNullOrWhiteSpace();
    }
}
