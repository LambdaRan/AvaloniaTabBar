using System.Collections;
using Avalonia.Controls;
using SimTabBar.Controls;

namespace SimTabBarDemo.Views;

public partial class BasicTabPage : UserControl
{
    public BasicTabPage()
    {
        InitializeComponent();
    }

    private void OnAddTabButtonClick(object? sender, EventArgs e)
    {
        var tabView = this.FindControl<TabBar>("BasicSimTabBar");
        if (tabView == null) return;

        var newTab = new TabBarItem
        {
            Header = $"新标签 {tabView.ItemCount + 1}",
            Content = new TextBlock { Text = $"新标签内容 {tabView.ItemCount + 1}", Margin = new Avalonia.Thickness(16) }
        };
        ((IList)tabView.Items).Add(newTab);
        tabView.SelectedItem = newTab;
    }
}
