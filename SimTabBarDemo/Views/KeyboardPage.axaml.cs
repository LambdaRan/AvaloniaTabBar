using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using SimTabBar.Controls;
using SimTabBarDemo.Helpers;

namespace SimTabBarDemo.Views;

public partial class KeyboardPage : UserControl
{
    private MruTabHistory? _mruHistory;
    private TabBar? _tabView;

    public KeyboardPage()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        var tabView = this.FindControl<TabBar>("KeyboardSimTabBar");
        _tabView = tabView;
        if (tabView != null)
        {
            // 禁用库的内置键盘处理，由本页面接管
            tabView.IsBuiltInKeyboardHandlingEnabled = false;

            // 初始化 MRU 历史管理器
            _mruHistory = new MruTabHistory(tabView);
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        _mruHistory?.Detach();
        _mruHistory = null;
        _tabView = null;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (_mruHistory != null)
        {
            if (e.Key == Key.Tab && e.KeyModifiers == KeyModifiers.Control)
            {
                var prev = _mruHistory.GetPreviousTab();
                if (prev != null) _tabView?.SelectTab(prev);
                LogAction($"Ctrl+Tab → MRU 切换到: {(prev as TabBarItem)?.Header ?? prev}");
                e.Handled = true;
                return;
            }
            else if (e.Key == Key.Tab && e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift))
            {
                _tabView?.SelectPreviousTab();
                LogAction($"Ctrl+Shift+Tab → 索引反向切换");
                e.Handled = true;
                return;
            }
            else if (e.Key == Key.F4 && e.KeyModifiers == KeyModifiers.Control)
            {
                // 保留 Ctrl+F4 关闭行为
                if (_tabView?.SelectedItem is TabBarItem tvi)
                {
                    tvi.RaiseCloseRequested();
                }
                else if (_tabView?.SelectedItem != null)
                {
                    var container = _tabView.ContainerFromItem(_tabView.SelectedItem) as TabBarItem;
                    container?.RaiseCloseRequested();
                }
                LogAction("Ctrl+F4 → 关闭当前标签");
                e.Handled = true;
                return;
            }
        }

        base.OnKeyDown(e);
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        LogAction($"选中变更: SelectedIndex = {_tabView?.SelectedIndex}");
    }

    private void OnTabCloseRequested(object? sender, TabBarCloseRequestedEventArgs e)
    {
        LogAction($"关闭请求: {e.Tab.Header}");
        // 关闭后清理 MRU 历史中的陈旧项
        _mruHistory?.Prune();
    }

    private void LogAction(string message)
    {
        var listBox = this.FindControl<ListBox>("ActionLogList");
        listBox?.Items?.Add(message);
    }
}
