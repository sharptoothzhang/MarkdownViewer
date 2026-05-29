using System;
using System.Text;
using System.Text.RegularExpressions;
using Markdig;

namespace MarkdownViewer.Core
{
    public class ParseResult
    {
        public string Html;
        public bool HasMermaid;
    }

    static class MarkdownParser
    {
        static readonly string CSS_LIGHT = @"<style>
body{font-family:'Segoe UI','Microsoft YaHei',sans-serif;padding:20px;font-size:15px;line-height:1.8;background:#fff;color:#333;overflow-x:hidden;word-wrap:break-word;max-width:100%}
h1{font-size:26px;color:#333;border-bottom:2px solid #407040;padding-bottom:8px}
h2{font-size:22px;color:#444;border-bottom:1px solid #ddd;padding-bottom:5px}
h3{font-size:18px;color:#555}
p{margin:10px 0}
code{background:#f0f0f0;padding:2px 6px;border-radius:3px;font-family:Consolas}
pre{background:#f5f5f5;padding:15px;border-radius:5px;border:1px solid #ddd;overflow-x:auto}
pre code{background:none;padding:0;border:none}
blockquote{border-left:4px solid #407040;margin:15px 0;padding:10px 15px;background:#f9f9f9;color:#555}
a{color:#4070a0}
ul,ol{margin:10px 0;padding-left:25px}
li{margin:5px 0}
hr{border:none;border-top:1px solid #ddd;margin:20px 0}
table{border-collapse:collapse;margin:15px 0}
table,th,td{border:1px solid #ddd;padding:8px 12px}
th{background:#f5f5f5;font-weight:600}
tr:nth-child(even){background:#fafafa}
del{color:#999}
.mermaid{background:#fff;text-align:center;margin:15px 0}
</style>";

        static readonly string CSS_DARK = @"<style>
body{font-family:'Segoe UI','Microsoft YaHei',sans-serif;padding:20px;font-size:15px;line-height:1.8;background:#1e1e1e;color:#d4d4d4;overflow-x:hidden;word-wrap:break-word;max-width:100%}
h1{font-size:26px;color:#fff;border-bottom:2px solid #4caf50;padding-bottom:8px}
h2{font-size:22px;color:#ccc;border-bottom:1px solid #444;padding-bottom:5px}
h3{font-size:18px;color:#aaa}
p{margin:10px 0}
code{background:#2d2d2d;padding:2px 6px;border-radius:3px;font-family:Consolas;color:#ce9178}
pre{background:#252526;padding:15px;border-radius:5px;border:1px solid #444;overflow-x:auto}
pre code{background:none;padding:0;border:none;color:#d4d4d4}
blockquote{border-left:4px solid #4caf50;margin:15px 0;padding:10px 15px;background:#2d2d2d;color:#aaa}
a{color:#6db3f2}
ul,ol{margin:10px 0;padding-left:25px}
li{margin:5px 0}
hr{border:none;border-top:1px solid #444;margin:20px 0}
table{border-collapse:collapse;margin:15px 0}
table,th,td{border:1px solid #444;padding:8px 12px}
th{background:#2d2d2d;font-weight:600}
tr:nth-child(even){background:#252526}
del{color:#6b6b6b}
.mermaid{background:#2d2d2d;text-align:center;margin:15px 0}
</style>";

        static MarkdownPipeline pipeline;

        static MarkdownParser()
        {
            pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();
        }

        public static ParseResult Parse(string markdown)
        {
            return Parse(markdown, false);
        }

        public static ParseResult Parse(string markdown, bool isDark)
        {
            if (string.IsNullOrEmpty(markdown))
            {
                return new ParseResult
                {
                    Html = isDark
                        ? "<html><head><meta charset='utf-8'><style>body{font-family:Segoe UI;padding:20px;background:#1e1e1e;color:#888}</style></head><body>无内容</body></html>"
                        : "<html><head><meta charset='utf-8'><style>body{font-family:Segoe UI;padding:20px;color:#888}</style></head><body>无内容</body></html>",
                    HasMermaid = false
                };
            }

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

            StringBuilder html = new StringBuilder();
            html.Append("<html><head><meta charset='utf-8'>");
            html.Append(isDark ? CSS_DARK : CSS_LIGHT);
            html.Append("</head><body>");
            html.Append(bodyHtml);
            html.Append("</body></html>");
            return new ParseResult { Html = html.ToString(), HasMermaid = mermaidBlocks.Count > 0 };
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
    }
}
