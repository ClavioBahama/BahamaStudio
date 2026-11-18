using System;
using System.Drawing;
using System.Windows.Forms;

namespace BahamaStudio
{
    // Ícone de bandeja para a app residente. Fechar a janela mantém o
    // reaplicador de transparência vivo; apenas "Sair" encerra o processo.
    public sealed class TrayIcon : IDisposable
    {
        private readonly NotifyIcon _icon;
        private readonly ToolStripMenuItem _toggle;
        private readonly MainWindow _window;
        private bool _disposed;

        public TrayIcon(MainWindow window)
        {
            _window = window;
            _icon = new NotifyIcon();

            try { _icon.Icon = Icon.ExtractAssociatedIcon(Paths.ExePath); }
            catch { _icon.Icon = SystemIcons.Application; }

            _icon.Text = "Bahama Studio";
            _icon.Visible = true;
            _icon.DoubleClick += delegate(object sender, EventArgs e) { Open(); };

            ContextMenuStrip menu = new ContextMenuStrip();

            ToolStripMenuItem open = new ToolStripMenuItem("Abrir");
            open.Click += delegate(object sender, EventArgs e) { Open(); };
            menu.Items.Add(open);

            _toggle = new ToolStripMenuItem("Transparência total");
            _toggle.CheckOnClick = true;
            _toggle.Checked = ExplorerTransparency.IsActive;
            _toggle.Enabled = ExplorerTransparency.IsSupported;
            _toggle.ToolTipText = ExplorerTransparency.IsSupported
                ? "Ativa ou desativa o reaplicador nativo do fundo do Explorador."
                : "Disponível apenas no Windows 11. No Windows 10 use Aparência → Vidro totalmente transparente → Aplicar no Explorer.";
            _toggle.Click += delegate(object sender, EventArgs e) { ToggleTransparency(); };
            menu.Items.Add(_toggle);

            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem exit = new ToolStripMenuItem("Sair");
            exit.Click += delegate(object sender, EventArgs e) { _window.ExitApplication(); };
            menu.Items.Add(exit);

            _icon.ContextMenuStrip = menu;
        }

        public void Refresh()
        {
            if (_toggle != null) _toggle.Checked = ExplorerTransparency.IsActive;
        }

        private void Open()
        {
            _window.ShowWindow();
        }

        private void ToggleTransparency()
        {
            if (!ExplorerTransparency.IsSupported)
            {
                _toggle.Checked = false;
                return;
            }
            bool enable = _toggle.Checked;
            ExplorerTransparency.SetEnabled(enable);
            _icon.Text = enable
                ? "Bahama Studio - transparência total ativa"
                : "Bahama Studio";
            Log.Write("[bandeja] transparência total " + (enable ? "ativada" : "desativada"));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _icon.Visible = false;
            if (_icon.ContextMenuStrip != null) _icon.ContextMenuStrip.Dispose();
            _icon.Dispose();
        }
    }
}
