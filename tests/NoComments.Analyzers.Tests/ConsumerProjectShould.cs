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

    [Fact]
    public async Task KeepTheAnalyzerFromFlowingToItsOwnDependents()
    {
        using var project = new ProjectBuilder(Fixture, TestOutputHelper);
        await project.AddFileAsync("Consumer/Consumer.csproj", ClassLibraryProject("Consumer.sarif"));
        await project.AddFileAsync("Consumer/Greeter.cs", ClassWithComment("Consumer"));
        await project.AddFileAsync(
            "Dependent/Dependent.csproj",
            ClassLibraryProject("Dependent.sarif", """<ProjectReference Include="../Consumer/Consumer.csproj" />"""));
        await project.AddFileAsync("Dependent/Greeter.cs", ClassWithComment("Dependent"));
        var addPackage = await project.RunDotnetAsync(
            ["add", "Consumer/Consumer.csproj", "package", PackageFixture.PackageId, "--version", PackageFixture.PackageVersion]);
        addPackage.ShouldBe(0);

        var dependent = await project.BuildAndGetOutputAsync(["Dependent/Dependent.csproj"], "Dependent/Dependent.sarif");
        var consumer = await project.ReadBuildOutputFileAsync("Consumer/Consumer.sarif");

        dependent.ExitCode.ShouldBe(0);
        consumer.DiagnosticsIn("Consumer/Greeter.cs").ShouldHaveSingleItem().RuleId.ShouldBe("NOC0001");
        dependent.DiagnosticsIn("Dependent/Greeter.cs").ShouldBeEmpty();
    }

    private static string ClassLibraryProject(string errorLog, string items = "") =>
        $"""
         <Project Sdk="Microsoft.NET.Sdk">
           <PropertyGroup>
             <TargetFramework>net$(NETCoreAppMaximumVersion)</TargetFramework>
             <ErrorLog>{errorLog},version=2.1</ErrorLog>
           </PropertyGroup>
           <ItemGroup>
             {items}
           </ItemGroup>
         </Project>
         """;

    private static string ClassWithComment(string namespaceName) =>
        $$"""
          namespace {{namespaceName}};

          public static class Greeter
          {
              // Says hello
              public static string Greet() => "Hello";
          }
          """;
}
