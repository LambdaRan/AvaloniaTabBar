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
        var current = _tabView.SelectedItem;
        foreach (var item in _history)
        {
            if (item != current && _tabView.Items.Contains(item))
                return item;
        }
        return null;
    }

    /// <summary>
    /// 获取 MRU 历史中的下一个标签（反向遍历）。
    /// </summary>
    public object? GetNextTab()
    {
        var current = _tabView.SelectedItem;
        var found = false;
        foreach (var item in _history.Reverse())
        {
            if (item == current) { found = true; continue; }
            if (found && _tabView.Items.Contains(item))
                return item;
        }
        return null;
    }

    /// <summary>
    /// 清理历史中已不存在于 TabBar 的标签。
    /// 在批量关闭（Close Other / Close All）后主动调用。
    /// </summary>
    public void Prune()
    {
        foreach (var item in _history.Where(i => !_tabView.Items.Contains(i)).ToList())
            _history.Remove(item);
    }

    /// <summary>
    /// 解除与 TabBar 的事件绑定。
    /// </summary>
    public void Detach()
    {
        _tabView.SelectionChanged -= OnSelectionChanged;
        _tabView.NextTabOnClose = null;
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
        return _history.FirstOrDefault(i => _tabView.Items.Contains(i));
    }
}
