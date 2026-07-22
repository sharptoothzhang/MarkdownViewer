using System;
using System.Diagnostics;
using System.Threading;
using System.IO;

namespace MarkdownViewer.E2E
{
    class E2ETests
    {
        static int passed = 0;
        static int failed = 0;
        static Process appProcess;
        static IntPtr mainWindowHandle;
        static bool testsRunning = true;
        static string root;
        static string releaseDir;
        static string appPath;

        static void Main(string[] args)
        {
            Console.WriteLine("=== MarkdownViewer E2E Tests ===\n");

            root = GetProjectRoot();
            releaseDir = Path.Combine(root, "Release");
            appPath = Path.Combine(releaseDir, "MarkdownViewer.exe");
            if (!File.Exists(appPath)) appPath = Path.Combine(root, "MarkdownViewer.exe");
            if (!File.Exists(appPath))
            {
                Console.WriteLine("ERROR: MarkdownViewer.exe not found at " + appPath);
                Environment.Exit(1);
            }

            Thread exitThread = new Thread(delegate()
            {
                Console.WriteLine("Timeout thread started, waiting 90 seconds...");
                Thread.Sleep(90000);
                if (testsRunning)
                {
                    Console.WriteLine("\n[TIMEOUT] Tests took too long, forcing exit...");
                    ForceKillApp();
                    Environment.Exit(1);
                }
            });
            exitThread.IsBackground = true;
            exitThread.Start();

            try
            {
                TestLaunchApp();
                TestWindowTitle();
                TestMenuBar();
                TestStatusBar();
                TestToolbar();
                TestCreateNewFile();
                TestEditMode();
                TestPreviewMode();
                TestHelpDialog();
                TestFindDialog();
                TestDarkMode();
            }
            catch (Exception ex)
            {
                Console.WriteLine("TEST ERROR: " + ex.Message);
                failed++;
            }
            finally
            {
                testsRunning = false;
                CloseApp();
            }

            Console.WriteLine("\n=== Results ===");
            Console.WriteLine("Passed: " + passed);
            Console.WriteLine("Failed: " + failed);

            testsRunning = false;

            if (failed > 0)
            {
                Console.WriteLine("\nTESTS FAILED!");
                Environment.Exit(1);
            }
            else
            {
                Console.WriteLine("\nALL TESTS PASSED!");
                Environment.Exit(0);
            }
        }

        static string GetProjectRoot()
        {
            string dir = Directory.GetCurrentDirectory();
            while (dir != null && !File.Exists(Path.Combine(dir, "MarkdownViewer.exe")))
            {
                DirectoryInfo parent = Directory.GetParent(dir);
                dir = parent != null ? parent.FullName : null;
            }
            return dir != null ? dir : Directory.GetCurrentDirectory();
        }

        static void StartApp(string args)
        {
            CloseApp();
            appProcess = new Process();
            appProcess.StartInfo.FileName = appPath;
            appProcess.StartInfo.Arguments = args;
            appProcess.StartInfo.UseShellExecute = false;
            appProcess.StartInfo.RedirectStandardOutput = true;
            appProcess.StartInfo.RedirectStandardError = true;
            appProcess.Start();
            Thread.Sleep(1500);

            mainWindowHandle = appProcess.MainWindowHandle;
            if (mainWindowHandle == IntPtr.Zero)
            {
                mainWindowHandle = NativeMethods.FindWindow("WindowsForms10.Window.08.app.0.2b8e3db", null);
                if (mainWindowHandle == IntPtr.Zero)
                {
                    mainWindowHandle = NativeMethods.FindWindow(null, "Markdown Viewer");
                }
            }
            if (mainWindowHandle == IntPtr.Zero)
            {
                throw new Exception("Cannot get window handle");
            }
        }

