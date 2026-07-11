using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IPSwitcher.Helpers;
using IPSwitcher.Models;
using IPSwitcher.Services;
using Microsoft.Win32;

namespace IPSwitcher.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IProfileRepository _profileRepo;
    private readonly ISettingsStore _settingsStore;
    private readonly AdapterService _adapterService;
    private readonly NetworkConfigService _configService;
    private readonly CurrentConfigReader _configReader;

    private AppSettings _settings;

    public ObservableCollection<AdapterInfo> Adapters { get; } = new();

    public ObservableCollection<ProfileViewModel> Profiles { get; } = new();

    [ObservableProperty]
    private AdapterInfo? _selectedAdapter;

    [ObservableProperty]
    private ProfileViewModel? _selectedProfile;

    [ObservableProperty]
    private CurrentConfig _currentConfig = CurrentConfig.Empty;

    [ObservableProperty]
    private string _statusText = "就绪";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isDarkMode;

    private AppTheme _theme;
    public AppTheme Theme
    {
        get => _theme;
        set
        {
            if (SetProperty(ref _theme, value))
            {
                IsDarkMode = ThemeHelper.ResolveDark(value);
                _settings.Theme = value;
                _settingsStore.Save(_settings);
            }
        }
    }

    public bool IsStaticFieldsEnabled => SelectedProfile is null || !SelectedProfile.UseDhcp;

    public MainViewModel(
        IProfileRepository profileRepo,
        ISettingsStore settingsStore,
        AdapterService adapterService,
        NetworkConfigService configService,
        CurrentConfigReader configReader)
    {
        _profileRepo = profileRepo;
        _settingsStore = settingsStore;
        _adapterService = adapterService;
        _configService = configService;
        _configReader = configReader;

        _settings = _settingsStore.Load();
        _theme = _settings.Theme;
        IsDarkMode = ThemeHelper.ResolveDark(_theme);
    }

    public void Initialize()
    {
        LoadProfiles();
        RefreshAdapters();
    }

    private void LoadProfiles()
    {
        Profiles.Clear();
        foreach (var p in _profileRepo.Load())
        {
            Profiles.Add(new ProfileViewModel(p));
        }

        if (_settings.LastProfileId.HasValue)
        {
            var last = Profiles.FirstOrDefault(p => p.Id == _settings.LastProfileId.Value);
            if (last is not null)
            {
                SelectedProfile = last;
            }
        }

        if (SelectedProfile is null && Profiles.Count > 0)
        {
            SelectedProfile = Profiles[0];
        }

        if (Profiles.Count == 0)
        {
            var seed = new NetworkProfile
            {
                Name = "新配置",
                UseDhcp = true,
                SubnetMask = "255.255.255.0",
            };
            var vm = new ProfileViewModel(seed);
            Profiles.Add(vm);
            SelectedProfile = vm;
            PersistProfiles();
        }
    }

    private void PersistProfiles()
    {
        var list = Profiles.Select(p => p.Source).ToList();
        _profileRepo.Save(list);
    }

    [RelayCommand]
    public void RefreshAdapters()
    {
        Adapters.Clear();
        foreach (var a in _adapterService.GetAdapters())
        {
            Adapters.Add(a);
        }

        var lastName = SelectedAdapter?.Name ?? _settings.LastAdapterName;
        AdapterInfo? target = null;

        if (!string.IsNullOrWhiteSpace(lastName))
        {
            target = Adapters.FirstOrDefault(a =>
                string.Equals(a.Name, lastName, StringComparison.OrdinalIgnoreCase));
        }

        target ??= Adapters.FirstOrDefault(a => a.IsDefault);
        target ??= Adapters.FirstOrDefault(a => a.IsUp);
        target ??= Adapters.FirstOrDefault();

        SelectedAdapter = target;
        RefreshCurrentConfig();
    }

    private void RefreshCurrentConfig()
    {
        if (SelectedAdapter is null)
        {
            CurrentConfig = CurrentConfig.Empty;
            return;
        }
        CurrentConfig = _configReader.Read(SelectedAdapter.Name);
    }

    partial void OnSelectedAdapterChanged(AdapterInfo? value)
    {
        if (value is not null)
        {
            _settings.LastAdapterName = value.Name;
            _settingsStore.Save(_settings);
        }
        RefreshCurrentConfig();
    }

    private ProfileViewModel? _subscribedProfile;

    partial void OnSelectedProfileChanged(ProfileViewModel? value)
    {
        if (_subscribedProfile is not null)
        {
            _subscribedProfile.PropertyChanged -= OnSelectedProfileItemChanged;
            _subscribedProfile = null;
        }

        if (value is not null)
        {
            _settings.LastProfileId = value.Id;
            _settingsStore.Save(_settings);
            value.PropertyChanged += OnSelectedProfileItemChanged;
            _subscribedProfile = value;
        }
        OnPropertyChanged(nameof(IsStaticFieldsEnabled));
    }

    private void OnSelectedProfileItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProfileViewModel.UseDhcp))
        {
            OnPropertyChanged(nameof(IsStaticFieldsEnabled));
        }
    }

    [RelayCommand]
    public void AddProfile()
    {
        var seed = new NetworkProfile
        {
            Name = $"配置 {Profiles.Count + 1}",
            UseDhcp = true,
            SubnetMask = "255.255.255.0",
        };
        var vm = new ProfileViewModel(seed);
        Profiles.Add(vm);
        SelectedProfile = vm;
        PersistProfiles();
        StatusText = $"已新增配置「{seed.Name}」，请在右侧编辑。";
    }

    [RelayCommand]
    public void DeleteProfile()
    {
        if (SelectedProfile is null)
        {
            return;
        }
        var name = SelectedProfile.Name;
        Profiles.Remove(SelectedProfile);
        SelectedProfile = Profiles.LastOrDefault();
        PersistProfiles();
        StatusText = $"已删除配置「{name}」。";
    }

    [RelayCommand]
    public void SaveProfile()
    {
        if (SelectedProfile is null)
        {
            StatusText = "没有选中的配置。";
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedProfile.Name))
        {
            StatusText = "保存失败：配置名称不能为空。";
            return;
        }

        if (!SelectedProfile.UseDhcp)
        {
            if (!IPv4Validator.IsValidAddress(SelectedProfile.IpAddress))
            {
                StatusText = "保存失败：IP 地址格式无效。";
                return;
            }
            if (!IPv4Validator.IsValidSubnetMask(SelectedProfile.SubnetMask))
            {
                StatusText = "保存失败：子网掩码格式无效。";
                return;
            }
            if (!IPv4Validator.IsValidOptionalAddress(SelectedProfile.Gateway))
            {
                StatusText = "保存失败：网关地址格式无效。";
                return;
            }
            if (!IPv4Validator.IsValidOptionalAddress(SelectedProfile.PrimaryDns))
            {
                StatusText = "保存失败：首选 DNS 格式无效。";
                return;
            }
            if (!IPv4Validator.IsValidOptionalAddress(SelectedProfile.SecondaryDns))
            {
                StatusText = "保存失败：备用 DNS 格式无效。";
                return;
            }
            if (!string.IsNullOrWhiteSpace(SelectedProfile.SecondaryDns) &&
                string.IsNullOrWhiteSpace(SelectedProfile.PrimaryDns))
            {
                StatusText = "保存失败：设置备用 DNS 需同时设置首选 DNS。";
                return;
            }
        }

        SelectedProfile.WriteBackToSource();
        PersistProfiles();
        StatusText = $"配置「{SelectedProfile.Name}」已保存。";
    }

    [RelayCommand]
    public async Task ApplyProfileAsync()
    {
        if (SelectedAdapter is null)
        {
            StatusText = "请先选择一个网络适配器。";
            return;
        }
        if (SelectedProfile is null)
        {
            StatusText = "请先选择一个配置文件。";
            return;
        }

        IsBusy = true;
        StatusText = $"正在应用配置「{SelectedProfile.Name}」到「{SelectedAdapter.Name}」...";
        try
        {
            var result = await _configService.ApplyAsync(SelectedAdapter.Name, SelectedProfile.Source);
            StatusText = result.Message;
            await Task.Delay(500);
            RefreshCurrentConfig();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public void ExportProfiles()
    {
        if (Profiles.Count == 0)
        {
            StatusText = "没有可导出的配置。";
            return;
        }

        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
            FileName = "ipswitcher-profiles.json",
        };
        if (dlg.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var list = Profiles.Select(p => p.Source).ToList();
            _profileRepo.ExportToFile(dlg.FileName, list);
            StatusText = $"已导出 {list.Count} 个配置到 {Path.GetFileName(dlg.FileName)}。";
        }
        catch (Exception ex)
        {
            StatusText = $"导出失败：{ex.Message}";
        }
    }

    [RelayCommand]
    public void ImportProfiles()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
        };
        if (dlg.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var imported = _profileRepo.ImportFromFile(dlg.FileName);
            if (imported.Count == 0)
            {
                StatusText = "导入的文件中没有配置。";
                return;
            }

            var msg = $"导入 {imported.Count} 个配置。是否合并到现有列表？\n\n" +
                      "是 = 合并到现有列表\n" +
                      "否 = 替换现有列表\n" +
                      "取消 = 放弃导入";
            var r = System.Windows.MessageBox.Show(msg, "导入配置", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

            if (r == MessageBoxResult.Cancel)
            {
                StatusText = "已取消导入。";
                return;
            }

            if (r == MessageBoxResult.Yes)
            {
                foreach (var p in imported)
                {
                    p.Id = Guid.NewGuid();
                    Profiles.Add(new ProfileViewModel(p));
                }
                StatusText = $"已合并导入 {imported.Count} 个配置。";
            }
            else
            {
                Profiles.Clear();
                foreach (var p in imported)
                {
                    p.Id = Guid.NewGuid();
                    Profiles.Add(new ProfileViewModel(p));
                }
                SelectedProfile = Profiles.FirstOrDefault();
                StatusText = $"已替换为导入的 {imported.Count} 个配置。";
            }

            PersistProfiles();
        }
        catch (Exception ex)
        {
            StatusText = $"导入失败：{ex.Message}";
        }
    }

    public void PersistOnExit()
    {
        _settings.LastAdapterName = SelectedAdapter?.Name;
        _settings.LastProfileId = SelectedProfile?.Id;
        _settingsStore.Save(_settings);
        PersistProfiles();
    }

    public void OnClosingToTray()
    {
        _settings.LastAdapterName = SelectedAdapter?.Name;
        _settings.LastProfileId = SelectedProfile?.Id;
        _settingsStore.Save(_settings);
    }
}
