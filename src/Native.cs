using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace BahamaStudio
{
    public static class Native
    {
        public const string Clsid = "{887D3A6A-502E-4AF5-9CE6-D515E12AFE89}";

        // ---------------- Version / Windows ----------------

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct OSVERSIONINFOEX
        {
            public int dwOSVersionInfoSize;
            public int dwMajorVersion;
            public int dwMinorVersion;
            public int dwBuildNumber;
            public int dwPlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szCSDVersion;
            public ushort wServicePackMajor;
            public ushort wServicePackMinor;
            public ushort wSuiteMask;
            public byte wProductType;
            public byte wReserved;
        }

        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int RtlGetVersion(ref OSVERSIONINFOEX versionInfo);

        public static WindowsInfo GetWindowsInfo()
        {
            WindowsInfo info = new WindowsInfo();
            try
            {
                OSVERSIONINFOEX os = new OSVERSIONINFOEX();
                os.dwOSVersionInfoSize = Marshal.SizeOf(typeof(OSVERSIONINFOEX));
                if (RtlGetVersion(ref os) == 0)
                {
                    info.Major = os.dwMajorVersion;
                    info.Minor = os.dwMinorVersion;
                    info.Build = os.dwBuildNumber;
                }
            }
            catch { }

            if (info.Build == 0)
            {
                info.Major = Environment.OSVersion.Version.Major;
                info.Minor = Environment.OSVersion.Version.Minor;
                info.Build = Environment.OSVersion.Version.Build;
            }

            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (key != null)
                    {
                        object ubr = key.GetValue("UBR");
                        if (ubr != null) info.UBR = Convert.ToInt32(ubr);
                        object dv = key.GetValue("DisplayVersion");
                        if (dv != null) info.DisplayVersion = Convert.ToString(dv);
                        object pn = key.GetValue("ProductName");
                        if (pn != null) info.ProductName = Convert.ToString(pn);
                    }
                }
            }
            catch { }

            if (string.IsNullOrEmpty(info.DisplayVersion))
            {
                if (info.Build >= 26100) info.DisplayVersion = "24H2";
                else if (info.Build >= 22631) info.DisplayVersion = "23H2";
                else if (info.Build >= 22621) info.DisplayVersion = "22H2";
            }
            return info;
        }

        public static bool IsAdmin()
        {
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch { return false; }
        }

        public static bool IsSystemDarkMode()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        object value = key.GetValue("AppsUseLightTheme");
                        if (value != null) return Convert.ToInt32(value) == 0;
                    }
                }
            }
            catch { }
            return true;
        }

        // ---------------- Files / hashes ----------------

        public static string ComputeSha256(string path)
        {
            try
            {
                using (SHA256 sha = SHA256.Create())
                using (FileStream fs = File.OpenRead(path))
                {
                    byte[] hash = sha.ComputeHash(fs);
                    StringBuilder sb = new StringBuilder(hash.Length * 2);
                    for (int i = 0; i < hash.Length; i++) sb.Append(hash[i].ToString("X2"));
                    return sb.ToString();
                }
            }
            catch { return string.Empty; }
        }

        public static string GetFileVersion(string path)
        {
            try
            {
                FileVersionInfo info = FileVersionInfo.GetVersionInfo(path);
                if (!string.IsNullOrEmpty(info.FileVersion)) return info.FileVersion;
                if (info.FileMajorPart != 0 || info.FileMinorPart != 0)
                    return info.FileMajorPart + "." + info.FileMinorPart + "." + info.FileBuildPart + "." + info.FilePrivatePart;
            }
            catch { }
            return string.Empty;
        }

        public sealed class SignatureInfo
        {
            public bool Signed;
            public string Subject;
        }

        public static SignatureInfo GetSignature(string path)
        {
            SignatureInfo info = new SignatureInfo();
            try
            {
                X509Certificate cert = X509Certificate.CreateFromSignedFile(path);
                info.Signed = true;
                info.Subject = cert.Subject;
            }
            catch { info.Signed = false; }
            return info;
        }

        public static bool IsPe64(string path)
        {
            try
            {
                using (FileStream fs = File.OpenRead(path))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    if (fs.Length < 0x40) return false;
                    fs.Position = 0x3C;
                    int peOffset = br.ReadInt32();
                    if (peOffset <= 0 || peOffset + 6 > fs.Length) return false;
                    fs.Position = peOffset;
                    if (br.ReadUInt32() != 0x00004550) return false; // PE\0\0
                    ushort machine = br.ReadUInt16();
                    return machine == 0x8664; // AMD64
                }
            }
            catch { return false; }
        }

        // ---------------- Registry / engine registration ----------------

        public sealed class EngineRegistration
        {
            public string DllPath;
            public string Root;
            public bool HasClsid;
            public bool HasFolderExtension;
        }

        private static readonly string[] ClsidPaths =
        {
            @"SOFTWARE\Classes\CLSID\" + Clsid,
            @"SOFTWARE\Classes\WOW6432Node\CLSID\" + Clsid
        };

        private static readonly string[] FolderExtensionPaths =
        {
            @"SOFTWARE\Classes\Drive\shellex\FolderExtensions\" + Clsid
        };

        public static EngineRegistration FindEngineRegistration()
        {
            EngineRegistration result = new EngineRegistration();
            RegistryHive[] hives = new RegistryHive[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser };
            foreach (RegistryHive hive in hives)
            {
                foreach (string sub in ClsidPaths)
                {
                    try
                    {
                        using (RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default))
                        {
                            using (RegistryKey clsid = baseKey.OpenSubKey(sub))
                            {
                                if (clsid == null) continue;
                                result.HasClsid = true;
                                using (RegistryKey inproc = clsid.OpenSubKey("InProcServer32"))
                                {
                                    if (inproc != null)
                                    {
                                        object value = inproc.GetValue(null);
                                        if (value != null && !string.IsNullOrEmpty(Convert.ToString(value)))
                                        {
                                            result.DllPath = Environment.ExpandEnvironmentVariables(Convert.ToString(value));
                                            result.Root = hive == RegistryHive.LocalMachine ? "HKLM" : "HKCU";
                                        }
                                    }
                                }
                            }
                            foreach (string folder in FolderExtensionPaths)
                            {
                                using (RegistryKey ext = baseKey.OpenSubKey(folder))
                                {
                                    if (ext != null)
                                    {
                                        result.HasFolderExtension = true;
                                        if (string.IsNullOrEmpty(result.Root))
                                            result.Root = hive == RegistryHive.LocalMachine ? "HKLM" : "HKCU";
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            return result;
        }

        // ---------------- Explorer / modules / crashes ----------------

        public static Process[] GetExplorerProcesses()
        {
            try { return Process.GetProcessesByName("explorer"); }
            catch { return new Process[0]; }
        }

        public static bool IsExplorerRunning()
        {
            Process[] processes = GetExplorerProcesses();
            bool running = processes.Length > 0;
            for (int i = 0; i < processes.Length; i++) { try { processes[i].Dispose(); } catch { } }
            return running;
        }

        public static bool IsEngineLoadedInExplorer()
        {
            Process[] processes = GetExplorerProcesses();
            try
            {
                for (int i = 0; i < processes.Length; i++)
                {
                    try
                    {
                        foreach (ProcessModule module in processes[i].Modules)
                        {
                            string name = module.ModuleName;
                            if (name != null && name.IndexOf("ExplorerBlurMica", StringComparison.OrdinalIgnoreCase) >= 0)
                                return true;
                        }
                    }
                    catch { }
                }
            }
            finally
            {
                for (int i = 0; i < processes.Length; i++) { try { processes[i].Dispose(); } catch { } }
            }
            return false;
        }

        public sealed class LoadedModule
        {
            public string Name = "";
            public string FileName = "";
        }

        public static List<LoadedModule> GetExplorerModules()
        {
            List<LoadedModule> list = new List<LoadedModule>();
            Process[] processes = GetExplorerProcesses();
            try
            {
                for (int i = 0; i < processes.Length; i++)
                {
                    try
                    {
                        foreach (ProcessModule module in processes[i].Modules)
                        {
                            try
                            {
                                string name = module.ModuleName;
                                string file = module.FileName;
                                if (string.IsNullOrEmpty(name)) continue;
                                bool exists = false;
                                for (int j = 0; j < list.Count; j++)
                                {
                                    if (string.Equals(list[j].Name, name, StringComparison.OrdinalIgnoreCase))
                                    {
                                        exists = true;
                                        break;
                                    }
                                }
                                if (exists) continue;
                                LoadedModule info = new LoadedModule();
                                info.Name = name;
                                info.FileName = file ?? "";
                                list.Add(info);
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
            }
            finally
            {
                for (int i = 0; i < processes.Length; i++) { try { processes[i].Dispose(); } catch { } }
            }
            list.Sort(delegate(LoadedModule a, LoadedModule b)
            {
                return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });
            return list;
        }

        public static List<string> GetExplorerModuleNames()
        {
            List<string> names = new List<string>();
            Process[] processes = GetExplorerProcesses();
            try
            {
                for (int i = 0; i < processes.Length; i++)
                {
                    try
                    {
                        foreach (ProcessModule module in processes[i].Modules)
                            if (module.ModuleName != null && !names.Contains(module.ModuleName))
                                names.Add(module.ModuleName);
                    }
                    catch { }
                }
            }
            finally
            {
                for (int i = 0; i < processes.Length; i++) { try { processes[i].Dispose(); } catch { } }
            }
            return names;
        }

        public sealed class CrashInfo
        {
            public DateTime Time;
            public string Application;
            public string FaultingModule;
            public string ExceptionCode;
            public string ReportId;
        }

        public static List<CrashInfo> GetExplorerCrashes(int days)
        {
            List<CrashInfo> list = new List<CrashInfo>();
            try
            {
                DateTime cutoff = DateTime.Now.AddDays(-Math.Abs(days));
                string xpath = "*[System[(EventID=1000) and TimeCreated[@SystemTime>='" +
                               cutoff.ToUniversalTime().ToString("o") + "']]]";
                EventLogQuery query = new EventLogQuery("Application", PathType.LogName, xpath);
                using (EventLogReader reader = new EventLogReader(query))
                {
                    EventRecord record;
                    int guard = 0;
                    while ((record = reader.ReadEvent()) != null && guard++ < 400)
                    {
                        using (record)
                        {
                            try
                            {
                                if (record.Properties.Count < 6) continue;
                                string app = record.Properties[0].Value == null ? "" : Convert.ToString(record.Properties[0].Value);
                                if (app.IndexOf("explorer", StringComparison.OrdinalIgnoreCase) < 0) continue;
                                CrashInfo info = new CrashInfo();
                                info.Time = record.TimeCreated.HasValue ? record.TimeCreated.Value : DateTime.Now;
                                info.Application = app;
                                info.FaultingModule = record.Properties[3].Value == null ? "" : Convert.ToString(record.Properties[3].Value);
                                info.ExceptionCode = record.Properties[5].Value == null ? "" : Convert.ToString(record.Properties[5].Value);
                                if (record.Properties.Count > 10 && record.Properties[10].Value != null)
                                    info.ReportId = Convert.ToString(record.Properties[10].Value);
                                list.Add(info);
                            }
                            catch { }
                        }
                    }
                }
            }
            catch { }
            list.Sort(delegate(CrashInfo a, CrashInfo b) { return b.Time.CompareTo(a.Time); });
            if (list.Count > 30) list.RemoveRange(30, list.Count - 30);
            return list;
        }

        public static bool HasExplorerCrashSince(DateTime time)
        {
            try
            {
                string xpath = "*[System[(EventID=1000) and TimeCreated[@SystemTime>='" +
                               time.ToUniversalTime().ToString("o") + "']]]";
                EventLogQuery query = new EventLogQuery("Application", PathType.LogName, xpath);
                using (EventLogReader reader = new EventLogReader(query))
                {
                    EventRecord record;
                    while ((record = reader.ReadEvent()) != null)
                    {
                        using (record)
                        {
                            try
                            {
                                if (record.Properties.Count < 1) continue;
                                string app = record.Properties[0].Value == null ? "" : Convert.ToString(record.Properties[0].Value);
                                if (app.IndexOf("explorer", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                            }
                            catch { }
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        public static int CountExplorerDumps(DateTime since)
        {
            int count = 0;
            try
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CrashDumps");
                if (Directory.Exists(folder))
                {
                    string[] files = Directory.GetFiles(folder, "explorer.exe.*.dmp");
                    for (int i = 0; i < files.Length; i++)
                    {
                        try { if (File.GetLastWriteTime(files[i]) >= since) count++; } catch { }
                    }
                }
            }
            catch { }
            return count;
        }

        public static async Task<bool> RestartExplorerAsync(int settleMilliseconds)
        {
            try
            {
                Process[] processes = GetExplorerProcesses();
                for (int i = 0; i < processes.Length; i++)
                {
                    try { processes[i].Kill(); } catch { }
                    try { processes[i].Dispose(); } catch { }
                }
                await Task.Delay(1800);
                Process.Start(new ProcessStartInfo("explorer.exe") { UseShellExecute = true });
                await Task.Delay(settleMilliseconds);
                return IsExplorerRunning();
            }
            catch { return false; }
        }

        // ---------------- Explorer windows / native backdrop ----------------

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);
        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder name, int maxCount);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);

        public static void ApplyWindowChrome(IntPtr hwnd)
        {
            try
            {
                // DWMWA_WINDOW_CORNER_PREFERENCE = 33, DWMWCP_ROUND = 2
                int rounding = 2;
                DwmSetWindowAttribute(hwnd, 33, ref rounding, sizeof(int));

                // DWMWA_USE_IMMERSIVE_DARK_MODE = 20
                int dark = 1;
                DwmSetWindowAttribute(hwnd, 20, ref dark, sizeof(int));
            }
            catch { }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS { public int Left; public int Right; public int Top; public int Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public int AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public int Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        public static List<IntPtr> GetExplorerWindows()
        {
            List<IntPtr> handles = new List<IntPtr>();
            try
            {
                EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
                {
                    try
                    {
                        if (!IsWindowVisible(hWnd)) return true;
                        StringBuilder sb = new StringBuilder(256);
                        GetClassName(hWnd, sb, sb.Capacity);
                        string cls = sb.ToString();
                        if (cls == "CabinetWClass" || cls == "ExploreWClass")
                            handles.Add(hWnd);
                    }
                    catch { }
                    return true;
                }, IntPtr.Zero);
            }
            catch { }
            return handles;
        }

        public static int ApplyNativeBackdrop(int type, int alpha, int r, int g, int b)
        {
            int applied = 0;
            int build = GetWindowsInfo().Build;
            List<IntPtr> windows = GetExplorerWindows();
            for (int i = 0; i < windows.Count; i++)
            {
                IntPtr hwnd = windows[i];
                try
                {
                    if (build >= 22621)
                    {
                        int value = type;
                        if (DwmSetWindowAttribute(hwnd, 38, ref value, sizeof(int)) == 0) applied++;
                    }
                    else if (build >= 22000)
                    {
                        int mica = (type == 2 || type == 4) ? 1 : 0;
                        if (DwmSetWindowAttribute(hwnd, 1029, ref mica, sizeof(int)) == 0) applied++;
                    }
                    else
                    {
                        int gradient = ((alpha & 0xFF) << 24) | ((b & 0xFF) << 16) | ((g & 0xFF) << 8) | (r & 0xFF);
                        AccentPolicy policy = new AccentPolicy();
                        policy.AccentState = type == 0 ? 0 : 4; // ACCENT_ENABLE_ACRYLICBLURBEHIND
                        policy.AccentFlags = 2;
                        policy.GradientColor = gradient;
                        int size = Marshal.SizeOf(typeof(AccentPolicy));
                        IntPtr ptr = Marshal.AllocHGlobal(size);
                        try
                        {
                            Marshal.StructureToPtr(policy, ptr, false);
                            WindowCompositionAttributeData data = new WindowCompositionAttributeData();
                            data.Attribute = 19; // WCA_ACCENT_POLICY
                            data.Data = ptr;
                            data.SizeOfData = size;
                            if (SetWindowCompositionAttribute(hwnd, ref data) != 0) applied++;
                        }
                        finally { Marshal.FreeHGlobal(ptr); }
                    }
                }
                catch { }
            }
            return applied;
        }

        // ---------------- Shell / links ----------------

        public static void OpenUrl(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch { }
        }

        public static void OpenFolder(string path)
        {
            try
            {
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = "\"" + path + "\"",
                    UseShellExecute = true
                });
            }
            catch { }
        }

        public static void OpenFolderAndSelect(string path)
        {
            try
            {
                if (File.Exists(path) || Directory.Exists(path))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = "/select,\"" + path + "\"",
                        UseShellExecute = true
                    });
                }
                else OpenFolder(Path.GetDirectoryName(path));
            }
            catch { }
        }
    }

    public sealed class WindowsInfo
    {
        public int Major;
        public int Minor;
        public int Build;
        public int UBR;
        public string DisplayVersion = "";
        public string ProductName = "";

        public bool IsWindows11 { get { return Build >= 22000; } }
        public bool IsWindows10 { get { return Build >= 10240 && Build < 22000; } }
        public bool SupportsMica { get { return Build >= 22000; } }
        public string Name { get { return IsWindows11 ? "Windows 11" : (IsWindows10 ? "Windows 10" : "Windows"); } }
        public string BuildLabel { get { return Build + (UBR > 0 ? "." + UBR : ""); } }
        public string FullLabel
        {
            get
            {
                string version = string.IsNullOrEmpty(DisplayVersion) ? "" : " " + DisplayVersion;
                return Name + version + "  •  build " + BuildLabel;
            }
        }
    }
}
