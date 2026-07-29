using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace SimTabBar.Icons;

public class PathIconSource : IconSource
{
    public static readonly StyledProperty<Geometry?> DataProperty =
        AvaloniaProperty.Register<PathIconSource, Geometry?>(nameof(Data));

    public Geometry? Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    public override Control CreateIconElement()
    {
        return new Viewbox
        {
            Width = 16,
            Height = 16,
            Child = new Avalonia.Controls.Shapes.Path { Data = Data }
        };
    }
}
