using System.IO.Compression;
using NoComments.Analyzers.Tests.Helpers;
using Shouldly;

namespace NoComments.Analyzers.Tests;

public class PackageShould(PackageFixture fixture)
{
    [Fact]
    public async Task ShipTheCodeFixesNextToTheAnalyzer()
    {
        var packagePath = Path.Combine(
            fixture.PackageDirectory,
            $"{PackageFixture.PackageId}.{PackageFixture.PackageVersion}.nupkg");
        await using var package = await ZipFile.OpenReadAsync(packagePath, TestContext.Current.CancellationToken);

        var analyzerEntries = package.Entries
            .Select(entry => entry.FullName)
            .Where(name => name.StartsWith("analyzers/", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal);

        analyzerEntries.ShouldBe(
        [
            "analyzers/dotnet/cs/NoComments.Analyzers.CodeFixes.dll",
            "analyzers/dotnet/cs/NoComments.Analyzers.dll",
        ]);
    }
}
