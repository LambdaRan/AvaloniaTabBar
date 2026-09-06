using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.VisualTree;
using SimTabBar.Controls;

using Xunit;
using Avalonia.Headless.XUnit;

namespace SimTabBar.Tests;

/// <summary>
/// 测试用数据项。ToString() 刻意返回一个不可能与 Title 混淆的值，
/// 用于钉住「Compact 图标回退取 Header 字符串而非 item.ToString()」这条不变量。
/// </summary>
public class SessionLikeItem : INotifyPropertyChanged
{
    private string _title = "";
    private bool _isActive;

    public string Title
    {
        get => _title;
        set { _title = value; OnChanged(nameof(Title)); }
    }

    public bool IsActive
    {
        get => _isActive;
        set { _isActive = value; OnChanged(nameof(IsActive)); }
    }

    public override string ToString() => "TOSTRING-" + _title;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class TabViewHeaderTemplateTests
{
    /// <summary>
    /// 绿点 + 标题的 header 模板，即 Armpi 场景的库内等价物。
    /// 命名 Dot / HeaderText 以便测试按名定位。
    /// </summary>
    private static readonly IDataTemplate DotTemplate = new FuncDataTemplate<SessionLikeItem>((_, _) =>
    {
        // lambda 构造的 DataTemplate 写不了 XAML 绑定语法，故显式挂 Binding。
        // 绑定不带 Source → 解析到模板根的 DataContext，也就是 presenter.Content，
        // 即数据项本身（这正是本特性要保证的语义）。
        var dot = new Ellipse
        {
            Name = "Dot",
            Width = 8,
            Height = 8,
            Fill = new SolidColorBrush(Color.Parse("#4CAF50")),
            VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
        };
        dot.Bind(Ellipse.IsVisibleProperty,
            new Avalonia.Data.Binding(nameof(SessionLikeItem.IsActive)));

        var text = new TextBlock
        {
            Name = "HeaderText",
            VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
        };
        text.Bind(TextBlock.TextProperty,
            new Avalonia.Data.Binding(nameof(SessionLikeItem.Title)));

        return new StackPanel
        {
            Orientation = global::Avalonia.Layout.Orientation.Horizontal,
            Spacing = 6,
            Children = { dot, text },
        };
    });

    /// <summary>
    /// ItemsSource 模式的 TabBar。header 车道的语义全在这个模式下，
    /// TestHelper.CreateTabBarWithTabs 造的是直接子项模式，用不上。
    /// </summary>
    private static (TabBar tabView, Window window, ObservableCollection<SessionLikeItem> items)
        CreateItemsSourceTabBar(params SessionLikeItem[] seed)
    {
        var items = new ObservableCollection<SessionLikeItem>(seed);
        var tabView = new TabBar
        {
            HeaderMemberPath = nameof(SessionLikeItem.Title),
            ItemsSource = items,
        };
        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();
        return (tabView, window, items);
    }

    private static ContentPresenter HeaderPresenterOf(TabBarItem item) =>
        TestHelper.Part<ContentPresenter>(item, "PART_HeaderPresenter");

    // ---------------------------------------------------------- 字符串车道（回归）

    [AvaloniaFact]
    public void Default_ItemHeaderTemplateIsNull()
    {
        // TabBar.HeaderTemplate 的默认值断言在 Task 2 —— 该属性此时还不存在。
        Assert.Null(new TabBarItem().HeaderTemplate);
    }

    [AvaloniaFact]
    public void NoTemplate_StringLane_RendersHeaderText()
    {
        // 回归钉：主题不再绑 Content="{TemplateBinding Header}" 之后，
        // 字符串车道改由 UpdateHeaderDisplay() 驱动，行为必须与旧版一致。
        var item = new SessionLikeItem { Title = "会话A" };
        var (tabView, window, _) = CreateItemsSourceTabBar(item);
        tabView.SelectedIndex = 0;
        TestHelper.Pump(window);

        var container = (TabBarItem)tabView.ContainerFromIndex(0)!;
        Assert.Equal("会话A", container.Header);

        var presenter = HeaderPresenterOf(container);
        // 无模板时 content 就是 Header 字符串；前提 1 的 `!(content is Control)`
        // 分支同样会把 DataContext 设成 content，故此处 DataContext 是字符串。
        Assert.Equal("会话A", presenter.DataContext as string);
        // ContentPresenter 对 string 内容自动生成的那个 TextBlock 就是 Child
        Assert.Equal("会话A", Assert.IsType<TextBlock>(presenter.Child).Text);

        window.Close();
    }

    // ---------------------------------------------------------- 模板车道（核心语义）

    [AvaloniaFact]
    public void Template_DataContextIsItem_NotHeaderString()
    {
        // 核心语义钉。前提 1：ContentTemplate 非空 → presenter.DataContext = presenter.Content。
        // 若 content 车道没被接管，这里会拿到 "会话A" 这个字符串，
        // 模板内的 {Binding IsActive} / {Binding Title} 全部失效。
        var item = new SessionLikeItem { Title = "会话A", IsActive = true };
        var (tabView, window, _) = CreateItemsSourceTabBar(item);
        tabView.SelectedIndex = 0;
        TestHelper.Pump(window);

        var container = (TabBarItem)tabView.ContainerFromIndex(0)!;
        Assert.Equal("会话A", container.Header);   // HeaderMemberPath 仍然生效

        container.HeaderTemplate = DotTemplate;    // 本任务直接在容器上设，TabBar 车道是 Task 2
        TestHelper.Pump(window);

        var presenter = HeaderPresenterOf(container);
        Assert.Same(item, presenter.DataContext);
        Assert.IsNotType<string>(presenter.DataContext);

        // 模板内绑定真的解析到了数据项成员
        var text = presenter.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Name == "HeaderText");
        Assert.Equal("会话A", text.Text);

        window.Close();
    }

