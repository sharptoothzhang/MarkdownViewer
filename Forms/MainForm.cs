using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Win32;
using Microsoft.Web.WebView2;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using MarkdownViewer.Core;
using MarkdownViewer.Resources;
using MarkdownViewer.Hooks;

namespace MarkdownViewer.Forms
{
    [System.Runtime.InteropServices.ComVisible(true)]
    public class MainForm : Form
    {
        RichTextBox Editor;
        WebView2 Preview;
        ToolStripButton ViewToggleBtn;
        FindReplaceDialog _findDlg;
        StatusStrip StatusBar;
        ToolStripStatusLabel StatusLabel;
        ToolStripStatusLabel ZoomValueLabel;
        string CurrentFile = "";
        bool IsPreviewMode = true;
        bool IsDirty = false;
        float ZoomLevel = 100f;
        Image editIcon;
        Image eyeIcon;
        string cachedHtml = null;
        string cachedText = null;
        bool IsDarkMode = false;
        System.Windows.Forms.Timer previewDebounceTimer;
        public bool EnableDebugLog = false;
        enum FileType { Unknown, Markdown, Text, Image, Pdf }
        FileType currentFileType = FileType.Unknown;

        public MainForm()
        {
            Text = "Markdown Viewer";
            Size = new Size(1000, 700);
            StartPosition = FormStartPosition.CenterScreen;
            FormClosing += OnFormClosing;
            KeyPreview = true;
            KeyDown += OnKeyDown;
            MouseWheel += OnMouseWheel;

            string iconPath = Path.Combine(Application.StartupPath, "app.ico");
            if (File.Exists(iconPath)) Icon = new Icon(iconPath);

            Log("APP", "StartupPath=" + Application.StartupPath);
            eyeIcon = Icons.GetEyeIcon();
            editIcon = Icons.GetEditIcon();

            AllowDrop = true;
            DragEnter += OnDragEnter;
            DragDrop += OnDragDrop;

            Editor = new RichTextBox();
            Editor.Multiline = true;
            Editor.ScrollBars = RichTextBoxScrollBars.Both;
            Editor.Font = new Font("Consolas", 12);
            Editor.Dock = DockStyle.Fill;
            Editor.Visible = false;
            Editor.AcceptsTab = true;
            Editor.WordWrap = true;
            Editor.TextChanged += OnTextChanged;
            Controls.Add(Editor);

            Preview = new WebView2();
            Preview.Dock = DockStyle.Fill;
            Preview.Size = this.ClientSize;
            Preview.PreviewKeyDown += OnPreviewKeyDown;
            Controls.Add(Preview);

            SetupStatusBar();
            SetupMenu();

            previewDebounceTimer = new System.Windows.Forms.Timer();
            previewDebounceTimer.Interval = 300;
            previewDebounceTimer.Tick += OnPreviewDebounceTick;

            this.Shown += OnFormShown;
        }

        async void OnFormShown(object sender, EventArgs e)
        {
            try
            {
                await Preview.EnsureCoreWebView2Async();
                Preview.CoreWebView2.Settings.IsScriptEnabled = true;
                Preview.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = false;
                Preview.CoreWebView2.Settings.IsWebMessageEnabled = true;
                Preview.CoreWebView2.Settings.AreDevToolsEnabled = false;
                Preview.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
                Preview.CoreWebView2.AddHostObjectToScript("app", this);
                Preview.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "appassets.local",
                    Application.StartupPath,
                    CoreWebView2HostResourceAccessKind.Allow);
                Log("PREVIEW", "WebView2 initialized, virtual host mapped");
                KeyHook.Install(OnGlobalKeyDown);
                Preview.CoreWebView2.NavigateToString("<html><body></body></html>");
                Log("PREVIEW", "Preload page ready");
                if (IsPreviewMode && !string.IsNullOrEmpty(Editor.Text))
                {
                    Log("PREVIEW", "DeferredRefresh after WebView2 init");
                    RefreshPreview();
                }
            }
            catch (Exception ex)
            {
                Log("PREVIEW", "WebView2 init error: " + ex.Message);
                MessageBox.Show(
                    "WebView2 初始化失败，预览功能不可用。\n\n" +
                    "请安装 Microsoft Edge WebView2 Runtime：\n" +
                    "https://developer.microsoft.com/en-us/microsoft-edge/webview2/\n\n" +
                    "错误信息: " + ex.Message,
                    " WebView2 错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string msg = e.TryGetWebMessageAsString();
                Log("JS_MESSAGE", "msg=" + msg);
                if (msg.StartsWith("KEY_"))
                {
                    string key = msg.Substring(4);
                    this.BeginInvoke(new Action(() =>
                    {
                        switch (key)
                        {
                            case "N": NewFile(); break;
                            case "O": OpenFileDialog(); break;
                            case "P": SwitchToPreview(); break;
                            case "E": SwitchToEdit(); break;
                            case "F": ShowFindReplace(); break;
                            case "S": SaveFile(); break;
                        }
                    }));
                }
                else if (msg == "MERMAID_OK" || msg == "MERMAID_SKIP")
                {
                    Log("JS_MESSAGE", "Mermaid ready=" + msg);
                }
            }
            catch (Exception ex)
            {
                Log("JS_MESSAGE", "error: " + ex.Message);
            }
        }

