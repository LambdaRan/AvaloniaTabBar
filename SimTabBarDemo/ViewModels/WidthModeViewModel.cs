using CommunityToolkit.Mvvm.ComponentModel;
using SimTabBar.Controls;

namespace SimTabBarDemo.ViewModels;

public partial class WidthModeViewModel : ObservableObject
{
    [ObservableProperty]
    private TabBarWidthMode _widthMode = TabBarWidthMode.Equal;
}
