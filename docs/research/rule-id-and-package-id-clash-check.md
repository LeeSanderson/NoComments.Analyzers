# Rule ID and package ID clash check

Research for issue #4 (part of #1). Checked on 2026-11-20.

## Question

1. Does a published or well-known Roslyn analyzer already use the rule ID prefix `NC` (`NC0001`-`NC0003`)?
2. Is the NuGet package ID `NoComments.Analyzers`, or a near-duplicate, taken on nuget.org?
3. If `NC` clashes, which nearby prefixes are free?

## Answer

- **`NC` is taken. Do not use it.** Two packages on nuget.org already ship `NC####` diagnostics. One of them also reports C# comments as `NC0001`, so it is in the same problem space.
- **The package ID `NoComments.Analyzers` is free.** A near-duplicate, `NoCommentsAnalyzer`, exists. It belongs to a different owner and does a similar job, so the name will be easy to confuse with ours.
- **Recommended prefix: `NOC`** (`NOC0001`-`NOC0003`). Nothing found uses it. `NCMT` and `NOCMT` are also free but harder to read. Avoid `NOCOMMENT`, because an F# no-comments analyzer already uses `NOCOMMENT001`-`003`.

## Findings

### 1. `NC` rule IDs already in use

| Package (nuget.org) | Owner | Latest version / downloads | IDs it ships | What it does |
|---|---|---|---|---|
| `NoCommentsAnalyzer` | lbsa71 (Stefan Andersson) | 1.0.76 (2025-06-02) / ~4.3k | `NC0001` | Forbids C# comments that lack an "intentional" marker. Has a code fix ("Remove unauthorized comment"). |
| `NoCyrillicInCodeAnalyzer` | eliseevev | 0.1.1 / ~2.9k | `NC0001`-`NC0010` | Forbids Cyrillic characters in identifiers. Has code fixes. |

How this was checked:

- I downloaded each `.nupkg` from the nuget.org flat container and extracted the diagnostic ID strings from the analyzer DLLs under `analyzers/dotnet/cs/`.
  - `https://api.nuget.org/v3-flatcontainer/nocommentsanalyzer/1.0.76/nocommentsanalyzer.1.0.76.nupkg`: `NoCommentsAnalyzer.dll` contains `NC0001`, the title "Unauthorized comment detected", category `Formatting`, and help link `https://github.com/lbsa71/no-ai-comments`. The nuspec gives the repository as `https://github.com/lbsa71/no-ai-comments`, which now resolves to `lbsa71/NoComments`.
  - `https://api.nuget.org/v3-flatcontainer/nocyrillicincodeanalyzer/0.1.1/nocyrillicincodeanalyzer.0.1.1.nupkg`: both `Eliseev.NoCyrillicInCodeAnalyzer.dll` and its `.CodeFixes.dll` contain `NC0001` to `NC0010`.
- GitHub code search for `NC0001` in C# confirms the source:
  - `lbsa71/NoComments` `NoCommentsAnalyzer/NoCommentsAnalyzer.cs`
  - `eliseevev/NoCyrillicInCodeAnalyzer` `.../NC0001NoCyrillicInTypeName.cs`, `NC0002...FixProvider.cs`
  - All other hits were unrelated: string literals, ticket numbers, and Mono XPath sources. The hit in `HanumantappaBudihal/.NET-Extensions` defines `ASYNC0001`, not `NC0001`.

Why the clash matters: rule IDs are the key for `.editorconfig` severity (`dotnet_diagnostic.NC0001.severity`), `#pragma warning disable NC0001`, `[SuppressMessage]`, and `<NoWarn>`. If two analyzers in one project share an ID, one setting silently changes both. `NoCommentsAnalyzer` is the analyzer most likely to be installed next to ours, or tried before ours, because it solves the same problem.

### 2. Package ID availability

Checked with the nuget.org flat-container index (`https://api.nuget.org/v3-flatcontainer/{id-lowercase}/index.json`, where 404 means the ID does not exist). I also used the search API (`https://azuresearch-usnc.nuget.org/query?q=...&prerelease=true`) and the registration API (`https://api.nuget.org/v3/registration5-semver1/{id}/index.json`).

