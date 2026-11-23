using System.IO.Compression;
using System.Reflection.Metadata;
using System.Text;
using System.Xml.Linq;
using NoComments.Analyzers.Tests.Helpers;
using Shouldly;

namespace NoComments.Analyzers.Tests;

public class PackageShould(PackageFixture fixture)
{
    private const string RepositoryUrl = "https://github.com/LeeSanderson/NoComments.Analyzers";

    private static readonly Guid SourceLinkKind = new("CC110556-A091-4D38-9FEC-25AB9A351A6A");

    [Fact]
    public async Task ShipTheCodeFixesNextToTheAnalyzer()
    {
        await using var package = await OpenPackageAsync("nupkg");

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

    [Fact]
    public async Task ShipNoLibraryForTheConsumerToCompileAgainst()
    {
        await using var package = await OpenPackageAsync("nupkg");

        package.Entries.ShouldNotContain(entry => entry.FullName.StartsWith("lib/", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DeclareItselfADevelopmentDependency()
    {
        var metadata = await ReadMetadataAsync();

        Element(metadata, "developmentDependency").Value.ShouldBe("true");
    }

    [Fact]
    public async Task CarryAnMitLicenseExpression()
    {
        var metadata = await ReadMetadataAsync();

        var license = Element(metadata, "license");
        license.Attribute("type")?.Value.ShouldBe("expression");
        license.Value.ShouldBe("MIT");
    }

    [Fact]
    public async Task ShipTheRepositoryReadmeAsThePackageReadme()
    {
        await using var package = await OpenPackageAsync("nupkg");
        var metadata = await ReadMetadataAsync();

        Element(metadata, "readme").Value.ShouldBe("README.md");
        var packedReadme = await ReadEntryTextAsync(package, "README.md");
        var repositoryReadme = await File.ReadAllTextAsync(
            Path.Combine(PathHelpers.GetRootDirectory(), "README.md"),
            TestContext.Current.CancellationToken);
        packedReadme.ShouldBe(repositoryReadme);
    }

    [Fact]
    public async Task ShipAnIcon()
    {
        await using var package = await OpenPackageAsync("nupkg");
        var metadata = await ReadMetadataAsync();

        Element(metadata, "icon").Value.ShouldBe("icon.png");
        package.GetEntry("icon.png").ShouldNotBeNull().Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task PointTheListingAtTheGitHubRepository()
    {
        var metadata = await ReadMetadataAsync();

        Element(metadata, "projectUrl").Value.ShouldBe(RepositoryUrl);
        var repository = Element(metadata, "repository");
        repository.Attribute("type")?.Value.ShouldBe("git");
        repository.Attribute("url")?.Value.ShouldBe(RepositoryUrl);
        Element(metadata, "releaseNotes").Value.ShouldBe(RepositoryUrl + "/blob/main/CHANGELOG.md");
        Element(metadata, "tags").Value.Split(' ').ShouldBe(
            ["analyzer", "roslyn", "comments", "code-quality", "csharp", "razor"]);
    }

    [Fact]
    public async Task ShipSymbolsForBothAssemblies()
    {
        await using var symbols = await OpenPackageAsync("snupkg");

        var symbolEntries = symbols.Entries
            .Select(entry => entry.FullName)
            .Where(name => name.EndsWith(".pdb", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal);

        symbolEntries.ShouldBe(
        [
            "analyzers/dotnet/cs/NoComments.Analyzers.CodeFixes.pdb",
            "analyzers/dotnet/cs/NoComments.Analyzers.pdb",
        ]);
    }

    [Theory]
    [InlineData("NoComments.Analyzers")]
    [InlineData("NoComments.Analyzers.CodeFixes")]
    public async Task LinkTheSymbolsToTheSourcesOnGitHub(string assemblyName)
    {
        await using var symbols = await OpenPackageAsync("snupkg");
        var entry = symbols.GetEntry($"analyzers/dotnet/cs/{assemblyName}.pdb").ShouldNotBeNull();
        using var pdb = new MemoryStream();
        await using (var entryStream = await entry.OpenAsync(TestContext.Current.CancellationToken))
        {
            await entryStream.CopyToAsync(pdb, TestContext.Current.CancellationToken);
        }

        pdb.Position = 0;
        using var provider = MetadataReaderProvider.FromPortablePdbStream(pdb);
        var reader = provider.GetMetadataReader();
        var sourceLink = reader.GetCustomDebugInformation(EntityHandle.ModuleDefinition)
            .Select(reader.GetCustomDebugInformation)
            .Where(information => reader.GetGuid(information.Kind) == SourceLinkKind)
            .Select(information => Encoding.UTF8.GetString(reader.GetBlobBytes(information.Value)))
            .SingleOrDefault();

        sourceLink.ShouldNotBeNull().ShouldContain("https://raw.githubusercontent.com/LeeSanderson/NoComments.Analyzers/");
    }

    private async Task<ZipArchive> OpenPackageAsync(string extension)
    {
        var packagePath = Path.Combine(
            fixture.PackageDirectory,
            $"{PackageFixture.PackageId}.{PackageFixture.PackageVersion}.{extension}");
        return await ZipFile.OpenReadAsync(packagePath, TestContext.Current.CancellationToken);
    }

    private async Task<XElement> ReadMetadataAsync()
    {
        await using var package = await OpenPackageAsync("nupkg");
        var nuspec = await ReadEntryTextAsync(package, $"{PackageFixture.PackageId}.nuspec");
        var document = XDocument.Parse(nuspec);
        return document.Root.ShouldNotBeNull().Elements().Single(element => element.Name.LocalName == "metadata");
    }

    private static XElement Element(XElement metadata, string name) =>
        metadata.Elements().SingleOrDefault(element => element.Name.LocalName == name)
        ?? throw new ShouldAssertException($"The nuspec has no <{name}> element:\n{metadata}");

    private static async Task<string> ReadEntryTextAsync(ZipArchive package, string entryName)
    {
        var entry = package.GetEntry(entryName) ?? throw new ShouldAssertException($"The package has no {entryName} entry.");
        await using var stream = await entry.OpenAsync(TestContext.Current.CancellationToken);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
    }
}
