using System.Collections;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.VisualTree;
using Avalonia.Headless.XUnit;
using SimTabBar.Controls;


[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions())
        .AfterSetup(_ =>
        {
            var app = Application.Current!;

            // 添加 Fluent 主题以提供内置控件样式
            app.Styles.Add(new FluentTheme());

            // 加载 SimTabBar 的 ControlTheme（包含 Light/Dark 主题字典）
            app.Styles.Add(new StyleInclude(new Uri("avares://SimTabBar/"))
            {
                Source = new Uri("avares://SimTabBar/Themes/SimTabBarTheme.axaml")
            });
        });
}

/// <summary>
/// 测试项目的最小 Application 子类。AppBuilder.Configure&lt;T&gt;() 所必需。
/// </summary>
public class App : Application
{
}

namespace SimTabBar.Tests
{
    public static class TestHelper
    {
        public static (TabBar tabView, Window window) CreateTabBarWithTabs(int tabCount = 3)
        {
            var tabView = new TabBar();

            for (int i = 0; i < tabCount; i++)
            {
                var tab = new TabBarItem
                {
                    Header = $"Tab {i + 1}",
                    Content = new TextBlock { Text = $"Content {i + 1}" }
                };
                ((IList)tabView.Items).Add(tab);
            }

            var window = new Window
            {
                Width = 800,
                Height = 600,
                Content = tabView
            };

            window.Show();
            return (tabView, window);
        }

        /// <summary>
        /// 跑完一轮布局与渲染。断言 Bounds / 伪类 / 模板子元素前必须先调用，
        /// 否则 headless 下 Measure/Arrange 还没发生。
        /// </summary>
        public static void Pump(Window window, double w = 800, double h = 600)
        {
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            window.Measure(new Size(w, h));
            window.Arrange(new Rect(0, 0, w, h));
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        }

        /// <summary>
        /// 按 x:Name 在可视化树里找模板部件。
        /// </summary>
        public static T Part<T>(Visual root, string name) where T : Control =>
            root.GetVisualDescendants().OfType<T>().First(c => c.Name == name);
    }
}
