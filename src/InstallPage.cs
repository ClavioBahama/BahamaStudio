using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace BahamaStudio
{
    public sealed class InstallPage : PageBase
    {
        private readonly AppSettings _fallback = AppSettings.Load();
        private EngineStatus _status;
        private bool _busy;

        private readonly Border _statusCard;
        private readonly Border _statusBadge;
        private readonly TextBlock _statusGlyph;
        private readonly TextBlock _statusTitle;
        private readonly TextBlock _statusBody;
        private readonly TextBlock _statusFoot;

        private readonly FactRow _factInstalled;
        private readonly FactRow _factBundled;
        private readonly FactRow _factTarget;
        private readonly FactRow _factConfig;

        private readonly ToggleRow _grantToggle;
        private readonly ToggleRow _validateToggle;
        private readonly Button _installButton;
        private readonly Border _progressCard;
        private readonly StackPanel _progressList;
        private readonly TextBlock _progressTitle;
        private readonly Border _successCard;
        private readonly TextBlock _successTitle;

        public InstallPage()
        {
            _statusGlyph = Ui.Icon(Glyphs.Download, 17, Theme.GetBrush("BrushAccent"));
            _statusBadge = new Border();
            _statusBadge.Width = 44;
            _statusBadge.Height = 44;
            _statusBadge.CornerRadius = new CornerRadius(12);
            _statusBadge.Background = Ui.Alpha(Theme.GetColor("ColorAccent"), 28);
            _statusBadge.Child = _statusGlyph;

            _statusTitle = Ui.Text("Verificando...", "TextH2");
            _statusBody = Ui.Body("");
            _statusBody.Margin = new Thickness(0, 4, 0, 0);
            _statusBody.MaxWidth = 560;

            _statusFoot = Ui.Caption("");
            _statusFoot.FontSize = 10.5;
            _statusFoot.Margin = new Thickness(0, 7, 0, 0);

            StackPanel statusText = new StackPanel();
            statusText.VerticalAlignment = VerticalAlignment.Center;
            statusText.Margin = new Thickness(15, 0, 0, 0);
            statusText.Children.Add(_statusTitle);
            statusText.Children.Add(_statusBody);
            statusText.Children.Add(_statusFoot);

            Grid statusGrid = new Grid();
            statusGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            statusGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetColumn(_statusBadge, 0);
            statusGrid.Children.Add(_statusBadge);
            Grid.SetColumn(statusText, 1);
            statusGrid.Children.Add(statusText);

            _statusCard = new Border();
            _statusCard.Style = Theme.GetStyle("Card");
            _statusCard.Padding = new Thickness(20);
            _statusCard.Child = statusGrid;

            _factInstalled = new FactRow("Instalada", "—");
            _factBundled = new FactRow("Incluída", "—");
            _factTarget = new FactRow("Destino", "—");
            _factConfig = new FactRow("Configuração", "—");

            AppSettings settings = Settings;
            _grantToggle = new ToggleRow("Ajustes de cor sem UAC",
                "Libera a gravação do config.ini para o seu usuário. Você troca as cores sem confirmação a cada teste.",
                settings.GrantQuickApply);
            _validateToggle = new ToggleRow("Abrir pasta de teste",
                "Depois de instalar, abre uma pasta e confirma se o Explorer carregou o efeito sem falhar.",
                settings.ValidateFolder);

            _grantToggle.Changed += delegate(bool value)
            {
                AppSettings s = Settings;
                s.GrantQuickApply = value;
                s.Save();
            };
            _validateToggle.Changed += delegate(bool value)
            {
                AppSettings s = Settings;
                s.ValidateFolder = value;
                s.Save();
            };

            _installButton = Ui.Btn("Instalar engine 2.1.0", Glyphs.Download, "BtnPrimary", InstallClicked);

            _progressTitle = Ui.Caption("");
            _progressTitle.FontSize = 11.5;
            _progressList = new StackPanel();
            _progressList.Margin = new Thickness(0, 8, 0, 0);

            StackPanel progressContent = new StackPanel();
            progressContent.Children.Add(_progressTitle);
            progressContent.Children.Add(_progressList);
            _progressCard = Ui.CardSoft(progressContent);
            _progressCard.Margin = new Thickness(0, 14, 0, 0);
            _progressCard.Visibility = Visibility.Collapsed;

            _successTitle = Ui.Text("Instalação concluída", "TextH2");
            StackPanel successContent = new StackPanel();
            successContent.Children.Add(_successTitle);
            TextBlock successBody = Ui.Body("A aparência já está ativa no Explorador. Você pode ajustar o efeito e as cores quando quiser.");
            successBody.Margin = new Thickness(0, 5, 0, 0);
            successContent.Children.Add(successBody);

            StackPanel successButtons = new StackPanel();
            successButtons.Orientation = Orientation.Horizontal;
            successButtons.Margin = new Thickness(0, 13, 0, 0);
            Button adjust = Ui.Btn("Ajustar aparência", Glyphs.Palette, "BtnPrimary", delegate
            {
                if (Host != null) Host.Navigate("aparencia");
            });
            Button test = Ui.Btn("Abrir uma pasta", Glyphs.Folder, "BtnGhost", delegate
            {
                Native.OpenFolder(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            });
            test.Margin = new Thickness(9, 0, 0, 0);
            successButtons.Children.Add(adjust);
            successButtons.Children.Add(test);
            successContent.Children.Add(successButtons);

            _successCard = new Border();
            _successCard.Background = Ui.SB("#10241C");
            _successCard.BorderBrush = Ui.Alpha(Theme.GetColor("ColorSuccess"), 90);
            _successCard.BorderThickness = new Thickness(1);
            _successCard.CornerRadius = new CornerRadius(12);
            _successCard.Padding = new Thickness(20);
            _successCard.Margin = new Thickness(0, 16, 0, 0);
            _successCard.Visibility = Visibility.Collapsed;
            _successCard.Child = successContent;

            Children.Add(Build());
            Refresh();
        }

        private AppSettings Settings
        {
            get { return Host != null && Host.Settings != null ? Host.Settings : _fallback; }
        }

        private FrameworkElement Build()
        {
            Grid root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Button diagnose = Ui.Btn("Diagnóstico", Glyphs.Diagnostic, "BtnGhost", delegate
            {
                if (Host != null) Host.Navigate("diagnostico");
            });
            FrameworkElement header = Ui.PageHeader("Instalação", "Colocar o vidro no Explorer",
                "Um fluxo guiado que verifica, instala, testa e desfaz sozinho se algo sair errado. Você acompanha cada etapa.",
                diagnose);
            header.Margin = new Thickness(34, 26, 34, 18);
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            StackPanel content = new StackPanel();
            content.Margin = new Thickness(34, 0, 34, 28);

            content.Children.Add(_statusCard);
            Ui.AddMargin(_statusCard, 0, 0, 0, 18);

            content.Children.Add(BuildSafetyCard());
            Ui.AddMargin(content.Children[content.Children.Count - 1], 0, 0, 0, 22);

            content.Children.Add(Ui.SectionTitle("O que acontece quando você instala",
                "Quatro etapas automáticas. Você só confirma o UAC quando o Windows pedir."));
            content.Children.Add(BuildSteps());
            Ui.AddMargin(content.Children[content.Children.Count - 1], 0, 0, 0, 22);

            content.Children.Add(Ui.SectionTitle("Opções", "Valem apenas para esta instalação. Você pode mudar depois."));
            content.Children.Add(BuildOptionsCard());
            Ui.AddMargin(content.Children[content.Children.Count - 1], 0, 0, 0, 22);

            content.Children.Add(Ui.SectionTitle("Resumo", "O que será gravado e onde."));
            content.Children.Add(BuildSummaryCard());

            content.Children.Add(_successCard);
            content.Children.Add(_progressCard);

            ScrollViewer scroll = Ui.Scroll(content, 6);
            Grid.SetRow(scroll, 1);
            root.Children.Add(scroll);
            return root;
        }

        private FrameworkElement BuildSafetyCard()
        {
            Border badge = new Border();
            badge.Width = 36;
            badge.Height = 36;
            badge.CornerRadius = new CornerRadius(10);
            badge.Background = Ui.Alpha(Theme.GetColor("ColorSuccess"), 26);
            badge.Child = Ui.Icon(Glyphs.Shield, 15, Theme.GetBrush("BrushSuccess"));
            badge.VerticalAlignment = VerticalAlignment.Top;

            StackPanel text = new StackPanel();
            text.Margin = new Thickness(13, 0, 0, 0);
            TextBlock title = Ui.Text("Projetado para não derrubar o seu Explorer", "TextBodyBright");
            title.FontSize = 12.5;
            title.FontWeight = FontWeights.SemiBold;
            text.Children.Add(title);
            TextBlock body = Ui.Caption("Esta engine 2.1.0 corrige a falha que travava o Explorador no Windows 11 24H2. " +
                                        "Antes de instalar, o Bahama Studio cria um backup do DLL e do config.ini. Depois do teste, " +
                                        "se o Explorer falhar ou não abrir uma pasta, a extensão é desregistrada e o Explorer reiniciado automaticamente.");
            body.Margin = new Thickness(0, 4, 0, 0);
            body.FontSize = 11.5;
            text.Children.Add(body);

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetColumn(badge, 0);
            grid.Children.Add(badge);
            Grid.SetColumn(text, 1);
            grid.Children.Add(text);

            Border card = Ui.CardSoft(grid);
            card.BorderBrush = Ui.Alpha(Theme.GetColor("ColorSuccess"), 55);
            return card;
        }

        private FrameworkElement BuildSteps()
        {
            UniformGrid grid = new UniformGrid();
            grid.Columns = 4;

            grid.Children.Add(StepCard("1", "Verificar", "Confere a engine incluída, a versão do Windows e possíveis conflitos.", Glyphs.Search));
            grid.Children.Add(StepCard("2", "Backup", "Salva o DLL e o config.ini atuais em uma pasta com data e hora.", Glyphs.Save));
            grid.Children.Add(StepCard("3", "Instalar", "Copia, registra o COM e grava a configuração escolhida.", Glyphs.Download));
            grid.Children.Add(StepCard("4", "Testar", "Reinicia o Explorer, abre uma pasta e valida o módulo carregado.", Glyphs.Check));
            return grid;
        }

        private Border StepCard(string number, string title, string body, string glyph)
        {
            Border badge = new Border();
            badge.Width = 30;
            badge.Height = 30;
            badge.CornerRadius = new CornerRadius(9);
            badge.Background = Ui.Alpha(Theme.GetColor("ColorAccent"), 24);
            TextBlock num = new TextBlock();
            num.Text = number;
            num.FontSize = 12;
            num.FontWeight = FontWeights.Bold;
            num.Foreground = Theme.GetBrush("BrushAccent");
            num.HorizontalAlignment = HorizontalAlignment.Center;
            num.VerticalAlignment = VerticalAlignment.Center;
            badge.Child = num;

            TextBlock heading = Ui.Text(title, "TextBodyBright");
            heading.FontSize = 12;
            heading.FontWeight = FontWeights.SemiBold;
            heading.Margin = new Thickness(0, 9, 0, 0);

            TextBlock text = Ui.Caption(body);
            text.FontSize = 10.5;
            text.Margin = new Thickness(0, 4, 0, 0);
            text.LineHeight = 15.5;

            StackPanel content = new StackPanel();
            content.Children.Add(badge);
            content.Children.Add(heading);
            content.Children.Add(text);

            Border card = Ui.CardSoft(content);
            card.Padding = new Thickness(15);
            card.Margin = new Thickness(0, 0, 9, 0);
            return card;
        }

        private FrameworkElement BuildOptionsCard()
        {
            StackPanel content = new StackPanel();
            content.Children.Add(_grantToggle);
            content.Children.Add(Ui.Divider(12, 12));
            content.Children.Add(_validateToggle);
            return Ui.Card(content);
        }

        private FrameworkElement BuildSummaryCard()
        {
            StackPanel facts = new StackPanel();
            facts.Children.Add(_factInstalled);
            facts.Children.Add(_factBundled);
            facts.Children.Add(_factTarget);
            facts.Children.Add(_factConfig);

            StackPanel actions = new StackPanel();
            actions.Orientation = Orientation.Horizontal;
            actions.Margin = new Thickness(0, 14, 0, 0);
            actions.Children.Add(_installButton);

            Button emergency = Ui.Btn("Desativar ou remover", Glyphs.Power, "BtnGhost", delegate
            {
                if (Host != null) Host.Navigate("emergencia");
            });
            emergency.Margin = new Thickness(10, 0, 0, 0);
            actions.Children.Add(emergency);

            TextBlock note = Ui.Caption("A engine é o ExplorerBlurMica de Maplespe, licenciado em LGPLv3. O Bahama Studio apenas gerencia a instalação.");
            note.FontSize = 10.5;
            note.Margin = new Thickness(0, 12, 0, 0);

            StackPanel content = new StackPanel();
            content.Children.Add(facts);
            content.Children.Add(actions);
            content.Children.Add(note);
            return Ui.Card(content);
        }

        public override void Refresh()
        {
            if (_busy) return;
            EngineStatus status = EngineManager.Detect();
            _status = status;

            Color accent = Theme.GetColor("ColorAccent");
            Color success = Theme.GetColor("ColorSuccess");
            Color warning = Theme.GetColor("ColorWarning");

            if (!status.IsInstalled)
            {
                _statusGlyph.Text = Glyphs.Download;
                _statusBadge.Background = Ui.Alpha(accent, 28);
                _statusGlyph.Foreground = Ui.SB(accent);
                _statusTitle.Text = "Tudo pronto para instalar";
                _statusBody.Text = "A engine 2.1.0 já vem incluída com o Bahama Studio. Não é preciso baixar nada nem editar arquivos à mão.";
                _statusFoot.Text = status.Windows.SupportsMica
                    ? "Recomendado: Windows 11 • Mica disponível • Acrílico preserva a sua cor"
                    : "Mica indisponível neste Windows • Acrílico recomendado • o resto funciona normalmente";
                _statusCard.BorderBrush = Ui.Alpha(accent, 70);
                Ui.SetButton(_installButton, "Instalar engine 2.1.0", Glyphs.Download, "BtnPrimary");
            }
            else if (!status.IsModern)
            {
                _statusGlyph.Text = Glyphs.Warning;
                _statusBadge.Background = Ui.Alpha(warning, 28);
                _statusGlyph.Foreground = Ui.SB(warning);
                _statusTitle.Text = "Atualização necessária";
                _statusBody.Text = "A versão instalada (" + status.InstalledVersion + ") pode ser a causa de travamentos no Windows 11 24H2. " +
                                   "Atualize para a 2.1.0 mantendo as suas cores atuais.";
                _statusFoot.Text = "O backup da versão antiga é feito antes da troca.";
                _statusCard.BorderBrush = Ui.Alpha(warning, 70);
                Ui.SetButton(_installButton, "Atualizar para 2.1.0", Glyphs.Refresh, "BtnPrimary");
            }
            else
            {
                _statusGlyph.Text = Glyphs.Check;
                _statusBadge.Background = Ui.Alpha(success, 28);
                _statusGlyph.Foreground = Ui.SB(success);
                _statusTitle.Text = "Engine instalada e pronta";
                _statusBody.Text = "A engine 2.1.0 está registrada neste computador" +
                                   (status.EngineLoaded ? " e já está carregada no Explorador." : ". Ela será carregada na próxima pasta que você abrir.");
                _statusFoot.Text = "Se algo parecer estranho, reinstale: o processo é seguro e mantém as suas cores.";
                _statusCard.BorderBrush = Ui.Alpha(success, 70);
                Ui.SetButton(_installButton, "Reinstalar engine", Glyphs.Refresh, "BtnSecondary");
            }

            _factInstalled.SetValue(
                status.IsInstalled ? "v" + status.InstalledVersion + " • " + (status.IsModern ? "atualizada" : "desatualizada") : "não detectada",
                status.IsInstalled ? (status.IsModern ? Theme.GetBrush("BrushSuccess") : Theme.GetBrush("BrushWarning")) : Theme.GetBrush("BrushMuted"));
            _factBundled.SetValue("v" + (string.IsNullOrEmpty(status.BundledVersion) ? "?" : status.BundledVersion) +
                                  (status.MatchesBundled ? " • idêntica à instalada" : " • incluída no app"),
                status.BundledExists ? Theme.GetBrush("BrushText") : Theme.GetBrush("BrushDanger"));
            _factTarget.SetValue(status.IsInstalled && !string.IsNullOrEmpty(status.RegisteredPath)
                ? status.RegisteredPath
                : Paths.DefaultTarget);

            string configValue;
            Brush configBrush;
            if (!status.IsInstalled)
            {
                configValue = "será criada na instalação";
                configBrush = Theme.GetBrush("BrushMuted");
            }
            else if (status.ConfigExists && status.ConfigWritable)
            {
                configValue = status.ConfigPath + " • gravação liberada";
                configBrush = Theme.GetBrush("BrushSuccess");
            }
            else if (status.ConfigExists)
            {
                configValue = status.ConfigPath + " • pede UAC";
                configBrush = Theme.GetBrush("BrushWarning");
            }
            else
            {
                configValue = status.ConfigPath + " • ausente";
                configBrush = Theme.GetBrush("BrushWarning");
            }
            _factConfig.SetValue(configValue, configBrush);
        }

        private void InstallClicked(object sender, RoutedEventArgs e)
        {
            if (_busy || Host == null) return;
            EngineStatus status = _status ?? EngineManager.Detect();
            bool reinstall = status.IsInstalled && status.IsModern;
            bool upgrade = status.IsInstalled && !status.IsModern;

            string title = reinstall ? "Reinstalar a engine?" : (upgrade ? "Atualizar a engine?" : "Instalar a engine agora?");
            string message;
            if (reinstall)
                message = "A engine será copiada novamente e registrada. O Explorador será reiniciado duas vezes. " +
                          "Suas cores atuais são mantidas e um backup é criado antes.";
            else if (upgrade)
                message = "Vamos substituir a versão " + status.InstalledVersion + " pela 2.1.0, que corrige a falha no Windows 11 24H2. " +
                          "As suas cores são preservadas e a versão antiga fica no backup.";
            else
                message = "Vamos copiar a engine 2.1.0 para " + Paths.DefaultTarget + ", registrar a extensão no Windows e testar o Explorador. " +
                          "O Windows vai pedir confirmação de administrador (UAC) uma vez.";

            Host.Dialog.Confirm(title, message, reinstall ? "Reinstalar" : (upgrade ? "Atualizar" : "Instalar"), false,
                delegate { RunInstall(); });
        }

        private async void RunInstall()
        {
            if (_busy || Host == null) return;
            _busy = true;
            _installButton.IsEnabled = false;
            _successCard.Visibility = Visibility.Collapsed;
            _progressCard.Visibility = Visibility.Visible;
            _progressList.Children.Clear();
            _progressTitle.Text = "Preparando a instalação...";

            AppSettings settings = Settings;
            EngineConfig config = EngineManager.LoadActiveConfig();

            Host.Busy.Show("Instalando a engine...", "Verificando os arquivos incluídos.");
            Progress<string> progress = new Progress<string>(delegate(string message)
            {
                _progressTitle.Text = message;
                if (Host != null) Host.Busy.Update(message);
            });

            bool grant = settings.GrantQuickApply;
            bool validate = settings.ValidateFolder;

            InstallReport report = await Task.Run(delegate
            {
                return EngineManager.Install(config, grant, validate, progress);
            });

            if (Host != null) Host.Busy.Hide();
            _busy = false;
            _installButton.IsEnabled = true;

            _progressList.Children.Clear();
            for (int i = 0; i < report.Steps.Count; i++) AddProgressRow(report.Steps[i], true);

            if (report.Success)
            {
                _progressTitle.Text = "Concluído";
                _progressCard.Visibility = Visibility.Collapsed;
                _successCard.Visibility = Visibility.Visible;
                _successTitle.Text = report.ModuleLoaded ? "Instalação concluída e carregada" : "Instalação concluída";
                if (Host != null) Host.Toast(report.Message, "success");
            }
            else if (report.Cancelled)
            {
                _progressTitle.Text = "Cancelado";
                if (Host != null) Host.Toast("Instalação cancelada. Nada foi alterado.", "info");
            }
            else
            {
                _progressTitle.Text = report.RolledBack ? "Reversão aplicada" : "Não foi possível concluir";
                AddProgressRow(report.Message, false);
                if (Host != null)
                {
                    Host.Toast(report.RolledBack ? "Reversão automática aplicada." : "A instalação não foi concluída.",
                        report.RolledBack ? "warning" : "error");
                    Host.Dialog.Alert(report.RolledBack ? "Segurança acionada" : "Instalação interrompida",
                        report.Message + (report.RolledBack ? " O Explorer foi reiniciado sem a extensão e seus arquivos estão intactos." : "") +
                        " Você pode tentar de novo ou usar a página Emergência.", "Entendi");
                }
            }

            Refresh();
            if (Host != null) Host.RefreshShellStatus();
        }

        private void AddProgressRow(string text, bool success)
        {
            Grid row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.Margin = new Thickness(0, 0, 0, 6);

            TextBlock icon = Ui.Icon(success ? Glyphs.Check : Glyphs.Warning, 11,
                success ? Theme.GetBrush("BrushSuccess") : Theme.GetBrush("BrushWarning"));
            icon.VerticalAlignment = VerticalAlignment.Top;
            icon.Margin = new Thickness(0, 2, 0, 0);
            Grid.SetColumn(icon, 0);
            row.Children.Add(icon);

            TextBlock label = Ui.Caption(text);
            label.FontSize = 11;
            label.Margin = new Thickness(8, 0, 0, 0);
            Grid.SetColumn(label, 1);
            row.Children.Add(label);

            _progressList.Children.Add(row);
        }
    }
}
