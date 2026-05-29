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

        public FindReplaceDialog(RichTextBox editor, Form owner)
        {
            this.editor = editor;
            Text = "查找";
            Size = new Size(380, 130);
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
            l1.Location = new Point(10, 12);
            l1.Size = new Size(40, 20);
            l1.ForeColor = SystemColors.WindowText;
            Controls.Add(l1);

            findBox = new TextBox();
            findBox.Location = new Point(50, 10);
            findBox.Size = new Size(200, 20);
            findBox.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter) { FindNext(); e.SuppressKeyPress = true; }
                if (e.KeyCode == Keys.Escape) { Hide(); e.SuppressKeyPress = true; }
            };
            Controls.Add(findBox);

            Button prevBtn = new Button();
            prevBtn.Text = "上一个";
            prevBtn.Location = new Point(255, 8);
            prevBtn.Size = new Size(55, 24);
            prevBtn.FlatStyle = FlatStyle.Flat;
            prevBtn.Click += delegate(object s, EventArgs e) { FindPrev(); };
            Controls.Add(prevBtn);

            Button nextBtn = new Button();
            nextBtn.Text = "下一个";
            nextBtn.Location = new Point(315, 8);
            nextBtn.Size = new Size(55, 24);
            nextBtn.FlatStyle = FlatStyle.Flat;
            nextBtn.Click += delegate(object s, EventArgs e) { FindNext(); };
            Controls.Add(nextBtn);

            Label l2 = new Label();
            l2.Text = "替换:";
            l2.Location = new Point(10, 42);
            l2.Size = new Size(40, 20);
            l2.ForeColor = SystemColors.WindowText;
            Controls.Add(l2);

            replaceBox = new TextBox();
            replaceBox.Location = new Point(50, 40);
            replaceBox.Size = new Size(200, 20);
            replaceBox.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter) { ReplaceNext(); e.SuppressKeyPress = true; }
                if (e.KeyCode == Keys.Escape) { Hide(); e.SuppressKeyPress = true; }
            };
            Controls.Add(replaceBox);

            Button replaceBtn = new Button();
            replaceBtn.Text = "替换";
            replaceBtn.Location = new Point(255, 38);
            replaceBtn.Size = new Size(55, 24);
            replaceBtn.FlatStyle = FlatStyle.Flat;
            replaceBtn.Click += delegate(object s, EventArgs e) { ReplaceNext(); };
            Controls.Add(replaceBtn);

            Button replaceAllBtn = new Button();
            replaceAllBtn.Text = "全部";
            replaceAllBtn.Location = new Point(315, 38);
            replaceAllBtn.Size = new Size(55, 24);
            replaceAllBtn.FlatStyle = FlatStyle.Flat;
            replaceAllBtn.Click += delegate(object s, EventArgs e) { ReplaceAll(); };
            Controls.Add(replaceAllBtn);

            caseCheck = new CheckBox();
            caseCheck.Text = "区分大小写";
            caseCheck.Location = new Point(50, 70);
            caseCheck.Size = new Size(100, 20);
            caseCheck.ForeColor = SystemColors.WindowText;
            Controls.Add(caseCheck);

            findBox.Focus();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape) { Hide(); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        void FindNext()
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
