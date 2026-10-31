using Swapping.Rules;

namespace Swapping.Rules.Tests;

public class SmokeTests
{
    [Fact]
    public void RulesAssemblyIsReachable()
    {
        Assert.Equal("Swapping.Rules", typeof(RulesAssembly).Assembly.GetName().Name);
    }
}
