using System.Collections;
using System.Linq;
using Avalonia.Controls;
using SimTabBar.Controls;

using Xunit;
using Avalonia.Headless.XUnit;

namespace SimTabBar.Tests;

public class TabViewContextMenuTests
{
    // --- CloseOtherTabsCommand（关闭其他标签页命令）---

    [AvaloniaFact]
    public void CloseOtherTabsCommand_ClosesAllExceptSelected()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(4);
        tabView.SelectedIndex = 1;

        tabView.CloseOtherTabsCommand.Execute(null);

        Assert.Equal(1, tabView.ItemCount);
        Assert.Equal(0, tabView.SelectedIndex);

        window.Close();
    }

    [AvaloniaFact]
    public void CloseOtherTabsCommand_SkipsNonClosableTabs()
    {
        var tabView = new TabBar();
        var pinned = new TabBarItem { Header = "Pinned", IsClosable = false };
        var tab2 = new TabBarItem { Header = "Tab 2" };
        var tab3 = new TabBarItem { Header = "Tab 3" };
        ((IList)tabView.Items).Add(pinned);
        ((IList)tabView.Items).Add(tab2);
        ((IList)tabView.Items).Add(tab3);

        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();

        tabView.SelectedIndex = 1; // Select tab2
        tabView.CloseOtherTabsCommand.Execute(null);

        // pinned（固定标签页）+ tab2 保留（tab3 被关闭）
        Assert.Equal(2, tabView.ItemCount);

        window.Close();
    }

    [AvaloniaFact]
    public void CloseOtherTabsCommand_CanExecute_FalseWithOneTab()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(1);

        Assert.False(tabView.CloseOtherTabsCommand.CanExecute(null));

        window.Close();
    }

    [AvaloniaFact]
    public void CloseOtherTabsCommand_CanExecute_FalseWhenAllOthersPinned()
    {
        var tabView = new TabBar();
        var selected = new TabBarItem { Header = "Selected" };
        var pinned1 = new TabBarItem { Header = "Pinned1", IsClosable = false };
        var pinned2 = new TabBarItem { Header = "Pinned2", IsClosable = false };
        ((IList)tabView.Items).Add(selected);
        ((IList)tabView.Items).Add(pinned1);
        ((IList)tabView.Items).Add(pinned2);

        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();

        tabView.SelectedIndex = 0;

        Assert.False(tabView.CloseOtherTabsCommand.CanExecute(null));

        window.Close();
    }

    [AvaloniaFact]
    public void CloseOtherTabsCommand_Cancel_PreventsIndividualClose()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(4);
        tabView.SelectedIndex = 0;

        // 取消关闭索引为 2 的标签页
        tabView.TabCloseRequested += (_, args) =>
        {
            if (args.Tab.Header?.ToString() == "Tab 3")
                args.Cancel = true;
        };

        tabView.CloseOtherTabsCommand.Execute(null);

        // Tab 1（选中）+ Tab 3（取消关闭）保留
        Assert.Equal(2, tabView.ItemCount);

        window.Close();
    }

    // --- CloseAllTabsCommand（关闭所有标签页命令）---

    [AvaloniaFact]
    public void CloseAllTabsCommand_ClosesAllTabs()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(4);

        tabView.CloseAllTabsCommand.Execute(null);

        Assert.Equal(0, tabView.ItemCount);

        window.Close();
    }

    [AvaloniaFact]
    public void CloseAllTabsCommand_SkipsNonClosableTabs()
    {
        var tabView = new TabBar();
        var pinned = new TabBarItem { Header = "Pinned", IsClosable = false };
        var tab2 = new TabBarItem { Header = "Tab 2" };
        var tab3 = new TabBarItem { Header = "Tab 3" };
        ((IList)tabView.Items).Add(pinned);
        ((IList)tabView.Items).Add(tab2);
        ((IList)tabView.Items).Add(tab3);

        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();

        tabView.CloseAllTabsCommand.Execute(null);

        // 仅固定标签页保留
        Assert.Equal(1, tabView.ItemCount);
        var remaining = tabView.ContainerFromIndex(0) as TabBarItem;
        Assert.Equal("Pinned", remaining?.Header?.ToString());

        window.Close();
    }

    [AvaloniaFact]
    public void CloseAllTabsCommand_CanExecute_FalseWhenAllPinned()
    {
        var tabView = new TabBar();
        var pinned1 = new TabBarItem { Header = "P1", IsClosable = false };
        var pinned2 = new TabBarItem { Header = "P2", IsClosable = false };
        ((IList)tabView.Items).Add(pinned1);
        ((IList)tabView.Items).Add(pinned2);

        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();

        Assert.False(tabView.CloseAllTabsCommand.CanExecute(null));

        window.Close();
    }

    [AvaloniaFact]
    public void CloseAllTabsCommand_CanExecute_TrueWithMixedTabs()
    {
        var tabView = new TabBar();
        var pinned = new TabBarItem { Header = "Pinned", IsClosable = false };
        var closable = new TabBarItem { Header = "Closable" };
        ((IList)tabView.Items).Add(pinned);
        ((IList)tabView.Items).Add(closable);

        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();

        Assert.True(tabView.CloseAllTabsCommand.CanExecute(null));

        window.Close();
    }

    // --- CloseTabCommand（关闭标签页命令）---

    [AvaloniaFact]
    public void CloseTabCommand_ClosesSelectedTab()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(3);
        tabView.SelectedIndex = 1;

        tabView.CloseTabCommand.Execute(null);

        Assert.Equal(2, tabView.ItemCount);
    }

    [AvaloniaFact]
    public void CloseTabCommand_DoesNothingWhenNonClosable()
    {
        var tabView = new TabBar();
        var pinned = new TabBarItem { Header = "Pinned", IsClosable = false };
        var tab2 = new TabBarItem { Header = "Tab 2" };
        ((IList)tabView.Items).Add(pinned);
        ((IList)tabView.Items).Add(tab2);

        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();

        tabView.SelectedIndex = 0;
        tabView.CloseTabCommand.Execute(null);

        // 固定标签页未被关闭
        Assert.Equal(2, tabView.ItemCount);

        window.Close();
    }

    // --- 上下文菜单附加 ---

    [AvaloniaFact]
    public void ContextMenu_NotAttachedByDefault()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(3);

        var tab = tabView.ContainerFromIndex(0) as TabBarItem;
        Assert.NotNull(tab);
        Assert.Null(tab!.ContextMenu);

        window.Close();
    }

    [AvaloniaFact]
    public void ContextMenu_AttachedWhenEnabled()
    {
        var tabView = new TabBar { IsTabContextMenuEnabled = true };
        for (int i = 0; i < 3; i++)
            ((IList)tabView.Items).Add(new TabBarItem { Header = $"Tab {i + 1}" });

        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();

        var tab = tabView.ContainerFromIndex(0) as TabBarItem;
        Assert.NotNull(tab);
        Assert.NotNull(tab!.ContextMenu);

        window.Close();
    }

    [AvaloniaFact]
    public void ContextMenu_NotAttachedWhenExplicitlyDisabled()
    {
        var tabView = new TabBar { IsTabContextMenuEnabled = false };
        for (int i = 0; i < 3; i++)
            ((IList)tabView.Items).Add(new TabBarItem { Header = $"Tab {i + 1}" });

        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();

        var tab = tabView.ContainerFromIndex(0) as TabBarItem;
        Assert.NotNull(tab);
        Assert.Null(tab!.ContextMenu);

        window.Close();
    }

    [AvaloniaFact]
    public void ContextMenu_UsesCustomMenuWhenProvided()
    {
        var customMenu = new ContextMenu
        {
            Items = { new MenuItem { Header = "Custom" } }
        };

        var tabView = new TabBar { IsTabContextMenuEnabled = true, TabItemContextMenu = customMenu };
        for (int i = 0; i < 2; i++)
            ((IList)tabView.Items).Add(new TabBarItem { Header = $"Tab {i + 1}" });

        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();

        var tab = tabView.ContainerFromIndex(0) as TabBarItem;
        Assert.NotNull(tab);
        Assert.Same(customMenu, tab!.ContextMenu);

        window.Close();
    }

    // --- CanExecute 更新 ---

    [AvaloniaFact]
    public void CloseOtherTabsCommand_CanExecute_UpdatesWhenTabsRemoved()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(2);
        tabView.SelectedIndex = 0;

        Assert.True(tabView.CloseOtherTabsCommand.CanExecute(null));

        // 关闭另一个标签页
        tabView.CloseOtherTabsCommand.Execute(null);

        // 现在只剩 1 个标签页，命令应被禁用
        Assert.False(tabView.CloseOtherTabsCommand.CanExecute(null));

        window.Close();
    }
}
