using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Threading;

namespace BahamaStudio
{
    // Aplica transparência nativa às janelas CabinetWClass e mantém o efeito
    // reaplicando-o depois de ativar, restaurar, minimizar ou mostrar a janela.
    public static class ExplorerTransparency
    {
        private const string ExplorerClass = "CabinetWClass";

        private const int WcaAccentPolicy = 19;
        private const int AccentDisabled = 0;
        private const int AccentEnableTransparentGradient = 2;

        private const uint EventSystemForeground = 0x0003;
        private const uint EventSystemMinimizeEnd = 0x0017;
        private const uint EventObjectShow = 0x8002;
        private const uint EventObjectLocationChange = 0x800B;

        private const uint WineventOutOfContext = 0x0000;
        private const uint WineventSkipOwnProcess = 0x0002;

        private const int ObjidWindow = 0;

        // O watchdog existe para vencer o repaint do Explorador no 24H2. A cada 5 s
        // (em vez de 1,2 s) é suficiente e reduz o trabalho em segundo plano.
        private const int WatchdogIntervalMs = 5000;

        // EVENT_OBJECT_LOCATIONCHANGE dispara em cascata. Ignorar a mesma janela
        // durante este intervalo evita reaplicações redundantes sem perder o efeito.
        private const int ReapplyDebounceMs = 300;

        private const int ClassCacheLimit = 256;

