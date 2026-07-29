using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Reactive;
using SimTabBar.Icons;
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
public class TabBar : SelectingItemsControl, IDisposable
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
			UpdateAllTabVisuals();
			UpdateCommandCanExecute();
		};
	}

	static TabBar()
	{
		TabWidthModeProperty.Changed.AddClassHandler<TabBar>((x, _) => x.UpdateAllTabVisuals());
		CloseButtonOverlayModeProperty.Changed.AddClassHandler<TabBar>((x, _) => x.UpdateAllTabVisuals());
		IsAddTabButtonVisibleProperty.Changed.AddClassHandler<TabBar>((x, _) => x.UpdateAllTabVisuals());
		TabStripHeaderProperty.Changed.AddClassHandler<TabBar>((x, _) => x.UpdateAllTabVisuals());
		TabStripFooterProperty.Changed.AddClassHandler<TabBar>((x, _) => x.UpdateAllTabVisuals());
		ItemsSourceProperty.Changed.AddClassHandler<TabBar>((x, _) => x.OnItemsReassigned());
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
	
    private const string ResourceKeyMinWidth = "TabBarItemMinWidth";
    private const string ResourceKeyMaxWidth = "TabBarItemMaxWidth";
    private const string ResourceKeyCompactWidth = "TabBarItemCompactWidth";

    private ContentPresenter? _tabContentPresenter;
    private ContentPresenter? _headerPresenter;
    private ContentPresenter? _footerPresenter;
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
    private bool _disposed;

    /// <summary>
    /// 缓存 HeaderMemberPath 和 IconSourceMemberPath 解析的属性信息查找，
    /// 以避免每个项的反射开销。假设类型多样性有限：通常为单个
    /// ItemsSource 数据类型，具有一致的属性结构。
    /// </summary>
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<(Type, string), System.Reflection.PropertyInfo?> _propertyCache = new();

    private void UnsubscribeItemsCollectionChanged()
    {
        if (_itemsCollectionChanged != null && Items is INotifyCollectionChanged ncc) {
            ncc.CollectionChanged -= _itemsCollectionChanged;
            _itemsCollectionChanged = null;
        }
    }

    private void SubscribeItemsCollectionChanged()
    {
        _itemsCollectionChanged = OnItemsCollectionChanged;
        if (Items is INotifyCollectionChanged notifyCollection) {
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

        if (container is TabBarItem tvi) {
            // 在容器创建时立即设置 IsSelected（安全网）
            tvi.IsSelected = (index == SelectedIndex);

            // 在容器创建时立即设置分隔符状态
            bool showSeparator = (index != SelectedIndex) && (index != SelectedIndex - 1);
            tvi.SetSeparatorState(showSeparator);

            // 附加上下文菜单
            tvi.ContextMenu = IsTabContextMenuEnabled
                ? (TabItemContextMenu ?? GetOrCreateDefaultContextMenu())
                : null;

            if (item != null && item is not TabBarItem) {
                // ItemsSource 模式：从成员路径设置 Header 和 IconSource
                // 使用 SetCurrentValue 以获得较低优先级，允许用户样式覆盖
                if (!string.IsNullOrEmpty(HeaderMemberPath)) {
                    var headerValue = GetPropertyValue(item, HeaderMemberPath);
                    tvi.SetCurrentValue(TabBarItem.HeaderProperty, headerValue);
                }
                if (!string.IsNullOrEmpty(IconSourceMemberPath)) {
                    var iconValue = GetPropertyValue(item, IconSourceMemberPath);
                    tvi.SetCurrentValue(TabBarItem.IconSourceProperty, iconValue as IconSource);
                }
                // 为标签页内容区域设置内容模板
                if (ContentTemplate != null) {
                    tvi.SetCurrentValue(ContentControl.ContentTemplateProperty, ContentTemplate);
                }
            }
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        UnsubscribeTemplateParts();

        base.OnApplyTemplate(e);

        _tabContentPresenter = e.NameScope.Find<ContentPresenter>("PART_TabContentPresenter");
        _tabStripGrid = e.NameScope.Find<Grid>("PART_TabStripGrid");
        _addButton = e.NameScope.Find<Button>("PART_AddButton");
        _tabStripScrollViewer = e.NameScope.Find<ScrollViewer>("PART_TabStripScrollViewer");
        _headerPresenter = e.NameScope.Find<ContentPresenter>("PART_HeaderPresenter");
        _footerPresenter = e.NameScope.Find<ContentPresenter>("PART_FooterPresenter");
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
        Avalonia.Threading.Dispatcher.UIThread.Post(SyncScrollThumb, Avalonia.Threading.DispatcherPriority.Render);
    }

    private void UnsubscribeTemplateParts()
    {
        if (_tabStripScrollViewer != null) {
            _tabStripScrollViewer.RemoveHandler(PointerWheelChangedEvent, OnTabStripPointerWheelChanged);
            _offsetSubscription?.Dispose();
            _offsetSubscription = null;
            _extentSubscription?.Dispose();
            _extentSubscription = null;
            _viewportSubscription?.Dispose();
            _viewportSubscription = null;
        }

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
            _viewportSubscription = _tabStripScrollViewer.GetObservable(ScrollViewer.ViewportProperty)
                .Subscribe(new AnonymousObserver<Size>(_ => SyncScrollThumb()));
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
        UnsubscribeItemsCollectionChanged();
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
        object? itemToRemove = e.Item;

        // 直接子项模式：item 就是 TabBarItem
        // ItemsSource 模式：item 是数据对象
        if (ItemsSource == null) {
            // 直接子项模式
            Items.Remove(e.Tab);
        }
        else {
            // ItemsSource 模式 — 从源集合中移除
            if (itemToRemove != null) {
                if (ItemsSource is IList { IsReadOnly: false, IsFixedSize: false } sourceList) {
                    sourceList.Remove(itemToRemove);
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

        // 自动选择下一个标签页
        if (ItemCount > 0) {
            if (NextTabOnClose != null) {
                var next = NextTabOnClose(e.Item);
                if (next != null) {
                    SelectedItem = next;
                    return;
                }
            }
            // 回退：选择相邻的标签页
            SelectedIndex = Math.Min(removedIndex, ItemCount - 1);
        }
    }

    private void OnTabStripSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        ScheduleLayoutUpdate();
        // 关闭按钮/分隔符状态不依赖于布局度量，立即更新
        UpdateCloseButtonAndSeparatorState();
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ScheduleLayoutUpdate();
        // 发布到 UI 线程，以防止当 ItemsSource
        // 从后台线程修改时出现跨线程访问
        Avalonia.Threading.Dispatcher.UIThread.Post(
            UpdateCloseButtonAndSeparatorState,
            Avalonia.Threading.DispatcherPriority.Render);
    }

    private void ScheduleLayoutUpdate()
    {
        if (_layoutUpdatePending) return;
        _layoutUpdatePending = true;
        Avalonia.Threading.Dispatcher.UIThread.Post(() => {
            _layoutUpdatePending = false;
            UpdateAllTabVisuals();
        }, Avalonia.Threading.DispatcherPriority.Render);
    }

    /// <summary>
    /// 在一次 O(n) 遍历中更新所有已实现的标签页容器的宽度、选中状态、
    /// 关闭按钮可见性和分隔符可见性。
    /// </summary>
    internal void UpdateAllTabVisuals()
    {
        int count = ItemCount;
        if (count == 0) return;

        var widthMode = TabWidthMode;
        var overlayMode = CloseButtonOverlayMode;
        int selectedIndex = SelectedIndex;

        // 预计算宽度参数（仅在网格可用时）
        double availableWidth = 0;
        double minWidth = _cachedMinWidth;
        double maxWidth = _cachedMaxWidth;
        double compactWidth = _cachedCompactWidth;
        bool canUpdateWidth = false;
        double equalTabWidth = 0;

        if (_tabStripGrid != null) {
            canUpdateWidth = true;
            availableWidth = _tabStripGrid.Bounds.Width;

            if (_headerPresenter != null) availableWidth -= _headerPresenter.Bounds.Width;
            if (_footerPresenter != null) availableWidth -= _footerPresenter.Bounds.Width;
            if (_addButton != null && IsAddTabButtonVisible) availableWidth -= _addButton.Bounds.Width;

            availableWidth = Math.Max(availableWidth, 0);

            if (widthMode == TabBarWidthMode.Equal) {
                equalTabWidth = availableWidth / count;
                equalTabWidth = Math.Clamp(equalTabWidth, minWidth, maxWidth);
            }
        }

        for (int i = 0; i < count; i++) {
            if (ContainerFromIndex(i) is not TabBarItem tvi) continue;

            // === 宽度（需要网格）===
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
                            selectedWidth = Math.Max(selectedWidth, minWidth);
                            tvi.Width = selectedWidth;
                            tvi.SetCompact(false);
                        }
                        else {
                            tvi.Width = compactWidth;
                            tvi.SetCompact(true);
                        }
                        break;
                }
            }

            // === 选择 ===
            tvi.IsSelected = (i == selectedIndex);
            // === 关闭按钮和分隔符 ===
            UpdateCloseAndSeparatorState(tvi, i, count, selectedIndex, overlayMode);
        }
        UpdateCommandCanExecute();
    }

    /// <summary>
    /// 更新关闭按钮可见性和分隔符状态，无需网格布局。
    /// 从事件处理程序中调用，此时容器可能已存在但不需要网格度量。
    /// </summary>
    private void UpdateCloseButtonAndSeparatorState()
    {
        int count = ItemCount;
        if (count == 0) return;

        var overlayMode = CloseButtonOverlayMode;
        int selectedIndex = SelectedIndex;

        for (int i = 0; i < count; i++) {
            if (ContainerFromIndex(i) is not TabBarItem tvi) continue;
            UpdateCloseAndSeparatorState(tvi, i, count, selectedIndex, overlayMode);
        }
    }

    /// <summary>
    /// 将关闭按钮可见性和分隔符伪类应用于单个标签页容器。
    /// 由 UpdateAllTabVisuals 和 UpdateCloseButtonAndSeparatorState 共用的逻辑。
    /// </summary>
    private static void UpdateCloseAndSeparatorState(
        TabBarItem tvi, int index, int count, int selectedIndex,
        TabBarCloseButtonOverlayMode overlayMode)
    {
        // --- 关闭按钮可见性 ---
        bool closeCollapsed;
        bool closeAlways = false;
        bool closeOnHover = false;

        if (!tvi.IsClosable) {
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
            _scrollThumb.Opacity = 0.8;
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

        var offset = _tabStripScrollViewer.Offset;
        double scrollAmount = 50;
        double newOffsetX = offset.X - (e.Delta.Y * scrollAmount);
        newOffsetX = Math.Max(0, newOffsetX);
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

        if (!hasOverflow) return;

        var geo = GetScrollGeometry();
        if (geo == null) return;

        _scrollThumb.Width = geo.Value.thumbWidth;
        double thumbX = geo.Value.scrollableRange > 0
            ? (_tabStripScrollViewer.Offset.X / geo.Value.scrollableRange) * geo.Value.movableRange
            : 0;
        _scrollThumb.Margin = new Thickness(thumbX, 0, 0, 0);
    }

    private void SyncScrollThumbPosition()
    {
        if (_tabStripScrollViewer == null || _scrollThumb == null) return;
        if (!_scrollThumb.IsVisible) return;

        var geo = GetScrollGeometry();
        if (geo == null) return;

        double thumbX = geo.Value.scrollableRange > 0
            ? (_tabStripScrollViewer.Offset.X / geo.Value.scrollableRange) * geo.Value.movableRange
            : 0;
        _scrollThumb.Margin = new Thickness(thumbX, 0, 0, 0);
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
        _isDraggingThumb = false;
        e.Pointer.Capture(null);
        e.Handled = true;
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
        _isDraggingThumb = false;
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
            if (SelectedItem is TabBarItem tvi) {
                tvi.RaiseCloseRequested();
            }
            else if (SelectedItem != null) {
                var container = ContainerFromItem(SelectedItem) as TabBarItem;
                container?.RaiseCloseRequested();
            }
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
    /// 可安全地多次调用（幂等）。
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

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed) {
            if (disposing) {
                DisposeSubscriptions();
            }
            _disposed = true;
        }
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
                tvi.ContextMenu = IsTabContextMenuEnabled
                    ? (TabItemContextMenu ?? GetOrCreateDefaultContextMenu())
                    : null;
            }
        }
    }

    private void OnCloseTab()
    {
        if (SelectedItem is TabBarItem tvi) {
            tvi.RaiseCloseRequested();
        }
        else if (SelectedItem != null && ContainerFromItem(SelectedItem) is TabBarItem container) {
            container.RaiseCloseRequested();
        }
    }

    private bool CanCloseOtherTabs()
    {
        if (ItemCount <= 1) return false;
        var selected = SelectedItem is TabBarItem t
            ? t
            : SelectedItem != null ? ContainerFromItem(SelectedItem) as TabBarItem : null;

        for (int i = 0; i < ItemCount; i++) {
            if (ContainerFromIndex(i) is TabBarItem tab && tab != selected && tab.IsClosable)
                return true;
        }
        return false;
    }

    private void OnCloseOtherTabs()
    {
        var selected = SelectedItem is TabBarItem t
            ? t
            : SelectedItem != null ? ContainerFromItem(SelectedItem) as TabBarItem : null;
        if (selected == null) return;

        var toClose = new List<TabBarItem>();
        for (int i = 0; i < ItemCount; i++) {
            if (ContainerFromIndex(i) is TabBarItem tab && tab != selected && tab.IsClosable)
                toClose.Add(tab);
        }

        _closeOtherTabsCommand?.NotifyCanExecuteChanged();
        _closeAllTabsCommand?.NotifyCanExecuteChanged();

        foreach (var tab in toClose) {
			tab.RaiseCloseRequested();
		}    
    }

    private bool CanCloseAllTabs()
    {
        for (int i = 0; i < ItemCount; i++) {
            if (ContainerFromIndex(i) is TabBarItem tab && tab.IsClosable)
                return true;
        }
        return false;
    }

    private void OnCloseAllTabs()
    {
        var toClose = new List<TabBarItem>();
        for (int i = 0; i < ItemCount; i++) {
            if (ContainerFromIndex(i) is TabBarItem tab && tab.IsClosable)
                toClose.Add(tab);
        }

        _closeOtherTabsCommand?.NotifyCanExecuteChanged();
        _closeAllTabsCommand?.NotifyCanExecuteChanged();

        foreach (var tab in toClose) {
            tab.RaiseCloseRequested(); 
        }
    }

    private void UpdateCommandCanExecute()
    {
        _closeOtherTabsCommand?.NotifyCanExecuteChanged();
        _closeAllTabsCommand?.NotifyCanExecuteChanged();
    }

    private static object? GetPropertyValue(object item, string propertyPath)
    {
        try {
            var type = item.GetType();
            var prop = _propertyCache.GetOrAdd((type, propertyPath), key => key.Item1.GetProperty(key.Item2));
            return prop?.GetValue(item);
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine(
                $"[TabBar] Failed to resolve property '{propertyPath}' " +
                $"on type '{item.GetType().Name}': {ex.Message}");
            return null;
        }
    }
}
