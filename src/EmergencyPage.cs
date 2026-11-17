using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace BahamaStudio
{
    public sealed class EmergencyPage : PageBase
    {
        private readonly TextBlock _stateTitle;
        private readonly TextBlock _stateBody;
        private readonly Border _stateBadge;
        private readonly TextBlock _stateIcon;
        private readonly Border _stateCard;
        private readonly TextBlock _backupSummary;
        private readonly StackPanel _backupList;
        private readonly Button _restoreButton;
        private readonly Button _disableButton;
        private readonly Button _removeButton;
        private EngineStatus _status;
        private bool _busy;

        public EmergencyPage()
        {
            _stateIcon = Ui.Icon(Glyphs.Shield, 18, Theme.GetBrush("BrushSuccess"));
            _stateBadge = new Border();
            _stateBadge.Width = 46;
            _stateBadge.Height = 46;
            _stateBadge.CornerRadius = new CornerRadius(13);
            _stateBadge.Background = Ui.Alpha(Theme.GetColor("ColorSuccess"), 28);
            _stateBadge.Child = _stateIcon;
            _stateBadge.VerticalAlignment = VerticalAlignment.Top;

            _stateTitle = Ui.Text("Verificando...", "TextH2");
            _stateBody = Ui.Body("");
            _stateBody.Margin = new Thickness(0, 4, 0, 0);
            _stateBody.MaxWidth = 540;

            StackPanel stateText = new StackPanel();
            stateText.VerticalAlignment = VerticalAlignment.Center;
            stateText.Margin = new Thickness(15, 0, 0, 0);
            stateText.Children.Add(_stateTitle);
            stateText.Children.Add(_stateBody);

            Grid stateGrid = new Grid();
            stateGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            stateGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetColumn(_stateBadge, 0);
            stateGrid.Children.Add(_stateBadge);
            Grid.SetColumn(stateText, 1);
            stateGrid.Children.Add(stateText);

            _stateCard = new Border();
            _stateCard.Style = Theme.GetStyle("Card");
            _stateCard.Padding = new Thickness(20);
            _stateCard.Child = stateGrid;

            _backupSummary = Ui.Caption("");
            _backupSummary.FontSize = 11;
            _backupList = new StackPanel();
            _backupList.Margin = new Thickness(0, 6, 0, 0);

            _restoreButton = Ui.Btn("Restaurar último backup", Glyphs.Undo, "BtnSecondary", RestoreClicked);
            _disableButton = Ui.Btn("Desativar a extensão", Glyphs.Power, "BtnDanger", DisableClicked);
            _removeButton = Ui.Btn("Remover tudo", Glyphs.Delete, "BtnGhost", RemoveClicked);

            Children.Add(Build());
            Refresh();
        }

        private FrameworkElement Build()
        {
            Grid root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Button dash = Ui.Btn("Voltar ao início", Glyphs.Home, "BtnGhost", delegate
            {
                if (Host != null) Host.Navigate("dashboard");
            });
            FrameworkElement header = Ui.PageHeader("Emergência", "Se o Explorer não abrir, comece por aqui",
                "Ações diretas para devolver o Explorador ao normal. Tudo pode ser desfeito depois.",
                dash);
            header.Margin = new Thickness(34, 26, 34, 18);
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            StackPanel content = new StackPanel();
            content.Margin = new Thickness(34, 0, 34, 28);

            content.Children.Add(_stateCard);
            Ui.AddMargin(_stateCard, 0, 0, 0, 18);

            content.Children.Add(BuildQuickActions());
            Ui.AddMargin(content.Children[content.Children.Count - 1], 0, 0, 0, 22);

            content.Children.Add(Ui.SectionTitle("Primeiros socorros",
                "Na ordem em que costumam resolver. Você pode parar assim que o Explorer voltar."));
            content.Children.Add(BuildFirstAid());
            Ui.AddMargin(content.Children[content.Children.Count - 1], 0, 0, 0, 22);

            content.Children.Add(Ui.SectionTitle("Backups de configuração",
                "Cada instalação e cada aplicação de aparência guarda uma cópia do config.ini anterior."));
            content.Children.Add(BuildBackupCard());
            Ui.AddMargin(content.Children[content.Children.Count - 1], 0, 0, 0, 22);

            content.Children.Add(Ui.SectionTitle("Registro técnico",
                "As últimas linhas do log local do Bahama Studio."));
            content.Children.Add(BuildLogCard());

            ScrollViewer scroll = Ui.Scroll(content, 6);
            Grid.SetRow(scroll, 1);
            root.Children.Add(scroll);
            return root;
        }

        private FrameworkElement BuildQuickActions()
        {
            UniformGrid grid = new UniformGrid();
            grid.Columns = 3;

            grid.Children.Add(ActionCard(Glyphs.Refresh, "Reiniciar o Explorer",
                "Fecha e reabre o explorer.exe sem mexer na extensão. O efeito atual continua configurado.",
                "Reiniciar agora", "BtnSecondary", RestartClicked, Theme.GetColor("ColorAccent")));

            grid.Children.Add(ActionCard(Glyphs.Power, "Desativar a extensão",
                "Desregistra o ExplorerBlurMica. O Explorador volta ao visual original. Os arquivos continuam no disco.",
                "Desativar", "BtnDanger", DisableClicked, Theme.GetColor("ColorWarning")));

            grid.Children.Add(ActionCard(Glyphs.Delete, "Remover completamente",
                "Desregistra e apaga o DLL, o config.ini e os arquivos de licença da pasta de instalação.",
                "Remover", "BtnGhost", RemoveClicked, Theme.GetColor("ColorDanger")));

            return grid;
        }

        private Border ActionCard(string glyph, string title, string body, string action,
            string style, RoutedEventHandler handler, Color color)
        {
            Border badge = new Border();
            badge.Width = 34;
            badge.Height = 34;
            badge.CornerRadius = new CornerRadius(10);
            badge.Background = Ui.Alpha(color, 26);
            badge.Child = Ui.Icon(glyph, 14, Ui.SB(color));
            badge.HorizontalAlignment = HorizontalAlignment.Left;

            TextBlock heading = Ui.Text(title, "TextBodyBright");
            heading.FontSize = 12.5;
            heading.FontWeight = FontWeights.SemiBold;
            heading.Margin = new Thickness(0, 10, 0, 0);

            TextBlock text = Ui.Caption(body);
            text.FontSize = 10.5;
            text.LineHeight = 16;
            text.Margin = new Thickness(0, 4, 0, 0);

            Button button = Ui.Btn(action, null, style, handler);
            button.Margin = new Thickness(0, 12, 0, 0);
            button.Height = 34;
            button.MinHeight = 0;
            button.HorizontalAlignment = HorizontalAlignment.Left;

            StackPanel content = new StackPanel();
            content.Children.Add(badge);
            content.Children.Add(heading);
            content.Children.Add(text);
            content.Children.Add(button);

            Border card = Ui.CardSoft(content);
            card.Padding = new Thickness(16);
            card.Margin = new Thickness(0, 0, 9, 0);
            return card;
        }

        private FrameworkElement BuildFirstAid()
        {
            StackPanel list = new StackPanel();
            list.Children.Add(FirstAidStep("1", "Segure ESC ao abrir uma pasta",
                "O ExplorerBlurMica ignora o efeito enquanto o ESC estiver pressionado. Abra uma pasta segurando ESC, " +
                "vá até o Bahama Studio e desative a extensão. Isso evita que a janela trave durante a recuperação.", Theme.GetColor("ColorAccent")));
            list.Children.Add(FirstAidStep("2", "Reinicie o explorer.exe",
                "Use o botão Reiniciar acima ou pressione Ctrl+Shift+Esc, encontre Explorador do Windows, clique com o botão direito " +
                "e escolha Reiniciar. O Windows costuma abrir uma janela de pasta sozinho.", Theme.GetColor("ColorAccent")));
            list.Children.Add(FirstAidStep("3", "Abra o Explorer manualmente",
                "Pressione Win+R, digite explorer.exe e confirme. Se nada abrir, pressione Ctrl+Shift+Esc e use Arquivo > Executar nova tarefa.", Theme.GetColor("ColorWarning")));
            list.Children.Add(FirstAidStep("4", "Desative a extensão",
                "Se o Explorer abrir e travar de novo, use Desativar a extensão. A DLL continua guardada para uma nova tentativa " +
                "com um efeito mais leve, como Acrílico ou Blur puro.", Theme.GetColor("ColorWarning")));
            list.Children.Add(FirstAidStep("5", "Se nada funcionar, reinicie o Windows",
                "Segure Shift ao clicar em Reiniciar para abrir as opções avançadas. Em último caso, o Modo de Segurança " +
                "permite desinstalar a extensão sem carregá-la.", Theme.GetColor("ColorDanger")));

            return Ui.Card(list);
        }

        private FrameworkElement FirstAidStep(string number, string title, string body, Color color)
        {
            Border badge = new Border();
            badge.Width = 24;
            badge.Height = 24;
            badge.CornerRadius = new CornerRadius(12);
            badge.Background = Ui.Alpha(color, 28);
            TextBlock num = new TextBlock();
            num.Text = number;
            num.FontSize = 10.5;
            num.FontWeight = FontWeights.Bold;
            num.Foreground = Ui.SB(color);
            num.HorizontalAlignment = HorizontalAlignment.Center;
            num.VerticalAlignment = VerticalAlignment.Center;
            badge.Child = num;
            badge.VerticalAlignment = VerticalAlignment.Top;

            TextBlock heading = Ui.Text(title, "TextBodyBright");
            heading.FontSize = 12;
            heading.FontWeight = FontWeights.SemiBold;
            TextBlock text = Ui.Caption(body);
            text.FontSize = 10.5;
            text.Margin = new Thickness(0, 2, 0, 0);
            text.LineHeight = 16;
            text.MaxWidth = 690;

            StackPanel bodyPanel = new StackPanel();
            bodyPanel.Margin = new Thickness(10, 0, 0, 0);
            bodyPanel.Children.Add(heading);
            bodyPanel.Children.Add(text);

            Grid row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.Margin = new Thickness(0, 0, 0, 13);
            Grid.SetColumn(badge, 0);
            row.Children.Add(badge);
            Grid.SetColumn(bodyPanel, 1);
            row.Children.Add(bodyPanel);
            return row;
        }

        private FrameworkElement BuildBackupCard()
        {
            StackPanel content = new StackPanel();
            content.Children.Add(_backupSummary);
            content.Children.Add(_backupList);

            StackPanel buttons = new StackPanel();
            buttons.Orientation = Orientation.Horizontal;
            buttons.Margin = new Thickness(0, 12, 0, 0);
            buttons.Children.Add(_restoreButton);
            Button open = Ui.Btn("Abrir pasta de backups", Glyphs.Folder, "BtnGhost", delegate
            {
                Native.OpenFolder(Paths.BackupsDir);
            });
            open.Margin = new Thickness(9, 0, 0, 0);
            buttons.Children.Add(open);
            content.Children.Add(buttons);

            TextBlock note = Ui.Caption("Restaurar grava o config.ini do backup mais recente. Reinicie o Explorer depois para carregar o efeito restaurado.");
            note.FontSize = 10.5;
            note.Margin = new Thickness(0, 9, 0, 0);
            content.Children.Add(note);

            return Ui.Card(content);
        }

        private FrameworkElement BuildLogCard()
        {
            StackPanel content = new StackPanel();

            StackPanel header = new StackPanel();
            header.Orientation = Orientation.Horizontal;
            TextBlock title = Ui.Label("BAHAMA-STUDIO.LOG");
            title.FontSize = 9.5;
            header.Children.Add(title);
            TextBlock path = Ui.Caption(Paths.Shorten(Paths.LogFile, 62));
            path.FontSize = 9.5;
            path.Margin = new Thickness(10, 0, 0, 0);
            header.Children.Add(path);
            content.Children.Add(header);

            Border box = new Border();
            box.Background = Ui.SB("#080D16");
            box.BorderBrush = Ui.SB("#1B2841");
            box.BorderThickness = new Thickness(1);
            box.CornerRadius = new CornerRadius(9);
            box.Padding = new Thickness(12);
            box.Margin = new Thickness(0, 9, 0, 0);

            string tail = Log.Tail(16);
            if (string.IsNullOrEmpty(tail)) tail = "(sem registros)";
            TextBlock log = Ui.Mono(tail);
            log.FontSize = 10;
            log.LineHeight = 15;
            log.MaxHeight = 240;
            box.Child = log;
            content.Children.Add(box);

            StackPanel buttons = new StackPanel();
            buttons.Orientation = Orientation.Horizontal;
            buttons.Margin = new Thickness(0, 10, 0, 0);
            Button open = Ui.Btn("Abrir log completo", Glyphs.Document, "BtnGhost", delegate
            {
                Native.OpenFolderAndSelect(Paths.LogFile);
            });
            open.Height = 32;
            open.MinHeight = 0;
            open.Padding = new Thickness(11, 4, 11, 4);
            buttons.Children.Add(open);
            Button refresh = Ui.Btn("Atualizar", Glyphs.Refresh, "BtnGhost", delegate { Refresh(); });
            refresh.Height = 32;
            refresh.MinHeight = 0;
            refresh.Padding = new Thickness(11, 4, 11, 4);
            refresh.Margin = new Thickness(8, 0, 0, 0);
            buttons.Children.Add(refresh);
            content.Children.Add(buttons);

            return Ui.Card(content);
        }

        public override void Refresh()
        {
            if (_busy) return;
            EngineStatus status = EngineManager.Detect();
            _status = status;

            Color success = Theme.GetColor("ColorSuccess");
            Color warning = Theme.GetColor("ColorWarning");
            Color danger = Theme.GetColor("ColorDanger");

            if (status.IsInstalled)
            {
                _stateIcon.Text = Glyphs.Shield;
                _stateIcon.Foreground = Ui.SB(success);
                _stateBadge.Background = Ui.Alpha(success, 28);
                _stateTitle.Text = "A extensão está ativa";
                _stateBody.Text = "Se o Explorador estiver instável, desative a extensão e reinicie. " +
                                  "Se estiver tudo bem, use esta página apenas quando precisar de manutenção.";
                _stateCard.BorderBrush = Ui.Alpha(success, 70);
                _disableButton.Content = null;
                Ui.SetButton(_disableButton, "Desativar a extensão", Glyphs.Power, "BtnDanger");
                Ui.SetButton(_removeButton, "Remover tudo", Glyphs.Delete, "BtnGhost");
            }
            else
            {
                _stateIcon.Text = Glyphs.Info;
                _stateIcon.Foreground = Ui.SB(warning);
                _stateBadge.Background = Ui.Alpha(warning, 28);
                _stateTitle.Text = "A extensão não está registrada";
                _stateBody.Text = "O Explorador está no visual original. Se ele ainda estiver instável, reinicie o processo ou " +
                                  "verifique os eventos no Diagnóstico.";
                _stateCard.BorderBrush = Ui.Alpha(warning, 70);
                Ui.SetButton(_disableButton, "Extensão já desativada", Glyphs.Check, "BtnGhost");
                Ui.SetButton(_removeButton, "Limpar arquivos restantes", Glyphs.Delete, "BtnGhost");
            }
            _disableButton.IsEnabled = status.IsInstalled;
            _removeButton.IsEnabled = status.IsInstalled;

            List<BackupInfo> backups = EngineManager.ListBackups();
            int withConfig = 0;
            for (int i = 0; i < backups.Count; i++) if (backups[i].HasConfig) withConfig++;
            _backupSummary.Text = backups.Count == 0
                ? "Nenhum backup criado ainda."
                : backups.Count + (backups.Count == 1 ? " backup" : " backups") + " • " + withConfig +
                  (withConfig == 1 ? " com configuração" : " com configuração");
            _restoreButton.IsEnabled = withConfig > 0;

            _backupList.Children.Clear();
            int limit = Math.Min(backups.Count, 5);
            for (int i = 0; i < limit; i++)
            {
                BackupInfo info = backups[i];
                Grid row = new Grid();
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(118) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.Margin = new Thickness(0, 0, 0, 5);

                TextBlock time = Ui.Caption(info.Time == DateTime.MinValue ? "—" : Ui.Fmt(info.Time));
                time.FontSize = 10;
                time.VerticalAlignment = VerticalAlignment.Center;
                Grid.SetColumn(time, 0);
                row.Children.Add(time);

                string parts = "";
                if (info.HasDll) parts += "DLL";
                if (info.HasConfig) parts += (parts.Length > 0 ? " + " : "") + "config.ini";
                if (parts.Length == 0) parts = "vazio";
                TextBlock items = Ui.Text(parts, "TextBodyBright");
                items.FontSize = 10.5;
                items.VerticalAlignment = VerticalAlignment.Center;
                Grid.SetColumn(items, 1);
                row.Children.Add(items);

                Button open = Ui.LinkBtn("abrir", delegate { Native.OpenFolder(info.Folder); });
                open.FontSize = 10;
                open.VerticalAlignment = VerticalAlignment.Center;
                Grid.SetColumn(open, 2);
                row.Children.Add(open);

                _backupList.Children.Add(row);
            }
        }

        private void RestoreClicked(object sender, RoutedEventArgs e)
        {
            if (Host == null) return;
            Host.Dialog.Confirm("Restaurar o último backup?",
                "O config.ini atual será substituído pelo backup mais recente que contém configuração. " +
                "Você pode reiniciar o Explorer em seguida para aplicar.",
                "Restaurar", false, delegate { RunRestore(); });
        }

        private async void RunRestore()
        {
            if (Host == null || _busy) return;
            _busy = true;
            Host.Busy.Show("Restaurando configuração...", "Copiando o backup mais recente.");
            string message = "";
            bool ok = await Task.Run(delegate
            {
                string result;
                bool success = EngineManager.RestoreLatestConfig(out result);
                message = result;
                return success;
            });
            Host.Busy.Hide();
            _busy = false;
            Host.Toast(message, ok ? "success" : "warning");
            Refresh();
        }

        private void DisableClicked(object sender, RoutedEventArgs e)
        {
            if (Host == null) return;
            Host.Dialog.Confirm("Desativar a extensão?",
                "O ExplorerBlurMica será desregistrado e o Explorador será reiniciado sem o efeito. " +
                "O DLL e o config.ini continuam no disco, então você pode instalar de novo depois.",
                "Desativar e reiniciar", true, delegate { RunUninstall(false); });
        }

        private void RemoveClicked(object sender, RoutedEventArgs e)
        {
            if (Host == null) return;
            Host.Dialog.Confirm("Remover completamente?",
                "O ExplorerBlurMica será desregistrado e o DLL, config.ini e arquivos de licença serão apagados. " +
                "Os backups em " + Paths.BackupsDir + " são mantidos.",
                "Remover tudo", true, delegate { RunUninstall(true); });
        }

        private async void RunUninstall(bool removeFiles)
        {
            if (Host == null || _busy) return;
            _busy = true;
            _disableButton.IsEnabled = false;
            _removeButton.IsEnabled = false;
            Host.Busy.Show(removeFiles ? "Removendo a extensão..." : "Desativando a extensão...",
                "O Explorador será reiniciado.");

            Progress<string> progress = new Progress<string>(delegate(string message)
            {
                if (Host != null) Host.Busy.Update(message);
            });

            InstallReport report = await Task.Run(delegate
            {
                return EngineManager.Uninstall(removeFiles, progress);
            });

            Host.Busy.Hide();
            _busy = false;
            _disableButton.IsEnabled = true;
            _removeButton.IsEnabled = true;

            if (report.Success)
            {
                Host.Toast(report.Message, "success");
                Host.Dialog.Alert(removeFiles ? "Extensão removida" : "Extensão desativada",
                    report.Message + " Se o Explorador não abrir sozinho, use Reiniciar o Explorer ou Win+R > explorer.exe.",
                    "Entendi");
            }
            else if (report.Cancelled)
            {
                Host.Toast("Operação cancelada. Nada foi alterado.", "info");
            }
            else
            {
                Host.Toast(report.Message, "error");
                Host.Dialog.Alert("Não foi possível concluir", report.Message + " Tente reiniciar o Explorador e repetir.", "Entendi");
            }

            Refresh();
            Host.RefreshShellStatus();
        }

        private async void RestartClicked(object sender, RoutedEventArgs e)
        {
            if (Host == null || _busy) return;
            _busy = true;
            Host.Busy.Show("Reiniciando o Explorer...", "As janelas abertas serão fechadas e reabertas.");
            await Task.Run(delegate
            {
                EngineManager.KillExplorer();
                System.Threading.Thread.Sleep(1200);
                EngineManager.StartExplorer();
                System.Threading.Thread.Sleep(2200);
            });
            Host.Busy.Hide();
            _busy = false;
            bool running = Native.IsExplorerRunning();
            Host.Toast(running ? "Explorador reiniciado." : "O Explorer não voltou. Tente Win+R > explorer.exe.",
                running ? "success" : "warning");
            Refresh();
            Host.RefreshShellStatus();
        }
    }
}
