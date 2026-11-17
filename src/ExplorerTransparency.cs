using System;
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
        private static IntPtr _hookMinimizeEnd = IntPtr.Zero;
        private static IntPtr _hookShow = IntPtr.Zero;
        private static IntPtr _hookLocation = IntPtr.Zero;
        private static DispatcherTimer _reapplyTimer;
        private static DispatcherTimer _watchdogTimer;
        private static int _applyCount;
        private static int _lastLoggedCount = -1;
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
                _watchdogTimer.Interval = TimeSpan.FromMilliseconds(1200);
                _watchdogTimer.Tick += delegate { ApplyAll(); };
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

            _hookForeground = SetWinEventHook(EventSystemForeground, EventSystemForeground,
                IntPtr.Zero, HookProc, 0, 0, flags);
            _hookMinimizeEnd = SetWinEventHook(EventSystemMinimizeEnd, EventSystemMinimizeEnd,
                IntPtr.Zero, HookProc, 0, 0, flags);
            _hookShow = SetWinEventHook(EventObjectShow, EventObjectShow,
                IntPtr.Zero, HookProc, 0, 0, flags);
            _hookLocation = SetWinEventHook(EventObjectLocationChange, EventObjectLocationChange,
                IntPtr.Zero, HookProc, 0, 0, flags);
        }

        private static void UninstallHooks()
        {
            if (_hookForeground != IntPtr.Zero)
            {
                UnhookWinEvent(_hookForeground);
                _hookForeground = IntPtr.Zero;
            }
            if (_hookMinimizeEnd != IntPtr.Zero)
            {
                UnhookWinEvent(_hookMinimizeEnd);
                _hookMinimizeEnd = IntPtr.Zero;
            }
            if (_hookShow != IntPtr.Zero)
            {
                UnhookWinEvent(_hookShow);
                _hookShow = IntPtr.Zero;
            }
            if (_hookLocation != IntPtr.Zero)
            {
                UnhookWinEvent(_hookLocation);
                _hookLocation = IntPtr.Zero;
            }
        }

        private static void OnWinEvent(IntPtr hook, uint eventType, IntPtr hwnd,
            int idObject, int idChild, uint eventThread, uint eventTime)
        {
            if (!_active) return;

            // EVENT_OBJECT_LOCATIONCHANGE também chega para controles internos.
            if (eventType == EventObjectLocationChange && idObject != ObjidWindow) return;

            bool isExplorer = hwnd != IntPtr.Zero && IsExplorerWindow(hwnd);

            if (isExplorer && (eventType == EventSystemForeground ||
                               eventType == EventSystemMinimizeEnd ||
                               eventType == EventObjectShow))
            {
                ApplyToWindow(hwnd);
            }

            // Ao perder o foco, o novo foreground pertence a outra janela:
            // reaplicar em todas garante que o Explorador não volte ao fundo padrão.
            if (eventType == EventSystemForeground || isExplorer)
                QueueReapply();
        }

        private static void QueueReapply()
        {
            if (!_active || _reapplyTimer == null) return;
            if (!_reapplyTimer.IsEnabled) _reapplyTimer.Start();
        }

        private static bool OnEnumWindow(IntPtr hwnd, IntPtr lParam)
        {
            if (!_active) return false;
            if (!IsWindowVisible(hwnd)) return true;
            if (!IsExplorerWindow(hwnd)) return true;

            ApplyToWindow(hwnd);
            _applyCount++;
            return true;
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
            if (!_active) return;

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

        private static void ApplyToWindow(IntPtr hwnd)
        {
            if (!_active) return;
            if (!IsWindowVisible(hwnd)) return;
            if (!IsExplorerWindow(hwnd)) return;

            ApplyAccent(hwnd, AccentEnableTransparentGradient, 2, CurrentGradient());
            ExtendFrame(hwnd, -1);
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
                    if (!IsExplorerWindow(hwnd)) return true;

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
