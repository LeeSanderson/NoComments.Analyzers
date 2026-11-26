# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [v0.1.0] - 2026-11-26

First release.

### Added

- **NOC0001** reports single-line comments (`//`).
- **NOC0002** reports multi-line comments (`/* */`).
- **NOC0003** reports documentation comments (`///` and `/** */`) when `GenerateDocumentationFile` is `true`. When it is `false`, these comments are reported as NOC0001 and NOC0002.
- **NOC0004** reports Razor comments (`@* *@`) on build. It can be suppressed only for the whole project.
- Every rule is a Warning in the `Maintainability` category. Each comment gets one diagnostic, and each rule's severity can be set separately.
- C# comments in `.razor` and `.cshtml` files are reported at their position in the Razor file. Other generated code is not reported.
- **Remove comment** code fix for NOC0001 to NOC0003. It deletes the comment and tidies the whitespace left behind. Fix All works at document, project and solution scope. The IDE offers the fix in `.cs` files. `dotnet format analyzers` applies it everywhere, including the C# in Razor files.
- A help page for each rule under `docs/rules`.
- Works alongside OpinionatedDotNetCodingStandards and `TreatWarningsAsErrors`.
- Needs Visual Studio 17.14 or later, or the .NET SDK 9.0.300 or later.

[Unreleased]: https://github.com/LeeSanderson/NoComments.Analyzers/compare/v0.1.0...HEAD
[v0.1.0]: https://github.com/LeeSanderson/NoComments.Analyzers/releases/tag/v0.1.0
