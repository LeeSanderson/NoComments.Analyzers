namespace NoComments.Analyzers.Tests.Helpers;

internal static class PathHelpers
{
    public static string GetRootDirectory()
    {
        var directory = Environment.CurrentDirectory;
        while (directory != null && !IsRepositoryRoot(directory))
        {
            directory = Path.GetDirectoryName(directory);
        }

        return directory ?? throw new InvalidOperationException("Cannot find the root of the git repository");
    }

    private static bool IsRepositoryRoot(string directory)
    {
        var gitPath = Path.Combine(directory, ".git");
        return Directory.Exists(gitPath) || File.Exists(gitPath);
    }
}
