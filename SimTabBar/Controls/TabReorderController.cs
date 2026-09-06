using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
using System.Collections;

namespace SimTabBar.Controls;

/// <summary>
/// 标签拖动排序引擎(纯指针方案,不进 OS 拖放循环)。
/// 状态机:Idle → Watching(左键按下)→ Dragging(水平位移超过阈值,
/// capture 到 TabBar)→ Idle(释放提交 / Escape / 捕获丢失取消)。
/// 拖动中只改 RenderTransform,不动数据、不动布局——与 UpdateAllTabVisuals
/// 的宽度写入链路完全隔离。释放时按"被拖项中心 vs 邻居缓存中点"计算
/// 目标索引,双模式统一 RemoveAt+Insert 提交(Avalonia 12 的 ItemCollection
/// 没有 Move,IList 接口同样没有)。
/// </summary>
internal sealed class TabReorderController
{
    private const double DragThreshold = 4;      // 触发拖动的水平位移像素
    private const double EdgeZone = 24;          // 边缘自动滚动触发区
    private const double AutoScrollStep = 8;     // 每 tick 滚动像素
    private static readonly TimeSpan AutoScrollInterval = TimeSpan.FromMilliseconds(50);

    private readonly TabBar _tabBar;

    // --- Watching 状态 ---
    private TabBarItem? _watched;
    private Point _watchPressPos;                // TabBar 坐标系按下点

    // --- Dragging 状态 ---
    private TabBarItem? _dragged;
    private int _dragIndex;
    private int _targetIndex;
    private double _startContentX;               // 拖动起点的内容空间 X(含滚动偏移)
    private Point _lastPointerPos;               // 最近一次指针位置(TabBar 坐标系)
    private double[] _lefts = Array.Empty<double>();   // 各容器内容空间左缘
    private double[] _widths = Array.Empty<double>();

    // --- 边缘自动滚动 ---
    private DispatcherTimer? _autoScrollTimer;
    private double _autoScrollDirection;         // -1 / 0 / +1

    internal TabReorderController(TabBar tabBar) => _tabBar = tabBar;

    internal bool IsDragging => _dragged != null;
    internal int TargetIndex => _targetIndex;
    internal bool IsAutoScrollPending => _autoScrollDirection != 0;

    private ScrollViewer? ScrollViewer => _tabBar.TabStripScrollViewer;

    internal void BeginWatch(TabBarItem item, PointerPressedEventArgs e)
    {
        if (!_tabBar.CanReorderTabs) return;
        _watched = item;
        _watchPressPos = e.GetPosition(_tabBar);
    }

    internal void OnPointerMoved(PointerEventArgs e)
    {
        if (_dragged != null)
        {
            _lastPointerPos = e.GetPosition(_tabBar);
            UpdateDrag();
            return;
        }

        if (_watched == null) return;

        // 按键可能已抬起(如指针移出窗口后释放,事件收不到),清理陈旧观察。
        if (!e.GetCurrentPoint(_tabBar).Properties.IsLeftButtonPressed)
        {
            _watched = null;
            return;
        }

        var pos = e.GetPosition(_tabBar);
        if (Math.Abs(pos.X - _watchPressPos.X) < DragThreshold) return;

        TryStartDrag(e);
    }

    internal void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (_dragged != null)
        {
            Commit();
            e.Pointer.Capture(null);
            return;
        }
        _watched = null;
    }

    /// <summary>
    /// 取消拖动(Escape / 指针捕获丢失 / 拖动项被应用移除)。
    /// 只复位视觉与状态,不改集合、不发完成事件。
    /// </summary>
    internal void Cancel()
    {
        if (_watched == null && _dragged == null) return;
        ClearVisuals();
        ResetState();
    }

    private void TryStartDrag(PointerEventArgs e)
    {
        var watched = _watched;
        _watched = null;
        if (watched == null) return;

        // ItemsSource 模式要求源集合是可变 IList,否则拒绝启动(与关闭路径同款守卫)。
        if (_tabBar.ItemsSource != null &&
            _tabBar.ItemsSource is not IList { IsReadOnly: false, IsFixedSize: false })
        {
            System.Diagnostics.Debug.WriteLine(
                "[TabBar] Tab reorder skipped: ItemsSource is not a mutable IList.");
            return;
        }

        // Item 语义与关闭事件一致:ItemsSource 模式=数据项,直接子项模式=容器自身。
        var item = _tabBar.ResolveItem(watched);
        var args = new TabBarDragStartingEventArgs(item, watched)
        {
            RoutedEvent = TabBar.TabDragStartingEvent
        };
        _tabBar.RaiseEvent(args);
        if (args.Cancel) return;

        _dragged = watched;
        _dragIndex = _tabBar.IndexFromContainer(watched);
        if (_dragIndex < 0)
        {
            _dragged = null;
            return;
        }

        // 与滚动滑块拖动同款:capture 到 TabBar,后续 Moved/Released 由
        // TabBar.OnPointerMoved/OnPointerReleased 转发进来。
        e.Pointer.Capture(_tabBar);

        watched.SetDragging(true);
    }

    private void UpdateDrag()
    {
        // Task 3 实现几何与让位视觉。
    }

    private void Commit()
    {
        // Task 4 实现提交。当前阶段释放只复位。
        ClearVisuals();
        ResetState();
    }

    private void ClearVisuals()
    {
        for (int i = 0; i < _tabBar.ItemCount; i++)
        {
            if (_tabBar.ContainerFromIndex(i) is TabBarItem tvi)
            {
                tvi.RenderTransform = TransformOperations.Identity;
                tvi.SetDragging(false);
            }
        }

        // 被拖容器可能已被应用移出集合(遍历覆盖不到),单独清理——
        // ItemsSource 模式下容器会被回收复用,残留 :dragging 会脏化下一个数据项。
        if (_dragged != null)
        {
            _dragged.RenderTransform = TransformOperations.Identity;
            _dragged.SetDragging(false);
        }

        StopAutoScroll();
    }

    private void ResetState()
    {
        _dragged = null;
        _watched = null;
        _dragIndex = 0;
        _targetIndex = 0;
        _lefts = Array.Empty<double>();
        _widths = Array.Empty<double>();
        StopAutoScroll();
    }

    private void StopAutoScroll()
    {
        _autoScrollDirection = 0;
        if (_autoScrollTimer == null) return;
        _autoScrollTimer.Stop();
        _autoScrollTimer = null;
    }

    /// <summary>供 Task 6 的边缘自动滚动与测试调用。当前为空操作。</summary>
    internal void PerformAutoScrollTick()
    {
    }
}
