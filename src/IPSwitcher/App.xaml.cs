using System.Threading;
using System.Windows;
using IPSwitcher.Helpers;
using IPSwitcher.Models;
using IPSwitcher.Services;
using IPSwitcher.ViewModels;

namespace IPSwitcher;

public partial class App : System.Windows.Application
{
    private const string MutexName = "Global\\IPSwitcher_SingleInstance_3F7B2E";
    private const string ShowEventName = "Global\\IPSwitcher_ShowEvent_3F7B2E";

    public static MainViewModel MainVM { get; private set; } = null!;

    private static ThemeManager? _themeManager;

    private Mutex? _mutex;
    private EventWaitHandle? _showEvent;
    private Thread? _signalThread;

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            try
            {
                _showEvent = EventWaitHandle.OpenExisting(ShowEventName);
                _showEvent.Set();
            }
            catch
            {
            }
            _mutex.Dispose();
            _mutex = null;
            Shutdown();
            Environment.Exit(0);
            return;
        }

        _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
        _signalThread = new Thread(SignalLoop) { IsBackground = true };
        _signalThread.Start();

        base.OnStartup(e);

        var profileRepo = new JsonProfileRepository();
        var settingsStore = new JsonSettingsStore();
        var adapterService = new AdapterService();
        var configService = new NetworkConfigService();
        var configReader = new CurrentConfigReader();

        MainVM = new MainViewModel(profileRepo, settingsStore, adapterService, configService, configReader);
        MainVM.Initialize();

        _themeManager = new ThemeManager(MainVM);
        _themeManager.ApplyTheme(MainVM.Theme);

        var window = new MainWindow { DataContext = MainVM };
        MainWindow = window;
        window.Show();
    }

    private void SignalLoop()
    {
        try
        {
            while (_showEvent?.WaitOne() == true)
            {
                Dispatcher.Invoke(() =>
                {
                    if (MainWindow is MainWindow w)
                    {
                        w.ShowFromTray();
                    }
                });
            }
        }
        catch
        {
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        MainVM?.PersistOnExit();
        _showEvent?.Dispose();
        try { _mutex?.ReleaseMutex(); } catch { }
        _mutex?.Dispose();
        base.OnExit(e);
    }

    public static void SwitchTheme(AppTheme theme)
    {
        MainVM.Theme = theme;
        _themeManager?.ApplyTheme(theme);
    }
}

public sealed class ThemeManager
{
    private readonly MainViewModel _vm;

    public ThemeManager(MainViewModel vm)
    {
        _vm = vm;
    }

    public void ApplyTheme(AppTheme theme)
    {
        var dark = ThemeHelper.ResolveDark(theme);
        _vm.IsDarkMode = dark;

        var app = System.Windows.Application.Current;
        if (app is null)
        {
            return;
        }

        var accent = (ResourceDictionary)app.Resources;
        accent.MergedDictionaries.Clear();

        var colors = new ResourceDictionary
        {
            Source = new Uri(dark ? "Themes/Colors.Dark.xaml" : "Themes/Colors.Light.xaml", UriKind.Relative),
        };
        accent.MergedDictionaries.Add(colors);

        var controls = new ResourceDictionary
        {
            Source = new Uri("Themes/Controls.xaml", UriKind.Relative),
        };
        accent.MergedDictionaries.Add(controls);
    }
}
