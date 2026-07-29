using System.Collections;
using System.Linq;
using Avalonia.Controls;
using Avalonia.VisualTree;
using SimTabBar.Controls;

using Xunit;
using Avalonia.Headless.XUnit;

namespace SimTabBar.Tests;

public class TabViewCloseTests
{
    [AvaloniaFact]
    public void TabBarItem_IsClosable_DefaultTrue()
    {
        var tab = new TabBarItem();
        Assert.True(tab.IsClosable);
    }

    [AvaloniaFact]
    public void TabBarItem_RaiseCloseRequested_WhenNotClosable_DoesNotRaiseEvent()
    {
        var tab = new TabBarItem { IsClosable = false };
        bool raised = false;
        tab.CloseRequested += (_, _) => raised = true;

        tab.RaiseCloseRequested();

        Assert.False(raised);
    }

    [AvaloniaFact]
    public void TabBarItem_RaiseCloseRequested_WhenClosable_RaisesEvent()
    {
        var tab = new TabBarItem { Header = "Test" };
        bool raised = false;
        tab.CloseRequested += (_, _) => raised = true;

        var window = new Window { Content = tab };
        window.Show();

        tab.RaiseCloseRequested();

        Assert.True(raised);

        window.Close();
    }

    [AvaloniaFact]
    public void TabBar_CancelClose_PreventsRemoval()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(3);
        int initialCount = tabView.ItemCount;

        tabView.TabCloseRequested += (_, args) => args.Cancel = true;

        // 模拟对选中标签页发起关闭请求
        tabView.SelectedIndex = 0;
        var item = tabView.SelectedItem;
        Assert.NotNull(item);
        var selectedTab = item as TabBarItem
            ?? tabView.ContainerFromItem(item!) as TabBarItem;

        selectedTab?.RaiseCloseRequested();

        Assert.Equal(initialCount, tabView.ItemCount);

