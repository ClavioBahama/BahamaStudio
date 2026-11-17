using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;

namespace BahamaStudio
{
    internal static class Args
    {
        public static bool Has(string[] args, string key)
        {
            string prefix = "--" + key;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == prefix) return true;
                if (args[i].StartsWith(prefix + "=", StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        public static string Get(string[] args, string key, string fallback)
        {
            string prefix = "--" + key + "=";
            for (int i = 0; i < args.Length; i++)
            {
                if (!args[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
                string value = args[i].Substring(prefix.Length).Trim();
                if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
                    value = value.Substring(1, value.Length - 2);
                return value;
            }
            return fallback;
        }
    }

    internal static class Ops
    {
        // SHA-256 da engine oficial 2.1.0 empacotada em engine\ExplorerBlurMica.dll.
        // Nenhuma outra DLL pode ser copiada ou registada pelas operações elevadas.
        private const string BundledDllSha256 =
            "36B44444FC8AE1C4FDC3A80CDC6B0C0A04CC843984189C17DADF964BCAA9F052";

        private const string EngineFileName = "ExplorerBlurMica.dll";
        private const string ConfigFileName = "config.ini";

        // Códigos de saída distintos para cada recusa de segurança.
        private const int ExitTargetNotAllowed = 10;
        private const int ExitSourceNotAllowed = 11;
        private const int ExitSidNotAllowed = 12;

        public static bool IsOperation(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
                if (args[i].StartsWith("--op=", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public static int Run(string[] args)
        {
            string op = Args.Get(args, "op", "").ToLowerInvariant();
            try
            {
                Log.Write("[ops] iniciando op=" + op + " (admin=" + Native.IsAdmin() + ")");
                switch (op)
                {
                    case "install": return Install(args);
                    case "uninstall": return Uninstall(args);
                    case "writeconfig": return WriteConfig(args);
                    case "grant": return Grant(args);
                    case "revoke": return Revoke(args);
                    default:
                        Log.Write("[ops] operação desconhecida: " + op);
                        return 2;
                }
            }
            catch (Exception ex)
            {
                Log.Write("[ops] exceção em " + op + ": " + ex);
                return 9;
            }
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            try { return Path.GetFullPath(path.Trim()); }
            catch { return ""; }
        }

        private static bool SamePath(string a, string b)
        {
            return !string.IsNullOrEmpty(a) && !string.IsNullOrEmpty(b) &&
                   string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        private static string RegisteredEnginePath()
        {
            try
            {
                Native.EngineRegistration registration = Native.FindEngineRegistration();
                if (registration == null || string.IsNullOrEmpty(registration.DllPath)) return "";
                return NormalizePath(registration.DllPath);
            }
            catch { return ""; }
        }

        // Só a DLL já registada ou o destino oficial de Program Files são aceites.
        private static bool IsAllowedTarget(string target)
        {
            string path = NormalizePath(target);
            if (path.Length == 0) return false;
            if (!string.Equals(Path.GetFileName(path), EngineFileName, StringComparison.OrdinalIgnoreCase))
            {
                Log.Write("[ops] alvo recusado (nome de ficheiro): " + target);
                return false;
            }
            if (SamePath(path, NormalizePath(Paths.DefaultTarget))) return true;

            string registered = RegisteredEnginePath();
            if (SamePath(path, registered)) return true;

            Log.Write("[ops] alvo recusado (caminho): " + target);
            return false;
        }

        // writeconfig/grant/revoke atuam apenas no config.ini ao lado dessa DLL.
        private static bool IsAllowedConfig(string target)
        {
            string path = NormalizePath(target);
            if (path.Length == 0) return false;
            if (!string.Equals(Path.GetFileName(path), ConfigFileName, StringComparison.OrdinalIgnoreCase))
            {
                Log.Write("[ops] config recusado (nome de ficheiro): " + target);
                return false;
            }

            string dir = NormalizePath(Path.GetDirectoryName(path));
            string defaultDir = NormalizePath(Path.GetDirectoryName(Paths.DefaultTarget));
            if (SamePath(dir, defaultDir)) return true;

            string registered = RegisteredEnginePath();
            if (registered.Length > 0 && SamePath(dir, NormalizePath(Path.GetDirectoryName(registered))))
                return true;

            Log.Write("[ops] config recusado (pasta): " + target);
            return false;
        }

        // A origem tem de ser exatamente a DLL empacotada ao lado do executável.
        private static bool IsAllowedSource(string source)
        {
            string path = NormalizePath(source);
            if (!SamePath(path, NormalizePath(Paths.BundledDll)))
            {
                Log.Write("[ops] origem recusada (caminho): " + source);
                return false;
            }
            string hash = Native.ComputeSha256(path);
            if (!string.Equals(hash, BundledDllSha256, StringComparison.OrdinalIgnoreCase))
            {
                Log.Write("[ops] origem recusada (SHA-256): " + hash);
                return false;
            }
            return true;
        }

        // Depois do UAC o SID continua a ser o do utilizador: o token é que muda.
        // Aceitar um SID arbitrário permitiria conceder acesso a outra conta.
        // O argumento --sid é opcional e só é aceite se for igual ao SID atual.
        private static bool TryResolveSid(string[] args, out string sid)
        {
            sid = "";
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                if (identity == null || identity.User == null) return false;

                string current = identity.User.Value;
                string supplied = Args.Get(args, "sid", "");
                if (string.IsNullOrEmpty(supplied))
                {
                    sid = current;
                    return true;
                }

                SecurityIdentifier parsed = new SecurityIdentifier(supplied);
                if (!string.Equals(parsed.Value, current, StringComparison.OrdinalIgnoreCase))
                {
                    Log.Write("[ops] SID recebido não corresponde ao utilizador que iniciou a operação.");
                    return false;
                }

                sid = current;
                return true;
            }
            catch (Exception ex)
            {
                Log.Write("[ops] SID inválido: " + ex.Message);
                return false;
            }
        }

        private static int Install(string[] args)
        {
            string target = Args.Get(args, "target", Paths.DefaultTarget);
            string source = Args.Get(args, "source", Paths.BundledDll);
            string config = Args.Get(args, "config", "");
            string license = Args.Get(args, "license", "");
            string readme = Args.Get(args, "readme", "");
            bool grant = Args.Get(args, "grant", "0") == "1";

            if (!IsAllowedTarget(target)) return ExitTargetNotAllowed;
            if (!IsAllowedSource(source)) return ExitSourceNotAllowed;

            string sid;
            if (!TryResolveSid(args, out sid)) return ExitSidNotAllowed;

            if (!File.Exists(source))
            {
                Log.Write("[ops] engine incluída não encontrada: " + source);
                return 3;
            }

            string dir = Path.GetDirectoryName(target);
            if (string.IsNullOrEmpty(dir))
            {
                Log.Write("[ops] destino inválido: " + target);
                return 3;
            }

            try { Directory.CreateDirectory(dir); }
            catch (Exception ex)
            {
                Log.Write("[ops] não foi possível criar " + dir + ": " + ex.Message);
                return 4;
            }

            // O Explorer mantém a DLL carregada (e o Windows recria o processo durante o UAC).
            // Renomear o ficheiro em uso é permitido, então guardamos a versão antiga de lado.
            RetireLoadedDll(target);

            if (!CopyWithRetry(source, target)) return 4;

            CleanupRetired(dir);

            string targetConfig = Path.Combine(dir, "config.ini");
            if (!string.IsNullOrEmpty(config) && File.Exists(config))
            {
                if (!CopyWithRetry(config, targetConfig)) Log.Write("[ops] aviso: config não copiada");
            }

            string targetLicense = Path.Combine(dir, "LICENSE-LGPLv3.txt");
            string targetReadme = Path.Combine(dir, "ReadMe-ExplorerBlurMica.txt");
            if (!string.IsNullOrEmpty(license) && File.Exists(license)) CopyWithRetry(license, targetLicense);
            if (!string.IsNullOrEmpty(readme) && File.Exists(readme)) CopyWithRetry(readme, targetReadme);

            int regsvr = CallRegSvr(target, false);
            if (regsvr != 0)
            {
                Log.Write("[ops] regsvr32 retornou " + regsvr + "; aplicando registro direto.");
                if (!RegisterDirect(target))
                {
                    Log.Write("[ops] falha no registro direto.");
                    return 5;
                }
            }

            if (grant && File.Exists(targetConfig)) GrantFile(targetConfig, sid);

            Log.Write("[ops] instalado em " + target);
            return 0;
        }

        private static int Uninstall(string[] args)
        {
            string target = Args.Get(args, "target", Paths.DefaultTarget);
            bool remove = Args.Get(args, "remove", "0") == "1";

            if (!IsAllowedTarget(target)) return ExitTargetNotAllowed;

            if (File.Exists(target)) CallRegSvr(target, true);
            UnregisterDirect();

            if (remove)
            {
                string dir = Path.GetDirectoryName(target);
                if (!string.IsNullOrEmpty(dir))
                {
                    RetireLoadedDll(target);
                    TryDelete(target);
                    TryDelete(Path.Combine(dir, "config.ini"));
                    TryDelete(Path.Combine(dir, "LICENSE-LGPLv3.txt"));
                    TryDelete(Path.Combine(dir, "ReadMe-ExplorerBlurMica.txt"));
                    CleanupRetired(dir);
                }
            }

            Log.Write("[ops] desinstalado (remove=" + remove + ") target=" + target);
            return 0;
        }

        private static int WriteConfig(string[] args)
        {
            string target = Args.Get(args, "target", "");
            string source = Args.Get(args, "source", "");
            if (!IsAllowedConfig(target)) return ExitTargetNotAllowed;
            if (string.IsNullOrEmpty(source) || !File.Exists(source))
            {
                Log.Write("[ops] writeconfig inválido.");
                return 3;
            }
            string sid;
            if (!TryResolveSid(args, out sid)) return ExitSidNotAllowed;
            if (!CopyWithRetry(source, target)) return 4;
            GrantFile(target, sid);
            Log.Write("[ops] config gravada em " + target);
            return 0;
        }

        private static int Grant(string[] args)
        {
            string target = Args.Get(args, "target", "");
            if (!IsAllowedConfig(target)) return ExitTargetNotAllowed;
            if (!File.Exists(target)) return 3;
            string sid;
            if (!TryResolveSid(args, out sid)) return ExitSidNotAllowed;
            return GrantFile(target, sid) ? 0 : 5;
        }

        private static int Revoke(string[] args)
        {
            string target = Args.Get(args, "target", "");
            if (!IsAllowedConfig(target)) return ExitTargetNotAllowed;
            if (!File.Exists(target)) return 3;
            string sid;
            if (!TryResolveSid(args, out sid)) return ExitSidNotAllowed;
            try
            {
                RunTool("icacls.exe", "\"" + target + "\" /remove *" + sid, 15000);
                Log.Write("[ops] permissão removida de " + target);
                return 0;
            }
            catch (Exception ex)
            {
                Log.Write("[ops] revoke: " + ex.Message);
                return 5;
            }
        }

        private static bool GrantFile(string path, string sid)
        {
            try
            {
                RunTool("icacls.exe", "\"" + path + "\" /grant *" + sid + ":(R,W)", 15000);
                Log.Write("[ops] permissão de gravação liberada para " + path);
                return true;
            }
            catch (Exception ex)
            {
                Log.Write("[ops] grant: " + ex.Message);
                return false;
            }
        }

        private static bool CopyWithRetry(string source, string target)
        {
            for (int attempt = 0; attempt < 4; attempt++)
            {
                try
                {
                    File.Copy(source, target, true);
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Write("[ops] cópia tentativa " + (attempt + 1) + ": " + ex.Message);
                    if (attempt == 1) RetireLoadedDll(target);
                    Thread.Sleep(600);
                }
            }
            return false;
        }

        private static void RetireLoadedDll(string target)
        {
            if (!target.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) return;
            try
            {
                if (!File.Exists(target)) return;
                string dir = Path.GetDirectoryName(target);
                if (string.IsNullOrEmpty(dir)) return;
                string retired = Path.Combine(dir,
                    Path.GetFileNameWithoutExtension(target) + ".old-" +
                    DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".dll");
                File.Move(target, retired);
                Log.Write("[ops] DLL anterior renomeada para " + retired);
            }
            catch (Exception ex)
            {
                Log.Write("[ops] não foi possível renomear a DLL em uso: " + ex.Message);
            }
        }

        private static void CleanupRetired(string dir)
        {
            try
            {
                string pattern = "*.old-*.dll";
                foreach (string file in Directory.GetFiles(dir, pattern))
                {
                    try { File.Delete(file); }
                    catch { } // Continua carregada nos processos antigos do Explorer; sai depois.
                }
            }
            catch (Exception ex)
            {
                Log.Write("[ops] limpeza de DLLs antigas: " + ex.Message);
            }
        }

        private static int CallRegSvr(string dll, bool unregister)
        {
            try
            {
                string exe = Path.Combine(Environment.SystemDirectory, "regsvr32.exe");
                string arguments = (unregister ? "/u " : "") + "/s \"" + dll + "\"";
                ProcessStartInfo info = new ProcessStartInfo(exe, arguments);
                info.UseShellExecute = false;
                info.CreateNoWindow = true;
                using (Process process = Process.Start(info))
                {
                    if (process == null) return -1;
                    if (!process.WaitForExit(45000))
                    {
                        try { process.Kill(); } catch { }
                        return -1;
                    }
                    return process.ExitCode;
                }
            }
            catch (Exception ex)
            {
                Log.Write("[ops] regsvr32: " + ex.Message);
                return -1;
            }
        }

        private static bool RegisterDirect(string dll)
        {
            try
            {
                using (RegistryKey clsid = Registry.LocalMachine.CreateSubKey(
                    @"SOFTWARE\Classes\CLSID\" + Native.Clsid + @"\InProcServer32"))
                {
                    if (clsid == null) return false;
                    clsid.SetValue(null, dll);
                    clsid.SetValue("ThreadingModel", "Apartment");
                }
                using (RegistryKey ext = Registry.LocalMachine.CreateSubKey(
                    @"SOFTWARE\Classes\Drive\shellex\FolderExtensions\" + Native.Clsid))
                {
                    if (ext == null) return false;
                    ext.SetValue("DriveMask", 255, RegistryValueKind.DWord);
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Write("[ops] registro direto: " + ex.Message);
                return false;
            }
        }

        private static void UnregisterDirect()
        {
            TryDeleteKey(@"SOFTWARE\Classes\CLSID\" + Native.Clsid);
            TryDeleteKey(@"SOFTWARE\Classes\WOW6432Node\CLSID\" + Native.Clsid);
            TryDeleteKey(@"SOFTWARE\Classes\Drive\shellex\FolderExtensions\" + Native.Clsid);
            TryDeleteKey(@"SOFTWARE\Classes\WOW6432Node\Drive\shellex\FolderExtensions\" + Native.Clsid);
        }

        private static void TryDeleteKey(string path)
        {
            try { Registry.LocalMachine.DeleteSubKeyTree(path, false); }
            catch (Exception ex) { Log.Write("[ops] chave " + path + ": " + ex.Message); }
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (Exception ex) { Log.Write("[ops] apagar " + path + ": " + ex.Message); }
        }

        private static void RunTool(string exe, string arguments, int timeout)
        {
            ProcessStartInfo info = new ProcessStartInfo(exe, arguments);
            info.UseShellExecute = false;
            info.CreateNoWindow = true;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            using (Process process = Process.Start(info))
            {
                if (process == null) return;
                process.WaitForExit(timeout);
            }
        }
    }

    internal static class SelfTest
    {
        public static void Run(MainWindow window, string outputDir)
        {
            try { Directory.CreateDirectory(outputDir); }
            catch (Exception ex)
            {
                Log.Write("[selftest] não foi possível criar " + outputDir + ": " + ex.Message);
                window.ForceClose();
                return;
            }

            string[] keys = { "dashboard", "aparencia", "instalar", "diagnostico", "emergencia" };
            List<string> audit = new List<string>();
            int step = 0;
            bool dialogCaptured = false;

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(850);
            timer.Tick += delegate
            {
                try
                {
                    if (step > 0 && !dialogCaptured)
                    {
                        string previous = keys[step - 1];
                        Capture(window, Path.Combine(outputDir, (step) + "-" + previous + ".png"));
                        Audit(window, previous, audit);
                    }

                    if (step >= keys.Length)
                    {
                        if (!dialogCaptured)
                        {
                            dialogCaptured = true;
                            window.Dialog.Alert("Diálogo de exemplo",
                                "Este é o host de diálogos do Bahama Studio. Ele também é usado para confirmações de instalação, " +
                                "remoção e reversão automática.", "Entendi");
                            return;
                        }
                        Capture(window, Path.Combine(outputDir, "6-dialogo.png"));
                        window.Dialog.Close();
                        timer.Stop();
                        WriteReport(window, outputDir, keys, audit);
                        window.ForceClose();
                        return;
                    }

                    window.Navigate(keys[step]);
                    step++;
                }
                catch (Exception ex)
                {
                    Log.Write("[selftest] " + ex.Message);
                    timer.Stop();
                    window.ForceClose();
                }
            };
            timer.Start();
        }

        private static void Capture(MainWindow window, string path)
        {
            window.UpdateLayout();
            int width = (int)Math.Ceiling(window.ActualWidth);
            int height = (int)Math.Ceiling(window.ActualHeight);
            if (width <= 0 || height <= 0) return;

            RenderTargetBitmap bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(window);
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write))
                encoder.Save(stream);
            Log.Write("[selftest] captura " + path);
        }

        private static void Audit(MainWindow window, string key, List<string> audit)
        {
            try
            {
                window.UpdateLayout();
                double limit = window.ActualWidth;
                int overflow = 0;
                int truncated = 0;
                List<string> notes = new List<string>();
                int visited = 0;

                Walk(window, window, delegate(FrameworkElement element)
                {
                    visited++;
                    if (element.Visibility != Visibility.Visible) return;
                    if (!element.IsHitTestVisible) return;

                    TextBlock text = element as TextBlock;
                    if (text != null && text.TextTrimming != TextTrimming.None && text.ActualWidth > 0 &&
                        text.DesiredSize.Width > text.ActualWidth + 1)
                    {
                        truncated++;
                        if (notes.Count < 10)
                            notes.Add("texto truncado: \"" + Short(text.Text) + "\" precisa " +
                                      text.DesiredSize.Width.ToString("0") + "px, tem " + text.ActualWidth.ToString("0") + "px");
                    }

                    Rect bounds = new Rect(0, 0, element.ActualWidth, element.ActualHeight);
                    try
                    {
                        GeneralTransform transform = element.TransformToAncestor(window);
                        bounds = transform.TransformBounds(bounds);
                    }
                    catch { return; }

                    if (element.ActualWidth > 1 && bounds.Right > limit + 1)
                    {
                        overflow++;
                        if (notes.Count < 10)
                            notes.Add("transbordo: " + Describe(element) + " termina em " + bounds.Right.ToString("0") +
                                      "px (limite " + limit.ToString("0") + "px)");
                    }
                });

                audit.Add("[" + key + "] elementos=" + visited + " transbordo=" + overflow + " textoTruncado=" + truncated);
                for (int i = 0; i < notes.Count; i++) audit.Add("    " + notes[i]);
            }
            catch (Exception ex)
            {
                audit.Add("[" + key + "] auditoria falhou: " + ex.Message);
            }
        }

        private static void Walk(DependencyObject parent, Visual root, Action<FrameworkElement> visit)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                FrameworkElement element = child as FrameworkElement;
                if (element != null)
                {
                    // ignora a raiz da janela para não medir a própria moldura
                    if (element != root) visit(element);
                }
                Walk(child, root, visit);
            }
        }

        private static string Short(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            value = value.Replace("\r", " ").Replace("\n", " ").Trim();
            return value.Length > 42 ? value.Substring(0, 42) + "..." : value;
        }

        private static string Describe(FrameworkElement element)
        {
            string name = element.GetType().Name;
            TextBlock text = element as TextBlock;
            if (text != null) return name + "(\"" + Short(text.Text) + "\")";
            string content = element.ToolTip as string;
            if (!string.IsNullOrEmpty(content)) return name + "(" + content + ")";
            return name;
        }

        private static void WriteReport(MainWindow window, string dir, string[] keys, List<string> audit)
        {
            try
            {
                EngineStatus status = EngineManager.Detect();
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Bahama Studio - autoteste de interface");
                sb.AppendLine("Data: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                sb.AppendLine("Janela: " + window.ActualWidth + " x " + window.ActualHeight);
                sb.AppendLine("Tema carregado: " + (Application.Current.Resources.MergedDictionaries.Count > 0));
                sb.AppendLine("Páginas visitadas: " + string.Join(", ", keys));
                sb.AppendLine();
                sb.AppendLine("Windows: " + status.Windows.Name + " " + status.Windows.DisplayVersion + " build " + status.Windows.BuildLabel);
                sb.AppendLine("Engine instalada: " + status.IsInstalled + " (" + status.InstalledVersion + ")");
                sb.AppendLine("Engine carregada no Explorer: " + status.EngineLoaded);
                sb.AppendLine("Config: " + status.ConfigPath + " gravável=" + status.ConfigWritable);
                sb.AppendLine("Falhas 7 dias: " + Native.GetExplorerCrashes(7).Count);
                sb.AppendLine();
                sb.AppendLine("Auditoria de layout");
                for (int i = 0; i < audit.Count; i++) sb.AppendLine("  " + audit[i]);
                File.WriteAllText(Path.Combine(dir, "selftest.txt"), sb.ToString(), new UTF8Encoding(false));
            }
            catch (Exception ex)
            {
                Log.Write("[selftest] relatório: " + ex.Message);
            }
        }
    }

    public static class Program
    {
        private static Mutex _instance;

        [STAThread]
        public static int Main(string[] args)
        {
            if (Ops.IsOperation(args))
            {
                try { return Ops.Run(args); }
                catch (Exception ex)
                {
                    Log.Write("[ops] falha fatal: " + ex);
                    return 9;
                }
            }

            bool selfTest = Args.Has(args, "selftest");
            bool startup = Args.Has(args, "startup");

            string shots = Args.Get(args, "screenshots", Path.Combine(Path.GetTempPath(), "BahamaStudio-selftest"));

            if (!selfTest && !AcquireInstance())
            {
                MessageBox.Show("O Bahama Studio já está aberto. Procure a janela na barra de tarefas.",
                    "Bahama Studio", MessageBoxButton.OK, MessageBoxImage.Information);
                return 0;
            }

            Application app = new Application();
            app.ShutdownMode = ShutdownMode.OnMainWindowClose;

            try
            {
                Theme.Load();
            }
            catch (Exception ex)
            {
                Log.Write("[startup] tema: " + ex);
                MessageBox.Show("Não foi possível carregar o tema visual.\n\n" + ex.Message,
                    "Bahama Studio", MessageBoxButton.OK, MessageBoxImage.Error);
                return 1;
            }

            app.DispatcherUnhandledException += delegate(object sender, DispatcherUnhandledExceptionEventArgs e)
            {
                Log.Write("[unhandled-ui] " + e.Exception);
                MessageBox.Show("Ocorreu um erro inesperado.\n\n" + e.Exception.Message +
                                "\n\nO detalhe foi gravado em:\n" + Paths.LogFile,
                    "Bahama Studio", MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
            {
                Log.Write("[unhandled] " + e.ExceptionObject);
            };

            MainWindow window = new MainWindow();
            app.MainWindow = window;

            if (selfTest)
            {
                string size = Args.Get(args, "size", "");
                if (!string.IsNullOrEmpty(size))
                {
                    string[] parts = size.ToLowerInvariant().Split('x');
                    double width;
                    double height;
                    if (parts.Length == 2 && double.TryParse(parts[0], out width) && double.TryParse(parts[1], out height))
                    {
                        window.Width = width;
                        window.Height = height;
                    }
                }
            }

            app.Startup += delegate
            {
                window.Show();
                if (startup)
                {
                    // Arrancado pelo atalho da pasta Inicialização: reaplica os efeitos
                    // a partir do config.ini e fica minimizado na barra de tarefas.
                    window.WindowState = WindowState.Minimized;
                    Log.Write("[startup] arranque automático: efeitos serão reaplicados do config.ini");
                }
                if (selfTest) SelfTest.Run(window, shots);
            };

            return app.Run();
        }

        private static bool AcquireInstance()
        {
            try
            {
                bool created;
                _instance = new Mutex(true, "Local\\BahamaStudio_SingleInstance_v1", out created);
                return created;
            }
            catch
            {
                return true;
            }
        }
    }
}
