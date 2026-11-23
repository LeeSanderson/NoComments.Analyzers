namespace NoComments.Analyzers.Tests.Helpers;

internal sealed record ReportedDiagnostic(string RuleId, int StartLine, int StartColumn, int EndLine, int EndColumn);