        window.Close();
    }

    [AvaloniaFact]
    public void TabBar_DefaultClose_RemovesTabAndSelectsAdjacent()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(3);
        tabView.SelectedIndex = 1;

        var item = tabView.SelectedItem;
        Assert.NotNull(item);
        var selectedTab = item as TabBarItem
            ?? tabView.ContainerFromItem(item!) as TabBarItem;

        selectedTab?.RaiseCloseRequested();

        Assert.Equal(2, tabView.ItemCount);
        // 移除索引 1 后，选中的应变为 min(1, count-1) = 1
        Assert.Equal(1, tabView.SelectedIndex);

        window.Close();
    }

    [AvaloniaFact]
    public void TabBar_CloseLastRemainingTab_SelectsNothing()
    {
        var tabView = new TabBar();
        var tab = new TabBarItem { Header = "Only" };
        ((IList)tabView.Items).Add(tab);

        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();

        tab.RaiseCloseRequested();

        Assert.Equal(0, tabView.ItemCount);
        Assert.Equal(-1, tabView.SelectedIndex);

        window.Close();
    }

    [AvaloniaFact]
    public void CloseButtonOverlayMode_Auto_ShowsWhenOneTab()
    {
        var tabView = new TabBar();
        tabView.CloseButtonOverlayMode = TabBarCloseButtonOverlayMode.Auto;
        var tab = new TabBarItem { Header = "Only" };
        ((IList)tabView.Items).Add(tab);

        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();

        // Auto 模式下只有 1 个标签页：关闭按钮应可见（不折叠）
        Assert.False(tab.HasPseudoClass("closecollapsed"));

        window.Close();
    }

    [AvaloniaFact]
    public void CloseButtonOverlayMode_Always_ShowsEvenWithOneTab()
    {
        var tabView = new TabBar();
        tabView.CloseButtonOverlayMode = TabBarCloseButtonOverlayMode.Always;
        var tab = new TabBarItem { Header = "Only" };
        ((IList)tabView.Items).Add(tab);

        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();

        // Always 模式：关闭按钮不应折叠
        Assert.False(tab.HasPseudoClass("closecollapsed"));
        Assert.True(tab.HasPseudoClass("closealways"));

        window.Close();
    }

    [AvaloniaFact]
    public void CloseButtonOverlayMode_OnPointerOver_SelectedTabShowsAlways()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(3);
        tabView.CloseButtonOverlayMode = TabBarCloseButtonOverlayMode.OnPointerOver;
        tabView.SelectedIndex = 0;

        var selectedTab = tabView.ContainerFromIndex(0) as TabBarItem;
        Assert.NotNull(selectedTab);

        // 选中的标签页：应始终显示关闭按钮
        Assert.False(selectedTab!.HasPseudoClass("closecollapsed"));
        Assert.True(selectedTab.HasPseudoClass("closealways"));
        Assert.False(selectedTab.HasPseudoClass("closeoverlay"));

        window.Close();
    }

    [AvaloniaFact]
    public void CloseButtonOverlayMode_OnPointerOver_UnselectedTabShowsOnHover()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(3);
        tabView.CloseButtonOverlayMode = TabBarCloseButtonOverlayMode.OnPointerOver;
        tabView.SelectedIndex = 0;

        var unselectedTab = tabView.ContainerFromIndex(1) as TabBarItem;
        Assert.NotNull(unselectedTab);

        // 未选中的标签页：折叠但带有 overlay（悬停时显示）
        Assert.True(unselectedTab!.HasPseudoClass("closecollapsed"));
        Assert.False(unselectedTab.HasPseudoClass("closealways"));
        Assert.True(unselectedTab.HasPseudoClass("closeoverlay"));

        window.Close();
    }

    [AvaloniaFact]
    public void CloseButtonOverlayMode_Auto_OneTab_NoHoverOverride()
    {
        var tabView = new TabBar();
        tabView.CloseButtonOverlayMode = TabBarCloseButtonOverlayMode.Auto;
        var tab = new TabBarItem { Header = "Only" };
        ((IList)tabView.Items).Add(tab);

        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();

        // Auto 模式下只有 1 个标签页：不折叠，无 closeoverlay，无 closealways
        Assert.False(tab.HasPseudoClass("closecollapsed"));
        Assert.False(tab.HasPseudoClass("closeoverlay"));
        Assert.False(tab.HasPseudoClass("closealways"));

        window.Close();
    }

    [AvaloniaFact]
    public void IsClosable_False_NoHoverOverride_InAnyMode()
    {
        // 验证 IsClosable=false 的标签页在任何模式下都不显示关闭按钮
        foreach (var mode in new[]
        {
            TabBarCloseButtonOverlayMode.Auto,
            TabBarCloseButtonOverlayMode.OnPointerOver,
            TabBarCloseButtonOverlayMode.Always
        })
        {
            var tabView = new TabBar();
            tabView.CloseButtonOverlayMode = mode;
            var tab = new TabBarItem { Header = "Pinned", IsClosable = false };
            ((IList)tabView.Items).Add(tab);
            ((IList)tabView.Items).Add(new TabBarItem { Header = "Other" });

            var window = new Window { Width = 800, Height = 600, Content = tabView };
            window.Show();

            Assert.True(tab.HasPseudoClass("closecollapsed"));
            Assert.False(tab.HasPseudoClass("closeoverlay"));
            Assert.False(tab.HasPseudoClass("closealways"));

            window.Close();
        }
    }

    private static Button? FindCloseButton(TabBarItem tab)
    {
        return tab.GetVisualDescendants()
            .OfType<Button>()
            .FirstOrDefault(b => b.Name == "PART_CloseButton");
    }

    [AvaloniaFact]
    public void CloseButton_IsVisible_Auto_MultipleTabs_ShowsButton()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(3);
        tabView.CloseButtonOverlayMode = TabBarCloseButtonOverlayMode.Auto;

        // 强制布局以确保模板已应用

        var tab = tabView.ContainerFromIndex(0) as TabBarItem;
        Assert.NotNull(tab);

        var btn = FindCloseButton(tab!);
        Assert.NotNull(btn);
        Assert.True(btn!.IsVisible, "Close button should be visible in Auto mode with 3 tabs");

        window.Close();
    }

    [AvaloniaFact]
    public void CloseButton_IsVisible_Auto_OneTab_ShowsButton()
    {
        var tabView = new TabBar();
        tabView.CloseButtonOverlayMode = TabBarCloseButtonOverlayMode.Auto;
        var tab = new TabBarItem { Header = "Only" };
        ((IList)tabView.Items).Add(tab);

        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();

        var btn = FindCloseButton(tab);
        Assert.NotNull(btn);
        Assert.True(btn!.IsVisible, "Close button should be visible in Auto mode even with 1 tab");

        window.Close();
    }

    [AvaloniaFact]
    public void CloseButton_IsVisible_Always_OneTab_ShowsButton()
    {
        var tabView = new TabBar();
        tabView.CloseButtonOverlayMode = TabBarCloseButtonOverlayMode.Always;
        var tab = new TabBarItem { Header = "Only" };
        ((IList)tabView.Items).Add(tab);

        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();

        var btn = FindCloseButton(tab);
        Assert.NotNull(btn);
        Assert.True(btn!.IsVisible, "Close button should be visible in Always mode even with 1 tab");

        window.Close();
    }

    [AvaloniaFact]
    public void CloseButton_IsVisible_OnPointerOver_SelectedShows_UnselectedHides()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(3);
        tabView.CloseButtonOverlayMode = TabBarCloseButtonOverlayMode.OnPointerOver;
        tabView.SelectedIndex = 0;
        tabView.UpdateAllTabVisuals();

        var selectedTab = tabView.ContainerFromIndex(0) as TabBarItem;
        var unselectedTab = tabView.ContainerFromIndex(1) as TabBarItem;
        Assert.NotNull(selectedTab);
        Assert.NotNull(unselectedTab);

        var selectedBtn = FindCloseButton(selectedTab!);
        var unselectedBtn = FindCloseButton(unselectedTab!);

        Assert.NotNull(selectedBtn);
        Assert.NotNull(unselectedBtn);
        Assert.True(selectedBtn!.IsVisible, "Selected tab close button should be visible in OnPointerOver mode");
        Assert.False(unselectedBtn!.IsVisible, "Unselected tab close button should be hidden in OnPointerOver mode");

        window.Close();
    }

    [AvaloniaFact]
    public void TabBar_Close_WithDetachedTab_HandlesGracefully()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(2);
        tabView.SelectedIndex = 0;

        // 验证 IndexFromContainer 对孤立容器返回 -1
        var orphanContainer = new TabBarItem();
        int index = tabView.IndexFromContainer(orphanContainer);
        Assert.Equal(-1, index);

        window.Close();
    }
}
