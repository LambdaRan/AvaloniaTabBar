using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SimTabBar.Controls;

using Xunit;
using Avalonia.Headless.XUnit;

namespace SimTabBar.Tests;

public class TabViewMRUTests
{
    [AvaloniaFact]
    public void NextTabOnClose_Null_FallsBackToAdjacent()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(3);
        tabView.SelectedIndex = 1;

        var item = tabView.SelectedItem;
        Assert.NotNull(item);
        var selectedTab = item as TabBarItem
            ?? tabView.ContainerFromItem(item!) as TabBarItem;

        selectedTab?.RaiseCloseRequested();

        // 默认行为：选择相邻标签页（索引 1 被移除，索引 1 变为原来的索引 2）
        Assert.Equal(1, tabView.SelectedIndex);

        window.Close();
    }

    [AvaloniaFact]
    public void NextTabOnClose_ReturnsItem_SelectsReturnedItem()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(3);
        tabView.SelectedIndex = 2; // Select Tab 3

        // 获取 Tab 1 的引用
        var tab1 = tabView.Items[0] as TabBarItem
            ?? tabView.ContainerFromItem(tabView.Items[0]!) as TabBarItem;

        // 回调始终返回 Tab 1
        tabView.NextTabOnClose = _ => tab1;

        var item = tabView.SelectedItem;
        Assert.NotNull(item);
        var selectedTab = item as TabBarItem
            ?? tabView.ContainerFromItem(item!) as TabBarItem;

        selectedTab?.RaiseCloseRequested();

        // 应选择 Tab 1（而非相邻的 Tab 2）
        Assert.Same(tab1, tabView.SelectedItem);

        window.Close();
    }

    [AvaloniaFact]
    public void NextTabOnClose_ReturnsNull_FallsBackToAdjacent()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(3);
        tabView.SelectedIndex = 1;

        // 回调返回 null -> 回退到默认行为
        tabView.NextTabOnClose = _ => null;

        var item = tabView.SelectedItem;
        Assert.NotNull(item);
        var selectedTab = item as TabBarItem
            ?? tabView.ContainerFromItem(item!) as TabBarItem;

        selectedTab?.RaiseCloseRequested();

        Assert.Equal(1, tabView.SelectedIndex);

        window.Close();
    }

    [AvaloniaFact]
    public void SelectTab_ValidItem_SelectsItem()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(3);
        tabView.SelectedIndex = 0;

        var tab2 = tabView.Items[1] as TabBarItem
            ?? tabView.ContainerFromItem(tabView.Items[1]!) as TabBarItem;

        tabView.SelectTab(tab2!);

        Assert.Same(tab2, tabView.SelectedItem);

        window.Close();
    }

    [AvaloniaFact]
    public void SelectTab_InvalidItem_NoChange()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(3);
        tabView.SelectedIndex = 0;
        var original = tabView.SelectedItem;

        var foreignTab = new TabBarItem { Header = "Foreign" };
        tabView.SelectTab(foreignTab);

        // 不在集合中的项不应改变选择
        Assert.Same(original, tabView.SelectedItem);

        window.Close();
    }

    [AvaloniaFact]
    public void KeyboardHandling_Disabled_IgnoresCtrlTab()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(3);
        tabView.SelectedIndex = 0;
        tabView.IsBuiltInKeyboardHandlingEnabled = false;

        // 模拟 Ctrl+Tab 按键
        var eventArgs = new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = Key.Tab,
            KeyModifiers = KeyModifiers.Control,
            Source = tabView,
        };
        tabView.RaiseEvent(eventArgs);

        // 键盘处理已禁用，选择不应改变
        Assert.Equal(0, tabView.SelectedIndex);

        window.Close();
    }

    [AvaloniaFact]
    public void KeyboardHandling_Enabled_CtrlTabSelectsNext()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(3);
        tabView.SelectedIndex = 0;
        tabView.IsBuiltInKeyboardHandlingEnabled = true;

        var eventArgs = new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = Key.Tab,
            KeyModifiers = KeyModifiers.Control,
            Source = tabView,
        };
        tabView.RaiseEvent(eventArgs);

        Assert.Equal(1, tabView.SelectedIndex);

        window.Close();
    }
}
