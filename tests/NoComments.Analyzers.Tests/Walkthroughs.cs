namespace NoComments.Analyzers.Tests;

internal static class Walkthroughs
{
    internal static readonly IReadOnlyDictionary<string, (string Before, string After)> All =
        new Dictionary<string, (string Before, string After)>(StringComparer.Ordinal)
        {
            ["01 Own line"] = (
                """
                class Greeter
                {
                    // Say hello to the user
                    public void Greet() { }
                }

                """,
                """
                class Greeter
                {
                    public void Greet() { }
                }

                """),
            ["02 After code"] = (
                """
                var total = price * quantity; // includes tax
                var label = "a // not a comment";

                """,
                """
                var total = price * quantity;
                var label = "a // not a comment";

                """),
            ["03 Several lines in a row"] = (
                """
                var a = 1;

                // First line of an explanation
                // Second line
                // Third line

                var b = 2;

                """,
                """
                var a = 1;

                var b = 2;

                """),
            ["04 Multi-line comment"] = (
                """
                    /*
                     * Calculates the thing.
                     */
                    int Calc() => 42;

                    int Other(int x) => x /* doubled
                        below */ * 2;

                """,
                """
                    int Calc() => 42;

                    int Other(int x) => x * 2;

                """),
            ["05 Between tokens"] = (
                """
                Call(/* first */ a, b /* second */, c);
                var x = a /* why */ + b;
                return/* no spaces */value;

                """,
                """
                Call(a, b, c);
                var x = a + b;
                return value;

                """),
            ["07 Start and end of file"] = (
                """
                // Copyright (c) Example Ltd.
                // Licensed under MIT.

                namespace Demo;

                public class A { }

                // end of file
                """,
                """
                namespace Demo;

                public class A { }

                """),
            ["09 Empty block"] = (
                """
                void Later()
                {
                    // TODO: implement
                }

                void Inline() { /* nothing yet */ }

                """,
                """
                void Later()
                {
                }

                void Inline() { }

                """),
            ["10 Two comments on one line"] = (
                """
                var a = 1; /* one */ // two
                /* three */ /* four */ var b = 2;
                /* five */ // six

                """,
                """
                var a = 1;
                var b = 2;

                """),
        };
}