| ID | Result |
|---|---|
| `NoComments.Analyzers` | **404, free** |
| `NoComments.Analyzer` | 404, free |
| `NoComments` | 404, free |
| `NoComments.CodeFixes` | 404, free |
| `NoComment.Analyzers` | 404, free |
| `NoCommentsAnalyzer` | **taken** (lbsa71, the `NC0001` analyzer above) |
| `Scowalt.NoCommentsAnalyzer` | taken (ScoWalt). This is an F# analyzer built on FSharp.Analyzers.SDK, not Roslyn. |

Nobody has reserved a `NoComments` ID prefix. The search API returns `"verified": false` for `NoCommentsAnalyzer`, so registering `NoComments.Analyzers` is not blocked. NuGet ID prefix reservation (https://learn.microsoft.com/nuget/nuget-org/id-prefix-reservation) is an option later if the owner wants the `NoComments.` namespace.

Naming risk: `NoComments.Analyzers` and `NoCommentsAnalyzer` differ only by a dot and an "s", and both report C# comments. The README should say plainly that this package is unrelated to `NoCommentsAnalyzer`. That is a branding decision for #1 and does not block anything.

Search terms that found nothing new: `NoComments`, `nocomment`, `comments analyzer`. Other comment-related packages exist (`CommentSense`, `Comment.Todo.Analyzer`, `XmlCommentAnalyzer`, and others), but none has a name close to ours.

### 3. Microsoft-reserved and well-known prefixes

No Microsoft or well-known third-party prefix is `NC` or starts with `NC`/`NOC`:

- Microsoft: `CS` (C# compiler), `CA` (.NET code-quality analyzers) and `IDE` (code-style analyzers) per https://learn.microsoft.com/dotnet/fundamentals/code-analysis/overview; `RS` (Roslyn SDK analyzers, `dotnet/roslyn-analyzers`); `SYSLIB` (https://learn.microsoft.com/dotnet/fundamentals/syslib-diagnostics/obsoletions-overview); `ASP`/`BL`/`MVC` (ASP.NET Core analyzers, https://learn.microsoft.com/aspnet/core/diagnostics/code-analysis); `NU` (NuGet, https://learn.microsoft.com/nuget/reference/errors-and-warnings); `NETSDK`, `MSB`, `BC`, `VSTHRD`, `EXTEXP`.
- Third party: Roslynator `RCS`, StyleCop `SA`/`SX`, Meziantou `MA`, SonarAnalyzer `S`, IDisposableAnalyzers `IDISP`, xUnit `xUnit`, NUnit `NUnit`, NSubstitute `NS`, SecurityCodeScan `SCS`, ErrorProne.NET `EPC`/`ERP`, AsyncFixer `AsyncFixer`, Moq `Moq`.

`NS` (NSubstitute) and `NU` (NuGet) are the closest, and neither overlaps `NC` or `NOC`. The only real clashes are the two community packages in section 1.

These prefix lists come from the cited docs and the projects' own published rule lists, not from a fresh scan of every package. That is enough here, because none of them is two letters beginning `N` other than `NS`/`NU`.

### 4. Free alternatives

GitHub code search, global and C#-only, as of 2026-11-20:

| Candidate | Analyzer/C# hits | Notes |
|---|---|---|
| `NOC0001`-`NOC0003` | **none** (no C# hits; global hits are unrelated game scripts and data files) | **Recommended.** Short, reads as "No Comments", and still sorts close to the `NC` that the planning docs used. |
| `NCMT0001` | none (global hits are weather station IDs like `US1NCMT0010`) | Free, but less readable. |
| `NOCMT0001` | none | Free, but long. |
| `NOCOMMENT001`-`003` | **taken** by `Scowalt.NoCommentsAnalyzer` (F#) | Avoid. It is a different language, but it is the same concept and would confuse users. |

`NOC0001` keeps the four-digit format the planning docs already use, so only the letters change.

## Caveats

- GitHub code search covers only indexed default branches and returns at most 100 results. It also rate-limited during this check. It cannot prove that an ID is unused, only that nothing visible uses it. The nuget.org package inspection is the stronger evidence, and it confirmed both `NC` clashes.
- Rule IDs are not registered anywhere. Uniqueness is only a convention, so a future package could still clash with `NOC`.

## Follow-up for the map (#1)

- Change the rule ID prefix from `NC` to `NOC` (`NOC0001`-`NOC0003`) wherever the planning docs or glossary mention it.
- Keep the package ID `NoComments.Analyzers`. You may want to add a README line saying it is unrelated to `NoCommentsAnalyzer`.
