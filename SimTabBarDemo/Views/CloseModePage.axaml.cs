using Avalonia.Controls;
using Avalonia.Interactivity;
using SimTabBar.Controls;

namespace SimTabBarDemo.Views;

public partial class CloseModePage : UserControl
{
    public CloseModePage()
    {
        InitializeComponent();
    }

    private void OnAutoChecked(object? sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { IsChecked: true })
            SetMode(TabBarCloseButtonOverlayMode.Auto);
    }

    private void OnPointerOverChecked(object? sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { IsChecked: true })
            SetMode(TabBarCloseButtonOverlayMode.OnPointerOver);
    }

    private void OnAlwaysChecked(object? sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { IsChecked: true })
            SetMode(TabBarCloseButtonOverlayMode.Always);
    }

    private void SetMode(TabBarCloseButtonOverlayMode mode)
    {
        var tabView = this.FindControl<TabBar>("CloseModeSimTabBar");
        if (tabView != null) tabView.CloseButtonOverlayMode = mode;
    }
}