        void OnGlobalKeyDown(int vkCode)
        {
            this.BeginInvoke(new Action(() =>
            {
                switch (vkCode)
                {
                    case 0x4E: NewFile(); break;
                    case 0x4F: OpenFileDialog(); break;
                    case 0x50: SwitchToPreview(); break;
                    case 0x45: SwitchToEdit(); break;
                    case 0x46:
                        if (!IsPreviewMode) ShowFindReplace();
                        break;
                    case 0x53: SaveFile(); break;
                }
            }));
        }

        void OnPreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.Control)
            {
                switch (e.KeyCode)
                {
                    case Keys.N: NewFile(); e.IsInputKey = true; break;
                    case Keys.O: OpenFileDialog(); e.IsInputKey = true; break;
                    case Keys.S: SaveFile(); e.IsInputKey = true; break;
                    case Keys.E: SwitchToEdit(); e.IsInputKey = true; break;
                    case Keys.P: SwitchToPreview(); e.IsInputKey = true; break;
                    case Keys.F: ShowFindReplace(); e.IsInputKey = true; break;
                }
            }
        }


        public void ReportScriptError(string message, string url, int line)
        {
            Log("JS_ERROR", "msg=" + message + " url=" + url + " line=" + line);
        }

        void SetupStatusBar()
        {
            StatusBar = new StatusStrip();
            StatusLabel = new ToolStripStatusLabel("就绪");
            StatusLabel.Spring = true;

            ToolStripStatusLabel sep1 = new ToolStripStatusLabel("|");

            ToolStripStatusLabel encLabel = new ToolStripStatusLabel("UTF-8");

            ToolStripStatusLabel sep2 = new ToolStripStatusLabel("|");

            ToolStripButton zoomOut = new ToolStripButton("−");
            zoomOut.Click += delegate(object s, EventArgs e) { Zoom(-10); };
            ZoomValueLabel = new ToolStripStatusLabel("100%");
            ZoomValueLabel.TextAlign = ContentAlignment.MiddleCenter;
            ToolStripButton zoomIn = new ToolStripButton("+");
            zoomIn.Click += delegate(object s, EventArgs e) { Zoom(10); };

            StatusBar.Items.AddRange(new ToolStripItem[] { StatusLabel, sep1, encLabel, sep2, zoomOut, ZoomValueLabel, zoomIn });
            Controls.Add(StatusBar);
        }

        void SetupMenu()
        {
            MenuStrip menu = new MenuStrip();
            MainMenuStrip = menu;

            ToolStripMenuItem fileMenu = new ToolStripMenuItem("文件(&F)");
            fileMenu.DropDownItems.Add("新建(&N)\tCtrl+N", null, delegate(object s, EventArgs e) { NewFile(); });
            fileMenu.DropDownItems.Add("打开(&O)...\tCtrl+O", null, delegate(object s, EventArgs e) { OpenFileDialog(); });

            ToolStripMenuItem recentMenu = new ToolStripMenuItem("最近文件(&R)");
            UpdateRecentFilesMenu(recentMenu);
            fileMenu.DropDownItems.Add(recentMenu);

            fileMenu.DropDownItems.Add("保存(&S)\tCtrl+S", null, delegate(object s, EventArgs e) { SaveFile(); });
            fileMenu.DropDownItems.Add("另存为(&A)...\tCtrl+Shift+S", null, delegate(object s, EventArgs e) { SaveFileAs(); });
            fileMenu.DropDownItems.Add(new ToolStripSeparator());

            ToolStripMenuItem assocMenu = new ToolStripMenuItem("关联(&S)");
            assocMenu.DropDownItems.Add("注册.md关联", null, delegate(object s, EventArgs e) { RegisterAssoc(true); });
            assocMenu.DropDownItems.Add("取消.md关联", null, delegate(object s, EventArgs e) { RegisterAssoc(false); });
            fileMenu.DropDownItems.Add(assocMenu);

            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("退出(&X)\tAlt+F4", null, delegate(object s, EventArgs e) { Close(); });
            menu.Items.Add(fileMenu);

            ToolStripMenuItem viewMenu = new ToolStripMenuItem("视图(&V)");
            viewMenu.DropDownItems.Add("编辑模式(&E)\tCtrl+E", null, delegate(object s, EventArgs e) { SwitchToEdit(); });
            viewMenu.DropDownItems.Add("预览模式(&P)\tCtrl+P", null, delegate(object s, EventArgs e) { SwitchToPreview(); });
            viewMenu.DropDownItems.Add(new ToolStripSeparator());
            viewMenu.DropDownItems.Add("深色模式(&D)", null, delegate(object s, EventArgs e) { ToggleDarkMode(); });
            viewMenu.DropDownItems.Add("查找替换(&F)...\tCtrl+F", null, delegate(object s, EventArgs e) { ShowFindReplace(); });
            menu.Items.Add(viewMenu);

            ToolStripMenuItem helpMenu = new ToolStripMenuItem("帮助(&H)");
            helpMenu.DropDownItems.Add("使用说明(&U)\tF1", null, delegate(object s, EventArgs e) { ShowHelp(); });
            helpMenu.DropDownItems.Add("关于(&A)", null, delegate(object s, EventArgs e) { ShowAbout(); });
            menu.Items.Add(helpMenu);

            ToolStripStatusLabel spring = new ToolStripStatusLabel();
            menu.Items.Add(spring);
            ViewToggleBtn = new ToolStripButton("预览");
            ViewToggleBtn.Image = eyeIcon;
            ViewToggleBtn.ImageScaling = ToolStripItemImageScaling.None;
            ViewToggleBtn.Click += delegate(object s, EventArgs e) { ToggleView(); };
            menu.Items.Add(ViewToggleBtn);

            Controls.Add(menu);
        }

        void UpdateRecentFilesMenu(ToolStripMenuItem recentMenu)
        {
            recentMenu.DropDownItems.Clear();
            string[] files = RecentFiles.Files;
            if (files.Length == 0)
            {
                recentMenu.Enabled = false;
            }
            else
            {
                recentMenu.Enabled = true;
                foreach (string f in files)
                {
                    string name = Path.GetFileName(f);
                    ToolStripMenuItem item = new ToolStripMenuItem(name, null, delegate(object s, EventArgs e) { OpenFile(f); });
                    item.ToolTipText = f;
                    recentMenu.DropDownItems.Add(item);
                }
                recentMenu.DropDownItems.Add(new ToolStripSeparator());
                recentMenu.DropDownItems.Add("清除列表", null, delegate(object s, EventArgs e) { RecentFiles.Clear(); UpdateRecentFilesMenu(recentMenu); });
            }
        }

        void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control)
            {
                switch (e.KeyCode)
                {
                    case Keys.N: NewFile(); e.SuppressKeyPress = true; break;
                    case Keys.O: OpenFileDialog(); e.SuppressKeyPress = true; break;
                    case Keys.S: SaveFile(); e.SuppressKeyPress = true; break;
                    case Keys.E: SwitchToEdit(); e.SuppressKeyPress = true; break;
                    case Keys.P: SwitchToPreview(); e.SuppressKeyPress = true; break;
                    case Keys.F: ShowFindReplace(); e.SuppressKeyPress = true; break;
                    case Keys.H: ShowFindReplace(true); e.SuppressKeyPress = true; break;
                }
                if (e.Shift && e.KeyCode == Keys.S) { SaveFileAs(); e.SuppressKeyPress = true; }
            }
            else if (e.KeyCode == Keys.F1)
            {
                ShowHelp();
                e.SuppressKeyPress = true;
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if ((keyData & Keys.Control) != 0)
            {
                switch (keyData & Keys.KeyCode)
                {
                    case Keys.N: NewFile(); return true;
                    case Keys.O: OpenFileDialog(); return true;
                    case Keys.S:
                        if ((keyData & Keys.Shift) != 0) SaveFileAs();
                        else SaveFile();
                        return true;
                    case Keys.E: SwitchToEdit(); return true;
                    case Keys.P: SwitchToPreview(); return true;
                    case Keys.F: ShowFindReplace(); return true;
                    case Keys.H: ShowFindReplace(true); return true;
                }
            }
            else if (keyData == Keys.F1)
            {
                ShowHelp();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        void OnMouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta != 0 && (Control.ModifierKeys & Keys.Control) != 0)
            {
                Zoom(e.Delta > 0 ? 10 : -10);
            }
        }

        void ToggleDarkMode()
        {
            IsDarkMode = !IsDarkMode;
            ApplyTheme();
        }

        void ApplyTheme()
        {
            if (IsDarkMode)
            {
                Editor.BackColor = Color.FromArgb(30, 30, 30);
                Editor.ForeColor = Color.FromArgb(220, 220, 220);
                BackColor = Color.FromArgb(45, 45, 45);
            }
            else
            {
                Editor.BackColor = Color.White;
                Editor.ForeColor = Color.Black;
                BackColor = SystemColors.Control;
            }
            cachedHtml = null;
            cachedText = null;
            if (IsPreviewMode) RefreshPreview();
        }

        void OnDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        void OnDragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    string ext = System.IO.Path.GetExtension(files[0]).ToLower();
                    bool isMdOrTxt = (ext == ".md" || ext == ".txt");
                    if (!isMdOrTxt)
                    {
                        MessageBox.Show("只支持拖拽 Markdown (.md) 和文本 (.txt) 文件", "不支持的文件类型", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    OpenFile(files[0]);
                }
            }
        }

        void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            KeyHook.Uninstall();
            if (!PromptSave()) e.Cancel = true;
        }

        void OnTextChanged(object sender, EventArgs e)
        {
            IsDirty = true;
            cachedHtml = null;
            UpdateStatusBar();
            previewDebounceTimer.Stop();
            previewDebounceTimer.Start();
        }

        void OnPreviewDebounceTick(object sender, EventArgs e)
        {
            previewDebounceTimer.Stop();
            RefreshPreview();
        }

        void NewFile()
        {
            if (!PromptSave()) return;
            CurrentFile = "";
            currentFileType = FileType.Markdown;
            Editor.Text = "";
            cachedHtml = null;
            cachedText = null;
            string mode = IsPreviewMode ? "[预览]" : "[编辑]";
            Text = "无标题 " + mode + " - Markdown Viewer";
            IsDirty = false;
            if (IsPreviewMode) RefreshPreview();
        }

        void OpenFileDialog()
        {
            OpenFileDialog d = new OpenFileDialog();
            d.Filter = "Markdown 文件|*.md|文本文件|*.txt|图片文件|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp|PDF 文件|*.pdf|所有文件|*.*";
            if (d.ShowDialog() == DialogResult.OK) OpenFile(d.FileName);
        }

        public void OpenFile(string path)
        {
            if (!PromptSave()) return;
            try
            {
                CurrentFile = Path.GetFullPath(path);
                string ext = Path.GetExtension(CurrentFile).ToLower();
                if (ext == ".md")
                    currentFileType = FileType.Markdown;
                else if (ext == ".txt")
                    currentFileType = FileType.Text;
                else if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".gif" || ext == ".bmp" || ext == ".webp")
                    currentFileType = FileType.Image;
                else if (ext == ".pdf")
                    currentFileType = FileType.Pdf;
                else
                    currentFileType = FileType.Unknown;
                LoadFileForPreview(CurrentFile);
                string mode = IsPreviewMode ? "[预览]" : "[编辑]";
                Text = Path.GetFileName(CurrentFile) + " " + mode + " - Markdown Viewer";
                IsDirty = false;
                RecentFiles.Add(CurrentFile);
                UpdateStatusBar();
                UpdateEditState();
            }
            catch (Exception ex)
            {
                MessageBox.Show("打开文件失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void LoadFileForPreview(string path)
        {
            if (currentFileType == FileType.Text)
            {
                Editor.Text = File.ReadAllText(path);
                SwitchToEdit();
            }
            else if (currentFileType == FileType.Markdown)
            {
                Editor.Text = File.ReadAllText(path);
                if (IsPreviewMode) RefreshPreview();
            }
            else if (currentFileType == FileType.Image)
            {
                Editor.Text = "";
                if (IsPreviewMode) PreviewImage(path);
            }
            else if (currentFileType == FileType.Pdf)
            {
                Editor.Text = "";
                if (IsPreviewMode) PreviewPdf(path);
            }
            else
            {
                Editor.Text = File.ReadAllText(path);
                if (IsPreviewMode) RefreshPreview();
            }
        }

        void PreviewImage(string path)
        {
            try
            {
                byte[] imageBytes = File.ReadAllBytes(path);
                string base64 = Convert.ToBase64String(imageBytes);
                string ext = Path.GetExtension(path).ToLower();
                string mimeType = ext == ".png" ? "image/png" : ext == ".jpg" || ext == ".jpeg" ? "image/jpeg" : ext == ".gif" ? "image/gif" : ext == ".webp" ? "image/webp" : "image/bmp";
                string dataUri = "data:" + mimeType + ";base64," + base64;
                string imgHtml = "<!DOCTYPE html><html><head><meta charset='utf-8'><style>body{margin:0;display:flex;justify-content:center;align-items:center;min-height:100vh;background:#f0f0f0}img{max-width:100%;max-height:100vh}</style></head><body><img src='" + dataUri + "'></body></html>";
                Preview.CoreWebView2.NavigateToString(imgHtml);
            }
            catch (Exception ex)
            {
                Log("PREVIEW", "PreviewImage failed: " + ex.Message);
            }
        }

        void PreviewPdf(string path)
        {
            try
            {
                string tempDir = Path.Combine(Application.StartupPath, "temp");
                if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
                string fileName = Path.GetFileName(path);
                string tempPath = Path.Combine(tempDir, fileName);
                File.Copy(path, tempPath, true);
                string pdfUrl = "http://appassets.local/temp/" + fileName;
                string pdfHtml = "<!DOCTYPE html><html><head><meta charset='utf-8'><title>PDF</title><style>body{margin:0;height:100vh;display:flex}iframe{flex:1;border:none}</style></head><body><iframe src='" + pdfUrl + "'></iframe></body></html>";
                Preview.CoreWebView2.NavigateToString(pdfHtml);
            }
            catch (Exception ex)
            {
                Log("PREVIEW", "PreviewPdf failed: " + ex.Message);
            }
        }

        bool CanEdit { get { return currentFileType == FileType.Markdown || currentFileType == FileType.Text; } }

        void UpdateEditState()
        {
            if (currentFileType == FileType.Text)
            {
                Editor.ReadOnly = false;
                ViewToggleBtn.Enabled = false;
                if (!IsPreviewMode) SwitchToEdit();
            }
            else if (CanEdit)
            {
                Editor.ReadOnly = false;
                ViewToggleBtn.Enabled = true;
            }
            else
            {
                Editor.ReadOnly = true;
                ViewToggleBtn.Enabled = false;
                if (!IsPreviewMode) SwitchToPreview();
            }
        }

        void SaveFile()
        {
            if (!CanEdit) return;
            if (CurrentFile.Length == 0) { SaveFileAs(); return; }
            try
            {
                File.WriteAllText(CurrentFile, Editor.Text);
                IsDirty = false;
                string mode = IsPreviewMode ? "[预览]" : "[编辑]";
                Text = Path.GetFileName(CurrentFile) + " " + mode + " - Markdown Viewer";
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存文件失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void SaveFileAs()
        {
            if (!CanEdit) return;
            SaveFileDialog d = new SaveFileDialog();
            d.Filter = "Markdown 文件|*.md";
            if (d.ShowDialog() == DialogResult.OK)
            {
                CurrentFile = d.FileName;
                SaveFile();
                RecentFiles.Add(CurrentFile);
            }
        }

        public void SwitchToEdit()
        {
            if (!CanEdit) return;
            string findText = "";
            bool wasFindVisible = false;
            if (_findDlg != null && _findDlg.Visible)
            {
                findText = _findDlg.FindText;
                wasFindVisible = true;
                _findDlg.Hide();
            }
            IsPreviewMode = false;
            Editor.Visible = true;
            Preview.Visible = false;
            ViewToggleBtn.Text = "编辑";
            ViewToggleBtn.Image = editIcon;
            string name = string.IsNullOrEmpty(CurrentFile) ? "无标题" : System.IO.Path.GetFileName(CurrentFile);
            Text = name + " [编辑] - Markdown Viewer";
            if (wasFindVisible && !string.IsNullOrEmpty(findText))
            {
                ShowFindReplace();
                if (_findDlg != null)
                {
                    _findDlg.SetFindText(findText);
                    _findDlg.FindNext();
                }
            }
        }

        public void SwitchToPreview()
        {
            if (currentFileType == FileType.Text) return;
            string findText = "";
            bool wasFindVisible = false;
            if (_findDlg != null && _findDlg.Visible)
            {
                findText = _findDlg.FindText;
                wasFindVisible = true;
                _findDlg.Hide();
            }
            IsPreviewMode = true;
            Editor.Visible = false;
            Preview.Visible = true;
            ViewToggleBtn.Text = "预览";
            ViewToggleBtn.Image = eyeIcon;
            string name = string.IsNullOrEmpty(CurrentFile) ? "无标题" : System.IO.Path.GetFileName(CurrentFile);
            Text = name + " [预览] - Markdown Viewer";
            RefreshPreviewForCurrentFile();
            if (wasFindVisible && !string.IsNullOrEmpty(findText))
            {
                if (Preview.CoreWebView2 != null)
                {
                    Preview.CoreWebView2.ExecuteScriptAsync(
                        "window.find(" + EscapeJs(findText) + "); true"
                    );
                }
            }
        }

        void RefreshPreviewForCurrentFile()
        {
            if (currentFileType == FileType.Markdown)
            {
                RefreshPreview();
            }
            else if (currentFileType == FileType.Image)
            {
                PreviewImage(CurrentFile);
            }
            else if (currentFileType == FileType.Pdf)
            {
                PreviewPdf(CurrentFile);
            }
        }

        string EscapeJs(string text)
        {
            if (text == null) return "''";
            string s = text.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
            return "'" + s + "'";
        }

        void ToggleView()
        {
            if (IsPreviewMode) SwitchToEdit();
            else SwitchToPreview();
        }

        void RefreshPreview()
        {
            Log("PREVIEW", "RefreshPreview called, IsPreviewMode=" + IsPreviewMode + ", CoreWebView2=" + (Preview.CoreWebView2 != null));
            if (IsPreviewMode && Preview.CoreWebView2 != null)
            {
                string text = Editor.Text;
                Log("PREVIEW", "TextLength=" + text.Length);
                string key = text + (IsDarkMode ? "|dark" : "|light");
                if (cachedHtml == null || cachedText != key)
                {
                    cachedText = key;
                    var result = MarkdownParser.Parse(text, IsDarkMode);
                    Log("PREVIEW", "MarkdownParser output length=" + result.Html.Length + ", HasMermaid=" + result.HasMermaid);
                    cachedHtml = BuildPreviewHtml(result.Html, result.HasMermaid);
                    Log("PREVIEW", "Final HTML length=" + cachedHtml.Length);
                }
                try
                {
                    Preview.CoreWebView2.NavigateToString(cachedHtml);
                    Log("PREVIEW", "NavigateToString HTML length=" + cachedHtml.Length);
                }
                catch (Exception ex)
                {
                    Log("PREVIEW", "NavigateToString failed: " + ex.Message);
                }
            }
            else
            {
                Log("PREVIEW", "RefreshPreview skipped: IsPreviewMode=" + IsPreviewMode + ", CoreWebView2=" + (Preview.CoreWebView2 != null));
            }
        }

        string BuildPreviewHtml(string fullHtml, bool hasMermaid)
        {
            string bodyContent = ExtractBody(fullHtml);
            string css = IsDarkMode ? CSS_DARK : CSS_LIGHT;
            string highlightScript = "<script src=\"https://appassets.local/highlight.min.js\"></script>";
            string hljsInit = @"
    try {
        if (typeof hljs !== 'undefined') {
            document.querySelectorAll('pre code').forEach(function(block) {
                hljs.highlightElement(block);
            });
        }
    } catch(ex) {}";
            if (hasMermaid)
            {
                string mermaidScript = "<script src=\"https://appassets.local/mermaid.min.js\"></script>";
                string theme = IsDarkMode ? "dark" : "default";
                string initScript = @"
<script>
window.onerror = function(msg, url, line) { try { window.chrome.webview.postMessage('JS_ERROR:' + msg); } catch(e) {} return true; };
document.addEventListener('DOMContentLoaded', function() {
" + hljsInit + @"
    var nodes = document.querySelectorAll('.mermaid[data-b64]');
    for (var i = 0; i < nodes.length; i++) {
        try {
            var b64 = nodes[i].getAttribute('data-b64');
            var text = decodeURIComponent(escape(atob(b64)));
            nodes[i].textContent = text;
        } catch(e) {
            window.chrome.webview.postMessage('MERMAID_DECODE_ERR:' + e.message);
        }
    }
    if (typeof mermaid !== 'undefined') {
        mermaid.initialize({ startOnLoad: false, theme: '" + theme + @"', securityLevel: 'loose' });
        if (typeof mermaid.run === 'function') {
            mermaid.run({ nodes: document.querySelectorAll('.mermaid') }).then(function() {
                window.chrome.webview.postMessage('MERMAID_OK');
            }).catch(function(e) {
                window.chrome.webview.postMessage('MERMAID_ERR:' + e.message);
            });
        } else if (typeof mermaid.init === 'function') {
            mermaid.init({ nodes: document.querySelectorAll('.mermaid') });
            window.chrome.webview.postMessage('MERMAID_OK');
        } else {
            window.chrome.webview.postMessage('MERMAID_ERR:no render function');
        }
    } else {
        window.chrome.webview.postMessage('MERMAID_SKIP');
    }
    window.chrome.webview.postMessage('RENDERED');
});
document.addEventListener('keydown', function(e) {
    if (e.ctrlKey) {
        var key = e.key.toUpperCase();
        if (key === 'N' || key === 'O' || key === 'P' || key === 'E' || key === 'S') {
            e.preventDefault();
            window.chrome.webview.postMessage('KEY_' + key);
        }
    }
});
</script>";
                return @"<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'>
" + css + @"
" + highlightScript + @"
" + mermaidScript + @"
" + initScript + @"
</head>
<body>
" + bodyContent + @"
</body>
</html>";
            }
            else
            {
                string initScript = @"
<script>
window.onerror = function(msg, url, line) { try { window.chrome.webview.postMessage('JS_ERROR:' + msg); } catch(e) {} return true; };
document.addEventListener('DOMContentLoaded', function() {
" + hljsInit + @"
    window.chrome.webview.postMessage('RENDERED');
});
document.addEventListener('keydown', function(e) {
    if (e.ctrlKey) {
        var key = e.key.toUpperCase();
        if (key === 'N' || key === 'O' || key === 'P' || key === 'E' || key === 'S') {
            e.preventDefault();
            window.chrome.webview.postMessage('KEY_' + key);
        }
    }
});
</script>";
                return @"<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'>
" + css + @"
" + highlightScript + @"
" + initScript + @"
</head>
<body>
" + bodyContent + @"
</body>
</html>";
            }
        }

        string ExtractBody(string html)
        {
            int bodyStart = html.IndexOf("<body>");
            int bodyEnd = html.IndexOf("</body>");
            if (bodyStart >= 0 && bodyEnd > bodyStart)
            {
                return html.Substring(bodyStart + 6, bodyEnd - bodyStart - 6);
            }
            return html;
        }

        static readonly string CSS_LIGHT = @"<style>
body{font-family:'Segoe UI','Microsoft YaHei',sans-serif;padding:20px;font-size:15px;line-height:1.6;background:#fff;color:#333;overflow-x:hidden;word-wrap:break-word;max-width:100%}
h1{font-size:26px;color:#333;border-bottom:2px solid #407040;padding-bottom:8px}
h2{font-size:22px;color:#444;border-bottom:1px solid #ddd;padding-bottom:5px}
h3{font-size:18px;color:#555}
p{margin:10px 0}
code{background:#f0f0f0;padding:2px 6px;border-radius:3px;font-family:Consolas}
pre{background:#f6f8fa;padding:15px;border-radius:5px;border:1px solid #ddd;overflow-x:auto}
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
pre code.hljs{display:block;overflow-x:auto;padding:1em}code.hljs{padding:3px 5px}.hljs{color:#24292e;background:#f6f8fa}.hljs-doctag,.hljs-keyword,.hljs-meta .hljs-keyword,.hljs-template-tag,.hljs-template-variable,.hljs-type,.hljs-variable.language_{color:#d73a49}.hljs-title,.hljs-title.class_,.hljs-title.class_.inherited__,.hljs-title.function_{color:#6f42c1}.hljs-attr,.hljs-attribute,.hljs-literal,.hljs-meta,.hljs-number,.hljs-operator,.hljs-selector-attr,.hljs-selector-class,.hljs-selector-id,.hljs-variable{color:#005cc5}.hljs-meta .hljs-string,.hljs-regexp,.hljs-string{color:#032f62}.hljs-built_in,.hljs-symbol{color:#e36209}.hljs-code,.hljs-comment,.hljs-formula{color:#6a737d}.hljs-name,.hljs-quote,.hljs-selector-pseudo,.hljs-selector-tag{color:#22863a}.hljs-subst{color:#24292e}.hljs-section{color:#005cc5;font-weight:700}.hljs-bullet{color:#735c0f}.hljs-emphasis{color:#24292e;font-style:italic}.hljs-strong{color:#24292e;font-weight:700}.hljs-addition{color:#22863a;background-color:#f0fff4}.hljs-deletion{color:#b31d28;background-color:#ffeef0}
</style>";

        static readonly string CSS_DARK = @"<style>
body{font-family:'Segoe UI','Microsoft YaHei',sans-serif;padding:20px;font-size:15px;line-height:1.6;background:#1e1e1e;color:#d4d4d4;overflow-x:hidden;word-wrap:break-word;max-width:100%}
h1{font-size:26px;color:#fff;border-bottom:2px solid #4caf50;padding-bottom:8px}
h2{font-size:22px;color:#ccc;border-bottom:1px solid #444;padding-bottom:5px}
h3{font-size:18px;color:#aaa}
p{margin:10px 0}
code{background:#2d2d2d;padding:2px 6px;border-radius:3px;font-family:Consolas;color:#ce9178}
pre{background:#1e1e1e;padding:15px;border-radius:5px;border:1px solid #444;overflow-x:auto}
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
pre code.hljs{display:block;overflow-x:auto;padding:1em}code.hljs{padding:3px 5px}.hljs{color:#c9d1d9;background:#1e1e1e}.hljs-doctag,.hljs-keyword,.hljs-meta .hljs-keyword,.hljs-template-tag,.hljs-template-variable,.hljs-type,.hljs-variable.language_{color:#ff7b72}.hljs-title,.hljs-title.class_,.hljs-title.class_.inherited__,.hljs-title.function_{color:#d2a8ff}.hljs-attr,.hljs-attribute,.hljs-literal,.hljs-meta,.hljs-number,.hljs-operator,.hljs-selector-attr,.hljs-selector-class,.hljs-selector-id,.hljs-variable{color:#79c0ff}.hljs-meta .hljs-string,.hljs-regexp,.hljs-string{color:#a5d6ff}.hljs-built_in,.hljs-symbol{color:#ffa657}.hljs-code,.hljs-comment,.hljs-formula{color:#8b949e}.hljs-name,.hljs-quote,.hljs-selector-pseudo,.hljs-selector-tag{color:#7ee787}.hljs-subst{color:#c9d1d9}.hljs-section{color:#1f6feb;font-weight:700}.hljs-bullet{color:#f2cc60}.hljs-emphasis{color:#c9d1d9;font-style:italic}.hljs-strong{color:#c9d1d9;font-weight:700}.hljs-addition{color:#aff5b4;background-color:#033a16}.hljs-deletion{color:#ffdcd7;background-color:#67060c}
</style>";

        void Log(string category, string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string logLine = string.Format("[{0}] [{1}] {2}", timestamp, category, message);
            System.Diagnostics.Debug.WriteLine(logLine);
            if (!EnableDebugLog) return;
            try
            {
                string logPath = System.IO.Path.Combine(Application.StartupPath, "debug_" + System.Diagnostics.Process.GetCurrentProcess().Id + ".log");
                using (System.IO.StreamWriter sw = new System.IO.StreamWriter(logPath, true))
                {
                    sw.WriteLine(logLine);
                    sw.Flush();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Log write error: " + ex.Message);
            }
        }

        void Log(string message) { Log("APP", message); }

        void UpdateStatusBar()
        {
            string text = Editor.Text;
            int chars = text.Length;
            int words = string.IsNullOrEmpty(text.Trim()) ? 0 : Regex.Split(text.Trim(), @"\s+").Length;
            double sizeKb = chars / 1024.0;
            StatusLabel.Text = string.Format("字数: {0}  词数: {1}  大小: {2:F1} KB", chars, words, sizeKb);
        }

        void Zoom(int delta)
        {
            ZoomLevel = Math.Max(50f, Math.Min(200f, ZoomLevel + delta));
            ZoomValueLabel.Text = ZoomLevel.ToString() + "%";
            ApplyZoom();
        }

        void ApplyZoom()
        {
            try
            {
                if (Preview.CoreWebView2 != null)
                {
                    Preview.ZoomFactor = (double)(ZoomLevel / 100.0);
                }
            }
            catch (Exception ex)
            {
                Log("ZOOM", "ApplyZoom error: " + ex.Message);
            }
        }

        bool PromptSave()
        {
            if (!IsDirty) return true;
            DialogResult result = MessageBox.Show("是否保存更改?", "Markdown Viewer", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (result == DialogResult.Yes) { SaveFile(); return true; }
            return result != DialogResult.Cancel;
        }

        void ShowFindReplace()
        {
            ShowFindReplace(false);
        }

        void ShowFindReplace(bool showReplace)
        {
            if (_findDlg == null || _findDlg.IsDisposed)
            {
                _findDlg = new FindReplaceDialog(Editor, this);
                _findDlg.FormClosed += delegate(object s, FormClosedEventArgs e) { _findDlg = null; };
            }
            _findDlg.SetShowReplace(showReplace);
            _findDlg.Hide();
            _findDlg.Show(this);
            _findDlg.Focus();
            _findDlg.WindowState = FormWindowState.Normal;
            _findDlg.ShowInTaskbar = false;
        }

        void ShowHelp()
        {
            using (HelpForm help = new HelpForm())
            {
                help.ShowDialog(this);
            }
        }

        void ShowAbout()
        {
            MessageBox.Show("Markdown Viewer v1.7\n\n一个简洁高效的 Markdown 查看和编辑工具。", "关于", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        void RegisterAssoc(bool register)
        {
            try
            {
                string exePath = Application.ExecutablePath;
                string iconPath = exePath + ",0";
                if (register)
                {
                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\.md"))
                    {
                        if (key != null) key.SetValue("", "MarkdownViewer.Markdown");
                    }

                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\MarkdownViewer.Markdown"))
                    {
                        if (key != null)
                        {
                            key.SetValue("", "Markdown 文档");
                            key.SetValue("DefaultIcon", iconPath);
                        }
                    }

                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\MarkdownViewer.Markdown\shell\open\command"))
                    {
                        if (key != null) key.SetValue("", "\"" + exePath + "\" \"%1\"");
                    }

                    NativeMethods.SHChangeNotify(NativeMethods.SHCNE_ASSOCCHANGED, NativeMethods.SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
                    MessageBox.Show("注册成功！请重启 Explorer 或注销后生效。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    try { Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\.md"); }
                    catch (Exception ex) { Log("ASSOC", "Delete .md key error: " + ex.Message); }
                    try { Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\MarkdownViewer.Markdown"); }
                    catch (Exception ex) { Log("ASSOC", "Delete MarkdownViewer key error: " + ex.Message); }
                    NativeMethods.SHChangeNotify(NativeMethods.SHCNE_ASSOCCHANGED, NativeMethods.SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
                    MessageBox.Show("取消成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("操作失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
