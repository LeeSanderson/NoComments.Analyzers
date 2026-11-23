using NoComments.Analyzers.Tests.Helpers;

namespace NoComments.Analyzers.Tests;

public class NoCommentsTestBase(PackageFixture fixture, ITestOutputHelper testOutputHelper)
{
    protected PackageFixture Fixture => fixture;
    protected ITestOutputHelper TestOutputHelper => testOutputHelper;

    internal async Task<ProjectBuilder> CreateProjectBuilderAsync(
        (string Name, string Value)[]? properties = null,
        (string Name, string Version)[]? packageReferences = null,
        string[]? additionalFiles = null)
    {
        var project = new ProjectBuilder(fixture, testOutputHelper);
        await project.AddCsprojFileAsync(properties, packageReferences, additionalFiles);
        return project;
    }
}
