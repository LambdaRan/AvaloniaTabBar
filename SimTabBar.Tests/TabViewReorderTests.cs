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
}
