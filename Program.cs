using System;
using System.Threading;
using System.Windows.Forms;
using MarkdownViewer.Core;
using MarkdownViewer.Forms;
using MarkdownViewer.Hooks;

namespace MarkdownViewer
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.ThreadException += delegate(object s, ThreadExceptionEventArgs e)
            {
                LogException("ThreadException", e.Exception);
            };

            AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e)
            {
                LogException("UnhandledException", e.ExceptionObject as Exception);
            };

            RecentFiles.Load();
            MainForm form = new MainForm();

            bool enableDebug = false;
            string fileToOpen = null;

            foreach (string arg in args)
            {
                if (arg == "--debug" || arg == "-d")
                {
                    enableDebug = true;
                }
                else if (arg.StartsWith("-"))
                {
                    // unknown option, ignore
                }
                else if (System.IO.File.Exists(arg))
                {
                    fileToOpen = arg;
                }
            }

            form.EnableDebugLog = enableDebug;
            DropHook.Install(form);

            try
            {
                if (fileToOpen != null) form.OpenFile(fileToOpen);
                Application.Run(form);
            }
            finally
            {
                DropHook.Uninstall();
            }
        }

        static void LogException(string type, Exception ex)
        {
            if (ex == null) return;
            try
            {
                string logPath = System.IO.Path.Combine(Application.StartupPath, "crash_" + System.Diagnostics.Process.GetCurrentProcess().Id + ".log");
                using (System.IO.StreamWriter sw = new System.IO.StreamWriter(logPath, true))
                {
                    sw.WriteLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + type + ": " + ex.Message);
                    sw.WriteLine(ex.StackTrace);
                    sw.WriteLine();
                }
            }
            catch (Exception logEx)
            {
                System.Diagnostics.Debug.WriteLine("Crash log write error: " + logEx.Message);
            }
            try
            {
                MessageBox.Show(type + ": " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception msgEx)
            {
                System.Diagnostics.Debug.WriteLine("MessageBox error: " + msgEx.Message);
            }
        }
    }
}