    [AvaloniaFact]
    public void Template_BindingFollowsItemPropertyChanged()
    {
        var item = new SessionLikeItem { Title = "会话A", IsActive = false };
        var (tabView, window, _) = CreateItemsSourceTabBar(item);
        tabView.SelectedIndex = 0;
        TestHelper.Pump(window);

        var container = (TabBarItem)tabView.ContainerFromIndex(0)!;
        container.HeaderTemplate = DotTemplate;
        TestHelper.Pump(window);

        var dot = HeaderPresenterOf(container)
            .GetVisualDescendants().OfType<Ellipse>().Single(e => e.Name == "Dot");
        Assert.False(dot.IsVisible);

        item.IsActive = true;      // 绿点亮起 —— Armpi 的 attach 状态翻转
        TestHelper.Pump(window);

        Assert.True(dot.IsVisible);
        window.Close();
    }

    // ---------------------------------------------------------- TabBar 层同步链路

    [AvaloniaFact]
    public void TabBarHeaderTemplate_DefaultIsNull()
    {
        Assert.Null(new TabBar().HeaderTemplate);
    }

    [AvaloniaFact]
    public void TabBarHeaderTemplate_PushedToAllRealizedContainers()
    {
        var (tabView, window, _) = CreateItemsSourceTabBar(
            new SessionLikeItem { Title = "A" },
            new SessionLikeItem { Title = "B" },
            new SessionLikeItem { Title = "C" });
        tabView.SelectedIndex = 0;
        TestHelper.Pump(window);

        tabView.HeaderTemplate = DotTemplate;
        TestHelper.Pump(window);

        for (int i = 0; i < 3; i++) {
            var container = (TabBarItem)tabView.ContainerFromIndex(i)!;
            Assert.Same(DotTemplate, container.HeaderTemplate);
            // 模板上下文是各自的数据项，不是 Header 字符串
            Assert.IsType<SessionLikeItem>(HeaderPresenterOf(container).DataContext);
        }
        window.Close();
    }

    [AvaloniaFact]
    public void TabBarHeaderTemplate_SetToNullAtRuntime_FallsBackToStringLane()
    {
        var item = new SessionLikeItem { Title = "会话A" };
        var (tabView, window, _) = CreateItemsSourceTabBar(item);
        tabView.HeaderTemplate = DotTemplate;
        tabView.SelectedIndex = 0;
        TestHelper.Pump(window);

        var container = (TabBarItem)tabView.ContainerFromIndex(0)!;
        Assert.Same(item, HeaderPresenterOf(container).DataContext);

        tabView.HeaderTemplate = null;
        TestHelper.Pump(window);

        Assert.Null(container.HeaderTemplate);
        // 回落字符串车道：content 是 Header 字符串
        var presenter = HeaderPresenterOf(container);
        Assert.Equal("会话A", presenter.DataContext as string);
        Assert.Equal("会话A", Assert.IsType<TextBlock>(presenter.Child).Text);
        window.Close();
    }

