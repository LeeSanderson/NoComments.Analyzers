# NoComments.Analyzers

A Roslyn analyzer package that reports every comment in C# and Razor source, so that intent has to be expressed in code: through names, types and tests.

Comments drift out of step with the code they describe, and nothing in the build checks them. NoComments reports each comment as a diagnostic at build time and in the IDE, and offers a **Remove comment** code fix for C# comments.

NoComments.Analyzers is not related to the `NoCommentsAnalyzer` package on nuget.org. It uses its own `NOC` rule prefix.

## Rules

| ID | Title | Default severity | Code fix | Documentation |
|---|---|---|---|---|
| NOC0001 | Do not use single-line comments | Warning | Yes | [NOC0001](https://github.com/LeeSanderson/NoComments.Analyzers/blob/main/docs/rules/NOC0001.md) |
| NOC0002 | Do not use multi-line comments | Warning | Yes | [NOC0002](https://github.com/LeeSanderson/NoComments.Analyzers/blob/main/docs/rules/NOC0002.md) |
| NOC0003 | Do not use documentation comments | Warning | Yes | [NOC0003](https://github.com/LeeSanderson/NoComments.Analyzers/blob/main/docs/rules/NOC0003.md) |
| NOC0004 | Do not use Razor comments | Warning | No | [NOC0004](https://github.com/LeeSanderson/NoComments.Analyzers/blob/main/docs/rules/NOC0004.md) |

All rules are in the `Maintainability` category. Each comment gets one diagnostic, and each rule's severity can be set separately. There are no built-in exemptions: license headers, `TODO` notes and tool directives written as comments are reported like any other comment.

Not reported: preprocessor directives (`#region`, `#pragma`, `#nullable`), shebang lines, disabled `#if` text, HTML comments (`<!-- -->`) and generated code. The one exception to the generated-code rule is a C# comment a person wrote in a Razor file, which is reported at its position in the `.razor` or `.cshtml` file.

## Installation

Add the package to each project you want to check:

```shell
dotnet add package NoComments.Analyzers
```

or in the project file:

```xml
<ItemGroup>
  <PackageReference Include="NoComments.Analyzers" Version="x.y.z" PrivateAssets="all" />
</ItemGroup>
```

To apply it to every project in a repository, add the `PackageReference` to a `Directory.Build.props` file.

The rules are warnings by default. To make comments fail the build, set `TreatWarningsAsErrors` to `true`, or raise individual rules in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.NOC0001.severity = error
dotnet_diagnostic.NOC0002.severity = error
dotnet_diagnostic.NOC0003.severity = none
```

The package needs Visual Studio 17.14 or later, or the .NET SDK 9.0.300 or later.

## Documentation comments, CS1591 and `GenerateDocumentationFile`

Whether `///` and `/** */` are documentation comments depends on `GenerateDocumentationFile`:

- `true`: they are documentation comments, reported as **NOC0003**. A block of `///` lines is one diagnostic.
- `false`: the compiler reads them as ordinary comments. Each `///` line is reported as **NOC0001**, and each `/** */` comment as **NOC0002**.

Switching `GenerateDocumentationFile` therefore moves these comments between NOC0003 and NOC0001/NOC0002. Check your `.editorconfig` severities and suppressions when you change it.

With `GenerateDocumentationFile` set to `true`, the compiler also reports CS1591 ("Missing XML comment for publicly visible type or member") for every public member without a documentation comment. That clashes with NOC0003, which asks you to remove those comments. Either add `CS1591` to `<NoWarn>`, or set `GenerateDocumentationFile` to `false`. NoComments does not change either setting for you.

## Suppression

NOC0001 to NOC0003 support the standard .NET suppression mechanisms: `.editorconfig` severity, `#pragma warning disable`, `[SuppressMessage]`, global suppressions and `<NoWarn>`. There is no comment-based suppression syntax.

NOC0004 can only be suppressed **project-wide**, because Razor comments are reported on the Razor file itself, which has no C# syntax tree. These work:

- `dotnet_diagnostic.NOC0004.severity = none` in a `.globalconfig` file
- `<NoWarn>$(NoWarn);NOC0004</NoWarn>`
- `[assembly: SuppressMessage("Maintainability", "NOC0004")]` with no target

`.editorconfig` severity and `#pragma warning disable` have no effect on NOC0004, and it can't be suppressed for a single file.

## Remove comment code fix

The **Remove comment** code fix deletes a C# comment and tidies the whitespace it leaves behind: a comment on its own line goes with its line, a trailing comment goes with the spaces before it, and no empty lines or fused words are left. Fix All (document, project and solution) gives the same text as removing the comments one at a time. See the rule pages for the full whitespace rules.

In Visual Studio and VS Code with C# Dev Kit, the fix is offered in `.cs` files. To fix a whole codebase from the command line:

```shell
dotnet format analyzers --diagnostics NOC0001 NOC0002 NOC0003
```

## Razor files

The C# in `@code`, `@functions`, `@{ }` and `@( )` blocks is reported under NOC0001 to NOC0003, and `@* *@` Razor comments under NOC0004. There are two editor limits:

- **No IDE fix in Razor files.** The IDEs offer no Remove comment action in `.razor` and `.cshtml` files. Bulk-fix the C# comments in Razor files from the command line instead; the fix edits the Razor file, never the generated C#:

  ```shell
  dotnet format analyzers --diagnostics NOC0001 NOC0002 NOC0003 --include-generated
  ```

- **NOC0004 shows on build only.** Razor comments are reported when the project builds, not while you edit the file. NOC0004 has no code fix; remove Razor comments by hand.

## ReSharper

ReSharper can hide Visual Studio squiggles that duplicate its own inspections, and with that setting on, NOC diagnostics may not be underlined in the editor. To see NOC squiggles, turn off ReSharper's "hide Visual Studio squiggles that duplicate ReSharper" option. The diagnostics are still reported in the Error List and on build either way.

## Overlapping Sonar rules

If you also use SonarAnalyzer.CSharp (for example through OpinionatedDotNetCodingStandards), these Sonar rules report some of the same comments, so one comment can get two diagnostics:

- **S125**: Sections of code should not be commented out
- **S1135**: Track uses of "TODO" tags
- **S1134**: Track uses of "FIXME" tags
- **S4663**: Comments should not be empty

Removing the comment clears both. You can also turn off the Sonar rules in `.editorconfig` and let NoComments report every comment.

## License

[MIT](https://github.com/LeeSanderson/NoComments.Analyzers/blob/main/LICENSE)
