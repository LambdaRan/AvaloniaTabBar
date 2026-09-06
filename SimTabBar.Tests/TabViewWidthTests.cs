using System.Collections;
using System.Collections.ObjectModel;
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

    [AvaloniaFact]
    public void TabWidthMode_Fixed_NarrowerThanMinWidth_NotClamped()
    {
        // MinWidth 墙的回归测试。ControlTheme 给每个 item 设了 MinWidth=100
        // （SimTabBarTheme.axaml:144），布局会把显式 Width 钳进 [MinWidth, MaxWidth]。
        // 必须断言实测 Bounds.Width 而不是 Width 属性 —— 后者是 60，
        // 前者在未放开 MinWidth 时会被夹成 100，只断言 Width 会假绿。
        var tabView = new TabBar { TabWidthMode = TabBarWidthMode.Fixed };
        tabView.Resources.Add("SimTabBarItemFixedWidth", 60.0);   // < 默认 MinWidth 100
        for (int i = 0; i < 3; i++)
            ((IList)tabView.Items).Add(new TabBarItem { Header = $"T{i}" });
        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();
        tabView.SelectedIndex = 0;
        TestHelper.Pump(window);

        for (int i = 0; i < 3; i++) {
            var tab = (TabBarItem)tabView.ContainerFromIndex(i)!;
            Assert.Equal(60, tab.Width);
            Assert.Equal(0, tab.MinWidth);          // :fixed 已放开下限
            Assert.Equal(60, tab.Bounds.Width);     // 实测未被夹回 100
        }
        window.Close();
    }

    [AvaloniaFact]
    public void TabWidthMode_SwitchFromFixedToEqual_RestoresMinWidthClamp()
    {
        // SetFixed(false) 的复位链。从 Fixed 切回 Equal 后，:fixed 必须清掉，
        // 否则残留的 MinWidth=0 会让 Equal 的钳位下限失效。
        var tabView = new TabBar { TabWidthMode = TabBarWidthMode.Fixed };
        tabView.Resources.Add("SimTabBarItemFixedWidth", 60.0);
        for (int i = 0; i < 12; i++)
            ((IList)tabView.Items).Add(new TabBarItem { Header = $"T{i}" });
        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();
        tabView.SelectedIndex = 0;
        TestHelper.Pump(window);

        var tab = (TabBarItem)tabView.ContainerFromIndex(0)!;
        Assert.Equal(0, tab.MinWidth);
        Assert.Equal(60, tab.Bounds.Width);

        tabView.TabWidthMode = TabBarWidthMode.Equal;
        TestHelper.Pump(window);

        Assert.False(tab.HasPseudoClass(":fixed"));
        Assert.Equal(100, tab.MinWidth);            // ControlTheme 的 setter 重新生效
        // 12 条标签平分 800px 窗口的视口，每条都远低于 MinWidth 100
        // → Math.Clamp（TabBar.cs:744）把它夹回 100。这与视口的精确值无关，
        // 任何 < 1200 的视口都成立，故断言不依赖具体算术。
        Assert.Equal(100, tab.Bounds.Width);
        window.Close();
    }

    [AvaloniaFact]
    public void TabWidthMode_Fixed_ContainerRelease_ClearsFixedState()
    {
        // ResetManagedVisualState() 中的 SetFixed(false)。
        // 镜像 RegressionTests.cs:706-724 的 Compact 版复位测试。
        var items = new ObservableCollection<SessionLikeItem>
        {
            new() { Title = "a" }, new() { Title = "b" },
        };
        var tabView = new TabBar
        {
            TabWidthMode = TabBarWidthMode.Fixed,
            HeaderMemberPath = nameof(SessionLikeItem.Title),
            ItemsSource = items,
        };
        tabView.Resources.Add("SimTabBarItemFixedWidth", 60.0);
        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();
        tabView.SelectedIndex = 0;
        TestHelper.Pump(window);

        var container = (TabBarItem)tabView.ContainerFromIndex(1)!;
        Assert.True(container.HasPseudoClass(":fixed"));
        Assert.Equal(60, container.Bounds.Width);

        items.RemoveAt(1);
        TestHelper.Pump(window);

        Assert.False(container.HasPseudoClass(":fixed"));
        Assert.True(double.IsNaN(container.Width));
        // 不断言 MinWidth：容器已脱离可视化树，Avalonia 停止对其应用主题样式，
        // 释放态 MinWidth 值不受控（既有 Compact 复位测试同样不断言，见
        // RegressionTests.cs:706-724）。附着容器上 :fixed 清除后 MinWidth 恢复
        // 100 的行为已由 SwitchFromFixedToEqual_RestoresMinWidthClamp 钉住。
        window.Close();
    }
}
