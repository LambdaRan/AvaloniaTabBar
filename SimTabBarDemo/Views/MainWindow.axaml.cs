using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;

namespace SimTabBarDemo.Views;

public partial class MainWindow : Window
{
    private readonly UserControl[] _pages;

    public MainWindow()
    {
        InitializeComponent();

        _pages = new UserControl[]
        {
            new BasicTabPage(),
            new BindingTabPage(),
            new CloseModePage(),
            new WidthModePage(),
            new HeaderFooterPage(),
            new KeyboardPage(),
            new PinnedTabPage(),
            new ContextMenuPage()
        };

        ShowScene(0);
    }

    private void OnSceneSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var listBox = sender as ListBox;
        if (listBox?.SelectedIndex >= 0)
        {
            ShowScene(listBox.SelectedIndex);
        }
    }

    private void ShowScene(int index)
    {
        var container = this.FindControl<Panel>("SceneContainer");
        if (container == null || index < 0 || index >= _pages.Length) return;

        container.Children.Clear();
        container.Children.Add(_pages[index]);
    }

    private void OnThemeToggleClick(object? sender, RoutedEventArgs e)
    {
        var app = Application.Current;
        if (app == null) return;

        app.RequestedThemeVariant = app.RequestedThemeVariant == ThemeVariant.Dark
            ? ThemeVariant.Light
            : ThemeVariant.Dark;
    }
}
