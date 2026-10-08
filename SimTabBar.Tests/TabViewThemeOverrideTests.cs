using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using SimTabBar.Controls;
using Xunit;

namespace SimTabBar.Tests;

/// <summary>
/// 「选中态三色可被使用者覆盖」是一份**契约**，不是巧合 —— 这张网把它钉住。
///
/// 主题里的三个键走的是 <c>{DynamicResource}</c>（`^:selected` 的 setter 与
/// `PART_ActiveIndicator` 的模板绑定），所以使用者在更外层放同名键就能改：
/// 控件 / Window / Application 三级都行，深色浅色可以用 ThemeDictionaries 分档，
/// 运行期改运行期字典也立即生效（Armpi 的配色面板走的就是最后这条）。
///
/// 谁要是把 DynamicResource 换成 StaticResource、或者把 token 挪进 ControlTheme
/// 自己的资源里，这几条就会红 —— 那时下游的「用户自定义配色」会静默失效，
/// 而不是报错。
///
/// ⚠️ 凡是临时改写共享 `Application.Current.Resources` 的几条（测试宿主是单例），
/// 都挂进 DisableParallelization 的 collection，别跟别的测试并行。
/// </summary>
[Collection("SimTabBar.AppResourceMutation")]
public class TabViewThemeOverrideTests
{
    private const string KeyBg = "SimTabBarItemBackgroundSelected";
    private const string KeyFg = "SimTabBarItemForegroundSelected";
    private const string KeyIndicator = "SimTabBarItemActiveIndicatorBrush";

    /// <summary>覆盖用的标记色 —— 故意选一个绝不会与库内默认撞的值，撞了说明覆盖没生效。</summary>
    private static readonly Color Override = Color.FromRgb(0xDE, 0xAD, 0xBE);
    private static readonly IBrush OverrideBrush = new SolidColorBrush(Override);

    /// <summary>Armpi 的形状：自定义 ThemeVariant + InheritVariant 落回 Dark。</summary>
    private static readonly ThemeVariant CustomDark = new("SimTabBarTestDark", ThemeVariant.Dark);

    private static (TabBar tabView, Window window, TabBarItem selected) Build(
        ThemeVariant? variant = null,
        Action<Window>? beforeShow = null,
        ThemeVariant? appVariant = null)
    {
        var tabView = new TabBar();
        for (var i = 0; i < 2; i++) ((IList)tabView.Items).Add(new TabBarItem { Header = $"T{i}" });

        var window = new Window { Width = 800, Height = 600, Content = tabView };
        if (appVariant is not null) Application.Current!.RequestedThemeVariant = appVariant;
        else if (variant is not null) window.RequestedThemeVariant = variant;

        beforeShow?.Invoke(window);
        window.Show();
        tabView.SelectedIndex = 0;
        TestHelper.Pump(window);
        return (tabView, window, (TabBarItem)tabView.ContainerFromIndex(0)!);
    }

    private static Color? ColorOf(IBrush? brush) => (brush as SolidColorBrush)?.Color;
    private static Color? Background(TabBarItem tab) => ColorOf(tab.Background);
    private static Color? Foreground(TabBarItem tab) => ColorOf(tab.Foreground);
    private static Color? Indicator(TabBarItem tab) =>
        ColorOf(TestHelper.Part<Border>(tab, "PART_ActiveIndicator").Background);

    [AvaloniaFact]
    public void WindowResources_Override_SelectedBackground()
    {
        var (_, window, tab) = Build(ThemeVariant.Dark,
            win => win.Resources[KeyBg] = OverrideBrush);

        Assert.Equal(Override, Background(tab));
        window.Close();
    }

    [AvaloniaFact]
    public void WindowResources_Override_SelectedForeground()
    {
        var (_, window, tab) = Build(ThemeVariant.Dark,
            win => win.Resources[KeyFg] = OverrideBrush);

        Assert.Equal(Override, Foreground(tab));
        window.Close();
    }

    /// <summary>指示条长在模板子元素上，宿主链与上面两条不同，单独钉一条。</summary>
    [AvaloniaFact]
    public void WindowResources_Override_ActiveIndicator()
    {
        var (_, window, tab) = Build(ThemeVariant.Dark,
            win => win.Resources[KeyIndicator] = OverrideBrush);

        Assert.Equal(Override, Indicator(tab));
        window.Close();
    }

