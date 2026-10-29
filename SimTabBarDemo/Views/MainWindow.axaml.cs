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
            new ContextMenuPage(),
            new DragReorderPage(),
            new ColorThemePage()
        };

        ShowScene(0);
    }

    private void OnSceneSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // XAML 里第一个 ListBoxItem 带 IsSelected="True"，populate 阶段就会触发
        // SelectionChanged —— 此时 _pages 还没赋值，Window 的 NameScope 也尚未挂上，
        // FindControl 会抛 "Could not find parent name scope"（Avalonia 12.1.4）。
        // 构造函数的 ShowScene(0) 会补上首屏。
        if (_pages is null) return;

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
