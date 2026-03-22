using System.Windows;
using HeatWatch.Core.Models;
using Microsoft.Win32;

namespace HeatWatch.Core.Theming;

public static class ThemeManager
{
    private const string DarkSource  = "pack://application:,,,/HeatWatch;component/Styles/Colors.xaml";
    private const string LightSource = "pack://application:,,,/HeatWatch;component/Styles/LightColors.xaml";

    public static void Apply(AppTheme theme)
    {
        var actual = theme == AppTheme.SystemDefault ? GetSystemTheme() : theme;
        var source = new Uri(actual == AppTheme.Light ? LightSource : DarkSource, UriKind.Absolute);

        var dicts = Application.Current.Resources.MergedDictionaries;

        // Find whichever colors dict is currently loaded (dark or light)
        var existing = dicts.FirstOrDefault(d =>
            d.Source?.OriginalString.EndsWith("Colors.xaml", StringComparison.OrdinalIgnoreCase) == true);

        var newDict = new ResourceDictionary { Source = source };

        if (existing != null)
            dicts[dicts.IndexOf(existing)] = newDict;
        else
            dicts.Insert(0, newDict);
    }

    private static AppTheme GetSystemTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key?.GetValue("AppsUseLightTheme") is int v && v == 1)
                return AppTheme.Light;
        }
        catch { }
        return AppTheme.Dark;
    }
}
