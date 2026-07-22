using System;
using System.Drawing;
using System.Windows.Forms;

namespace MarkdownViewer.Forms
{
    class FindReplaceDialog : Form
    {
        TextBox findBox;
        TextBox replaceBox;
        CheckBox caseCheck;
        RichTextBox editor;
        Panel replacePanel;
        LinkLabel linkReplace;
        bool showReplace = false;

        public string FindText { get { return findBox.Text; } }
        public string ReplaceText { get { return replaceBox.Text; } }
        public bool CaseSensitive { get { return caseCheck.Checked; } }
        public bool ShowReplace { get { return showReplace; } }

        public void SetFindText(string text)
        {
            findBox.Text = text;
        }

        public void SetReplaceText(string text)
        {
            replaceBox.Text = text;
        }

        public void SetCaseSensitive(bool value)
        {
            caseCheck.Checked = value;
        }

        public FindReplaceDialog(RichTextBox editor, Form owner)
        {
            this.editor = editor;
            Text = "查找";
            Size = new Size(380, 100);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Location = new Point(owner.Right - Width - 20, owner.Top + 60);
            FormClosing += delegate(object s, FormClosingEventArgs e)
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    findBox.Text = "";
                    replaceBox.Text = "";
                    Hide();
                }
            };

            Label l1 = new Label();
            l1.Text = "查找:";
            l1.Location = new Point(10, 10);
            l1.Size = new Size(40, 20);
            l1.ForeColor = SystemColors.WindowText;
            Controls.Add(l1);

