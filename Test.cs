using System;
using MarkdownViewer.Core;

class MarkdownParserTest
{
    static int passed = 0;
    static int failed = 0;

    static void Main()
    {
        Console.WriteLine("=== MarkdownParser Unit Tests ===\n");

        TestEmpty();
        TestHeadings();
        TestInlineFormatting();
        TestLists();
        TestCodeBlock();
        TestBlockquote();
        TestTable();
        TestHr();
        TestSpecialChars();
        TestMultiParagraph();
        TestNestedBlockquote();

        Console.WriteLine("\n=== Results ===");
        Console.WriteLine("Passed: " + passed);
        Console.WriteLine("Failed: " + failed);

        if (failed > 0)
        {
            Console.WriteLine("\nTESTS FAILED!");
            Environment.Exit(1);
        }
        Console.WriteLine("\nALL TESTS PASSED!");
    }

    static void TestEmpty()
    {
        Assert("Empty string returns body", MarkdownParser.Parse("").Html != null && MarkdownParser.Parse("").Html.Contains("<body>无内容</body>"));
        Assert("Null returns body", MarkdownParser.Parse(null).Html != null && MarkdownParser.Parse(null).Html.Contains("<body>无内容</body>"));
    }

    static void TestHeadings()
    {
        Assert("H1", MarkdownParser.Parse("# Hello").Html.Contains("<h1") && MarkdownParser.Parse("# Hello").Html.Contains("Hello</h1>"));
        Assert("H2", MarkdownParser.Parse("## Hello").Html.Contains("<h2") && MarkdownParser.Parse("## Hello").Html.Contains("Hello</h2>"));
        Assert("H3", MarkdownParser.Parse("### Hello").Html.Contains("<h3") && MarkdownParser.Parse("### Hello").Html.Contains("Hello</h3>"));
        Assert("H6", MarkdownParser.Parse("###### Hello").Html.Contains("<h6") && MarkdownParser.Parse("###### Hello").Html.Contains("Hello</h6>"));
    }

    static void TestInlineFormatting()
    {
        Assert("Bold", MarkdownParser.Parse("**bold**").Html.Contains("<strong>bold</strong>"));
        Assert("Italic", MarkdownParser.Parse("*italic*").Html.Contains("<em>italic</em>"));
        Assert("BoldItalic", MarkdownParser.Parse("***bold***").Html.Contains("<strong>") && MarkdownParser.Parse("***bold***").Html.Contains("<em>"));
        Assert("Strikethrough", MarkdownParser.Parse("~~del~~").Html.Contains("<del>del</del>"));
        Assert("InlineCode", MarkdownParser.Parse("`code`").Html.Contains("<code>code</code>"));
        Assert("Link", MarkdownParser.Parse("[text](http://example.com)").Html.Contains("<a href=") && MarkdownParser.Parse("[text](http://example.com)").Html.Contains("text</a>"));
        Assert("Image", MarkdownParser.Parse("![alt](http://example.com/img.png)").Html.Contains("<img src=") && MarkdownParser.Parse("![alt](http://example.com/img.png)").Html.Contains("alt"));
    }

    static void TestLists()
    {
        Assert("UnorderedList dash", MarkdownParser.Parse("- item").Html.Contains("<li>item</li>"));
        Assert("UnorderedList asterisk", MarkdownParser.Parse("* item").Html.Contains("<li>item</li>"));
        Assert("UnorderedList plus", MarkdownParser.Parse("+ item").Html.Contains("<li>item</li>"));
        Assert("OrderedList", MarkdownParser.Parse("1. item").Html.Contains("<li>item</li>"));
        Assert("TaskList unchecked", MarkdownParser.Parse("- [ ] todo").Html.Contains("type=\"checkbox\""));
        Assert("TaskList checked", MarkdownParser.Parse("- [x] done").Html.Contains("checked"));
    }

    static void TestCodeBlock()
    {
        string result = MarkdownParser.Parse("```\ncode\nline2\n```").Html;
        Assert("CodeBlock", result.Contains("<pre>") && result.Contains("code") && result.Contains("line2"));
    }

    static void TestBlockquote()
    {
        string result = MarkdownParser.Parse("> quote").Html;
        Assert("Blockquote", result.Contains("<blockquote>") && result.Contains("quote"));
    }

    static void TestTable()
    {
        string input = "| a | b |\n|---|---|\n| 1 | 2 |";
        string result = MarkdownParser.Parse(input).Html;
        Assert("Table", result.Contains("<table>") && result.Contains("<th>") && result.Contains("<td>"));
    }

    static void TestHr()
    {
        Assert("Hr dash", MarkdownParser.Parse("---").Html.Contains("<hr"));
        Assert("Hr asterisk", MarkdownParser.Parse("***").Html.Contains("<hr"));
        Assert("Hr underscore", MarkdownParser.Parse("___").Html.Contains("<hr"));
    }

    static void TestSpecialChars()
    {
        Assert("HtmlEncode &", MarkdownParser.Parse("a & b").Html.Contains("&amp;"));
    }

    static void TestMultiParagraph()
    {
        string result = MarkdownParser.Parse("para1\n\npara2").Html;
        Assert("Multi paragraph", result.Contains("<p>para1</p>") && result.Contains("<p>para2</p>"));
    }

    static void TestNestedBlockquote()
    {
        string result = MarkdownParser.Parse("> line1\n> line2").Html;
        Assert("Nested blockquote", result.Contains("<blockquote>") && result.Contains("line1") && result.Contains("line2"));
    }

    static void Assert(string name, bool condition)
    {
        if (condition)
        {
            Console.WriteLine("[PASS] " + name);
            passed++;
        }
        else
        {
            Console.WriteLine("[FAIL] " + name);
            failed++;
        }
    }
}
