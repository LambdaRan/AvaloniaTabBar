using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
using System.Collections;
using System.Globalization;

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

        // 按下已触发选中;Compact 模式下选中会改变全部标签宽度,
        // 必须先跑完布局再缓存 bounds,否则拿到的是选中前的旧几何。
        _tabBar.UpdateLayout();
        CacheBounds();

        _startContentX = _watchPressPos.X + (ScrollViewer?.Offset.X ?? 0);
        _lastPointerPos = e.GetPosition(_tabBar);
        _targetIndex = _dragIndex;

        watched.SetDragging(true);
        UpdateDrag();
    }

    private void CacheBounds()
    {
        int count = _tabBar.ItemCount;
        _lefts = new double[count];
        _widths = new double[count];
        // 统一折算到内容空间(TabBar 坐标 + 缓存时滚动偏移),
        // 后续滚动变化只影响指针侧换算,缓存本身保持有效。
        double offset = ScrollViewer?.Offset.X ?? 0;
        for (int i = 0; i < count; i++)
        {
            if (_tabBar.ContainerFromIndex(i) is not { } c) continue;
            var p = c.TranslatePoint(new Point(0, 0), _tabBar);
            _lefts[i] = (p?.X ?? 0) + offset;
            _widths[i] = c.Bounds.Width;
        }
    }

    private static TransformOperations Translate(double x) =>
        TransformOperations.Parse("translateX(" + x.ToString(CultureInfo.InvariantCulture) + "px)");

    private void UpdateDrag()
    {
        if (_dragged == null || _lefts.Length == 0) return;

        // === 被拖项跟随指针(仅 X 轴) ===
        double pointerContentX = _lastPointerPos.X + (ScrollViewer?.Offset.X ?? 0);
        double dx = pointerContentX - _startContentX;
        _dragged.RenderTransform = Translate(dx);

        // === 目标索引 = 非拖项中点在被拖项当前中心左侧的个数 ===
        // (Xaml.Behaviors ItemDragBehavior 同款"过半判定")
        double draggedCenter = _lefts[_dragIndex] + _widths[_dragIndex] / 2 + dx;
        int target = 0;
        for (int i = 0; i < _lefts.Length; i++)
        {
            if (i == _dragIndex) continue;
            if (_lefts[i] + _widths[i] / 2 < draggedCenter) target++;
        }
        _targetIndex = Math.Clamp(target, 0, _lefts.Length - 1);

        // === 邻居让位:(dragIndex, target] 左移、[target, dragIndex) 右移,
        // 位移量 = 被拖项宽度。非过渡项归位用 Identity 而非 null,
        // 保证 TransformOperationsTransition 能插值回位。 ===
        for (int i = 0; i < _lefts.Length; i++)
        {
            if (i == _dragIndex) continue;
            if (_tabBar.ContainerFromIndex(i) is not TabBarItem tvi) continue;

            double shift = 0;
            if (_dragIndex < _targetIndex && i > _dragIndex && i <= _targetIndex)
                shift = -_widths[_dragIndex];
            else if (_targetIndex < _dragIndex && i >= _targetIndex && i < _dragIndex)
                shift = _widths[_dragIndex];

            tvi.RenderTransform = shift == 0 ? TransformOperations.Identity : Translate(shift);
        }

        UpdateAutoScroll();
    }

    private void Commit()
    {
        var dragged = _dragged;
        if (dragged == null) return;
        int newIndex = _targetIndex;

        // 先复位视觉再改集合:直接子项模式的 RemoveAt+Insert 会触发
        // ReleaseContainer+重 Prepare,残留的 RenderTransform 会脏化新位置。
        ClearVisuals();
        ResetState();

        // 拖动期间应用可能移除了被拖项(容器已不在 Items 中)→ 放弃提交。
        int oldIndex = _tabBar.IndexFromContainer(dragged);
        if (oldIndex < 0) return;

        // Item 必须在变更前解析:RemoveAt 之后容器→数据项的映射即失效。
        var item = _tabBar.ResolveItem(dragged);
        bool wasSelected = ReferenceEquals(_tabBar.SelectedItem, item);

        if (oldIndex == newIndex) return;   // 未移动:不发事件

        // 双模式统一 RemoveAt+Insert:Avalonia 12 的 ItemCollection 没有 Move,
        // IList 接口同样没有(ObservableCollection.Move 是类自有成员)。
        int insertAt;
        if (_tabBar.ItemsSource is IList source)
        {
            source.RemoveAt(oldIndex);
            // 钳位:拖动中应用可能移除了「非被拖项」使列表收缩,陈旧的 newIndex
            // 会越界。Math.Min 给到 append 语义,Insert 永不抛(集合变更守卫只覆盖被拖项)。
            insertAt = Math.Min(newIndex, source.Count);
            source.Insert(insertAt, item);
        }
        else
        {
            _tabBar.Items.RemoveAt(oldIndex);
            insertAt = Math.Min(newIndex, _tabBar.Items.Count);
            _tabBar.Items.Insert(insertAt, item);
        }

        // Remove 会清掉选中(按对象跟踪的 SelectedItem 被移除),Insert 不会恢复,
        // 必须显式还原——按下时已选中被拖项,拖动后选中必须跟着走。
        if (wasSelected) _tabBar.SelectedItem = item;

        // 事件在集合变更与选中恢复之后发出:应用收到时 UI 已是最终状态,
        // 可直接做顺序持久化。
        var args = new TabBarReorderCompletedEventArgs(item, dragged, oldIndex, insertAt)
        {
            RoutedEvent = TabBar.TabReorderCompletedEvent
        };
        _tabBar.RaiseEvent(args);
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

    /// <summary>
    /// 指针进入 ScrollViewer 视口左右 24px 边缘区且有横向溢出时,
    /// 启动 50ms 定时滚动;离开边缘区或溢出消失即停。
    /// </summary>
    private void UpdateAutoScroll()
    {
        var sv = ScrollViewer;
        if (sv == null || _dragged == null)
        {
            StopAutoScroll();
            return;
        }

        double maxOffset = sv.Extent.Width - sv.Viewport.Width;
        if (maxOffset <= 0)
        {
            StopAutoScroll();
            return;
        }

        var origin = sv.TranslatePoint(new Point(0, 0), _tabBar);
        if (origin == null)
        {
            StopAutoScroll();
            return;
        }

        double x = _lastPointerPos.X;
        double left = origin.Value.X;
        double right = origin.Value.X + sv.Bounds.Width;
        _autoScrollDirection = x < left + EdgeZone ? -1 : x > right - EdgeZone ? 1 : 0;

        if (_autoScrollDirection == 0) StopAutoScroll();
        else StartAutoScroll();
    }

    private void StartAutoScroll()
    {
        if (_autoScrollTimer != null) return;
        _autoScrollTimer = new DispatcherTimer { Interval = AutoScrollInterval };
        _autoScrollTimer.Tick += (_, _) => PerformAutoScrollTick();
        _autoScrollTimer.Start();
    }

    private void StopAutoScroll()
    {
        _autoScrollDirection = 0;
        if (_autoScrollTimer == null) return;
        _autoScrollTimer.Stop();
        _autoScrollTimer = null;
    }

    /// <summary>
    /// 滚动一个步长并重算拖动几何——Offset 变化使指针的内容空间坐标改变,
    /// 被拖项跟随位置与让位目标索引都必须跟着更新。测试可直接调用。
    /// </summary>
    internal void PerformAutoScrollTick()
    {
        var sv = ScrollViewer;
        if (sv == null || _dragged == null || _autoScrollDirection == 0) return;

        double maxOffset = sv.Extent.Width - sv.Viewport.Width;
        if (maxOffset <= 0) return;

        double newOffset = Math.Clamp(sv.Offset.X + _autoScrollDirection * AutoScrollStep, 0, maxOffset);
        if (newOffset == sv.Offset.X) return;

        sv.Offset = new Vector(newOffset, sv.Offset.Y);
        UpdateDrag();
    }
}
