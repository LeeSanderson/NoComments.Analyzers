using Microsoft.CodeAnalysis;
using NoComments.Analyzers.Tests.Helpers;
using Shouldly;

namespace NoComments.Analyzers.Tests;

public class RemoveCommentShould
{
    [Fact]
    public async Task RemoveACommentOnItsOwnLineTogetherWithTheLine()
    {
        var result = await SingleFixHarness.ApplySingleFixesUntilNoneRemainAsync(
            "class Greeter\n{\n    // Say hello to the user\n    public void Greet() { }\n}\n",
            DocumentationMode.Diagnose);

        result.ShouldBe("class Greeter\n{\n    public void Greet() { }\n}\n");
    }
}
