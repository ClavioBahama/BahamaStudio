using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace BahamaStudio
{
    /// <summary>
    /// Leitura local das características reais do computador. Nada é enviado para a internet.
    /// </summary>
    public sealed class MachineProfile
    {
        public string WindowsName = "";
        public string BuildLabel = "";
        public string DisplayVersion = "";
        public string ProductName = "";
        public string Architecture = "";
        public string ProcessArchitecture = "";
        public string Processor = "";
        public string RamGb = "";
        public string Screen = "";
        public string Theme = "";
        public string ComputerName = "";
        public string UserName = "";
        public string ProgramFiles = "";
        public string LocalAppData = "";
        public string LogsDir = "";
        public string BackupsDir = "";
        public string BundledVersion = "";
        public string RegisteredPath = "";
        public string InstalledVersion = "";
        public bool EngineLoaded;
        public bool SupportsMica;
        public string ConfigPath = "";
        public bool ConfigExists;

        [StructLayout(LayoutKind.Sequential)]
        private struct MemoryStatusEx
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", EntryPoint = "GlobalMemoryStatusEx", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx status);

        public static MachineProfile Capture()
        {
            MachineProfile p = new MachineProfile();
            try
            {
                WindowsInfo w = Native.GetWindowsInfo();
                p.WindowsName = w.Name;
                p.BuildLabel = w.BuildLabel;
                p.DisplayVersion = w.DisplayVersion;
                p.ProductName = w.ProductName;
                p.SupportsMica = w.SupportsMica;
            }
            catch { }

            p.Architecture = Environment.Is64BitOperatingSystem ? "x64" : "x86";
            p.ProcessArchitecture = Environment.Is64BitProcess ? "x64" : "x86";
            p.Processor = SafeEnv("PROCESSOR_IDENTIFIER", "desconhecido");
            p.RamGb = ReadRam();
            p.Screen = Math.Round(SystemParameters.PrimaryScreenWidth) + "x" +
                       Math.Round(SystemParameters.PrimaryScreenHeight);
            p.Theme = Native.IsSystemDarkMode() ? "escuro" : "claro";
            p.ComputerName = Environment.MachineName;
            p.UserName = Environment.UserName;

            p.ProgramFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            p.LocalAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            p.LogsDir = Paths.LogsDir;
            p.BackupsDir = Paths.BackupsDir;

            try
            {
                EngineStatus status = EngineManager.Detect();
                p.BundledVersion = status.BundledVersion;
                p.RegisteredPath = status.RegisteredPath;
                p.InstalledVersion = status.InstalledVersion;
                p.EngineLoaded = status.EngineLoaded;
                p.ConfigPath = status.ConfigPath;
                p.ConfigExists = status.ConfigExists;
            }
            catch { }

            return p;
        }

        public string Summary
        {
            get
            {
                if (SupportsMica) return "Windows 11 • Acrílico recomendado";
                if (IsWindows10Modern()) return "Windows 10 1809+ • Acrílico recomendado";
                return "Sistema legado • Blur puro recomendado";
            }
        }

        public string[] ReportLines()
        {
            return new string[]
            {
                "  Máquina: " + ComputerName + " (" + UserName + ")",
                "  Windows: " + WindowsName + " " + DisplayVersion + " (build " + BuildLabel + ")",
                "  Edição: " + ProductName,
                "  Arquitetura: " + Architecture + " / processo " + ProcessArchitecture,
                "  Processador: " + Processor,
                "  Memória RAM: " + RamGb,
                "  Ecrã principal: " + Screen,
                "  Tema: " + Theme,
                "  Mica nativo: " + (SupportsMica ? "sim" : "não"),
                "  Program Files: " + ProgramFiles,
                "  LocalAppData: " + LocalAppData,
                "  Logs: " + LogsDir,
                "  Backups: " + BackupsDir,
                "  Engine incluída: " + BundledVersion,
                "  Engine registrada: " + (string.IsNullOrEmpty(InstalledVersion) ? "ausente" : "v" + InstalledVersion) + " em " + RegisteredPath,
                "  Módulo carregado no Explorer: " + (EngineLoaded ? "sim" : "não"),
                "  Config: " + ConfigPath + (ConfigExists ? " (existe)" : " (ausente)"),
                "  Perfil: " + Summary
            };
        }

        /// <summary>
        /// Aplica a recomendação ao objeto de configuração. As cores escolhidas pelo usuário
        /// nunca são alteradas.
        /// </summary>
        public static void ApplyRecommendation(MachineProfile profile, EngineConfig config)
        {
            if (profile == null || config == null) return;
            if (profile.SupportsMica)
            {
                config.Effect = 1;
                config.ClearAddress = true;
                config.ClearBarBg = true;
                config.ClearWinUIBg = true;
                config.ShowLine = true;
                config.TotalTransparency = false;
                config.TotalTransparencyLevel = 100;
            }
            else if (profile.IsWindows10Modern())
            {
                config.Effect = 1;
                config.ClearAddress = true;
                config.ClearBarBg = true;
                config.ClearWinUIBg = false;
                config.ShowLine = true;
                config.TotalTransparency = false;
                config.TotalTransparencyLevel = 100;
            }
            else
            {
                config.Effect = 3;
                config.ClearAddress = true;
                config.ClearBarBg = true;
                config.ClearWinUIBg = false;
                config.ShowLine = true;
                config.TotalTransparency = false;
                config.TotalTransparencyLevel = 100;
            }
        }

        private bool IsWindows10Modern()
        {
            int build;
            if (!int.TryParse(BuildLabel.Split('.')[0], out build)) return false;
            return build >= 17763 && build < 22000;
        }

        private static string SafeEnv(string name, string fallback)
        {
            try
            {
                string value = Environment.GetEnvironmentVariable(name);
                return string.IsNullOrEmpty(value) ? fallback : value;
            }
            catch { return fallback; }
        }

        private static string ReadRam()
        {
            try
            {
                MemoryStatusEx status = new MemoryStatusEx();
                status.dwLength = (uint)Marshal.SizeOf(typeof(MemoryStatusEx));
                if (!GlobalMemoryStatusEx(ref status)) return "desconhecida";
                double gb = status.ullTotalPhys / (1024.0 * 1024.0 * 1024.0);
                return Math.Round(gb, 1).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " GB";
            }
            catch { return "desconhecida"; }
        }
    }
}
