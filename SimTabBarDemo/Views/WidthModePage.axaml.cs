using Avalonia.Controls;
using Avalonia.Interactivity;
using SimTabBar.Controls;

namespace SimTabBarDemo.Views;

public partial class WidthModePage : UserControl
{
    public WidthModePage()
    {
        InitializeComponent();
    }

    private void OnEqualChecked(object? sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { IsChecked: true })
            SetMode(TabBarWidthMode.Equal);
    }

    private void OnSizeToContentChecked(object? sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { IsChecked: true })
            SetMode(TabBarWidthMode.SizeToContent);
    }

    private void OnCompactChecked(object? sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { IsChecked: true })
            SetMode(TabBarWidthMode.Compact);
    }

    private void SetMode(TabBarWidthMode mode)
    {
        var tabView = this.FindControl<TabBar>("WidthModeSimTabBar");
        if (tabView != null) tabView.TabWidthMode = mode;
    }
}
