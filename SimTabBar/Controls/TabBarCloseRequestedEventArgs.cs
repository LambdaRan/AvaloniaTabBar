using Avalonia.Interactivity;

namespace SimTabBar.Controls;

public class TabBarCloseRequestedEventArgs : RoutedEventArgs
{
    public object Item { get; }
    public TabBarItem Tab { get; }
    public bool Cancel { get; set; }

    public TabBarCloseRequestedEventArgs(object item, TabBarItem tab)
    {
        Item = item;
        Tab = tab;
    }
}
