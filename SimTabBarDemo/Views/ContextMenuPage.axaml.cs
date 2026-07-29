using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SimTabBar.Controls;

namespace SimTabBarDemo.Views;

public partial class ContextMenuPage : UserControl
{
    public ContextMenuPage()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // 默认启用内置菜单
        var tabView = this.FindControl<TabBar>("ContextMenuSimTabBar");
        if (tabView != null)
        {
            tabView.IsTabContextMenuEnabled = true;
        }
    }

    private void OnDefaultMenuChecked(object? sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { IsChecked: false }) return;

        var tabView = this.FindControl<TabBar>("ContextMenuSimTabBar");
        if (tabView == null) return;

        tabView.IsTabContextMenuEnabled = true;
        tabView.TabItemContextMenu = null; // 使用内置默认菜单
        LogAction("切换为：内置默认菜单");
    }

    private void OnCustomMenuChecked(object? sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { IsChecked: false }) return;

        var tabView = this.FindControl<TabBar>("ContextMenuSimTabBar");
        if (tabView == null) return;

        tabView.IsTabContextMenuEnabled = true;
        tabView.TabItemContextMenu = BuildCustomContextMenu(tabView);
        LogAction("切换为：自定义菜单");
    }

    private void OnDisabledMenuChecked(object? sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { IsChecked: false }) return;

        var tabView = this.FindControl<TabBar>("ContextMenuSimTabBar");
        if (tabView == null) return;

        tabView.IsTabContextMenuEnabled = false;
        LogAction("切换为：禁用菜单");
    }

    private ContextMenu BuildCustomContextMenu(TabBar tabView)
    {
        return new ContextMenu
        {
            Items =
            {
                new MenuItem
                {
                    Header = "🔒 固定标签",
                    IsEnabled = true
                },
                new MenuItem
                {
                    Header = "✏ 重命名",
                    IsEnabled = true
                },
                new MenuItem
                {
                    Header = "📋 复制标签名称"
                },
                new Separator(),
                new MenuItem
                {
                    Header = "关闭",
                    Command = tabView.CloseTabCommand
                },
                new MenuItem
                {
                    Header = "关闭其他标签",
                    Command = tabView.CloseOtherTabsCommand
                },
                new MenuItem
                {
                    Header = "关闭所有标签",
                    Command = tabView.CloseAllTabsCommand
                }
            }
        };
    }

    private void OnTabCloseRequested(object? sender, TabBarCloseRequestedEventArgs e)
    {
        LogAction($"关闭请求: {e.Tab.Header}");
    }

    private void LogAction(string message)
    {
        var listBox = this.FindControl<ListBox>("ActionLogList");
        listBox?.Items?.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
    }
}