    [AvaloniaFact]
    public void HeaderTemplate_AppliedToContainerRealizedAfterTemplateSet()
    {
        // 实体化推值路径（PrepareContainerForItemOverride），区别于上面的运行期刷新路径
        var (tabView, window, items) = CreateItemsSourceTabBar(new SessionLikeItem { Title = "A" });
        tabView.HeaderTemplate = DotTemplate;
        tabView.SelectedIndex = 0;
        TestHelper.Pump(window);

        items.Add(new SessionLikeItem { Title = "B", IsActive = true });
        TestHelper.Pump(window);

        var container = (TabBarItem)tabView.ContainerFromIndex(1)!;
        Assert.Same(DotTemplate, container.HeaderTemplate);
        Assert.Same(items[1], HeaderPresenterOf(container).DataContext);
        Assert.True(HeaderPresenterOf(container)
            .GetVisualDescendants().OfType<Ellipse>().Single(e => e.Name == "Dot").IsVisible);
        window.Close();
    }

    [AvaloniaFact]
    public void HeaderTemplate_CoexistsWithHeaderMemberPath()
    {
        // 两条车道并存不冲突：HeaderMemberPath 仍驱动 Header 字符串，
        // HeaderTemplate 独立决定视觉。
        var item = new SessionLikeItem { Title = "会话A" };
        var (tabView, window, _) = CreateItemsSourceTabBar(item);
        tabView.HeaderTemplate = DotTemplate;
        tabView.SelectedIndex = 0;
        TestHelper.Pump(window);

        var container = (TabBarItem)tabView.ContainerFromIndex(0)!;
        Assert.Equal("会话A", container.Header);

        item.Title = "改名了";      // HeaderMemberPath 的绑定必须还活着
        TestHelper.Pump(window);

        Assert.Equal("改名了", container.Header);
        Assert.Equal("改名了", HeaderPresenterOf(container)
            .GetVisualDescendants().OfType<TextBlock>().Single(t => t.Name == "HeaderText").Text);
        window.Close();
    }

    [AvaloniaFact]
    public void HeaderTemplate_ContainerReuse_RebindsToNewItem()
    {
        var a = new SessionLikeItem { Title = "A" };
        var b = new SessionLikeItem { Title = "B" };
        var (tabView, window, items) = CreateItemsSourceTabBar(a, b);
        tabView.HeaderTemplate = DotTemplate;
        tabView.SelectedIndex = 0;
        TestHelper.Pump(window);

        var container = (TabBarItem)tabView.ContainerFromIndex(0)!;
        Assert.Same(a, HeaderPresenterOf(container).DataContext);

        items.RemoveAt(0);          // a 移除，容器被回收给 b
        TestHelper.Pump(window);

        var reused = (TabBarItem)tabView.ContainerFromIndex(0)!;
        Assert.Same(b, HeaderPresenterOf(reused).DataContext);
        Assert.Equal("B", HeaderPresenterOf(reused)
            .GetVisualDescendants().OfType<TextBlock>().Single(t => t.Name == "HeaderText").Text);
        window.Close();
    }

    [AvaloniaFact]
    public void HeaderTemplate_CompactFallbackStillUsesHeaderString()
    {
        // 钉住关键不变量：模板车道不污染 Compact 模式的图标回退。
        // FirstTextElement(Header) 取的是 "会话A" 的首字符 "会"，
        // 而不是 item.ToString() 的首字符 "T"（SessionLikeItem.ToString 刻意返回 TOSTRING-前缀）。
        var item = new SessionLikeItem { Title = "会话A" };
        var (tabView, window, _) = CreateItemsSourceTabBar(item);
        tabView.HeaderTemplate = DotTemplate;
        tabView.TabWidthMode = TabBarWidthMode.Compact;
        TestHelper.Pump(window);

        tabView.SelectedIndex = 1;  // 让索引 0 落入未选中 → 收缩成紧凑态
        TestHelper.Pump(window);

        var container = (TabBarItem)tabView.ContainerFromIndex(0)!;
        Assert.True(container.HasPseudoClass(":compact"));
        Assert.Equal("会话A", container.Header);

        var fallback = TestHelper.Part<ContentPresenter>(container, "PART_IconPresenter")
            .GetVisualDescendants().OfType<TextBlock>().Single();
        Assert.Equal("会", fallback.Text);
        window.Close();
    }
}
