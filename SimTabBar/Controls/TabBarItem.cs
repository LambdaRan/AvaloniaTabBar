using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using SimTabBar.Icons;
using System.Globalization;

namespace SimTabBar.Controls;

[TemplatePart("PART_RootBorder", typeof(Border))]
[TemplatePart("PART_IconPresenter", typeof(ContentPresenter))]
[TemplatePart("PART_HeaderPresenter", typeof(ContentPresenter))]
[TemplatePart("PART_CloseButton", typeof(Button))]
[TemplatePart("PART_ActiveIndicator", typeof(Border))]
[TemplatePart("PART_Separator", typeof(Border))]
[PseudoClasses(PcSeparator, PcCompact, PcCloseCollapsed, PcCloseAlways, PcCloseOverlay, PcIcon)]
public class TabBarItem : ContentControl
{
    // 伪类名必须以冒号开头，否则样式选择器（如 ^:compact）永远匹配不上。
    internal const string PcSeparator = ":separator";
    internal const string PcCompact = ":compact";
    internal const string PcCloseCollapsed = ":closecollapsed";
    internal const string PcCloseAlways = ":closealways";
    internal const string PcCloseOverlay = ":closeoverlay";
    internal const string PcIcon = ":icon";

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

    /// <summary>
    /// 缓存的父级 TabBar。避免每次指针/属性事件都遍历可视化树。
    /// </summary>
    private TabBar? _parentTabBar;

    private IDisposable? _headerBinding;
    private IDisposable? _iconBinding;

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
        IsClosableProperty.Changed.AddClassHandler<TabBarItem>((x, _) => {
            x.UpdatePseudoClasses();
            // 通知父级 TabBar 重新评估所有标签页的关闭按钮可见性（覆盖模式
            // 可能需要覆盖默认状态）。走 ScheduleLayoutUpdate 以便批量设置
            // IsClosable 时合并为一次 O(n) 遍历，而不是每项一次。
            x._parentTabBar?.ScheduleLayoutUpdate();
        });
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _parentTabBar = this.FindAncestorOfType<TabBar>();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _parentTabBar = null;
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

        // Item 的语义由父级 TabBar 决定：ItemsSource 模式下是数据项，
        // 直接子项模式下是本容器自身。不要用 DataContext —— 在直接子项
        // 模式下它是从父级继承来的 ViewModel，所有标签页都是同一个对象。
        var item = (_parentTabBar ?? this.FindAncestorOfType<TabBar>())?.ResolveCloseItem(this)
                   ?? (object)this;

