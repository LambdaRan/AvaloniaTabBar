using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Reactive;
using Avalonia.Threading;
using System.Collections;
using System.Collections.Specialized;
using System.Windows.Input;

namespace SimTabBar.Controls;

[TemplatePart("PART_TabContentPresenter", typeof(ContentPresenter))]
[TemplatePart("PART_TabStripGrid", typeof(Grid))]
[TemplatePart("PART_AddButton", typeof(Button))]
[TemplatePart("PART_TabStripScrollViewer", typeof(ScrollViewer))]
[TemplatePart("PART_HeaderPresenter", typeof(ContentPresenter))]
[TemplatePart("PART_FooterPresenter", typeof(ContentPresenter))]
[TemplatePart("PART_ScrollThumb", typeof(Border))]
public class TabBar : SelectingItemsControl
{
    private RelayCommand? _closeTabCommand;
    private RelayCommand? _closeOtherTabsCommand;
    private RelayCommand? _closeAllTabsCommand;

    public ICommand CloseTabCommand =>
        _closeTabCommand ??= new RelayCommand(OnCloseTab);

    public ICommand CloseOtherTabsCommand =>
        _closeOtherTabsCommand ??= new RelayCommand(OnCloseOtherTabs, CanCloseOtherTabs);

    public ICommand CloseAllTabsCommand =>
        _closeAllTabsCommand ??= new RelayCommand(OnCloseAllTabs, CanCloseAllTabs);

    /// <summary>
    /// 应用程序提供的回调，当标签页关闭时由库调用。
    /// 参数：正在关闭的标签页项。
    /// 返回值：下一个要选中的标签页项，或返回 null 以使用默认的相邻选择。
    /// 仅在被关闭的标签页原本处于选中状态时调用 —— 关闭后台标签页不会移动选中项。
    /// </summary>
    public Func<object?, object?>? NextTabOnClose { get; set; }

    /// <summary>
    /// 是否启用库内置的键盘导航（Ctrl+Tab / Ctrl+Shift+Tab / Ctrl+F4）。
    /// 默认为 true。设置为 false 可让应用程序独立处理键盘事件，避免事件冲突。
    /// </summary>
    public bool IsBuiltInKeyboardHandlingEnabled { get; set; } = true;

    /// <summary>
    /// 以编程方式选中指定的标签页项。
    /// </summary>
    public void SelectTab(object item)
    {
        if (Items.Contains(item) || ContainerFromItem(item) != null) {
            SelectedItem = item;
        }
    }

	#region 上下文菜单

	public static readonly StyledProperty<ContextMenu?> TabItemContextMenuProperty =
        AvaloniaProperty.Register<TabBar, ContextMenu?>(nameof(TabItemContextMenu));

    public static readonly StyledProperty<bool> IsTabContextMenuEnabledProperty =
        AvaloniaProperty.Register<TabBar, bool>(nameof(IsTabContextMenuEnabled), false);

    public ContextMenu? TabItemContextMenu
    {
        get => GetValue(TabItemContextMenuProperty);
        set => SetValue(TabItemContextMenuProperty, value);
    }

    public bool IsTabContextMenuEnabled
    {
        get => GetValue(IsTabContextMenuEnabledProperty);
        set => SetValue(IsTabContextMenuEnabledProperty, value);
    }

    private ContextMenu? _cachedContextMenu;

	#endregion

	#region 样式属性

	public static readonly StyledProperty<TabBarWidthMode> TabWidthModeProperty =
        AvaloniaProperty.Register<TabBar, TabBarWidthMode>(nameof(TabWidthMode), TabBarWidthMode.Equal);

    public static readonly StyledProperty<TabBarCloseButtonOverlayMode> CloseButtonOverlayModeProperty =
        AvaloniaProperty.Register<TabBar, TabBarCloseButtonOverlayMode>(
            nameof(CloseButtonOverlayMode), TabBarCloseButtonOverlayMode.Auto);

    public static readonly StyledProperty<bool> IsAddTabButtonVisibleProperty =
        AvaloniaProperty.Register<TabBar, bool>(nameof(IsAddTabButtonVisible), true);

    public static readonly StyledProperty<ICommand?> AddTabButtonCommandProperty =
        AvaloniaProperty.Register<TabBar, ICommand?>(nameof(AddTabButtonCommand));

    public static readonly StyledProperty<object?> AddTabButtonCommandParameterProperty =
        AvaloniaProperty.Register<TabBar, object?>(nameof(AddTabButtonCommandParameter));

    public static readonly StyledProperty<object?> TabStripHeaderProperty =
        AvaloniaProperty.Register<TabBar, object?>(nameof(TabStripHeader));

	public static readonly StyledProperty<IDataTemplate?> TabStripHeaderTemplateProperty =
	AvaloniaProperty.Register<TabBar, IDataTemplate?>(nameof(TabStripHeaderTemplate));

	public static readonly StyledProperty<object?> TabStripFooterProperty =
        AvaloniaProperty.Register<TabBar, object?>(nameof(TabStripFooter));

    public static readonly StyledProperty<IDataTemplate?> TabStripFooterTemplateProperty =
        AvaloniaProperty.Register<TabBar, IDataTemplate?>(nameof(TabStripFooterTemplate));

    public static readonly StyledProperty<IDataTemplate?> ContentTemplateProperty =
        ContentControl.ContentTemplateProperty.AddOwner<TabBar>();

    public static readonly StyledProperty<string?> HeaderMemberPathProperty =
        AvaloniaProperty.Register<TabBar, string?>(nameof(HeaderMemberPath));

    public static readonly StyledProperty<string?> IconSourceMemberPathProperty =
        AvaloniaProperty.Register<TabBar, string?>(nameof(IconSourceMemberPath));

	public TabBarWidthMode TabWidthMode {
		get => GetValue(TabWidthModeProperty);
		set => SetValue(TabWidthModeProperty, value);
	}

