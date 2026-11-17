using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BahamaStudio
{
    public sealed class DashboardPage : PageBase
    {
        private readonly StatusTile _engineTile = new StatusTile(Glyphs.Download, "Engine");
        private readonly StatusTile _explorerTile = new StatusTile(Glyphs.Folder, "Explorador");
        private readonly StatusTile _systemTile = new StatusTile(Glyphs.Monitor, "Sistema");
        private readonly StatusTile _crashTile = new StatusTile(Glyphs.Bug, "Estabilidade");

        private readonly PreviewExplorer _preview = new PreviewExplorer();
        private readonly Border _stateChip;
        private readonly TextBlock _stateChipText;
        private readonly Button _primary;
        private Border _alert;
        private readonly TextBlock _alertBody;
        private readonly TextBlock _crashHeadline;
        private readonly TextBlock _crashDetail;
        private readonly Border _crashBadge;
        private readonly TextBlock _crashIcon;

        private EngineStatus _status;

        public DashboardPage()
        {
            _stateChipText = new TextBlock();
            _stateChipText.Style = Theme.GetStyle("ChipText");
            _stateChip = new Border();
            _stateChip.Style = Theme.GetStyle("Chip");
            _stateChip.Child = _stateChipText;

            _primary = Ui.Btn("Ajustar aparência", Glyphs.Palette, "BtnPrimary", PrimaryClicked);

            _alertBody = Ui.Body("");
            _alertBody.Foreground = Ui.SB("#FFD5DD");

            _crashHeadline = Ui.H3("Verificando...");
            _crashDetail = Ui.Caption("");
            _crashDetail.Margin = new Thickness(0, 3, 0, 0);
            _crashIcon = Ui.Icon(Glyphs.Check, 16, Theme.GetBrush("BrushSuccess"));
            _crashBadge = new Border();
            _crashBadge.Width = 40;
            _crashBadge.Height = 40;
            _crashBadge.CornerRadius = new CornerRadius(11);
            _crashBadge.Background = Ui.Alpha(Theme.GetColor("ColorSuccess"), 28);
            _crashBadge.Child = _crashIcon;
            _crashBadge.VerticalAlignment = VerticalAlignment.Top;

            Children.Add(Build());
            Refresh();
        }

        private FrameworkElement Build()
        {
            StackPanel content = new StackPanel();
            content.Margin = new Thickness(34, 26, 34, 30);

            content.Children.Add(BuildHero());
            Ui.AddMargin(content.Children[content.Children.Count - 1], 0, 0, 0, 18);

            _alert = BuildAlert();
            _alert.Visibility = Visibility.Collapsed;
            content.Children.Add(_alert);
            Ui.AddMargin(_alert, 0, 0, 0, 18);

            content.Children.Add(BuildTiles());
            Ui.AddMargin(content.Children[content.Children.Count - 1], 0, 0, 0, 24);

            content.Children.Add(Ui.SectionTitle("Saúde do sistema", "Um resumo rápido do que está acontecendo com o seu Explorer."));
            content.Children.Add(BuildHealthCard());
            Ui.AddMargin(content.Children[content.Children.Count - 1], 0, 0, 0, 24);

            content.Children.Add(Ui.SectionTitle("Primeiros passos", "Tudo o que você precisa para deixar o Explorador com a sua cara."));
            content.Children.Add(BuildTips());
            Ui.AddMargin(content.Children[content.Children.Count - 1], 0, 0, 0, 18);

            TextBlock footer = Ui.Caption("Engine ExplorerBlurMica 2.1.0 • Maplespe • LGPLv3  |  Bahama Studio, criado por Clávio Bahama — gerenciador independente, sem telemetria.");
            footer.HorizontalAlignment = HorizontalAlignment.Center;
            footer.Margin = new Thickness(0, 6, 0, 0);
            content.Children.Add(footer);

            ScrollViewer scroll = Ui.Scroll(content, 6);
            return scroll;
        }

        private FrameworkElement BuildHero()
        {
            Border hero = new Border();
            hero.Style = Theme.GetStyle("CardRaised");
            hero.Background = Theme.GetBrush("BrushHero");
            hero.Padding = new Thickness(28);
            LinearGradientBrush border = new LinearGradientBrush();
            border.StartPoint = new Point(0, 0);
            border.EndPoint = new Point(1, 1);
            border.GradientStops.Add(new GradientStop(Color.FromArgb(110, 110, 168, 255), 0));
            border.GradientStops.Add(new GradientStop(Color.FromArgb(30, 167, 139, 250), 0.55));
            border.GradientStops.Add(new GradientStop(Color.FromArgb(80, 167, 139, 250), 1));
            hero.BorderBrush = border;

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(330) });

            StackPanel left = new StackPanel();
            left.Margin = new Thickness(0, 2, 26, 0);

            StackPanel chips = new StackPanel();
            chips.Orientation = Orientation.Horizontal;
            _stateChip.VerticalAlignment = VerticalAlignment.Center;
            chips.Children.Add(_stateChip);
            Border engineChip = Ui.Chip("ENGINE OFICIAL 2.1.0");
            engineChip.Margin = new Thickness(8, 0, 0, 0);
            engineChip.VerticalAlignment = VerticalAlignment.Center;
            chips.Children.Add(engineChip);
            left.Children.Add(chips);

            TextBlock title = Ui.H1("Seu Explorer, com vidro, cor e personalidade.");
            title.FontSize = 30;
            title.LineHeight = 35;
            title.Margin = new Thickness(0, 14, 0, 0);
            title.MaxWidth = 520;
            left.Children.Add(title);

            TextBlock body = Ui.Body("Instale, teste e ajuste o ExplorerBlurMica com prévia ao vivo, diagnósticos e reversão automática. Sem editar INI no escuro — e sem medo de quebrar o Explorador.");
            body.Margin = new Thickness(0, 9, 0, 0);
            body.MaxWidth = 500;
            left.Children.Add(body);

            StackPanel actions = new StackPanel();
            actions.Orientation = Orientation.Horizontal;
            actions.Margin = new Thickness(0, 20, 0, 0);
            actions.Children.Add(_primary);

            Button install = Ui.Btn("Instalar / atualizar", Glyphs.Download, "BtnSecondary", delegate { Go("instalar"); });
            install.Margin = new Thickness(10, 0, 0, 0);
            actions.Children.Add(install);

            Button restart = Ui.Btn("Reiniciar Explorer", Glyphs.Refresh, "BtnGhost", RestartClicked);
            restart.Margin = new Thickness(10, 0, 0, 0);
            actions.Children.Add(restart);
            left.Children.Add(actions);

            TextBlock note = Ui.Caption("Reversão automática ativada • sem telemetria • código de gerenciamento local");
            note.Margin = new Thickness(1, 14, 0, 0);
            left.Children.Add(note);

            Grid.SetColumn(left, 0);
            grid.Children.Add(left);

            Grid previewGrid = new Grid();
            previewGrid.Width = 330;
            previewGrid.Height = 222;
            previewGrid.HorizontalAlignment = HorizontalAlignment.Right;
            previewGrid.VerticalAlignment = VerticalAlignment.Center;
            previewGrid.Cursor = Cursors.Hand;
            previewGrid.ToolTip = "Abrir a prévia interativa";
            previewGrid.MouseLeftButtonUp += delegate { Go("aparencia"); };

            Border shell = new Border();
            shell.CornerRadius = new CornerRadius(12);
            shell.ClipToBounds = true;
            shell.BorderBrush = Ui.SB("#33FFFFFF");
            shell.BorderThickness = new Thickness(1);
            shell.Background = Ui.SB("#0A1020");
            Viewbox view = new Viewbox();
            view.Stretch = Stretch.Uniform;
            view.Child = _preview;
            shell.Child = view;
            previewGrid.Children.Add(shell);

            Border live = new Border();
            live.Background = Ui.SB("#D90B1220");
            live.BorderBrush = Ui.SB("#33FFFFFF");
            live.BorderThickness = new Thickness(1);
            live.CornerRadius = new CornerRadius(12);
            live.Padding = new Thickness(9, 4, 9, 4);
            live.HorizontalAlignment = HorizontalAlignment.Left;
            live.VerticalAlignment = VerticalAlignment.Bottom;
            live.Margin = new Thickness(12);
            live.IsHitTestVisible = false;
            StackPanel liveRow = new StackPanel();
            liveRow.Orientation = Orientation.Horizontal;
            Border dot = new Border();
            dot.Width = 6;
            dot.Height = 6;
            dot.CornerRadius = new CornerRadius(3);
            dot.Background = Theme.GetBrush("BrushSuccess");
            dot.VerticalAlignment = VerticalAlignment.Center;
            liveRow.Children.Add(dot);
            TextBlock liveText = new TextBlock();
            liveText.Text = "PRÉVIA AO VIVO";
            liveText.FontSize = 8.5;
            liveText.FontWeight = FontWeights.SemiBold;
            liveText.Foreground = Ui.SB("#D7E3F5");
            liveText.Margin = new Thickness(6, 0, 0, 0);
            liveRow.Children.Add(liveText);
            live.Child = liveRow;
            previewGrid.Children.Add(live);

            Grid.SetColumn(previewGrid, 1);
            grid.Children.Add(previewGrid);

            hero.Child = grid;
            return hero;
        }

        private Border BuildAlert()
        {
            Border alert = new Border();
            alert.Background = Theme.GetBrush("BrushDangerSoft");
            alert.BorderBrush = Ui.Alpha(Theme.GetColor("ColorDanger"), 90);
            alert.BorderThickness = new Thickness(1);
            alert.CornerRadius = new CornerRadius(12);
            alert.Padding = new Thickness(16, 14, 16, 14);

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Border badge = new Border();
            badge.Width = 36;
            badge.Height = 36;
            badge.CornerRadius = new CornerRadius(10);
            badge.Background = Ui.Alpha(Theme.GetColor("ColorDanger"), 32);
            badge.Child = Ui.Icon(Glyphs.Warning, 15, Theme.GetBrush("BrushDanger"));
            badge.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(badge, 0);
            grid.Children.Add(badge);

            TextBlock title = Ui.Text("O Explorador precisa de atenção", "TextBodyBright");
            title.FontSize = 12.5;
            title.FontWeight = FontWeights.SemiBold;
            StackPanel text = new StackPanel();
            text.Margin = new Thickness(12, 0, 12, 0);
            text.VerticalAlignment = VerticalAlignment.Center;
            text.Children.Add(title);
            text.Children.Add(_alertBody);
            _alertBody.Margin = new Thickness(0, 2, 0, 0);
            _alertBody.FontSize = 11.5;
            Grid.SetColumn(text, 1);
            grid.Children.Add(text);

            Button recover = Ui.Btn("Abrir recuperação", Glyphs.Shield, "BtnDanger", delegate { Go("emergencia"); });
            recover.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(recover, 2);
            grid.Children.Add(recover);

            alert.Child = grid;
            return alert;
        }

        private FrameworkElement BuildTiles()
        {
            Grid grid = Ui.Columns(Ui.Star(1), Ui.Star(1), Ui.Star(1), Ui.Star(1));
            grid.Children.Add(_engineTile);
            grid.Children.Add(_explorerTile);
            grid.Children.Add(_systemTile);
            grid.Children.Add(_crashTile);

            for (int i = 0; i < grid.Children.Count; i++)
            {
                FrameworkElement tile = grid.Children[i] as FrameworkElement;
                if (tile == null) continue;
                Grid.SetColumn(tile, i);
                if (i < grid.Children.Count - 1) tile.Margin = new Thickness(0, 0, 10, 0);
            }
            return grid;
        }

        private Border BuildHealthCard()
        {
            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Grid.SetColumn(_crashBadge, 0);
            grid.Children.Add(_crashBadge);

            StackPanel text = new StackPanel();
            text.Margin = new Thickness(14, 0, 14, 0);
            text.VerticalAlignment = VerticalAlignment.Center;
            text.Children.Add(_crashHeadline);
            text.Children.Add(_crashDetail);
            Grid.SetColumn(text, 1);
            grid.Children.Add(text);

            Button diagnostics = Ui.Btn("Ver diagnóstico", Glyphs.Diagnostic, "BtnSecondary", delegate { Go("diagnostico"); });
            diagnostics.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(diagnostics, 2);
            grid.Children.Add(diagnostics);

            return Ui.Card(grid);
        }

        private FrameworkElement BuildTips()
        {
            Grid grid = Ui.Columns(Ui.Star(1), Ui.Star(1), Ui.Star(1));

            Border tip1 = TipCard(Glyphs.Download, "Instalação em 1 clique",
                "O Bahama Studio copia a engine 2.1.0, registra e testa o Explorador por você. Se algo falhar, a extensão é desativada.",
                "Abrir instalador", "instalar", Theme.GetColor("ColorAccent"));
            Border tip2 = TipCard(Glyphs.Palette, "Ajuste com prévia",
                "Troque entre Acrílico, Mica, Blur puro e Mica Alt, mexa nas cores e veja o resultado antes de aplicar.",
                "Explorar aparência", "aparencia", Theme.GetColor("ColorAccent2"));
            Border tip3 = TipCard(Glyphs.Shield, "Nunca fique na mão",
                "Segure ESC ao abrir uma pasta ou use a página de emergência para desativar a extensão e reiniciar o Explorador.",
                "Ver emergência", "emergencia", Theme.GetColor("ColorSuccess"));

            grid.Children.Add(tip1);
            grid.Children.Add(tip2);
            grid.Children.Add(tip3);
            for (int i = 0; i < grid.Children.Count; i++)
            {
                FrameworkElement card = grid.Children[i] as FrameworkElement;
                if (card == null) continue;
                Grid.SetColumn(card, i);
                if (i < grid.Children.Count - 1) card.Margin = new Thickness(0, 0, 12, 0);
            }
            return grid;
        }

        private Border TipCard(string glyph, string title, string body, string link, string page, Color color)
        {
            Border badge = new Border();
            badge.Width = 36;
            badge.Height = 36;
            badge.CornerRadius = new CornerRadius(10);
            badge.Background = Ui.Alpha(color, 26);
            badge.Child = Ui.Icon(glyph, 15, Ui.SB(color));
            badge.HorizontalAlignment = HorizontalAlignment.Left;

            TextBlock heading = Ui.H3(title);
            heading.Margin = new Thickness(0, 11, 0, 0);

            TextBlock text = Ui.Caption(body);
            text.Margin = new Thickness(0, 5, 0, 0);
            text.FontSize = 11.5;
            text.LineHeight = 17;

            Button action = Ui.LinkBtn(link, delegate { Go(page); });
            action.Margin = new Thickness(0, 12, 0, 0);

            StackPanel content = new StackPanel();
            content.Children.Add(badge);
            content.Children.Add(heading);
            content.Children.Add(text);
            content.Children.Add(action);

            Border card = Ui.CardSoft(content);
            card.Padding = new Thickness(18);
            return card;
        }

        public override void Refresh()
        {
            EngineStatus status = EngineManager.Detect();
            _status = status;

            Color success = Theme.GetColor("ColorSuccess");
            Color accent = Theme.GetColor("ColorAccent");
            Color warning = Theme.GetColor("ColorWarning");
            Color danger = Theme.GetColor("ColorDanger");

            string engineValue;
            string engineDetail;
            Color engineColor;
            if (!status.IsInstalled)
            {
                engineValue = "Não instalada";
                engineDetail = "Use a página Instalar para começar";
                engineColor = warning;
            }
            else if (!status.IsModern)
            {
                engineValue = "Desatualizada";
                engineDetail = "Versão " + status.InstalledVersion + " • atualize para 2.1.0";
                engineColor = warning;
            }
            else
            {
                engineValue = "Ativa • 2.1.0";
                engineDetail = Paths.Shorten(status.RegisteredPath, 42);
                engineColor = success;
            }
            _engineTile.Set(engineValue, engineDetail, engineColor);

            _explorerTile.Set(
                status.ExplorerRunning ? "Em execução" : "Fechado",
                status.EngineLoaded ? "Módulo carregado no Explorer" :
                    (status.ExplorerRunning ? "Abra uma pasta para carregar" : "Será iniciado automaticamente"),
                status.ExplorerRunning ? success : warning);

            string systemDetail = "Build " + status.Windows.BuildLabel + " • " +
                (status.Windows.SupportsMica ? "Mica disponível" : "Mica indisponível • use Acrílico");
            _systemTile.Set(status.Windows.Name + (string.IsNullOrEmpty(status.Windows.DisplayVersion) ? "" : " " + status.Windows.DisplayVersion),
                systemDetail, status.Windows.IsWindows11 ? accent : warning);

            List<Native.CrashInfo> crashes = Native.GetExplorerCrashes(7);
            Native.CrashInfo latest = crashes.Count > 0 ? crashes[0] : null;
            _crashTile.Set(
                crashes.Count == 0 ? "Estável" : crashes.Count + (crashes.Count == 1 ? " falha" : " falhas"),
                crashes.Count == 0 ? "Nenhuma falha em 7 dias" : "Última " + Ui.Relative(latest.Time),
                crashes.Count == 0 ? success : danger);

            if (crashes.Count == 0)
            {
                _crashHeadline.Text = "Nenhuma falha do Explorer nos últimos 7 dias";
                _crashDetail.Text = "A extensão está rodando sem registros de falha. Bom sinal.";
                _crashIcon.Text = Glyphs.Check;
                _crashIcon.Foreground = Theme.GetBrush("BrushSuccess");
                _crashBadge.Background = Ui.Alpha(success, 28);
            }
            else
            {
                _crashHeadline.Text = crashes.Count + (crashes.Count == 1 ? " falha registrada" : " falhas registradas") + " nos últimos 7 dias";
                _crashDetail.Text = "Última em " + Ui.Fmt(latest.Time) + " • " + latest.FaultingModule + " • " + latest.ExceptionCode;
                _crashIcon.Text = Glyphs.Bug;
                _crashIcon.Foreground = Theme.GetBrush("BrushDanger");
                _crashBadge.Background = Ui.Alpha(danger, 28);
            }

            bool recentCrash = latest != null && (DateTime.Now - latest.Time).TotalMinutes < 60;
            if (recentCrash)
            {
                _alert.Visibility = Visibility.Visible;
                _alertBody.Text = "Última falha " + Ui.Relative(latest.Time) + " em " + latest.FaultingModule +
                                  ". Se o Explorador estiver reiniciando sozinho, desative a extensão agora.";
            }
            else if (!status.ExplorerRunning)
            {
                _alert.Visibility = Visibility.Visible;
                _alertBody.Text = "O Explorer não está em execução. Reinicie por aqui ou use o modo de emergência.";
            }
            else
            {
                _alert.Visibility = Visibility.Collapsed;
            }

            if (status.IsInstalled && status.IsModern)
            {
                SetStateChip("ENGINE ATIVA", success);
                Ui.SetButton(_primary, "Ajustar aparência", Glyphs.Palette, "BtnPrimary");
            }
            else if (status.IsInstalled)
            {
                SetStateChip("ATUALIZAÇÃO DISPONÍVEL", warning);
                Ui.SetButton(_primary, "Atualizar engine", Glyphs.Download, "BtnPrimary");
            }
            else
            {
                SetStateChip("ENGINE AUSENTE", warning);
                Ui.SetButton(_primary, "Instalar engine 2.1.0", Glyphs.Download, "BtnPrimary");
            }

            EngineConfig config = EngineManager.LoadActiveConfig();
            _preview.Update(config, Native.IsSystemDarkMode());
        }

        private void SetStateChip(string text, Color color)
        {
            _stateChipText.Text = text;
            _stateChipText.Foreground = Ui.SB(color);
            _stateChip.Background = Ui.Alpha(color, 24);
            _stateChip.BorderBrush = Ui.Alpha(color, 70);
        }

        private void PrimaryClicked(object sender, RoutedEventArgs e)
        {
            if (_status != null && _status.NeedsInstall) Go("instalar");
            else Go("aparencia");
        }

        private void Go(string page)
        {
            if (Host != null) Host.Navigate(page);
        }

        private async void RestartClicked(object sender, RoutedEventArgs e)
        {
            if (Host == null) return;
            Host.Busy.Show("Reiniciando o Explorer...", "As janelas abertas serão fechadas e reabertas.");
            await Task.Run(delegate
            {
                EngineManager.KillExplorer();
                Thread.Sleep(1200);
                EngineManager.StartExplorer();
                Thread.Sleep(2200);
            });
            Host.Busy.Hide();
            bool running = Native.IsExplorerRunning();
            Host.Toast(running ? "Explorador reiniciado." : "O Explorer não voltou. Use a Emergência.", running ? "success" : "warning");
            Refresh();
        }
    }
}