        var args = new TabBarCloseRequestedEventArgs(item, this);
        args.RoutedEvent = CloseRequestedEvent;
        RaiseEvent(args);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        // 左键单击时选中此标签页（SelectingItemsControl 不会自动选择自定义容器）
        if (IsEnabled && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) {
            var parentTabBar = _parentTabBar;
            if (parentTabBar != null) {
                int index = parentTabBar.IndexFromContainer(this);
                if (index >= 0)
                    parentTabBar.SelectedIndex = index;

                // 把键盘焦点移到 TabBar，否则 TabBar.OnKeyDown 收不到事件，
                // Ctrl+Tab / Ctrl+F4 等内置快捷键将完全失效。
                parentTabBar.Focus(NavigationMethod.Pointer);
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
        PseudoClasses.Set(PcIcon, _iconElement != null);
        // 基于 IsClosable 的初始 closecollapsed 状态。
        // 将由父级 TabBar 的 SetCloseButtonState() 根据覆盖模式覆盖。
        PseudoClasses.Set(PcCloseCollapsed, !IsClosable);
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
            // 新模板部件不知道当前的覆盖模式，恢复上次的可见性决定。
            _closeButton.IsVisible = !PseudoClasses.Contains(PcCloseCollapsed)
                                     || PseudoClasses.Contains(PcCloseAlways);
        }

        UpdateIconDisplay();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsSelectedProperty)
            PseudoClasses.Set(":selected", (bool)change.NewValue!);
        if (change.Property == HeaderProperty) {
            _cachedCompactFallbackText = FirstTextElement(change.NewValue);
            UpdateIconDisplay();
        }
    }

    /// <summary>
    /// 取 Header 的第一个字符作为紧凑模式下的图标回退。
    /// 按文本元素（而非 UTF-16 码元）切分，避免把 emoji 的代理对截断成乱码。
    /// </summary>
    private static string? FirstTextElement(object? header)
    {
        // 控件类型的 Header 无法有意义地取首字符（ToString 会得到类型名）。
        var text = header is Control ? null : header as string ?? header?.ToString();
        if (string.IsNullOrEmpty(text)) return null;

        var e = StringInfo.GetTextElementEnumerator(text);
        return e.MoveNext() ? (string)e.Current : null;
    }

    private void UpdateIconDisplay()
    {
        if (_iconPresenter == null) return;

        if (_iconElement != null) {
            _iconPresenter.Content = _iconElement;
            _iconPresenter.IsVisible = true;
        }
        else if (PseudoClasses.Contains(PcCompact) && _cachedCompactFallbackText != null) {
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
        if (IsEnabled && _parentTabBar != null) {
            int index = _parentTabBar.IndexFromContainer(this);
            if (index >= 0)
                _parentTabBar.SelectedIndex = index;
        }
    }

    internal void SetCompact(bool isCompact)
    {
        PseudoClasses.Set(PcCompact, isCompact);
        UpdateIconDisplay();
    }

    /// <summary>
    /// 根据覆盖模式设置完整的关闭按钮视觉状态。
    /// 由父级 TabBar 调用以协调关闭按钮可见性。
    /// </summary>
    internal void SetCloseButtonState(bool closeCollapsed, bool closeAlways, bool closeOnHover)
    {
        PseudoClasses.Set(PcCloseCollapsed, closeCollapsed);
        PseudoClasses.Set(PcCloseAlways, closeAlways);
        PseudoClasses.Set(PcCloseOverlay, closeOnHover);

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
        PseudoClasses.Set(PcSeparator, showSeparator);
    }

    /// <summary>
    /// 把 HeaderMemberPath / IconSourceMemberPath 绑定到数据项上。
    /// 用真正的绑定而非一次性反射取值，这样数据项的属性变更（INotifyPropertyChanged）、
    /// 嵌套路径（"A.B.C"）以及运行时修改路径都能生效。
    /// </summary>
    internal void ApplyMemberBindings(object item, string? headerPath, string? iconPath)
    {
        _headerBinding?.Dispose();
        _headerBinding = null;
        if (!string.IsNullOrEmpty(headerPath)) {
            _headerBinding = this.Bind(HeaderProperty, new Binding(headerPath)
            {
                Source = item,
                Mode = BindingMode.OneWay,
                // Template 优先级低于本地值与样式触发器，因此使用方仍可覆盖。
                Priority = BindingPriority.Template,
            });
        }

        _iconBinding?.Dispose();
        _iconBinding = null;
        if (!string.IsNullOrEmpty(iconPath)) {
            _iconBinding = this.Bind(IconSourceProperty, new Binding(iconPath)
            {
                Source = item,
                Mode = BindingMode.OneWay,
                Priority = BindingPriority.Template,
                // 与旧的 "value as IconSource" 行为一致：类型不符时静默取 null，
                // 而不是抛出绑定错误。
                Converter = AsIconSourceConverter.Instance,
            });
        }
    }

    internal void ClearMemberBindings()
    {
        _headerBinding?.Dispose();
        _headerBinding = null;
        _iconBinding?.Dispose();
        _iconBinding = null;
    }

    /// <summary>
    /// 容器被回收或移出 TabBar 时复位由 TabBar 施加的视觉状态。
    /// </summary>
    internal void ResetManagedVisualState()
    {
        Width = double.NaN;
        SetCompact(false);
        SetSeparatorState(false);
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

    private sealed class AsIconSourceConverter : IValueConverter
    {
        internal static readonly AsIconSourceConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value as IconSource;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
