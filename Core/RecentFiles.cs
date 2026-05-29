using System;
using System.IO;
using Microsoft.Win32;

namespace MarkdownViewer.Core
{
    class RecentFiles
    {
        const int MAX_RECENT = 10;
        static string[] files = new string[0];

        public static string[] Files { get { return files; } }

        public static void Clear()
        {
            files = new string[0];
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\MarkdownViewer\RecentFiles"))
                {
                    if (key != null) key.DeleteValue("Count", false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("RecentFiles.Clear error: " + ex.Message);
            }
        }

        public static void Load()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\MarkdownViewer\RecentFiles"))
                {
                    if (key != null)
                    {
                        int count = (int)key.GetValue("Count", 0);
                        files = new string[count];
                        for (int i = 0; i < count; i++)
                        {
                            files[i] = key.GetValue("File" + i, "") as string;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("RecentFiles.Load error: " + ex.Message);
                files = new string[0];
            }
        }

        public static void Add(string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                path = Path.GetFullPath(path);
            }
            catch
            {
                return;
            }

            string[] newFiles = new string[Math.Min(files.Length + 1, MAX_RECENT)];
            newFiles[0] = path;
            int j = 1;
            for (int i = 0; i < files.Length && j < MAX_RECENT; i++)
            {
                if (string.IsNullOrEmpty(files[i])) continue;
                if (files[i] == path) continue;
                try
                {
                    if (File.Exists(files[i]))
                        newFiles[j++] = files[i];
                }
                catch
                {
                    // 跳过无法访问的文件
                }
            }
            files = newFiles;

            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\MarkdownViewer\RecentFiles"))
                {
                    if (key != null)
                    {
                        key.SetValue("Count", files.Length);
                        for (int i = 0; i < files.Length; i++)
                            key.SetValue("File" + i, files[i]);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("RecentFiles.Add save error: " + ex.Message);
            }
        }
    }
}
