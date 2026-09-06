using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using SimTabBar.Controls;
using Xunit;

namespace SimTabBar.Tests;

public class TabViewReorderTests
{
    [AvaloniaFact]
    public void CanReorderTabs_DefaultFalse()
    {
        Assert.False(new TabBar().CanReorderTabs);
    }

    [AvaloniaFact]
    public void DragStartingEventArgs_ExposesItemTabCancel()
    {
        var tab = new TabBarItem();
        var args = new TabBarDragStartingEventArgs("item", tab);
        Assert.Equal("item", args.Item);
        Assert.Same(tab, args.Tab);
        Assert.False(args.Cancel);
        args.Cancel = true;
        Assert.True(args.Cancel);
    }

    [AvaloniaFact]
    public void ReorderCompletedEventArgs_ExposesIndices()
    {
        var tab = new TabBarItem();
        var args = new TabBarReorderCompletedEventArgs("item", tab, 2, 0);
        Assert.Equal("item", args.Item);
        Assert.Same(tab, args.Tab);
        Assert.Equal(2, args.OldIndex);
        Assert.Equal(0, args.NewIndex);
    }

    private sealed class Doc
    {
        public string Title { get; set; } = "";
    }

    private static (TabBar tabView, Window window, ObservableCollection<Doc> docs) CreateItemsSourceTabBar(int count = 4)
    {
        var docs = new ObservableCollection<Doc>();
        for (int i = 0; i < count; i++) docs.Add(new Doc { Title = $"Doc {i + 1}" });

        var tabView = new TabBar
        {
            ItemsSource = docs,
            HeaderMemberPath = "Title",
            CanReorderTabs = true
        };
        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();
        TestHelper.Pump(window);
        return (tabView, window, docs);
    }

    private static Point CenterOf(Control c, Window window)
    {
        var p = c.TranslatePoint(new Point(c.Bounds.Width / 2, c.Bounds.Height / 2), window);
        Assert.NotNull(p);
        return p!.Value;
    }

    /// <summary>按下 tab 中心 → 先移动 10px 越过阈值 → 再移动到目标 → 释放。</summary>
    private static void DragBy(Window window, TabBarItem tab, double deltaX)
    {
        var start = CenterOf(tab, window);
        window.MouseDown(start, MouseButton.Left);
        window.MouseMove(start.WithX(start.X + System.Math.Sign(deltaX) * 10), RawInputModifiers.LeftMouseButton);
        var end = start.WithX(start.X + deltaX);
        window.MouseMove(end, RawInputModifiers.LeftMouseButton);
        window.MouseUp(end, MouseButton.Left);
    }

    /// <summary>
    /// 逐帧 Pump 直到 tab 中心越过阈值(让位过渡为 120ms RenderTransform 动画,
    /// headless 下按真实时间推进)。超时由后续断言兜底,不在此处判定。
    /// </summary>
    private static void PumpUntil(Window window, TabBarItem tab, double centerBelow)
    {
        for (int i = 0; i < 200; i++)
        {
            TestHelper.Pump(window);
            if (CenterOf(tab, window).X < centerBelow) return;
        }
    }

    [AvaloniaFact]
    public void Reorder_Disabled_DragDoesNotStart()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(3);
        TestHelper.Pump(window);
        // 默认 CanReorderTabs=false
        int starting = 0;
        tabView.TabDragStarting += (_, _) => starting++;

        var tab0 = (TabBarItem)tabView.ContainerFromIndex(0)!;
        DragBy(window, tab0, +200);

