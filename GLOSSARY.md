# NoComments

A Roslyn analyzer package that treats every comment in C# and Razor source as a violation, so intent has to be expressed in code.

## Language

**Comment**:
A developer's note in C# or Razor source, in one of the comment forms. Directives (`#region`, `#pragma`, `#nullable`), shebang lines, disabled `#if` text and HTML comments (`<!-- -->`) are not comments.
_Avoid_: Remark, annotation

**Razor file**:
A `.razor` or `.cshtml` file. Its C# (in `@code`, `@functions`, `@{ }` and `@( )`) is C# source; its markup is not.

### Comment forms

**Single-line comment**:
A comment starting with `//` that is not a documentation comment.
_Avoid_: Line comment, inline comment

**Multi-line comment**:
A comment delimited by `/*` and `*/` that is not a documentation comment.
_Avoid_: Block comment, delimited comment

**Documentation comment**:
A comment written as `///` or `/** */` that carries XML documentation.
_Avoid_: XML comment, doc comment

**Razor comment**:
A comment delimited by `@*` and `*@` in a Razor file.
_Avoid_: Razor block comment, at-star comment

### Scope

**Generated code**:
Source that Roslyn's generated-code detection classifies as generated. Comments in it are never reported, except comments a person wrote in a Razor file, which Roslyn only sees through the generated code.
_Avoid_: Auto-generated code, designer code

**Suppression**:
A standard .NET mechanism that silences a reported diagnostic without changing the comment, such as `[SuppressMessage]`, `#pragma warning disable` or an `.editorconfig` severity of `none`. Comment-based suppression does not exist here.
_Avoid_: Exemption, ignore, allow-list
