using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;

namespace SimTabBarDemo.Views;

public partial class ColorThemePage : UserControl
{
    private const string KeyBackground = "SimTabBarItemBackgroundSelected";
    private const string KeyForeground = "SimTabBarItemForegroundSelected";
    private const string KeyIndicator = "SimTabBarItemActiveIndicatorBrush";

    /// <summary>预设：暗档 / 亮档各一套（底色、文字），指示条两档共用一个强调色。</summary>
    private static readonly Dictionary<string, (string DarkBg, string DarkFg, string LightBg, string LightFg, string Accent)> Presets = new()
    {
        ["vscode"] = ("#1E1E1E", "#FFFFFF", "#FFFFFF", "#1E1E1E", "#007ACC"),
        ["warm"] = ("#2E2A24", "#F2E9DC", "#FBF3E4", "#3A3226", "#B06A10"),
        ["green"] = ("#1F2A22", "#E4F0E6", "#F0F6EE", "#22331F", "#1F7A3D"),
    };

    public ColorThemePage()
    {
        InitializeComponent();
    }

    private void OnPresetClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string key } || !Presets.TryGetValue(key, out var p)) return;

        Apply(
            dark: Spawn(p.DarkBg, p.DarkFg, p.Accent),
            light: Spawn(p.LightBg, p.LightFg, p.Accent));
    }

    private void OnResetClick(object? sender, RoutedEventArgs e)
    {
        // 空字典 = 这一层没有这些键 → 落回库内主题的默认值。
        Apply(new ResourceDictionary(), new ResourceDictionary());
    }

    /// <summary>
    /// 覆盖写在 Application.Resources 的 ThemeDictionaries 里（不是平铺键）——
    /// 明暗两档各有一套值，切主题时 DynamicResource 自己换成对应那份。
    /// 这是「运行期换配色」的用法，Armpi 的配色面板同理。
    /// </summary>
    private static void Apply(ResourceDictionary dark, ResourceDictionary light)
    {
        if (Application.Current?.Resources is not { } resources) return;

        resources.ThemeDictionaries[ThemeVariant.Dark] = dark;
        resources.ThemeDictionaries[ThemeVariant.Light] = light;
    }

    private static ResourceDictionary Spawn(string background, string foreground, string accent) => new()
    {
        [KeyBackground] = new SolidColorBrush(Color.Parse(background)),
        [KeyForeground] = new SolidColorBrush(Color.Parse(foreground)),
        [KeyIndicator] = new SolidColorBrush(Color.Parse(accent)),
    };
}
