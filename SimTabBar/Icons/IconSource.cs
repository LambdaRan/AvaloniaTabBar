using Avalonia;
using Avalonia.Controls;

namespace SimTabBar.Icons;

public abstract class IconSource : AvaloniaObject
{
    public abstract Control CreateIconElement();
}
