using Employee.UnsafeNativeMethod;
using System;
using System.IO;
using System.Text;

namespace Employee
{
    public class Profile
    {
        public const string @true = "true";
        public const string @false = "false";
        private string m_filePath = string.Empty;
        private string m_appName = string.Empty;

        public Profile(string filePath, string app)
        {
            m_filePath = filePath;
            m_appName = app;
        }

        public string this[string key] {
            get {
                if (!string.IsNullOrEmpty(m_filePath) &&
                    File.Exists(Path.GetFullPath(m_filePath)) &&
                    !string.IsNullOrEmpty(m_appName)) {
                    return GetString(m_appName, key, m_filePath);
                }
                return null;
            }

            set {
                if (!string.IsNullOrEmpty(m_filePath) &&
                    !string.IsNullOrEmpty(m_appName)) {
                    SetString(m_appName, key, value ?? string.Empty, m_filePath);
                }
            }
        }

        public static string DefaultAppName => Path.GetFileNameWithoutExtension(System.Windows.Forms.Application.ExecutablePath);

        public static string DefaultFileName => string.Format("{0}.ini", DefaultAppName);

        public static void CreateUnicodeFormatFile(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath) && !File.Exists(filePath)) {
                StreamWriter streamWriter = new StreamWriter(filePath, false, Encoding.Unicode);
                streamWriter.Close();
            }
        }

        public static string GetString(string key)
        {
            return GetString(DefaultAppName, key);
        }

        public static string GetString(string app, string key)
        {
            return GetString(app, key, DefaultFileName);
        }

        public static string GetString(string app, string key, string fileName)
        {
            string text = null;
            if (!string.IsNullOrEmpty(fileName) &&
                File.Exists(Path.GetFullPath(fileName))) {
                StringBuilder sb = new StringBuilder(260);
                int length = unchecked((int)kernel32.GetPrivateProfileString(app, key,
                    string.Empty, sb, Convert.ToUInt32(sb.Capacity), Path.GetFullPath(fileName)));
                if (length > 0) {
                    text = sb.ToString().Substring(0, length);
                }
            }
            return text;
        }

        public static void SetString(string key, string s)
        {
            SetString(DefaultAppName, key, s, DefaultFileName);
        }

        public static void SetString(string app, string key, string s)
        {
            SetString(app, key, s, DefaultFileName);
        }

        public static void SetString(string app, string key, string s, string fileName)
        {
            if (!string.IsNullOrEmpty(fileName)) {
                CreateUnicodeFormatFile(Path.GetFullPath(fileName));
                kernel32.WritePrivateProfileString(app, key, s, Path.GetFullPath(fileName));
            }
        }

        public static int? GetInteger(string key)
        {
            return GetInteger(DefaultAppName, key);
        }

        public static int? GetInteger(string app, string key)
        {
            return GetInteger(app, key, DefaultFileName);
        }

        public static int? GetInteger(string app, string key, string fileName)
        {
            int? i = null;
            string value = GetString(app, key, fileName);
            if (!string.IsNullOrEmpty(value)) {
                if (value.Length >= 2 && value.Substring(0, 2).Equals(
                    "0x", StringComparison.CurrentCultureIgnoreCase)) {
                    try {
                        i = Convert.ToInt32(value, 16);
                    } catch {

                    }
                } else {
                    if (int.TryParse(value, out int _i)) {
                        i = _i;
                    }
                }
            }
            return i;
        }

        public static void SetInteger(string key, int n)
        {
            SetInteger(DefaultAppName, key, n);
        }

        public static void SetInteger(string app, string key, int n)
        {
            SetInteger(app, key, n, DefaultFileName);
        }

        public static void SetInteger(string app, string key, int n, string fileName)
        {
            if (!string.IsNullOrEmpty(fileName)) {
                CreateUnicodeFormatFile(Path.GetFullPath(fileName));
                kernel32.WritePrivateProfileString(
                    app, key, n.ToString(), Path.GetFullPath(fileName));
            }
        }

        public static bool? GetBoolean(string key)
        {
            return GetBoolean(DefaultAppName, key);
        }

        public static bool? GetBoolean(string app, string key)
        {
            return GetBoolean(app, key, DefaultFileName);
        }

        public static bool? GetBoolean(string app, string key, string fileName)
        {
            bool b = false;
            string value = GetString(app, key, fileName);
            if (!string.IsNullOrEmpty(value)) {
                if (value.Equals(@true, StringComparison.CurrentCultureIgnoreCase)) {
                    b = true;
                } else if (value.Equals(@false, StringComparison.CurrentCultureIgnoreCase)) {
                    b = false;
                } else {
                    int.TryParse(value, out int i);
                    b = (i == 0 ? false : true);
                }
            }
            return b;
        }

        public static void SetBoolean(string key, bool b)
        {
            SetBoolean(DefaultAppName, key, b);
        }

        public static void SetBoolean(string app, string key, bool b)
        {
            SetBoolean(app, key, b, DefaultFileName);
        }

        public static void SetBoolean(string app, string key, bool b, string fileName)
        {
            if (!string.IsNullOrEmpty(fileName)) {
                CreateUnicodeFormatFile(Path.GetFullPath(fileName));
                kernel32.WritePrivateProfileString(
                    app, key, b ? @true : @false, Path.GetFullPath(fileName));
            }
        }
    }
}
