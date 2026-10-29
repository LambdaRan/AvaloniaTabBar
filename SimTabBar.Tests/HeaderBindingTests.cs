using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using SimTabBar.Controls;
using Xunit;

namespace SimTabBar.Tests;

/// <summary>
/// HeaderBinding / IconSourceBinding：对标 Avalonia 自带的
/// <c>ItemsControl.DisplayMemberBinding</c>（BindingBase 而不是字符串路径）。
/// XAML 侧由 [AssignBinding] + [InheritDataTypeFromItems] 编译成 compiled binding，
/// 这里同时验证 XAML 形态（HeaderBindingProbe）和 C# 形态（CompiledBinding.Create）。
/// </summary>
public class HeaderBindingTests
{
    private static void Pump(Window window, double w = 800, double h = 600) =>
        TestHelper.Pump(window, w, h);

    [AvaloniaFact]
    public void XamlHeaderBinding_IsCompiled_AndDrivesHeader()
    {
        var vm = new ProbeViewModel
        {
            Items = new ObservableCollection<ProbeItem>
            {
                new() { Title = "one" },
                new() { Title = "two" },
            },
        };
        var probe = new HeaderBindingProbe { DataContext = vm };
        var window = new Window { Width = 800, Height = 600, Content = probe };
        window.Show();

        var tabView = probe.FindControl<TabBar>("ProbeTabBar")!;
        Assert.NotNull(tabView.HeaderBinding);
        Assert.IsType<CompiledBinding>(tabView.HeaderBinding);
        tabView.SelectedIndex = 0;
        Pump(window);

        Assert.Equal("one", ((TabBarItem)tabView.ContainerFromIndex(0)!).Header);
        Assert.Equal("two", ((TabBarItem)tabView.ContainerFromIndex(1)!).Header);

        // compiled binding 同样跟随 INPC。
        vm.Items[0].Title = "CHANGED";
        Pump(window);
        Assert.Equal("CHANGED", ((TabBarItem)tabView.ContainerFromIndex(0)!).Header);
        window.Close();
    }

    [AvaloniaFact]
    public void HeaderBinding_FromCompiledBindingApi_DrivesHeader()
    {
        var items = new ObservableCollection<ProbeItem> { new() { Title = "one" } };
        var tabView = new TabBar
        {
            ItemsSource = items,
            HeaderBinding = CompiledBinding.Create<ProbeItem, string?>(item => item.Title),
        };
        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();
        tabView.SelectedIndex = 0;
        Pump(window);

        Assert.Equal("one", ((TabBarItem)tabView.ContainerFromIndex(0)!).Header);
        window.Close();
    }

    [AvaloniaFact]
    public void HeaderBinding_SupportsNestedPath_AndTracksChanges()
    {
        var inner = new ProbeInner { Name = "deep" };
        var item = new ProbeItem { Title = "one", Inner = inner };
        var tabView = new TabBar
        {
            ItemsSource = new ObservableCollection<ProbeItem> { item },
            HeaderBinding = CompiledBinding.Create<ProbeItem, string?>(i => i.Inner!.Name),
        };
        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();
        tabView.SelectedIndex = 0;
        Pump(window);

        var container = (TabBarItem)tabView.ContainerFromIndex(0)!;
        Assert.Equal("deep", container.Header);

        inner.Name = "changed";
        Pump(window);
        Assert.Equal("changed", container.Header);

        item.Inner = new ProbeInner { Name = "replaced" };
        Pump(window);
        Assert.Equal("replaced", container.Header);
        window.Close();
    }

    [AvaloniaFact]
    public void HeaderBinding_TakesPrecedenceOverSelector()
    {
        var items = new ObservableCollection<ProbeItem> { new() { Title = "one" } };
        var tabView = new TabBar
        {
            ItemsSource = items,
            HeaderSelector = item => "S:" + ((ProbeItem)item).Title,
            HeaderBinding = CompiledBinding.Create<ProbeItem, string?>(item => item.Alternate),
        };
        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();
        tabView.SelectedIndex = 0;
        Pump(window);

        Assert.Equal("B:one", ((TabBarItem)tabView.ContainerFromIndex(0)!).Header);
        window.Close();
    }

}

/// <summary>XAML 探针用的数据项（必须是顶层 public 类型，XAML 无法解析嵌套类型）。</summary>
public class ProbeItem : INotifyPropertyChanged
{
    private string? _title;
    private ProbeInner? _inner;

    public string? Title
    {
        get => _title;
        set
        {
            _title = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Alternate)));
        }
    }

    /// <summary>给「绑定优先级」测试用的前缀值（compiled binding 只支持属性访问）。</summary>
    public string? Alternate => "B:" + _title;

    public ProbeInner? Inner
    {
        get => _inner;
        set
        {
            _inner = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Inner)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public class ProbeInner : INotifyPropertyChanged
{
    private string? _name;

    public string? Name
    {
        get => _name;
        set
        {
            _name = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public class ProbeViewModel
{
    public ObservableCollection<ProbeItem> Items { get; set; } = new();
}
