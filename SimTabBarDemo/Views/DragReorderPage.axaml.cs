using System.Collections.ObjectModel;
using Avalonia.Controls;
using SimTabBar.Controls;

namespace SimTabBarDemo.Views;

public partial class DragReorderPage : UserControl
{
    public sealed class DemoDoc
    {
        public string Title { get; set; } = "";
    }

    private readonly ObservableCollection<DemoDoc> _docs = new()
    {
        new DemoDoc { Title = "文档 A" },
        new DemoDoc { Title = "文档 B" },
        new DemoDoc { Title = "文档 C" },
        new DemoDoc { Title = "文档 D" },
    };

    public DragReorderPage()
    {
        InitializeComponent();
        SourceTabBar.ItemsSource = _docs;
    }

    private void OnTabDragStarting(object? sender, TabBarDragStartingEventArgs e)
    {
        // 拦截演示：ItemsSource 栏的第 3 项
        if (sender == SourceTabBar && RejectThirdCheck.IsChecked == true
            && _docs.Count > 2 && ReferenceEquals(e.Item, _docs[2]))
        {
            e.Cancel = true;
            Log($"Starting → 已取消（拒拖 {TitleOf(e.Item)}）");
            return;
        }
        Log($"Starting：{TitleOf(e.Item)}");
    }

    private void OnTabReorderCompleted(object? sender, TabBarReorderCompletedEventArgs e)
        => Log($"Completed：{TitleOf(e.Item)}  {e.OldIndex} → {e.NewIndex}");

    private static string TitleOf(object? item) =>
        item switch
        {
            DemoDoc d => d.Title,
            TabBarItem t => t.Header?.ToString() ?? "?",
            _ => item?.ToString() ?? "null",
        };

    private void Log(string message)
    {
        EventLog.Items.Add(message);
        EventLog.ScrollIntoView(EventLog.Items.Count - 1);
    }
}
