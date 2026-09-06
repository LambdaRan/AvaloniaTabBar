using Avalonia.Interactivity;

namespace SimTabBar.Controls;

/// <summary>
/// 标签拖动排序即将开始时触发。设置 Cancel=true 可阻止本次拖动
/// （例如应用判定该页签不允许重排）。Item 的语义与
/// <see cref="TabBarCloseRequestedEventArgs.Item"/> 一致：
/// ItemsSource 模式下是数据项，直接子项模式下是容器自身。
/// </summary>
public class TabBarDragStartingEventArgs : RoutedEventArgs
{
    public object? Item { get; }
    public TabBarItem Tab { get; }
    public bool Cancel { get; set; }

    public TabBarDragStartingEventArgs(object? item, TabBarItem tab)
    {
        Item = item;
        Tab = tab;
    }
}
