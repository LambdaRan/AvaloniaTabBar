using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using SimTabBar.Icons;

namespace SimTabBar.Controls;

[TemplatePart("PART_RootBorder", typeof(Border))]
[TemplatePart("PART_IconPresenter", typeof(ContentPresenter))]
[TemplatePart("PART_HeaderPresenter", typeof(ContentPresenter))]
[TemplatePart("PART_CloseButton", typeof(Button))]
[TemplatePart("PART_ActiveIndicator", typeof(Border))]
[TemplatePart("PART_Separator", typeof(Border))]
[PseudoClasses("separator", "compact", "closecollapsed", "closealways", "closeoverlay", "icon")]
public class TabBarItem : ContentControl
{
    public static readonly StyledProperty<object?> HeaderProperty =
        AvaloniaProperty.Register<TabBarItem, object?>(nameof(Header));

    public static readonly StyledProperty<Avalonia.Controls.Templates.IDataTemplate?> HeaderTemplateProperty =
        AvaloniaProperty.Register<TabBarItem, Avalonia.Controls.Templates.IDataTemplate?>(nameof(HeaderTemplate));

    public static readonly StyledProperty<IconSource?> IconSourceProperty =
        AvaloniaProperty.Register<TabBarItem, IconSource?>(nameof(IconSource));

    public static readonly StyledProperty<bool> IsClosableProperty =
        AvaloniaProperty.Register<TabBarItem, bool>(nameof(IsClosable), defaultValue: true);

    public static readonly StyledProperty<bool> IsSelectedProperty =
        SelectingItemsControl.IsSelectedProperty.AddOwner<TabBarItem>();

    public static readonly RoutedEvent<TabBarCloseRequestedEventArgs> CloseRequestedEvent =
        RoutedEvent.Register<TabBarItem, TabBarCloseRequestedEventArgs>(
            nameof(CloseRequested), RoutingStrategies.Bubble);

    private Control? _iconElement;
    private ContentPresenter? _iconPresenter;
    private Button? _closeButton;
    private EventHandler<RoutedEventArgs>? _closeButtonClickHandler;
    private TextBlock? _compactFallbackTextBlock;
    private string? _cachedCompactFallbackText;
    private bool _showCloseOnHover;

    public TabBarItem()
    {
        ContextRequested += OnTabContextRequested;
    }

    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public Avalonia.Controls.Templates.IDataTemplate? HeaderTemplate
    {
        get => GetValue(HeaderTemplateProperty);
        set => SetValue(HeaderTemplateProperty, value);
    }

    public IconSource? IconSource
    {
        get => GetValue(IconSourceProperty);
        set => SetValue(IconSourceProperty, value);
    }

    public bool IsClosable
    {
        get => GetValue(IsClosableProperty);
        set => SetValue(IsClosableProperty, value);
    }

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public Control? IconElement => _iconElement;

    public event EventHandler<TabBarCloseRequestedEventArgs> CloseRequested
    {
        add => AddHandler(CloseRequestedEvent, value);
        remove => RemoveHandler(CloseRequestedEvent, value);
    }

    static TabBarItem()
    {
        IconSourceProperty.Changed.AddClassHandler<TabBarItem>((x, e) => x.OnIconSourceChanged(e));
        IsClosableProperty.Changed.AddClassHandler<TabBarItem>((x, e) => {
            x.UpdatePseudoClasses();
            // 通知父级 TabBar，以便它可以重新评估关闭按钮可见性
            // 在所有标签页中（覆盖模式可能需要覆盖默认状态）。
            var parentTabBar = x.FindAncestorOfType<TabBar>();
            parentTabBar?.UpdateAllTabVisuals();
        });
    }

    private void OnIconSourceChanged(AvaloniaPropertyChangedEventArgs e)
    {
        var newSource = e.NewValue as IconSource;
        _iconElement = newSource?.CreateIconElement();
        UpdatePseudoClasses();
        UpdateIconDisplay();
    }

