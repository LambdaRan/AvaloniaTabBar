using System.Collections;
using Avalonia.Controls;
using SimTabBar.Controls;

using Xunit;
using Avalonia.Headless.XUnit;

namespace SimTabBar.Tests;

public class TabViewWidthTests
{
    [AvaloniaFact]
    public void TabWidthMode_Default_IsEqual()
    {
        var tabView = new TabBar();
        Assert.Equal(TabBarWidthMode.Equal, tabView.TabWidthMode);
    }

    [AvaloniaFact]
    public void TabWidthMode_SizeToContent_SetsWidthToAuto()
    {
        var tabView = new TabBar();
        tabView.TabWidthMode = TabBarWidthMode.SizeToContent;

        var tab = new TabBarItem { Header = "Test" };
        ((IList)tabView.Items).Add(tab);

        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();

        // SizeToContent：宽度应为 NaN（自动）
        Assert.True(double.IsNaN(tab.Width));

        window.Close();
    }

    [AvaloniaFact]
    public void TabWidthMode_Compact_UnselectedTabsAreNarrow()
    {
        var tabView = new TabBar();
        tabView.TabWidthMode = TabBarWidthMode.Compact;

        for (int i = 0; i < 3; i++)
        {
            ((IList)tabView.Items).Add(new TabBarItem { Header = $"Tab {i + 1}" });
        }

        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();

        // 确保选中一个标签页，以便 Compact 模式能区分选中与未选中
        tabView.SelectedIndex = 0;

        // 选中的标签页（索引 0）宽度应大于 36
        var selectedTab = tabView.ContainerFromIndex(0) as TabBarItem;
        Assert.NotNull(selectedTab);
        Assert.True(selectedTab!.Width > 36,
            $"Expected selected tab Width > 36, but was {selectedTab.Width}");

        // 未选中的标签页宽度应为 36px
        for (int i = 1; i < 3; i++)
        {
            var unselectedTab = tabView.ContainerFromIndex(i) as TabBarItem;
            Assert.NotNull(unselectedTab);
            Assert.Equal(36, unselectedTab!.Width);
        }

        window.Close();
    }

    [AvaloniaFact]
    public void IsAddTabButtonVisible_DefaultTrue()
    {
        var tabView = new TabBar();
        Assert.True(tabView.IsAddTabButtonVisible);
    }

    [AvaloniaFact]
    public void TabBarItem_IsClosable_False_SetsPseudoClass()
    {
        var tab = new TabBarItem { IsClosable = false };
        Assert.True(tab.HasPseudoClass(":closecollapsed"));
    }
}