    /// <summary>
    /// Application.Resources 这一级：Avalonia 文档的查找顺序里它既是第 6 档
    /// （theme resources 之前）又可能被算成第 5 档（挂在 Application.Styles 上的
    /// StyleInclude），实测**赢**。这条是整张网里最该留住的一条。
    /// </summary>
    [AvaloniaFact]
    public void ApplicationResources_Override_SelectedBackground()
    {
        var app = Application.Current!;
        var saved = app.Resources;
        try
        {
            app.Resources = new ResourceDictionary { [KeyBg] = OverrideBrush };
            var (_, window, tab) = Build(ThemeVariant.Dark);

            Assert.Equal(Override, Background(tab));
            window.Close();
        }
        finally { app.Resources = saved; }
    }

    /// <summary>demo 场景页与最常见用法：App 级分档字典，明暗各自给值。</summary>
    [AvaloniaFact]
    public void AppThemeDictionary_Override_DarkVariant()
    {
        var app = Application.Current!;
        var saved = app.Resources;
        try
        {
            app.Resources = new ResourceDictionary();
            app.Resources.ThemeDictionaries[ThemeVariant.Dark] =
                new ResourceDictionary { [KeyBg] = OverrideBrush };

            var (_, window, tab) = Build(ThemeVariant.Dark);

            Assert.Equal(Override, Background(tab));
            window.Close();
        }
        finally { app.Resources = saved; }
    }

    /// <summary>Armpi 那条路：自定义档 + 分档字典，明暗各配一套。</summary>
    [AvaloniaFact]
    public void AppThemeDictionary_Override_CustomVariant()
    {
        var app = Application.Current!;
        var savedResources = app.Resources;
        var savedVariant = app.RequestedThemeVariant;
        try
        {
            app.Resources = new ResourceDictionary();
            app.Resources.ThemeDictionaries[CustomDark] =
                new ResourceDictionary { [KeyBg] = OverrideBrush };

            var (_, window, tab) = Build(appVariant: CustomDark);

            Assert.Equal(Override, Background(tab));
            window.Close();
        }
        finally
        {
            app.Resources = savedResources;
            app.RequestedThemeVariant = savedVariant;
        }
    }

    /// <summary>用户选完色立即生效 —— 运行期改运行期字典，不重启。</summary>
    [AvaloniaFact]
    public void RuntimeOverride_TakesEffectImmediately()
    {
        var app = Application.Current!;
        var savedResources = app.Resources;
        var savedVariant = app.RequestedThemeVariant;
        try
        {
            app.Resources = new ResourceDictionary();
            var palette = new ResourceDictionary { [KeyBg] = new SolidColorBrush(Colors.Black) };
            app.Resources.ThemeDictionaries[CustomDark] = palette;

            var (_, window, tab) = Build(appVariant: CustomDark);
            Assert.NotEqual(Override, Background(tab));

            palette[KeyBg] = OverrideBrush;   // 用户在配色面板里挑了色
            TestHelper.Pump(window);

            Assert.Equal(Override, Background(tab));
            window.Close();
        }
        finally
        {
            app.Resources = savedResources;
            app.RequestedThemeVariant = savedVariant;
        }
    }

    /// <summary>
    /// 阴性对照：分档键放错变体（Light 的值在 Dark 下）不许生效。
    /// 没有这条，「覆盖生效」可能只是分档被忽略的假象。
    /// </summary>
    [AvaloniaFact]
    public void ThemeDictionaryOverride_WrongVariant_DoesNotApply()
    {
        var (_, window, tab) = Build(ThemeVariant.Dark, win =>
            win.Resources.ThemeDictionaries[ThemeVariant.Light] =
                new ResourceDictionary { [KeyBg] = OverrideBrush });

        Assert.NotEqual(Override, Background(tab));
        window.Close();
    }

    /// <summary>README 里那张默认值表不许悄悄漂 —— 表里的值就是这里的值。</summary>
    [AvaloniaFact]
    public void Defaults_MatchDocumentedPalette()
    {
        var (_, darkWindow, darkTab) = Build(ThemeVariant.Dark);
        var (_, lightWindow, lightTab) = Build(ThemeVariant.Light);

        Assert.Equal(Color.Parse("#1E1E1E"), Background(darkTab));
        Assert.Equal(Color.Parse("#FFFFFF"), Foreground(darkTab));
        Assert.Equal(Color.Parse("#007ACC"), Indicator(darkTab));

        Assert.Equal(Color.Parse("#FFFFFF"), Background(lightTab));
        Assert.Equal(Color.Parse("#1E1E1E"), Foreground(lightTab));
        Assert.Equal(Color.Parse("#007ACC"), Indicator(lightTab));

        darkWindow.Close();
        lightWindow.Close();
    }
}

/// <summary>要改写共享 Application 资源的几条，禁止与其他测试并行。</summary>
[CollectionDefinition("SimTabBar.AppResourceMutation", DisableParallelization = true)]
public class AppResourceMutationCollection;
