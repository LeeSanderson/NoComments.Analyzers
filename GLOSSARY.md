# NoComments

A Roslyn analyzer package that treats every comment in C# source as a violation, so intent has to be expressed in code.

## Language

**Comment**:
Comment trivia in C# source, in one of the three comment forms. Directives (`#region`, `#pragma`, `#nullable`), shebang lines and disabled `#if` text are not comments.
_Avoid_: Remark, annotation

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

### Scope

**Generated code**:
Source that Roslyn's generated-code detection classifies as generated. Comments in it are never reported.
_Avoid_: Auto-generated code, designer code

**Suppression**:
A standard .NET mechanism that silences a reported diagnostic without changing the comment, such as `[SuppressMessage]`, `#pragma warning disable` or an `.editorconfig` severity of `none`. Comment-based suppression does not exist here.
_Avoid_: Exemption, ignore, allow-list
