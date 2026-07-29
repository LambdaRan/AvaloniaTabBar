using Avalonia.Controls;
using SimTabBar.Controls;

namespace SimTabBarDemo.Views;

public partial class PinnedTabPage : UserControl
{
    public PinnedTabPage()
    {
        InitializeComponent();
    }

    private void OnTabCloseRequested(object? sender, TabBarCloseRequestedEventArgs e)
    {
        // 演示：取消关闭标题中包含 "⚠" 的标签
        if (e.Tab.Header is string header && header.Contains('⚠'))
        {
            e.Cancel = true;
            // 在实际应用中，此处应显示保存对话框
        }
    }
}
