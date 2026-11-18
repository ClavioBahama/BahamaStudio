using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BahamaStudio
{
    public sealed class AppearancePage : PageBase
    {
        private EngineConfig _config;
        private bool _dirty;
        private bool _sync;
        private bool _editingDark;
        private bool _previewDark;
        private bool _configWritable = true;
        private bool _installed;
        private bool _supportsMica;
        private bool _nativeTransparencySupported;

        private readonly PreviewExplorer _preview = new PreviewExplorer();
        private readonly List<EffectCard> _cards = new List<EffectCard>();
        private readonly Slider _r;
        private readonly Slider _g;
        private readonly Slider _b;
        private readonly Slider _a;
        private readonly TextBlock _rValue;
        private readonly TextBlock _gValue;
        private readonly TextBlock _bValue;
        private readonly TextBlock _aValue;
        private readonly Slider _totalLevel;
        private readonly TextBlock _totalLevelValue;
        private readonly Border _swatch;
        private readonly TextBlock _colorLabel;
        private readonly ToggleRow _addrToggle;
        private readonly ToggleRow _barToggle;
        private readonly ToggleRow _winuiToggle;
        private readonly ToggleRow _lineToggle;
        private readonly ToggleRow _totalToggle;
        private readonly TextBlock _summary;
        private readonly TextBlock _hint;
        private readonly Border _dirtyChip;
        private readonly TextBlock _dirtyText;
        private readonly Button _apply;
        private readonly Button _save;
        private readonly TextBlock _effectNote;
        private readonly TextBlock _alphaNote;
        private Border _glassCard;
        private readonly Border _glassChip;
        private readonly TextBlock _glassChipText;
        private Button _editLight;
        private Button _editDark;
        private Button _viewLight;
        private Button _viewDark;

        public AppearancePage()
        {
            _config = EngineManager.LoadActiveConfig();
            _supportsMica = Native.GetWindowsInfo().SupportsMica;
            _nativeTransparencySupported = ExplorerTransparency.IsSupported;
            NormalizeConfig();
            _previewDark = Native.IsSystemDarkMode();
            _editingDark = _previewDark;

            _r = NewSlider();
            _g = NewSlider();
            _b = NewSlider();
            _a = NewSlider();
            _rValue = NewValue();
            _gValue = NewValue();
            _bValue = NewValue();
            _aValue = NewValue();
            _totalLevelValue = NewValue();
            _totalLevelValue.Text = LevelText(_config.TotalTransparencyLevel);
            _totalLevel = NewLevelSlider();
            _totalLevel.ValueChanged += TotalLevelChanged;

            _swatch = new Border();
            _swatch.Width = 52;
            _swatch.Height = 52;
            _swatch.CornerRadius = new CornerRadius(11);
            _swatch.BorderThickness = new Thickness(1);
            _swatch.BorderBrush = Ui.SB("#40FFFFFF");
            _swatch.VerticalAlignment = VerticalAlignment.Center;

            _colorLabel = Ui.Text("", "TextMono");
            _colorLabel.FontSize = 11;

            _addrToggle = new ToggleRow("Limpar barra de endereço",
                "Remove o fundo sólido atrás do caminho e da pesquisa.", _config.ClearAddress);
            _barToggle = new ToggleRow("Limpar fundo da barra de rolagem",
                "Deixa a barra de rolagem transparente, no estilo do vidro.", _config.ClearBarBg);
            _winuiToggle = new ToggleRow("Limpar barra de ferramentas WinUI",
                "Remove o fundo da barra de ferramentas nova do Windows 11.", _config.ClearWinUIBg);
            _lineToggle = new ToggleRow("Mostrar linha divisória",
                "Mantém a linha entre a árvore de pastas e o painel de conteúdo.", _config.ShowLine);
            _totalToggle = new ToggleRow("Transparência total",
                _nativeTransparencySupported
                    ? "Alfa 0 no fundo da janela: sem tinta e sem cor por cima do papel de parede."
                    : "Indisponível no Windows 10: o reaplicador nativo deixa o Explorador branco/preto.",
                _nativeTransparencySupported && _config.TotalTransparency);
            _totalToggle.IsEnabled = _nativeTransparencySupported;
            _totalToggle.ToolTip = _nativeTransparencySupported
                ? "Mantém o vidro visível enquanto a app está aberta."
                : "Disponível apenas no Windows 11. No Windows 10 use o cartão Vidro totalmente transparente e Aplicar no Explorer.";
            _addrToggle.Changed += delegate(bool value) { _config.ClearAddress = value; MarkDirty(); };
            _barToggle.Changed += delegate(bool value) { _config.ClearBarBg = value; MarkDirty(); };
            _winuiToggle.Changed += delegate(bool value) { _config.ClearWinUIBg = value; MarkDirty(); };
            _lineToggle.Changed += delegate(bool value) { _config.ShowLine = value; MarkDirty(); };
            _totalToggle.Changed += delegate(bool value)
            {
                if (!_nativeTransparencySupported) return;
                _config.TotalTransparency = value;
                ExplorerTransparency.SetEnabled(value, _config.TotalTransparencyLevel);
                MarkDirty();
            };
            _summary = Ui.Text("", "TextBodyBright");
            _summary.FontSize = 12;
            _summary.FontWeight = FontWeights.SemiBold;

            _hint = Ui.Caption("");
            _hint.FontSize = 11;
            _hint.Margin = new Thickness(0, 4, 0, 0);

            _dirtyText = new TextBlock();
            _dirtyText.Style = Theme.GetStyle("ChipText");
            _dirtyChip = new Border();
            _dirtyChip.Style = Theme.GetStyle("Chip");
            _dirtyChip.Child = _dirtyText;

            _apply = Ui.Btn("Aplicar no Explorer", Glyphs.Check, "BtnPrimary", ApplyClicked);
            _save = Ui.Btn("Salvar sem reiniciar", Glyphs.Save, "BtnGhost", SaveClicked);
            _save.Margin = new Thickness(10, 0, 0, 0);

            _effectNote = Ui.Caption("");
            _effectNote.FontSize = 11;
            _effectNote.TextWrapping = TextWrapping.Wrap;
            _alphaNote = Ui.Caption("Nível 0 remove toda a tinta (RGBA 0,0,0,0) e mantém o desfoque. 255 deixa a cor quase sólida.");
            _glassChipText = new TextBlock();
            _glassChip = new Border();
            _glassCard = null;
            _alphaNote.FontSize = 10.5;
            _alphaNote.TextWrapping = TextWrapping.Wrap;

            Children.Add(Build());
            Refresh();
        }

        private void NormalizeConfig()
        {
            if (_config.Effect != 0 && _config.Effect != 1 && _config.Effect != 2 && _config.Effect != 3 && _config.Effect != 4)
            {
                _config.Effect = 1;
                _dirty = true;
            }
            if (_config.TotalTransparencyLevel < 0)
            {
                _config.TotalTransparencyLevel = 0;
                _dirty = true;
            }
            if (_config.TotalTransparencyLevel > 100)
            {
                _config.TotalTransparencyLevel = 100;
                _dirty = true;
            }
        }

        private FrameworkElement Build()
        {
            Grid root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Button reset = Ui.Btn("Restaurar padrões", Glyphs.Undo, "BtnGhost", ResetClicked);
            FrameworkElement header = Ui.PageHeader("Aparência",
                "Ajuste o vidro do seu jeito",
                "Escolha o efeito, mexa nas cores e veja o Explorador antes de aplicar. Nada é alterado até você confirmar.",
                reset);
            header.Margin = new Thickness(34, 26, 34, 18);
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            Grid body = Ui.Columns(Ui.Star(1), Ui.Px(18), Ui.Px(500));
            body.Margin = new Thickness(34, 0, 34, 26);
            Grid.SetRow(body, 1);
            root.Children.Add(body);

            ScrollViewer leftScroll = Ui.Scroll(BuildLeft(), 8);
            Grid.SetColumn(leftScroll, 0);
            body.Children.Add(leftScroll);

            ScrollViewer rightScroll = Ui.Scroll(BuildRight(), 8);
            Grid.SetColumn(rightScroll, 2);
            body.Children.Add(rightScroll);

            return root;
        }

        private FrameworkElement BuildLeft()
        {
            StackPanel panel = new StackPanel();
            panel.Margin = new Thickness(0, 0, 4, 10);

            panel.Children.Add(Ui.SectionTitle("Efeito de vidro", "O Acrílico é o que melhor respeita a cor que você escolher. Mica e Mica Alt usam as cores do Windows."));

            StackPanel cards = new StackPanel();
            AddEffectCard(cards, 1, "Acrílico", EngineConfig.EffectDescription(1), true);
            AddEffectCard(cards, 2, "Mica", EngineConfig.EffectDescription(2), false);
            AddEffectCard(cards, 3, "Blur puro", EngineConfig.EffectDescription(3), false);
            AddEffectCard(cards, 4, "Mica Alt", EngineConfig.EffectDescription(4), false);
            AddEffectCard(cards, 0, "Blur (legado)", EngineConfig.EffectDescription(0), false);
            panel.Children.Add(cards);

            _effectNote.Margin = new Thickness(2, 2, 0, 0);
            panel.Children.Add(_effectNote);

            if (!_supportsMica)
            {
                TextBlock micaNote = Ui.Caption("Este Windows não tem Mica nativo. Mica e Mica Alt ficam indisponíveis; use Acrílico ou Blur puro.");
                micaNote.FontSize = 10.5;
                micaNote.Foreground = Ui.SB(Theme.GetColor("ColorWarning"));
                micaNote.TextWrapping = TextWrapping.Wrap;
                micaNote.Margin = new Thickness(2, 4, 0, 0);
                panel.Children.Add(micaNote);
            }

            panel.Children.Add(new Border { Height = 14 });
            panel.Children.Add(BuildGlassModeCard());

            panel.Children.Add(new Border { Height = 18 });
            panel.Children.Add(BuildTotalTransparencyCard());

            panel.Children.Add(Ui.SectionTitle("Personalização manual", "Escolha a transparência da cor, o tom RGB e veja o resultado na prévia ao lado."));
            panel.Children.Add(BuildColorCard());
            panel.Children.Add(new Border { Height = 18 });
            panel.Children.Add(Ui.SectionTitle("Detalhes do Explorador", "Ajustes finos que combinam com o efeito escolhido."));
            panel.Children.Add(BuildDetailsCard());

            return panel;
        }

        private void AddEffectCard(StackPanel parent, int id, string name, string description, bool recommended)
        {
            if (!_supportsMica && (id == 2 || id == 4)) description += " — indisponível neste Windows";
            EffectCard card = new EffectCard(id, name, description, recommended);
            if (!_supportsMica && (id == 2 || id == 4)) card.IsEnabled = false;
            card.Checked += delegate
            {
                if (_sync) return;
                _config.Effect = id;
                MarkDirty();
            };
            _cards.Add(card);
            parent.Children.Add(card);
        }

        private FrameworkElement BuildGlassModeCard()
        {
            StackPanel content = new StackPanel();

            Grid header = Ui.Columns(Ui.Auto(), Ui.Star(1), GridLength.Auto);

            Border badge = new Border();
            badge.Width = 36;
            badge.Height = 36;
            badge.CornerRadius = new CornerRadius(10);
            badge.Background = Ui.Alpha(Theme.GetColor("ColorAccent"), 30);
            badge.VerticalAlignment = VerticalAlignment.Center;
            badge.Child = Ui.Icon(Glyphs.Palette, 16, Theme.GetBrush("BrushAccent"));
            Grid.SetColumn(badge, 0);
            header.Children.Add(badge);

            StackPanel titles = new StackPanel();
            titles.Margin = new Thickness(11, 0, 8, 0);
            titles.VerticalAlignment = VerticalAlignment.Center;
            TextBlock title = Ui.Text("Vidro totalmente transparente", "TextBodyBright");
            title.FontSize = 13;
            title.FontWeight = FontWeights.SemiBold;
            titles.Children.Add(title);
            TextBlock subtitle = Ui.Caption("Opacidade zero • RGBA(0, 0, 0, 0)");
            subtitle.FontSize = 10.5;
            subtitle.Margin = new Thickness(0, 2, 0, 0);
            titles.Children.Add(subtitle);
            Grid.SetColumn(titles, 1);
            header.Children.Add(titles);

            _glassChip.CornerRadius = new CornerRadius(9);
            _glassChip.Padding = new Thickness(8, 3, 8, 3);
            _glassChip.VerticalAlignment = VerticalAlignment.Center;
            _glassChipText.FontSize = 8.5;
            _glassChipText.FontWeight = FontWeights.Bold;
            _glassChip.Child = _glassChipText;
            Grid.SetColumn(_glassChip, 2);
            header.Children.Add(_glassChip);

            content.Children.Add(header);

            TextBlock description = Ui.Caption(
                "Remove toda a tinta do vidro e mantém o desfoque do Acrílico: é o glassmorphism puro. " +
                "O papel de parede aparece sem nenhuma camada de cor por cima, no tema claro e no escuro.");
            description.FontSize = 11;
            description.Margin = new Thickness(0, 11, 0, 0);
            content.Children.Add(description);

            Button activate = Ui.Btn("Ativar vidro transparente", Glyphs.Check, "BtnSecondary",
                delegate { ApplyTransparentGlass(); });
            activate.Margin = new Thickness(0, 12, 0, 0);
            activate.HorizontalAlignment = HorizontalAlignment.Left;
            activate.ToolTip = "Aplica Acrílico com RGBA(0,0,0,0): sem tinta, só o desfoque.";
            System.Windows.Automation.AutomationProperties.SetName(activate, "Ativar vidro totalmente transparente");
            content.Children.Add(activate);

            _glassCard = Ui.Card(content);
            return _glassCard;
        }

        private FrameworkElement BuildTotalTransparencyCard()
        {
            StackPanel content = new StackPanel();

            Grid header = Ui.Columns(Ui.Auto(), Ui.Star(1), GridLength.Auto);

            Border badge = new Border();
            badge.Width = 36;
            badge.Height = 36;
            badge.CornerRadius = new CornerRadius(10);
            badge.Background = Ui.Alpha(Theme.GetColor("ColorAccent"), 30);
            badge.VerticalAlignment = VerticalAlignment.Center;
            badge.Child = Ui.Icon(Glyphs.Monitor, 16, Theme.GetBrush("BrushAccent"));
            Grid.SetColumn(badge, 0);
            header.Children.Add(badge);

            StackPanel titles = new StackPanel();
            titles.Margin = new Thickness(11, 0, 8, 0);
            titles.VerticalAlignment = VerticalAlignment.Center;
            TextBlock title = Ui.Text("Fundo do Explorador", "TextBodyBright");
            title.FontSize = 13;
            title.FontWeight = FontWeights.SemiBold;
            titles.Children.Add(title);
            TextBlock subtitle = Ui.Caption(_nativeTransparencySupported
                ? "Efeito nativo • janelas CabinetWClass"
                : "Não suportado no Windows 10");
            subtitle.FontSize = 10.5;
            subtitle.Margin = new Thickness(0, 2, 0, 0);
            titles.Children.Add(subtitle);
            Grid.SetColumn(titles, 1);
            header.Children.Add(titles);

            Border chip = Ui.Chip(_nativeTransparencySupported ? "WIN32" : "WIN 10",
                Theme.GetBrush("BrushAccent"), Theme.GetColor("ColorAccent"));
            chip.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(chip, 2);
            header.Children.Add(chip);

            content.Children.Add(header);

            TextBlock description = Ui.Caption(_nativeTransparencySupported
                ? "Zera a cor de fundo das janelas do Explorador com SetWindowCompositionAttribute " +
                  "e DwmExtendFrameIntoClientArea. O efeito é reaplicado ao ativar, restaurar, minimizar " +
                  "e ao trocar de foco, inclusive em janelas abertas depois."
                : "Este efeito usa SetWindowCompositionAttribute e margens -1 no DWM. No Windows 10 " +
                  "isso pinta o cliente de branco/preto e o vidro só reaparece ao minimizar e restaurar. " +
                  "Por isso o reaplicador fica desativado neste sistema.");
            description.FontSize = 11;
            description.Margin = new Thickness(0, 11, 0, 0);
            content.Children.Add(description);

            content.Children.Add(Ui.Divider(12, 12));
            content.Children.Add(_totalToggle);

            StackPanel levelBlock = new StackPanel();
            levelBlock.Margin = new Thickness(0, 14, 0, 0);

            Grid levelHeader = Ui.Columns(Ui.Star(1), GridLength.Auto);
            TextBlock levelTitle = Ui.Text("Nível de transparência", "TextBodyBright");
            levelTitle.FontSize = 11.5;
            levelTitle.FontWeight = FontWeights.SemiBold;
            levelTitle.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(levelTitle, 0);
            levelHeader.Children.Add(levelTitle);

            _totalLevelValue.FontSize = 11;
            _totalLevelValue.FontWeight = FontWeights.SemiBold;
            _totalLevelValue.Foreground = Theme.GetBrush("BrushAccent");
            Grid.SetColumn(_totalLevelValue, 1);
            levelHeader.Children.Add(_totalLevelValue);
            levelBlock.Children.Add(levelHeader);

            TextBlock levelCaption = Ui.Caption(
                "100% remove toda a tinta. 0% deixa o fundo sólido no tom do tema. " +
                "Os valores do meio misturam os dois extremos.");
            levelCaption.FontSize = 10.5;
            levelCaption.Margin = new Thickness(0, 3, 0, 0);
            levelBlock.Children.Add(levelCaption);

            _totalLevel.Margin = new Thickness(0, 8, 0, 2);
            _totalLevel.IsEnabled = _nativeTransparencySupported;
            System.Windows.Automation.AutomationProperties.SetName(_totalLevel, "Nível de transparência total");
            levelBlock.Children.Add(_totalLevel);
            content.Children.Add(levelBlock);

            TextBlock note = Ui.Caption(_nativeTransparencySupported
                ? "Este interruptor mantém o vidro visível enquanto a app está aberta, inclusive " +
                  "minimizada no arranque. A configuração da engine continua gravada no config.ini, " +
                  "por isso o efeito também persiste depois de fechar. Use o cartão Vidro totalmente " +
                  "transparente para aplicar Acrílico RGBA(0,0,0,0) como base persistente."
                : "No Windows 10 a alternativa estável é o cartão Vidro totalmente transparente: " +
                  "ele grava Acrílico RGBA(0,0,0,0) na engine e aplica o efeito dentro do Explorer " +
                  "sem depender do reaplicador nativo.");
            note.FontSize = 10.5;
            note.Margin = new Thickness(0, 11, 0, 0);
            note.Foreground = Theme.GetBrush("BrushWarning");
            content.Children.Add(note);

            return Ui.Card(content);
        }

        private void ApplyTransparentGlass()
        {
            _config.Effect = 1;
            // Base persistente: a engine aplica Acrílico RGBA(0,0,0,0) dentro do Explorer.
            _config.Light = new Rgba(0, 0, 0, 0);
            _config.Dark = new Rgba(0, 0, 0, 0);
            // Reaplicador: no Windows 11 24H2 o Explorer repinta o fundo, por isso o
            // "Fundo do Explorador" precisa ficar ligado. No Windows 10 ele é desativado
            // porque produz cliente branco/preto; a base acrílica basta para o efeito.
            _config.TotalTransparency = _nativeTransparencySupported;
            if (_nativeTransparencySupported) _config.TotalTransparencyLevel = 100;
            _sync = true;
            SyncAll();
            _sync = false;
            MarkDirty();
            if (Host != null) Host.Toast(_nativeTransparencySupported
                ? "Vidro transparente ativo. Use Aplicar para gravar e reaplicar no Explorador."
                : "Vidro transparente ativo. Use Aplicar para gravar o acrílico no Explorador.", "info");
        }

        private FrameworkElement BuildColorCard()
        {
            StackPanel content = new StackPanel();

            Grid header = Ui.Columns(Ui.Star(1), GridLength.Auto);
            StackPanel title = new StackPanel();
            title.Children.Add(Ui.H3("Cor do vidro"));
            TextBlock subtitle = Ui.Caption("Edite o tema claro ou escuro e veja na prévia.");
            subtitle.Margin = new Thickness(0, 2, 0, 0);
            title.Children.Add(subtitle);
            Grid.SetColumn(title, 0);
            header.Children.Add(title);

            StackPanel segments = new StackPanel();
            segments.Orientation = Orientation.Horizontal;
            _editLight = Segment("Claro", delegate { SetEditingTheme(false); });
            _editDark = Segment("Escuro", delegate { SetEditingTheme(true); });
            _editLight.Margin = new Thickness(0, 0, 6, 0);
            segments.Children.Add(_editLight);
            segments.Children.Add(_editDark);
            Grid.SetColumn(segments, 1);
            header.Children.Add(segments);
            content.Children.Add(header);

            StackPanel swatchRow = new StackPanel();
            swatchRow.Orientation = Orientation.Horizontal;
            swatchRow.Margin = new Thickness(0, 14, 0, 0);
            swatchRow.Children.Add(_swatch);
            StackPanel colorText = new StackPanel();
            colorText.Margin = new Thickness(12, 0, 0, 0);
            colorText.VerticalAlignment = VerticalAlignment.Center;
            TextBlock caption = Ui.Caption("Tema em edição");
            caption.FontSize = 10.5;
            colorText.Children.Add(caption);
            colorText.Children.Add(_colorLabel);
            swatchRow.Children.Add(colorText);
            content.Children.Add(swatchRow);

            content.Children.Add(BuildPresets());
            content.Children.Add(BuildTransparencyBlock());

            StackPanel sliders = new StackPanel();
            sliders.Margin = new Thickness(0, 12, 0, 0);
            sliders.Children.Add(SliderRow("R", _r, _rValue));
            sliders.Children.Add(SliderRow("G", _g, _gValue));
            sliders.Children.Add(SliderRow("B", _b, _bValue));
            content.Children.Add(sliders);

            Border card = Ui.Card(content);
            return card;
        }

        private FrameworkElement BuildPresets()
        {
            WrapPanel wrap = new WrapPanel();
            wrap.Margin = new Thickness(0, 14, 0, 2);
            AddPreset(wrap, "Neve", Color.FromRgb(255, 255, 255));
            AddPreset(wrap, "Carvão", Color.FromRgb(22, 25, 32));
            AddPreset(wrap, "Oceano", Color.FromRgb(34, 89, 156));
            AddPreset(wrap, "Menta", Color.FromRgb(34, 154, 116));
            AddPreset(wrap, "Lavanda", Color.FromRgb(124, 102, 222));
            AddPreset(wrap, "Rosé", Color.FromRgb(214, 96, 150));
            return wrap;
        }

        private void AddPreset(Panel parent, string name, Color color)
        {
            Button button = new Button();
            button.Style = Theme.GetStyle("BtnGhost");
            button.Height = 30;
            button.MinHeight = 0;
            button.Padding = new Thickness(9, 4, 11, 4);
            button.Margin = new Thickness(0, 0, 7, 7);
            button.Cursor = System.Windows.Input.Cursors.Hand;
            button.ToolTip = name;

            Border dot = new Border();
            dot.Width = 11;
            dot.Height = 11;
            dot.CornerRadius = new CornerRadius(6);
            dot.Background = Ui.SB(color);
            dot.BorderBrush = Ui.SB("#44FFFFFF");
            dot.BorderThickness = new Thickness(1);
            dot.VerticalAlignment = VerticalAlignment.Center;

            TextBlock label = new TextBlock();
            label.Text = name;
            label.FontSize = 10.5;
            label.FontWeight = FontWeights.SemiBold;
            label.VerticalAlignment = VerticalAlignment.Center;
            label.Margin = new Thickness(6, 0, 0, 0);

            StackPanel content = new StackPanel();
            content.Orientation = Orientation.Horizontal;
            content.Children.Add(dot);
            content.Children.Add(label);
            button.Content = content;

            button.Click += delegate
            {
                if (_editingDark) _config.Dark = new Rgba(color.R, color.G, color.B, _config.Dark.A);
                else _config.Light = new Rgba(color.R, color.G, color.B, _config.Light.A);
                SyncColorUi();
                MarkDirty();
            };
            parent.Children.Add(button);
        }

        private FrameworkElement BuildTransparencyBlock()
        {
            StackPanel block = new StackPanel();
            block.Margin = new Thickness(0, 16, 0, 0);

            Grid header = Ui.Columns(Ui.Star(1), GridLength.Auto);
            TextBlock title = Ui.Text("Transparência da cor (A)", "TextBodyBright");
            title.FontSize = 11.5;
            title.FontWeight = FontWeights.SemiBold;
            title.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(title, 0);
            header.Children.Add(title);
            _aValue.FontSize = 11;
            _aValue.FontWeight = FontWeights.SemiBold;
            _aValue.Foreground = Theme.GetBrush("BrushAccent");
            Grid.SetColumn(_aValue, 1);
            header.Children.Add(_aValue);
            block.Children.Add(header);

            TextBlock sliderCaption = Ui.Caption("Arraste até 0 para remover a tinta e manter só o desfoque, ou até 255 para uma cor quase sólida.");
            sliderCaption.FontSize = 10.5;
            sliderCaption.Margin = new Thickness(0, 3, 0, 0);
            block.Children.Add(sliderCaption);

            _a.Margin = new Thickness(0, 8, 0, 2);
            System.Windows.Automation.AutomationProperties.SetName(_a, "Transparência da cor");
            block.Children.Add(_a);

            WrapPanel presets = new WrapPanel();
            presets.Margin = new Thickness(0, 5, 0, 0);
            AddAlphaPreset(presets, "0 · sem tinta", 0);
            AddAlphaPreset(presets, "64", 64);
            AddAlphaPreset(presets, "128", 128);
            AddAlphaPreset(presets, "192", 192);
            AddAlphaPreset(presets, "255 · sólida", 255);
            AddZeroRgbaPreset(presets);
            block.Children.Add(presets);

            _alphaNote.Margin = new Thickness(0, 7, 0, 0);
            block.Children.Add(_alphaNote);

            return block;
        }

        private void AddAlphaPreset(Panel parent, string name, int alpha)
        {
            Button button = new Button();
            button.Style = Theme.GetStyle("BtnGhost");
            button.Height = 28;
            button.MinHeight = 0;
            button.Padding = new Thickness(9, 3, 10, 3);
            button.Margin = new Thickness(0, 0, 6, 6);
            button.Cursor = System.Windows.Input.Cursors.Hand;
            button.ToolTip = "Ajusta a transparência para " + alpha + ".";
            System.Windows.Automation.AutomationProperties.SetName(button, "Transparência " + name);

            TextBlock label = new TextBlock();
            label.Text = name;
            label.FontSize = 10.5;
            label.FontWeight = FontWeights.SemiBold;
            label.VerticalAlignment = VerticalAlignment.Center;
            button.Content = label;

            int value = alpha;
            button.Click += delegate
            {
                _a.Value = value;
                SyncColorUi();
                MarkDirty();
            };
            parent.Children.Add(button);
        }

        private void AddZeroRgbaPreset(Panel parent)
        {
            Button button = new Button();
            button.Style = Theme.GetStyle("BtnGhost");
            button.Height = 28;
            button.MinHeight = 0;
            button.Padding = new Thickness(9, 3, 10, 3);
            button.Margin = new Thickness(0, 0, 6, 6);
            button.Cursor = System.Windows.Input.Cursors.Hand;
            button.ToolTip = "Zera R, G, B e A do tema em edição: RGBA(0, 0, 0, 0), sem tinta.";
            System.Windows.Automation.AutomationProperties.SetName(button, "Preset RGBA 0 0 0 0");

            Border dot = new Border();
            dot.Width = 11;
            dot.Height = 11;
            dot.CornerRadius = new CornerRadius(6);
            dot.Background = Brushes.Transparent;
            dot.BorderBrush = Theme.GetBrush("BrushAccent");
            dot.BorderThickness = new Thickness(1);
            dot.VerticalAlignment = VerticalAlignment.Center;

            TextBlock label = new TextBlock();
            label.Text = "RGBA 0,0,0,0";
            label.FontSize = 10.5;
            label.FontWeight = FontWeights.SemiBold;
            label.VerticalAlignment = VerticalAlignment.Center;
            label.Margin = new Thickness(6, 0, 0, 0);

            StackPanel content = new StackPanel();
            content.Orientation = Orientation.Horizontal;
            content.Children.Add(dot);
            content.Children.Add(label);
            button.Content = content;

            button.Click += delegate { SetEditingRgba(new Rgba(0, 0, 0, 0)); };
            parent.Children.Add(button);
        }

        private FrameworkElement BuildDetailsCard()
        {
            StackPanel content = new StackPanel();
            content.Children.Add(_addrToggle);
            content.Children.Add(Ui.Divider(11, 11));
            content.Children.Add(_barToggle);
            content.Children.Add(Ui.Divider(11, 11));
            content.Children.Add(_winuiToggle);
            content.Children.Add(Ui.Divider(11, 11));
            content.Children.Add(_lineToggle);
            return Ui.Card(content);
        }

        private FrameworkElement BuildRight()
        {
            StackPanel panel = new StackPanel();
            panel.Width = 472;
            panel.Margin = new Thickness(0, 0, 4, 10);

            Border previewCard = Ui.CardRaised(BuildPreviewCard());
            panel.Children.Add(previewCard);
            Ui.AddMargin(previewCard, 0, 0, 0, 16);

            Border applyCard = Ui.Card(BuildApplyCard());
            panel.Children.Add(applyCard);
            Ui.AddMargin(applyCard, 0, 0, 0, 16);

            Border tip = Ui.CardSoft(BuildTipCard());
            panel.Children.Add(tip);

            return panel;
        }

        private FrameworkElement BuildPreviewCard()
        {
            StackPanel content = new StackPanel();

            Grid header = Ui.Columns(Ui.Star(1), GridLength.Auto);
            StackPanel title = new StackPanel();
            title.Children.Add(Ui.H3("Prévia ao vivo"));
            TextBlock subtitle = Ui.Caption("Simulação do Explorador com o efeito escolhido.");
            subtitle.Margin = new Thickness(0, 2, 0, 0);
            title.Children.Add(subtitle);
            Grid.SetColumn(title, 0);
            header.Children.Add(title);

            StackPanel segments = new StackPanel();
            segments.Orientation = Orientation.Horizontal;
            _viewLight = Segment("Claro", delegate { SetPreviewTheme(false); });
            _viewDark = Segment("Escuro", delegate { SetPreviewTheme(true); });
            _viewLight.Margin = new Thickness(0, 0, 6, 0);
            segments.Children.Add(_viewLight);
            segments.Children.Add(_viewDark);
            Grid.SetColumn(segments, 1);
            header.Children.Add(segments);
            content.Children.Add(header);

            Border shell = new Border();
            shell.CornerRadius = new CornerRadius(12);
            shell.ClipToBounds = true;
            shell.BorderBrush = Ui.SB("#2CFFFFFF");
            shell.BorderThickness = new Thickness(1);
            shell.Margin = new Thickness(0, 14, 0, 0);
            shell.Background = Ui.SB("#080D18");

            Viewbox view = new Viewbox();
            view.Stretch = Stretch.Uniform;
            view.Child = _preview;
            view.Width = 472;
            view.Height = 319;
            shell.Child = view;
            content.Children.Add(shell);

            return content;
        }

        private FrameworkElement BuildApplyCard()
        {
            StackPanel content = new StackPanel();
            content.Children.Add(_dirtyChip);

            _summary.Margin = new Thickness(0, 10, 0, 0);
            content.Children.Add(_summary);
            content.Children.Add(_hint);

            StackPanel buttons = new StackPanel();
            buttons.Orientation = Orientation.Horizontal;
            buttons.Margin = new Thickness(0, 14, 0, 0);
            buttons.Children.Add(_apply);
            buttons.Children.Add(_save);
            content.Children.Add(buttons);

            TextBlock safe = Ui.Caption("Se o Explorador falhar após aplicar, a configuração anterior é restaurada automaticamente.");
            safe.FontSize = 10.5;
            safe.Margin = new Thickness(0, 11, 0, 0);
            content.Children.Add(safe);
            return content;
        }

        private FrameworkElement BuildTipCard()
        {
            Border badge = new Border();
            badge.Width = 32;
            badge.Height = 32;
            badge.CornerRadius = new CornerRadius(9);
            badge.Background = Ui.Alpha(Theme.GetColor("ColorWarning"), 26);
            badge.Child = Ui.Icon(Glyphs.Lightbulb, 14, Theme.GetBrush("BrushWarning"));

            StackPanel header = new StackPanel();
            header.Orientation = Orientation.Horizontal;
            header.Children.Add(badge);
            TextBlock title = Ui.H3("Dicas de vidro");
            title.Margin = new Thickness(10, 0, 0, 0);
            title.VerticalAlignment = VerticalAlignment.Center;
            header.Children.Add(title);

            StackPanel content = new StackPanel();
            content.Children.Add(header);

            TextBlock text = Ui.Caption("Acrílico é o efeito que respeita a sua cor; Mica e Mica Alt usam as cores do Windows. " +
                                        "O modo Vidro totalmente transparente aplica RGBA(0,0,0,0): remove a tinta e mantém o desfoque. " +
                                        "Se o Explorador reiniciar sozinho, segure ESC ao abrir uma pasta e use a Emergência.");
            text.FontSize = 11.5;
            text.Margin = new Thickness(0, 10, 0, 0);
            content.Children.Add(text);
            return content;
        }

        private Button Segment(string text, RoutedEventHandler handler)
        {
            Button button = new Button();
            button.Style = Theme.GetStyle("BtnGhost");
            button.Height = 28;
            button.MinHeight = 0;
            button.Padding = new Thickness(12, 3, 12, 3);
            button.Cursor = System.Windows.Input.Cursors.Hand;
            TextBlock label = new TextBlock();
            label.Text = text;
            label.FontSize = 10.5;
            label.FontWeight = FontWeights.SemiBold;
            button.Content = label;
            if (handler != null) button.Click += handler;
            return button;
        }

        private static void SetSegment(Button button, bool selected)
        {
            if (button == null) return;
            if (selected)
            {
                button.Style = Theme.GetStyle("BtnSecondary");
                button.Background = Ui.Alpha(Theme.GetColor("ColorAccent"), 40);
                button.BorderBrush = Theme.GetBrush("BrushAccent");
                button.Foreground = Theme.GetBrush("BrushText");
            }
            else
            {
                button.Style = Theme.GetStyle("BtnGhost");
                button.Background = Brushes.Transparent;
                button.BorderBrush = Ui.SB("#26374F");
                button.Foreground = Theme.GetBrush("BrushMuted");
            }
        }

        private Slider NewSlider()
        {
            Slider slider = new Slider();
            slider.Style = Theme.GetStyle("SliderModern");
            slider.Minimum = 0;
            slider.Maximum = 255;
            slider.TickFrequency = 1;
            slider.IsSnapToTickEnabled = true;
            slider.ValueChanged += ColorSliderChanged;
            return slider;
        }

        private static TextBlock NewValue()
        {
            TextBlock block = new TextBlock();
            block.FontFamily = Theme.GetFont("FontMono");
            block.FontSize = 10.5;
            block.Foreground = Theme.GetBrush("BrushMuted");
            block.TextAlignment = TextAlignment.Right;
            block.VerticalAlignment = VerticalAlignment.Center;
            block.Text = "0";
            return block;
        }

        private Slider NewLevelSlider()
        {
            Slider slider = new Slider();
            slider.Style = Theme.GetStyle("SliderModern");
            slider.Minimum = 0;
            slider.Maximum = 100;
            slider.TickFrequency = 1;
            slider.IsSnapToTickEnabled = true;
            return slider;
        }

        private static string LevelText(int level)
        {
            if (level >= 100) return "100% (sem tinta)";
            if (level <= 0) return "0% (sólido)";
            return level + "%";
        }

        private FrameworkElement SliderRow(string label, Slider slider, TextBlock value)
        {
            Grid row = Ui.Columns(Ui.Px(18), Ui.Star(1), Ui.Px(42));
            row.Margin = new Thickness(0, 3, 0, 3);
            TextBlock name = new TextBlock();
            name.Text = label;
            name.FontSize = 10.5;
            name.FontWeight = FontWeights.SemiBold;
            name.Foreground = Theme.GetBrush("BrushDim");
            name.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(name, 0);
            row.Children.Add(name);
            Grid.SetColumn(slider, 1);
            row.Children.Add(slider);
            Grid.SetColumn(value, 2);
            row.Children.Add(value);
            return row;
        }

        private void ColorSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_sync) return;
            Rgba color = new Rgba((int)_r.Value, (int)_g.Value, (int)_b.Value, (int)_a.Value);
            if (_editingDark) _config.Dark = color;
            else _config.Light = color;
            SyncColorUi();
            MarkDirty();
        }

        private void TotalLevelChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_sync || _totalLevelValue == null) return;
            int level = (int)e.NewValue;
            _config.TotalTransparencyLevel = level;
            _totalLevelValue.Text = LevelText(level);
            ExplorerTransparency.SetLevel(level);
            MarkDirty();
        }

        private void SetEditingRgba(Rgba color)
        {
            if (_editingDark) _config.Dark = color;
            else _config.Light = color;
            SyncColorUi();
            MarkDirty();
        }

        private void SyncColorUi()
        {
            Rgba color = _editingDark ? _config.Dark : _config.Light;
            _sync = true;
            _r.Value = color.R;
            _g.Value = color.G;
            _b.Value = color.B;
            _a.Value = color.A;
            _sync = false;

            _rValue.Text = color.R.ToString();
            _gValue.Text = color.G.ToString();
            _bValue.Text = color.B.ToString();
            _aValue.Text = AlphaText(color.A);
            _swatch.Background = Ui.SB(Color.FromArgb((byte)color.A, (byte)color.R, (byte)color.G, (byte)color.B));
            _colorLabel.Text = color.ToHex().ToUpperInvariant() + "  •  opacidade " + AlphaText(color.A);
            SetSegment(_editLight, !_editingDark);
            SetSegment(_editDark, _editingDark);
        }

        private void SetEditingTheme(bool dark)
        {
            _editingDark = dark;
            SyncColorUi();
        }

        private void SetPreviewTheme(bool dark)
        {
            _previewDark = dark;
            SetSegment(_viewLight, !dark);
            SetSegment(_viewDark, dark);
            _preview.Update(_config, _previewDark);
        }

        private void MarkDirty()
        {
            if (_sync) return;
            _dirty = true;
            _preview.Update(_config, _previewDark);
            UpdateSummary();
            UpdateDirty();
        }

        private void UpdateSummary()
        {
            int lightPercent = (int)Math.Round(_config.Light.A * 100.0 / 255.0);
            int darkPercent = (int)Math.Round(_config.Dark.A * 100.0 / 255.0);
            bool glass = IsTransparentGlass();
            if (glass)
            {
                _summary.Text = EngineConfig.EffectName(_config.Effect) + "  •  vidro transparente  •  RGBA 0,0,0,0";
            }
            else
            {
                _summary.Text = EngineConfig.EffectName(_config.Effect) + "  •  claro " + lightPercent + "%  •  escuro " + darkPercent + "%";
            }
            _effectNote.Text = "Efeito selecionado: " + EngineConfig.EffectName(_config.Effect) + ". " +
                               EngineConfig.EffectDescription(_config.Effect);
            string colorNote = EngineConfig.EffectColorNote(_config.Effect);
            if (colorNote.Length > 0)
            {
                _effectNote.Text = _effectNote.Text + "  " + colorNote;
                _effectNote.Foreground = Theme.GetBrush("BrushWarning");
            }
            else
            {
                _effectNote.Foreground = Theme.GetBrush("BrushDim");
            }
            UpdateGlassMode(glass);
        }

        private bool IsTransparentGlass()
        {
            if (!EngineConfig.EffectUsesColor(_config.Effect)) return false;
            return _config.Light.R == 0 && _config.Light.G == 0 && _config.Light.B == 0 && _config.Light.A == 0 &&
                   _config.Dark.R == 0 && _config.Dark.G == 0 && _config.Dark.B == 0 && _config.Dark.A == 0;
        }

        private void UpdateGlassMode(bool active)
        {
            if (_glassCard == null || _glassChip == null) return;
            if (active)
            {
                Color success = Theme.GetColor("ColorSuccess");
                _glassCard.Background = Ui.Alpha(success, 18);
                _glassCard.BorderBrush = Ui.Alpha(success, 70);
                _glassChip.Background = Ui.Alpha(success, 30);
                _glassChipText.Text = "ATIVO";
                _glassChipText.Foreground = Ui.SB(success);
            }
            else
            {
                Color accent = Theme.GetColor("ColorAccent");
                _glassCard.Background = Ui.Alpha(accent, 14);
                _glassCard.BorderBrush = Ui.Alpha(accent, 58);
                _glassChip.Background = Ui.Alpha(accent, 24);
                _glassChipText.Text = "RGBA 0,0,0,0";
                _glassChipText.Foreground = Theme.GetBrush("BrushAccent");
            }
        }

        private static string AlphaText(int alpha)
        {
            int percent = (int)Math.Round(alpha * 100.0 / 255.0);
            if (alpha <= 0) return "0 (sem tinta)";
            if (alpha >= 255) return "255 (sólida)";
            return alpha + " (" + percent + "%)";
        }

        private void UpdateDirty()
        {
            if (_dirty)
            {
                Color warning = Theme.GetColor("ColorWarning");
                _dirtyText.Text = "ALTERAÇÕES NÃO APLICADAS";
                _dirtyText.Foreground = Ui.SB(warning);
                _dirtyChip.Background = Ui.Alpha(warning, 24);
                _dirtyChip.BorderBrush = Ui.Alpha(warning, 70);
                _save.IsEnabled = true;
            }
            else
            {
                Color success = Theme.GetColor("ColorSuccess");
                _dirtyText.Text = "SINCRONIZADO COM O EXPLORER";
                _dirtyText.Foreground = Ui.SB(success);
                _dirtyChip.Background = Ui.Alpha(success, 24);
                _dirtyChip.BorderBrush = Ui.Alpha(success, 70);
                _save.IsEnabled = false;
            }
            _apply.IsEnabled = true;
        }

        private void UpdateHint()
        {
            if (!_installed)
                _hint.Text = "A engine ainda não está instalada. O botão vai abrir a instalação guiada.";
            else if (!_configWritable)
                _hint.Text = "O Windows pedirá confirmação (UAC) para gravar o config.ini e reiniciar o Explorer.";
            else
                _hint.Text = "Grava o config.ini e reinicia o Explorer para carregar o efeito.";
        }

        private void ApplyClicked(object sender, RoutedEventArgs e)
        {
            ApplyAsync(true);
        }

        private void SaveClicked(object sender, RoutedEventArgs e)
        {
            ApplyAsync(false);
        }

        private async void ApplyAsync(bool restart)
        {
            if (!_dirty)
            {
                if (Host != null) Host.Toast("Nenhuma alteração pendente.", "info");
                return;
            }

            if (!_installed)
            {
                if (Host != null)
                {
                    Host.Dialog.Confirm("Instalar a engine primeiro?",
                        "A engine ExplorerBlurMica ainda não está instalada. Vamos abrir a instalação guiada com as suas cores já selecionadas.",
                        "Abrir instalador", false, delegate { Host.Navigate("instalar"); });
                }
                return;
            }

            EngineConfig config = _config.Clone();
            _apply.IsEnabled = false;
            _save.IsEnabled = false;
            Host.Busy.Show("Aplicando aparência...", "Gravando o config.ini...");

            Progress<string> progress = new Progress<string>(delegate(string message)
            {
                Host.Busy.Update(message);
            });

            ApplyReport report = await Task.Run(delegate
            {
                return EngineManager.ApplyConfig(config, restart, progress);
            });

            Host.Busy.Hide();
            _apply.IsEnabled = true;

            if (report.Success)
            {
                _dirty = false;
                UpdateDirty();
                Host.Toast(report.Message, "success");
                Refresh();
            }
            else
            {
                UpdateDirty();
                Host.Toast(report.Message, report.RolledBack ? "warning" : "error");
                Host.Dialog.Alert(report.RolledBack ? "Reversão aplicada" : "Não foi possível aplicar",
                    report.Message + (report.RolledBack ? " A configuração anterior está ativa novamente." : "") +
                    " Se precisar, use a página Emergência.", "Entendi");
                Refresh();
            }
        }

        private void ResetClicked(object sender, RoutedEventArgs e)
        {
            if (Host == null) return;
            Host.Dialog.Confirm("Restaurar os padrões?",
                "Volta para o Acrílico com as cores recomendadas do Bahama Studio (claro azulado e escuro profundo). Você ainda pode revisar antes de aplicar.",
                "Restaurar", false, delegate
                {
                    _config = EngineConfig.CreateDefault();
                    _sync = true;
                    SyncAll();
                    _sync = false;
                    MarkDirty();
                });
        }

        /// <summary>
        /// Aplica uma configuração recomendada sem gravar nada: fica pendente na página
        /// Aparência para o usuário revisar e clicar em Aplicar ou Gravar.
        /// </summary>
        public void ApplyRecommended(EngineConfig config)
        {
            if (config == null) return;
            _config = config;
            NormalizeConfig();
            _dirty = true;
            _sync = true;
            SyncAll();
            _sync = false;
            UpdateDirty();
            UpdateHint();
            UpdateSummary();
        }

        public override void Refresh()
        {
            EngineStatus status = EngineManager.Detect();
            _installed = status.IsInstalled;
            _configWritable = status.ConfigWritable;

            if (!_dirty)
            {
                _config = EngineManager.LoadActiveConfig();
                NormalizeConfig();
                _sync = true;
                SyncAll();
                _sync = false;
                UpdateDirty();
            }
            else
            {
                _preview.Update(_config, _previewDark);
            }

            UpdateHint();
            UpdateSummary();
            SetSegment(_viewLight, !_previewDark);
            SetSegment(_viewDark, _previewDark);
        }

        private void SyncAll()
        {
            for (int i = 0; i < _cards.Count; i++)
                _cards[i].SetSelected(_cards[i].EffectId == _config.Effect);

            _addrToggle.SetSilent(_config.ClearAddress);
            _barToggle.SetSilent(_config.ClearBarBg);
            _winuiToggle.SetSilent(_config.ClearWinUIBg);
            _lineToggle.SetSilent(_config.ShowLine);
            _totalToggle.SetSilent(_nativeTransparencySupported && _config.TotalTransparency);

            SyncColorUi();
            _preview.Update(_config, _previewDark);
            UpdateSummary();
            ExplorerTransparency.SetEnabled(_nativeTransparencySupported && _config.TotalTransparency,
                _config.TotalTransparencyLevel);
        }
    }
}