            findBox = new TextBox();
            findBox.Location = new Point(50, 8);
            findBox.Size = new Size(150, 20);
            findBox.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter) { FindNext(); e.SuppressKeyPress = true; }
                if (e.KeyCode == Keys.Escape) { Hide(); e.SuppressKeyPress = true; }
            };
            Controls.Add(findBox);

            Button prevBtn = new Button();
            prevBtn.Text = "上一个";
            prevBtn.Location = new Point(215, 6);
            prevBtn.Size = new Size(55, 24);
            prevBtn.FlatStyle = FlatStyle.Flat;
            prevBtn.Click += delegate(object s, EventArgs e) { FindPrev(); };
            Controls.Add(prevBtn);

            Button nextBtn = new Button();
            nextBtn.Text = "下一个";
            nextBtn.Location = new Point(275, 6);
            nextBtn.Size = new Size(55, 24);
            nextBtn.FlatStyle = FlatStyle.Flat;
            nextBtn.Click += delegate(object s, EventArgs e) { FindNext(); };
            Controls.Add(nextBtn);

            caseCheck = new CheckBox();
            caseCheck.Text = "区分大小写";
            caseCheck.Location = new Point(50, 35);
            caseCheck.Size = new Size(100, 20);
            caseCheck.ForeColor = SystemColors.WindowText;
            Controls.Add(caseCheck);

            linkReplace = new LinkLabel();
            linkReplace.Text = "替换";
            linkReplace.Location = new Point(50, 35);
            linkReplace.Size = new Size(50, 20);
            linkReplace.LinkClicked += delegate(object s, LinkLabelLinkClickedEventArgs e)
            {
                showReplace = !showReplace;
                UpdateReplaceVisibility();
            };
            Controls.Add(linkReplace);

            replacePanel = new Panel();
            replacePanel.Location = new Point(0, 60);
            replacePanel.Size = new Size(380, 50);
            Controls.Add(replacePanel);

            Label l2 = new Label();
            l2.Text = "替换:";
            l2.Location = new Point(10, 8);
            l2.Size = new Size(40, 20);
            l2.ForeColor = SystemColors.WindowText;
            replacePanel.Controls.Add(l2);

            replaceBox = new TextBox();
            replaceBox.Location = new Point(50, 6);
            replaceBox.Size = new Size(150, 20);
            replaceBox.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter) { ReplaceNext(); e.SuppressKeyPress = true; }
                if (e.KeyCode == Keys.Escape) { Hide(); e.SuppressKeyPress = true; }
            };
            replacePanel.Controls.Add(replaceBox);

            Button replaceBtn = new Button();
            replaceBtn.Text = "替换";
            replaceBtn.Location = new Point(210, 4);
            replaceBtn.Size = new Size(55, 24);
            replaceBtn.FlatStyle = FlatStyle.Flat;
            replaceBtn.Click += delegate(object s, EventArgs e) { ReplaceNext(); };
            replacePanel.Controls.Add(replaceBtn);

            Button replaceAllBtn = new Button();
            replaceAllBtn.Text = "全部";
            replaceAllBtn.Location = new Point(270, 4);
            replaceAllBtn.Size = new Size(55, 24);
            replaceAllBtn.FlatStyle = FlatStyle.Flat;
            replaceAllBtn.Click += delegate(object s, EventArgs e) { ReplaceAll(); };
            replacePanel.Controls.Add(replaceAllBtn);

            UpdateReplaceVisibility();

            findBox.Focus();
        }

        public void SetShowReplace(bool show)
        {
            showReplace = show;
            UpdateReplaceVisibility();
        }

        void UpdateReplaceVisibility()
        {
            if (replacePanel == null) return;
            replacePanel.Visible = showReplace;
            if (showReplace)
            {
                Size = new Size(380, 140);
            }
            else
            {
                Size = new Size(380, 100);
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape) { Hide(); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        public void FindNext()
        {
            if (string.IsNullOrEmpty(findBox.Text)) return;
            RichTextBoxFinds options = RichTextBoxFinds.None;
            if (caseCheck.Checked) options |= RichTextBoxFinds.MatchCase;

            int startPos = editor.SelectionStart + editor.SelectionLength;
            int pos = editor.Find(findBox.Text, startPos, options);
            if (pos == -1)
            {
                pos = editor.Find(findBox.Text, 0, options);
                if (pos == -1)
                {
                    System.Media.SystemSounds.Beep.Play();
                    return;
                }
            }
            editor.Select(pos, findBox.Text.Length);
            editor.ScrollToCaret();
            editor.Focus();
        }

        void FindPrev()
        {
            if (string.IsNullOrEmpty(findBox.Text)) return;
            RichTextBoxFinds options = RichTextBoxFinds.Reverse;
            if (caseCheck.Checked) options |= RichTextBoxFinds.MatchCase;

            int startPos = editor.SelectionStart - 1;
            if (startPos < 0) startPos = editor.Text.Length;
            int pos = editor.Find(findBox.Text, 0, startPos, options);
            if (pos == -1)
            {
                pos = editor.Find(findBox.Text, editor.Text.Length, RichTextBoxFinds.Reverse);
                if (pos == -1)
                {
                    System.Media.SystemSounds.Beep.Play();
                    return;
                }
            }
            editor.Select(pos, findBox.Text.Length);
            editor.ScrollToCaret();
            editor.Focus();
        }

        void ReplaceNext()
        {
            if (string.IsNullOrEmpty(findBox.Text)) return;
            if (editor.SelectionLength > 0 && string.Equals(editor.SelectedText, findBox.Text, caseCheck.Checked ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))
            {
                editor.SelectedText = replaceBox.Text;
            }
            FindNext();
        }

        void ReplaceAll()
        {
            if (string.IsNullOrEmpty(findBox.Text)) return;
            RichTextBoxFinds options = RichTextBoxFinds.None;
            if (caseCheck.Checked) options |= RichTextBoxFinds.MatchCase;

            int count = 0;
            int pos = 0;
            string text = editor.Text;
            string search = findBox.Text;
            string replace = replaceBox.Text;

            if (caseCheck.Checked)
            {
                while ((pos = text.IndexOf(search, pos, StringComparison.Ordinal)) != -1)
                {
                    text = text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
                    count++;
                    pos += replace.Length;
                }
            }
            else
            {
                while ((pos = text.IndexOf(search, pos, StringComparison.OrdinalIgnoreCase)) != -1)
                {
                    text = text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
                    count++;
                    pos += replace.Length;
                }
            }

            if (count > 0)
            {
                editor.Text = text;
                MessageBox.Show("已替换 " + count + " 处", "替换", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                System.Media.SystemSounds.Beep.Play();
            }
        }
    }
}
