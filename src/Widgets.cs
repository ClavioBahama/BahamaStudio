using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BahamaStudio
{
    public sealed class StatusTile : Border
    {
        private readonly TextBlock _value;
        private readonly TextBlock _detail;
        private readonly Border _badge;
        private readonly TextBlock _icon;

        public StatusTile(string glyph, string label)
        {
            Style = Theme.GetStyle("CardSoft");
            Padding = new Thickness(16);
            CornerRadius = new CornerRadius(12);
            Margin = new Thickness(0);

            _badge = new Border();
            _badge.Width = 32;
            _badge.Height = 32;
            _badge.CornerRadius = new CornerRadius(9);
            _badge.Background = Ui.Alpha(Ui.SB("#6EA8FF").Color, 26);
            _icon = Ui.Icon(glyph, 14, Theme.GetBrush("BrushAccent"));
            _badge.Child = _icon;

            TextBlock labelBlock = Ui.Label(label.ToUpperInvariant());
            labelBlock.FontSize = 9.5;
            labelBlock.VerticalAlignment = VerticalAlignment.Center;

            StackPanel top = new StackPanel();
            top.Orientation = Orientation.Horizontal;
            top.Children.Add(_badge);
            labelBlock.Margin = new Thickness(9, 0, 0, 0);
            top.Children.Add(labelBlock);

            _value = Ui.Text("—", "TextValue");
            _value.Margin = new Thickness(0, 10, 0, 0);
            _value.FontSize = 16.5;

            _detail = Ui.Caption("");
            _detail.Margin = new Thickness(0, 3, 0, 0);
            _detail.MaxWidth = 220;

            StackPanel content = new StackPanel();
            content.Children.Add(top);
            content.Children.Add(_value);
            content.Children.Add(_detail);
            Child = content;
        }

        public void Set(string value, string detail, Color accent)
        {
            _value.Text = value;
            _detail.Text = detail;
            _badge.Background = Ui.Alpha(accent, 26);
            _icon.Foreground = Ui.SB(accent);
        }
    }

    public sealed class FactRow : Grid
    {
        private readonly TextBlock _value;
        private readonly Brush _defaultBrush;

        public FactRow(string label, string value)
            : this(label, value, null)
        {
        }

        public FactRow(string label, string value, Brush valueBrush)
        {
            _defaultBrush = valueBrush ?? Theme.GetBrush("BrushText");
            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(124) });
            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Margin = new Thickness(0, 0, 0, 9);

            TextBlock caption = Ui.Caption(label);
            caption.VerticalAlignment = VerticalAlignment.Top;
            Grid.SetColumn(caption, 0);
            Children.Add(caption);

            _value = Ui.Text(value, "TextBodyBright");
            _value.FontSize = 12;
            _value.TextWrapping = TextWrapping.NoWrap;
            _value.TextTrimming = TextTrimming.CharacterEllipsis;
            _value.TextAlignment = TextAlignment.Right;
            _value.VerticalAlignment = VerticalAlignment.Top;
            _value.ToolTip = value;
            _value.Foreground = _defaultBrush;
            Grid.SetColumn(_value, 1);
            Children.Add(_value);
        }

        public void SetValue(string value)
        {
            SetValue(value, _defaultBrush);
        }

        public void SetValue(string value, Brush brush)
        {
            _value.Text = value;
            _value.ToolTip = value;
            _value.Foreground = brush ?? _defaultBrush;
        }
    }

    public sealed class ToggleRow : Grid
    {
        private readonly ToggleButton _toggle;
        private bool _silent;

        public ToggleRow(string title, string subtitle, bool value)
        {
            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Margin = new Thickness(0, 0, 0, 2);

            StackPanel text = new StackPanel();
            TextBlock heading = Ui.Text(title, "TextBodyBright");
            heading.FontSize = 12.5;
            heading.FontWeight = FontWeights.SemiBold;
            text.Children.Add(heading);
            if (!string.IsNullOrEmpty(subtitle))
            {
                TextBlock sub = Ui.Caption(subtitle);
                sub.Margin = new Thickness(0, 2, 0, 0);
                text.Children.Add(sub);
            }
            text.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(text, 0);
            Children.Add(text);

            _toggle = new ToggleButton();
            _toggle.Style = Theme.GetStyle("Switch");
            _toggle.IsChecked = value;
            _toggle.VerticalAlignment = VerticalAlignment.Center;
            _toggle.Margin = new Thickness(18, 0, 0, 0);
            _toggle.Checked += ToggleChanged;
            _toggle.Unchecked += ToggleChanged;
            Grid.SetColumn(_toggle, 1);
            Children.Add(_toggle);
        }

        public event Action<bool> Changed;

        public bool IsOn
        {
            get { return _toggle.IsChecked == true; }
        }

        public void SetSilent(bool value)
        {
            _silent = true;
            _toggle.IsChecked = value;
            _silent = false;
        }

        private void ToggleChanged(object sender, RoutedEventArgs e)
        {
            if (_silent) return;
            if (Changed != null) Changed(IsOn);
        }
    }

    public sealed class EffectCard : RadioButton
    {
        private readonly Border _check;
        public readonly int EffectId;

        public EffectCard(int effectId, string name, string description, bool recommended)
        {
            EffectId = effectId;
            Style = Theme.GetStyle("EffectCard");
            GroupName = "effect";
            Margin = new Thickness(0, 0, 0, 8);

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Border swatch = BuildSwatch(effectId);
            swatch.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(swatch, 0);
            grid.Children.Add(swatch);

            StackPanel text = new StackPanel();
            text.VerticalAlignment = VerticalAlignment.Center;
            text.Margin = new Thickness(12, 0, 8, 0);
            StackPanel titleRow = new StackPanel();
            titleRow.Orientation = Orientation.Horizontal;
            TextBlock title = Ui.Text(name, "TextBodyBright");
            title.FontSize = 12.5;
            title.FontWeight = FontWeights.SemiBold;
            titleRow.Children.Add(title);
            if (recommended)
            {
                Border badge = new Border();
                badge.Background = Ui.Alpha(Theme.GetColor("ColorAccent"), 30);
                badge.CornerRadius = new CornerRadius(10);
                badge.Padding = new Thickness(7, 2, 7, 2);
                badge.Margin = new Thickness(8, 0, 0, 0);
                TextBlock badgeText = new TextBlock();
                badgeText.Text = "RECOMENDADO";
                badgeText.FontSize = 8;
                badgeText.FontWeight = FontWeights.Bold;
                badgeText.Foreground = Theme.GetBrush("BrushAccent");
                badge.Child = badgeText;
                titleRow.Children.Add(badge);
            }
            text.Children.Add(titleRow);
            TextBlock desc = Ui.Caption(description);
            desc.Margin = new Thickness(0, 2, 0, 0);
            text.Children.Add(desc);
            Grid.SetColumn(text, 1);
            grid.Children.Add(text);

            _check = new Border();
            _check.Width = 22;
            _check.Height = 22;
            _check.CornerRadius = new CornerRadius(11);
            _check.Background = Ui.Alpha(Theme.GetColor("ColorAccent"), 38);
            _check.BorderBrush = Theme.GetBrush("BrushAccent");
            _check.BorderThickness = new Thickness(1);
            _check.Opacity = 0;
            _check.VerticalAlignment = VerticalAlignment.Center;
            _check.Child = Ui.Icon(Glyphs.Check, 10, Theme.GetBrush("BrushAccent"));
            Grid.SetColumn(_check, 2);
            grid.Children.Add(_check);

            Content = grid;
            Checked += delegate { SetSelected(true); };
        }

        public void SetSelected(bool selected)
        {
            _check.Opacity = selected ? 1 : 0;
            if (selected) IsChecked = true;
        }

        private static Border BuildSwatch(int effect)
        {
            Border border = new Border();
            border.Width = 54;
            border.Height = 44;
            border.CornerRadius = new CornerRadius(9);
            border.BorderThickness = new Thickness(1);
            border.BorderBrush = Ui.SB("#2AFFFFFF");
            border.ClipToBounds = true;

            LinearGradientBrush brush = new LinearGradientBrush();
            brush.StartPoint = new Point(0, 0);
            brush.EndPoint = new Point(1, 1);

            switch (effect)
            {
                case 1:
                    brush.GradientStops.Add(new GradientStop(Color.FromArgb(220, 96, 165, 250), 0));
                    brush.GradientStops.Add(new GradientStop(Color.FromArgb(220, 167, 139, 250), 0.55));
                    brush.GradientStops.Add(new GradientStop(Color.FromArgb(180, 244, 114, 182), 1));
                    break;
                case 2:
                    brush.GradientStops.Add(new GradientStop(Color.FromArgb(235, 168, 198, 232), 0));
                    brush.GradientStops.Add(new GradientStop(Color.FromArgb(225, 46, 66, 96), 0.6));
                    brush.GradientStops.Add(new GradientStop(Color.FromArgb(235, 18, 26, 40), 1));
                    break;
                case 3:
                    brush.GradientStops.Add(new GradientStop(Color.FromArgb(240, 48, 70, 102), 0));
                    brush.GradientStops.Add(new GradientStop(Color.FromArgb(245, 18, 27, 42), 1));
                    break;
                case 4:
                    brush.GradientStops.Add(new GradientStop(Color.FromArgb(235, 60, 92, 138), 0));
                    brush.GradientStops.Add(new GradientStop(Color.FromArgb(245, 10, 16, 28), 1));
                    break;
                default:
                    brush.GradientStops.Add(new GradientStop(Color.FromArgb(230, 60, 80, 110), 0));
                    brush.GradientStops.Add(new GradientStop(Color.FromArgb(240, 20, 28, 42), 1));
                    break;
            }
            border.Background = brush;

            Grid overlay = new Grid();
            Border sheen = new Border();
            sheen.Height = 20;
            sheen.VerticalAlignment = VerticalAlignment.Top;
            sheen.Background = new LinearGradientBrush(
                Color.FromArgb(70, 255, 255, 255),
                Color.FromArgb(0, 255, 255, 255),
                90);
            overlay.Children.Add(sheen);

            if (effect == 3)
            {
                Ellipse haze = new Ellipse();
                haze.Width = 34;
                haze.Height = 24;
                haze.Fill = new RadialGradientBrush(
                    Color.FromArgb(170, 255, 255, 255),
                    Color.FromArgb(0, 255, 255, 255));
                haze.HorizontalAlignment = HorizontalAlignment.Center;
                haze.VerticalAlignment = VerticalAlignment.Center;
                overlay.Children.Add(haze);
            }
            border.Child = overlay;
            return border;
        }
    }

    public sealed class ToastHost : StackPanel
    {
        public ToastHost()
        {
            Orientation = Orientation.Vertical;
            HorizontalAlignment = HorizontalAlignment.Right;
            VerticalAlignment = VerticalAlignment.Bottom;
            Margin = new Thickness(0, 0, 26, 24);
            IsHitTestVisible = false;
            MaxWidth = 430;
        }

        public void Show(string message, string kind)
        {
            Color accent;
            string glyph;
            switch (kind)
            {
                case "success": accent = Theme.GetColor("ColorSuccess"); glyph = Glyphs.Check; break;
                case "warning": accent = Theme.GetColor("ColorWarning"); glyph = Glyphs.Warning; break;
                case "error": accent = Theme.GetColor("ColorDanger"); glyph = Glyphs.Warning; break;
                default: accent = Theme.GetColor("ColorAccent"); glyph = Glyphs.Info; break;
            }

            Border card = new Border();
            card.Background = Ui.SB("#F2121B2B");
            card.BorderBrush = Ui.Alpha(accent, 110);
            card.BorderThickness = new Thickness(1);
            card.CornerRadius = new CornerRadius(11);
            card.Padding = new Thickness(14, 11, 16, 11);
            card.Margin = new Thickness(0, 8, 0, 0);
            card.Effect = new DropShadowEffect
            {
                BlurRadius = 26,
                ShadowDepth = 6,
                Opacity = 0.5,
                Color = Colors.Black
            };

            Border badge = new Border();
            badge.Width = 26;
            badge.Height = 26;
            badge.CornerRadius = new CornerRadius(8);
            badge.Background = Ui.Alpha(accent, 34);
            badge.Child = Ui.Icon(glyph, 12, Ui.SB(accent));
            badge.VerticalAlignment = VerticalAlignment.Center;

            TextBlock text = Ui.Text(message, "TextBodyBright");
            text.FontSize = 12;
            text.MaxWidth = 340;
            text.VerticalAlignment = VerticalAlignment.Center;

            StackPanel row = new StackPanel();
            row.Orientation = Orientation.Horizontal;
            row.Children.Add(badge);
            text.Margin = new Thickness(10, 0, 0, 0);
            row.Children.Add(text);
            card.Child = row;

            TranslateTransform move = new TranslateTransform(0, 8);
            card.RenderTransform = move;
            card.Opacity = 0;
            Children.Add(card);

            card.BeginAnimation(UIElement.OpacityProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(170)));
            move.BeginAnimation(TranslateTransform.YProperty,
                new DoubleAnimation(8, 0, TimeSpan.FromMilliseconds(200))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                });

            if (Children.Count > 3) Children.RemoveAt(0);

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(4200);
            timer.Tick += delegate
            {
                timer.Stop();
                card.BeginAnimation(UIElement.OpacityProperty,
                    new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(220)));
                DispatcherTimer remove = new DispatcherTimer();
                remove.Interval = TimeSpan.FromMilliseconds(240);
                remove.Tick += delegate
                {
                    remove.Stop();
                    Children.Remove(card);
                };
                remove.Start();
            };
            timer.Start();
        }
    }

    public sealed class BusyOverlay : Grid
    {
        private readonly TextBlock _title;
        private readonly TextBlock _subtitle;
        private readonly RotateTransform _rotation;

        public BusyOverlay()
        {
            Visibility = Visibility.Collapsed;
            Background = Ui.SB("#CC04070D");

            Border card = new Border();
            card.Width = 430;
            card.Style = Theme.GetStyle("CardRaised");
            card.Padding = new Thickness(26);
            card.HorizontalAlignment = HorizontalAlignment.Center;
            card.VerticalAlignment = VerticalAlignment.Center;
            card.Background = Ui.SB("#101A2A");

            Grid spinner = new Grid();
            spinner.Width = 44;
            spinner.Height = 44;
            spinner.VerticalAlignment = VerticalAlignment.Top;

            Ellipse track = new Ellipse();
            track.Stroke = Ui.SB("#22FFFFFF");
            track.StrokeThickness = 3;
            spinner.Children.Add(track);

            System.Windows.Shapes.Path arc = new System.Windows.Shapes.Path();
            arc.Data = Geometry.Parse("M 22,4 A 18,18 0 0 1 40,22");
            arc.Stroke = Theme.GetBrush("BrushAccent");
            arc.StrokeThickness = 3;
            arc.StrokeStartLineCap = PenLineCap.Round;
            arc.StrokeEndLineCap = PenLineCap.Round;
            _rotation = new RotateTransform(0, 22, 22);
            arc.RenderTransform = _rotation;
            spinner.Children.Add(arc);

            _title = Ui.Text("Trabalhando...", "TextBodyBright");
            _title.FontSize = 15;
            _title.FontWeight = FontWeights.SemiBold;
            _title.TextWrapping = TextWrapping.NoWrap;

            _subtitle = Ui.Caption("Aguarde um instante.");
            _subtitle.FontSize = 11.5;
            _subtitle.Margin = new Thickness(0, 5, 0, 0);
            _subtitle.MaxWidth = 300;

            StackPanel text = new StackPanel();
            text.VerticalAlignment = VerticalAlignment.Center;
            text.Margin = new Thickness(18, 0, 0, 0);
            text.Children.Add(_title);
            text.Children.Add(_subtitle);

            StackPanel row = new StackPanel();
            row.Orientation = Orientation.Horizontal;
            row.Children.Add(spinner);
            row.Children.Add(text);
            card.Child = row;
            Children.Add(card);
        }

        public void Show(string title, string subtitle)
        {
            _title.Text = title;
            _subtitle.Text = subtitle;
            Visibility = Visibility.Visible;
            _rotation.BeginAnimation(RotateTransform.AngleProperty,
                new DoubleAnimation(0, 360, TimeSpan.FromMilliseconds(900))
                {
                    RepeatBehavior = RepeatBehavior.Forever
                });
        }

        public void Update(string subtitle)
        {
            _subtitle.Text = subtitle;
        }

        public void Hide()
        {
            Visibility = Visibility.Collapsed;
            _rotation.BeginAnimation(RotateTransform.AngleProperty, null);
        }
    }

    public sealed class DialogHost : Grid
    {
        private Action _confirm;
        private Border _card;

        public DialogHost()
        {
            Visibility = Visibility.Collapsed;
            Background = Ui.SB("#B804070D");
        }

        public bool IsOpen
        {
            get { return Visibility == Visibility.Visible; }
        }

        public void Confirm(string title, string message, string confirmText, bool danger, Action onConfirm)
        {
            Build(title, message, "CONFIRMAÇÃO", confirmText, danger ? "BtnDangerSolid" : "BtnPrimary", onConfirm, true);
        }

        public void Alert(string title, string message, string closeText)
        {
            Build(title, message, "AVISO", closeText, "BtnPrimary", null, false);
        }

        public void Close()
        {
            Visibility = Visibility.Collapsed;
            _confirm = null;
        }

        private void Build(string title, string message, string kicker, string confirmText,
            string confirmStyle, Action onConfirm, bool cancellable)
        {
            _confirm = onConfirm;
            Children.Clear();

            _card = new Border();
            _card.Width = 490;
            _card.Style = Theme.GetStyle("CardRaised");
            _card.Padding = new Thickness(26);
            _card.Background = Ui.SB("#101A2B");
            _card.HorizontalAlignment = HorizontalAlignment.Center;
            _card.VerticalAlignment = VerticalAlignment.Center;
            _card.RenderTransformOrigin = new Point(0.5, 0.5);

            TextBlock kickerBlock = Ui.Label(kicker);
            kickerBlock.Foreground = Theme.GetBrush("BrushAccent");
            kickerBlock.FontSize = 9.5;

            TextBlock titleBlock = Ui.Text(title, "TextH2");
            titleBlock.Margin = new Thickness(0, 9, 0, 0);

            TextBlock messageBlock = Ui.Body(message);
            messageBlock.Margin = new Thickness(0, 9, 0, 0);
            messageBlock.MaxWidth = 430;

            StackPanel buttons = new StackPanel();
            buttons.Orientation = Orientation.Horizontal;
            buttons.HorizontalAlignment = HorizontalAlignment.Right;
            buttons.Margin = new Thickness(0, 24, 0, 0);

            if (cancellable)
            {
                Button cancel = Ui.Btn("Cancelar", null, "BtnGhost", delegate { Close(); });
                cancel.Margin = new Thickness(0, 0, 10, 0);
                buttons.Children.Add(cancel);
            }

            Button confirm = Ui.Btn(confirmText, null, confirmStyle, delegate
            {
                Action action = _confirm;
                Close();
                if (action != null) action();
            });
            buttons.Children.Add(confirm);

            StackPanel content = new StackPanel();
            content.Children.Add(kickerBlock);
            content.Children.Add(titleBlock);
            content.Children.Add(messageBlock);
            content.Children.Add(buttons);
            _card.Child = content;
            Children.Add(_card);

            Visibility = Visibility.Visible;
            _card.Opacity = 0;
            _card.BeginAnimation(UIElement.OpacityProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150)));

            ScaleTransform scale = new ScaleTransform(0.97, 0.97);
            _card.RenderTransform = scale;
            scale.BeginAnimation(ScaleTransform.ScaleXProperty,
                new DoubleAnimation(0.97, 1, TimeSpan.FromMilliseconds(170))
                { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
            scale.BeginAnimation(ScaleTransform.ScaleYProperty,
                new DoubleAnimation(0.97, 1, TimeSpan.FromMilliseconds(170))
                { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        }
    }

    public sealed class PreviewExplorer : Grid
    {
        private const double PanelWidth = 500;
        private const double PanelHeight = 338;
        private const double WindowWidth = 452;
        private const double WindowHeight = 294;
        private const double WindowLeft = 24;
        private const double WindowTop = 22;

        private readonly Canvas _blurWallpaper;
        private readonly BlurEffect _blur;
        private readonly Border _window;
        private readonly Grid _windowRoot;
        private readonly Border _tint;
        private readonly Border _topSheen;
        private Border _tabBar;
        private Border _sidebar;
        private Border _addressBar;
        private Border _searchBar;
        private readonly Border _windowBorder;
        private Border _separator;
        private Border _sidebarSeparator;
        private readonly List<TextBlock> _primary = new List<TextBlock>();
        private readonly List<TextBlock> _muted = new List<TextBlock>();
        private bool _dark = true;

        public PreviewExplorer()
        {
            Width = PanelWidth;
            Height = PanelHeight;
            ClipToBounds = true;
            Background = Brushes.Transparent;

            Grid root = new Grid();
            root.Width = PanelWidth;
            root.Height = PanelHeight;
            root.ClipToBounds = true;
            root.SizeChanged += delegate
            {
                root.Clip = new RectangleGeometry(new Rect(0, 0, root.ActualWidth, root.ActualHeight), 14, 14);
            };
            Children.Add(root);

            root.Children.Add(CreateWallpaper());

            Border vignette = new Border();
            vignette.Background = new RadialGradientBrush(
                Color.FromArgb(0, 0, 0, 0),
                Color.FromArgb(120, 0, 0, 0));
            ((RadialGradientBrush)vignette.Background).GradientOrigin = new Point(0.5, 0.4);
            ((RadialGradientBrush)vignette.Background).Center = new Point(0.5, 0.4);
            ((RadialGradientBrush)vignette.Background).RadiusX = 0.85;
            ((RadialGradientBrush)vignette.Background).RadiusY = 0.9;
            vignette.IsHitTestVisible = false;
            root.Children.Add(vignette);

            _window = new Border();
            _window.Width = WindowWidth;
            _window.Height = WindowHeight;
            _window.CornerRadius = new CornerRadius(10);
            _window.BorderThickness = new Thickness(1);
            _window.BorderBrush = Ui.SB("#38FFFFFF");
            _window.Background = Brushes.Transparent;
            _window.HorizontalAlignment = HorizontalAlignment.Left;
            _window.VerticalAlignment = VerticalAlignment.Top;
            _window.Margin = new Thickness(WindowLeft, WindowTop, 0, 0);
            _window.Effect = new DropShadowEffect
            {
                BlurRadius = 28,
                ShadowDepth = 10,
                Direction = 270,
                Opacity = 0.55,
                Color = Colors.Black
            };
            root.Children.Add(_window);

            _windowRoot = new Grid();
            _windowRoot.ClipToBounds = true;
            _windowRoot.SizeChanged += delegate
            {
                _windowRoot.Clip = new RectangleGeometry(
                    new Rect(0, 0, _windowRoot.ActualWidth, _windowRoot.ActualHeight), 9, 9);
            };
            _window.Child = _windowRoot;

            _blurWallpaper = CreateWallpaper();
            _blurWallpaper.Width = PanelWidth;
            _blurWallpaper.Height = PanelHeight;
            _blurWallpaper.HorizontalAlignment = HorizontalAlignment.Left;
            _blurWallpaper.VerticalAlignment = VerticalAlignment.Top;
            _blurWallpaper.Margin = new Thickness(-WindowLeft, -WindowTop, 0, 0);
            _blurWallpaper.IsHitTestVisible = false;
            _blur = new BlurEffect();
            _blur.Radius = 28;
            _blur.KernelType = KernelType.Gaussian;
            _blurWallpaper.Effect = _blur;
            _windowRoot.Children.Add(_blurWallpaper);

            _tint = new Border();
            _tint.Background = Ui.SB("#3A000000");
            _windowRoot.Children.Add(_tint);

            _topSheen = new Border();
            _topSheen.Height = 80;
            _topSheen.VerticalAlignment = VerticalAlignment.Top;
            _topSheen.Background = new LinearGradientBrush(
                Color.FromArgb(26, 255, 255, 255),
                Color.FromArgb(0, 255, 255, 255),
                90);
            _topSheen.IsHitTestVisible = false;
            _windowRoot.Children.Add(_topSheen);

            Grid content = BuildContent();
            _windowRoot.Children.Add(content);

            _windowBorder = new Border();
            _windowBorder.BorderThickness = new Thickness(1, 1, 1, 1);
            _windowBorder.BorderBrush = Ui.SB("#22FFFFFF");
            _windowBorder.CornerRadius = new CornerRadius(9);
            _windowBorder.IsHitTestVisible = false;
            _windowRoot.Children.Add(_windowBorder);

            Update(EngineConfig.CreateDefault(), true);
        }

        public void Update(EngineConfig config, bool dark)
        {
            if (config == null) config = EngineConfig.CreateDefault();
            _dark = dark;

            Rgba baseColor = dark ? config.Dark : config.Light;
            int effect = config.Effect;
            double factor = 0;
            if (effect == 1) factor = 0.88;
            else if (effect == 2) factor = 0.55;
            else if (effect == 4) factor = 0.78;

            int alpha = (int)Math.Round(baseColor.A * factor);
            Color top;
            Color bottom;
            if (effect == 3)
            {
                top = Color.FromArgb(dark ? (byte)30 : (byte)22, 10, 16, 26);
                bottom = Color.FromArgb(dark ? (byte)18 : (byte)12, 0, 0, 0);
            }
            else if (effect == 0)
            {
                top = dark ? Color.FromArgb(255, 18, 26, 40) : Color.FromArgb(255, 246, 248, 252);
                bottom = top;
            }
            else
            {
                if (alpha <= 0)
                {
                    // RGBA(0,0,0,0): sem nenhuma tinta, só o desfoque do vidro.
                    top = Color.FromArgb(0, (byte)baseColor.R, (byte)baseColor.G, (byte)baseColor.B);
                    bottom = top;
                }
                else
                {
                    top = Color.FromArgb((byte)Math.Min(255, alpha + 14), (byte)baseColor.R, (byte)baseColor.G, (byte)baseColor.B);
                    bottom = Color.FromArgb((byte)Math.Max(0, alpha - 18), (byte)baseColor.R, (byte)baseColor.G, (byte)baseColor.B);
                }
            }

            LinearGradientBrush tint = new LinearGradientBrush();
            tint.StartPoint = new Point(0, 0);
            tint.EndPoint = new Point(0.25, 1);
            tint.GradientStops.Add(new GradientStop(top, 0));
            tint.GradientStops.Add(new GradientStop(bottom, 1));
            _tint.Background = tint;

            _window.Background = effect == 0
                ? (dark ? Ui.SB("#0F1624") : Ui.SB("#F6F8FC"))
                : Brushes.Transparent;
            _blurWallpaper.Visibility = effect == 0 ? Visibility.Collapsed : Visibility.Visible;
            _blur.Radius = effect == 1 ? 24 : effect == 2 ? 30 : effect == 3 ? 22 : 26;

            Brush primary = dark ? Ui.SB("#EFF5FF") : Ui.SB("#1A2436");
            Brush muted = dark ? Ui.SB("#A9B7CD") : Ui.SB("#5B6B82");
            for (int i = 0; i < _primary.Count; i++) _primary[i].Foreground = primary;
            for (int i = 0; i < _muted.Count; i++) _muted[i].Foreground = muted;

            Brush chrome = dark ? Ui.SB("#14FFFFFF") : Ui.SB("#7AFFFFFF");
            Brush pill = dark ? Ui.SB("#1CFFFFFF") : Ui.SB("#B8FFFFFF");
            Brush line = dark ? Ui.SB("#26FFFFFF") : Ui.SB("#26000000");

            _tabBar.Background = chrome;
            _sidebar.Background = chrome;
            _addressBar.Background = pill;
            _searchBar.Background = pill;
            _separator.Background = line;
            _sidebarSeparator.Background = line;
            _windowBorder.BorderBrush = dark ? Ui.SB("#2CFFFFFF") : Ui.SB("#44000000");
            _topSheen.Opacity = dark ? 1 : 0.5;
        }

        private Canvas CreateWallpaper()
        {
            Canvas canvas = new Canvas();
            canvas.Width = PanelWidth;
            canvas.Height = PanelHeight;
            canvas.ClipToBounds = true;

            LinearGradientBrush background = new LinearGradientBrush();
            background.StartPoint = new Point(0, 0);
            background.EndPoint = new Point(1, 1);
            background.GradientStops.Add(new GradientStop(Color.FromRgb(8, 16, 38), 0));
            background.GradientStops.Add(new GradientStop(Color.FromRgb(24, 44, 82), 0.45));
            background.GradientStops.Add(new GradientStop(Color.FromRgb(58, 28, 70), 1));
            canvas.Background = background;

            canvas.Children.Add(Glow(170, 300, 60, Color.FromArgb(220, 255, 190, 120), 0.85));
            canvas.Children.Add(Glow(260, -70, 210, Color.FromArgb(200, 45, 212, 191), 0.55));
            canvas.Children.Add(Glow(280, 220, 130, Color.FromArgb(190, 139, 92, 246), 0.6));

            Ellipse sun = new Ellipse();
            sun.Width = 96;
            sun.Height = 96;
            sun.Fill = new RadialGradientBrush(
                Color.FromArgb(255, 255, 214, 160),
                Color.FromArgb(0, 255, 170, 90));
            Canvas.SetLeft(sun, 320);
            Canvas.SetTop(sun, 54);
            canvas.Children.Add(sun);

            AddStars(canvas);

            System.Windows.Shapes.Path back = new System.Windows.Shapes.Path();
            back.Data = Geometry.Parse("M0,248 L78,190 L138,232 L214,158 L284,226 L352,176 L422,232 L500,196 L500,338 L0,338 Z");
            back.Fill = new SolidColorBrush(Color.FromArgb(235, 12, 22, 42));
            canvas.Children.Add(back);

            System.Windows.Shapes.Path front = new System.Windows.Shapes.Path();
            front.Data = Geometry.Parse("M0,278 L92,226 L162,268 L240,208 L318,270 L404,224 L500,272 L500,338 L0,338 Z");
            front.Fill = new SolidColorBrush(Color.FromArgb(250, 7, 13, 26));
            canvas.Children.Add(front);

            Border ground = new Border();
            ground.Width = PanelWidth;
            ground.Height = 30;
            ground.Background = Ui.SB("#05090F");
            Canvas.SetLeft(ground, 0);
            Canvas.SetTop(ground, PanelHeight - 30);
            canvas.Children.Add(ground);

            return canvas;
        }

        private static Ellipse Glow(double size, double left, double top, Color color, double opacity)
        {
            Ellipse ellipse = new Ellipse();
            ellipse.Width = size;
            ellipse.Height = size;
            RadialGradientBrush brush = new RadialGradientBrush(color, Color.FromArgb(0, color.R, color.G, color.B));
            ellipse.Fill = brush;
            ellipse.Opacity = opacity;
            Canvas.SetLeft(ellipse, left);
            Canvas.SetTop(ellipse, top);
            return ellipse;
        }

        private static void AddStars(Canvas canvas)
        {
            double[,] points = new double[,]
            {
                { 38, 46 }, { 84, 96 }, { 136, 40 }, { 196, 84 }, { 244, 34 },
                { 300, 108 }, { 356, 60 }, { 410, 32 }, { 456, 92 }, { 470, 148 },
                { 64, 150 }, { 262, 132 }
            };
            for (int i = 0; i < points.GetLength(0); i++)
            {
                Ellipse star = new Ellipse();
                star.Width = 2.2;
                star.Height = 2.2;
                star.Fill = new SolidColorBrush(Color.FromArgb((byte)(120 + (i % 3) * 40), 255, 255, 255));
                Canvas.SetLeft(star, points[i, 0]);
                Canvas.SetTop(star, points[i, 1]);
                canvas.Children.Add(star);
            }
        }

        private Grid BuildContent()
        {
            Grid grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(76) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Grid toolbar = new Grid();
            toolbar.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) });
            toolbar.RowDefinitions.Add(new RowDefinition { Height = new GridLength(45) });
            toolbar.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1) });

            _tabBar = new Border();
            _tabBar.Background = Ui.SB("#14FFFFFF");
            Grid.SetRow(_tabBar, 0);

            StackPanel tabs = new StackPanel();
            tabs.Orientation = Orientation.Horizontal;
            tabs.VerticalAlignment = VerticalAlignment.Center;
            tabs.Margin = new Thickness(10, 0, 8, 0);

            Border appDot = new Border();
            appDot.Width = 8;
            appDot.Height = 8;
            appDot.CornerRadius = new CornerRadius(2);
            appDot.Background = Theme.GetBrush("BrushAccentGradient");
            appDot.VerticalAlignment = VerticalAlignment.Center;
            tabs.Children.Add(appDot);

            TextBlock tabTitle = SmallText("Documentos", 10.5, false);
            tabTitle.Margin = new Thickness(7, 0, 0, 0);
            _primary.Add(tabTitle);
            tabs.Children.Add(tabTitle);

            TextBlock tabClose = SmallText("\uE8BB", 8, false);
            tabClose.FontFamily = Theme.GetFont("FontIcons");
            tabClose.Margin = new Thickness(12, 0, 0, 0);
            _muted.Add(tabClose);
            tabs.Children.Add(tabClose);

            TextBlock plus = SmallText("+", 12, false);
            plus.Margin = new Thickness(14, 0, 0, 0);
            plus.VerticalAlignment = VerticalAlignment.Center;
            _muted.Add(plus);
            tabs.Children.Add(plus);

            _tabBar.Child = tabs;
            toolbar.Children.Add(_tabBar);

            Grid addressRow = new Grid();
            addressRow.Margin = new Thickness(8, 0, 8, 0);
            addressRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            addressRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(106) });

            _addressBar = new Border();
            _addressBar.Height = 25;
            _addressBar.CornerRadius = new CornerRadius(6);
            _addressBar.Background = Ui.SB("#1CFFFFFF");
            _addressBar.VerticalAlignment = VerticalAlignment.Center;
            _addressBar.Margin = new Thickness(0, 0, 6, 0);

            StackPanel crumbs = new StackPanel();
            crumbs.Orientation = Orientation.Horizontal;
            crumbs.VerticalAlignment = VerticalAlignment.Center;
            crumbs.Margin = new Thickness(8, 0, 8, 0);

            TextBlock crumbIcon = SmallText(Glyphs.Monitor, 9, false);
            crumbIcon.FontFamily = Theme.GetFont("FontIcons");
            _muted.Add(crumbIcon);
            crumbs.Children.Add(crumbIcon);

            TextBlock crumb1 = SmallText("Este Computador", 9, false);
            crumb1.Margin = new Thickness(6, 0, 0, 0);
            _muted.Add(crumb1);
            crumbs.Children.Add(crumb1);

            TextBlock arrow = SmallText("\uE76C", 7, false);
            arrow.FontFamily = Theme.GetFont("FontIcons");
            arrow.Margin = new Thickness(5, 0, 0, 0);
            _muted.Add(arrow);
            crumbs.Children.Add(arrow);

            TextBlock crumb2 = SmallText("Documentos", 9, true);
            crumb2.Margin = new Thickness(5, 0, 0, 0);
            _primary.Add(crumb2);
            crumbs.Children.Add(crumb2);

            _addressBar.Child = crumbs;
            Grid.SetColumn(_addressBar, 0);
            addressRow.Children.Add(_addressBar);

            _searchBar = new Border();
            _searchBar.Height = 25;
            _searchBar.CornerRadius = new CornerRadius(6);
            _searchBar.Background = Ui.SB("#1CFFFFFF");
            _searchBar.VerticalAlignment = VerticalAlignment.Center;

            StackPanel search = new StackPanel();
            search.Orientation = Orientation.Horizontal;
            search.VerticalAlignment = VerticalAlignment.Center;
            search.Margin = new Thickness(8, 0, 8, 0);

            TextBlock searchIcon = SmallText(Glyphs.Search, 8.5, false);
            searchIcon.FontFamily = Theme.GetFont("FontIcons");
            _muted.Add(searchIcon);
            search.Children.Add(searchIcon);
            TextBlock searchText = SmallText("Pesquisar", 9, false);
            searchText.Margin = new Thickness(6, 0, 0, 0);
            _muted.Add(searchText);
            search.Children.Add(searchText);

            _searchBar.Child = search;
            Grid.SetColumn(_searchBar, 1);
            addressRow.Children.Add(_searchBar);

            Grid.SetRow(addressRow, 1);
            toolbar.Children.Add(addressRow);

            _separator = new Border();
            _separator.Background = Ui.SB("#26FFFFFF");
            Grid.SetRow(_separator, 2);
            toolbar.Children.Add(_separator);

            Grid.SetRow(toolbar, 0);
            grid.Children.Add(toolbar);

            Grid body = new Grid();
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(118) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _sidebar = new Border();
            _sidebar.Background = Ui.SB("#14FFFFFF");
            _sidebar.BorderThickness = new Thickness(0, 0, 1, 0);
            _sidebarSeparator = new Border();
            _sidebarSeparator.Background = Ui.SB("#26FFFFFF");
            _sidebar.BorderBrush = _sidebarSeparator.Background;
            _sidebar.Child = BuildSidebar();
            Grid.SetColumn(_sidebar, 0);
            body.Children.Add(_sidebar);

            Grid main = new Grid();
            main.Margin = new Thickness(14, 12, 14, 12);
            main.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            main.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            main.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            StackPanel heading = new StackPanel();
            heading.Orientation = Orientation.Horizontal;
            TextBlock title = SmallText("Documentos", 13.5, true);
            title.FontWeight = FontWeights.SemiBold;
            _primary.Add(title);
            heading.Children.Add(title);

            TextBlock count = SmallText("6 itens", 8.5, false);
            count.Margin = new Thickness(8, 3, 0, 0);
            _muted.Add(count);
            heading.Children.Add(count);
            Grid.SetRow(heading, 0);
            main.Children.Add(heading);

            TextBlock subtitle = SmallText("Organizado por nome", 8, false);
            subtitle.Margin = new Thickness(0, 2, 0, 8);
            _muted.Add(subtitle);
            Grid.SetRow(subtitle, 1);
            main.Children.Add(subtitle);

            UniformGrid files = new UniformGrid();
            files.Columns = 3;
            files.Rows = 2;
            files.VerticalAlignment = VerticalAlignment.Top;
            AddTile(files, Glyphs.Folder, Color.FromRgb(96, 165, 250), "Projetos");
            AddTile(files, Glyphs.Document, Color.FromRgb(52, 211, 153), "Orçamento");
            AddTile(files, Glyphs.Photo, Color.FromRgb(251, 146, 60), "Fotos");
            AddTile(files, Glyphs.Music, Color.FromRgb(167, 139, 250), "Músicas");
            AddTile(files, Glyphs.Folder, Color.FromRgb(244, 114, 182), "Arquivo");
            AddTile(files, Glyphs.Document, Color.FromRgb(45, 212, 191), "Notas");
            Grid.SetRow(files, 2);
            main.Children.Add(files);

            Grid.SetColumn(main, 1);
            body.Children.Add(main);

            Grid.SetRow(body, 1);
            grid.Children.Add(body);
            return grid;
        }

        private UIElement BuildSidebar()
        {
            StackPanel panel = new StackPanel();
            panel.Margin = new Thickness(9, 10, 9, 8);

            TextBlock caption = SmallText("ACESSO RÁPIDO", 7.5, false);
            caption.FontWeight = FontWeights.SemiBold;
            caption.Margin = new Thickness(6, 0, 0, 8);
            _muted.Add(caption);
            panel.Children.Add(caption);

            AddSideItem(panel, Glyphs.Monitor, "Área de Trabalho", false);
            AddSideItem(panel, Glyphs.Download, "Downloads", false);
            AddSideItem(panel, Glyphs.Folder, "Documentos", true);
            AddSideItem(panel, Glyphs.Photo, "Imagens", false);
            AddSideItem(panel, Glyphs.Music, "Música", false);
            AddSideItem(panel, Glyphs.Globe, "Este Computador", false);
            return panel;
        }

        private void AddSideItem(Panel parent, string glyph, string text, bool selected)
        {
            Border row = new Border();
            row.CornerRadius = new CornerRadius(5);
            row.Padding = new Thickness(6, 4, 6, 4);
            row.Margin = new Thickness(0, 1, 0, 1);
            if (selected) row.Background = Ui.Alpha(Theme.GetColor("ColorAccent"), 45);

            StackPanel content = new StackPanel();
            content.Orientation = Orientation.Horizontal;

            TextBlock icon = SmallText(glyph, 8.5, false);
            icon.FontFamily = Theme.GetFont("FontIcons");
            content.Children.Add(icon);

            TextBlock label = SmallText(text, 8.5, selected);
            label.Margin = new Thickness(6, 0, 0, 0);
            if (!selected) _muted.Add(label);
            else _primary.Add(label);
            content.Children.Add(label);

            row.Child = content;
            parent.Children.Add(row);
        }

        private void AddTile(UniformGrid grid, string glyph, Color color, string caption)
        {
            StackPanel tile = new StackPanel();
            tile.Margin = new Thickness(2, 2, 8, 8);

            Border iconBox = new Border();
            iconBox.Width = 36;
            iconBox.Height = 27;
            iconBox.CornerRadius = new CornerRadius(6);
            LinearGradientBrush brush = new LinearGradientBrush(
                Ui.Alpha(color, 235).Color,
                Ui.Alpha(color, 150).Color,
                90);
            iconBox.Background = brush;
            iconBox.Child = Ui.Icon(glyph, 11, Ui.SB("#FFFFFF"));
            tile.Children.Add(iconBox);

            TextBlock label = SmallText(caption, 8.5, true);
            label.Margin = new Thickness(0, 4, 0, 0);
            label.TextTrimming = TextTrimming.CharacterEllipsis;
            _primary.Add(label);
            tile.Children.Add(label);

            grid.Children.Add(tile);
        }

        private static TextBlock SmallText(string text, double size, bool primary)
        {
            TextBlock block = new TextBlock();
            block.Text = text;
            block.FontSize = size;
            block.Foreground = Ui.SB(primary ? "#EFF5FF" : "#A9B7CD");
            block.VerticalAlignment = VerticalAlignment.Center;
            return block;
        }
    }
}
