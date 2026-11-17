using System.Windows.Controls;

namespace BahamaStudio
{
    public abstract class PageBase : Grid
    {
        public MainWindow Host;

        public abstract void Refresh();
    }
}