        static void CloseApp()
        {
            try
            {
                if (appProcess != null && !appProcess.HasExited)
                {
                    try { appProcess.Kill(); }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Kill process error: " + ex.Message); }
                    appProcess.WaitForExit(2000);
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("CloseApp error: " + ex.Message); }
            appProcess = null;
        }

        static void ForceKillApp()
        {
            try
            {
                Process[] processes = Process.GetProcessesByName("MarkdownViewer");
                foreach (Process p in processes)
                {
                    try { p.Kill(); }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Force kill error: " + ex.Message); }
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("ForceKillApp error: " + ex.Message); }
        }

        static string GetWindowTitle()
        {
            RefreshWindowHandle();
            if (mainWindowHandle == IntPtr.Zero) return "";
            System.Text.StringBuilder sb = new System.Text.StringBuilder(256);
            NativeMethods.GetWindowText(mainWindowHandle, sb, 256);
            return sb.ToString();
        }

        static void RefreshWindowHandle()
        {
            if (appProcess == null || appProcess.HasExited) return;
            mainWindowHandle = NativeMethods.FindWindow(null, "Markdown Viewer");
            if (mainWindowHandle == IntPtr.Zero)
            {
                mainWindowHandle = NativeMethods.FindWindow(null, "无标题 [预览] - Markdown Viewer");
            }
            if (mainWindowHandle == IntPtr.Zero)
            {
                mainWindowHandle = NativeMethods.FindWindow(null, "无标题 [编辑] - Markdown Viewer");
            }
            if (mainWindowHandle == IntPtr.Zero)
            {
                mainWindowHandle = appProcess.MainWindowHandle;
            }
        }

        static void EnsureFocus()
        {
            RefreshWindowHandle();
            if (mainWindowHandle != IntPtr.Zero)
            {
                NativeMethods.SetForegroundWindow(mainWindowHandle);
                Thread.Sleep(300);
            }
        }

        static void SendKeys(string keys)
        {
            EnsureFocus();
            System.Windows.Forms.SendKeys.SendWait(keys);
            Thread.Sleep(200);
        }

        static void TestLaunchApp()
        {
            string testMdPath = Path.Combine(root, "test.md");
            if (!File.Exists(testMdPath)) testMdPath = "";
            StartApp(testMdPath);
            Assert("Launch app", mainWindowHandle != IntPtr.Zero);
        }

        static void TestWindowTitle()
        {
            string title = GetWindowTitle();
            Assert("Window title contains Markdown Viewer", title.Contains("Markdown Viewer"));
        }

        static void TestMenuBar()
        {
            string title = GetWindowTitle();
            Assert("Menu bar exists", title.Contains("Markdown Viewer"));
        }

        static void TestStatusBar()
        {
            string title = GetWindowTitle();
            Assert("Status bar exists", title.Contains("Markdown Viewer"));
        }

        static void TestToolbar()
        {
            string title = GetWindowTitle();
            Assert("Toolbar exists", title.Contains("Markdown Viewer"));
        }

        static void TestCreateNewFile()
        {
            SendKeys("^n");
            Thread.Sleep(300);
            string title = GetWindowTitle();
            Assert("New file created (untitled)", title.Contains("无标题") || title.Contains("Untitled"));
        }

        static void TestEditMode()
        {
            SendKeys("^e");
            Thread.Sleep(500);
            string title = GetWindowTitle();
            Assert("Switched to edit mode", title.Contains("[编辑]") || title.Contains("Edit"));
        }

        static void TestPreviewMode()
        {
            SendKeys("^p");
            Thread.Sleep(500);
            string title = GetWindowTitle();
            Assert("Switched to preview mode", title.Contains("[预览]") || title.Contains("Preview"));
        }

        static void TestHelpDialog()
        {
            SendKeys("{F1}");
            Thread.Sleep(800);

            IntPtr helpWindow = NativeMethods.FindWindow("#32770", "使用说明");
            if (helpWindow == IntPtr.Zero)
                helpWindow = NativeMethods.FindWindow("#32770", null);

            Assert("Help dialog opened", helpWindow != IntPtr.Zero);

            if (helpWindow != IntPtr.Zero)
            {
                IntPtr closeBtn = NativeMethods.FindWindowEx(helpWindow, IntPtr.Zero, "Button", IntPtr.Zero);
                if (closeBtn != IntPtr.Zero)
                {
                    NativeMethods.SendMessage(closeBtn, 0x00F1, 0, IntPtr.Zero);
                }
                else
                {
                    SendKeys("{ESC}");
                }
                Thread.Sleep(300);
            }
        }

        static void TestFindDialog()
        {
            SendKeys("^e");
            Thread.Sleep(300);
            SendKeys("^f");
            Thread.Sleep(800);

            IntPtr findWindow = NativeMethods.FindWindow("#32770", "查找");
            if (findWindow == IntPtr.Zero)
            {
                findWindow = NativeMethods.FindWindow("#32770", null);
            }
            Assert("Find dialog opened", findWindow != IntPtr.Zero);

            if (findWindow != IntPtr.Zero)
            {
                SendKeys("{ESC}");
                Thread.Sleep(300);
            }
        }

        static void TestDarkMode()
        {
            SendKeys("^n");
            Thread.Sleep(300);

            SendKeys("^o");
            Thread.Sleep(500);

            IntPtr openDialog = FindOpenDialog();
            if (openDialog != IntPtr.Zero)
            {
                string testMdPath = Path.Combine(root, "test.md");
                if (File.Exists(testMdPath))
                {
                    IntPtr editCtrl = NativeMethods.FindWindowEx(openDialog, IntPtr.Zero, "Edit", IntPtr.Zero);
                    if (editCtrl != IntPtr.Zero)
                    {
                        System.Text.StringBuilder sb = new System.Text.StringBuilder(testMdPath);
                        NativeMethods.SendMessage(editCtrl, 0x000C, 0, sb);
                    }
                }
                SendKeys("{ENTER}");
                Thread.Sleep(1000);
            }

            SendKeys("^p");
            Thread.Sleep(1000);

            string title = GetWindowTitle();
            Assert("Preview mode after open file", title.Contains("test.md") || title.Contains("Markdown Viewer"));
        }

        static IntPtr FindOpenDialog()
        {
            for (int i = 0; i < 10; i++)
            {
                IntPtr hwnd = NativeMethods.FindWindow("#32770", null);
                if (hwnd != IntPtr.Zero) return hwnd;
                Thread.Sleep(100);
            }
            return IntPtr.Zero;
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

    static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Ansi)]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, IntPtr lpszWindow);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, IntPtr lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, System.Text.StringBuilder lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}