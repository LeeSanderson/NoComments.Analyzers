# Compatibility with Opinionated.DotNet.CodingStandards

Research for [#3](https://github.com/LeeSanderson/NoComments.Analyzers/issues/3). Researched 2026-11-20.

Question: what does [Opinionated.DotNet.CodingStandards](https://github.com/LeeSanderson/OpinionatedDotNetCodingStandards) require of a consuming project, does anything in it clash with banning comments, and which Microsoft.CodeAnalysis version should NoComments compile against so that it loads in every setup that package supports?

Sources are linked inline. "Standards repo" means commit `0c8be7c` on `main` of the standards repo (package v0.0.22, the latest on nuget.org at the time of writing). Paths below are relative to `packages/Opinionated.DotNet.CodingStandards/pkgsrc/` unless stated.

## Summary

- The standards package effectively needs the **.NET 10 SDK (Roslyn 5.0+)**. Its README says ".NET 10 SDK" is a prerequisite, and one of its bundled analyzers (Meziantou.Analyzer 3.0.341) ships builds for Roslyn 5.0, 5.6 and 5.9 only.
- It forces `TreatWarningsAsErrors=true`, `AnalysisLevel=latest-all`, `LangVersion=latest`, `Nullable=enable` and **`GenerateDocumentationFile=true`**. It also raises a warning if `GenerateDocumentationFile` is turned off, because IDE0005 needs it.
- It does **not** require documentation comments. CS1591, every StyleCop SA1600-SA1651 documentation rule, SA1633 and S1451 (file headers) are all `none`, and IDE0073 is `silent`. No rule in the package forces a comment to exist, so nothing clashes outright with banning comments.
- Some soft overlaps exist. A few rules in the package already report some comments (S125 commented-out code, S1135 TODO, S1134 FIXME, S4663 empty comments). Other rules only check the shape of documentation comments (MA0211, MA0197, MA0218, SA1653, SA1654). If the code has no comments, none of these fire.
- The lowest Roslyn host in the setups the standards package supports is **Visual Studio 2022 17.14 (Roslyn 4.14)**. Microsoft lists VS 17.14 as the minimum VS for SDK 10.0.100.
- **Recommendation** (for the follow-up grilling ticket "Lowest supported Roslyn/SDK version" to decide): compile NoComments against **Microsoft.CodeAnalysis.CSharp 4.14.0**. See [Recommendation](#recommendation-minimum-microsoftcodeanalysis-version).

## 1. What the standards package requires of a consuming project

### Packaging and how it applies

- It is a `developmentDependency` package with `build/`, `buildTransitive/` and `buildMultiTargeting/` props and targets. The `buildTransitive` and `buildMultiTargeting` files import the `build/` ones. Source: `Opinionated.DotNet.CodingStandards.nuspec` and the `build*/` folders in the standards repo.
- The README recommends a `GlobalPackageReference` in `Directory.Packages.props`, so it applies to every project in a solution. Source: [`pkgsrc/README.md`](https://github.com/LeeSanderson/OpinionatedDotNetCodingStandards/blob/main/packages/Opinionated.DotNet.CodingStandards/pkgsrc/README.md).
- The package project itself targets `netstandard2.0` with `IncludeBuildOutput=false` (no library assets), so it places **no constraint on the consumer's target framework**. Source: `src/Opinionated.DotNet.CodingStandards/Opinionated.DotNet.CodingStandards.csproj`. Its own tests build consumer projects at `net$(NETCoreAppMaximumVersion)` (that is, `net10.0`), so only that target framework is tested.

### SDK version

- Repository README: "Prerequisites: [.NET 10 SDK]". Repository `global.json`: `"version": "10.0.101", "rollForward": "latestFeature"`.
- The bundled analyzers make this a hard requirement in practice (see the table in section 2). On anything older than Roslyn 5.0, Meziantou.Analyzer loads nothing. The standards repo's own `CHANGELOG.md` (v0.0.x entry for MA0216) says the same: "the compiler picks the folder matching its own Roslyn version, so on older SDKs no loaded analyzer defines the [rule]".

### MSBuild properties set by `build/Opinionated.DotNet.CodingStandards.props`

All of these are set only if the property is empty, so a consumer can override each one:

| Property | Value |
|---|---|
| `LangVersion` | `latest` |
| `ImplicitUsings` | `enable` |
| `Nullable` | `enable` |
| `TreatWarningsAsErrors` | `true` |
| `MSBuildTreatWarningsAsErrors` | `true` |
| `Features` | `strict` |
| `Deterministic` | `true` |
| `EnableNETAnalyzers` | `true` |
| `AnalysisLevel` | `latest-all` |
| `EnforceCodeStyleInBuild` | `true` |
| `ReportAnalyzer` | `true` |
| `DebugType` | `embedded` |
| `GenerateDocumentationFile` | `true` (comment in the props file: "Must be set to true for IDE0005 ... to work", linking [dotnet/roslyn#41640](https://github.com/dotnet/roslyn/issues/41640)) |
| `RestoreUseStaticGraphEvaluation` | `true` |
| `NuGetAudit` / `NuGetAuditMode` / `NuGetAuditLevel` | `true` / `all` / `low`, and NU1900-NU1904 are added to `WarningsAsErrors` |
| `_SkipUpgradeNetAnalyzersNuGetWarning` | `true` (set without a condition) |

### Targets (`build/Opinionated.DotNet.CodingStandards.targets`)

- Adds several global `.editorconfig` files as `EditorConfigFiles` items: `FileDefaults`, `CodeStyle`, `analyzers/*.editorconfig` (one per bundled analyzer), `ReSharper`, `Opinionated`, and `TestProject` (only when `IsTestProject` is true).
- Each analyzer config has `is_global = true` with `global_level = 0`. `CodeStyle.editorconfig` has `global_level = -1`. The comment in the targets file says: "a particular .NET analysis rule can only be configured once across all imported global EditorConfig files".
- Adds banned-API text files as `AdditionalFiles`, controlled by seven `Ban*` properties.
- Sets `IsTestProject` for projects whose name ends in `Tests`, and adds `InternalsVisibleTo($(MSBuildProjectName).Tests)` to all other projects.

### Bundled analyzers and the Roslyn version each one needs

The dependency versions come from the nuspec. The referenced `Microsoft.CodeAnalysis` versions were read from the assembly metadata of each nupkg, downloaded from nuget.org on 2026-11-20 (`AssemblyReference` table, read with `System.Reflection.Metadata`).

| Package (version) | Analyzer folder(s) | References `Microsoft.CodeAnalysis` |
|---|---|---|
| Meziantou.Analyzer 3.0.341 | `analyzers/dotnet/roslyn5.0/cs`, `roslyn5.6/cs`, `roslyn5.9/cs` | 5.0.0.0 / 5.6.0.0 / 5.9.0.0 |
| Microsoft.CodeAnalysis.NetAnalyzers 11.0.100 | `analyzers/dotnet/cs` (not versioned) | 4.14.0.0 |
| Microsoft.CodeAnalysis.BannedApiAnalyzers 5.6.0 | `analyzers/dotnet/cs` (not versioned) | 4.12.0.0 |
| SonarAnalyzer.CSharp 10.36.0.15337 | `analyzers/` | 1.3.1.0 |
| StyleCop.Analyzers 1.3.180 (first stable 1.3) | `analyzers/dotnet/cs` | 1.2.0.0 |

What happens when the host compiler is older:

- **Versioned folders (`roslynX.Y`)**: the SDK reads the compiler's assembly version into `CompilerApiVersion=roslyn<major>.<minor>` ([`Microsoft.PackageDependencyResolution.targets`](https://github.com/dotnet/sdk/blob/main/src/Tasks/Microsoft.NET.Build.Tasks/targets/Microsoft.PackageDependencyResolution.targets)). `ResolvePackageAssets` drops every folder newer than the compiler and keeps only the highest one that is not newer ([`ResolvePackageAssets.cs`](https://github.com/dotnet/sdk/blob/main/src/Tasks/Microsoft.NET.Build.Tasks/ResolvePackageAssets.cs), `AddAnalyzer` / `_maxApplicableVersion`). So on a Roslyn 4.x host, Meziantou.Analyzer 3.0.341 loads **nothing**, and there is no diagnostic to say so.
- **Unversioned folders**: the analyzer is passed to the compiler, which refuses it and reports **CS9057** (a warning): "Analyzer assembly '{0}' cannot be used because it references version '{1}' of the compiler, which is newer than the currently running version '{2}'." ([`CSharpResources.resx`](https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/CSharpResources.resx), `WRN_AnalyzerReferencesNewerCompiler`, `ErrorCode` 9057). The standards package sets `TreatWarningsAsErrors=true`, so this warning **breaks the build**. NetAnalyzers 11.0.100 therefore needs Roslyn 4.14 or newer.

### `.editorconfig` rules that touch comments or documentation

These values were taken from the shipped `config/**/*.editorconfig` files. The full list is in the standards repo's [`docs/rule-reference.md`](https://github.com/LeeSanderson/OpinionatedDotNetCodingStandards/blob/main/docs/rule-reference.md).

**Rules that would require comments: all off.**

| Rule | Meaning | Severity |
|---|---|---|
| CS1591 | Missing XML comment for publicly visible member | `none` (`Opinionated.editorconfig`: "documenting everything is not always necessary") |
| SA1600-SA1648, SA1650, SA1651 | StyleCop documentation rules (elements must be documented, parameters documented, and so on) | `none` |
| SA1633-SA1641 | StyleCop file header rules | `none` |
| SA0001 / SA0002 | XML comment analysis disabled / settings file | `none` |
| S1451 | Sonar: track lack of copyright/licence header | `none` |
| IDE0073 | File header must match template | `silent` (`file_header_template = unset` in `CodeStyle.editorconfig`) |
| CA1014 | Mark assemblies with CLSCompliant | `none` |

**Rules that require a setting NoComments must leave alone.**

| Rule | Meaning | Severity |
|---|---|---|
| `EnableGenerateDocumentationFile` | Roslyn reports this when `GenerateDocumentationFile` is not `true` (IDE0005 cannot run without it) | `warning` |
| IDE0005 | Unnecessary using directive | `warning` |

**Rules that already report some comments (overlap with NoComments, but no clash).**

| Rule | Meaning | Severity |
|---|---|---|
| S125 | Sections of code should not be commented out | `warning` |
| S1135 | Track uses of "TODO" tags | `warning` |
| S1134 | Track uses of "FIXME" tags | `warning` |
| S4663 | Comments should not be empty | `warning` |
| SA1120 | Comments should contain text | `none` |
| MA0026 | Fix TODO comment | `none` |

**Rules that check the shape of documentation comments (they fire only when documentation comments exist).**

| Rule | Meaning | Severity |
|---|---|---|
| MA0211 | Use multi-line syntax for XML `<summary>` | `warning` |
| MA0197 | No `<inheritdoc/>` on types ([doc](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0197.md)) | `warning` |
| MA0218 | Empty `language` attribute in XML comment | `warning` |
| MA0219 | Missing `language` attribute on `<c>`/`<code>` | `suggestion` |
| MA0154 / MA0177 | Use langword / single-line XML comment syntax | `none` |
| SA1653 | Only one partial declaration should contain a summary | `warning` |
| SA1654 | Properly encode generic brackets | `warning` |
| SA1649 | File name should match first type name (not about comments) | `warning` |

StyleCop comment-layout rules (SA1004, SA1005, SA1108, SA1512, SA1515 and others) are all `none`.

## 2. Microsoft.CodeAnalysis to .NET SDK to Visual Studio

Roslyn package to C#/VS/.NET, from [dotnet/roslyn `docs/wiki/NuGet-packages.md`](https://github.com/dotnet/roslyn/blob/main/docs/wiki/NuGet-packages.md). The SDK feature band comes from [.NET SDK, MSBuild, and Visual Studio versioning](https://learn.microsoft.com/dotnet/core/porting/versioning-sdk-msbuild-vs) (updated 2026-11-19). Rows marked † come from the Roslyn assembly found in installed SDKs and VS on this machine (2026-11-20), because the Roslyn wiki stops at 5.0.

| Roslyn (`Microsoft.CodeAnalysis`) | C# | Visual Studio | .NET SDK band | nuget.org release |
|---|---|---|---|---|
| 4.0 | 10 | 2022 17.0 | 6.0.1xx | 2021-11 |
| 4.3.1 | 10 | 17.3 | 6.0.4xx (6.0.428† ships 4.3) | 2022-09 |
| 4.4 | 11 | 17.4 | 7.0.1xx | 2022-11 |
| 4.8 | 12 | 17.8 | 8.0.1xx | 2023-11 |
| 4.11 | 12 | 17.11 | 8.0.4xx (8.0.428† ships 4.11) | 2024-08 |
| 4.12 | 13 | 17.12 | 9.0.1xx | 2024-12 |
| 4.14 | 13 | 17.14 (final VS 2022 minor; 17.14.21† ships 4.14) | 9.0.3xx (9.0.308†/9.0.321† ship 4.14) | 2025-05 |
| 5.0 | 14 | 2026 18.0 | 10.0.1xx | 2025-11 |
| 5.6† | 14 | 18.6 | 10.0.3xx (10.0.303† ships 5.6) | 2026-07 |
| 5.9† | 14 | 18.9 | 10.0.4xx (10.0.403† ships 5.9) | no stable release on nuget.org |
| 5.11† | 14 | 18.11 (18.11.3† ships 5.11) | n/a (monthly VS release) | no stable release on nuget.org |
| 5.12 | 15 | 18.12 | 11.0.1xx | 2026-11-13 |

Notes:

- Since VS 18.0, Visual Studio ships a minor version every month, and the Roslyn minor version follows the VS minor version (18.6 → 5.6, 18.9 → 5.9, 18.11 → 5.11). Only some of these versions are published to nuget.org: stable `Microsoft.CodeAnalysis.CSharp` versions after 4.14.0 are 5.0.0, 5.6.0 and 5.12.0 (nuget.org registration API, 2026-11-20). The SDK still ships quarterly ([versioning doc](https://learn.microsoft.com/dotnet/core/porting/versioning-sdk-msbuild-vs)).
- **Minimum VS per SDK** (same doc, "Targeting and support rules"): SDK 10.0.100 → VS **17.14**; 10.0.200/300/400 → VS 18.0; 11.0.100 → VS 18.11. Quote: "The first version of Visual Studio that supports a new TargetFramework becomes a floor for the feature bands of that SDK for Roslyn API surface, MSBuild targets, source generators, analyzers".
- In the IDE, analyzers run in Visual Studio's own Roslyn, not the SDK's. So the IDE version, not the SDK, sets the floor for live analysis. VS 17.14 runs Roslyn 4.14 (confirmed from the installed VS 17.14.21 `MSBuild\Current\Bin\Roslyn\Microsoft.CodeAnalysis.dll`).
- **Support status on 2026-11-20** (same doc): the .NET 8 (8.0.1xx/8.0.4xx) and .NET 9 (9.0.1xx/9.0.3xx) SDK lifecycles end "Nov '26". The SDKs still in support are 10.0.1xx, 10.0.4xx and 11.0.1xx. VS 2022 17.14 is still the "primary supported configuration" with SDK 9.0.3xx.

## 3. What this means for NoComments

### Setups the standards package supports

| Host | Roslyn | Standards package works? |
|---|---|---|
| `dotnet build`, SDK 10.0.1xx | 5.0 | Yes (Meziantou loads `roslyn5.0`) |
| `dotnet build`, SDK 10.0.3xx / 10.0.4xx / 11.0.1xx | 5.6 / 5.9 / 5.12 | Yes |
| VS 2026 18.x IDE | 5.x | Yes |
| VS 2022 17.14 IDE with SDK 10.0.1xx | 4.14 | Partly: Microsoft lists it as the minimum VS for SDK 10.0.100, NetAnalyzers loads, Meziantou silently loads nothing |
| SDK 9.0.3xx or older | ≤ 4.14 | Not as intended: the README requires the .NET 10 SDK and Meziantou loads nothing |
| Older than Roslyn 4.14 | < 4.14 | No: NetAnalyzers gives CS9057, which is an error under `TreatWarningsAsErrors` |

So the lowest Roslyn that NoComments must load in, if it is to load everywhere the standards package does, is **4.14** (the VS 17.14 IDE).

### Points for the analyzer design (beyond the version choice)

1. **`TreatWarningsAsErrors=true`**: any NC diagnostic at `Warning` severity fails the consumer's build, and so does CS9057 if NoComments references a Roslyn newer than the host. The default severity of NC0001-NC0003 matters a lot for these consumers.
2. **Documentation comments are only recognised when `GenerateDocumentationFile=true`.** The command-line parser sets `DocumentationMode.Diagnose` only when `/doc` is passed, and otherwise `DocumentationMode.None` ([`CSharpCommandLineParser.cs`](https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/CommandLine/CSharpCommandLineParser.cs), `parseDocumentationComments`). The lexer only produces documentation comment trivia when `DocumentationMode >= Parse` ([`Lexer.cs`](https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Parser/Lexer.cs), `SuppressDocumentationCommentParse`). Otherwise `///` and `/** */` lex as ordinary `SingleLineCommentTrivia` / `MultiLineCommentTrivia`. The standards package forces `GenerateDocumentationFile=true`, so NC0003 behaves as expected there. In a project without that setting, a `///` comment in a command-line build would come through as a single-line comment (NC0001), not NC0003. The specs for NC0001 and NC0003 should state which behaviour is intended, for example by classifying `///` by its text and not only by its trivia kind.
3. **Do not touch `GenerateDocumentationFile`.** Turning it off would trigger the `EnableGenerateDocumentationFile` warning, which is an error under the standards package, and would break IDE0005. If no documentation comments exist, the generated XML file is just empty, which is harmless.
4. **Severity configuration**: the standards package configures rules through global configs (`is_global = true`). Roslyn allows a rule to be configured only once across global configs at the same level, so NoComments should rely on the `DiagnosticDescriptor` default severity and normal `.editorconfig` overrides, and should not ship its own global config that might collide. (No NC IDs appear in the standards configs today, so there is no collision now.)
5. **Overlapping diagnostics**: S125, S1135, S1134 and S4663 will report on the same comments as NC0001/NC0002. This is harmless duplication, and it goes away once the comment is removed.

## Recommendation: minimum Microsoft.CodeAnalysis version

*This is a recommendation for the grilling ticket "Lowest supported Roslyn/SDK version" to decide, not a decision.*

**Compile NoComments against `Microsoft.CodeAnalysis.CSharp` 4.14.0** (and `Microsoft.CodeAnalysis.CSharp.Workspaces` 4.14.0 for the code fix assembly), using one unversioned `analyzers/dotnet/cs` folder.

Reasons:

- 4.14 is the lowest Roslyn host among the setups the standards package supports: the VS 2022 17.14 IDE, which is the minimum VS for SDK 10.0.100. Any version up to 4.14 loads everywhere the standards package loads. Anything at 5.0 or above causes CS9057 in VS 17.14, which `TreatWarningsAsErrors` turns into an error.
- VS 17.14 is the final VS 2022 minor version and is still in support. It is also the oldest host Microsoft currently treats as a primary supported configuration. Every SDK still in support (10.0.x, 11.0.x) ships Roslyn 5.0 or newer.
- NoComments needs only long-standing APIs (comment trivia, `DiagnosticAnalyzer`, `CodeFixProvider`, `GeneratedCodeAnalysisFlags`). Nothing it needs was added after 4.x, so going higher gains nothing.
- Going lower (for example to 4.8 for .NET 8 / VS 17.8) would also work, because there are no API needs that rule it out. But it only adds toolchains whose SDK support ended in November 2026, which the standards package does not support anyway. The grilling ticket should decide whether NoComments wants to reach consumers who do not use the standards package on those older toolchains. If it does, 4.8.0 (the .NET 8 LTS baseline) is the natural lower choice.
- Roslyn-versioned folders (`roslynX.Y`, as Meziantou uses) are not needed unless a later feature requires a newer API.

## Sources

- Standards repo: [LeeSanderson/OpinionatedDotNetCodingStandards](https://github.com/LeeSanderson/OpinionatedDotNetCodingStandards) at `0c8be7c`. Files used: the nuspec, `pkgsrc/build/*.props|targets`, `pkgsrc/config/**/*.editorconfig`, `pkgsrc/README.md`, `README.md`, `global.json`, `CHANGELOG.md`, `src/.../*.csproj`.
- Bundled analyzer nupkgs from `https://api.nuget.org/v3-flatcontainer/{id}/{version}/` (assembly references read from their metadata).
- [dotnet/roslyn docs/wiki/NuGet-packages.md](https://github.com/dotnet/roslyn/blob/main/docs/wiki/NuGet-packages.md): Roslyn ↔ C# ↔ VS ↔ .NET.
- [learn.microsoft.com: .NET SDK, MSBuild, and Visual Studio versioning](https://learn.microsoft.com/dotnet/core/porting/versioning-sdk-msbuild-vs): SDK bands, minimum VS, lifecycle.
- [dotnet/sdk Microsoft.PackageDependencyResolution.targets](https://github.com/dotnet/sdk/blob/main/src/Tasks/Microsoft.NET.Build.Tasks/targets/Microsoft.PackageDependencyResolution.targets) and [ResolvePackageAssets.cs](https://github.com/dotnet/sdk/blob/main/src/Tasks/Microsoft.NET.Build.Tasks/ResolvePackageAssets.cs): how the SDK chooses a `roslynX.Y` analyzer folder.
- [dotnet/roslyn CSharpResources.resx](https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/CSharpResources.resx) / `ErrorCode.cs`: CS9057.
- [dotnet/roslyn Lexer.cs](https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Parser/Lexer.cs) and [CSharpCommandLineParser.cs](https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/CommandLine/CSharpCommandLineParser.cs): how `DocumentationMode` decides whether a comment is lexed as a documentation comment.
- nuget.org registration API for `Microsoft.CodeAnalysis.CSharp` (stable versions and publish dates).
- Local evidence (2026-11-20): Roslyn assembly versions in installed SDKs 6.0.428, 8.0.428, 9.0.308, 9.0.321, 10.0.303 and 10.0.403, and in VS 17.14.21 and 18.11.3.
