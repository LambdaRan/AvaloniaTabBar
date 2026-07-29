using System.Collections;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using SimTabBar.Controls;

using Xunit;
using Avalonia.Headless.XUnit;

namespace SimTabBar.Tests;

public class TabViewAppearanceTests
{
    private static Color? GetBackgroundColor(TabBarItem tab)
    {
        return (tab.Background as SolidColorBrush)?.Color;
    }

    [AvaloniaFact]
    public void SelectedTab_HasDifferentBackground_FromUnselected()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(3);
        tabView.SelectedIndex = 0;

        var selectedTab = tabView.ContainerFromIndex(0) as TabBarItem;
        var unselectedTab = tabView.ContainerFromIndex(1) as TabBarItem;
        Assert.NotNull(selectedTab);
        Assert.NotNull(unselectedTab);

        // 验证 :selected 伪类已设置
        Assert.True(selectedTab!.HasPseudoClass(":selected"), "选中的标签页应具有 :selected 伪类");
        Assert.False(unselectedTab!.HasPseudoClass(":selected"), "未选中的标签页不应具有 :selected 伪类");

        var selectedColor = GetBackgroundColor(selectedTab);
        var unselectedColor = GetBackgroundColor(unselectedTab);

        Assert.NotNull(selectedColor);
        Assert.NotNull(unselectedColor);
        Assert.NotEqual(selectedColor, unselectedColor);

        window.Close();
    }

    [AvaloniaFact]
    public void SelectedTab_BackgroundIsNotTransparent()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(2);
        tabView.SelectedIndex = 0;

        var selectedTab = tabView.ContainerFromIndex(0) as TabBarItem;
        Assert.NotNull(selectedTab);

        var color = GetBackgroundColor(selectedTab!);
        Assert.NotNull(color);
        Assert.NotEqual(0u, color!.Value.ToUInt32()); // 非透明

        window.Close();
    }

    [AvaloniaFact]
    public void UnselectedTab_BackgroundIsTransparent()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(2);
        tabView.SelectedIndex = 0;

        var unselectedTab = tabView.ContainerFromIndex(1) as TabBarItem;
        Assert.NotNull(unselectedTab);

        var brush = unselectedTab!.Background as SolidColorBrush;
        Assert.NotNull(brush);
        // Alpha 值应为 0（完全透明）
        Assert.Equal(0, brush!.Color.A);

        window.Close();
    }

    [AvaloniaFact]
    public void NewlyAddedTab_WhenSelected_HasSelectedPseudoClass()
    {
        var tabView = new TabBar();
        ((IList)tabView.Items).Add(new TabBarItem { Header = "Tab 1" });
        ((IList)tabView.Items).Add(new TabBarItem { Header = "Tab 2" });

        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();

        // 添加一个新标签页并选中它
        var newTab = new TabBarItem { Header = "New Tab" };
        ((IList)tabView.Items).Add(newTab);
        tabView.SelectedIndex = 2;

        // 关键修复：新添加的选中标签页必须设置 :selected 伪类
        Assert.True(newTab.HasPseudoClass(":selected"),
            "新添加的选中标签页必须具有 :selected 伪类");

        // 之前选中的标签页应失去 :selected
        var oldTab = tabView.ContainerFromIndex(0) as TabBarItem;
        Assert.NotNull(oldTab);
        Assert.False(oldTab!.HasPseudoClass(":selected"),
            "之前选中的标签页不应具有 :selected 伪类");

        window.Close();
    }
}
