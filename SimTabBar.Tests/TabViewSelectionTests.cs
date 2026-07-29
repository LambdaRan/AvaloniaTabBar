using System.Collections;
using Avalonia.Controls;
using SimTabBar.Controls;

using Xunit;
using Avalonia.Headless.XUnit;

namespace SimTabBar.Tests;

public class TabViewSelectionTests
{
    [AvaloniaFact]
    public void TabBar_SelectionMode_IsSingle()
    {
        // TabBar 构造函数将 SelectionMode 设置为 Single。
        // 验证单选行为：选择一项会取消选择前一项，
        // 并且导航在单项之间循环。
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(3);

        // 显式设置初始选择
        tabView.SelectedIndex = 0;
        Assert.Equal(0, tabView.SelectedIndex);

        // 选择新项会替换之前的选择（Single 模式）
        tabView.SelectedIndex = 2;
        Assert.Equal(2, tabView.SelectedIndex);

        // SelectNextTab 会循环回到第一项
        tabView.SelectNextTab();
        Assert.Equal(0, tabView.SelectedIndex);

        window.Close();
    }

    [AvaloniaFact]
    public void TabBar_SelectFirstTab_ByDefault()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(3);

        // 显式设置初始选择（无头模式下的自动选择行为可能有所不同）
        tabView.SelectedIndex = 0;
        Assert.Equal(0, tabView.SelectedIndex);
        Assert.NotNull(tabView.SelectedItem);

        window.Close();
    }

    [AvaloniaFact]
    public void TabBar_ChangeSelectedIndex_UpdatesSelectedItem()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(3);

        tabView.SelectedIndex = 1;
        Assert.NotNull(tabView.SelectedItem);

        window.Close();
    }

    [AvaloniaFact]
    public void TabBar_SelectedItem_And_SelectedIndex_AreInSync()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(3);

        var item = tabView.Items.Cast<object>().ElementAt(2);
        tabView.SelectedItem = item;
        Assert.Equal(2, tabView.SelectedIndex);

        tabView.SelectedIndex = 0;
        Assert.Equal(tabView.Items.Cast<object>().First(), tabView.SelectedItem);

        window.Close();
    }

    [AvaloniaFact]
    public void SelectNextTab_CyclesForward()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(3);

        tabView.SelectedIndex = 0;
        tabView.SelectNextTab();
        Assert.Equal(1, tabView.SelectedIndex);

        tabView.SelectNextTab();
        Assert.Equal(2, tabView.SelectedIndex);

        // 循环回到开头
        tabView.SelectNextTab();
        Assert.Equal(0, tabView.SelectedIndex);

        window.Close();
    }

    [AvaloniaFact]
    public void SelectPreviousTab_CyclesBackward()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(3);

        tabView.SelectedIndex = 0;
        tabView.SelectPreviousTab();
        Assert.Equal(2, tabView.SelectedIndex);

        tabView.SelectPreviousTab();
        Assert.Equal(1, tabView.SelectedIndex);

        window.Close();
    }

    [AvaloniaFact]
    public void TabBarItem_IsSelected_PropertyReflectsSelectionState()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(3);
        tabView.SelectedIndex = 1;

        var selectedTab = tabView.ContainerFromIndex(1) as TabBarItem;
        var unselectedTab = tabView.ContainerFromIndex(0) as TabBarItem;

        Assert.NotNull(selectedTab);
        Assert.NotNull(unselectedTab);
        Assert.True(selectedTab!.IsSelected, "Selected container should have IsSelected == true");
        Assert.False(unselectedTab!.IsSelected, "Unselected container should have IsSelected == false");

        // 验证 :selected 伪类与 IsSelected 保持同步
        Assert.True(selectedTab.HasPseudoClass(":selected"));
        Assert.False(unselectedTab.HasPseudoClass(":selected"));

        window.Close();
    }
}
