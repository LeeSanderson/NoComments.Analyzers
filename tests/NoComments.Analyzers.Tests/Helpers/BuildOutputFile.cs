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

    public IEnumerable<BuildOutputFileRule> AllRules() => Runs?.SelectMany(r => r.Tool?.Driver?.Rules ?? []) ?? [];

    public bool HasError() => AllResults().Any(r => r.Level == "error" && !r.IsSuppressed);
    public bool HasError(string ruleId) => AllResults().Any(r => r.Level == "error" && r.RuleId == ruleId && !r.IsSuppressed);
    public bool HasWarning(string ruleId) => AllResults().Any(r => r.Level == "warning" && r.RuleId == ruleId && !r.IsSuppressed);
    public bool HasNote(string ruleId) => AllResults().Any(r => r.Level == "note" && r.RuleId == ruleId && !r.IsSuppressed);

    public BuildOutputFileRule Rule(string ruleId) =>
        AllRules().FirstOrDefault(r => r.Id == ruleId)
        ?? throw new InvalidOperationException($"The SARIF log does not describe rule {ruleId}");

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

        [JsonPropertyName("tool")]
        public BuildOutputFileTool? Tool { get; set; }
    }

    internal sealed class BuildOutputFileTool
    {
        [JsonPropertyName("driver")]
        public BuildOutputFileDriver? Driver { get; set; }
    }

    internal sealed class BuildOutputFileDriver
    {
        [JsonPropertyName("rules")]
        public BuildOutputFileRule[]? Rules { get; set; }
    }

    internal sealed class BuildOutputFileRule
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("shortDescription")]
        public BuildOutputFileRunResultMessage? ShortDescription { get; set; }

        [JsonPropertyName("fullDescription")]
        public BuildOutputFileRunResultMessage? FullDescription { get; set; }

        [JsonPropertyName("helpUri")]
        public Uri? HelpUri { get; set; }

        [JsonPropertyName("defaultConfiguration")]
        public BuildOutputFileRuleConfiguration? DefaultConfiguration { get; set; }

        [JsonPropertyName("properties")]
        public BuildOutputFileRuleProperties? Properties { get; set; }

        public string DefaultLevel => DefaultConfiguration?.Level ?? "warning";

        public bool IsEnabledByDefault => DefaultConfiguration?.Enabled ?? true;
    }

    internal sealed class BuildOutputFileRuleConfiguration
    {
        [JsonPropertyName("level")]
        public string? Level { get; set; }

        [JsonPropertyName("enabled")]
        public bool? Enabled { get; set; }
    }

    internal sealed class BuildOutputFileRuleProperties
    {
        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("tags")]
        public string[]? Tags { get; set; }
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
