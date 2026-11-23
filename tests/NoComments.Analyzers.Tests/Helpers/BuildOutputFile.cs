using System.Text.Json.Serialization;

namespace NoComments.Analyzers.Tests.Helpers;

internal sealed class BuildOutputFile
{
    [JsonPropertyName("runs")]
    public BuildOutputFileRun[]? Runs { get; set; }

    [JsonIgnore]
    public string ProjectDirectory { get; set; } = "";

    [JsonIgnore]
    public int ExitCode { get; set; }

    public IEnumerable<BuildOutputFileRunResult> AllResults() => Runs?.SelectMany(r => r.Results ?? []) ?? [];

    public bool HasError() => AllResults().Any(r => r.Level == "error" && !r.IsSuppressed);
    public bool HasError(string ruleId) => HasUnsuppressed("error", ruleId);
    public bool HasWarning(string ruleId) => HasUnsuppressed("warning", ruleId);
    public bool HasNote(string ruleId) => HasUnsuppressed("note", ruleId);

    private bool HasUnsuppressed(string level, string ruleId) =>
        AllResults().Any(r => r.Level == level && r.RuleId == ruleId && !r.IsSuppressed);

    public IReadOnlyList<ReportedDiagnostic> DiagnosticsIn(string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(ProjectDirectory, relativePath));
        return [.. AllResults()
            .Where(r => !r.IsSuppressed)
            .Select(r => (Result: r, Location: r.Locations?.FirstOrDefault()?.PhysicalLocation))
            .Where(r => r.Location?.LocalPath() is { } path && string.Equals(path, fullPath, StringComparison.OrdinalIgnoreCase))
            .Select(r => r.Location!.ToReportedDiagnostic(r.Result.RuleId ?? ""))
            .OrderBy(d => d.StartLine)
            .ThenBy(d => d.StartColumn)];
    }

    internal sealed class BuildOutputFileRun
    {
        [JsonPropertyName("results")]
        public BuildOutputFileRunResult[]? Results { get; set; }
    }

    internal sealed class BuildOutputFileRunResult
    {
        [JsonPropertyName("ruleId")]
        public string? RuleId { get; set; }

        [JsonPropertyName("level")]
        public string? Level { get; set; }

        [JsonPropertyName("message")]
        public BuildOutputFileRunResultMessage? Message { get; set; }

        [JsonPropertyName("locations")]
        public BuildOutputFileLocation[]? Locations { get; set; }

        [JsonPropertyName("suppressions")]
        public object[]? Suppressions { get; set; }

        public bool IsSuppressed => Suppressions is { Length: > 0 };

        public override string ToString()
        {
            var location = Locations?.FirstOrDefault()?.PhysicalLocation;
            return location is null
                ? $"{Level}:{RuleId} {Message}"
                : $"{Level}:{RuleId} {location} {Message}";
        }
    }

    internal sealed class BuildOutputFileLocation
    {
        [JsonPropertyName("physicalLocation")]
        public BuildOutputFilePhysicalLocation? PhysicalLocation { get; set; }
    }

    internal sealed class BuildOutputFilePhysicalLocation
    {
        [JsonPropertyName("artifactLocation")]
        public BuildOutputFileArtifactLocation? ArtifactLocation { get; set; }

        [JsonPropertyName("region")]
        public BuildOutputFileRegion? Region { get; set; }

        public string? LocalPath() =>
            ArtifactLocation?.Uri is { IsAbsoluteUri: true, IsFile: true } uri
                ? Path.GetFullPath(uri.LocalPath)
                : null;

        public ReportedDiagnostic ToReportedDiagnostic(string ruleId)
        {
            var region = Region ?? new BuildOutputFileRegion();
            return new ReportedDiagnostic(
                ruleId,
                region.StartLine,
                region.StartColumn,
                region.EndLine ?? region.StartLine,
                region.EndColumn ?? region.StartColumn);
        }

        public override string ToString()
        {
            var region = Region ?? new BuildOutputFileRegion();
            return $"{ArtifactLocation?.Uri}({region.StartLine},{region.StartColumn})-({region.EndLine},{region.EndColumn})";
        }
    }

    internal sealed class BuildOutputFileArtifactLocation
    {
        [JsonPropertyName("uri")]
        public Uri? Uri { get; set; }
    }

    internal sealed class BuildOutputFileRegion
    {
        [JsonPropertyName("startLine")]
        public int StartLine { get; set; }

        [JsonPropertyName("startColumn")]
        public int StartColumn { get; set; }

        [JsonPropertyName("endLine")]
        public int? EndLine { get; set; }

        [JsonPropertyName("endColumn")]
        public int? EndColumn { get; set; }
    }

    internal sealed class BuildOutputFileRunResultMessage
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        public override string ToString()
        {
            return Text ?? "";
        }
    }
}
