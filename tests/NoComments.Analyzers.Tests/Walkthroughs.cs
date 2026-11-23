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
            ["06 Documentation comments"] = (
                """
                /// <summary>
                /// Adds two numbers.
                /// </summary>
                /// <param name="a">First.</param>
                public int Add(int a, int b) => a + b;

                /** <summary>Old style.</summary> */
                public int Sub(int a, int b) => a - b;

                """,
                """
                public int Add(int a, int b) => a + b;

                public int Sub(int a, int b) => a - b;

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
            ["08 #region and #if"] = (
                """
                #region Helpers // part of the region name
                    // helper comment
                    void Help() { }
                #endregion
                #if DEBUG // a real comment
                #endif

                """,
                """
                #region Helpers // part of the region name
                    void Help() { }
                #endregion
                #if DEBUG
                #endif

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
            ["11 C# from a Razor @code block"] = (
                """
                class Counter
                {
                    // current value
                    private int count; // starts at zero

                    private void Up() => count++; /* bump */
                }

                """,
                """
                class Counter
                {
                    private int count;

                    private void Up() => count++;
                }

                """),
            ["12 C# from a Razor code block"] = (
                """
                void Render()
                {
                    var total = 3; // three
                }

                """,
                """
                void Render()
                {
                    var total = 3;
                }

                """),
        };
}