	public TabBarCloseButtonOverlayMode CloseButtonOverlayMode {
		get => GetValue(CloseButtonOverlayModeProperty);
		set => SetValue(CloseButtonOverlayModeProperty, value);
	}

	public bool IsAddTabButtonVisible {
		get => GetValue(IsAddTabButtonVisibleProperty);
		set => SetValue(IsAddTabButtonVisibleProperty, value);
	}

	public ICommand? AddTabButtonCommand {
		get => GetValue(AddTabButtonCommandProperty);
		set => SetValue(AddTabButtonCommandProperty, value);
	}

	public object? AddTabButtonCommandParameter {
		get => GetValue(AddTabButtonCommandParameterProperty);
		set => SetValue(AddTabButtonCommandParameterProperty, value);
	}

	public object? TabStripHeader {
		get => GetValue(TabStripHeaderProperty);
		set => SetValue(TabStripHeaderProperty, value);
	}

	public object? TabStripFooter {
		get => GetValue(TabStripFooterProperty);
		set => SetValue(TabStripFooterProperty, value);
	}

	public IDataTemplate? TabStripHeaderTemplate {
		get => GetValue(TabStripHeaderTemplateProperty);
		set => SetValue(TabStripHeaderTemplateProperty, value);
	}

	public IDataTemplate? TabStripFooterTemplate {
		get => GetValue(TabStripFooterTemplateProperty);
		set => SetValue(TabStripFooterTemplateProperty, value);
	}

	public IDataTemplate? ContentTemplate {
		get => GetValue(ContentTemplateProperty);
		set => SetValue(ContentTemplateProperty, value);
	}

	public string? HeaderMemberPath {
		get => GetValue(HeaderMemberPathProperty);
		set => SetValue(HeaderMemberPathProperty, value);
	}

	public string? IconSourceMemberPath {
		get => GetValue(IconSourceMemberPathProperty);
		set => SetValue(IconSourceMemberPathProperty, value);
	}

	#endregion

	public static readonly RoutedEvent<TabBarCloseRequestedEventArgs> TabCloseRequestedEvent =
        RoutedEvent.Register<TabBar, TabBarCloseRequestedEventArgs>(
            nameof(TabCloseRequested), RoutingStrategies.Bubble);

    public event EventHandler<TabBarCloseRequestedEventArgs> TabCloseRequested
    {
        add => AddHandler(TabCloseRequestedEvent, value);
        remove => RemoveHandler(TabCloseRequestedEvent, value);
    }

	public TabBar()
	{
		SelectionMode = SelectionMode.Single;
		SelectionChanged += (_, _) => {
			UpdateTabContent();
			// UpdateAllTabVisuals 末尾已经调用 UpdateCommandCanExecute
			UpdateAllTabVisuals();
		};
	}

	static TabBar()
	{
		// 内置键盘导航需要键盘焦点才能收到 OnKeyDown，因此 TabBar 必须可获焦。
		// 焦点框由 ControlTheme 中的 FocusAdorner="{x:Null}" 抑制。
		FocusableProperty.OverrideDefaultValue<TabBar>(true);

		// 只影响关闭按钮/分隔符的属性走轻量更新，不需要重新测量布局。
		CloseButtonOverlayModeProperty.Changed.AddClassHandler<TabBar>((x, _) => x.UpdateCloseButtonAndSeparatorState());
		// 宽度模式变化需要完整的一次重算。
		TabWidthModeProperty.Changed.AddClassHandler<TabBar>((x, _) => x.UpdateAllTabVisuals());
		// IsAddTabButtonVisible / TabStripHeader / TabStripFooter 会改变标签条的
		// 可用宽度，这由 ScrollViewer 的 Viewport 订阅统一处理 —— 那才是唯一在
		// 布局完成后拿到正确尺寸的时机，所以这里不再挂处理器。
		ItemsSourceProperty.Changed.AddClassHandler<TabBar>((x, _) => x.OnItemsReassigned());
		HeaderMemberPathProperty.Changed.AddClassHandler<TabBar>((x, _) => x.RefreshContainerMemberBindings());
		IconSourceMemberPathProperty.Changed.AddClassHandler<TabBar>((x, _) => x.RefreshContainerMemberBindings());
		ContentTemplateProperty.Changed.AddClassHandler<TabBar>((x, _) => x.RefreshContainerContentTemplates());
		TabItemContextMenuProperty.Changed.AddClassHandler<TabBar>((x, _) => {
			x._cachedContextMenu = null;
			x.UpdateAllContainerMenus();
		});
		IsTabContextMenuEnabledProperty.Changed.AddClassHandler<TabBar>((x, _) => {
			x.UpdateAllContainerMenus();
			x.UpdateCommandCanExecute();
		});
	}

	public event EventHandler? AddTabButtonClick;

    private const string ResourceKeyMinWidth = "SimTabBarItemMinWidth";
    private const string ResourceKeyMaxWidth = "SimTabBarItemMaxWidth";
    private const string ResourceKeyCompactWidth = "SimTabBarItemCompactWidth";

    private const double WheelScrollAmount = 50;
    private const double ThumbHoverOpacity = 0.8;

    private ContentPresenter? _tabContentPresenter;
    private Grid? _tabStripGrid;
    private Button? _addButton;
    private ScrollViewer? _tabStripScrollViewer;
    private Border? _scrollThumb;
    private bool _closeHandlerWired;
    private bool _isDraggingThumb;
    private double _dragStartX;
    private double _dragStartOffset;

    private double _cachedMinWidth = 100;
    private double _cachedMaxWidth = 240;
    private double _cachedCompactWidth = 36;

    private NotifyCollectionChangedEventHandler? _itemsCollectionChanged;

    private IDisposable? _contentSubscription;
    private IDisposable? _contentTemplateSubscription;

