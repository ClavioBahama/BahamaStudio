using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BahamaStudio
{
    public static class Glyphs
    {
        public const string Home = "\uE80F";
        public const string Palette = "\uE790";
        public const string Download = "\uE896";
        public const string Diagnostic = "\uE9D9";
        public const string Shield = "\uEA18";
        public const string Warning = "\uE7BA";
        public const string Lock = "\uE72E";
        public const string Refresh = "\uE72C";
        public const string Check = "\uE73E";
        public const string Close = "\uE8BB";
        public const string Minimize = "\uE921";
        public const string Maximize = "\uE922";
        public const string Restore = "\uE923";
        public const string Settings = "\uE713";
        public const string Folder = "\uE8B7";
        public const string Info = "\uE946";
        public const string Bug = "\uEBE8";
        public const string Power = "\uE7E8";
        public const string ChevronRight = "\uE76C";
        public const string Add = "\uE710";
        public const string Search = "\uE721";
        public const string Document = "\uE8A5";
        public const string Photo = "\uEB9F";
        public const string Music = "\uE8D6";
        public const string Monitor = "\uE7F4";
        public const string Delete = "\uE74D";
        public const string Wrench = "\uE90F";
        public const string Clock = "\uE823";
        public const string Star = "\uE734";
        public const string External = "\uE8A7";
        public const string Copy = "\uE8C8";
        public const string Save = "\uE74E";
        public const string Undo = "\uE7A7";
        public const string Lightbulb = "\uE82F";
        public const string Globe = "\uE774";
        public const string Heart = "\uEB51";
        public const string User = "\uE77B";
        public const string Windows = "\uE7F4";
    }

    public static class Ui
    {
        public static SolidColorBrush SB(string hex)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        }

        public static SolidColorBrush SB(Color color)
        {
            return new SolidColorBrush(color);
        }

        public static SolidColorBrush Alpha(Color color, byte alpha)
        {
            return new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
        }

        public static SolidColorBrush Alpha(Color color, int alpha)
        {
            int a = alpha;
            if (a < 0) a = 0;
            if (a > 255) a = 255;
            return new SolidColorBrush(Color.FromArgb((byte)a, color.R, color.G, color.B));
        }

        public static T Find<T>(string key) where T : class
        {
            return Application.Current.TryFindResource(key) as T;
        }

        public static TextBlock Text(string text, string styleKey)
        {
            TextBlock block = new TextBlock();
            Style style = Theme.GetStyle(styleKey);
            if (style != null) block.Style = style;
            block.Text = text;
            return block;
        }

        public static TextBlock H1(string text) { return Text(text, "TextH1"); }
        public static TextBlock H2(string text) { return Text(text, "TextH2"); }
        public static TextBlock H3(string text) { return Text(text, "TextH3"); }
        public static TextBlock Body(string text) { return Text(text, "TextBody"); }
        public static TextBlock Caption(string text) { return Text(text, "TextCaption"); }
        public static TextBlock Label(string text) { return Text(text, "TextLabel"); }
        public static TextBlock Mono(string text) { return Text(text, "TextMono"); }

        public static TextBlock Icon(string glyph, double size)
        {
            return Icon(glyph, size, Theme.GetBrush("BrushMuted"));
        }

        public static TextBlock Icon(string glyph, double size, Brush brush)
        {
            TextBlock block = new TextBlock();
            block.Text = glyph;
            block.FontFamily = Theme.GetFont("FontIcons");
            block.FontSize = size;
            block.Foreground = brush;
            block.VerticalAlignment = VerticalAlignment.Center;
            block.HorizontalAlignment = HorizontalAlignment.Center;
            block.TextAlignment = TextAlignment.Center;
            return block;
        }

        public static StackPanel V(double gap, params UIElement[] items)
        {
            StackPanel panel = new StackPanel();
            panel.Orientation = Orientation.Vertical;
            List<UIElement> visible = new List<UIElement>();
            for (int i = 0; i < items.Length; i++) if (items[i] != null) visible.Add(items[i]);
            for (int i = 0; i < visible.Count; i++)
            {
                if (i < visible.Count - 1) AddMargin(visible[i], 0, 0, 0, gap);
                panel.Children.Add(visible[i]);
            }
            return panel;
        }

        public static StackPanel H(double gap, params UIElement[] items)
        {
            StackPanel panel = new StackPanel();
            panel.Orientation = Orientation.Horizontal;
            List<UIElement> visible = new List<UIElement>();
            for (int i = 0; i < items.Length; i++) if (items[i] != null) visible.Add(items[i]);
            for (int i = 0; i < visible.Count; i++)
            {
                if (i < visible.Count - 1) AddMargin(visible[i], 0, 0, gap, 0);
                panel.Children.Add(visible[i]);
            }
            return panel;
        }

        public static void AddMargin(UIElement element, double left, double top, double right, double bottom)
        {
            FrameworkElement fe = element as FrameworkElement;
            if (fe == null) return;
            Thickness m = fe.Margin;
            fe.Margin = new Thickness(m.Left + left, m.Top + top, m.Right + right, m.Bottom + bottom);
        }

        public static Grid Columns(params GridLength[] widths)
        {
            Grid grid = new Grid();
            for (int i = 0; i < widths.Length; i++)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = widths[i] });
            return grid;
        }

        public static Grid Rows(params GridLength[] heights)
        {
            Grid grid = new Grid();
            for (int i = 0; i < heights.Length; i++)
                grid.RowDefinitions.Add(new RowDefinition { Height = heights[i] });
            return grid;
        }

        public static GridLength Star(double weight)
        {
            return new GridLength(weight, GridUnitType.Star);
        }

        public static GridLength Px(double value)
        {
            return new GridLength(value, GridUnitType.Pixel);
        }

        public static GridLength Auto()
        {
            return GridLength.Auto;
        }

        public static void Put(Grid grid, UIElement element, int column, int row)
        {
            Grid.SetColumn(element, column);
            Grid.SetRow(element, row);
            grid.Children.Add(element);
        }

        public static Border Card(UIElement child)
        {
            Border border = new Border();
            border.Style = Theme.GetStyle("Card");
            border.Child = child;
            return border;
        }

        public static Border CardSoft(UIElement child)
        {
            Border border = new Border();
            border.Style = Theme.GetStyle("CardSoft");
            border.Child = child;
            return border;
        }

        public static Border CardRaised(UIElement child)
        {
            Border border = new Border();
            border.Style = Theme.GetStyle("CardRaised");
            border.Child = child;
            return border;
        }

        public static Border Chip(string text)
        {
            Border border = new Border();
            border.Style = Theme.GetStyle("Chip");
            border.Child = Text(text, "ChipText");
            return border;
        }

        public static Border Chip(string text, Brush foreground, Color tint)
        {
            Border border = new Border();
            border.Style = Theme.GetStyle("Chip");
            border.Background = Alpha(tint, 30);
            border.BorderBrush = Alpha(tint, 70);
            TextBlock block = Text(text, "ChipText");
            block.Foreground = foreground;
            border.Child = block;
            return border;
        }

        public static Button Btn(string text, string glyph, string styleKey, RoutedEventHandler handler)
        {
            Button button = new Button();
            button.Style = Theme.GetStyle(styleKey);
            SetButtonContent(button, text, glyph);
            if (handler != null) button.Click += handler;
            return button;
        }

        public static void SetButton(Button button, string text, string glyph, string styleKey)
        {
            if (button == null) return;
            button.Style = Theme.GetStyle(styleKey);
            SetButtonContent(button, text, glyph);
        }

        public static void SetButtonContent(Button button, string text, string glyph)
        {
            if (button == null) return;
            if (!string.IsNullOrEmpty(text)) System.Windows.Automation.AutomationProperties.SetName(button, text);
            if (string.IsNullOrEmpty(glyph))
            {
                TextBlock label = new TextBlock();
                label.Text = text;
                label.FontSize = 12.5;
                label.FontWeight = FontWeights.SemiBold;
                label.VerticalAlignment = VerticalAlignment.Center;
                button.Content = label;
            }
            else
            {
                StackPanel content = new StackPanel();
                content.Orientation = Orientation.Horizontal;
                content.VerticalAlignment = VerticalAlignment.Center;
                TextBlock icon = Icon(glyph, 13.5);
                icon.Margin = new Thickness(0, 0, 8, 0);
                icon.Foreground = button.Foreground;
                content.Children.Add(icon);
                if (!string.IsNullOrEmpty(text))
                {
                    TextBlock label = new TextBlock();
                    label.Text = text;
                    label.FontSize = 12.5;
                    label.FontWeight = FontWeights.SemiBold;
                    label.VerticalAlignment = VerticalAlignment.Center;
                    content.Children.Add(label);
                }
                button.Content = content;
            }
        }

        public static Button LinkBtn(string text, RoutedEventHandler handler)
        {
            Button button = new Button();
            button.Style = Theme.GetStyle("BtnLink");
            button.Content = text;
            if (handler != null) button.Click += handler;
            return button;
        }

        public static Border Divider()
        {
            Border border = new Border();
            border.Height = 1;
            border.Background = SB("#1B2841");
            border.Margin = new Thickness(0, 14, 0, 14);
            return border;
        }

        public static Border Divider(double top, double bottom)
        {
            Border border = Divider();
            border.Margin = new Thickness(0, top, 0, bottom);
            return border;
        }

        public static FrameworkElement PageHeader(string kicker, string title, string subtitle, UIElement right)
        {
            Grid grid = new Grid();
            grid.Margin = new Thickness(0, 0, 0, 22);
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock kickerBlock = Label(kicker.ToUpperInvariant());
            kickerBlock.Foreground = Theme.GetBrush("BrushAccent");
            kickerBlock.FontSize = 10;
            kickerBlock.Margin = new Thickness(1, 0, 0, 8);

            StackPanel left = new StackPanel();
            left.Children.Add(kickerBlock);
            left.Children.Add(H1(title));
            TextBlock sub = Body(subtitle);
            sub.Margin = new Thickness(0, 6, 0, 0);
            sub.MaxWidth = 640;
            left.Children.Add(sub);
            Grid.SetColumn(left, 0);
            grid.Children.Add(left);

            if (right != null)
            {
                FrameworkElement align = right as FrameworkElement;
                if (align != null)
                {
                    align.VerticalAlignment = VerticalAlignment.Bottom;
                    align.HorizontalAlignment = HorizontalAlignment.Right;
                }
                Grid.SetColumn(right, 1);
                grid.Children.Add(right);
            }
            return grid;
        }

        public static StackPanel SectionTitle(string title, string subtitle)
        {
            StackPanel panel = new StackPanel();
            panel.Margin = new Thickness(1, 4, 0, 10);
            panel.Children.Add(H3(title));
            if (!string.IsNullOrEmpty(subtitle))
            {
                TextBlock sub = Caption(subtitle);
                sub.Margin = new Thickness(0, 3, 0, 0);
                panel.Children.Add(sub);
            }
            return panel;
        }

        public static ScrollViewer Scroll(UIElement content, double rightPadding)
        {
            ScrollViewer viewer = new ScrollViewer();
            viewer.Style = Theme.GetStyle("PageScroll");
            viewer.Content = content;
            viewer.Padding = new Thickness(0, 0, rightPadding, 0);
            return viewer;
        }

        public static string Fmt(DateTime time)
        {
            return time.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
        }

        public static string Relative(DateTime time)
        {
            TimeSpan delta = DateTime.Now - time;
            if (delta.TotalSeconds < 45) return "agora mesmo";
            if (delta.TotalMinutes < 60) return ((int)delta.TotalMinutes) + " min atrás";
            if (delta.TotalHours < 24) return ((int)delta.TotalHours) + " h atrás";
            if (delta.TotalDays < 7) return ((int)delta.TotalDays) + " d atrás";
            return time.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        }

        public static string Bytes(long bytes)
        {
            if (bytes >= 1024L * 1024L) return (bytes / (1024.0 * 1024.0)).ToString("0.0", CultureInfo.InvariantCulture) + " MB";
            if (bytes >= 1024L) return (bytes / 1024.0).ToString("0.0", CultureInfo.InvariantCulture) + " KB";
            return bytes + " B";
        }

        public static void Tip(FrameworkElement element, string text)
        {
            if (element == null || string.IsNullOrEmpty(text)) return;
            ToolTip tip = new ToolTip();
            tip.Content = text;
            element.ToolTip = tip;
        }

        public static TextBlock Badge(string text, Color color)
        {
            TextBlock block = new TextBlock();
            block.Text = text;
            block.FontSize = 10;
            block.FontWeight = FontWeights.SemiBold;
            block.Foreground = Alpha(color, 255);
            block.Background = Alpha(color, 28);
            block.Padding = new Thickness(7, 2, 7, 3);
            block.Margin = new Thickness(8, 0, 0, 0);
            block.VerticalAlignment = VerticalAlignment.Center;
            return block;
        }
    }
}