        [StructLayout(LayoutKind.Sequential)]
        private struct Margins
        {
            public int Left;
            public int Right;
            public int Top;
            public int Bottom;
        }

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

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
            int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder name, int maxCount);

        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd,
            ref WindowCompositionAttributeData data);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc,
            WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margins margins);

        private static bool _active;
        private static IntPtr _hookForeground = IntPtr.Zero;

        // Os hooks de janela são instalados por PID do explorer.exe: o próprio
        // Windows filtra os eventos antes de chegarem ao nosso callback.
        private static readonly List<IntPtr> _processHooks = new List<IntPtr>();
        private static readonly List<int> _hookPids = new List<int>();

        // Cache de classes e de última aplicação para não repetir P/Invoke e
        // SetWindowCompositionAttribute quando nada mudou.
        private static readonly Dictionary<IntPtr, bool> _classCache = new Dictionary<IntPtr, bool>();
        private static readonly Dictionary<IntPtr, uint> _lastApply = new Dictionary<IntPtr, uint>();

        private static DispatcherTimer _reapplyTimer;
        private static DispatcherTimer _watchdogTimer;
        private static int _applyCount;
        private static int _lastLoggedCount = -1;
        private static bool _forceApply;
        private static int _level = 100;
        private static int _gradientColor;
        private static int _gradientLevel = -1;
        private static DateTime _gradientStamp = DateTime.MinValue;

        // O coletor de lixo não enxerga referências nativas: os delegates
        // precisam ficar em campos estáticos enquanto os hooks estiverem ativos.
        private static readonly WinEventDelegate HookProc = OnWinEvent;
        private static readonly EnumWindowsProc EnumProc = OnEnumWindow;

        public static bool IsActive
        {
            get { return _active; }
        }

        public static int Level
        {
            get { return _level; }
        }

        public static void SetEnabled(bool enabled)
        {
            SetEnabled(enabled, _level);
        }

        public static void SetEnabled(bool enabled, int level)
        {
            SetLevel(level);
            if (enabled) Start();
            else Stop();
        }

        public static void SetLevel(int level)
        {
            int clamped = ClampLevel(level);
            if (clamped == _level) return;

            _level = clamped;
            _gradientLevel = -1;

            if (_active) ApplyAll();
        }

        private static int ClampLevel(int level)
        {
            if (level < 0) return 0;
            if (level > 100) return 100;
            return level;
        }

        public static void Start()
        {
            if (_active) return;

            try
            {
                _active = true;
                _lastLoggedCount = -1;
                EnsureTimers();
                InstallHooks();
                ApplyAll();
                Log.Write("[transparency] transparência total ativada em " + _level + "%");
            }
            catch (Exception ex)
            {
                _active = false;
                UninstallHooks();
                StopTimers();
                Log.Write("[transparency] falha ao ativar: " + ex.Message);
            }
        }

        public static void Stop()
        {
            if (!_active) return;

            try
            {
                _active = false;
                UninstallHooks();
                StopTimers();
                _classCache.Clear();
                _lastApply.Clear();
                RestoreAll();
                Log.Write("[transparency] transparência total desativada");
            }
            catch (Exception ex)
            {
                Log.Write("[transparency] falha ao restaurar: " + ex.Message);
            }
        }

        private static void EnsureTimers()
        {
            if (_reapplyTimer == null)
            {
                _reapplyTimer = new DispatcherTimer();
                _reapplyTimer.Interval = TimeSpan.FromMilliseconds(120);
                _reapplyTimer.Tick += delegate
                {
                    _reapplyTimer.Stop();
                    ApplyAll();
                };
            }

            if (_watchdogTimer == null)
            {
                _watchdogTimer = new DispatcherTimer();
                _watchdogTimer.Interval = TimeSpan.FromMilliseconds(WatchdogIntervalMs);
                _watchdogTimer.Tick += delegate
                {
                    // O Explorador pode ter reiniciado: atualiza os hooks por PID
                    // e limpa os caches antes de reaplicar a todas as janelas.
                    EnsureExplorerHooks();
                    _classCache.Clear();
                    _lastApply.Clear();
                    ApplyAll(true);
                };
            }

            _reapplyTimer.Stop();
            _watchdogTimer.Stop();
            _watchdogTimer.Start();
        }

        private static void StopTimers()
        {
            if (_reapplyTimer != null) _reapplyTimer.Stop();
            if (_watchdogTimer != null) _watchdogTimer.Stop();
        }

        private static void InstallHooks()
        {
            uint flags = WineventOutOfContext | WineventSkipOwnProcess;

            // O foreground é global: a janela que ganha foco pode ser de qualquer
            // aplicação, e este evento é pouco frequente.
            _hookForeground = SetWinEventHook(EventSystemForeground, EventSystemForeground,
                IntPtr.Zero, HookProc, 0, 0, flags);

            EnsureExplorerHooks();
        }

        private static void EnsureExplorerHooks()
        {
            if (!_active) return;

            uint flags = WineventOutOfContext | WineventSkipOwnProcess;
            List<int> pids = new List<int>();
            try
            {
                Process[] processes = Native.GetExplorerProcesses();
                for (int i = 0; i < processes.Length; i++)
                {
                    try
                    {
                        int pid = processes[i].Id;
                        if (pid > 0 && !pids.Contains(pid)) pids.Add(pid);
                    }
                    catch { }
                    try { processes[i].Dispose(); } catch { }
                }
            }
            catch { }

            if (SamePids(pids, _hookPids)) return;

            for (int i = 0; i < _processHooks.Count; i++)
            {
                try { UnhookWinEvent(_processHooks[i]); } catch { }
            }
            _processHooks.Clear();
            _hookPids.Clear();

            // Os restantes eventos interessam apenas quando vêm do explorer.exe.
            // Passar o PID ao SetWinEventHook filtra no sistema, antes do callback.
            for (int i = 0; i < pids.Count; i++)
            {
                IntPtr hook = SetWinEventHook(EventSystemMinimizeEnd, EventSystemMinimizeEnd,
                    IntPtr.Zero, HookProc, (uint)pids[i], 0, flags);
                if (hook != IntPtr.Zero) _processHooks.Add(hook);

                hook = SetWinEventHook(EventObjectShow, EventObjectShow,
                    IntPtr.Zero, HookProc, (uint)pids[i], 0, flags);
                if (hook != IntPtr.Zero) _processHooks.Add(hook);

                hook = SetWinEventHook(EventObjectLocationChange, EventObjectLocationChange,
                    IntPtr.Zero, HookProc, (uint)pids[i], 0, flags);
                if (hook != IntPtr.Zero) _processHooks.Add(hook);

                _hookPids.Add(pids[i]);
            }
        }

        private static bool SamePids(List<int> a, List<int> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++) if (a[i] != b[i]) return false;
            return true;
        }

        private static void UninstallHooks()
        {
            if (_hookForeground != IntPtr.Zero)
            {
                UnhookWinEvent(_hookForeground);
                _hookForeground = IntPtr.Zero;
            }
            for (int i = 0; i < _processHooks.Count; i++)
            {
                try { UnhookWinEvent(_processHooks[i]); } catch { }
            }
            _processHooks.Clear();
            _hookPids.Clear();
        }

        private static void OnWinEvent(IntPtr hook, uint eventType, IntPtr hwnd,
            int idObject, int idChild, uint eventThread, uint eventTime)
        {
            if (!_active) return;

            // EVENT_OBJECT_LOCATIONCHANGE também chega para controles internos.
            if (eventType == EventObjectLocationChange &&
                (idObject != ObjidWindow || idChild != 0)) return;

            bool isExplorer = hwnd != IntPtr.Zero && IsExplorerWindowCached(hwnd);

            if (isExplorer && (eventType == EventSystemForeground ||
                               eventType == EventSystemMinimizeEnd ||
                               eventType == EventObjectShow))
            {
                ApplyToWindow(hwnd, false);
            }

            // Ao perder o foco, o novo foreground pertence a outra janela:
            // reaplicar em todas garante que o Explorador não volte ao fundo padrão.
            if (eventType == EventSystemForeground || isExplorer)
                QueueReapply();
        }

        private static void QueueReapply()
        {
            if (!_active || _reapplyTimer == null) return;
            // Um tick já agendado chega: não reiniciar o timer a cada evento.
            if (!_reapplyTimer.IsEnabled) _reapplyTimer.Start();
        }

        private static bool OnEnumWindow(IntPtr hwnd, IntPtr lParam)
        {
            if (!_active) return false;
            if (!IsWindowVisible(hwnd)) return true;
            if (!IsExplorerWindowCached(hwnd)) return true;

            ApplyToWindow(hwnd, _forceApply);
            _applyCount++;
            return true;
        }

        private static bool IsExplorerWindowCached(IntPtr hwnd)
        {
            bool result;
            if (_classCache.TryGetValue(hwnd, out result)) return result;

            result = IsExplorerWindow(hwnd);
            if (_classCache.Count >= ClassCacheLimit) _classCache.Clear();
            _classCache[hwnd] = result;
            return result;
        }

        private static bool IsExplorerWindow(IntPtr hwnd)
        {
            try
            {
                StringBuilder name = new StringBuilder(64);
                GetClassName(hwnd, name, name.Capacity);
                return string.Equals(name.ToString(), ExplorerClass, StringComparison.Ordinal);
            }
            catch
            {
                return false;
            }
        }

        private static void ApplyAll()
        {
            ApplyAll(false);
        }

        private static void ApplyAll(bool force)
        {
            if (!_active) return;

            _forceApply = force;
            _applyCount = 0;
            try
            {
                EnumWindows(EnumProc, IntPtr.Zero);
            }
            catch
            {
                // Enumeração defensiva: uma janela fechada não deve parar o efeito.
            }

            if (_applyCount != _lastLoggedCount)
            {
                _lastLoggedCount = _applyCount;
                Log.Write("[transparency] aplicada em " + _applyCount + " janela(s) do Explorador");
            }
        }

        private static void ApplyToWindow(IntPtr hwnd, bool force)
        {
            if (!_active) return;
            if (!IsWindowVisible(hwnd)) return;
            if (!IsExplorerWindowCached(hwnd)) return;
            if (!force && !ShouldApply(hwnd)) return;

            ApplyAccent(hwnd, AccentEnableTransparentGradient, 2, CurrentGradient());
            ExtendFrame(hwnd, -1);
        }

        private static bool ShouldApply(IntPtr hwnd)
        {
            uint now = unchecked((uint)Environment.TickCount);
            uint last;
            if (_lastApply.TryGetValue(hwnd, out last) &&
                unchecked(now - last) < ReapplyDebounceMs) return false;

            _lastApply[hwnd] = now;
            return true;
        }

        private static int CurrentGradient()
        {
            // 100% = 0x00000000 exato, sem tinta. Abaixo disso, a cor de fundo
            // do tema é misturada para dar o nível escolhido.
            if (_level >= 100) return 0;

            if (_level != _gradientLevel ||
                (DateTime.UtcNow - _gradientStamp).TotalSeconds >= 5)
            {
                _gradientLevel = _level;
                _gradientStamp = DateTime.UtcNow;
                _gradientColor = BuildGradient(_level);
            }
            return _gradientColor;
        }

        private static int BuildGradient(int level)
        {
            int alpha = (100 - level) * 255 / 100;
            if (alpha <= 0) return 0;

            // Aproxima o fundo do Explorador no tema atual.
            bool dark = Native.IsSystemDarkMode();
            int tone = dark ? 32 : 243;
            return (alpha << 24) | (tone << 16) | (tone << 8) | tone;
        }

        private static void RestoreAll()
        {
            try
            {
                EnumWindows(delegate(IntPtr hwnd, IntPtr lParam)
                {
                    if (!IsWindowVisible(hwnd)) return true;
                    if (!IsExplorerWindowCached(hwnd)) return true;

                    ApplyAccent(hwnd, AccentDisabled, 0, 0);
                    ExtendFrame(hwnd, 0);
                    return true;
                }, IntPtr.Zero);
            }
            catch
            {
            }
        }

        private static void ApplyAccent(IntPtr hwnd, int state, int flags, int gradient)
        {
            AccentPolicy policy = new AccentPolicy();
            policy.AccentState = state;
            policy.AccentFlags = flags;
            policy.GradientColor = gradient;
            policy.AnimationId = 0;

            int size = Marshal.SizeOf(typeof(AccentPolicy));
            IntPtr memory = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(policy, memory, false);

                WindowCompositionAttributeData data = new WindowCompositionAttributeData();
                data.Attribute = WcaAccentPolicy;
                data.Data = memory;
                data.SizeOfData = size;
                SetWindowCompositionAttribute(hwnd, ref data);
            }
            finally
            {
                Marshal.FreeHGlobal(memory);
            }
        }

        private static void ExtendFrame(IntPtr hwnd, int value)
        {
            Margins margins = new Margins();
            margins.Left = value;
            margins.Right = value;
            margins.Top = value;
            margins.Bottom = value;
            DwmExtendFrameIntoClientArea(hwnd, ref margins);
        }
    }
}
