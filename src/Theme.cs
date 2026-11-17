using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Resources;

namespace BahamaStudio
{
    public static class Theme
    {
        public static void Load()
        {
            Stream stream = typeof(Theme).Assembly.GetManifestResourceStream("Theme.xaml");
            if (stream == null)
            {
                Uri uri = new Uri("Theme.xaml", UriKind.Relative);
                StreamResourceInfo info = Application.GetResourceStream(uri);
                if (info != null) stream = info.Stream;
            }
            if (stream == null)
                throw new InvalidOperationException("Theme.xaml não foi encontrado nos recursos do aplicativo.");

            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                ResourceDictionary dictionary = (ResourceDictionary)XamlReader.Parse(reader.ReadToEnd());
                Application.Current.Resources.MergedDictionaries.Add(dictionary);
            }
        }

        public static Style GetStyle(string key)
        {
            object value = Application.Current.TryFindResource(key);
            return value as Style;
        }

        public static Brush GetBrush(string key)
        {
            object value = Application.Current.TryFindResource(key);
            return value as Brush;
        }

        public static Color GetColor(string key)
        {
            object value = Application.Current.TryFindResource(key);
            if (value is Color) return (Color)value;
            return Colors.White;
        }

        public static FontFamily GetFont(string key)
        {
            object value = Application.Current.TryFindResource(key);
            return value as FontFamily;
        }
    }
}
