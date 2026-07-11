using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace IPSwitcher.ViewModels;

public enum ThemeTag
{
    System = 0,
    Light = 1,
    Dark = 2,
}

public sealed class NullToBoolConverter : IValueConverter
{
    public static readonly NullToBoolConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public sealed class InverseBoolConverter : IValueConverter
{
    public static readonly InverseBoolConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return !b;
        }
        return true;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return !b;
        }
        return true;
    }
}

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public static readonly BoolToVisibilityConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b && b)
        {
            return Visibility.Visible;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Visibility v)
        {
            return v == Visibility.Visible;
        }
        return false;
    }
}

public sealed class NetworkCategoryToIndexConverter : IValueConverter
{
    public static readonly NetworkCategoryToIndexConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is IPSwitcher.Models.NetworkCategory c)
        {
            return c switch
            {
                IPSwitcher.Models.NetworkCategory.Public => 1,
                IPSwitcher.Models.NetworkCategory.Private => 2,
                _ => 0,
            };
        }
        return 0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int i)
        {
            return i switch
            {
                1 => IPSwitcher.Models.NetworkCategory.Public,
                2 => IPSwitcher.Models.NetworkCategory.Private,
                _ => null,
            };
        }
        return null;
    }
}
