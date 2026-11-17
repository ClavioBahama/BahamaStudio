using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace BahamaStudio
{
    public struct Rgba
    {
        public int R;
        public int G;
        public int B;
        public int A;

        public Rgba(int r, int g, int b, int a)
        {
            R = Clamp(r);
            G = Clamp(g);
            B = Clamp(b);
            A = Clamp(a);
        }

        public static int Clamp(int value)
        {
            if (value < 0) return 0;
            if (value > 255) return 255;
            return value;
        }

        public Rgba WithAlpha(int alpha)
        {
            return new Rgba(R, G, B, alpha);
        }

        public string ToHex()
        {
            return string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", R, G, B);
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "rgba({0}, {1}, {2}, {3})", R, G, B, A);
        }
    }

    public sealed class EngineConfig
    {
        // O Acrílico é o efeito que realmente usa a cor personalizada no Windows 11 24H2+.
        public int Effect = 1;
        public bool ClearAddress = true;
        public bool ClearBarBg = true;
        public bool ClearWinUIBg = true;
        public bool ShowLine = true;
        public bool TotalTransparency = false;
        public int TotalTransparencyLevel = 100;
        public Rgba Light = new Rgba(245, 249, 255, 200);
        public Rgba Dark = new Rgba(18, 34, 54, 228);

        public static EngineConfig CreateDefault()
        {
            return new EngineConfig();
        }

        public EngineConfig Clone()
        {
            EngineConfig copy = new EngineConfig();
            copy.Effect = Effect;
            copy.ClearAddress = ClearAddress;
            copy.ClearBarBg = ClearBarBg;
            copy.ClearWinUIBg = ClearWinUIBg;
            copy.ShowLine = ShowLine;
            copy.TotalTransparency = TotalTransparency;
            copy.TotalTransparencyLevel = TotalTransparencyLevel;
            copy.Light = Light;
            copy.Dark = Dark;
            return copy;
        }

        public static EngineConfig Load(string path)
        {
            EngineConfig config = CreateDefault();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return config;

            try
            {
                string section = "";
                string[] lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.Length == 0) continue;
                    if (line.StartsWith(";") || line.StartsWith("#")) continue;

                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        section = line.Substring(1, line.Length - 2).Trim().ToLowerInvariant();
                        continue;
                    }

                    int equals = line.IndexOf('=');
                    if (equals <= 0) continue;
                    string key = line.Substring(0, equals).Trim().ToLowerInvariant();
                    string value = line.Substring(equals + 1).Trim();

                    if (section == "config")
                    {
                        if (key == "effect") config.Effect = ParseInt(value, config.Effect);
                        else if (key == "clearaddress") config.ClearAddress = ParseBool(value, config.ClearAddress);
                        else if (key == "clearbarbg") config.ClearBarBg = ParseBool(value, config.ClearBarBg);
                        else if (key == "clearwinuibg") config.ClearWinUIBg = ParseBool(value, config.ClearWinUIBg);
                        else if (key == "showline") config.ShowLine = ParseBool(value, config.ShowLine);
                        else if (key == "totaltransparency") config.TotalTransparency = ParseBool(value, config.TotalTransparency);
                        else if (key == "totaltransparencylevel") config.TotalTransparencyLevel = ParseInt(value, config.TotalTransparencyLevel);
                    }
                    else if (section == "light" || section == "dark")
                    {
                        Rgba current = section == "light" ? config.Light : config.Dark;
                        int r = current.R, g = current.G, b = current.B, a = current.A;
                        if (key == "r") r = ParseInt(value, r);
                        else if (key == "g") g = ParseInt(value, g);
                        else if (key == "b") b = ParseInt(value, b);
                        else if (key == "a") a = ParseInt(value, a);
                        if (section == "light") config.Light = new Rgba(r, g, b, a);
                        else config.Dark = new Rgba(r, g, b, a);
                    }
                }
            }
            catch
            {
                // Configuração inválida: mantém os padrões seguros.
            }
            return config;
        }

        private static int ParseInt(string value, int fallback)
        {
            int parsed;
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed)) return parsed;
            return fallback;
        }

        private static bool ParseBool(string value, bool fallback)
        {
            if (string.IsNullOrEmpty(value)) return fallback;
            string v = value.Trim().ToLowerInvariant();
            if (v == "true" || v == "1" || v == "yes" || v == "on") return true;
            if (v == "false" || v == "0" || v == "no" || v == "off") return false;
            return fallback;
        }

        public string Serialize()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[config]\r\n");
            sb.Append("effect=").Append(Effect.ToString(CultureInfo.InvariantCulture)).Append("\r\n");
            sb.Append("clearAddress=").Append(ClearAddress ? "true" : "false").Append("\r\n");
            sb.Append("clearBarBg=").Append(ClearBarBg ? "true" : "false").Append("\r\n");
            sb.Append("clearWinUIBg=").Append(ClearWinUIBg ? "true" : "false").Append("\r\n");
            sb.Append("showLine=").Append(ShowLine ? "true" : "false").Append("\r\n");
            sb.Append("totalTransparency=").Append(TotalTransparency ? "true" : "false").Append("\r\n");
            sb.Append("totalTransparencyLevel=").Append(TotalTransparencyLevel.ToString(CultureInfo.InvariantCulture)).Append("\r\n");
            sb.Append("[light]\r\n");
            sb.Append("r=").Append(Light.R.ToString(CultureInfo.InvariantCulture)).Append("\r\n");
            sb.Append("g=").Append(Light.G.ToString(CultureInfo.InvariantCulture)).Append("\r\n");
            sb.Append("b=").Append(Light.B.ToString(CultureInfo.InvariantCulture)).Append("\r\n");
            sb.Append("a=").Append(Light.A.ToString(CultureInfo.InvariantCulture)).Append("\r\n");
            sb.Append("[dark]\r\n");
            sb.Append("r=").Append(Dark.R.ToString(CultureInfo.InvariantCulture)).Append("\r\n");
            sb.Append("g=").Append(Dark.G.ToString(CultureInfo.InvariantCulture)).Append("\r\n");
            sb.Append("b=").Append(Dark.B.ToString(CultureInfo.InvariantCulture)).Append("\r\n");
            sb.Append("a=").Append(Dark.A.ToString(CultureInfo.InvariantCulture)).Append("\r\n");
            return sb.ToString();
        }

        public void Save(string path)
        {
            string folder = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder)) Directory.CreateDirectory(folder);
            File.WriteAllText(path, Serialize(), new UTF8Encoding(false));
        }

        public static string EffectName(int effect)
        {
            switch (effect)
            {
                case 0: return "Blur (legado)";
                case 1: return "Acrílico";
                case 2: return "Mica";
                case 3: return "Blur puro";
                case 4: return "Mica Alt";
                default: return "Efeito " + effect;
            }
        }

        public static string EffectShort(int effect)
        {
            switch (effect)
            {
                case 0: return "Blur";
                case 1: return "Acrílico";
                case 2: return "Mica";
                case 3: return "Blur puro";
                case 4: return "Mica Alt";
                default: return "Efeito " + effect;
            }
        }

        public static bool EffectUsesColor(int effect)
        {
            return effect == 0 || effect == 1;
        }

        public static string EffectDescription(int effect)
        {
            switch (effect)
            {
                case 1: return "Desfoque com a sua cor. É o efeito que melhor mostra a transparência personalizada.";
                case 2: return "Vidro nativo do Windows 11: usa as cores do sistema e ignora a cor personalizada.";
                case 3: return "Somente o desfoque do Windows, sem tingir com a cor escolhida.";
                case 4: return "Mica com mais contraste. Também usa as cores do sistema, não a sua.";
                default: return "Blur no estilo antigo do Windows; aceita a cor personalizada.";
            }
        }

        public static string EffectColorNote(int effect)
        {
            if (EffectUsesColor(effect)) return "";
            return "Neste Windows, " + EffectName(effect) + " usa as cores do sistema/papel de parede. " +
                   "A sua cor aparece no Acrílico e no Blur.";
        }
    }

    public static class Paths
    {
        public static string AppDir
        {
            get { return AppDomain.CurrentDomain.BaseDirectory; }
        }

        public static string ExePath
        {
            get
            {
                try { return Process.GetCurrentProcess().MainModule.FileName; }
                catch { return Path.Combine(AppDir, "BahamaStudio.exe"); }
            }
        }

        public static string DataDir
        {
            get
            {
                string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string path = Path.Combine(local, "BahamaStudio");
                string legacy = Path.Combine(local, "MicaStudio");

                // Migração transparente de quem já usava o nome antigo.
                if (!Directory.Exists(path) && Directory.Exists(legacy))
                {
                    try { Directory.Move(legacy, path); }
                    catch { path = legacy; }
                }

                EnsureDir(path);

                try
                {
                    string oldLog = Path.Combine(path, "logs", "mica-studio.log");
                    string newLog = Path.Combine(path, "logs", "bahama-studio.log");
                    if (File.Exists(oldLog) && !File.Exists(newLog)) File.Move(oldLog, newLog);
                }
                catch { }

                return path;
            }
        }

        public static string LogsDir { get { return EnsureDir(Path.Combine(DataDir, "logs")); } }
        public static string BackupsDir { get { return EnsureDir(Path.Combine(DataDir, "Backups")); } }
        public static string TempDir { get { return EnsureDir(Path.Combine(DataDir, "Temp")); } }
        public static string SettingsFile { get { return Path.Combine(DataDir, "settings.ini"); } }
        public static string LogFile { get { return Path.Combine(LogsDir, "bahama-studio.log"); } }

        public static string BundledDll { get { return Path.Combine(AppDir, "engine", "ExplorerBlurMica.dll"); } }
        public static string BundledConfig { get { return Path.Combine(AppDir, "engine", "config.default.ini"); } }
        public static string BundledLicense { get { return Path.Combine(AppDir, "engine", "LICENSE-LGPLv3.txt"); } }
        public static string BundledReadme { get { return Path.Combine(AppDir, "engine", "ReadMe-ExplorerBlurMica.txt"); } }

        public static string DefaultTarget
        {
            get
            {
                string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                if (string.IsNullOrEmpty(programFiles)) programFiles = @"C:\Program Files";
                return Path.Combine(programFiles, "ExplorerBlurMica.dll");
            }
        }

        public static string EnsureDir(string path)
        {
            try { if (!Directory.Exists(path)) Directory.CreateDirectory(path); }
            catch { }
            return path;
        }

        public static string Shorten(string path, int max)
        {
            if (string.IsNullOrEmpty(path)) return "";
            if (path.Length <= max) return path;
            int keep = Math.Max(8, max - 3);
            string tail = path.Substring(path.Length - keep);
            int slash = tail.IndexOf('\\');
            if (slash >= 0 && slash < tail.Length - 1) tail = tail.Substring(slash);
            return "..." + tail;
        }
    }

    public static class Log
    {
        private static readonly object Gate = new object();

        public static void Write(string message)
        {
            try
            {
                lock (Gate)
                {
                    string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) +
                                  "  " + message + "\r\n";
                    File.AppendAllText(Paths.LogFile, line, new UTF8Encoding(false));
                }
            }
            catch { }
        }

        public static string Tail(int lineCount)
        {
            try
            {
                if (!File.Exists(Paths.LogFile)) return "(sem registros)";
                string[] lines = File.ReadAllLines(Paths.LogFile);
                int start = Math.Max(0, lines.Length - lineCount);
                StringBuilder sb = new StringBuilder();
                for (int i = start; i < lines.Length; i++) sb.AppendLine(lines[i]);
                return sb.ToString();
            }
            catch { return "(não foi possível ler o log)"; }
        }
    }

    public sealed class AppSettings
    {
        public bool FirstRun = true;
        public string LastPage = "dashboard";
        public bool GrantQuickApply = true;
        public bool ValidateFolder = true;
        public bool DarkPreview = true;

        public static AppSettings Load()
        {
            AppSettings settings = new AppSettings();
            try
            {
                if (!File.Exists(Paths.SettingsFile)) return settings;
                string[] lines = File.ReadAllLines(Paths.SettingsFile);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    int equals = line.IndexOf('=');
                    if (equals <= 0) continue;
                    string key = line.Substring(0, equals).Trim().ToLowerInvariant();
                    string value = line.Substring(equals + 1).Trim();
                    if (key == "firstrun") settings.FirstRun = value == "1" || value == "true";
                    else if (key == "lastpage") settings.LastPage = value;
                    else if (key == "grantquickapply") settings.GrantQuickApply = value == "1" || value == "true";
                    else if (key == "validatefolder") settings.ValidateFolder = value == "1" || value == "true";
                    else if (key == "darkpreview") settings.DarkPreview = value == "1" || value == "true";
                }
            }
            catch { }
            return settings;
        }

        public void Save()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("firstRun=").Append(FirstRun ? "1" : "0").Append("\r\n");
                sb.Append("lastPage=").Append(LastPage ?? "dashboard").Append("\r\n");
                sb.Append("grantQuickApply=").Append(GrantQuickApply ? "1" : "0").Append("\r\n");
                sb.Append("validateFolder=").Append(ValidateFolder ? "1" : "0").Append("\r\n");
                sb.Append("darkPreview=").Append(DarkPreview ? "1" : "0").Append("\r\n");
                File.WriteAllText(Paths.SettingsFile, sb.ToString(), new UTF8Encoding(false));
            }
            catch { }
        }
    }

    public sealed class EngineStatus
    {
        public string RegisteredPath = "";
        public string ConfigPath = "";
        public bool HasClsid;
        public bool HasFolderExtension;
        public bool InstalledExists;
        public bool ConfigExists;
        public bool ConfigWritable;
        public string InstalledVersion = "";
        public string ConfigVersion = "";
        public bool BundledExists;
        public string BundledVersion = "";
        public string BundledHash = "";
        public string InstalledHash = "";
        public bool MatchesBundled;
        public bool ExplorerRunning;
        public bool EngineLoaded;
        public WindowsInfo Windows = new WindowsInfo();
        public int ExplorerProcessCount;
        public int DumpCount;

        public bool IsInstalled
        {
            get { return HasClsid && HasFolderExtension && InstalledExists; }
        }

        public bool IsModern
        {
            get { return VersionAtLeast(InstalledVersion, "2.1.0"); }
        }

        public bool IsOutdated
        {
            get { return IsInstalled && !IsModern; }
        }

        public bool NeedsInstall
        {
            get { return !IsInstalled || !IsModern; }
        }

        public static bool VersionAtLeast(string version, string minimum)
        {
            try
            {
                Version v = new Version(version);
                Version m = new Version(minimum);
                return v >= m;
            }
            catch { return false; }
        }
    }

    public sealed class OpResult
    {
        public bool Success;
        public bool Cancelled;
        public int ExitCode;
        public string Message = "";
    }

    public sealed class InstallReport
    {
        public bool Success;
        public bool RolledBack;
        public bool Cancelled;
        public bool ModuleLoaded;
        public string Message = "";
        public string Target = "";
        public List<string> Steps = new List<string>();

        public void Step(string text)
        {
            Steps.Add(text);
            Log.Write("[install] " + text);
        }
    }

    public sealed class ApplyReport
    {
        public bool Success;
        public bool RolledBack;
        public bool Cancelled;
        public bool ModuleLoaded;
        public string Message = "";
        public List<string> Steps = new List<string>();

        public void Step(string text)
        {
            Steps.Add(text);
            Log.Write("[apply] " + text);
        }
    }

    public sealed class BackupInfo
    {
        public string Folder = "";
        public DateTime Time;
        public bool HasDll;
        public bool HasConfig;
        public string DllPath = "";
        public string ConfigPath = "";
    }

    public static class EngineManager
    {
        public static EngineStatus Detect()
        {
            EngineStatus status = new EngineStatus();
            status.Windows = Native.GetWindowsInfo();

            Native.EngineRegistration registration = Native.FindEngineRegistration();
            status.HasClsid = registration.HasClsid;
            status.HasFolderExtension = registration.HasFolderExtension;
            if (!string.IsNullOrEmpty(registration.DllPath))
            {
                status.RegisteredPath = registration.DllPath;
                status.ConfigPath = Path.Combine(Path.GetDirectoryName(registration.DllPath) ?? "", "config.ini");
                status.InstalledExists = File.Exists(registration.DllPath);
                if (status.InstalledExists)
                {
                    status.InstalledVersion = Native.GetFileVersion(registration.DllPath);
                    status.InstalledHash = Native.ComputeSha256(registration.DllPath);
                }
            }
            if (string.IsNullOrEmpty(status.ConfigPath))
            {
                status.ConfigPath = Path.Combine(
                    Path.GetDirectoryName(Paths.DefaultTarget) ?? "", "config.ini");
            }
            status.ConfigExists = File.Exists(status.ConfigPath);
            status.ConfigWritable = CanWrite(status.ConfigPath);
            if (status.ConfigExists) status.ConfigVersion = Native.GetFileVersion(Paths.BundledDll);

            status.BundledExists = File.Exists(Paths.BundledDll);
            if (status.BundledExists)
            {
                status.BundledVersion = Native.GetFileVersion(Paths.BundledDll);
                status.BundledHash = Native.ComputeSha256(Paths.BundledDll);
            }
            status.MatchesBundled = status.InstalledExists && status.BundledExists &&
                                    !string.IsNullOrEmpty(status.InstalledHash) &&
                                    status.InstalledHash == status.BundledHash;

            status.ExplorerRunning = Native.IsExplorerRunning();
            if (status.ExplorerRunning)
            {
                status.EngineLoaded = Native.IsEngineLoadedInExplorer();
                Process[] processes = Native.GetExplorerProcesses();
                status.ExplorerProcessCount = processes.Length;
                for (int i = 0; i < processes.Length; i++) { try { processes[i].Dispose(); } catch { } }
            }
            status.DumpCount = Native.CountExplorerDumps(DateTime.Now.AddDays(-7));
            return status;
        }

        public static EngineConfig LoadActiveConfig()
        {
            EngineStatus status = Detect();
            if (status.InstalledExists && File.Exists(status.ConfigPath))
                return EngineConfig.Load(status.ConfigPath);

            // Primeira execução (ou config ainda ausente): adapta ao PC sem gravar nada.
            EngineConfig config = File.Exists(Paths.BundledConfig)
                ? EngineConfig.Load(Paths.BundledConfig)
                : EngineConfig.CreateDefault();
            MachineProfile.ApplyRecommendation(MachineProfile.Capture(), config);
            return config;
        }

        public static bool CanWrite(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite)) { }
                    return true;
                }

                string folder = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return false;
                string probe = Path.Combine(folder, ".mica-write-" + Guid.NewGuid().ToString("N") + ".tmp");
                using (FileStream fs = new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { }
                File.Delete(probe);
                return true;
            }
            catch { return false; }
        }

        // ---------------- Elevated operations ----------------

        public static OpResult RunElevated(string arguments)
        {
            OpResult result = new OpResult();
            try
            {
                Log.Write("[elevate] " + arguments);
                ProcessStartInfo info = new ProcessStartInfo(Paths.ExePath, arguments);
                info.UseShellExecute = true;
                info.Verb = "runas";
                info.WindowStyle = ProcessWindowStyle.Hidden;

                using (Process process = Process.Start(info))
                {
                    if (process == null)
                    {
                        result.Message = "Não foi possível iniciar a operação elevada.";
                        return result;
                    }
                    process.WaitForExit();
                    result.ExitCode = process.ExitCode;
                    result.Success = process.ExitCode == 0;
                    if (!result.Success) result.Message = "A operação elevada retornou o código " + process.ExitCode + ".";
                }
            }
            catch (Win32Exception ex)
            {
                if (ex.NativeErrorCode == 1223)
                {
                    result.Cancelled = true;
                    result.Message = "Operação cancelada na janela do Windows.";
                }
                else
                {
                    result.Message = "Falha ao elevar privilégios: " + ex.Message;
                }
                Log.Write("[elevate] " + result.Message);
            }
            catch (Exception ex)
            {
                result.Message = "Falha na operação elevada: " + ex.Message;
                Log.Write("[elevate] " + ex);
            }
            return result;
        }

        private static string Q(string key, string value)
        {
            if (value == null) value = "";
            return "--" + key + "=\"" + value.Replace("\"", "") + "\"";
        }

        public static InstallReport Install(EngineConfig config, bool grantQuickApply, bool validateFolder, IProgress<string> progress)
        {
            InstallReport report = new InstallReport();
            string target = Paths.DefaultTarget;
            string tempConfig = Path.Combine(Paths.TempDir, "install-" + Guid.NewGuid().ToString("N") + ".ini");

            try
            {
                progress.Report("Verificando os arquivos da engine...");
                if (!File.Exists(Paths.BundledDll))
                {
                    report.Message = "A engine 2.1.0 não foi encontrada na pasta engine do aplicativo.";
                    return report;
                }

                string sourceVersion = Native.GetFileVersion(Paths.BundledDll);
                if (!EngineStatus.VersionAtLeast(sourceVersion, "2.1.0"))
                {
                    report.Message = "A engine incluída está desatualizada (" + sourceVersion + ").";
                    return report;
                }

                EngineStatus before = Detect();
                if (before.IsInstalled && !string.IsNullOrEmpty(before.RegisteredPath))
                    target = before.RegisteredPath;

                report.Target = target;
                report.Step("Engine " + sourceVersion + " verificada (x64)");

                progress.Report("Criando um ponto de restauração...");
                string backupFolder = null;
                if (before.InstalledExists || File.Exists(before.ConfigPath))
                {
                    backupFolder = CreateBackupFolder();
                    if (before.InstalledExists)
                    {
                        try { File.Copy(before.RegisteredPath, Path.Combine(backupFolder, "ExplorerBlurMica.dll"), true); }
                        catch (Exception ex) { Log.Write("[backup] dll: " + ex.Message); }
                    }
                    if (File.Exists(before.ConfigPath))
                    {
                        try { File.Copy(before.ConfigPath, Path.Combine(backupFolder, "config.ini"), true); }
                        catch (Exception ex) { Log.Write("[backup] config: " + ex.Message); }
                    }
                    report.Step("Backup criado em " + backupFolder);
                }
                else
                {
                    report.Step("Instalação limpa: nada anterior para copiar");
                }

                config.Save(tempConfig);

                progress.Report("Encerrando o Explorador...");
                KillExplorer();
                Thread.Sleep(1200);
                KillExplorer();
                report.Step("Explorador encerrado para liberar a DLL");

                progress.Report("Copiando e registrando a engine (UAC)...");
                string args = "--op=install " +
                              Q("target", target) + " " +
                              Q("source", Paths.BundledDll) + " " +
                              Q("config", tempConfig) + " " +
                              Q("license", Paths.BundledLicense) + " " +
                              Q("readme", Paths.BundledReadme) + " " +
                              "--grant=" + (grantQuickApply ? "1" : "0");

                OpResult op = RunElevated(args);
                if (!op.Success)
                {
                    report.Cancelled = op.Cancelled;
                    report.Message = op.Cancelled
                        ? "Instalação cancelada. A extensão não foi alterada."
                        : "Não foi possível copiar/registrar a engine. " + op.Message;
                    Log.Write("[install] " + report.Message);
                    return report;
                }
                report.Step("Engine copiada para " + target);
                report.Step("Extensão registrada no Windows (regsvr32)");

                progress.Report("Verificando o registro...");
                EngineStatus after = Detect();
                if (!after.HasClsid || !after.HasFolderExtension)
                {
                    report.Message = "O registro da extensão não foi confirmado. Nada foi ativado.";
                    return report;
                }
                report.Step("Registro confirmado (CLSID + FolderExtension)");

                progress.Report("Iniciando o Explorador...");
                DateTime marker = DateTime.Now.AddSeconds(-1);
                StartExplorer();
                Thread.Sleep(2400);

                bool loaded = false;
                if (validateFolder)
                {
                    progress.Report("Abrindo uma pasta de teste...");
                    StartExplorerAt(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                    loaded = WaitForModule(12000);
                }
                else
                {
                    loaded = WaitForModule(4000);
                }

                progress.Report("Verificando a estabilidade do Explorador...");
                Thread.Sleep(1800);
                bool crashed = Native.HasExplorerCrashSince(marker);
                bool running = Native.IsExplorerRunning();

                if (crashed || !running)
                {
                    progress.Report("Reversão de segurança em andamento...");
                    Rollback(target);
                    report.RolledBack = true;
                    report.Success = false;
                    report.ModuleLoaded = false;
                    report.Step("Falha detectada no Explorador");
                    report.Step("Extensão desativada automaticamente");
                    report.Message = "O Explorador não se manteve estável com esta instalação. A extensão foi desativada e o Explorador reiniciado. Seus arquivos estão intactos.";
                    return report;
                }

                report.ModuleLoaded = loaded;
                if (loaded) report.Step("Módulo carregado no Explorador");
                else report.Step("Extensão pronta (será carregada ao abrir uma pasta)");

                if (grantQuickApply) report.Step("Ajustes de cor liberados sem UAC (somente config.ini)");

                report.Success = true;
                report.Message = "Tudo pronto! A aparência do Explorador já está ativa.";
                Log.Write("[install] sucesso em " + target);
                return report;
            }
            catch (Exception ex)
            {
                report.Success = false;
                report.Message = "Falha inesperada: " + ex.Message;
                Log.Write("[install] exceção: " + ex);
                return report;
            }
            finally
            {
                try { File.Delete(tempConfig); } catch { }
                if (!Native.IsExplorerRunning()) StartExplorer();
            }
        }

        public static ApplyReport ApplyConfig(EngineConfig config, bool restart, IProgress<string> progress)
        {
            ApplyReport report = new ApplyReport();
            try
            {
                EngineStatus status = Detect();
                if (!status.IsInstalled)
                {
                    report.Message = "A engine ainda não está instalada. Use a página Instalar.";
                    return report;
                }

                progress.Report("Gravando config.ini...");
                string backup = null;
                if (File.Exists(status.ConfigPath))
                {
                    string folder = CreateBackupFolder();
                    string copy = Path.Combine(folder, "config.ini");
                    try { File.Copy(status.ConfigPath, copy, true); backup = copy; } catch { }
                }

                string error;
                if (!WriteConfig(config, status.ConfigPath, out error))
                {
                    report.Message = "Não foi possível gravar a configuração. " + error;
                    return report;
                }
                report.Step("Configuração gravada em " + status.ConfigPath);

                if (!restart)
                {
                    report.Success = true;
                    report.Message = "Configuração salva. Abra uma pasta para ver o resultado.";
                    return report;
                }

                progress.Report("Reiniciando o Explorador...");
                DateTime marker = DateTime.Now.AddSeconds(-1);
                KillExplorer();
                Thread.Sleep(900);
                StartExplorer();
                Thread.Sleep(1600);
                StartExplorerAt(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                bool loaded = WaitForModule(10000);
                Thread.Sleep(1200);

                bool crashed = Native.HasExplorerCrashSince(marker);
                bool running = Native.IsExplorerRunning();
                if (crashed || !running)
                {
                    progress.Report("Reversão automática da configuração...");
                    if (!string.IsNullOrEmpty(backup)) WriteConfigFile(backup, status.ConfigPath);
                    KillExplorer();
                    Thread.Sleep(700);
                    StartExplorer();
                    report.RolledBack = true;
                    report.Success = false;
                    report.Message = "Esta combinação instabilizou o Explorador. A configuração anterior foi restaurada automaticamente.";
                    report.Step("Configuração anterior restaurada");
                    return report;
                }

                report.Success = true;
                report.ModuleLoaded = loaded;
                report.Message = loaded
                    ? "Aparência aplicada e carregada no Explorador."
                    : "Aparência aplicada. Abra uma pasta para carregar o efeito.";
                report.Step("Explorador reiniciado e estável");
                return report;
            }
            catch (Exception ex)
            {
                report.Message = "Falha inesperada: " + ex.Message;
                Log.Write("[apply] exceção: " + ex);
                return report;
            }
        }

        public static InstallReport Uninstall(bool removeFiles, IProgress<string> progress)
        {
            InstallReport report = new InstallReport();
            try
            {
                EngineStatus status = Detect();
                string target = status.IsInstalled && !string.IsNullOrEmpty(status.RegisteredPath)
                    ? status.RegisteredPath
                    : Paths.DefaultTarget;

                progress.Report("Encerrando o Explorador...");
                KillExplorer();
                Thread.Sleep(900);

                progress.Report(removeFiles ? "Removendo a extensão e os arquivos..." : "Desativando a extensão...");
                OpResult op = RunElevated("--op=uninstall " + Q("target", target) + " --remove=" + (removeFiles ? "1" : "0"));
                if (op.Cancelled)
                {
                    report.Cancelled = true;
                    report.Message = "Operação cancelada. Nada foi alterado.";
                }
                else if (!op.Success)
                {
                    report.Message = "Falha ao desregistrar a extensão. " + op.Message;
                }
                else
                {
                    report.Success = true;
                    report.Message = removeFiles
                        ? "Extensão removida. O Explorador voltou ao normal."
                        : "Extensão desativada. O Explorador voltou ao normal.";
                    report.Step("Extensão desregistrada");
                }
                return report;
            }
            catch (Exception ex)
            {
                report.Message = "Falha inesperada: " + ex.Message;
                Log.Write("[uninstall] exceção: " + ex);
                return report;
            }
            finally
            {
                StartExplorer();
                Thread.Sleep(1200);
                if (!Native.IsExplorerRunning()) StartExplorer();
            }
        }

        public static bool WriteConfig(EngineConfig config, string targetPath, out string error)
        {
            error = "";
            try
            {
                if (CanWrite(targetPath))
                {
                    config.Save(targetPath);
                    return true;
                }

                string temp = Path.Combine(Paths.TempDir, "config-" + Guid.NewGuid().ToString("N") + ".ini");
                config.Save(temp);
                try
                {
                    OpResult op = RunElevated("--op=writeconfig " + Q("target", targetPath) + " " + Q("source", temp));
                    if (!op.Success)
                    {
                        error = op.Cancelled ? "Cancelado na janela do Windows." : op.Message;
                        return false;
                    }
                    return true;
                }
                finally
                {
                    try { File.Delete(temp); } catch { }
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static bool WriteConfigFile(string sourcePath, string targetPath)
        {
            try
            {
                if (CanWrite(targetPath))
                {
                    File.Copy(sourcePath, targetPath, true);
                    return true;
                }
                OpResult op = RunElevated("--op=writeconfig " + Q("target", targetPath) + " " + Q("source", sourcePath));
                return op.Success;
            }
            catch (Exception ex)
            {
                Log.Write("[config] restore: " + ex.Message);
                return false;
            }
        }

        public static bool GrantQuickApply(out string message)
        {
            message = "";
            try
            {
                EngineStatus status = Detect();
                OpResult op = RunElevated("--op=grant " + Q("target", status.ConfigPath));
                if (!op.Success)
                {
                    message = op.Cancelled ? "Cancelado." : op.Message;
                    return false;
                }
                message = "Ajustes de cor agora podem ser gravados sem UAC.";
                return true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }

        public static bool RevokeQuickApply(out string message)
        {
            message = "";
            try
            {
                EngineStatus status = Detect();
                OpResult op = RunElevated("--op=revoke " + Q("target", status.ConfigPath));
                if (!op.Success)
                {
                    message = op.Cancelled ? "Cancelado." : op.Message;
                    return false;
                }
                message = "Ajustes rápidos desativados (UAC será solicitado).";
                return true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }

        // ---------------- Backups ----------------

        public static string CreateBackupFolder()
        {
            string folder = Path.Combine(Paths.BackupsDir, DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture));
            int suffix = 1;
            while (Directory.Exists(folder))
            {
                folder = Path.Combine(Paths.BackupsDir,
                    DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + "-" + suffix);
                suffix++;
            }
            Directory.CreateDirectory(folder);
            return folder;
        }

        public static List<BackupInfo> ListBackups()
        {
            List<BackupInfo> list = new List<BackupInfo>();
            try
            {
                foreach (string folder in Directory.GetDirectories(Paths.BackupsDir))
                {
                    BackupInfo info = new BackupInfo();
                    info.Folder = folder;
                    try { info.Time = Directory.GetCreationTime(folder); } catch { info.Time = DateTime.MinValue; }
                    string dll = Path.Combine(folder, "ExplorerBlurMica.dll");
                    string config = Path.Combine(folder, "config.ini");
                    info.HasDll = File.Exists(dll);
                    info.HasConfig = File.Exists(config);
                    info.DllPath = dll;
                    info.ConfigPath = config;
                    list.Add(info);
                }
                list.Sort(delegate(BackupInfo a, BackupInfo b) { return b.Time.CompareTo(a.Time); });
            }
            catch { }
            return list;
        }

        public static bool RestoreLatestConfig(out string message)
        {
            message = "";
            try
            {
                EngineStatus status = Detect();
                List<BackupInfo> backups = ListBackups();
                for (int i = 0; i < backups.Count; i++)
                {
                    if (!backups[i].HasConfig) continue;
                    if (WriteConfigFile(backups[i].ConfigPath, status.ConfigPath))
                    {
                        message = "Configuração restaurada do backup de " + backups[i].Time.ToString("dd/MM/yyyy HH:mm") + ".";
                        return true;
                    }
                    message = "Não foi possível gravar o backup.";
                    return false;
                }
                message = "Nenhum backup de configuração foi encontrado.";
                return false;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }

        // ---------------- Explorer helpers ----------------

        public static void KillExplorer()
        {
            Process[] processes = Native.GetExplorerProcesses();
            for (int i = 0; i < processes.Length; i++)
            {
                try { processes[i].Kill(); } catch { }
                try { processes[i].WaitForExit(2000); } catch { }
                try { processes[i].Dispose(); } catch { }
            }
        }

        public static void StartExplorer()
        {
            try
            {
                Process.Start(new ProcessStartInfo("explorer.exe") { UseShellExecute = true });
            }
            catch (Exception ex) { Log.Write("[explorer] start: " + ex.Message); }
        }

        public static void StartExplorerAt(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = "/n,\"" + path + "\"",
                    UseShellExecute = true
                });
            }
            catch (Exception ex) { Log.Write("[explorer] open: " + ex.Message); }
        }

        public static bool WaitForModule(int timeoutMilliseconds)
        {
            int waited = 0;
            while (waited < timeoutMilliseconds)
            {
                if (Native.IsEngineLoadedInExplorer()) return true;
                Thread.Sleep(500);
                waited += 500;
            }
            return Native.IsEngineLoadedInExplorer();
        }

        private static void Rollback(string target)
        {
            try
            {
                KillExplorer();
                Thread.Sleep(700);
                RunElevated("--op=uninstall " + Q("target", target) + " --remove=0");
            }
            catch (Exception ex)
            {
                Log.Write("[rollback] " + ex.Message);
            }
            finally
            {
                StartExplorer();
                Thread.Sleep(1200);
                if (!Native.IsExplorerRunning()) StartExplorer();
            }
        }
    }
}
