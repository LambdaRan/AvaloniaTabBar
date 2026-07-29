using System.Collections;
using Avalonia.Controls;
using Avalonia.VisualTree;
using System.Linq;
using SimTabBar.Controls;

using Xunit;
using Avalonia.Headless.XUnit;

namespace SimTabBar.Tests;

public class TabViewSeparatorTests
{
    [AvaloniaFact]
    public void SetSeparatorState_True_SetsSeparatorPseudoClass()
    {
        var tab = new TabBarItem { Header = "Test" };
        tab.SetSeparatorState(true);
        Assert.True(tab.HasPseudoClass(":separator"));
    }

    [AvaloniaFact]
    public void SetSeparatorState_False_RemovesSeparatorPseudoClass()
    {
        var tab = new TabBarItem { Header = "Test" };
        tab.SetSeparatorState(true);
        tab.SetSeparatorState(false);
        Assert.False(tab.HasPseudoClass(":separator"));
    }

    [AvaloniaFact]
    public void Separator_ExistsInTemplate()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(2);
        tabView.SelectedIndex = 0;

        var tab = tabView.ContainerFromIndex(1) as TabBarItem;
        Assert.NotNull(tab);

        // 在模板中查找分隔符边框
        var separator = tab!.GetVisualDescendants()
            .OfType<Border>()
            .FirstOrDefault(b => b.Name == "PART_Separator");

        Assert.NotNull(separator);

        window.Close();
    }

    [AvaloniaFact]
    public void Separator_VisibleOnUnselectedTabs_NotAdjacentToSelected()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(4);
        tabView.SelectedIndex = 1;

        var tab0 = tabView.ContainerFromIndex(0) as TabBarItem;
        var tab1 = tabView.ContainerFromIndex(1) as TabBarItem;
        var tab2 = tabView.ContainerFromIndex(2) as TabBarItem;
        var tab3 = tabView.ContainerFromIndex(3) as TabBarItem;

        Assert.False(tab0!.HasPseudoClass(":separator"), "Tab before selected should not have separator");
        Assert.False(tab1!.HasPseudoClass(":separator"), "Selected tab should not have separator");
        Assert.True(tab2!.HasPseudoClass(":separator"), "Tab after selected should have separator");
        Assert.True(tab3!.HasPseudoClass(":separator"), "Tab after selected should have separator");

        window.Close();
    }

    [AvaloniaFact]
    public void Separator_UpdatesWhenSelectionChanges()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(3);
        tabView.SelectedIndex = 0;

        var tab0 = tabView.ContainerFromIndex(0) as TabBarItem;
        var tab1 = tabView.ContainerFromIndex(1) as TabBarItem;
        var tab2 = tabView.ContainerFromIndex(2) as TabBarItem;

        Assert.False(tab0!.HasPseudoClass(":separator"));
        Assert.True(tab1!.HasPseudoClass(":separator"));
        Assert.True(tab2!.HasPseudoClass(":separator"));

        tabView.SelectedIndex = 2;

        Assert.True(tab0!.HasPseudoClass(":separator"), "Tab 0 should have separator after selection change");
        Assert.False(tab1!.HasPseudoClass(":separator"), "Tab 1 (before selected) should not have separator");
        Assert.False(tab2!.HasPseudoClass(":separator"), "Selected tab should not have separator");

        window.Close();
    }

    [AvaloniaFact]
    public void Separator_FirstTabSelected_NoTabBefore()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(3);
        tabView.SelectedIndex = 0;

        var tab0 = tabView.ContainerFromIndex(0) as TabBarItem;
        var tab1 = tabView.ContainerFromIndex(1) as TabBarItem;
        var tab2 = tabView.ContainerFromIndex(2) as TabBarItem;

        Assert.False(tab0!.HasPseudoClass(":separator"));
        Assert.True(tab1!.HasPseudoClass(":separator"));
        Assert.True(tab2!.HasPseudoClass(":separator"));

        window.Close();
    }

    [AvaloniaFact]
    public void Separator_UpdatesWhenTabAdded()
    {
        var tabView = new TabBar();
        ((IList)tabView.Items).Add(new TabBarItem { Header = "Tab 1" });
        ((IList)tabView.Items).Add(new TabBarItem { Header = "Tab 2" });

        var window = new Window { Width = 800, Height = 600, Content = tabView };
        window.Show();

        tabView.SelectedIndex = 0;

        ((IList)tabView.Items).Add(new TabBarItem { Header = "Tab 3" });
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();

        var tab0 = tabView.ContainerFromIndex(0) as TabBarItem;
        var tab1 = tabView.ContainerFromIndex(1) as TabBarItem;
        var tab2 = tabView.ContainerFromIndex(2) as TabBarItem;

        Assert.False(tab0!.HasPseudoClass(":separator"));
        Assert.True(tab1!.HasPseudoClass(":separator"));
        Assert.True(tab2!.HasPseudoClass(":separator"));

        window.Close();
    }

    [AvaloniaFact]
    public void Separator_Border_IsVisible_When_PseudoClass_Set()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(3);
        tabView.SelectedIndex = 0;

        // 标签页 1 的分隔符应可见（未选中，且不在选中标签页之前）
        var tab1 = tabView.ContainerFromIndex(1) as TabBarItem;
        Assert.NotNull(tab1);
        Assert.True(tab1!.HasPseudoClass(":separator"), "标签页 1 应具有分隔符伪类");

        // 在可视化树中查找分隔符边框
        var separator = tab1.GetVisualDescendants()
            .OfType<Border>()
            .FirstOrDefault(b => b.Name == "PART_Separator");

        Assert.NotNull(separator);
        Assert.True(separator!.IsVisible, "Separator border should be visible when pseudo-class is set");

        window.Close();
    }
}
