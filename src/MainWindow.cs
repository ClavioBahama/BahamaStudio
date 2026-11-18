using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shell;
using System.Windows.Threading;

namespace BahamaStudio
{
    public sealed class MainWindow : Window
    {
        public AppSettings Settings;
        public ToastHost Toasts;
        public BusyOverlay Busy;
        public DialogHost Dialog;

        private readonly Dictionary<string, PageBase> _pages = new Dictionary<string, PageBase>();
        private readonly Dictionary<string, RadioButton> _navButtons = new Dictionary<string, RadioButton>();
        private readonly string[] _keys = { "dashboard", "aparencia", "instalar", "diagnostico", "emergencia" };

        private readonly ContentControl _content;
        private readonly TextBlock _statusEngine;
        private readonly Border _statusEngineDot;
        private readonly TextBlock _statusExplorer;
        private readonly Border _statusExplorerDot;
        private readonly TextBlock _maxGlyph;
        private readonly DispatcherTimer _timer;
        private TrayIcon _tray;
        private bool _suppressNav;
        private string _current = "";
        private bool _ready;

        public MainWindow()
        {
            Settings = AppSettings.Load();
            _tray = new TrayIcon(this);

            Title = "Bahama Studio";
            Width = 1180;
            Height = 790;
            MinWidth = 1040;
            MinHeight = 700;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.CanResize;
            Background = Ui.SB("#070B12");
            FontFamily = Theme.GetFont("FontUi");
            TextOptions.SetTextRenderingMode(this, TextRenderingMode.ClearType);
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);

            WindowChrome chrome = new WindowChrome();
            chrome.CaptionHeight = 46;
            chrome.ResizeBorderThickness = new Thickness(6);
            chrome.CornerRadius = new CornerRadius(0);
            chrome.GlassFrameThickness = new Thickness(0);
            chrome.UseAeroCaptionButtons = false;
            WindowChrome.SetWindowChrome(this, chrome);

            Toasts = new ToastHost();
            Busy = new BusyOverlay();
            Dialog = new DialogHost();
            _content = new ContentControl();
            _content.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            _content.VerticalContentAlignment = VerticalAlignment.Stretch;

            _statusEngine = new TextBlock();
            _statusEngine.FontSize = 10.5;
            _statusEngine.Foreground = Theme.GetBrush("BrushMuted");
            _statusEngine.VerticalAlignment = VerticalAlignment.Center;

            _statusEngineDot = new Border();
            _statusEngineDot.Width = 6;
            _statusEngineDot.Height = 6;
            _statusEngineDot.CornerRadius = new CornerRadius(3);
            _statusEngineDot.Background = Theme.GetBrush("BrushMuted");
            _statusEngineDot.VerticalAlignment = VerticalAlignment.Center;
            _statusEngineDot.Margin = new Thickness(0, 0, 6, 0);

            _statusExplorer = new TextBlock();
            _statusExplorer.FontSize = 10.5;
            _statusExplorer.Foreground = Theme.GetBrush("BrushMuted");
            _statusExplorer.VerticalAlignment = VerticalAlignment.Center;

            _statusExplorerDot = new Border();
            _statusExplorerDot.Width = 6;
            _statusExplorerDot.Height = 6;
            _statusExplorerDot.CornerRadius = new CornerRadius(3);
            _statusExplorerDot.Background = Theme.GetBrush("BrushMuted");
            _statusExplorerDot.VerticalAlignment = VerticalAlignment.Center;
            _statusExplorerDot.Margin = new Thickness(0, 0, 6, 0);

            _maxGlyph = new TextBlock();
            _maxGlyph.Text = Glyphs.Maximize;

