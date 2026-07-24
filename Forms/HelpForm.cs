using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Web.WebView2;
using Microsoft.Web.WebView2.WinForms;

namespace MarkdownViewer.Forms
{
    class HelpForm : Form
    {
        WebView2 web;

        public HelpForm()
        {
            Text = "使用说明";
            Size = new System.Drawing.Size(700, 500);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            web = new WebView2();
            web.Dock = DockStyle.Fill;
            Controls.Add(web);

            this.Shown += async delegate(object s, EventArgs e)
            {
                try
                {
                    await web.EnsureCoreWebView2Async();
                    web.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = false;
                    web.CoreWebView2.Settings.AreDevToolsEnabled = false;
                    string helpPath = Path.Combine(Application.StartupPath, "Resources", "help.html");
                    string helpHtml = File.Exists(helpPath) ? File.ReadAllText(helpPath) : "<html><body>帮助文件不存在</body></html>";
                    web.NavigateToString(helpHtml);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("无法加载帮助内容: " + ex.Message, "错误");
                }
            };
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape) { Close(); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}