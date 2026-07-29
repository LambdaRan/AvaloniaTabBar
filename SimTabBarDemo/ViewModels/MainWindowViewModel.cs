using CommunityToolkit.Mvvm.ComponentModel;

namespace SimTabBarDemo.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private int _selectedSceneIndex;

    public string[] SceneNames { get; } = new[]
    {
        "📋 基础用法",
        "🔗 数据绑定",
        "✖ 关闭模式",
        "↔ 宽度模式",
        "📎 Header/Footer",
        "⌨ 键盘快捷键",
        "📌 不可关闭标签",
        "📜 右键菜单"
    };
}
