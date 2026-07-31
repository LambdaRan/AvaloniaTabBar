using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using SimTabBar.Controls;
using Xunit;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;

namespace SimTabBar.Tests;

/// <summary>
/// 针对已修复缺陷的回归测试。
/// 关键点：这里断言的是**渲染结果**（Bounds.Width / IsVisible / 事件 Handled），
/// 而不仅仅是属性值 —— 原先的测试只断言属性，导致"Width=36 但实际渲染 100"
/// 这类缺陷长期通过测试。
/// </summary>
public class RegressionTests
{
    /// <summary>推进布局与合成。命中测试需要一次渲染 tick，否则合成树是陈旧的。</summary>
    private static void Pump(Window window, double w = 800, double h = 600)
    {
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.Measure(new Size(w, h));
        window.Arrange(new Rect(0, 0, w, h));
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
    }

    private static T Part<T>(Visual root, string name) where T : Control =>
        root.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    private static T? PartOrNull<T>(Visual root, string name) where T : Control =>
        root.GetVisualDescendants().OfType<T>().FirstOrDefault(c => c.Name == name);

    private static void RaiseLeftPress(TabBarItem tab)
    {
        var args = new PointerPressedEventArgs(tab, new Pointer(1, PointerType.Mouse, true),
            tab, new Point(5, 5), 0,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed),
            KeyModifiers.None) { RoutedEvent = InputElement.PointerPressedEvent };
        tab.RaiseEvent(args);
    }

    // ---------------------------------------------------------------- 紧凑模式

    [AvaloniaFact]
    public void Compact_UnselectedTabs_RenderAtCompactWidth_NotClampedByMinWidth()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(3);
        tabView.TabWidthMode = TabBarWidthMode.Compact;
        tabView.SelectedIndex = 0;
        Pump(window);

        for (int i = 1; i < 3; i++)
        {
            var tab = (TabBarItem)tabView.ContainerFromIndex(i)!;
            // 属性与实际渲染宽度都必须是 36：主题里 MinWidth=100 曾把它夹回 100。
            Assert.Equal(36, tab.Width);
            Assert.Equal(36, tab.Bounds.Width);
        }

        var selected = (TabBarItem)tabView.ContainerFromIndex(0)!;
        Assert.True(selected.Bounds.Width > 36,
            $"selected tab should expand, was {selected.Bounds.Width}");

        window.Close();
    }

    [AvaloniaFact]
    public void Compact_HidesHeaderText_AndCloseButton()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(3);
        tabView.TabWidthMode = TabBarWidthMode.Compact;
        tabView.CloseButtonOverlayMode = TabBarCloseButtonOverlayMode.Always;
        tabView.SelectedIndex = 0;
        Pump(window);

        var compact = (TabBarItem)tabView.ContainerFromIndex(1)!;
        // 伪类必须带冒号，否则主题选择器 "^:compact" 永远匹配不上。
        Assert.True(compact.HasPseudoClass(":compact"));
        Assert.False(Part<ContentPresenter>(compact, "PART_HeaderPresenter").IsVisible);
        // 36px 放不下图标 + 关闭按钮，紧凑标签页不显示关闭按钮。
        Assert.False(Part<Button>(compact, "PART_CloseButton").IsVisible);
        // 图标跨满三列并居中，否则会贴在 36px 标签的左边缘。
        var icon = Part<ContentPresenter>(compact, "PART_IconPresenter");
        Assert.Equal(3, Grid.GetColumnSpan(icon));

        // 选中的标签页不是紧凑态，表头与关闭按钮照常显示。
        var selected = (TabBarItem)tabView.ContainerFromIndex(0)!;
        Assert.False(selected.HasPseudoClass(":compact"));
        Assert.True(Part<ContentPresenter>(selected, "PART_HeaderPresenter").IsVisible);
        Assert.True(Part<Button>(selected, "PART_CloseButton").IsVisible);

        window.Close();
    }

    [AvaloniaFact]
    public void Compact_LeavingCompactMode_RestoresWidth()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(3);
        tabView.TabWidthMode = TabBarWidthMode.Compact;
        tabView.SelectedIndex = 0;
        Pump(window);
        Assert.Equal(36, ((TabBarItem)tabView.ContainerFromIndex(1)!).Bounds.Width);

        tabView.TabWidthMode = TabBarWidthMode.Equal;
        Pump(window);

        var tab = (TabBarItem)tabView.ContainerFromIndex(1)!;
        Assert.False(tab.HasPseudoClass(":compact"));
        Assert.True(tab.Bounds.Width > 36);
        Assert.True(Part<ContentPresenter>(tab, "PART_HeaderPresenter").IsVisible);

        window.Close();
    }

    // ------------------------------------------------------------ 宽度与溢出

    [AvaloniaFact]
    public void Equal_TabsFitViewport_NoOverflow()
    {
        var tabView = new TabBar { TabWidthMode = TabBarWidthMode.Equal };
        for (int i = 0; i < 6; i++) ((IList)tabView.Items).Add(new TabBarItem { Header = $"T{i}" });
        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();
        tabView.SelectedIndex = 0;
        Pump(window);

        var sv = Part<ScrollViewer>(tabView, "PART_TabStripScrollViewer");
        // 旧实现用 Grid.Bounds 手工减 Header/Footer/AddButton，漏掉了它们的 Margin
        // （共 16px），导致 Equal 模式恒定溢出并冒出滚动滑块。
        Assert.True(sv.Extent.Width <= sv.Viewport.Width + 1,
            $"tab strip overflowed: extent={sv.Extent.Width} viewport={sv.Viewport.Width}");
        Assert.False(Part<Border>(tabView, "PART_ScrollThumb").IsVisible);

        window.Close();
    }

    [AvaloniaFact]
    public void Equal_WidthsShrink_WhenTabStripHeaderAdded()
    {
        var tabView = new TabBar { TabWidthMode = TabBarWidthMode.Equal };
        for (int i = 0; i < 4; i++) ((IList)tabView.Items).Add(new TabBarItem { Header = $"T{i}" });
        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();
        tabView.SelectedIndex = 0;
        Pump(window);
        double before = ((TabBarItem)tabView.ContainerFromIndex(0)!).Bounds.Width;

        tabView.TabStripHeader = new Border { Width = 200, Height = 20 };
        Pump(window);

        double after = ((TabBarItem)tabView.ContainerFromIndex(0)!).Bounds.Width;
        // 旧实现只监听 PART_TabStripGrid 的 SizeChanged，而它的尺寸不随 Header 变化，
        // 所以宽度永远不重算，标签条长期溢出。
        Assert.True(after < before, $"widths should shrink: {before} -> {after}");

        var sv = Part<ScrollViewer>(tabView, "PART_TabStripScrollViewer");
        Assert.True(sv.Extent.Width <= sv.Viewport.Width + 1,
            $"overflow after header added: extent={sv.Extent.Width} viewport={sv.Viewport.Width}");

        window.Close();
    }

    [AvaloniaFact]
    public void CompactWidth_HonoursResourceOverride()
    {
        // 宽度资源键原先写成 "TabBarItemCompactWidth"，与主题里的
        // "SimTabBarItemCompactWidth" 不匹配，使用方的覆盖值被静默忽略。
        var tabView = new TabBar { TabWidthMode = TabBarWidthMode.Compact };
        tabView.Resources["SimTabBarItemCompactWidth"] = 48d;
        for (int i = 0; i < 3; i++) ((IList)tabView.Items).Add(new TabBarItem { Header = $"T{i}" });
        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();
        tabView.SelectedIndex = 0;
        Pump(window);

        Assert.Equal(48, ((TabBarItem)tabView.ContainerFromIndex(1)!).Bounds.Width);

        window.Close();
    }

    [AvaloniaFact]
    public void Equal_WidthsGrow_WhenAddButtonHidden()
    {
        // IsAddTabButtonVisible 的显式处理器已移除，改由 ScrollViewer 的
        // Viewport 订阅统一驱动 —— 确认这条路径确实生效。
        var tabView = new TabBar { TabWidthMode = TabBarWidthMode.Equal };
        // 4 个标签：宽度落在 [MinWidth 100, MaxWidth 240] 之间，不会被夹住，
        // 这样可用宽度的变化才观察得到。
        for (int i = 0; i < 4; i++) ((IList)tabView.Items).Add(new TabBarItem { Header = $"T{i}" });
        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();
        tabView.SelectedIndex = 0;
        Pump(window);
        double before = ((TabBarItem)tabView.ContainerFromIndex(0)!).Bounds.Width;

        tabView.IsAddTabButtonVisible = false;
        Pump(window);

        double after = ((TabBarItem)tabView.ContainerFromIndex(0)!).Bounds.Width;
        Assert.True(after > before, $"widths should grow when add button hidden: {before} -> {after}");
        window.Close();
    }

    [AvaloniaFact]
    public void Equal_NoSpuriousScrollbar_FromLayoutRounding()
    {
        // UseLayoutRounding 默认为 true，会把每个标签页的宽度**向上**取整
        // （151.2 -> 152），n 个标签累加后超出视口，凭空冒出滚动条。
        // 只要宽度没被 MinWidth/MaxWidth 夹住，就绝不该出现溢出。
        var misfires = new List<string>();

        foreach (var winW in new[] { 800.0, 801.0, 823.0, 900.0, 1000.0, 1024.0, 1280.0, 1366.0 })
        {
            for (int n = 1; n <= 9; n++)
            {
                var tabView = new TabBar { TabWidthMode = TabBarWidthMode.Equal };
                for (int i = 0; i < n; i++) ((IList)tabView.Items).Add(new TabBarItem { Header = $"T{i}" });
                var window = new Window { Width = winW, Height = 600, Content = tabView };
                window.Show();
                tabView.SelectedIndex = 0;
                Pump(window, winW, 600);

                var t0 = (TabBarItem)tabView.ContainerFromIndex(0)!;
                var sv = Part<ScrollViewer>(tabView, "PART_TabStripScrollViewer");
                var thumb = Part<Border>(tabView, "PART_ScrollThumb");

                // 被 MinWidth 夹住时溢出是设计预期（标签不再继续变窄）。
                bool clampedByMinWidth = t0.Width <= t0.MinWidth + 0.001;
                if (!clampedByMinWidth && sv.Extent.Width > sv.Viewport.Width + 1)
                {
                    misfires.Add($"win={winW} n={n} width={t0.Width} bounds={t0.Bounds.Width} " +
                                 $"extent={sv.Extent.Width} viewport={sv.Viewport.Width} thumb={thumb.IsVisible}");
                }
                window.Close();
            }
        }

        Assert.Empty(misfires);
    }

    [AvaloniaFact]
    public void Equal_ScrollbarAppears_OnlyAfterMinWidthReached()
    {
        // 800px 窗口、视口 756、MinWidth=100 -> 7 个标签刚好不溢出（108 each），
        // 第 8 个开始被夹到 100，8*100=800 > 756，此时才该出现滚动条。
        var tabView = new TabBar { TabWidthMode = TabBarWidthMode.Equal };
        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();

        for (int n = 1; n <= 7; n++)
        {
            ((IList)tabView.Items).Add(new TabBarItem { Header = $"T{n}" });
            if (n == 1) tabView.SelectedIndex = 0;
            Pump(window);
            Assert.False(Part<Border>(tabView, "PART_ScrollThumb").IsVisible,
                $"{n} 个标签不该出现滚动条");
        }

        ((IList)tabView.Items).Add(new TabBarItem { Header = "T8" });
        Pump(window);
        var t0 = (TabBarItem)tabView.ContainerFromIndex(0)!;
        Assert.Equal(t0.MinWidth, t0.Width);   // 已经夹到 MinWidth
        Assert.True(Part<Border>(tabView, "PART_ScrollThumb").IsVisible,
            "宽度已到 MinWidth 且总长超出视口，应出现滚动条");

        window.Close();
    }

    [AvaloniaFact]
    public void ScrollThumb_IsTransparentUntilPointerOver()
    {
        var tabView = new TabBar { TabWidthMode = TabBarWidthMode.Equal };
        for (int i = 0; i < 10; i++) ((IList)tabView.Items).Add(new TabBarItem { Header = $"T{i}" });
        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();
        tabView.SelectedIndex = 0;
        Pump(window);

        var thumb = Part<Border>(tabView, "PART_ScrollThumb");
        var grid = Part<Grid>(tabView, "PART_TabStripGrid");
        // 设计如此：滚动指示条是悬停才淡入的浮层，IsVisible 与可见性不等价。
        Assert.True(thumb.IsVisible);
        Assert.Equal(0, thumb.Opacity);

        grid.RaiseEvent(new PointerEventArgs(InputElement.PointerEnteredEvent, grid,
            new Pointer(1, PointerType.Mouse, true), grid, new Point(5, 5), 0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.Other), KeyModifiers.None));
        Assert.Equal(0.8, thumb.Opacity);

        window.Close();
    }

    // ------------------------------------------------------------ 关闭语义

    [AvaloniaFact]
    public void ClosingBackgroundTab_KeepsSelection()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(4);
        tabView.SelectedIndex = 3;
        Pump(window);
        var selectedBefore = tabView.SelectedItem;

        // 关闭索引 0（非选中项）
        ((TabBarItem)tabView.ContainerFromIndex(0)!).RaiseCloseRequested();
        Pump(window);

        Assert.Equal(3, tabView.ItemCount);
        // 旧实现无条件执行 SelectedIndex = min(removedIndex, count-1)，
        // 把选中项从 Tab 4 甩到了 Tab 2。
        Assert.Same(selectedBefore, tabView.SelectedItem);
        Assert.Equal(2, tabView.SelectedIndex); // 索引前移，但仍是同一个对象

        window.Close();
    }

    [AvaloniaFact]
    public void ClosingBackgroundTab_DoesNotInvokeNextTabOnClose()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(3);
        tabView.SelectedIndex = 2;
        Pump(window);
        bool called = false;
        tabView.NextTabOnClose = _ => { called = true; return null; };

        ((TabBarItem)tabView.ContainerFromIndex(0)!).RaiseCloseRequested();
        Pump(window);

        Assert.False(called, "NextTabOnClose 只应在关闭当前选中标签页时调用");
        window.Close();
    }

    [AvaloniaFact]
    public void ClosingSelectedTab_StillSelectsAdjacent()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(3);
        tabView.SelectedIndex = 1;
        Pump(window);

        ((TabBarItem)tabView.ContainerFromIndex(1)!).RaiseCloseRequested();
        Pump(window);

        Assert.Equal(2, tabView.ItemCount);
        Assert.Equal(1, tabView.SelectedIndex);
        window.Close();
    }

    [AvaloniaFact]
    public void CloseRequested_Item_IsContainer_InDirectChildrenMode()
    {
        var tabView = new TabBar();
        var a = new TabBarItem { Header = "A" };
        var b = new TabBarItem { Header = "B" };
        ((IList)tabView.Items).Add(a);
        ((IList)tabView.Items).Add(b);
        // 页面级 DataContext —— TabBarItem 会继承它。
        var vm = new object();
        var window = new Window { Width = 800, Height = 600, Content = tabView, DataContext = vm };
        window.Show();
        Pump(window);

        var reported = new List<object?>();
        tabView.TabCloseRequested += (_, e) => { reported.Add(e.Item); e.Cancel = true; };
        a.RaiseCloseRequested();
        b.RaiseCloseRequested();

        // 旧实现用 "DataContext ?? this"，两个标签页都会报告同一个页面 VM。
        Assert.Equal(new object?[] { a, b }, reported);
        Assert.DoesNotContain(vm, reported);

        window.Close();
    }

    [AvaloniaFact]
    public void CloseRequested_Item_IsDataItem_InItemsSourceMode()
    {
        var items = new ObservableCollection<Doc> { new() { Title = "one" }, new() { Title = "two" } };
        var tabView = new TabBar { HeaderMemberPath = nameof(Doc.Title), ItemsSource = items };
        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();
        tabView.SelectedIndex = 0;
        Pump(window);

        object? reported = null;
        tabView.TabCloseRequested += (_, e) => { reported = e.Item; e.Cancel = true; };
        ((TabBarItem)tabView.ContainerFromIndex(1)!).RaiseCloseRequested();

        Assert.Same(items[1], reported);
        window.Close();
    }

    [AvaloniaFact]
    public void Close_RemovesCorrectItem_WhenSourceHasDuplicates()
    {
        // 同一个对象在集合里出现两次时，按值 Remove 会删掉第一个。
        var dup = new Doc { Title = "dup" };
        var items = new ObservableCollection<Doc> { dup, new() { Title = "middle" }, dup };
        var tabView = new TabBar { HeaderMemberPath = nameof(Doc.Title), ItemsSource = items };
        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();
        tabView.SelectedIndex = 1;
        Pump(window);

        ((TabBarItem)tabView.ContainerFromIndex(2)!).RaiseCloseRequested();
        Pump(window);

        Assert.Equal(2, items.Count);
        Assert.Same(dup, items[0]);
        Assert.Equal("middle", items[1].Title);
        window.Close();
    }

    // -------------------------------------------------- 成员路径改为真正的绑定

    [AvaloniaFact]
    public void HeaderMemberPath_TracksPropertyChanges()
    {
        var items = new ObservableCollection<Doc> { new() { Title = "one" } };
        var tabView = new TabBar { HeaderMemberPath = nameof(Doc.Title), ItemsSource = items };
        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();
        tabView.SelectedIndex = 0;
        Pump(window);

        Assert.Equal("one", ((TabBarItem)tabView.ContainerFromIndex(0)!).Header);
        items[0].Title = "CHANGED";
        Pump(window);
        // 旧实现只在容器创建时反射取值一次，INotifyPropertyChanged 完全被忽略。
        Assert.Equal("CHANGED", ((TabBarItem)tabView.ContainerFromIndex(0)!).Header);

        window.Close();
    }

    [AvaloniaFact]
    public void HeaderMemberPath_SupportsNestedPath()
    {
        var items = new ObservableCollection<Doc> { new() { Inner = new Inner { Name = "deep" } } };
        var tabView = new TabBar { HeaderMemberPath = "Inner.Name", ItemsSource = items };
        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();
        tabView.SelectedIndex = 0;
        Pump(window);

        // 旧实现用 Type.GetProperty(name)，"A.B" 这种路径直接解析失败返回 null。
        Assert.Equal("deep", ((TabBarItem)tabView.ContainerFromIndex(0)!).Header);
        window.Close();
    }

    [AvaloniaFact]
    public void HeaderMemberPath_RuntimeChange_RefreshesContainers()
    {
        var items = new ObservableCollection<Doc> { new() { Title = "one" } };
        var tabView = new TabBar { HeaderMemberPath = nameof(Doc.Title), ItemsSource = items };
        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();
        tabView.SelectedIndex = 0;
        Pump(window);
        Assert.Equal("one", ((TabBarItem)tabView.ContainerFromIndex(0)!).Header);

        tabView.HeaderMemberPath = nameof(Doc.Alternate);
        Pump(window);

        Assert.Equal("ALT-one", ((TabBarItem)tabView.ContainerFromIndex(0)!).Header);
        window.Close();
    }

    [AvaloniaFact]
    public void ContentTemplate_RuntimeChange_RefreshesContainers()
    {
        var items = new ObservableCollection<string> { "x", "y" };
        var tabView = new TabBar { ItemsSource = items };
        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();
        tabView.SelectedIndex = 0;
        Pump(window);

        var template = new FuncDataTemplate<string>((s, _) => new TextBlock { Text = "T:" + s });
        tabView.ContentTemplate = template;
        Pump(window);

        Assert.Same(template, ((TabBarItem)tabView.ContainerFromIndex(0)!).ContentTemplate);
        Assert.Same(template, Part<ContentPresenter>(tabView, "PART_TabContentPresenter").ContentTemplate);
        window.Close();
    }

    [AvaloniaFact]
    public void IconSourceMemberPath_NonIconValue_IsIgnored()
    {
        // 路径解析出来的不是 IconSource 时，静默取 null（与旧的 "as IconSource" 行为一致）。
        var items = new ObservableCollection<Doc> { new() { Title = "one" } };
        var tabView = new TabBar { IconSourceMemberPath = nameof(Doc.Title), ItemsSource = items };
        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();
        tabView.SelectedIndex = 0;
        Pump(window);

        Assert.Null(((TabBarItem)tabView.ContainerFromIndex(0)!).IconSource);
        window.Close();
    }

    // ------------------------------------------------------------ 滚轮与焦点

    [AvaloniaFact]
    public void Wheel_NotHandled_WhenNoOverflow()
    {
        var tabView = new TabBar();
        ((IList)tabView.Items).Add(new TabBarItem { Header = "A" });
        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();
        tabView.SelectedIndex = 0;
        Pump(window);

        var sv = Part<ScrollViewer>(tabView, "PART_TabStripScrollViewer");
        Assert.True(sv.Extent.Width <= sv.Viewport.Width, "precondition: no overflow");

        var args = Wheel(sv);
        sv.RaiseEvent(args);

        // 旧实现无条件 e.Handled = true，导致 TabBar 放在可滚动页面里时，
        // 指针悬停于标签条上滚轮完全失灵。
        Assert.False(args.Handled);
        window.Close();
    }

    [AvaloniaFact]
    public void Wheel_Handled_WhenOverflowing()
    {
        var tabView = new TabBar { TabWidthMode = TabBarWidthMode.SizeToContent };
        for (int i = 0; i < 12; i++)
            ((IList)tabView.Items).Add(new TabBarItem { Header = $"A fairly long tab header {i}" });
        var window = new Window { Width = 300, Height = 400, Content = tabView };
        window.Show();
        tabView.SelectedIndex = 0;
        Pump(window, 300, 400);

        var sv = Part<ScrollViewer>(tabView, "PART_TabStripScrollViewer");
        Assert.True(sv.Extent.Width > sv.Viewport.Width, "precondition: overflow");

        var args = Wheel(sv);
        sv.RaiseEvent(args);

        Assert.True(args.Handled);
        Assert.True(sv.Offset.X > 0);
        window.Close();
    }

    private static PointerWheelEventArgs Wheel(Control target) =>
        new(target, new Pointer(1, PointerType.Mouse, true), target, new Point(10, 10), 0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.Other),
            KeyModifiers.None, new Vector(0, -1)) { RoutedEvent = InputElement.PointerWheelChangedEvent };

    [AvaloniaFact]
    public void ClickingTab_FocusesTabBar_SoCtrlTabWorks()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(3);
        tabView.SelectedIndex = 0;
        Pump(window);

        RaiseLeftPress((TabBarItem)tabView.ContainerFromIndex(1)!);
        Pump(window);

        Assert.Equal(1, tabView.SelectedIndex);
        // 旧实现下 TabBar/TabBarItem 都不可获焦，点击后无人持有键盘焦点，
        // TabBar.OnKeyDown 永远收不到事件，Ctrl+Tab 等快捷键形同虚设。
        Assert.Same(tabView, window.FocusManager?.GetFocusedElement());

        window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.Control);
        Pump(window);
        Assert.Equal(2, tabView.SelectedIndex);

        window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.Control | RawInputModifiers.Shift);
        Pump(window);
        Assert.Equal(1, tabView.SelectedIndex);

        window.Close();
    }

    [AvaloniaFact]
    public void CtrlF4_ClosesSelectedTab()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(3);
        tabView.SelectedIndex = 1;
        Pump(window);
        RaiseLeftPress((TabBarItem)tabView.ContainerFromIndex(1)!);
        Pump(window);

        window.KeyPressQwerty(PhysicalKey.F4, RawInputModifiers.Control);
        Pump(window);

        Assert.Equal(2, tabView.ItemCount);
        window.Close();
    }

    [AvaloniaFact]
    public void BuiltInKeyboardHandling_CanBeDisabled()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(3);
        tabView.IsBuiltInKeyboardHandlingEnabled = false;
        tabView.SelectedIndex = 0;
        Pump(window);
        RaiseLeftPress((TabBarItem)tabView.ContainerFromIndex(0)!);
        Pump(window);

        window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.Control);
        Pump(window);

        Assert.Equal(0, tabView.SelectedIndex);
        window.Close();
    }

    // ---------------------------------------------------------- 滚动滑块状态

    [AvaloniaFact]
    public void ThumbOpacity_ResetsAfterDragEnds()
    {
        var tabView = new TabBar { TabWidthMode = TabBarWidthMode.SizeToContent };
        for (int i = 0; i < 12; i++)
            ((IList)tabView.Items).Add(new TabBarItem { Header = $"A fairly long tab header {i}" });
        var window = new Window { Width = 300, Height = 400, Content = tabView };
        window.Show();
        tabView.SelectedIndex = 0;
        Pump(window, 300, 400);

        var thumb = Part<Border>(tabView, "PART_ScrollThumb");
        var grid = Part<Grid>(tabView, "PART_TabStripGrid");
        Assert.True(thumb.IsVisible, "precondition: thumb visible");

        // 指针进入标签条 -> 滑块淡入
        grid.RaiseEvent(new PointerEventArgs(InputElement.PointerEnteredEvent, grid,
            new Pointer(2, PointerType.Mouse, true), grid, new Point(5, 5), 0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.Other), KeyModifiers.None));
        Assert.Equal(0.8, thumb.Opacity);

        // 开始拖动滑块
        var press = new PointerPressedEventArgs(thumb, new Pointer(2, PointerType.Mouse, true),
            thumb, new Point(2, 1), 0,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed),
            KeyModifiers.None) { RoutedEvent = InputElement.PointerPressedEvent };
        thumb.RaiseEvent(press);

        // 拖动过程中指针移出标签条：PointerExited 因为正在拖动而跳过淡出
        grid.RaiseEvent(new PointerEventArgs(InputElement.PointerExitedEvent, grid,
            new Pointer(2, PointerType.Mouse, true), grid, new Point(-5, -5), 0,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other), KeyModifiers.None));
        Assert.Equal(0.8, thumb.Opacity);

        // 结束拖动：旧实现不复位透明度，滑块永久停留在 0.8
        tabView.RaiseEvent(new PointerCaptureLostEventArgs(tabView, new Pointer(2, PointerType.Mouse, true)));
        Assert.Equal(0, thumb.Opacity);

        window.Close();
    }

    // -------------------------------------------------------------- 容器清理

    [AvaloniaFact]
    public void RemovedContainer_HasManagedStateReset()
    {
        var tabView = new TabBar { TabWidthMode = TabBarWidthMode.Compact };
        var a = new TabBarItem { Header = "A" };
        var b = new TabBarItem { Header = "B" };
        ((IList)tabView.Items).Add(a);
        ((IList)tabView.Items).Add(b);
        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();
        tabView.SelectedIndex = 0;
        Pump(window);
        Assert.True(b.HasPseudoClass(":compact"));
        Assert.Equal(36, b.Width);

        // 注意用 ItemCollection 自己的 Remove：ItemsSourceView 显式实现的
        // IList.Remove 会抛 NotSupportedException。
        tabView.Items.Remove(b);
        Pump(window);

        Assert.True(double.IsNaN(b.Width));        Assert.False(b.HasPseudoClass(":compact"));
        Assert.False(b.HasPseudoClass(":separator"));
        window.Close();
    }

    [AvaloniaFact]
    public void RemovedContainer_KeepsUserContextMenu()
    {
        var tabView = new TabBar { IsTabContextMenuEnabled = false };
        var userMenu = new ContextMenu();
        var a = new TabBarItem { Header = "A", ContextMenu = userMenu };
        ((IList)tabView.Items).Add(a);
        ((IList)tabView.Items).Add(new TabBarItem { Header = "B" });
        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();
        tabView.SelectedIndex = 0;
        Pump(window);

        tabView.Items.Remove(a);
        Pump(window);

        // 清理容器时不应连使用方自己设置的 ContextMenu 一起清掉。
        Assert.Same(userMenu, a.ContextMenu);
        window.Close();
    }

    [AvaloniaFact]
    public void RemovedContainer_HasManagedStateReset_ItemsSourceMode()
    {
        var items = new ObservableCollection<Doc> { new() { Title = "a" }, new() { Title = "b" } };
        var tabView = new TabBar { TabWidthMode = TabBarWidthMode.Compact, HeaderMemberPath = nameof(Doc.Title), ItemsSource = items };
        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();
        tabView.SelectedIndex = 0;
        Pump(window);
        var container = (TabBarItem)tabView.ContainerFromIndex(1)!;
        Assert.Equal(36, container.Width);
        Assert.Equal("b", container.Header);

        items.RemoveAt(1);
        Pump(window);

        Assert.True(double.IsNaN(container.Width));
        Assert.False(container.HasPseudoClass(":compact"));
        window.Close();
    }

    [AvaloniaFact]
    public void UserContextMenu_NotClobbered_WhenFeatureDisabled()
    {
        var tabView = new TabBar { IsTabContextMenuEnabled = false };
        var userMenu = new ContextMenu();
        var a = new TabBarItem { Header = "A", ContextMenu = userMenu };
        ((IList)tabView.Items).Add(a);
        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();
        tabView.SelectedIndex = 0;
        Pump(window);

        // 旧实现在 PrepareContainerForItemOverride 里无条件写
        // tvi.ContextMenu = ...，把使用方在 XAML 上设的菜单直接抹掉。
        Assert.Same(userMenu, a.ContextMenu);

        // 打开内置菜单会覆盖；再关掉时只清除本控件挂上去的那个。
        tabView.IsTabContextMenuEnabled = true;
        Pump(window);
        Assert.NotSame(userMenu, a.ContextMenu);
        Assert.NotNull(a.ContextMenu);

        tabView.IsTabContextMenuEnabled = false;
        Pump(window);
        Assert.Null(a.ContextMenu);

        window.Close();
    }

    [AvaloniaFact]
    public void ItemsClear_ReleasesDirectContainers()
    {
        // Reset（Items.Clear()）不带 OldItems，需要单独处理。
        var tabView = new TabBar { TabWidthMode = TabBarWidthMode.Compact };
        var a = new TabBarItem { Header = "A" };
        var b = new TabBarItem { Header = "B" };
        ((IList)tabView.Items).Add(a);
        ((IList)tabView.Items).Add(b);
        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();
        tabView.SelectedIndex = 0;
        Pump(window);
        Assert.Equal(36, b.Width);

        tabView.Items.Clear();
        Pump(window);

        Assert.True(double.IsNaN(a.Width));
        Assert.True(double.IsNaN(b.Width));
        Assert.False(b.HasPseudoClass(":compact"));
        window.Close();
    }

    // ---------------------------------------------------------- 紧凑首字回退

    [AvaloniaFact]
    public void CompactFallbackText_HandlesSurrogatePairs()
    {
        var tabView = new TabBar { TabWidthMode = TabBarWidthMode.Compact };
        ((IList)tabView.Items).Add(new TabBarItem { Header = "A" });
        ((IList)tabView.Items).Add(new TabBarItem { Header = "📋 报表" });
        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();
        tabView.SelectedIndex = 0;
        Pump(window);

        var compact = (TabBarItem)tabView.ContainerFromIndex(1)!;
        var icon = Part<ContentPresenter>(compact, "PART_IconPresenter");
        var text = (icon.Content as TextBlock)?.Text;
        // 按 UTF-16 码元切分会把 emoji 的代理对截成半个字符。
        Assert.Equal("📋", text);
        window.Close();
    }

    // ------------------------------------------------------------ 命令状态

    [AvaloniaFact]
    public void CloseAllTabs_ClosesEverything_AndUpdatesCanExecute()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(5);
        tabView.IsTabContextMenuEnabled = true;
        tabView.SelectedIndex = 2;
        Pump(window);
        Assert.True(tabView.CloseAllTabsCommand.CanExecute(null));

        tabView.CloseAllTabsCommand.Execute(null);
        Pump(window);

        Assert.Equal(0, tabView.ItemCount);
        Assert.False(tabView.CloseAllTabsCommand.CanExecute(null));
        window.Close();
    }

    [AvaloniaFact]
    public void CloseOtherTabs_KeepsSelectedTab()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(5);
        tabView.IsTabContextMenuEnabled = true;
        tabView.SelectedIndex = 2;
        Pump(window);
        var kept = tabView.SelectedItem;

        tabView.CloseOtherTabsCommand.Execute(null);
        Pump(window);

        Assert.Equal(1, tabView.ItemCount);
        Assert.Same(kept, tabView.SelectedItem);
        Assert.False(tabView.CloseOtherTabsCommand.CanExecute(null));
        window.Close();
    }

    [AvaloniaFact]
    public void PinnedTab_SurvivesCloseAll()
    {
        var tabView = new TabBar { IsTabContextMenuEnabled = true };
        var pinned = new TabBarItem { Header = "Pinned", IsClosable = false };
        ((IList)tabView.Items).Add(pinned);
        for (int i = 0; i < 3; i++) ((IList)tabView.Items).Add(new TabBarItem { Header = $"T{i}" });
        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();
        tabView.SelectedIndex = 0;
        Pump(window);

        tabView.CloseAllTabsCommand.Execute(null);
        Pump(window);

        Assert.Equal(1, tabView.ItemCount);
        Assert.Same(pinned, tabView.ContainerFromIndex(0));
        window.Close();
    }

    // ------------------------------------------------------------- 测试数据

    public class Inner
    {
        public string Name { get; set; } = "";
    }

    public class Doc : INotifyPropertyChanged
    {
        private string _title = "";
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Alternate)));
            }
        }

        public string Alternate => "ALT-" + _title;
        public Inner? Inner { get; set; }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
