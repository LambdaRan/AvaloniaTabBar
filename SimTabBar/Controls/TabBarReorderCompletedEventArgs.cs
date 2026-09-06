using Avalonia.Interactivity;

namespace SimTabBar.Controls;

/// <summary>
/// 标签拖动排序完成后触发（集合已变更、选中已恢复、视觉已复位），
/// 应用可在此安全地做顺序持久化。OldIndex==NewIndex 时不会触发本事件。
/// </summary>
public class TabBarReorderCompletedEventArgs : RoutedEventArgs
{
    public object? Item { get; }
    public TabBarItem Tab { get; }
    public int OldIndex { get; }
    public int NewIndex { get; }

    public TabBarReorderCompletedEventArgs(object? item, TabBarItem tab, int oldIndex, int newIndex)
    {
        Item = item;
        Tab = tab;
        OldIndex = oldIndex;
        NewIndex = newIndex;
    }
}
