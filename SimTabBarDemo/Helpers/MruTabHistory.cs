using Avalonia.Controls;
using SimTabBar.Controls;

namespace SimTabBarDemo.Helpers;

/// <summary>
/// MRU 标签历史管理器。
/// 维护一个按最近访问时间排序的标签栈。
/// </summary>
public class MruTabHistory
{
    private readonly LinkedList<object> _history = new();
    private readonly TabBar _tabView;

    /// <summary>
    /// 当前存在于 TabBar 中的项。每次查询前重建一次，把原本
    /// "每个历史项都 Items.Contains(O(n))" 的 O(n·m) 降到 O(n+m)。
    /// </summary>
    private readonly HashSet<object> _liveItems = new();

    public MruTabHistory(TabBar tabView)
    {
        _tabView = tabView;
        _tabView.SelectionChanged += OnSelectionChanged;
        _tabView.NextTabOnClose = OnTabClosing;
    }

    /// <summary>
    /// 获取 MRU 历史中的上一个标签（最近访问的，排除当前）。
    /// </summary>
    public object? GetPreviousTab()
    {
        RefreshLiveItems();
        var current = _tabView.SelectedItem;
        foreach (var item in _history)
        {
            if (item != current && _liveItems.Contains(item))
                return item;
        }
        return null;
    }

    /// <summary>
    /// 获取 MRU 历史中的下一个标签（反向遍历）。
    /// </summary>
    public object? GetNextTab()
    {
        RefreshLiveItems();
        var current = _tabView.SelectedItem;
        var found = false;
        // 直接沿链表反向走，避免 LINQ Reverse() 每次调用都物化一份缓冲。
        for (var node = _history.Last; node != null; node = node.Previous)
        {
            if (node.Value == current) { found = true; continue; }
            if (found && _liveItems.Contains(node.Value))
                return node.Value;
        }
        return null;
    }

    /// <summary>
    /// 清理历史中已不存在于 TabBar 的标签。
    /// 在批量关闭（Close Other / Close All）后主动调用。
    /// </summary>
    public void Prune()
    {
        RefreshLiveItems();
        // 原地删除节点，不分配中间列表。
        var node = _history.First;
        while (node != null)
        {
            var next = node.Next;
            if (!_liveItems.Contains(node.Value))
                _history.Remove(node);
            node = next;
        }
    }

    /// <summary>
    /// 解除与 TabBar 的事件绑定。
    /// </summary>
    public void Detach()
    {
        _tabView.SelectionChanged -= OnSelectionChanged;
        _tabView.NextTabOnClose = null;
    }

    private void RefreshLiveItems()
    {
        _liveItems.Clear();
        foreach (var item in _tabView.Items)
        {
            if (item != null)
                _liveItems.Add(item);
        }
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var selected = _tabView.SelectedItem;
        if (selected == null) return;

        // 移到栈顶（去重）
        _history.Remove(selected);
        _history.AddFirst(selected);

        // 清理已不存在的标签
        Prune();
    }

    private object? OnTabClosing(object? closingItem)
    {
        // 从历史中移除被关闭的标签
        if (closingItem != null)
            _history.Remove(closingItem);

        // 返回 MRU 历史中的上一个标签
        RefreshLiveItems();
        foreach (var item in _history)
        {
            if (_liveItems.Contains(item))
                return item;
        }
        return null;
    }
}
