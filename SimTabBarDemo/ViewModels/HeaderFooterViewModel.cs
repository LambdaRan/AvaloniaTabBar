using CommunityToolkit.Mvvm.ComponentModel;

namespace SimTabBarDemo.ViewModels;

public partial class HeaderFooterViewModel : ObservableObject
{
    [ObservableProperty]
    private string _searchText = string.Empty;
}
