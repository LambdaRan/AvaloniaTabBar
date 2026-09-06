using System.Collections;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Media;
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

    // ---------------------------------------------------------------- Fixed 模式

    [AvaloniaFact]
    public void TabWidthMode_Fixed_AllTabsSameExplicitWidth()
    {
        var tabView = new TabBar { TabWidthMode = TabBarWidthMode.Fixed };
        for (int i = 0; i < 4; i++)
            ((IList)tabView.Items).Add(new TabBarItem { Header = $"Tab {i + 1}" });
        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();
        tabView.SelectedIndex = 0;
        TestHelper.Pump(window);

        for (int i = 0; i < 4; i++) {
            var tab = (TabBarItem)tabView.ContainerFromIndex(i)!;
            Assert.Equal(160, tab.Width);
            Assert.Equal(160, tab.Bounds.Width);   // 实测宽度，不只是属性值
        }
        window.Close();
    }

    [AvaloniaFact]
    public void TabWidthMode_Fixed_WidthIndependentOfCount()
    {
        // 「互不影响」：2 条与 8 条时每条宽度相同。Equal 做不到这点。
        double WidthOfFirst(int count)
        {
            var tabView = new TabBar { TabWidthMode = TabBarWidthMode.Fixed };
            for (int i = 0; i < count; i++)
                ((IList)tabView.Items).Add(new TabBarItem { Header = $"T{i}" });
            var window = new Window { Width = 800, Height = 600, Content = tabView };
            window.Show();
            tabView.SelectedIndex = 0;
            TestHelper.Pump(window);
            double w = ((TabBarItem)tabView.ContainerFromIndex(0)!).Bounds.Width;
            window.Close();
            return w;
        }

        Assert.Equal(160, WidthOfFirst(2));
        Assert.Equal(160, WidthOfFirst(8));
    }

    [AvaloniaFact]
    public void TabWidthMode_Fixed_WidthIndependentOfViewport()
    {
        // 与 Equal 的根本区别：窗口变宽变窄，已存在标签宽度不跳。
        var tabView = new TabBar { TabWidthMode = TabBarWidthMode.Fixed };
        for (int i = 0; i < 3; i++)
            ((IList)tabView.Items).Add(new TabBarItem { Header = $"T{i}" });
        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();
        tabView.SelectedIndex = 0;
        TestHelper.Pump(window);

        var tab = (TabBarItem)tabView.ContainerFromIndex(0)!;
        Assert.Equal(160, tab.Bounds.Width);

        TestHelper.Pump(window, 1200, 600);
        Assert.Equal(160, tab.Bounds.Width);

        TestHelper.Pump(window, 500, 600);
        Assert.Equal(160, tab.Bounds.Width);
        window.Close();
    }

    [AvaloniaFact]
    public void TabWidthMode_Fixed_SetsFixedPseudoClass()
    {
        var tabView = new TabBar { TabWidthMode = TabBarWidthMode.Fixed };
        ((IList)tabView.Items).Add(new TabBarItem { Header = "A" });
        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();
        tabView.SelectedIndex = 0;
        TestHelper.Pump(window);

        var tab = (TabBarItem)tabView.ContainerFromIndex(0)!;
        Assert.True(tab.HasPseudoClass(":fixed"));
        Assert.False(tab.HasPseudoClass(":compact"));
        window.Close();
    }

    [AvaloniaFact]
    public void TabWidthMode_Fixed_OverflowDoesNotShrinkTabs()
    {
        // 总宽超视口 → 走现成的 ScrollViewer + 滚动条，标签本身不缩。
        var tabView = new TabBar { TabWidthMode = TabBarWidthMode.Fixed };
        for (int i = 0; i < 10; i++)
            ((IList)tabView.Items).Add(new TabBarItem { Header = $"T{i}" });
        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();
        tabView.SelectedIndex = 0;
        TestHelper.Pump(window);

        for (int i = 0; i < 10; i++)
            Assert.Equal(160, ((TabBarItem)tabView.ContainerFromIndex(i)!).Bounds.Width);

        var sv = TestHelper.Part<ScrollViewer>(tabView, "PART_TabStripScrollViewer");
        Assert.True(sv.Extent.Width > sv.Viewport.Width,
            $"10 × 160 应超出视口：extent={sv.Extent.Width} viewport={sv.Viewport.Width}");
        Assert.True(TestHelper.Part<Border>(tabView, "PART_ScrollThumb").IsVisible);
        window.Close();
    }
}