    private IDisposable? _offsetSubscription;
    private IDisposable? _extentSubscription;
    private IDisposable? _viewportSubscription;

    private bool _layoutUpdatePending;

    /// <summary>
    /// 直接子项模式下由本控件施加过视觉状态的容器。
    /// Avalonia 不会为"容器就是项"的情况调用 ClearContainerForItemOverride，
    /// 因此需要自己记账，在项被移除时复位这些容器。
    /// </summary>
    private readonly HashSet<TabBarItem> _ownedDirectContainers = new();

    private void UnsubscribeItemsCollectionChanged()
    {
        if (_itemsCollectionChanged == null) return;
        if (Items is INotifyCollectionChanged ncc)
            ncc.CollectionChanged -= _itemsCollectionChanged;
        _itemsCollectionChanged = null;
    }

    private void SubscribeItemsCollectionChanged()
    {
        UnsubscribeItemsCollectionChanged();
        if (Items is INotifyCollectionChanged notifyCollection) {
            _itemsCollectionChanged = OnItemsCollectionChanged;
            notifyCollection.CollectionChanged += _itemsCollectionChanged;
        }
    }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recyclingKey)
        => new TabBarItem();

    protected override bool NeedsContainerOverride(object? item, int index, out object? recyclingKey)
        => NeedsContainer<TabBarItem>(item, out recyclingKey);

    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);

        if (container is not TabBarItem tvi) return;

        // 在容器创建时立即设置 IsSelected（安全网）
        tvi.IsSelected = (index == SelectedIndex);

        // 容器刚实现时就把关闭按钮与分隔符状态写好。放在这里（而不是依赖
        // 布局完成后再扫一遍）才能保证新容器一出现状态就是正确的。
        // 此时 TabBarItem 的模板可能还没应用，_closeButton 为 null；
        // TabBarItem.OnApplyTemplate 会根据伪类恢复按钮可见性。
        UpdateCloseAndSeparatorState(tvi, index, SelectedIndex, CloseButtonOverlayMode);

        // 附加上下文菜单
        ApplyTabContextMenu(tvi);

        // ItemsSource 模式：把 Header / IconSource 绑定到数据项的成员路径上。
        // 直接子项模式下 item 就是容器本身，什么也不做。
        if (item != null && !ReferenceEquals(item, tvi)) {
            tvi.ApplyMemberBindings(item, HeaderMemberPath, IconSourceMemberPath);
            // 为标签页内容区域设置内容模板
            if (ContentTemplate != null)
                tvi.SetCurrentValue(ContentControl.ContentTemplateProperty, ContentTemplate);
        }
        else {
            // 直接子项模式。Avalonia 不会为"容器就是项"的情况调用
            // ClearContainerForItemOverride，所以要自己记账，移除时复位状态。
            _ownedDirectContainers.Add(tvi);
        }
    }

    protected override void ClearContainerForItemOverride(Control container)
    {
        base.ClearContainerForItemOverride(container);

        if (container is TabBarItem tvi)
            ReleaseContainer(tvi);
    }

    /// <summary>
    /// 复位由本控件施加到容器上的状态（绑定、宽度、伪类、上下文菜单）。
    /// </summary>
    private void ReleaseContainer(TabBarItem tvi)
    {
        _ownedDirectContainers.Remove(tvi);
        tvi.ClearMemberBindings();
        tvi.ResetManagedVisualState();
        // 只清理由本控件挂上去的菜单，不要动使用方自己设置的 ContextMenu。
        if (IsOwnedContextMenu(tvi.ContextMenu))
            tvi.ContextMenu = null;
    }

    /// <summary>
    /// 移除那些已不在 Items 中、但仍留有本控件状态的直接子项容器。
    /// 用于 Reset（例如 Items.Clear()）—— 这种事件不带 OldItems。
    /// </summary>
    private void ReleaseStaleDirectContainers()
    {
        if (_ownedDirectContainers.Count == 0) return;

        var live = new HashSet<object?>();
        foreach (var item in Items) live.Add(item);

        // 先物化再遍历：ReleaseContainer 会修改 _ownedDirectContainers。
        var stale = _ownedDirectContainers.Where(c => !live.Contains(c)).ToList();
        foreach (var c in stale) ReleaseContainer(c);
    }

    private ContextMenu? ResolveTabContextMenu()
        => IsTabContextMenuEnabled
            ? (TabItemContextMenu ?? GetOrCreateDefaultContextMenu())
            : null;

    /// <summary>
    /// 判断某个 ContextMenu 是否由本控件挂上去的。
    /// 使用方在 XAML 里给 TabBarItem 自己设置的菜单不应被覆盖或清除。
    /// </summary>
    private bool IsOwnedContextMenu(ContextMenu? menu)
        => menu != null
           && (ReferenceEquals(menu, _cachedContextMenu) || ReferenceEquals(menu, TabItemContextMenu));

    /// <summary>
    /// 应用内置/自定义标签页菜单。菜单功能关闭时只清除本控件挂上去的菜单，
    /// 保留使用方自己设置的 ContextMenu。
    /// </summary>
    private void ApplyTabContextMenu(TabBarItem tvi)
    {
        var menu = ResolveTabContextMenu();
        if (menu != null)
            tvi.ContextMenu = menu;
        else if (IsOwnedContextMenu(tvi.ContextMenu))
            tvi.ContextMenu = null;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        UnsubscribeTemplateParts();

        base.OnApplyTemplate(e);

        _tabContentPresenter = e.NameScope.Find<ContentPresenter>("PART_TabContentPresenter");
        _tabStripGrid = e.NameScope.Find<Grid>("PART_TabStripGrid");
        _addButton = e.NameScope.Find<Button>("PART_AddButton");
        _tabStripScrollViewer = e.NameScope.Find<ScrollViewer>("PART_TabStripScrollViewer");
        _scrollThumb = e.NameScope.Find<Border>("PART_ScrollThumb");

        SubscribeTemplateParts();
        // 连接来自 TabBarItem 的关闭事件处理程序（冒泡）
        if (!_closeHandlerWired) {
            AddHandler(TabBarItem.CloseRequestedEvent, OnTabItemCloseRequested);
            _closeHandlerWired = true;
        }
        CacheDimensionResources();
        UpdateTabContent();
        UpdateAllTabVisuals();
        // 确保布局完成后覆盖滚动条同步
        Dispatcher.UIThread.Post(SyncScrollThumb, DispatcherPriority.Render);
    }

    private void UnsubscribeTemplateParts()
    {
        if (_tabStripScrollViewer != null) {
            _tabStripScrollViewer.RemoveHandler(PointerWheelChangedEvent, OnTabStripPointerWheelChanged);
        }
        _offsetSubscription?.Dispose();
        _offsetSubscription = null;
        _extentSubscription?.Dispose();
        _extentSubscription = null;
        _viewportSubscription?.Dispose();
        _viewportSubscription = null;

        if (_scrollThumb != null) {
			_scrollThumb.PointerPressed -= OnScrollThumbPointerPressed;
		}
        if (_addButton != null) {
			_addButton.Click -= OnAddButtonClicked;
		}
        if (_tabStripGrid != null) {
            _tabStripGrid.SizeChanged -= OnTabStripSizeChanged;
            _tabStripGrid.PointerEntered -= OnTabStripPointerEntered;
            _tabStripGrid.PointerExited -= OnTabStripPointerExited;
        }
        UnsubscribeItemsCollectionChanged();
    }

    private void SubscribeTemplateParts()
    {
        if (_tabStripScrollViewer != null) {
            _tabStripScrollViewer.AddHandler(PointerWheelChangedEvent, OnTabStripPointerWheelChanged, RoutingStrategies.Tunnel);

            _offsetSubscription = _tabStripScrollViewer.GetObservable(ScrollViewer.OffsetProperty)
                .Subscribe(new AnonymousObserver<Vector>(_ => SyncScrollThumbPosition()));
            _extentSubscription = _tabStripScrollViewer.GetObservable(ScrollViewer.ExtentProperty)
                .Subscribe(new AnonymousObserver<Size>(_ => SyncScrollThumb()));
            // Viewport 就是标签条真正可用的宽度（模板里的 "*" 列已经扣掉了
            // Header / Footer / 新建按钮 及其 Margin）。它变化时重算标签宽度 ——
            // 这是唯一能在布局完成后拿到正确尺寸的时机。
            _viewportSubscription = _tabStripScrollViewer.GetObservable(ScrollViewer.ViewportProperty)
                .Subscribe(new AnonymousObserver<Size>(_ => {
                    SyncScrollThumb();
                    ScheduleLayoutUpdate();
                }));
        }
        if (_scrollThumb != null) {
			_scrollThumb.PointerPressed += OnScrollThumbPointerPressed;
		}
        if (_addButton != null) {
			_addButton.Click += OnAddButtonClicked;
		}
        if (_tabStripGrid != null) {
            _tabStripGrid.SizeChanged += OnTabStripSizeChanged;
            _tabStripGrid.PointerEntered += OnTabStripPointerEntered;
            _tabStripGrid.PointerExited += OnTabStripPointerExited;
        }
        // 订阅项集合更改
        SubscribeItemsCollectionChanged();
    }

    private void CacheDimensionResources()
    {
        if (this.TryFindResource(ResourceKeyMinWidth, out var mw) && mw is double d1) _cachedMinWidth = d1;
        if (this.TryFindResource(ResourceKeyMaxWidth, out var mxw) && mxw is double d2) _cachedMaxWidth = d2;
        if (this.TryFindResource(ResourceKeyCompactWidth, out var cw) && cw is double d3) _cachedCompactWidth = d3;
    }

    /// <summary>
    /// 当 ItemsSource 属性更改时调用。
    /// 取消订阅旧集合并订阅新集合。
    /// </summary>
    private void OnItemsReassigned()
    {
        // 守卫：如果模板尚未应用则跳过 — OnApplyTemplate
        // 将通过 SubscribeTemplateParts + UpdateAllTabVisuals 处理初始设置。
        if (_tabStripGrid == null) return;
        SubscribeItemsCollectionChanged();
        ScheduleLayoutUpdate();
    }

    private void UpdateTabContent()
    {
        _contentSubscription?.Dispose();
        _contentSubscription = null;
        _contentTemplateSubscription?.Dispose();
        _contentTemplateSubscription = null;

        if (_tabContentPresenter == null) return;

        if (SelectedItem == null) {
            _tabContentPresenter.Content = null;
            _tabContentPresenter.ContentTemplate = null;
            return;
        }

        var tvi = SelectedItem as TabBarItem ?? ContainerFromItem(SelectedItem) as TabBarItem;
        if (tvi == null) {
            _tabContentPresenter.Content = null;
            _tabContentPresenter.ContentTemplate = null;
            return;
        }

        _tabContentPresenter.Content = tvi.Content;
        _tabContentPresenter.ContentTemplate = tvi.ContentTemplate;
        _contentSubscription = tvi.GetObservable(ContentControl.ContentProperty)
            .Subscribe(new AnonymousObserver<object?>(c => { if (_tabContentPresenter != null) _tabContentPresenter.Content = c; }));
        _contentTemplateSubscription = tvi.GetObservable(ContentControl.ContentTemplateProperty)
            .Subscribe(new AnonymousObserver<IDataTemplate?>(t => { if (_tabContentPresenter != null) _tabContentPresenter.ContentTemplate = t; }));
    }

    private void OnAddButtonClicked(object? sender, RoutedEventArgs e)
    {
        AddTabButtonClick?.Invoke(this, EventArgs.Empty);
        if (AddTabButtonCommand?.CanExecute(AddTabButtonCommandParameter) == true) {
            AddTabButtonCommand.Execute(AddTabButtonCommandParameter);
        }
    }

    /// <summary>
    /// 解析关闭事件中 <see cref="TabBarCloseRequestedEventArgs.Item"/> 应该携带的对象。
    /// ItemsSource 模式下是底层数据项；直接子项模式下是容器自身。
    /// 绝不能用容器的 DataContext —— 直接子项模式下它是从父级继承来的
    /// ViewModel，所有标签页都会拿到同一个对象。
    /// </summary>
    internal object ResolveCloseItem(TabBarItem container)
    {
        if (ItemsSource != null) {
            var item = ItemFromContainer(container);
            if (item != null && !ReferenceEquals(item, container))
                return item;
        }
        return container;
    }

    private void OnTabItemCloseRequested(object? sender, TabBarCloseRequestedEventArgs e)
    {
        // 首先触发 TabCloseRequested 事件 — 外部处理程序有机会取消
        var closeArgs = new TabBarCloseRequestedEventArgs(e.Item, e.Tab);
        closeArgs.RoutedEvent = TabCloseRequestedEvent;
        RaiseEvent(closeArgs);

        if (closeArgs.Cancel) return;

        // 自动移除标签页
        int removedIndex = IndexFromContainer(e.Tab);
        if (removedIndex < 0) return;  // 容器不再有效，中止

        // 只有被关闭的标签页原本就是选中项时，才需要重新选择。
        bool wasSelected = removedIndex == SelectedIndex;
        object? previouslySelected = SelectedItem;
        object? itemToRemove = e.Item;

        // 直接子项模式：item 就是 TabBarItem
        // ItemsSource 模式：item 是数据对象
        if (ItemsSource == null) {
            // 直接子项模式。按索引移除，避免集合中存在重复容器时删错项。
            // 必须走 ItemCollection 自己的成员：ItemsSourceView 显式实现的
            // IList.Remove 会抛 NotSupportedException("Collection is read-only")。
            if (removedIndex < Items.Count && ReferenceEquals(Items[removedIndex], e.Tab))
                Items.RemoveAt(removedIndex);
            else
                Items.Remove(e.Tab);
        }
        else {
            // ItemsSource 模式 — 从源集合中移除
            if (itemToRemove != null) {
                if (ItemsSource is IList { IsReadOnly: false, IsFixedSize: false } sourceList) {
                    RemoveAtOrByValue(sourceList, removedIndex, itemToRemove);
                }
                else {
                    // 源集合是只读的、固定大小的，或者不是 IList。
                    // 请在 TabCloseRequested 事件处理程序中处理移除操作。
                    System.Diagnostics.Debug.WriteLine(
                        "[TabBar] Tab close requested but ItemsSource is not a mutable IList. " +
                        "Handle removal in the TabCloseRequested event instead.");
                    return; // 不要调整 SelectedIndex — 标签页未被移除
                }
            }
        }

        if (ItemCount == 0) return;

        if (!wasSelected) {
            // 关闭后台标签页不应该移动选中项。移除会让后面的索引前移，
            // 所以按对象而不是按索引恢复。
            if (previouslySelected != null && !ReferenceEquals(SelectedItem, previouslySelected))
                SelectedItem = previouslySelected;
            return;
        }

        // 关闭的是当前标签页：交给应用回调，或回退到相邻标签页。
        if (NextTabOnClose != null) {
            var next = NextTabOnClose(e.Item);
            if (next != null) {
                SelectedItem = next;
                return;
            }
        }
        SelectedIndex = Math.Min(removedIndex, ItemCount - 1);
    }

    /// <summary>
    /// 优先按索引移除（精确），索引处对象不匹配时回退到按值移除。
    /// </summary>
    private static void RemoveAtOrByValue(IList list, int index, object? value)
    {
        if (index >= 0 && index < list.Count && ReferenceEquals(list[index], value))
            list.RemoveAt(index);
        else
            list.Remove(value);
    }

    private void OnTabStripSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        // ScheduleLayoutUpdate 之后运行的 UpdateAllTabVisuals 已经包含了
        // 关闭按钮与分隔符的更新，不需要再单独跑一遍 O(n)。
        ScheduleLayoutUpdate();
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // 直接子项模式下 Avalonia 不调用 ClearContainerForItemOverride，
        // 在这里复位被移除容器上的托管状态（宽度 / 伪类 / 菜单 / 绑定）。
        if (_ownedDirectContainers.Count > 0) {
            if (e.Action == NotifyCollectionChangedAction.Reset) {
                ReleaseStaleDirectContainers();
            }
            else if (e.OldItems != null) {
                foreach (var old in e.OldItems) {
                    if (old is TabBarItem tvi)
                        ReleaseContainer(tvi);
                }
            }
        }

        ScheduleLayoutUpdate();
    }

    /// <summary>
    /// 合并同一帧内的多次视觉更新为一次 O(n) 遍历。
    /// 可从任意线程调用。
    /// </summary>
    internal void ScheduleLayoutUpdate()
    {
        // 集合可能被后台线程修改，此时不能直接读写 _layoutUpdatePending。
        if (!Dispatcher.UIThread.CheckAccess()) {
            Dispatcher.UIThread.Post(ScheduleLayoutUpdate, DispatcherPriority.Render);
            return;
        }

        if (_layoutUpdatePending) return;
        _layoutUpdatePending = true;
        Dispatcher.UIThread.Post(() => {
            _layoutUpdatePending = false;
            UpdateAllTabVisuals();
        }, DispatcherPriority.Render);
    }

    /// <summary>
    /// 在一次 O(n) 遍历中更新所有已实现的标签页容器的宽度、选中状态、
    /// 关闭按钮可见性和分隔符可见性。
    /// </summary>
    /// <remarks>
    /// 与 <see cref="OnCloseAllTabs"/> / <see cref="CanCloseOtherTabs"/> 等一样，
    /// 本方法只遍历"已实现"的容器。这依赖模板使用非虚拟化的面板
    /// （见 SimTabBarTheme.axaml 中的 StackPanel）；若改用
    /// VirtualizingStackPanel，未实现的标签页会被静默跳过。
    /// </remarks>
    internal void UpdateAllTabVisuals()
    {
        int count = ItemCount;
        if (count == 0) {
            UpdateCommandCanExecute();
            return;
        }

        var widthMode = TabWidthMode;
        var overlayMode = CloseButtonOverlayMode;
        int selectedIndex = SelectedIndex;

        // 可用宽度直接取 ScrollViewer 的视口宽度：模板中标签条所在的 "*" 列
        // 已经把 Header / Footer / 新建按钮 及其 Margin 全部扣除了。手工用
        // Bounds 相减会漏掉 Margin，而且在属性变更回调里这些 Bounds 还没测量。
        double availableWidth = _tabStripScrollViewer?.Viewport.Width ?? 0;
        bool canUpdateWidth = availableWidth > 0;

        double minWidth = _cachedMinWidth;
        double maxWidth = _cachedMaxWidth;
        double compactWidth = _cachedCompactWidth;
        double equalTabWidth = 0;

        if (canUpdateWidth && widthMode == TabBarWidthMode.Equal) {
            // 必须向下取整到整数设备像素。UseLayoutRounding 默认为 true，布局会把
            // 每个标签页的宽度**向上**取整（151.2 -> 152），n 个标签累加后就会超出
            // 视口若干像素，凭空冒出一条滚动条。
            equalTabWidth = Math.Clamp(FloorToDevicePixel(availableWidth / count), minWidth, maxWidth);
        }

        for (int i = 0; i < count; i++) {
            if (ContainerFromIndex(i) is not TabBarItem tvi) continue;

            // === 宽度（需要视口宽度）===
            if (canUpdateWidth) {
                switch (widthMode)
                {
                    case TabBarWidthMode.Equal:
                        tvi.Width = equalTabWidth;
                        tvi.SetCompact(false);
                        break;

                    case TabBarWidthMode.SizeToContent:
                        tvi.Width = double.NaN;
                        tvi.SetCompact(false);
                        break;

                    case TabBarWidthMode.Compact:
                        if (i == selectedIndex) {
                            double selectedWidth = availableWidth - (compactWidth * (count - 1));
                            selectedWidth = Math.Max(FloorToDevicePixel(selectedWidth), minWidth);
                            tvi.Width = selectedWidth;
                            tvi.SetCompact(false);
                        }
                        else {
                            // 注意：紧凑宽度小于主题里的 MinWidth，靠 ControlTheme 中
                            // "^:compact" 的 MinWidth setter 放开下限，否则布局会把
                            // 宽度重新夹回 MinWidth。
                            tvi.Width = compactWidth;
                            tvi.SetCompact(true);
                        }
                        break;
                }
            }

            // === 选择 ===
            tvi.IsSelected = (i == selectedIndex);
            // === 关闭按钮和分隔符 ===
            UpdateCloseAndSeparatorState(tvi, i, selectedIndex, overlayMode);
        }
        UpdateCommandCanExecute();
    }

    /// <summary>
    /// 向下取整到整数设备像素。
    /// 布局在 UseLayoutRounding 开启时会把宽度向上取整，若直接使用
    /// availableWidth / count 这种分数值，n 个标签累加后必然超出视口。
    /// </summary>
    private double FloorToDevicePixel(double value)
    {
        double scale = Avalonia.Layout.LayoutHelper.GetLayoutScale(this);
        if (scale <= 0 || double.IsNaN(scale) || double.IsInfinity(scale)) return Math.Floor(value);
        return Math.Floor(value * scale) / scale;
    }

    /// <summary>
    /// 更新关闭按钮可见性和分隔符状态，无需布局度量。
    /// 供只影响这两者的属性变更使用。
    /// </summary>
    private void UpdateCloseButtonAndSeparatorState()
    {
        int count = ItemCount;
        if (count == 0) return;

        var overlayMode = CloseButtonOverlayMode;
        int selectedIndex = SelectedIndex;

        for (int i = 0; i < count; i++) {
            if (ContainerFromIndex(i) is not TabBarItem tvi) continue;
            UpdateCloseAndSeparatorState(tvi, i, selectedIndex, overlayMode);
        }
    }

    /// <summary>
    /// 将关闭按钮可见性和分隔符伪类应用于单个标签页容器。
    /// 由 UpdateAllTabVisuals 和 UpdateCloseButtonAndSeparatorState 共用的逻辑。
    /// </summary>
    private static void UpdateCloseAndSeparatorState(
        TabBarItem tvi, int index, int selectedIndex,
        TabBarCloseButtonOverlayMode overlayMode)
    {
        // --- 关闭按钮可见性 ---
        bool closeCollapsed;
        bool closeAlways = false;
        bool closeOnHover = false;

        // 紧凑标签页只有 36px，放不下图标 + 关闭按钮，一律隐藏关闭按钮。
        // 紧凑状态由本次遍历中先执行的 SetCompact 写入伪类，两条调用路径都能读到。
        if (!tvi.IsClosable || tvi.HasPseudoClass(TabBarItem.PcCompact)) {
            closeCollapsed = true;
        }
        else {
            switch (overlayMode)
            {
                case TabBarCloseButtonOverlayMode.Always:
                    closeCollapsed = false;
                    closeAlways = true;
                    break;
                case TabBarCloseButtonOverlayMode.OnPointerOver:
                    if (index == selectedIndex) {
                        closeCollapsed = false;
                        closeAlways = true;
                    }
                    else {
                        closeCollapsed = true;
                        closeOnHover = true;
                    }
                    break;
                case TabBarCloseButtonOverlayMode.Auto:
                default:
                    closeCollapsed = false;
                    break;
            }
        }

        tvi.SetCloseButtonState(closeCollapsed, closeAlways, closeOnHover);
        // --- 分隔符可见性 ---
        bool showSeparator = (index != selectedIndex) && (index != selectedIndex - 1);
        tvi.SetSeparatorState(showSeparator);
    }

    private void OnTabStripPointerEntered(object? sender, PointerEventArgs e)
    {
        if (_scrollThumb != null && _scrollThumb.IsVisible) {
            _scrollThumb.Opacity = ThumbHoverOpacity;
        }
    }

    private void OnTabStripPointerExited(object? sender, PointerEventArgs e)
    {
        if (_scrollThumb != null && !_isDraggingThumb) {
            _scrollThumb.Opacity = 0;
        }
    }

    private void OnTabStripPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (_tabStripScrollViewer == null) return;

        double maxOffset = _tabStripScrollViewer.Extent.Width - _tabStripScrollViewer.Viewport.Width;
        // 没有横向溢出：不要吞掉滚轮事件，否则外层滚动容器（例如整页滚动）
        // 在指针悬停于标签条上时会完全失灵。
        if (maxOffset <= 0) return;

        var offset = _tabStripScrollViewer.Offset;
        double newOffsetX = Math.Clamp(offset.X - (e.Delta.Y * WheelScrollAmount), 0, maxOffset);
        // 已经滚到边界，同样把事件交还给外层。
        if (newOffsetX == offset.X) return;

        _tabStripScrollViewer.Offset = new Vector(newOffsetX, offset.Y);
        e.Handled = true;
    }

    private (double scrollableRange, double movableRange, double thumbWidth)? GetScrollGeometry()
    {
        if (_tabStripScrollViewer == null || _scrollThumb == null) return null;

        double extentW = _tabStripScrollViewer.Extent.Width;
        double viewportW = _tabStripScrollViewer.Viewport.Width;
        double scrollableRange = extentW - viewportW;
        if (scrollableRange <= 0) return null;

        double thumbWidth = Math.Max(24, (viewportW / extentW) * viewportW);
        double movableRange = viewportW - thumbWidth;
        return (scrollableRange, movableRange, thumbWidth);
    }

    private void SyncScrollThumb()
    {
        if (_tabStripScrollViewer == null || _scrollThumb == null) return;

        var extent = _tabStripScrollViewer.Extent;
        var viewport = _tabStripScrollViewer.Viewport;

        bool hasOverflow = extent.Width > viewport.Width + 1;
        _scrollThumb.IsVisible = hasOverflow;

        if (!hasOverflow) {
            _scrollThumb.Opacity = 0;
            return;
        }

        var geo = GetScrollGeometry();
        if (geo == null) return;

        _scrollThumb.Width = geo.Value.thumbWidth;
        _scrollThumb.Margin = new Thickness(ComputeThumbOffset(geo.Value), 0, 0, 0);
    }

    private void SyncScrollThumbPosition()
    {
        if (_tabStripScrollViewer == null || _scrollThumb == null) return;
        if (!_scrollThumb.IsVisible) return;

        var geo = GetScrollGeometry();
        if (geo == null) return;

        _scrollThumb.Margin = new Thickness(ComputeThumbOffset(geo.Value), 0, 0, 0);
    }

    private double ComputeThumbOffset((double scrollableRange, double movableRange, double thumbWidth) geo)
    {
        // 滑块比视口还宽时 movableRange 为负，此时没有可移动空间。
        if (geo.movableRange <= 0 || geo.scrollableRange <= 0) return 0;
        double offsetX = _tabStripScrollViewer!.Offset.X;
        return Math.Clamp(offsetX / geo.scrollableRange, 0, 1) * geo.movableRange;
    }

    private void OnScrollThumbPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_scrollThumb == null || _tabStripScrollViewer == null) return;
        _isDraggingThumb = true;
        _dragStartX = e.GetPosition(_tabStripScrollViewer).X;
        _dragStartOffset = _tabStripScrollViewer.Offset.X;
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    private void OnScrollThumbPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDraggingThumb || _tabStripScrollViewer == null || _scrollThumb == null) return;

        var geo = GetScrollGeometry();
        if (geo == null || geo.Value.movableRange <= 0) return;

        double deltaX = e.GetPosition(_tabStripScrollViewer).X - _dragStartX;
        double scrollDelta = (deltaX / geo.Value.movableRange) * geo.Value.scrollableRange;
        double newOffset = Math.Clamp(_dragStartOffset + scrollDelta, 0, geo.Value.scrollableRange);

        _tabStripScrollViewer.Offset = new Vector(newOffset, _tabStripScrollViewer.Offset.Y);
        e.Handled = true;
    }

    private void OnScrollThumbPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        EndThumbDrag();
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    /// <summary>
    /// 结束滑块拖动，并按指针当前是否还在标签条上恢复滑块透明度。
    /// 拖动期间 OnTabStripPointerExited 会被跳过，所以必须在这里补上，
    /// 否则滑块会永久停留在悬停透明度。
    /// </summary>
    private void EndThumbDrag()
    {
        if (!_isDraggingThumb) return;
        _isDraggingThumb = false;

        if (_scrollThumb == null) return;
        bool pointerStillOver = _tabStripGrid?.IsPointerOver == true;
        _scrollThumb.Opacity = (pointerStillOver && _scrollThumb.IsVisible) ? ThumbHoverOpacity : 0;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_isDraggingThumb) {
            OnScrollThumbPointerMoved(this, e);
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_isDraggingThumb) {
            OnScrollThumbPointerReleased(this, e);
        }
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        EndThumbDrag();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (!IsBuiltInKeyboardHandlingEnabled) return;

        if (e.Key == Key.Tab && e.KeyModifiers == KeyModifiers.Control) {
            SelectNextTab();
            e.Handled = true;
        }
        else if (e.Key == Key.Tab && e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift)) {
            SelectPreviousTab();
            e.Handled = true;
        }
        else if (e.Key == Key.F4 && e.KeyModifiers == KeyModifiers.Control) {
            SelectedContainer()?.RaiseCloseRequested();
            e.Handled = true;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        DisposeSubscriptions();
    }

    /// <summary>
    /// 释放所有可观察对象订阅和事件处理程序。
    /// 可安全地多次调用（幂等）。重新挂回可视化树时 OnApplyTemplate 会重新订阅。
    /// </summary>
    private void DisposeSubscriptions()
    {
        _offsetSubscription?.Dispose();
        _offsetSubscription = null;
        _extentSubscription?.Dispose();
        _extentSubscription = null;
        _viewportSubscription?.Dispose();
        _viewportSubscription = null;
        _contentSubscription?.Dispose();
        _contentSubscription = null;
        _contentTemplateSubscription?.Dispose();
        _contentTemplateSubscription = null;
        UnsubscribeItemsCollectionChanged();
    }

    public void SelectNextTab()
    {
        if (ItemCount == 0) return;
        SelectedIndex = (SelectedIndex + 1) % ItemCount;
    }

    public void SelectPreviousTab()
    {
        if (ItemCount == 0) return;
        SelectedIndex = (SelectedIndex - 1 + ItemCount) % ItemCount;
    }

    /// <summary>
    /// 当前选中项对应的容器（直接子项模式下就是选中项本身）。
    /// </summary>
    private TabBarItem? SelectedContainer()
    {
        var selected = SelectedItem;
        if (selected == null) return null;
        return selected as TabBarItem ?? ContainerFromItem(selected) as TabBarItem;
    }

    private ContextMenu GetOrCreateDefaultContextMenu()
    {
        return _cachedContextMenu ??= new ContextMenu
        {
            Items =
            {
                new MenuItem
                {
                    Header = "Close",
                    Command = CloseTabCommand
                },
                new MenuItem
                {
                    Header = "Close Other Tabs",
                    Command = CloseOtherTabsCommand
                },
                new MenuItem
                {
                    Header = "Close All Tabs",
                    Command = CloseAllTabsCommand
                }
            }
        };
    }

    private void UpdateAllContainerMenus()
    {
        for (int i = 0; i < ItemCount; i++) {
            if (ContainerFromIndex(i) is TabBarItem tvi) {
                ApplyTabContextMenu(tvi);
            }
        }
    }

    /// <summary>
    /// HeaderMemberPath / IconSourceMemberPath 运行时变更后，
    /// 重新绑定所有已实现容器。
    /// </summary>
    private void RefreshContainerMemberBindings()
    {
        for (int i = 0; i < ItemCount; i++) {
            if (ContainerFromIndex(i) is not TabBarItem tvi) continue;
            var item = ItemFromContainer(tvi);
            if (item != null && !ReferenceEquals(item, tvi))
                tvi.ApplyMemberBindings(item, HeaderMemberPath, IconSourceMemberPath);
        }
    }

    /// <summary>
    /// ContentTemplate 运行时变更后，同步到所有已实现容器。
    /// 置为 null 时回落到 ItemsControl 自身的 ItemTemplate。
    /// </summary>
    private void RefreshContainerContentTemplates()
    {
        var template = ContentTemplate ?? ItemTemplate;
        for (int i = 0; i < ItemCount; i++) {
            if (ContainerFromIndex(i) is not TabBarItem tvi) continue;
            var item = ItemFromContainer(tvi);
            if (item != null && !ReferenceEquals(item, tvi))
                tvi.SetCurrentValue(ContentControl.ContentTemplateProperty, template);
        }
    }

    private void OnCloseTab() => SelectedContainer()?.RaiseCloseRequested();

    private bool CanCloseOtherTabs()
    {
        if (ItemCount <= 1) return false;
        var selected = SelectedContainer();

        for (int i = 0; i < ItemCount; i++) {
            if (ContainerFromIndex(i) is TabBarItem tab && tab != selected && tab.IsClosable)
                return true;
        }
        return false;
    }

    private void OnCloseOtherTabs()
    {
        var selected = SelectedContainer();
        if (selected == null) return;

        CloseAll(tab => tab != selected);
    }

    private bool CanCloseAllTabs()
    {
        for (int i = 0; i < ItemCount; i++) {
            if (ContainerFromIndex(i) is TabBarItem tab && tab.IsClosable)
                return true;
        }
        return false;
    }

    private void OnCloseAllTabs() => CloseAll(_ => true);

    /// <summary>
    /// 关闭所有满足条件且可关闭的标签页。先收集容器再逐个请求关闭，
    /// 避免在遍历过程中修改集合。
    /// </summary>
    private void CloseAll(Func<TabBarItem, bool> predicate)
    {
        var toClose = new List<TabBarItem>();
        for (int i = 0; i < ItemCount; i++) {
            if (ContainerFromIndex(i) is TabBarItem tab && tab.IsClosable && predicate(tab))
                toClose.Add(tab);
        }

        foreach (var tab in toClose) {
            tab.RaiseCloseRequested();
        }

        // 关闭完成之后再刷新命令状态 —— 在循环之前刷新时状态还没变，是无效调用。
        UpdateCommandCanExecute();
    }

    private void UpdateCommandCanExecute()
    {
        _closeOtherTabsCommand?.NotifyCanExecuteChanged();
        _closeAllTabsCommand?.NotifyCanExecuteChanged();
    }
}
