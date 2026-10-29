using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using SimTabBar.Controls;
using SimTabBar.Icons;
using Xunit;

namespace SimTabBar.Tests;

/// <summary>
/// HeaderSelector / IconSourceSelector 是代码侧的 AOT 友好取值车道：
/// 强类型委托在编译期静态引用数据成员，不依赖反射元数据。
/// XAML 侧对应 HeaderBinding / IconSourceBinding（见 HeaderBindingTests）。
/// </summary>
public class TabViewSelectorTests
{
    private static void Pump(Window window, double w = 800, double h = 600) =>
        TestHelper.Pump(window, w, h);

    [AvaloniaFact]
    public void HeaderSelector_DrivesHeader()
    {
        var items = new ObservableCollection<SelectorDoc>
        {
            new() { Title = "one" },
            new() { Title = "two" },
        };
        var tabView = new TabBar
        {
            ItemsSource = items,
            HeaderSelector = item => ((SelectorDoc)item).Title,
        };
        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();
        tabView.SelectedIndex = 0;
        Pump(window);

        Assert.Equal("one", ((TabBarItem)tabView.ContainerFromIndex(0)!).Header);
        Assert.Equal("two", ((TabBarItem)tabView.ContainerFromIndex(1)!).Header);
        window.Close();
    }

    [AvaloniaFact]
    public void HeaderSelector_TracksPropertyChanges()
    {
        var items = new ObservableCollection<SelectorDoc> { new() { Title = "one" } };
        var tabView = new TabBar
        {
            ItemsSource = items,
            HeaderSelector = item => ((SelectorDoc)item).Title,
        };
        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();
        tabView.SelectedIndex = 0;
        Pump(window);
        Assert.Equal("one", ((TabBarItem)tabView.ContainerFromIndex(0)!).Header);

        items[0].Title = "CHANGED";
        Pump(window);

        // 与 HeaderBinding 车道一致：数据项发出 PropertyChanged 后重新取值。
        Assert.Equal("CHANGED", ((TabBarItem)tabView.ContainerFromIndex(0)!).Header);
        window.Close();
    }

    [AvaloniaFact]
    public void HeaderSelector_RuntimeChange_RefreshesContainers()
    {
        var items = new ObservableCollection<SelectorDoc> { new() { Title = "one" } };
        var tabView = new TabBar { ItemsSource = items };
        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();
        tabView.SelectedIndex = 0;
        Pump(window);

        var container = (TabBarItem)tabView.ContainerFromIndex(0)!;
        Assert.Null(container.Header);

        tabView.HeaderSelector = item => "S:" + ((SelectorDoc)item).Title;
        Pump(window);

        Assert.Equal("S:one", container.Header);
        window.Close();
    }

    [AvaloniaFact]
    public void IconSourceSelector_DrivesIconSource()
    {
        var icon = new PathIconSource();
        var items = new ObservableCollection<SelectorDoc> { new() { Title = "one", Icon = icon } };
        var tabView = new TabBar
        {
            ItemsSource = items,
            IconSourceSelector = item => ((SelectorDoc)item).Icon,
        };
        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();
        tabView.SelectedIndex = 0;
        Pump(window);

        Assert.Same(icon, ((TabBarItem)tabView.ContainerFromIndex(0)!).IconSource);
        window.Close();
    }

    [AvaloniaFact]
    public void SelectorValue_DoesNotOverrideLocalHeader()
    {
        // Template 优先级低于本地值：使用方在容器上显式赋 Header 后，
        // 数据项变更不应把它冲掉。
        var items = new ObservableCollection<SelectorDoc> { new() { Title = "one" } };
        var tabView = new TabBar
        {
            ItemsSource = items,
            HeaderSelector = item => ((SelectorDoc)item).Title,
        };
        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();
        tabView.SelectedIndex = 0;
        Pump(window);

        var container = (TabBarItem)tabView.ContainerFromIndex(0)!;
        Assert.Equal("one", container.Header);

        container.Header = "local";
        items[0].Title = "CHANGED";
        Pump(window);

        Assert.Equal("local", container.Header);
        window.Close();
    }

    public class SelectorDoc : INotifyPropertyChanged
    {
        private string _title = "";

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
            }
        }

        public PathIconSource? Icon { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