            Grid root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(46) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28) });

            FrameworkElement titleBar = BuildTitleBar();
            Grid.SetRow(titleBar, 0);
            root.Children.Add(titleBar);

            FrameworkElement body = BuildBody();
            Grid.SetRow(body, 1);
            root.Children.Add(body);

            FrameworkElement statusBar = BuildStatusBar();
            Grid.SetRow(statusBar, 2);
            root.Children.Add(statusBar);

            Grid.SetRowSpan(Busy, 3);
            Grid.SetRowSpan(Dialog, 3);
            Grid.SetRowSpan(Toasts, 3);
            root.Children.Add(Busy);
            root.Children.Add(Dialog);
            root.Children.Add(Toasts);

            Content = root;

            SourceInitialized += delegate
            {
                try
                {
                    IntPtr handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                    Native.ApplyWindowChrome(handle);
                }
                catch { }
            };

            StateChanged += delegate { UpdateMaximizeGlyph(); };

            PreviewKeyDown += WindowKeyDown;

            Loaded += delegate
            {
                _ready = true;
                string start = Settings.LastPage;
                bool valid = false;
                for (int i = 0; i < _keys.Length; i++) if (_keys[i] == start) valid = true;
                Navigate(valid ? start : "dashboard");

                // LoadActiveConfig calcula SHA-256, lista processos e consulta eventos.
                // Executar em segundo plano para não bloquear a interface na abertura.
                ThreadPool.QueueUserWorkItem(delegate(object state)
                {
                    EngineConfig activeConfig = EngineManager.LoadActiveConfig();
                    if (!ExplorerTransparency.IsSupported) return;
                    if (!activeConfig.TotalTransparency) return;

                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        ExplorerTransparency.SetEnabled(true, activeConfig.TotalTransparencyLevel);
                    }));
                });
            };

            Closing += delegate(object sender, System.ComponentModel.CancelEventArgs e)
            {
                if (Busy.Visibility == Visibility.Visible && !_forceClose)
                {
                    e.Cancel = true;
                    Toast("Aguarde a operação terminar antes de fechar.", "warning");
                    return;
                }
                Settings.Save();

                // Fechar a janela deixa a app residente na bandeja e mantém o
                // reaplicador ativo. "Sair" (menu da bandeja) encerra de vez.
                if (!_forceClose)
                {
                    e.Cancel = true;
                    Hide();
                    if (_tray != null) _tray.Refresh();
                    return;
                }

                ExplorerTransparency.Stop();
                if (_tray != null)
                {
                    _tray.Dispose();
                    _tray = null;
                }
            };

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(20);
            _timer.Tick += delegate { RefreshShellStatus(); };
            _timer.Start();
        }

        private bool _forceClose;

        public string CurrentPage
        {
            get { return _current; }
        }

        // ----------------------------- Shell -----------------------------

        private FrameworkElement BuildTitleBar()
        {
            Border bar = new Border();
            bar.Background = Ui.SB("#080D16");
            bar.BorderBrush = Ui.SB("#152136");
            bar.BorderThickness = new Thickness(0, 0, 0, 1);

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel left = new StackPanel();
            left.Orientation = Orientation.Horizontal;
            left.Margin = new Thickness(14, 0, 0, 0);
            left.VerticalAlignment = VerticalAlignment.Center;

            Border mark = new Border();
            mark.Width = 26;
            mark.Height = 26;
            mark.CornerRadius = new CornerRadius(8);
            mark.Background = Theme.GetBrush("BrushAccentGradient");
            mark.VerticalAlignment = VerticalAlignment.Center;
            mark.Child = Ui.Icon(Glyphs.Folder, 12.5, Ui.SB("#FFFFFF"));
            left.Children.Add(mark);

            StackPanel titles = new StackPanel();
            titles.VerticalAlignment = VerticalAlignment.Center;
            titles.Margin = new Thickness(9, 0, 0, 0);
            TextBlock name = new TextBlock();
            name.Text = "Bahama Studio";
            name.FontSize = 12.5;
            name.FontWeight = FontWeights.SemiBold;
            name.Foreground = Theme.GetBrush("BrushText");
            titles.Children.Add(name);
            TextBlock sub = new TextBlock();
            sub.Text = "GERENCIADOR DO EXPLORER";
            sub.FontSize = 7.5;
            sub.FontWeight = FontWeights.SemiBold;
            sub.Foreground = Theme.GetBrush("BrushDim");
            sub.Margin = new Thickness(0, 1, 0, 0);
            titles.Children.Add(sub);
            left.Children.Add(titles);

            Border chip = Ui.Chip("ENGINE 2.1.0");
            chip.Margin = new Thickness(14, 0, 0, 0);
            chip.VerticalAlignment = VerticalAlignment.Center;
            left.Children.Add(chip);

            Grid.SetColumn(left, 0);
            grid.Children.Add(left);

            StackPanel buttons = new StackPanel();
            buttons.Orientation = Orientation.Horizontal;
            buttons.VerticalAlignment = VerticalAlignment.Center;
            buttons.Margin = new Thickness(0, 0, 7, 0);

            Button about = WindowButton(Glyphs.Settings, "Sobre o Bahama Studio (Ctrl+,)", ShowAbout);
            about.Margin = new Thickness(0, 0, 6, 0);
            buttons.Children.Add(about);

            Button minimize = WindowButton(Glyphs.Minimize, "Minimizar", delegate { WindowState = WindowState.Minimized; });
            buttons.Children.Add(minimize);

            Button maximize = WindowButton(Glyphs.Maximize, "Maximizar", delegate { ToggleMaximize(); });
            maximize.Content = _maxGlyph;
            maximize.Margin = new Thickness(2, 0, 2, 0);
            buttons.Children.Add(maximize);

            Button close = new Button();
            close.Style = Theme.GetStyle("BtnWindowClose");
            close.Content = Glyphs.Close;
            close.ToolTip = "Fechar";
            System.Windows.Automation.AutomationProperties.SetName(close, "Fechar");
            close.Click += delegate { Close(); };
            WindowChrome.SetIsHitTestVisibleInChrome(close, true);
            buttons.Children.Add(close);

            Grid.SetColumn(buttons, 2);
            grid.Children.Add(buttons);

            bar.Child = grid;
            return bar;
        }

        private Button WindowButton(string glyph, string tip, RoutedEventHandler handler)
        {
            Button button = new Button();
            button.Style = Theme.GetStyle("BtnWindow");
            button.Content = glyph;
            button.ToolTip = tip;
            System.Windows.Automation.AutomationProperties.SetName(button, tip);
            button.Click += handler;
            WindowChrome.SetIsHitTestVisibleInChrome(button, true);
            return button;
        }

        private FrameworkElement BuildBody()
        {
            Grid body = new Grid();
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(226) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            FrameworkElement sidebar = BuildSidebar();
            Grid.SetColumn(sidebar, 0);
            body.Children.Add(sidebar);

            Grid canvas = new Grid();
            canvas.Background = Ui.SB("#090E18");

            Border topGlow = new Border();
            topGlow.Background = new RadialGradientBrush(
                Color.FromArgb(40, 110, 168, 255),
                Color.FromArgb(0, 110, 168, 255));
            topGlow.HorizontalAlignment = HorizontalAlignment.Right;
            topGlow.VerticalAlignment = VerticalAlignment.Top;
            topGlow.Width = 620;
            topGlow.Height = 420;
            topGlow.Margin = new Thickness(0, -180, -160, 0);
            topGlow.IsHitTestVisible = false;
            canvas.Children.Add(topGlow);

            Border bottomGlow = new Border();
            bottomGlow.Background = new RadialGradientBrush(
                Color.FromArgb(26, 167, 139, 250),
                Color.FromArgb(0, 167, 139, 250));
            bottomGlow.HorizontalAlignment = HorizontalAlignment.Left;
            bottomGlow.VerticalAlignment = VerticalAlignment.Bottom;
            bottomGlow.Width = 560;
            bottomGlow.Height = 400;
            bottomGlow.Margin = new Thickness(-220, 0, 0, -200);
            bottomGlow.IsHitTestVisible = false;
            canvas.Children.Add(bottomGlow);

            canvas.Children.Add(_content);

            Grid.SetColumn(canvas, 1);
            body.Children.Add(canvas);
            return body;
        }

        private FrameworkElement BuildSidebar()
        {
            Border sidebar = new Border();
            sidebar.Background = Ui.SB("#080D17");
            sidebar.BorderBrush = Ui.SB("#152136");
            sidebar.BorderThickness = new Thickness(0, 0, 1, 0);

            Grid layout = new Grid();
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            TextBlock menu = Ui.Label("MENU");
            menu.FontSize = 8.5;
            menu.Margin = new Thickness(20, 16, 0, 6);
            Grid.SetRow(menu, 0);
            layout.Children.Add(menu);

            StackPanel nav = new StackPanel();
            nav.Margin = new Thickness(0, 0, 0, 8);
            AddNav(nav, "dashboard", Glyphs.Home, "Visão geral");
            AddNav(nav, "aparencia", Glyphs.Palette, "Aparência");
            AddNav(nav, "instalar", Glyphs.Download, "Instalar");
            AddNav(nav, "diagnostico", Glyphs.Diagnostic, "Diagnóstico");
            AddNav(nav, "emergencia", Glyphs.Shield, "Emergência");
            Grid.SetRow(nav, 1);
            layout.Children.Add(nav);

            Border footer = BuildSidebarFooter();
            footer.Margin = new Thickness(12, 0, 12, 12);
            Grid.SetRow(footer, 2);
            layout.Children.Add(footer);

            sidebar.Child = layout;
            return sidebar;
        }

        private void AddNav(Panel parent, string key, string glyph, string label)
        {
            RadioButton button = new RadioButton();
            button.Style = Theme.GetStyle("NavItem");
            button.GroupName = "nav";
            button.Tag = key;

            StackPanel content = new StackPanel();
            content.Orientation = Orientation.Horizontal;

            TextBlock icon = Ui.Icon(glyph, 15);
            icon.Margin = new Thickness(0, 0, 10, 0);
            icon.SetBinding(TextBlock.ForegroundProperty,
                new System.Windows.Data.Binding("Foreground") { Source = button });
            content.Children.Add(icon);

            TextBlock text = new TextBlock();
            text.Text = label;
            text.FontSize = 12.5;
            text.FontWeight = FontWeights.SemiBold;
            text.VerticalAlignment = VerticalAlignment.Center;
            text.SetBinding(TextBlock.ForegroundProperty,
                new System.Windows.Data.Binding("Foreground") { Source = button });
            content.Children.Add(text);

            button.Content = content;
            System.Windows.Automation.AutomationProperties.SetName(button, label);
            button.Checked += delegate
            {
                if (_suppressNav || !_ready) return;
                Navigate(key);
            };

            _navButtons[key] = button;
            parent.Children.Add(button);
        }

        private Border BuildSidebarFooter()
        {
            StackPanel content = new StackPanel();

            Grid engineRow = new Grid();
            engineRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            engineRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Border dot = new Border();
            dot.Width = 7;
            dot.Height = 7;
            dot.CornerRadius = new CornerRadius(4);
            dot.Background = Theme.GetBrush("BrushMuted");
            dot.VerticalAlignment = VerticalAlignment.Center;
            TextBlock engine = new TextBlock();
            engine.Text = "Verificando engine...";
            engine.FontSize = 10;
            engine.Foreground = Theme.GetBrush("BrushMuted");
            engine.Margin = new Thickness(7, 0, 0, 0);
            engine.TextTrimming = TextTrimming.CharacterEllipsis;
            Grid.SetColumn(dot, 0);
            engineRow.Children.Add(dot);
            Grid.SetColumn(engine, 1);
            engineRow.Children.Add(engine);
            content.Children.Add(engineRow);

            Grid explorerRow = new Grid();
            explorerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            explorerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Border edot = new Border();
            edot.Width = 7;
            edot.Height = 7;
            edot.CornerRadius = new CornerRadius(4);
            edot.Background = Theme.GetBrush("BrushMuted");
            edot.VerticalAlignment = VerticalAlignment.Center;
            TextBlock explorer = new TextBlock();
            explorer.Text = "Verificando Explorador...";
            explorer.FontSize = 10;
            explorer.Foreground = Theme.GetBrush("BrushMuted");
            explorer.Margin = new Thickness(7, 0, 0, 0);
            explorer.TextTrimming = TextTrimming.CharacterEllipsis;
            Grid.SetColumn(edot, 0);
            explorerRow.Children.Add(edot);
            Grid.SetColumn(explorer, 1);
            explorerRow.Children.Add(explorer);
            explorerRow.Margin = new Thickness(0, 6, 0, 0);
            content.Children.Add(explorerRow);

            Border card = new Border();
            card.Style = Theme.GetStyle("CardSoft");
            card.Padding = new Thickness(12, 10, 12, 10);
            card.Child = content;

            _footerEngine = engine;
            _footerEngineDot = dot;
            _footerExplorer = explorer;
            _footerExplorerDot = edot;
            return card;
        }

        private TextBlock _footerEngine;
        private Border _footerEngineDot;
        private TextBlock _footerExplorer;
        private Border _footerExplorerDot;

        private FrameworkElement BuildStatusBar()
        {
            Border bar = new Border();
            bar.Background = Ui.SB("#080D16");
            bar.BorderBrush = Ui.SB("#152136");
            bar.BorderThickness = new Thickness(0, 1, 0, 0);

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel left = new StackPanel();
            left.Orientation = Orientation.Horizontal;
            left.Margin = new Thickness(14, 0, 0, 0);
            left.VerticalAlignment = VerticalAlignment.Center;
            left.Children.Add(_statusEngineDot);
            left.Children.Add(_statusEngine);

            Border divider = new Border();
            divider.Width = 1;
            divider.Height = 12;
            divider.Background = Ui.SB("#1F2C44");
            divider.Margin = new Thickness(12, 0, 12, 0);
            divider.VerticalAlignment = VerticalAlignment.Center;
            left.Children.Add(divider);
            left.Children.Add(_statusExplorerDot);
            left.Children.Add(_statusExplorer);

            Grid.SetColumn(left, 0);
            grid.Children.Add(left);

            TextBlock right = new TextBlock();
            right.Text = "Bahama Studio 1.0  •  Criado por Clávio Bahama  •  engine de Maplespe (LGPLv3)";
            right.FontSize = 9.5;
            right.Foreground = Theme.GetBrush("BrushDim");
            right.VerticalAlignment = VerticalAlignment.Center;
            right.Margin = new Thickness(0, 0, 16, 0);
            Grid.SetColumn(right, 1);
            grid.Children.Add(right);

            bar.Child = grid;
            return bar;
        }

        // ----------------------------- Behavior -----------------------------

        public void Toast(string message, string kind)
        {
            if (Toasts != null) Toasts.Show(message, kind);
        }

        public void Navigate(string key)
        {
            if (string.IsNullOrEmpty(key)) key = "dashboard";
            bool valid = false;
            for (int i = 0; i < _keys.Length; i++) if (_keys[i] == key) valid = true;
            if (!valid) key = "dashboard";

            bool same = key == _current;
            _current = key;

            PageBase page;
            if (!_pages.TryGetValue(key, out page))
            {
                page = CreatePage(key);
                page.Host = this;
                _pages[key] = page;
            }

            _suppressNav = true;
            RadioButton nav;
            if (_navButtons.TryGetValue(key, out nav)) nav.IsChecked = true;
            _suppressNav = false;

            _content.Content = page;
            if (!same) AnimatePage(page);

            page.Refresh();

            try
            {
                Settings.LastPage = key;
                Settings.Save();
            }
            catch { }

            RefreshShellStatus();
        }

        public void ApplyMachineRecommendation(EngineConfig config)
        {
            if (config == null) return;
            PageBase page;
            if (!_pages.TryGetValue("aparencia", out page))
            {
                page = CreatePage("aparencia");
                page.Host = this;
                _pages["aparencia"] = page;
            }
            AppearancePage appearance = page as AppearancePage;
            if (appearance == null) return;
            appearance.ApplyRecommended(config);
            Navigate("aparencia");
            Toast("Perfil da máquina aplicado. Revise e clique em Aplicar.", "success");
        }

        private PageBase CreatePage(string key)
        {
            switch (key)
            {
                case "aparencia": return new AppearancePage();
                case "instalar": return new InstallPage();
                case "diagnostico": return new DiagnosticsPage();
                case "emergencia": return new EmergencyPage();
                default: return new DashboardPage();
            }
        }

        private void AnimatePage(PageBase page)
        {
            page.Opacity = 0;
            TranslateTransform move = new TranslateTransform(12, 0);
            page.RenderTransform = move;
            page.BeginAnimation(UIElement.OpacityProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)));
            move.BeginAnimation(TranslateTransform.XProperty,
                new DoubleAnimation(12, 0, TimeSpan.FromMilliseconds(220))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                });
        }

        public void RefreshShellStatus()
        {
            try
            {
                bool running = Native.IsExplorerRunning();
                Color success = Theme.GetColor("ColorSuccess");
                Color warning = Theme.GetColor("ColorWarning");

                _statusExplorerDot.Background = running ? Ui.SB(success) : Ui.SB(warning);
                _statusExplorer.Text = running ? "Explorador em execução" : "Explorador fechado";
                if (_footerExplorerDot != null)
                {
                    _footerExplorerDot.Background = running ? Ui.SB(success) : Ui.SB(warning);
                    _footerExplorer.Text = running ? "Explorador em execução" : "Explorador fechado";
                    _footerExplorer.Foreground = running ? Ui.SB(success) : Ui.SB(warning);
                }

                Native.EngineRegistration registration = Native.FindEngineRegistration();
                bool installed = registration.HasClsid && registration.HasFolderExtension &&
                                 !string.IsNullOrEmpty(registration.DllPath) && File.Exists(registration.DllPath);
                string version = installed ? Native.GetFileVersion(registration.DllPath) : "";

                _statusEngine.Text = installed
                    ? "Engine " + (string.IsNullOrEmpty(version) ? "instalada" : "v" + version) + "  •  LGPLv3"
                    : "Engine não instalada";
                _statusEngineDot.Background = installed ? Ui.SB(success) : Ui.SB(warning);
                if (_footerEngineDot != null)
                {
                    _footerEngineDot.Background = installed ? Ui.SB(success) : Ui.SB(warning);
                    _footerEngine.Text = installed
                        ? "Engine v" + (string.IsNullOrEmpty(version) ? "?" : version) + " ativa"
                        : "Engine não instalada";
                    _footerEngine.Foreground = installed ? Ui.SB(success) : Ui.SB(warning);
                }
            }
            catch { }
        }

        private void WindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && Dialog.IsOpen)
            {
                Dialog.Close();
                e.Handled = true;
                return;
            }

            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.D1 || e.Key == Key.NumPad1) { Navigate("dashboard"); e.Handled = true; }
                else if (e.Key == Key.D2 || e.Key == Key.NumPad2) { Navigate("aparencia"); e.Handled = true; }
                else if (e.Key == Key.D3 || e.Key == Key.NumPad3) { Navigate("instalar"); e.Handled = true; }
                else if (e.Key == Key.D4 || e.Key == Key.NumPad4) { Navigate("diagnostico"); e.Handled = true; }
                else if (e.Key == Key.D5 || e.Key == Key.NumPad5) { Navigate("emergencia"); e.Handled = true; }
                else if (e.Key == Key.OemComma) { ShowAbout(this, null); e.Handled = true; }
                return;
            }

            if (e.Key == Key.F5)
            {
                PageBase page;
                if (_pages.TryGetValue(_current, out page)) page.Refresh();
                RefreshShellStatus();
                Toast("Dados atualizados.", "info");
                e.Handled = true;
            }
        }

        private void ToggleMaximize()
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            UpdateMaximizeGlyph();
        }

        private void UpdateMaximizeGlyph()
        {
            _maxGlyph.Text = WindowState == WindowState.Maximized ? Glyphs.Restore : Glyphs.Maximize;
        }

        private void ShowAbout(object sender, RoutedEventArgs e)
        {
            string message = "Bahama Studio 1.0\n\n" +
                             "Criado por Clávio Bahama.\n\n" +
                             "Gerenciador independente para o ExplorerBlurMica 2.1.0, criado por Maplespe e licenciado em LGPLv3.\n\n" +
                             "A engine incluída em engine\\ExplorerBlurMica.dll é o projeto oficial. " +
                             "Este aplicativo não altera o código da engine e não envia nenhum dado pela internet.\n\n" +
                             "Dados locais: " + Paths.DataDir + "\n" +
                             "Log: " + Paths.LogFile + "\n" +
                             "Backups: " + Paths.BackupsDir + "\n\n" +
                             "Atalhos: Ctrl+1 a Ctrl+5 para navegar, F5 para atualizar, Esc para fechar diálogos.";

            Dialog.Confirm("Sobre o Bahama Studio", message, "Abrir pasta de dados", false, delegate
            {
                Native.OpenFolder(Paths.DataDir);
            });
        }

        public void ShowWindow()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        public void ExitApplication()
        {
            ExplorerTransparency.Stop();
            ForceClose();
        }

        public void ForceClose()
        {
            _forceClose = true;
            Close();
        }
    }
}
