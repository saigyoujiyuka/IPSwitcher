namespace IPSwitcher.Models;

public enum AppTheme
{
    System = 0,
    Light = 1,
    Dark = 2,
}

public sealed class AppSettings
{
    public AppTheme Theme { get; set; } = AppTheme.System;

    public string? LastAdapterName { get; set; }

    public Guid? LastProfileId { get; set; }

    public bool MinimizeToTray { get; set; } = true;
}
