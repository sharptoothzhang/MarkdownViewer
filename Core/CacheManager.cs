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

        public static string GetCompositeKey(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return "";
            try
            {
                var info = new FileInfo(filePath);
                string raw = filePath + "|" + info.Length + "|" + info.LastWriteTimeUtc.Ticks;
                return HashString(raw);
            }
            catch
            {
                return "";
            }
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

        // 缓存目录分层规则（两级目录 + 完整 key 作文件名）：
        //   key 为 base64 字符串，长度为动态值（当前实现中为 44 位）
        //   存储路径 = cache/{key[0..2]}/{key[2..4]}/{完整key}.html
        //   例：key = "aabbccddee..." → cache/aa/bb/aabbccddee....html
        // 目的：通过首两段各2位字符做两级目录划分，使单个目录下文件数可控；
        //       同时保留完整 key 作为文件名便于通过 key 直接定位文件。
        public static string GetCacheDir(string fileHash)
        {
            if (string.IsNullOrEmpty(fileHash)) return "";
            string p1 = fileHash.Length >= 2 ? fileHash.Substring(0, 2) : fileHash;
            string p2 = fileHash.Length >= 4 ? fileHash.Substring(2, 2) : "00";
            return Path.Combine(CacheDir, p1, p2);
        }

        // 由 key 计算出缓存在磁盘上的完整文件路径（含文件名）
        // 格式：<CacheDir>/{key[0..2]}/{key[2..4]}/{完整key}.html
        internal static string GetCacheFilePath(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";
            return Path.Combine(GetCacheDir(key), key + ".html");
        }

        public static CacheEntry ReadCache(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            string htmlPath = GetCacheFilePath(key);
            if (!File.Exists(htmlPath)) return null;
            try
            {
                string html = File.ReadAllText(htmlPath);
                return new CacheEntry { Html = html, Metadata = new CacheMetadata() };
            }
            catch
            {
                return null;
            }
        }

        // ===== 以下为旧版接口，已弃用，请使用上方基于 key 的重载 =====
        // 保留仅作向后兼容占位，不建议继续调用

        public static CacheEntry ReadCache(string fileHash, FileDescription description)
        {
            // 委托给新方法，忽略过期的 description 参数
            return ReadCache(fileHash);
        }

        public static void WriteCache(string fileHash, FileDescription description, string content, string html)
        {
            // 委托给新方法，忽略过期的 description 参数
            WriteCache(fileHash, html);
        }

        // ========== 新版接口：基于完整 key 的文件路径 ==========

        public static void WriteCache(string key, string html)
        {
            if (string.IsNullOrEmpty(key) || html == null) return;
            try
            {
                string fullPath = GetCacheFilePath(key);
                string dir = System.IO.Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(fullPath, html);
            }
            catch { }
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

        private static string HashString(string input)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(input);
                byte[] hash = sha.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
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
                string[] partitions = Directory.GetDirectories(CacheDir);

                foreach (string partition in partitions)
                {
                    try
                    {
                        if (!Directory.Exists(partition)) continue;

                        string[] shards = Directory.GetDirectories(partition);
                        foreach (string shard in shards)
                        {
                            try
                            {
                                if (!Directory.Exists(shard)) continue;

                                // 直接按文件自身修改时间判定过期，不再依赖已弃用的 metadata.json
                                if (new DirectoryInfo(shard).LastWriteTime < cutoff)
                                    Directory.Delete(shard, true);
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
            }
            catch { }
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