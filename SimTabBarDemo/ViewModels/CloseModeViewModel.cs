using CommunityToolkit.Mvvm.ComponentModel;
using SimTabBar.Controls;

namespace SimTabBarDemo.ViewModels;

public partial class CloseModeViewModel : ObservableObject
{
    [ObservableProperty]
    private TabBarCloseButtonOverlayMode _overlayMode = TabBarCloseButtonOverlayMode.Auto;
}
