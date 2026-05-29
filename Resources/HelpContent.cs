namespace MarkdownViewer.Resources
{
    static class HelpContent
    {
        public static string Html = @"
<html><head><meta charset='utf-8'><style>
body{font-family:'Segoe UI', 'Microsoft YaHei', sans-serif;padding:25px;max-width:700px;margin:0 auto}
h1{color:#407040;border-bottom:2px solid #407040;padding-bottom:10px}
h2{color:#505050;margin-top:25px}
p{margin:10px 0;line-height:1.6}
code{background:#f5f5f5;padding:2px 7px;border-radius:4px;font-family:Consolas;font-size:14px}
ul{margin:10px 0;padding-left:20px}
li{margin:5px 0}
table{border-collapse:collapse;margin:15px 0}
table,th,td{border:1px solid #ddd;padding:8px 12px}
th{background:#f5f5f5;font-weight:600}
tr:nth-child(even){background:#fafafa}
</style></head><body>
<h1>Markdown Viewer 帮助</h1>
<h2>简介</h2>
<p>Markdown Viewer 是一个简洁高效的 Markdown 文件查看和编辑工具。</p>
<h2>快捷键</h2>
<table>
<tr><th>快捷键</th><th>功能</th></tr>
<tr><td>Ctrl+N</td><td>新建文件</td></tr>
<tr><td>Ctrl+O</td><td>打开文件</td></tr>
<tr><td>Ctrl+S</td><td>保存文件</td></tr>
<tr><td>Ctrl+Shift+S</td><td>另存为</td></tr>
<tr><td>Ctrl+E</td><td>切换到编辑模式</td></tr>
<tr><td>Ctrl+P</td><td>切换到预览模式</td></tr>
<tr><td>Ctrl+F</td><td>查找和替换</td></tr>
<tr><td>F1</td><td>查看帮助</td></tr>
<tr><td>Esc</td><td>关闭帮助对话框</td></tr>
<tr><td>拖拽</td><td>拖拽 md 文件到窗口打开</td></tr>
</table>
<h2>Markdown 语法支持</h2>
<table>
<tr><th>语法</th><th>效果</th></tr>
<tr><td># ## ###</td><td>标题</td></tr>
<tr><td>**text**</td><td>粗体</td></tr>
<tr><td>*text*</td><td>斜体</td></tr>
<tr><td>`code`</td><td>行内代码</td></tr>
<tr><td>```</td><td>代码块</td></tr>
<tr><td>&gt; text</td><td>引用</td></tr>
<tr><td>- * +</td><td>无序列表</td></tr>
<tr><td>1. 2.</td><td>有序列表</td></tr>
<tr><td>[文本](链接)</td><td>链接</td></tr>
<tr><td>![alt](图片)</td><td>图片</td></tr>
<tr><td>---</td><td>分隔线</td></tr>
</table>
<h2>版本</h2>
<p>v1.6</p>
</body></html>";
    }
}
