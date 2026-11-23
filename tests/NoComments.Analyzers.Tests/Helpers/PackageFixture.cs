using System.Text;
using CliWrap;

namespace NoComments.Analyzers.Tests.Helpers;

public sealed class PackageFixture : IAsyncLifetime
{
    internal const string PackageId = "NoComments.Analyzers";
    internal const string PackageVersion = "999.9.9";

    private readonly TemporaryDirectory _packageDirectory = TemporaryDirectory.Create();

    public string PackageDirectory => _packageDirectory.FullPath;

    public async ValueTask InitializeAsync()
    {
        var projectPath =
            Path.Combine(
                PathHelpers.GetRootDirectory(),
                "src",
                "NoComments.Analyzers.Package",
                "NoComments.Analyzers.Package.csproj");

        string[] args = ["pack", projectPath, $"-p:Version={PackageVersion}", "--output", _packageDirectory.FullPath];
        var output = new StringBuilder();
        var result = await Cli.Wrap("dotnet")
            .WithArguments(args)
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(output))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(output))
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException("Error while creating the NuGet package:\n" + output);
        }
    }

    public ValueTask DisposeAsync()
    {
        _packageDirectory.Dispose();
        return ValueTask.CompletedTask;
    }
}
