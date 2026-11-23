using NoComments.Analyzers.Tests.Helpers;
using Shouldly;

namespace NoComments.Analyzers.Tests;

public class ConsumerProjectShould(PackageFixture fixture, ITestOutputHelper testOutputHelper)
    : NoCommentsTestBase(fixture, testOutputHelper)
{
    [Fact]
    public async Task BuildWithTheConsumerSdkVersionTheRunRequests()
    {
        var requested = Version.Parse(
            Environment.GetEnvironmentVariable("NOCOMMENTS_CONSUMER_SDK_VERSION") is { Length: > 0 } fromEnvironment
                ? fromEnvironment
                : "10.0.100");
        using var project = await CreateProjectBuilderAsync();

        var actual = Version.Parse((await project.GetSdkVersionAsync()).Split('-')[0]);

        actual.Major.ShouldBe(requested.Major);
        actual.Minor.ShouldBe(requested.Minor);
        actual.Build.ShouldBeGreaterThanOrEqualTo(requested.Build);
    }
}
