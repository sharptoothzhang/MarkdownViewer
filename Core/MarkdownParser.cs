using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Markdig;

namespace MarkdownViewer.Core
{
    public class ParseResult
    {
        public string Html;
        public bool HasMermaid;
        public List<TitleItem> Headings;
    }

    public class TitleItem
    {
        public int Level;
        public string Text;
        public string Anchor;
        public int LineNumber;
    }

    static class MarkdownParser
    {
        static MarkdownPipeline pipeline;

        static MarkdownParser()
        {
            pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();
        }

        public static ParseResult Parse(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
            {
                return new ParseResult
                {
                    Html = "<body>无内容</body>",
                    HasMermaid = false,
                    Headings = new List<TitleItem>()
                };
            }

            markdown = PreprocessMarkdown(markdown);

            var mermaidBlocks = new System.Collections.Generic.List<string>();
            string processed = Regex.Replace(
                markdown,
                @"```mermaid\s*\n([\s\S]*?)```",
                delegate(Match m)
                {
                    string code = m.Groups[1].Value.Trim();
                    code = FixMermaidSyntax(code);
                    int index = mermaidBlocks.Count;
                    mermaidBlocks.Add(code);
                    return "\n%%MERMAID_" + index + "%%\n";
                }
            );

            string bodyHtml = Markdig.Markdown.ToHtml(processed, pipeline);

            for (int i = 0; i < mermaidBlocks.Count; i++)
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(mermaidBlocks[i]);
                string b64 = Convert.ToBase64String(bytes);
                string placeholder = "<p>%%MERMAID_" + i + "%%</p>";
                string replacement = "<div class=\"mermaid\" data-b64=\"" + b64 + "\"></div>";
                if (bodyHtml.Contains(placeholder))
                    bodyHtml = bodyHtml.Replace(placeholder, replacement);
                else
                    bodyHtml = bodyHtml.Replace("%%MERMAID_" + i + "%%", replacement);
            }

            string fullHtml = "<body>" + bodyHtml + "</body>";
            return new ParseResult
            {
                Html = fullHtml,
                HasMermaid = mermaidBlocks.Count > 0,
                Headings = ExtractHeadingsFromHtml(fullHtml)
            };
        }

        public static List<TitleItem> ParseHeadings(string markdown)
        {
            if (string.IsNullOrEmpty(markdown)) return new List<TitleItem>();
            var result = Parse(markdown);
            return ExtractHeadingsFromHtml(result.Html);
        }

        static List<TitleItem> ExtractHeadingsFromHtml(string html)
        {
            var headings = new List<TitleItem>();
            var matches = Regex.Matches(html, @"<h([1-6])([^>]*)>(.*?)</h\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            foreach (System.Text.RegularExpressions.Match m in matches)
            {
                int level = int.Parse(m.Groups[1].Value);
                string attrs = m.Groups[2].Value;
                string text = Regex.Replace(m.Groups[3].Value, @"<[^>]+>", "");
                string anchor = "";
                System.Text.RegularExpressions.Match idMatch = Regex.Match(attrs, @"id=""([^""]*)""", RegexOptions.IgnoreCase);
                if (idMatch.Success)
                    anchor = idMatch.Groups[1].Value;
                if (!string.IsNullOrEmpty(anchor))
                    headings.Add(new TitleItem { Level = level, Text = text, Anchor = anchor, LineNumber = 0 });
            }
            return headings;
        }

        static string GenerateAnchor(string text)
        {
            string anchor = text.ToLower();
            anchor = Regex.Replace(anchor, @"[^\w\u4e00-\u9fa5]+", "");
            if (anchor.Length == 0) anchor = "section";
            return anchor;
        }

        static string FixMermaidSyntax(string code)
        {
            string[] lines = code.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                // 修复反向虚线箭头: A<--B: msg → B-->>A: msg
                line = Regex.Replace(line, @"(\w+)<--(\w+):", "$2-->>$1:");

                // 修复 flowchart 节点: Node[text<br/>...] → Node["text<br/>..."]
                if (Regex.IsMatch(line, @"\[[^\]]*<br\s*/?>[^\]]*\]") && !line.Contains("\""))
                {
                    line = Regex.Replace(line, @"(\w+)\[([^\]]+)\]", delegate(Match m)
                    {
                        string node = m.Groups[1].Value;
                        string text = m.Groups[2].Value;
                        if (text.Contains("<br") && !text.StartsWith("\""))
                            return node + "[\"" + text + "\"]";
                        return m.Value;
                    });
                }

                lines[i] = line;
            }
            return string.Join("\n", lines);
        }

        static string PreprocessMarkdown(string markdown)
        {
            string fixedText = Regex.Replace(markdown, @"\*\*([^*]+)\*(?!\*)", "**$1**");
            
            string[] lines = fixedText.Split('\n');
            StringBuilder result = new StringBuilder(fixedText.Length);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (i > 0 && line.TrimStart().StartsWith("|") && !string.IsNullOrWhiteSpace(lines[i - 1]) && !lines[i - 1].Contains("|"))
                    result.Append('\n');
                result.Append(line);
                if (i < lines.Length - 1)
                    result.Append('\n');
            }
            return result.ToString();
        }
    }
}