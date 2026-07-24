using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MarkdownViewer.Core
{
    public class CacheManager
    {
        public static readonly string CacheDir = Path.Combine(Application.StartupPath, "cache");
        private static readonly TimeSpan CacheExpiry = TimeSpan.FromDays(30);
        private static System.Windows.Forms.Timer _cleanupTimer;
        private static bool _isUserActive = false;
        private static System.Windows.Forms.Timer _userActivityTimer;

        static CacheManager()
        {
            if (!Directory.Exists(CacheDir))
                Directory.CreateDirectory(CacheDir);
        }

        public static string ComputeFileHash(string filePath)
        {
            try
            {
                string fileName = Path.GetFileName(filePath);
                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(fileName));
                    return BitConverter.ToString(hash).Replace("-", "").ToLower();
                }
            }
            catch
            {
                return "";
            }
        }

        public static FileDescription GetFileDescription(string filePath)
        {
            try
            {
                var info = new FileInfo(filePath);
                return new FileDescription
                {
                    FileLength = info.Length,
                    LastModified = info.LastWriteTime
                };
            }
            catch
            {
                return new FileDescription();
            }
        }

        public static string GetCacheDir(string fileHash)
        {
            if (string.IsNullOrEmpty(fileHash)) return "";
            string dir1 = fileHash.Substring(0, Math.Min(2, fileHash.Length));
            string dir2 = fileHash.Substring(2, Math.Min(8, fileHash.Length - 2));
            return Path.Combine(CacheDir, dir1, dir2);
        }

        public static CacheEntry ReadCache(string fileHash, FileDescription description)
        {
            string cacheDir = GetCacheDir(fileHash);
            string metadataPath = Path.Combine(cacheDir, "metadata.json");
            
            if (!File.Exists(metadataPath))
                return null;

            try
            {
                string json = File.ReadAllText(metadataPath);
                var metadata = ParseMetadata(json);
                
                if (metadata == null)
                    return null;

                if (metadata.fileLength != description.FileLength || metadata.lastModified != description.LastModified)
                    return null;

                string htmlPath = Path.Combine(cacheDir, "preview.html");

                if (!File.Exists(htmlPath))
                    return null;

                string html = File.ReadAllText(htmlPath);

                metadata.lastAccess = DateTime.Now;
                metadata.accessCount++;
                File.WriteAllText(metadataPath, SerializeMetadata(metadata));

                return new CacheEntry
                {
                    Html = html,
                    Metadata = metadata
                };
            }
            catch
            {
                return null;
            }
        }

        public static void WriteCache(string fileHash, FileDescription description, string content, string html)
        {
            try
            {
                string cacheDir = GetCacheDir(fileHash);
                if (!Directory.Exists(cacheDir))
                    Directory.CreateDirectory(cacheDir);

                File.WriteAllText(Path.Combine(cacheDir, "preview.html"), html);

                var metadata = new CacheMetadata
                {
                    fileLength = description.FileLength,
                    lastModified = description.LastModified,
                    lastAccess = DateTime.Now,
                    accessCount = 1
                };

                File.WriteAllText(Path.Combine(cacheDir, "metadata.json"), SerializeMetadata(metadata));
            }
            catch
            {
            }
        }

        static CacheMetadata ParseMetadata(string json)
        {
            try
            {
                var meta = new CacheMetadata();
                string[] lines = json.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    string trimmed = line.Trim().Trim(',');
                    if (trimmed.StartsWith("\"fileLength\""))
                    {
                        int start = trimmed.IndexOf(':') + 1;
                        string val = trimmed.Substring(start).Trim().Trim('"');
                        long length;
                        long.TryParse(val, out length);
                        meta.fileLength = length;
                    }
                    else if (trimmed.StartsWith("\"lastModified\""))
                    {
                        int start = trimmed.IndexOf(':') + 1;
                        string dateStr = trimmed.Substring(start).Trim().Trim('"');
                        DateTime dt;
                        if (DateTime.TryParse(dateStr, out dt))
                            meta.lastModified = dt;
                    }
                    else if (trimmed.StartsWith("\"lastAccess\""))
                    {
                        int start = trimmed.IndexOf(':') + 1;
                        string dateStr = trimmed.Substring(start).Trim().Trim('"');
                        DateTime dt;
                        if (DateTime.TryParse(dateStr, out dt))
                            meta.lastAccess = dt;
                    }
                    else if (trimmed.StartsWith("\"accessCount\""))
                    {
                        int start = trimmed.IndexOf(':') + 1;
                        string countStr = trimmed.Substring(start).Trim().Trim('"');
                        int count;
                        int.TryParse(countStr, out count);
                        meta.accessCount = count;
                    }
                }
                return meta;
            }
            catch
            {
                return null;
            }
        }

        static string SerializeMetadata(CacheMetadata meta)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"fileLength\":" + meta.fileLength + ",");
            sb.Append("\"lastModified\":\"" + meta.lastModified.ToString("o") + "\",");
            sb.Append("\"lastAccess\":\"" + meta.lastAccess.ToString("o") + "\",");
            sb.Append("\"accessCount\":" + meta.accessCount);
            sb.Append("}");
            return sb.ToString();
        }

        static string EscapeJson(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        public static void StartCleanupTimer()
        {
            _cleanupTimer = new System.Windows.Forms.Timer();
            _cleanupTimer.Interval = 5 * 60 * 1000;
            _cleanupTimer.Tick += delegate(object s, EventArgs e)
            {
                _cleanupTimer.Stop();
                if (!_isUserActive)
                {
                    CleanupOldCache();
                }
            };
            _cleanupTimer.Start();

            _userActivityTimer = new System.Windows.Forms.Timer();
            _userActivityTimer.Interval = 1000;
            _userActivityTimer.Tick += delegate(object s, EventArgs e)
            {
                _isUserActive = true;
                _userActivityTimer.Stop();
            };
            _userActivityTimer.Start();
        }

        public static void NotifyUserActivity()
        {
            _isUserActive = false;
            if (_userActivityTimer != null)
            {
                _userActivityTimer.Stop();
                _userActivityTimer.Start();
            }
        }

        private static void CleanupOldCache()
        {
            try
            {
                if (!Directory.Exists(CacheDir)) return;

                DateTime cutoff = DateTime.Now - CacheExpiry;
                string[] dirs = Directory.GetDirectories(CacheDir);

                foreach (string dir in dirs)
                {
                    try
                    {
                        string metadataPath = Path.Combine(dir, "metadata.json");
                        if (!File.Exists(metadataPath)) continue;

                        string json = File.ReadAllText(metadataPath);
                        var metadata = ParseMetadata(json);
                        
                        if (metadata != null && metadata.lastAccess < cutoff)
                        {
                            Directory.Delete(dir, true);
                        }
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }
    }

    public class CacheEntry
    {
        public string Html;
        public CacheMetadata Metadata;
    }

    public class FileDescription
    {
        public long FileLength;
        public DateTime LastModified;
    }

    public class CacheMetadata
    {
        public long fileLength;
        public DateTime lastModified;
        public DateTime lastAccess;
        public int accessCount;
    }
}