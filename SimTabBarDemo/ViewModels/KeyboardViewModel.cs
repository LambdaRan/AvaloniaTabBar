using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SimTabBarDemo.ViewModels;

public partial class KeyboardViewModel : ObservableObject
{
    public ObservableCollection<string> ActionLog { get; } = new();

    public void LogAction(string action)
    {
        ActionLog.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {action}");
        if (ActionLog.Count > 50) ActionLog.RemoveAt(50);
    }
}
