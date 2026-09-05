using System.Collections;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using SimTabBar.Controls;
using Xunit;

namespace SimTabBar.Tests;

/// <summary>
/// 「选中初值推入时机」契约测试：
///
/// 1. 实体化前推入（集合非空）→ 必须落地。回归防护：曾在 PrepareContainerForItemOverride
///    本地预置 IsSelected，触发基类 pull 分支 toggle 反选，初值被清成 -1。
///
/// 2. 集合为空时推入 → 丢弃。这是 Avalonia 框架语义（ListBox/TabControl 基线一致），
///    使用方应在集合非空后再设初值。固化之，防止未来"半吊子恢复"破坏可预期性。
/// </summary>
public class TabViewInitialSelectionTests
{
    private static void Pump(Window window, double w = 800, double h = 600)
    {
        Dispatcher.UIThread.RunJobs();
        window.Measure(new Size(w, h));
        window.Arrange(new Rect(0, 0, w, h));
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    private static void AssertTabSelected(TabBar bar, int expectedIndex, object? expectedItem = null)
    {
        Assert.Equal(expectedIndex, bar.SelectedIndex);
        if (expectedItem != null)
            Assert.Same(expectedItem, bar.SelectedItem);
        for (int i = 0; i < bar.ItemCount; i++)
        {
            var container = (TabBarItem)bar.ContainerFromIndex(i)!;
            bool want = i == expectedIndex;
            Assert.True(container.IsSelected == want,
                $"container[{i}].IsSelected should be {want}, actual {container.IsSelected} (SelectedIndex={bar.SelectedIndex})");
            Assert.True(container.HasPseudoClass(":selected") == want,
                $"container[{i}] :selected pseudo should be {want}");
        }
    }

    public class Item
    {
        public string Title { get; set; } = "";
    }

    // ------------------------------------------------ 契约一：实体化前初值必须落地

    [AvaloniaFact]
    public void ItemsPresent_SelectedIndexSetBeforeShow_Lands()
    {
        var items = new ObservableCollection<Item> { new(), new(), new() };
        var bar = new TabBar { ItemsSource = items, SelectedIndex = 1 };
        var window = new Window { Width = 800, Height = 600, Content = bar };
        window.Show();
        Pump(window);
        AssertTabSelected(bar, 1);
        window.Close();
    }

    [AvaloniaFact]
    public void ItemsPresent_SelectedItemSetBeforeShow_Lands()
    {
        var items = new ObservableCollection<Item> { new(), new(), new() };
        var bar = new TabBar { ItemsSource = items, SelectedItem = items[2] };
        var window = new Window { Width = 800, Height = 600, Content = bar };
        window.Show();
        Pump(window);
        AssertTabSelected(bar, 2, items[2]);
        window.Close();
    }

    [AvaloniaFact]
    public void DirectChildren_SelectedIndexSetBeforeShow_Lands()
    {
        var bar = new TabBar();
        for (int i = 0; i < 3; i++)
            ((IList)bar.Items).Add(new TabBarItem { Header = $"T{i}" });
        bar.SelectedIndex = 1;
        var window = new Window { Width = 800, Height = 600, Content = bar };
        window.Show();
        Pump(window);
        AssertTabSelected(bar, 1);
        window.Close();
    }

    [AvaloniaFact]
    public void ItemsPresent_SelectedIndexSetAfterRealization_Lands()
    {
        var items = new ObservableCollection<Item> { new(), new(), new() };
        var bar = new TabBar { ItemsSource = items };
        var window = new Window { Width = 800, Height = 600, Content = bar };
        window.Show();
        Pump(window);           // 完全实体化后再改
        bar.SelectedIndex = 1;
        Pump(window);
        AssertTabSelected(bar, 1);
        window.Close();
    }

    /// <summary>复刻 BindingTabPage（ItemsSource + SelectedItem 双向绑定、集合初始非空）。</summary>
    [AvaloniaFact]
    public void BindingMode_ItemsPresent_InitialSelectedItem_Lands()
    {
        var vm = new BindingVm();
        var bar = new TabBar
        {
            ItemsSource = vm.Documents,
            DataContext = vm,
        };
        bar.Bind(SelectingItemsControl.SelectedItemProperty, new Avalonia.Data.Binding(nameof(BindingVm.SelectedDocument))
        {
            Source = vm,
            Mode = Avalonia.Data.BindingMode.TwoWay,
        });
        // 绑定初值在实体化（Show）之前就已推入
        var window = new Window { Width = 800, Height = 600, Content = bar };
        window.Show();
        Pump(window);
        AssertTabSelected(bar, 2, vm.Documents[2]);
        window.Close();
    }

    public class BindingVm
    {
        public ObservableCollection<Item> Documents { get; } =
            new() { new Item { Title = "a" }, new Item { Title = "b" }, new Item { Title = "c" } };
        public Item? SelectedDocument { get; set; }
        public BindingVm() => SelectedDocument = Documents[2];
    }

    // ----------------------------- 契约二：空集合推入不落地 = 框架语义（固化）

    [AvaloniaFact]
    public void SelectedIndexPushedWhileEmpty_IsDropped_EvenAfterFillBeforeShow()
    {
        // ListBox 基线一致：空集合推入当场钳成 -1，集合到达后不恢复。
        var items = new ObservableCollection<Item>();
        var bar = new TabBar { ItemsSource = items, SelectedIndex = 1 };
        items.Add(new Item());
        items.Add(new Item());
        items.Add(new Item());
        var window = new Window { Width = 800, Height = 600, Content = bar };
        window.Show();
        Pump(window);
        AssertTabSelected(bar, -1);
        window.Close();
    }

    [AvaloniaFact]
    public void SelectedItemPushedWhileEmpty_IsDropped_EvenAfterFill()
    {
        var items = new ObservableCollection<Item>();
        var target = new Item();
        var bar = new TabBar { ItemsSource = items, SelectedItem = target };
        items.Add(new Item());
        items.Add(target);      // 目标出现在索引 1 —— 框架也不会回头恢复它
        var window = new Window { Width = 800, Height = 600, Content = bar };
        window.Show();
        Pump(window);
        AssertTabSelected(bar, -1);
        Assert.Null(bar.SelectedItem);
        window.Close();
    }

    [AvaloniaFact]
    public void PushedWhileEmpty_FilledAfterRealization_StaysUnselected()
    {
        var items = new ObservableCollection<Item>();
        var bar = new TabBar { ItemsSource = items, SelectedIndex = 1 };
        var window = new Window { Width = 800, Height = 600, Content = bar };
        window.Show();
        Pump(window);           // 以空集合实体化
        items.Add(new Item());
        items.Add(new Item());
        items.Add(new Item());
        Pump(window);
        AssertTabSelected(bar, -1);
        window.Close();
    }
}
