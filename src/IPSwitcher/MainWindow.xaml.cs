using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using IPSwitcher.Helpers;
using IPSwitcher.Models;
using IPSwitcher.ViewModels;
using WinForms = System.Windows.Forms;

namespace IPSwitcher;

public partial class MainWindow : Window
{
    private WinForms.NotifyIcon? _notifyIcon;
    private bool _isExplicitClose;
    private bool _hasShownTrayNotification;

    public MainViewModel VM => (MainViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        DwmHelper.ApplyDarkMode(hwnd, VM.IsDarkMode);
        DwmHelper.TryApplyMica(hwnd);

        VM.PropertyChanged += OnVmPropertyChanged;
        ApplyThemeComboBox();

        EnsureTrayIcon();
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsDarkMode))
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            DwmHelper.ApplyDarkMode(hwnd, VM.IsDarkMode);
        }
    }

    private void ApplyThemeComboBox()
    {
        for (int i = 0; i < ThemeComboBox.Items.Count; i++)
        {
            if (ThemeComboBox.Items[i] is ComboBoxItem item &&
                item.Tag is ThemeTag tag &&
                (int)tag == (int)VM.Theme)
            {
                ThemeComboBox.SelectionChanged -= ThemeComboBox_OnSelectionChanged;
                ThemeComboBox.SelectedIndex = i;
                ThemeComboBox.SelectionChanged += ThemeComboBox_OnSelectionChanged;
                return;
            }
        }
    }

    private void ThemeComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeComboBox.SelectedItem is ComboBoxItem item && item.Tag is ThemeTag tag)
        {
            var theme = (AppTheme)(int)tag;
            App.SwitchTheme(theme);
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            DwmHelper.ApplyDarkMode(hwnd, VM.IsDarkMode);
            DwmHelper.TryApplyMica(hwnd);
        }
    }

    #region Tray Icon

    private void EnsureTrayIcon()
    {
        if (_notifyIcon is not null)
        {
            return;
        }

        _notifyIcon = new WinForms.NotifyIcon
        {
            Text = "IPSwitcher",
            Visible = true,
            Icon = LoadTrayIcon(),
        };
        _notifyIcon.DoubleClick += (s, e) => ShowFromTray();

        BuildTrayMenu();

        VM.Profiles.CollectionChanged += (s, e) => BuildTrayMenu();
    }

    private static System.Drawing.Icon LoadTrayIcon()
    {
        // Method 1: Load .ico from embedded assembly resource stream
        try
        {
            var asm = typeof(MainWindow).Assembly;
            var stream = asm.GetManifestResourceStream("IPSwitcher.Assets.app.ico");
            if (stream is not null)
            {
                return new System.Drawing.Icon(stream);
            }
        }
        catch
        {
        }

        // Method 2: Extract Win32 icon from exe (<ApplicationIcon> embeds it)
        try
        {
            var exePath = Environment.ProcessPath ?? typeof(MainWindow).Assembly.Location;
            var icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
            if (icon is not null)
            {
                return icon;
            }
        }
        catch
        {
        }

        // Method 3: Generate a simple icon in-memory
        return GenerateTrayIcon();
    }

    private static System.Drawing.Icon GenerateTrayIcon()
    {
        using var bmp = new System.Drawing.Bitmap(32, 32);
        using var g = System.Drawing.Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var bg = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(0, 120, 212));
        g.FillEllipse(bg, 2, 2, 28, 28);
        using var font = new System.Drawing.Font("Segoe UI", 12, System.Drawing.FontStyle.Bold);
        var sf = new System.Drawing.StringFormat
        {
            Alignment = System.Drawing.StringAlignment.Center,
            LineAlignment = System.Drawing.StringAlignment.Center,
        };
        g.DrawString("IP", font, System.Drawing.Brushes.White, 16, 16, sf);

        var hicon = bmp.GetHicon();
        return System.Drawing.Icon.FromHandle(hicon);
    }

    private void BuildTrayMenu()
    {
        if (_notifyIcon is null)
        {
            return;
        }

        var menu = new WinForms.ContextMenuStrip();

        var showItem = new WinForms.ToolStripMenuItem("显示主窗口");
        showItem.Click += (s, e) => ShowFromTray();
        menu.Items.Add(showItem);

        menu.Items.Add(new WinForms.ToolStripSeparator());

        var refreshItem = new WinForms.ToolStripMenuItem("刷新适配器");
        refreshItem.Click += (s, e) => VM.RefreshAdapters();
        menu.Items.Add(refreshItem);

        menu.Items.Add(new WinForms.ToolStripSeparator());

        foreach (var p in VM.Profiles)
        {
            var item = new WinForms.ToolStripMenuItem($"应用: {p.Name}") { Tag = p };
            item.Click += TrayApplyProfile_Click;
            menu.Items.Add(item);
        }

        menu.Items.Add(new WinForms.ToolStripSeparator());

        var exitItem = new WinForms.ToolStripMenuItem("退出");
        exitItem.Click += (s, e) => ExitApplication();
        menu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = menu;
    }

    private void TrayApplyProfile_Click(object? sender, EventArgs e)
    {
        if (sender is WinForms.ToolStripMenuItem item && item.Tag is ProfileViewModel pvm)
        {
            VM.SelectedProfile = pvm;
            _ = VM.ApplyProfileCommand.ExecuteAsync(null);
        }
    }

    private void ExitApplication()
    {
        _isExplicitClose = true;
        if (_notifyIcon is not null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
        Close();
    }

    public void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        Topmost = true;
        Topmost = false;
    }

    #endregion

    private void MainWindow_OnStateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
            ShowTrayNotificationIfFirstTime();
        }
    }

    private void MainWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        if (!_isExplicitClose)
        {
            e.Cancel = true;
            Hide();
            ShowTrayNotificationIfFirstTime();
            return;
        }

        VM.PersistOnExit();
        if (_notifyIcon is not null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
    }

    private void ShowTrayNotificationIfFirstTime()
    {
        if (_hasShownTrayNotification || _notifyIcon is null)
        {
            return;
        }
        _hasShownTrayNotification = true;
        try
        {
            _notifyIcon.ShowBalloonTip(
                3000,
                "IPSwitcher",
                "程序已最小化到系统托盘。双击托盘图标可恢复窗口。",
                WinForms.ToolTipIcon.Info);
        }
        catch
        {
        }
    }
}