    public void RaiseCloseRequested()
    {
        if (!IsClosable) return;

        var args = new TabBarCloseRequestedEventArgs(DataContext ?? this, this);
        args.RoutedEvent = CloseRequestedEvent;
        RaiseEvent(args);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        // 左键单击时选中此标签页（SelectingItemsControl 不会自动选择自定义容器）
        if (IsEnabled && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) {
            var parentTabBar = this.FindAncestorOfType<TabBar>();
            if (parentTabBar != null) {
                int index = parentTabBar.IndexFromContainer(this);
                if (index >= 0)
                    parentTabBar.SelectedIndex = index;
            }
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (e.InitialPressMouseButton == MouseButton.Middle && IsClosable) {
            RaiseCloseRequested();
        }
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set("icon", _iconElement != null);
        // 基于 IsClosable 的初始 closecollapsed 状态。
        // 将由父级 TabBar 的 SetCloseButtonState() 根据覆盖模式覆盖。
        PseudoClasses.Set("closecollapsed", !IsClosable);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        // 在替换模板部件之前取消订阅旧的关闭按钮
        if (_closeButton != null && _closeButtonClickHandler != null)
            _closeButton.Click -= _closeButtonClickHandler;

        base.OnApplyTemplate(e);

        _iconPresenter = e.NameScope.Find<ContentPresenter>("PART_IconPresenter");
        _closeButton = e.NameScope.Find<Button>("PART_CloseButton");

        if (_closeButton != null) {
            _closeButtonClickHandler = (_, _) => RaiseCloseRequested();
            _closeButton.Click += _closeButtonClickHandler;
        }

        UpdateIconDisplay();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsSelectedProperty)
            PseudoClasses.Set(":selected", (bool)change.NewValue!);
        if (change.Property == HeaderProperty) {
            var headerStr = change.NewValue as string;
            _cachedCompactFallbackText = (headerStr != null && headerStr.Length > 0)
                ? headerStr.Substring(0, 1)
                : null;
            UpdateIconDisplay();
        }
    }

    private void UpdateIconDisplay()
    {
        if (_iconPresenter == null) return;

        if (_iconElement != null) {
            _iconPresenter.Content = _iconElement;
            _iconPresenter.IsVisible = true;
        }
        else if (PseudoClasses.Contains("compact") && _cachedCompactFallbackText != null) {
            // 回退：显示标题的第一个字符（缓存以避免每次布局遍历时分配）
            if (_compactFallbackTextBlock == null)
                _compactFallbackTextBlock = new TextBlock
                {
                    FontSize = 14,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                };
            _compactFallbackTextBlock.Text = _cachedCompactFallbackText;
            _iconPresenter.Content = _compactFallbackTextBlock;
            _iconPresenter.IsVisible = true;
        }
        else {
            _iconPresenter.IsVisible = false;
        }
    }

    private void OnTabContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        // 右键单击时选中此标签页（以便上下文菜单命令针对正确的标签页）
        if (IsEnabled) {
            var parentTabBar = this.FindAncestorOfType<TabBar>();
            if (parentTabBar != null) {
                int index = parentTabBar.IndexFromContainer(this);
                if (index >= 0)
                    parentTabBar.SelectedIndex = index;
            }
        }
    }

    internal void SetCompact(bool isCompact)
    {
        PseudoClasses.Set("compact", isCompact);
        UpdateIconDisplay();
    }

    /// <summary>
    /// 根据覆盖模式设置完整的关闭按钮视觉状态。
    /// 由父级 TabBar 调用以协调关闭按钮可见性。
    /// </summary>
    internal void SetCloseButtonState(bool closeCollapsed, bool closeAlways, bool closeOnHover)
    {
        PseudoClasses.Set("closecollapsed", closeCollapsed);
        PseudoClasses.Set("closealways", closeAlways);
        PseudoClasses.Set("closeoverlay", closeOnHover);

        _showCloseOnHover = closeOnHover;

        // 直接控制按钮可见性（最可靠的方法）。
        // 当 _showCloseOnHover 为 true 时，按钮保持隐藏，直到指针进入。
        if (_closeButton != null) {
            _closeButton.IsVisible = !closeCollapsed || closeAlways;
            // 修复：如果在指针已位于此项目上方时切换到悬停模式，
            // 立即显示关闭按钮（OnPointerEntered 不会重新触发）。
            if (closeOnHover && IsPointerOver)
                _closeButton.IsVisible = true;
        }
    }

    internal void SetSeparatorState(bool showSeparator)
    {
        PseudoClasses.Set(":separator", showSeparator);
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        if (_showCloseOnHover && _closeButton != null) {
            _closeButton.IsVisible = true;
        }
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (_showCloseOnHover && _closeButton != null) {
            _closeButton.IsVisible = false;
        }
    }

    internal bool HasPseudoClass(string name) => PseudoClasses.Contains(name);
}