        Assert.Equal(0, starting);
        window.Close();
    }

    [AvaloniaFact]
    public void Reorder_ItemsSourceMode_StartingFires_WithDataItem()
    {
        var (tabView, window, docs) = CreateItemsSourceTabBar();
        var doc1 = docs[0];
        object? startedItem = null;
        tabView.TabDragStarting += (_, e) => startedItem = e.Item;

        var tab0 = (TabBarItem)tabView.ContainerFromIndex(0)!;
        DragBy(window, tab0, +200);

        Assert.Same(doc1, startedItem);
        window.Close();
    }

    [AvaloniaFact]
    public void Reorder_DirectChildrenMode_StartingFires_WithContainer()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(3);
        tabView.CanReorderTabs = true;
        TestHelper.Pump(window);
        object? startedItem = null;
        tabView.TabDragStarting += (_, e) => startedItem = e.Item;

        var tab0 = (TabBarItem)tabView.ContainerFromIndex(0)!;
        DragBy(window, tab0, +200);

        Assert.Same(tab0, startedItem);
        window.Close();
    }

    [AvaloniaFact]
    public void Reorder_StartingCancelled_NoDragState_NoReorder()
    {
        var (tabView, window, docs) = CreateItemsSourceTabBar();
        tabView.TabDragStarting += (_, e) => e.Cancel = true;
        int completed = 0;
        tabView.TabReorderCompleted += (_, _) => completed++;

        var tab0 = (TabBarItem)tabView.ContainerFromIndex(0)!;
        DragBy(window, tab0, +200);

        Assert.Equal(0, completed);
        Assert.False(tab0.HasPseudoClass(TabBarItem.PcDragging));
        Assert.Equal(new[] { "Doc 1", "Doc 2", "Doc 3", "Doc 4" }, docs.Select(d => d.Title));
        window.Close();
    }

    [AvaloniaFact]
    public void Reorder_DuringDrag_DraggingPseudoClass_SetThenCleared()
    {
        var (tabView, window, _) = CreateItemsSourceTabBar();
        var tab0 = (TabBarItem)tabView.ContainerFromIndex(0)!;
        var start = CenterOf(tab0, window);

        window.MouseDown(start, MouseButton.Left);
        window.MouseMove(start.WithX(start.X + 10), RawInputModifiers.LeftMouseButton);
        Assert.True(tab0.HasPseudoClass(TabBarItem.PcDragging));
        Assert.True(tabView.ReorderController.IsDragging);

        var at = start.WithX(start.X + 150);
        window.MouseMove(at, RawInputModifiers.LeftMouseButton);
        window.MouseUp(at, MouseButton.Left);
        Assert.False(tab0.HasPseudoClass(TabBarItem.PcDragging));
        Assert.False(tabView.ReorderController.IsDragging);
        window.Close();
    }

    [AvaloniaFact]
    public void Reorder_BelowThreshold_TreatedAsClick()
    {
        var (tabView, window, _) = CreateItemsSourceTabBar();
        int starting = 0;
        tabView.TabDragStarting += (_, _) => starting++;

        var tab1 = (TabBarItem)tabView.ContainerFromIndex(1)!;
        var c = CenterOf(tab1, window);
        window.MouseDown(c, MouseButton.Left);
        window.MouseMove(c.WithX(c.X + 2), RawInputModifiers.LeftMouseButton);
        window.MouseUp(c.WithX(c.X + 2), MouseButton.Left);

        Assert.Equal(0, starting);
        Assert.Equal(1, tabView.SelectedIndex);
        window.Close();
    }

    [AvaloniaFact]
    public void Reorder_PressOnCloseButton_DoesNotStart()
    {
        var (tabView, window, _) = CreateItemsSourceTabBar();
        int starting = 0;
        tabView.TabDragStarting += (_, _) => starting++;

        var tab0 = (TabBarItem)tabView.ContainerFromIndex(0)!;
        var closeBtn = TestHelper.Part<Button>(tab0, "PART_CloseButton");
        var c = CenterOf(closeBtn, window);
        window.MouseDown(c, MouseButton.Left);
        window.MouseMove(c.WithX(c.X + 100), RawInputModifiers.LeftMouseButton);
        window.MouseUp(c.WithX(c.X + 100), MouseButton.Left);

        Assert.Equal(0, starting);
        Assert.Equal(4, tabView.ItemCount);
        window.Close();
    }

    [AvaloniaFact]
    public void Reorder_FixedSizeItemsSource_DragRejected()
    {
        var source = new[] { "a", "b", "c" };   // 数组:IList 但 IsFixedSize=true
        var tabView = new TabBar { ItemsSource = source, CanReorderTabs = true };
        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();
        TestHelper.Pump(window);

        int starting = 0;
        tabView.TabDragStarting += (_, _) => starting++;
        var tab0 = (TabBarItem)tabView.ContainerFromIndex(0)!;
        DragBy(window, tab0, +200);

        Assert.Equal(0, starting);
        Assert.Equal(new[] { "a", "b", "c" }, source);
        window.Close();
    }

    [AvaloniaFact]
    public void Reorder_DuringDrag_DraggedFollowsPointer_NeighborsYield()
    {
        var (tabView, window, _) = CreateItemsSourceTabBar();
        var tab0 = (TabBarItem)tabView.ContainerFromIndex(0)!;
        var tab1 = (TabBarItem)tabView.ContainerFromIndex(1)!;
        var start = CenterOf(tab0, window);
        double center1Before = CenterOf(tab1, window).X;

        window.MouseDown(start, MouseButton.Left);
        var at = start.WithX(start.X + 150);
        window.MouseMove(at, RawInputModifiers.LeftMouseButton);

        // 被拖项视觉中心跟随指针(+150,容差 10)
        Assert.InRange(CenterOf(tab0, window).X - start.X, 140, 160);

        // 中点判定:+150 后中心约 248.5,未过右邻中点(实测约 287.5,标签宽 189)
        // → 目标索引仍为 0,右邻让位尚未触发(位移 0)。
        Assert.Equal(0, tabView.ReorderController.TargetIndex);
        Assert.InRange(CenterOf(tab1, window).X, center1Before - 1, center1Before + 1);

        // 拖满一个标签宽度(+230):中心约 328.5 越过右邻中点 → target=1
        window.MouseMove(start.WithX(start.X + 230), RawInputModifiers.LeftMouseButton);

        // 右邻让位:向左平移约一个被拖项宽度(实测标签宽 189)。
        // 让位经 120ms RenderTransform 过渡动画,逐帧 Pump 到过渡结束再断言。
        PumpUntil(window, tab1, center1Before - 100);
        Assert.True(CenterOf(tab1, window).X < center1Before - 100,
            $"neighbor should yield left, before={center1Before} now={CenterOf(tab1, window).X}");

        // 目标索引:中心 98.5+230=328.5,已过 B 的中点(约 287.5)→ target=1
        Assert.Equal(1, tabView.ReorderController.TargetIndex);

        // 本测试不释放指针,避免依赖后续任务才实现的提交行为
        window.Close();
    }

    [AvaloniaFact]
    public void Reorder_DuringDrag_TargetIndex_TracksPointer()
    {
        var (tabView, window, _) = CreateItemsSourceTabBar();
        var tab0 = (TabBarItem)tabView.ContainerFromIndex(0)!;
        var tab2 = (TabBarItem)tabView.ContainerFromIndex(2)!;
        var start = CenterOf(tab0, window);

        window.MouseDown(start, MouseButton.Left);
        window.MouseMove(start.WithX(start.X + 10), RawInputModifiers.LeftMouseButton);
        Assert.Equal(0, tabView.ReorderController.TargetIndex);

        // 拖过第 3 个标签中心 → target=2
        var over = CenterOf(tab2, window);
        window.MouseMove(over.WithX(over.X + 10), RawInputModifiers.LeftMouseButton);
        Assert.Equal(2, tabView.ReorderController.TargetIndex);

        // 拖回起点 → target=0
        window.MouseMove(start, RawInputModifiers.LeftMouseButton);
        Assert.Equal(0, tabView.ReorderController.TargetIndex);

        window.Close();
    }

    [AvaloniaFact]
    public void Reorder_ItemsSource_DragRightTwo_CommitsAndRaisesCompleted()
    {
        var (tabView, window, docs) = CreateItemsSourceTabBar();
        var doc1 = docs[0];
        TabBarReorderCompletedEventArgs? completed = null;
        tabView.TabReorderCompleted += (_, e) => completed = e;

        var tab0 = (TabBarItem)tabView.ContainerFromIndex(0)!;
        var tab2 = (TabBarItem)tabView.ContainerFromIndex(2)!;
        double delta = CenterOf(tab2, window).X - CenterOf(tab0, window).X + 10;
        DragBy(window, tab0, delta);

        Assert.Equal(new[] { "Doc 2", "Doc 3", "Doc 1", "Doc 4" }, docs.Select(d => d.Title));
        Assert.NotNull(completed);
        Assert.Equal(0, completed!.OldIndex);
        Assert.Equal(2, completed.NewIndex);
        Assert.Same(doc1, completed.Item);
        window.Close();
    }

    [AvaloniaFact]
    public void Reorder_Selection_FollowsMovedTab()
    {
        var (tabView, window, docs) = CreateItemsSourceTabBar();
        var doc1 = docs[0];
        var tab0 = (TabBarItem)tabView.ContainerFromIndex(0)!;
        var tab2 = (TabBarItem)tabView.ContainerFromIndex(2)!;
        DragBy(window, tab0, CenterOf(tab2, window).X - CenterOf(tab0, window).X + 10);

        Assert.Same(doc1, tabView.SelectedItem);
        Assert.Equal(2, tabView.SelectedIndex);
        window.Close();
    }

    [AvaloniaFact]
    public void Reorder_DirectChildren_CommitsAndKeepsContainerState()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(3);
        tabView.CanReorderTabs = true;
        TestHelper.Pump(window);

        var tab2 = (TabBarItem)tabView.ContainerFromIndex(2)!;
        var tab1 = (TabBarItem)tabView.ContainerFromIndex(1)!;
        double delta = CenterOf(tab1, window).X - CenterOf(tab2, window).X - 10;
        DragBy(window, tab2, delta);

        // A B C → C 拖到 B 前 → A C B
        Assert.Same(tab2, tabView.Items[1]);
        Assert.Same(tab1, tabView.Items[2]);
        // 容器经历 RemoveAt+Insert 的 Release+重 Prepare 后净状态完好
        Assert.Equal("Tab 3", tab2.Header);
        Assert.False(tab2.HasPseudoClass(TabBarItem.PcDragging));
        Assert.Equal(1, tabView.SelectedIndex);
        window.Close();
    }

    [AvaloniaFact]
    public void Reorder_DropAtSameIndex_NoCompletedEvent()
    {
        var (tabView, window, docs) = CreateItemsSourceTabBar();
        int completed = 0;
        tabView.TabReorderCompleted += (_, _) => completed++;

        var tab0 = (TabBarItem)tabView.ContainerFromIndex(0)!;
        DragBy(window, tab0, +20);   // 未越过 B 的中点(标签宽约 185)

        Assert.Equal(0, completed);
        Assert.Equal(new[] { "Doc 1", "Doc 2", "Doc 3", "Doc 4" }, docs.Select(d => d.Title));
        window.Close();
    }

    [AvaloniaFact]
    public void Reorder_DragToLast_LandsAtEnd()
    {
        var (tabView, window, docs) = CreateItemsSourceTabBar();
        var tab0 = (TabBarItem)tabView.ContainerFromIndex(0)!;
        var tab3 = (TabBarItem)tabView.ContainerFromIndex(3)!;
        DragBy(window, tab0, CenterOf(tab3, window).X - CenterOf(tab0, window).X + 50);

        Assert.Equal(new[] { "Doc 2", "Doc 3", "Doc 4", "Doc 1" }, docs.Select(d => d.Title));
        window.Close();
    }

    [AvaloniaFact]
    public void Reorder_DragToFirst_LandsAtStart()
    {
        var (tabView, window, docs) = CreateItemsSourceTabBar();
        var tab2 = (TabBarItem)tabView.ContainerFromIndex(2)!;
        var tab0 = (TabBarItem)tabView.ContainerFromIndex(0)!;
        DragBy(window, tab2, CenterOf(tab0, window).X - CenterOf(tab2, window).X - 10);

        // C 拖到 A 中点左侧 → target=0 → C A B D
        Assert.Equal(new[] { "Doc 3", "Doc 1", "Doc 2", "Doc 4" }, docs.Select(d => d.Title));
        window.Close();
    }

    [AvaloniaFact]
    public void Reorder_PinnedTab_IsDraggable()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(3);
        tabView.CanReorderTabs = true;
        var pinned = (TabBarItem)tabView.Items[0]!;
        pinned.IsClosable = false;
        TestHelper.Pump(window);

        var tab2 = (TabBarItem)tabView.ContainerFromIndex(2)!;
        DragBy(window, pinned, CenterOf(tab2, window).X - CenterOf(pinned, window).X + 10);

        // 固定页一视同仁:A B C → B C A
        Assert.Same(pinned, tabView.Items[2]);
        window.Close();
    }

    [AvaloniaFact]
    public void Reorder_Escape_CancelsWithoutCommit()
    {
        var (tabView, window, docs) = CreateItemsSourceTabBar();
        var tab0 = (TabBarItem)tabView.ContainerFromIndex(0)!;
        var start = CenterOf(tab0, window);

        window.MouseDown(start, MouseButton.Left);
        var at = start.WithX(start.X + 200);
        window.MouseMove(at, RawInputModifiers.LeftMouseButton);
        Assert.True(tabView.ReorderController.IsDragging);

        // 按下时焦点已转移到 TabBar(现有行为),KeyPress 直达 OnKeyDown
        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, "");

        Assert.False(tabView.ReorderController.IsDragging);
        Assert.False(tab0.HasPseudoClass(TabBarItem.PcDragging));

        window.MouseUp(at, MouseButton.Left);
        Assert.Equal(new[] { "Doc 1", "Doc 2", "Doc 3", "Doc 4" }, docs.Select(d => d.Title));
        window.Close();
    }

    [AvaloniaFact]
    public void Reorder_DraggedItemRemovedDuringDrag_CommitAbandoned()
    {
        var (tabView, window, docs) = CreateItemsSourceTabBar();
        var doc1 = docs[0];
        int completed = 0;
        tabView.TabReorderCompleted += (_, _) => completed++;

        var tab0 = (TabBarItem)tabView.ContainerFromIndex(0)!;
        var start = CenterOf(tab0, window);
        window.MouseDown(start, MouseButton.Left);
        var at = start.WithX(start.X + 200);
        window.MouseMove(at, RawInputModifiers.LeftMouseButton);

        docs.Remove(doc1);          // 拖动中应用移除被拖项
        TestHelper.Pump(window);

        window.MouseUp(at, MouseButton.Left);   // 不得崩溃

        Assert.Equal(0, completed);
        Assert.Equal(new[] { "Doc 2", "Doc 3", "Doc 4" }, docs.Select(d => d.Title));
        window.Close();
    }

    [AvaloniaFact]
    public void Reorder_EdgeAutoScroll_PendsAndTicks()
    {
        // 12 个标签 × MinWidth 100 = 1200 > 视口约 760 → 横向溢出
        var (tabView, window, _) = CreateItemsSourceTabBar(12);
        var sv = tabView.TabStripScrollViewer!;
        Assert.True(sv.Extent.Width > sv.Viewport.Width);

        var tab0 = (TabBarItem)tabView.ContainerFromIndex(0)!;
        var start = CenterOf(tab0, window);
        window.MouseDown(start, MouseButton.Left);
        window.MouseMove(start.WithX(start.X + 10), RawInputModifiers.LeftMouseButton);
        Assert.False(tabView.ReorderController.IsAutoScrollPending);

        // 拖到 ScrollViewer 右缘 5px 内(边缘区 24px)
        var svOrigin = sv.TranslatePoint(new Point(0, 0), window)!.Value;
        var edge = new Point(svOrigin.X + sv.Bounds.Width - 5, start.Y);
        window.MouseMove(edge, RawInputModifiers.LeftMouseButton);
        Assert.True(tabView.ReorderController.IsAutoScrollPending);

        double offsetBefore = sv.Offset.X;
        tabView.ReorderController.PerformAutoScrollTick();
        Assert.True(sv.Offset.X > offsetBefore);

        window.MouseUp(edge, MouseButton.Left);
        window.Close();
    }
}
