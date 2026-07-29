using Avalonia.Controls;
using SimTabBarDemo.ViewModels;

namespace SimTabBarDemo.Views;

public partial class BindingTabPage : UserControl
{
    public BindingTabPage()
    {
        InitializeComponent();
        DataContext = new BindingTabViewModel();
    }
}
