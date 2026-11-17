using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BahamaStudio
{
    public sealed class DiagnosticsPage : PageBase
    {
        private readonly StackPanel _systemPanel = new StackPanel();
        private readonly StackPanel _enginePanel = new StackPanel();
        private readonly StackPanel _explorerPanel = new StackPanel();
        private readonly StackPanel _stabilityPanel = new StackPanel();
        private readonly StackPanel _checksPanel = new StackPanel();
        private readonly StackPanel _crashesPanel = new StackPanel();

        private readonly Border _healthCard;
        private readonly Border _healthBadge;
        private readonly TextBlock _healthIcon;
        private readonly TextBlock _healthTitle;
        private readonly TextBlock _healthBody;
        private readonly TextBlock _healthFoot;

        private EngineStatus _status;
        private List<Native.CrashInfo> _crashes = new List<Native.CrashInfo>();

        public DiagnosticsPage()
        {
            _healthIcon = Ui.Icon(Glyphs.Diagnostic, 18, Theme.GetBrush("BrushAccent"));
            _healthBadge = new Border();
            _healthBadge.Width = 46;
            _healthBadge.Height = 46;
            _healthBadge.CornerRadius = new CornerRadius(13);
            _healthBadge.Background = Ui.Alpha(Theme.GetColor("ColorAccent"), 28);
            _healthBadge.Child = _healthIcon;
            _healthBadge.VerticalAlignment = VerticalAlignment.Top;

            _healthTitle = Ui.Text("Analisando...", "TextH2");
            _healthBody = Ui.Body("");
            _healthBody.Margin = new Thickness(0, 4, 0, 0);
            _healthBody.MaxWidth = 520;

            _healthFoot = Ui.Caption("");
            _healthFoot.FontSize = 10.5;
            _healthFoot.Margin = new Thickness(0, 6, 0, 0);

            StackPanel healthText = new StackPanel();
            healthText.VerticalAlignment = VerticalAlignment.Center;
            healthText.Margin = new Thickness(15, 0, 0, 0);
            healthText.Children.Add(_healthTitle);
            healthText.Children.Add(_healthBody);
            healthText.Children.Add(_healthFoot);

            Grid healthGrid = new Grid();
            healthGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            healthGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            healthGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(_healthBadge, 0);
            healthGrid.Children.Add(_healthBadge);
            Grid.SetColumn(healthText, 1);
            healthGrid.Children.Add(healthText);

            StackPanel healthActions = new StackPanel();
            healthActions.VerticalAlignment = VerticalAlignment.Center;
            Button rescan = Ui.Btn("Reanalisar", Glyphs.Refresh, "BtnSecondary", delegate { Refresh(); });
            healthActions.Children.Add(rescan);
            Grid.SetColumn(healthActions, 2);
            healthGrid.Children.Add(healthActions);

            _healthCard = new Border();
            _healthCard.Style = Theme.GetStyle("Card");
            _healthCard.Padding = new Thickness(20);
            _healthCard.Child = healthGrid;

            Children.Add(Build());
            Refresh();
        }

        private FrameworkElement Build()
        {
            Grid root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Button logs = Ui.Btn("Abrir log", Glyphs.Document, "BtnGhost", delegate { OpenLog(); });
            FrameworkElement header = Ui.PageHeader("Diagnóstico", "O que está acontecendo de verdade",
                "Uma leitura direta do registro, do config.ini e dos módulos carregados no Explorer. Sem achismo.",
                logs);
            header.Margin = new Thickness(34, 26, 34, 18);
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            StackPanel content = new StackPanel();
            content.Margin = new Thickness(34, 0, 34, 28);

            content.Children.Add(_healthCard);
            Ui.AddMargin(_healthCard, 0, 0, 0, 18);

            Grid columns = Ui.Columns(Ui.Star(1), Ui.Px(16), Ui.Star(1));
            content.Children.Add(columns);

            StackPanel left = new StackPanel();
            left.Children.Add(BuildCard("Sistema", Glyphs.Monitor, _systemPanel));
            Ui.AddMargin(left.Children[left.Children.Count - 1], 0, 0, 0, 16);
            left.Children.Add(BuildCard("Engine", Glyphs.Download, _enginePanel));
            Grid.SetColumn(left, 0);
            columns.Children.Add(left);

            StackPanel right = new StackPanel();
            right.Children.Add(BuildCard("Explorador", Glyphs.Folder, _explorerPanel));
            Ui.AddMargin(right.Children[right.Children.Count - 1], 0, 0, 0, 16);
            right.Children.Add(BuildCard("Estabilidade", Glyphs.Bug, _stabilityPanel));
            Grid.SetColumn(right, 2);
            columns.Children.Add(right);

            Border checksCard = BuildCard("Verificações", Glyphs.Check, _checksPanel);
            checksCard.Margin = new Thickness(0, 18, 0, 0);
            content.Children.Add(checksCard);

            Border crashesCard = BuildCard("Falhas recentes do Explorer", Glyphs.Warning, _crashesPanel);
            crashesCard.Margin = new Thickness(0, 16, 0, 0);
            content.Children.Add(crashesCard);

            Border tools = BuildToolsCard();
            tools.Margin = new Thickness(0, 16, 0, 0);
            content.Children.Add(tools);

            ScrollViewer scroll = Ui.Scroll(content, 6);
            Grid.SetRow(scroll, 1);
            root.Children.Add(scroll);
            return root;
        }

        private Border BuildCard(string title, string glyph, Panel body)
        {
            Border badge = new Border();
            badge.Width = 28;
            badge.Height = 28;
            badge.CornerRadius = new CornerRadius(8);
            badge.Background = Ui.Alpha(Theme.GetColor("ColorAccent"), 20);
            badge.Child = Ui.Icon(glyph, 12.5, Theme.GetBrush("BrushAccent"));

            StackPanel header = new StackPanel();
            header.Orientation = Orientation.Horizontal;
            header.Children.Add(badge);
            TextBlock heading = Ui.Text(title, "TextBodyBright");
            heading.FontSize = 12.5;
            heading.FontWeight = FontWeights.SemiBold;
            heading.Margin = new Thickness(9, 0, 0, 0);
            heading.VerticalAlignment = VerticalAlignment.Center;
            header.Children.Add(heading);

            StackPanel content = new StackPanel();
            content.Children.Add(header);
            Border divider = Ui.Divider(13, 13);
            content.Children.Add(divider);
            content.Children.Add(body);
            return Ui.Card(content);
        }

        private Border BuildToolsCard()
        {
            StackPanel content = new StackPanel();
            content.Children.Add(Ui.SectionTitle("Manutenção",
                "Ferramentas que ajudam a investigar sem precisar abrir o Editor do Registro."));

            WrapPanel buttons = new WrapPanel();
            buttons.Children.Add(Tool("Detetar e adaptar ao PC", Glyphs.Monitor, delegate { AdaptToMachine(); }));
            buttons.Children.Add(Tool("Copiar relatório", Glyphs.Copy, delegate { CopyReport(); }));
            buttons.Children.Add(Tool("Abrir pasta de logs", Glyphs.Document, delegate { Native.OpenFolder(Paths.LogsDir); }));
            buttons.Children.Add(Tool("Abrir backups", Glyphs.Save, delegate { Native.OpenFolder(Paths.BackupsDir); }));
            buttons.Children.Add(Tool("Abrir CrashDumps", Glyphs.Warning, delegate
            {
                string path = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CrashDumps");
                if (System.IO.Directory.Exists(path)) Native.OpenFolder(path);
                else if (Host != null) Host.Toast("A pasta CrashDumps não existe neste usuário.", "info");
            }));
            buttons.Children.Add(Tool("Página de emergência", Glyphs.Shield, delegate
            {
                if (Host != null) Host.Navigate("emergencia");
            }));
            content.Children.Add(buttons);

            TextBlock note = Ui.Caption("Os relatórios são gerados localmente. Nada é enviado para a internet.");
            note.FontSize = 10.5;
            note.Margin = new Thickness(0, 8, 0, 0);
            content.Children.Add(note);
            return Ui.Card(content);
        }

        private Button Tool(string text, string glyph, RoutedEventHandler handler)
        {
            Button button = Ui.Btn(text, glyph, "BtnGhost", handler);
            button.Margin = new Thickness(0, 0, 8, 8);
            button.Height = 34;
            button.MinHeight = 0;
            button.Padding = new Thickness(12, 5, 12, 5);
            return button;
        }

        public override void Refresh()
        {
            EngineStatus status = EngineManager.Detect();
            _status = status;
            _crashes = Native.GetExplorerCrashes(7);

            Color success = Theme.GetColor("ColorSuccess");
            Color warning = Theme.GetColor("ColorWarning");
            Color danger = Theme.GetColor("ColorDanger");
            Color accent = Theme.GetColor("ColorAccent");

            int bad = 0;
            int warn = 0;
            if (!status.Windows.IsWindows11) warn++;
            if (!status.IsInstalled) bad++;
            else
            {
                if (!status.IsModern) bad++;
                if (!status.MatchesBundled) warn++;
                if (!status.ConfigExists) warn++;
                if (!status.ConfigWritable) warn++;
            }
            if (!status.ExplorerRunning) warn++;
            if (status.DumpCount > 0) warn++;

            Color healthColor = bad > 0 ? danger : (warn > 0 ? warning : success);
            string healthState = bad > 0 ? "Atenção necessária" : (warn > 0 ? "Funcional, com ressalvas" : "Tudo saudável");
            _healthIcon.Text = bad > 0 ? Glyphs.Warning : (warn > 0 ? Glyphs.Info : Glyphs.Check);
            _healthIcon.Foreground = Ui.SB(healthColor);
            _healthBadge.Background = Ui.Alpha(healthColor, 28);
            _healthTitle.Text = healthState;
            _healthBody.Text = bad > 0
                ? "Encontramos " + bad + (bad == 1 ? " ponto crítico" : " pontos críticos") + " que impedem o efeito de funcionar corretamente. Veja os detalhes abaixo."
                : (warn > 0
                    ? "A engine está funcional. Existem " + warn + (warn == 1 ? " aviso" : " avisos") + " que valem a sua atenção, mas nada bloqueante."
                    : "A engine está instalada, registrada e o Explorador está estável. Nenhum ajuste é necessário agora.");
            _healthFoot.Text = "Última verificação: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") +
                               "  •  " + status.Windows.Name + " build " + status.Windows.BuildLabel;
            _healthCard.BorderBrush = Ui.Alpha(healthColor, 70);

            BuildSystem(status, accent, success, warning);
            BuildEngine(status, success, warning, danger);
            BuildExplorer(status, success, warning);
            BuildStability(status, success, warning, danger);
            BuildChecks(status, success, warning, danger);
            BuildCrashes(success, danger);
        }

        private void BuildSystem(EngineStatus status, Color accent, Color success, Color warning)
        {
            MachineProfile profile = MachineProfile.Capture();
            _systemPanel.Children.Clear();
            _systemPanel.Children.Add(new FactRow("Windows", status.Windows.Name + " " + status.Windows.DisplayVersion));
            _systemPanel.Children.Add(new FactRow("Build", status.Windows.BuildLabel));
            _systemPanel.Children.Add(new FactRow("Arquitetura", profile.Architecture + " / processo " + profile.ProcessArchitecture));
            _systemPanel.Children.Add(new FactRow("Mica nativo", status.Windows.SupportsMica ? "disponível" : "indisponível",
                status.Windows.SupportsMica ? Theme.GetBrush("BrushSuccess") : Theme.GetBrush("BrushWarning")));
            _systemPanel.Children.Add(new FactRow("Processador", profile.Processor));
            _systemPanel.Children.Add(new FactRow("Memória RAM", profile.RamGb));
            _systemPanel.Children.Add(new FactRow("Ecrã", profile.Screen));
            _systemPanel.Children.Add(new FactRow("Permissões", Native.IsAdmin() ? "administrador" : "usuário padrão",
                Native.IsAdmin() ? Theme.GetBrush("BrushSuccess") : Theme.GetBrush("BrushText")));
            _systemPanel.Children.Add(new FactRow("Tema do Windows", profile.Theme));
            _systemPanel.Children.Add(new FactRow("Máquina", profile.ComputerName + " • " + profile.UserName));
            _systemPanel.Children.Add(new FactRow("Perfil", profile.Summary));
            if (!status.Windows.SupportsMica)
            {
                TextBlock note = Ui.Caption("Este Windows não tem Mica nativo. Use Acrílico ou Blur puro na página Aparência — a adaptação automática já escolhe um desses efeitos.");
                note.FontSize = 10.5;
                note.Foreground = Ui.SB(warning);
                note.Margin = new Thickness(0, 6, 0, 0);
                _systemPanel.Children.Add(note);
            }
        }

        private void BuildEngine(EngineStatus status, Color success, Color warning, Color danger)
        {
            _enginePanel.Children.Clear();
            _enginePanel.Children.Add(new FactRow("Estado", status.IsInstalled ? (status.IsModern ? "instalada" : "desatualizada") : "ausente",
                status.IsInstalled ? (status.IsModern ? Theme.GetBrush("BrushSuccess") : Theme.GetBrush("BrushWarning")) : Theme.GetBrush("BrushDanger")));
            _enginePanel.Children.Add(new FactRow("Versão", string.IsNullOrEmpty(status.InstalledVersion) ? "—" : "v" + status.InstalledVersion,
                status.IsModern ? Theme.GetBrush("BrushSuccess") : Theme.GetBrush("BrushWarning")));
            _enginePanel.Children.Add(new FactRow("Arquivo", status.IsInstalled ? Paths.Shorten(status.RegisteredPath, 46) : "não registrado"));
            _enginePanel.Children.Add(new FactRow("CLSID", status.HasClsid ? "registrado" : "ausente",
                status.HasClsid ? Theme.GetBrush("BrushSuccess") : Theme.GetBrush("BrushDanger")));
            _enginePanel.Children.Add(new FactRow("FolderExtension", status.HasFolderExtension ? "registrado" : "ausente",
                status.HasFolderExtension ? Theme.GetBrush("BrushSuccess") : Theme.GetBrush("BrushDanger")));
            _enginePanel.Children.Add(new FactRow("Config", status.ConfigExists ? Paths.Shorten(status.ConfigPath, 46) : "ausente",
                status.ConfigExists ? Theme.GetBrush("BrushText") : Theme.GetBrush("BrushWarning")));
            _enginePanel.Children.Add(new FactRow("Gravação", status.ConfigWritable ? "liberada" : "requer UAC",
                status.ConfigWritable ? Theme.GetBrush("BrushSuccess") : Theme.GetBrush("BrushWarning")));
            _enginePanel.Children.Add(new FactRow("Cópia incluída", status.BundledExists ? "v" + status.BundledVersion : "ausente",
                status.BundledExists ? Theme.GetBrush("BrushText") : Theme.GetBrush("BrushDanger")));
            _enginePanel.Children.Add(new FactRow("Integridade", status.MatchesBundled ? "idêntica à oficial" : (status.IsInstalled ? "diferente da incluída" : "—"),
                status.MatchesBundled ? Theme.GetBrush("BrushSuccess") : Theme.GetBrush("BrushWarning")));
            if (status.IsInstalled)
            {
                string hash = status.InstalledHash;
                if (!string.IsNullOrEmpty(hash) && hash.Length > 18) hash = hash.Substring(0, 18) + "...";
                _enginePanel.Children.Add(new FactRow("SHA-256", hash));
            }

            if (status.IsInstalled && !status.IsModern)
            {
                Button fix = Ui.Btn("Atualizar agora", Glyphs.Refresh, "BtnSecondary", delegate
                {
                    if (Host != null) Host.Navigate("instalar");
                });
                fix.Margin = new Thickness(0, 8, 0, 0);
                fix.HorizontalAlignment = HorizontalAlignment.Left;
                _enginePanel.Children.Add(fix);
            }
        }

        private void BuildExplorer(EngineStatus status, Color success, Color warning)
        {
            _explorerPanel.Children.Clear();
            _explorerPanel.Children.Add(new FactRow("Processo", status.ExplorerRunning ? "em execução" : "fechado",
                status.ExplorerRunning ? Theme.GetBrush("BrushSuccess") : Theme.GetBrush("BrushWarning")));
            _explorerPanel.Children.Add(new FactRow("Instâncias", status.ExplorerProcessCount.ToString()));
            _explorerPanel.Children.Add(new FactRow("Engine carregada", status.EngineLoaded ? "sim" : "não",
                status.EngineLoaded ? Theme.GetBrush("BrushSuccess") : Theme.GetBrush("BrushMuted")));
            _explorerPanel.Children.Add(new FactRow("Dumps (7 dias)", status.DumpCount.ToString(),
                status.DumpCount == 0 ? Theme.GetBrush("BrushSuccess") : Theme.GetBrush("BrushWarning")));

            if (status.ExplorerRunning)
            {
                List<Native.LoadedModule> modules = Native.GetExplorerModules();
                _explorerPanel.Children.Add(new FactRow("Módulos", modules.Count + " carregados"));
                int shown = 0;
                for (int i = 0; i < modules.Count && shown < 6; i++)
                {
                    string name = modules[i].Name;
                    if (string.IsNullOrEmpty(name)) continue;
                    bool isEngine = name.IndexOf("ExplorerBlurMica", StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!isEngine && shown >= 4) continue;
                    TextBlock line = Ui.Mono(name);
                    line.FontSize = 10;
                    line.Margin = new Thickness(0, 1, 0, 1);
                    if (isEngine) line.Foreground = Theme.GetBrush("BrushSuccess");
                    _explorerPanel.Children.Add(line);
                    shown++;
                }
            }

            StackPanel actions = new StackPanel();
            actions.Orientation = Orientation.Horizontal;
            actions.Margin = new Thickness(0, 10, 0, 0);
            Button restart = Ui.Btn("Reiniciar", Glyphs.Refresh, "BtnGhost", RestartExplorer);
            restart.Height = 32;
            restart.MinHeight = 0;
            restart.Padding = new Thickness(11, 4, 11, 4);
            actions.Children.Add(restart);
            Button open = Ui.Btn("Abrir pasta", Glyphs.Folder, "BtnGhost", delegate
            {
                Native.OpenFolder(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            });
            open.Height = 32;
            open.MinHeight = 0;
            open.Padding = new Thickness(11, 4, 11, 4);
            open.Margin = new Thickness(8, 0, 0, 0);
            actions.Children.Add(open);
            _explorerPanel.Children.Add(actions);
        }

        private void BuildStability(EngineStatus status, Color success, Color warning, Color danger)
        {
            _stabilityPanel.Children.Clear();
            List<Native.CrashInfo> recent = Native.GetExplorerCrashes(1);
            bool anyRecent = recent.Count > 0;

            _stabilityPanel.Children.Add(new FactRow("Falhas (24 h)", recent.Count.ToString(),
                anyRecent ? Theme.GetBrush("BrushDanger") : Theme.GetBrush("BrushSuccess")));
            _stabilityPanel.Children.Add(new FactRow("Falhas (7 dias)", _crashes.Count.ToString(),
                _crashes.Count == 0 ? Theme.GetBrush("BrushSuccess") : Theme.GetBrush("BrushWarning")));
            _stabilityPanel.Children.Add(new FactRow("Dumps", status.DumpCount.ToString(),
                status.DumpCount == 0 ? Theme.GetBrush("BrushSuccess") : Theme.GetBrush("BrushWarning")));
            _stabilityPanel.Children.Add(new FactRow("Reversão", "automática", Theme.GetBrush("BrushSuccess")));

            TextBlock note = Ui.Caption(anyRecent
                ? "Houve falha recente. Se ela se repetir, desative a extensão pela página Emergência e reaplique um efeito mais leve."
                : "Se uma combinação de efeito e cor travar o Explorer, o Bahama Studio restaura o config.ini anterior automaticamente.");
            note.FontSize = 10.5;
            note.Margin = new Thickness(0, 7, 0, 0);
            note.Foreground = anyRecent ? Ui.SB(danger) : Ui.SB(warning);
            _stabilityPanel.Children.Add(note);
        }

        private void BuildChecks(EngineStatus status, Color success, Color warning, Color danger)
        {
            _checksPanel.Children.Clear();
            _checksPanel.Children.Add(CheckRow(status.Windows.IsWindows11,
                "Windows compatível", status.Windows.Name + " build " + status.Windows.BuildLabel, false));
            _checksPanel.Children.Add(CheckRow(status.IsInstalled,
                "Engine instalada", status.IsInstalled ? "Arquivo encontrado em " + Paths.Shorten(status.RegisteredPath, 40) : "Nenhuma engine registrada", true));
            _checksPanel.Children.Add(CheckRow(status.IsModern,
                "Versão 2.1.0 ou superior", status.IsInstalled ? "Instalada: " + status.InstalledVersion : "Não instalada", true));
            _checksPanel.Children.Add(CheckRow(status.HasClsid,
                "CLSID registrado", Native.Clsid, true));
            _checksPanel.Children.Add(CheckRow(status.HasFolderExtension,
                "Extensão de pasta registrada", "Drive\\shellex\\FolderExtensions", true));
            _checksPanel.Children.Add(CheckRow(status.ConfigExists,
                "config.ini presente", status.ConfigExists ? Paths.Shorten(status.ConfigPath, 46) : "Será criado na instalação", false));
            _checksPanel.Children.Add(CheckRow(status.ConfigWritable,
                "Configuração gravável", status.ConfigWritable ? "Ajustes rápidos disponíveis" : "Cada gravação pedirá UAC", false));
            _checksPanel.Children.Add(CheckRow(status.MatchesBundled,
                "Integridade da engine", status.MatchesBundled ? "Idêntica à versão oficial incluída" : "Arquivo diferente da versão incluída", false));
            _checksPanel.Children.Add(CheckRow(status.ExplorerRunning,
                "Explorador em execução", status.ExplorerRunning ? "Pronto para carregar o efeito" : "Inicie o Explorador", false));
            _checksPanel.Children.Add(CheckRow(_crashes.Count == 0,
                "Sem falhas em 7 dias", _crashes.Count == 0 ? "Nenhum evento 1000 registrado" : _crashes.Count + " evento(s) encontrados", false));
        }

        private FrameworkElement CheckRow(bool ok, string title, string detail, bool critical)
        {
            Color color = ok ? Theme.GetColor("ColorSuccess") : (critical ? Theme.GetColor("ColorDanger") : Theme.GetColor("ColorWarning"));

            Border iconBadge = new Border();
            iconBadge.Width = 22;
            iconBadge.Height = 22;
            iconBadge.CornerRadius = new CornerRadius(11);
            iconBadge.Background = Ui.Alpha(color, 28);
            iconBadge.Child = Ui.Icon(ok ? Glyphs.Check : (critical ? Glyphs.Close : Glyphs.Warning), 9.5, Ui.SB(color));
            iconBadge.VerticalAlignment = VerticalAlignment.Top;

            TextBlock heading = Ui.Text(title, "TextBodyBright");
            heading.FontSize = 11.5;
            heading.TextWrapping = TextWrapping.NoWrap;
            heading.TextTrimming = TextTrimming.CharacterEllipsis;
            TextBlock sub = Ui.Caption(detail);
            sub.FontSize = 10;
            sub.TextWrapping = TextWrapping.NoWrap;
            sub.TextTrimming = TextTrimming.CharacterEllipsis;
            sub.Margin = new Thickness(0, 1, 0, 0);

            StackPanel text = new StackPanel();
            text.Margin = new Thickness(9, 0, 10, 0);
            text.VerticalAlignment = VerticalAlignment.Center;
            text.Children.Add(heading);
            text.Children.Add(sub);

            Grid row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Margin = new Thickness(0, 0, 0, 7);
            Grid.SetColumn(iconBadge, 0);
            row.Children.Add(iconBadge);
            Grid.SetColumn(text, 1);
            row.Children.Add(text);

            TextBlock result = new TextBlock();
            result.Text = ok ? "OK" : "ATENÇÃO";
            result.FontSize = 9;
            result.FontWeight = FontWeights.Bold;
            result.Foreground = Ui.SB(color);
            result.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(result, 2);
            row.Children.Add(result);

            return row;
        }

        private void BuildCrashes(Color success, Color danger)
        {
            _crashesPanel.Children.Clear();
            if (_crashes.Count == 0)
            {
                Grid ok = new Grid();
                ok.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                ok.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                TextBlock icon = Ui.Icon(Glyphs.Check, 13, Theme.GetBrush("BrushSuccess"));
                icon.Margin = new Thickness(0, 1, 8, 0);
                icon.VerticalAlignment = VerticalAlignment.Top;
                Grid.SetColumn(icon, 0);
                ok.Children.Add(icon);
                TextBlock text = Ui.Caption("Nenhum evento de falha do explorer.exe nos últimos 7 dias.");
                text.FontSize = 11;
                Grid.SetColumn(text, 1);
                ok.Children.Add(text);
                _crashesPanel.Children.Add(ok);
                return;
            }

            int limit = Math.Min(_crashes.Count, 8);
            for (int i = 0; i < limit; i++)
            {
                Native.CrashInfo crash = _crashes[i];
                Grid row = new Grid();
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(118) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.Margin = new Thickness(0, 0, 0, 6);

                TextBlock time = Ui.Caption(Ui.Fmt(crash.Time));
                time.FontSize = 10;
                time.VerticalAlignment = VerticalAlignment.Center;
                Grid.SetColumn(time, 0);
                row.Children.Add(time);

                TextBlock info = Ui.Text((crash.FaultingModule ?? "?") + "  •  " + (crash.ExceptionCode ?? "?"), "TextBodyBright");
                info.FontSize = 11;
                info.TextWrapping = TextWrapping.NoWrap;
                info.TextTrimming = TextTrimming.CharacterEllipsis;
                info.VerticalAlignment = VerticalAlignment.Center;
                Grid.SetColumn(info, 1);
                row.Children.Add(info);

                TextBlock tag = new TextBlock();
                tag.Text = "EVENTO 1000";
                tag.FontSize = 8.5;
                tag.FontWeight = FontWeights.SemiBold;
                tag.Foreground = Ui.SB(danger);
                tag.VerticalAlignment = VerticalAlignment.Center;
                tag.Margin = new Thickness(8, 0, 0, 0);
                Grid.SetColumn(tag, 2);
                row.Children.Add(tag);

                _crashesPanel.Children.Add(row);
            }

            if (_crashes.Count > limit)
            {
                TextBlock more = Ui.Caption("+ " + (_crashes.Count - limit) + " evento(s) anteriores. Veja o Visualizador de Eventos para o histórico completo.");
                more.FontSize = 10;
                more.Margin = new Thickness(0, 4, 0, 0);
                _crashesPanel.Children.Add(more);
            }
        }

        private void AdaptToMachine()
        {
            try
            {
                MachineProfile profile = MachineProfile.Capture();
                EngineStatus status = EngineManager.Detect();
                EngineConfig config = status.ConfigExists && System.IO.File.Exists(status.ConfigPath)
                    ? EngineConfig.Load(status.ConfigPath)
                    : EngineConfig.CreateDefault();

                if (status.ConfigExists)
                {
                    MessageBoxResult answer = MessageBox.Show(
                        "Já existe um config.ini em:\n" + status.ConfigPath +
                        "\n\nDeseja aplicar as definições recomendadas para este PC? As cores atuais serão preservadas.",
                        "Bahama Studio — Detetar e adaptar ao PC",
                        MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (answer != MessageBoxResult.Yes) return;
                }

                MachineProfile.ApplyRecommendation(profile, config);
                if (Host != null) Host.ApplyMachineRecommendation(config);
                Refresh();
            }
            catch (Exception ex)
            {
                if (Host != null) Host.Toast("Não foi possível adaptar: " + ex.Message, "error");
            }
        }

        private void CopyReport()
        {
            try
            {
                EngineStatus status = _status ?? EngineManager.Detect();
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Bahama Studio - Relatório de diagnóstico");
                sb.AppendLine("Gerado em: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                sb.AppendLine();
                sb.AppendLine("Sistema");
                sb.AppendLine("  Windows: " + status.Windows.Name + " " + status.Windows.DisplayVersion + " (build " + status.Windows.BuildLabel + ")");
                sb.AppendLine("  Arquitetura: " + (Environment.Is64BitOperatingSystem ? "x64" : "x86"));
                sb.AppendLine("  Administrador: " + (Native.IsAdmin() ? "sim" : "não"));
                sb.AppendLine("  Tema: " + (Native.IsSystemDarkMode() ? "escuro" : "claro"));
                MachineProfile profile = MachineProfile.Capture();
                string[] profileLines = profile.ReportLines();
                for (int i = 0; i < profileLines.Length; i++) sb.AppendLine(profileLines[i]);
                sb.AppendLine();
                sb.AppendLine("Engine");
                sb.AppendLine("  Instalada: " + (status.IsInstalled ? "sim" : "não"));
                sb.AppendLine("  Versão: " + status.InstalledVersion);
                sb.AppendLine("  Caminho: " + status.RegisteredPath);
                sb.AppendLine("  CLSID: " + (status.HasClsid ? "ok" : "ausente"));
                sb.AppendLine("  FolderExtension: " + (status.HasFolderExtension ? "ok" : "ausente"));
                sb.AppendLine("  SHA-256: " + status.InstalledHash);
                sb.AppendLine("  Config: " + status.ConfigPath);
                sb.AppendLine("  Config gravável: " + (status.ConfigWritable ? "sim" : "não"));
                sb.AppendLine();
                sb.AppendLine("Explorador");
                sb.AppendLine("  Em execução: " + (status.ExplorerRunning ? "sim" : "não"));
                sb.AppendLine("  Instâncias: " + status.ExplorerProcessCount);
                sb.AppendLine("  Engine carregada: " + (status.EngineLoaded ? "sim" : "não"));
                sb.AppendLine("  Dumps (7 dias): " + status.DumpCount);
                sb.AppendLine();
                sb.AppendLine("Falhas recentes (7 dias): " + _crashes.Count);
                int limit = Math.Min(_crashes.Count, 10);
                for (int i = 0; i < limit; i++)
                {
                    Native.CrashInfo crash = _crashes[i];
                    sb.AppendLine("  " + Ui.Fmt(crash.Time) + "  " + crash.FaultingModule + "  " + crash.ExceptionCode);
                }
                Clipboard.SetText(sb.ToString());
                if (Host != null) Host.Toast("Relatório copiado para a área de transferência.", "success");
            }
            catch (Exception ex)
            {
                if (Host != null) Host.Toast("Não foi possível copiar: " + ex.Message, "error");
            }
        }

        private void OpenLog()
        {
            try
            {
                if (System.IO.File.Exists(Paths.LogFile)) Native.OpenFolderAndSelect(Paths.LogFile);
                else Native.OpenFolder(Paths.LogsDir);
            }
            catch (Exception ex)
            {
                if (Host != null) Host.Toast("Não foi possível abrir o log: " + ex.Message, "error");
            }
        }

        private async void RestartExplorer(object sender, RoutedEventArgs e)
        {
            if (Host == null) return;
            Host.Busy.Show("Reiniciando o Explorer...", "As janelas abertas serão fechadas e reabertas.");
            await System.Threading.Tasks.Task.Run(delegate
            {
                EngineManager.KillExplorer();
                System.Threading.Thread.Sleep(1200);
                EngineManager.StartExplorer();
                System.Threading.Thread.Sleep(2200);
            });
            Host.Busy.Hide();
            bool running = Native.IsExplorerRunning();
            Host.Toast(running ? "Explorador reiniciado." : "O Explorer não voltou. Use a página Emergência.",
                running ? "success" : "warning");
            Refresh();
            Host.RefreshShellStatus();
        }
    }
}
