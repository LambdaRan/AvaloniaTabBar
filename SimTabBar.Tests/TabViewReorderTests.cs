using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using SimTabBar.Controls;
using Xunit;

namespace SimTabBar.Tests;

public class TabViewReorderTests
{
    [AvaloniaFact]
    public void CanReorderTabs_DefaultFalse()
    {
        Assert.False(new TabBar().CanReorderTabs);
    }

    [AvaloniaFact]
    public void DragStartingEventArgs_ExposesItemTabCancel()
    {
        var tab = new TabBarItem();
        var args = new TabBarDragStartingEventArgs("item", tab);
        Assert.Equal("item", args.Item);
        Assert.Same(tab, args.Tab);
        Assert.False(args.Cancel);
        args.Cancel = true;
        Assert.True(args.Cancel);
    }

    [AvaloniaFact]
    public void ReorderCompletedEventArgs_ExposesIndices()
    {
        var tab = new TabBarItem();
        var args = new TabBarReorderCompletedEventArgs("item", tab, 2, 0);
        Assert.Equal("item", args.Item);
        Assert.Same(tab, args.Tab);
        Assert.Equal(2, args.OldIndex);
        Assert.Equal(0, args.NewIndex);
    }
}
