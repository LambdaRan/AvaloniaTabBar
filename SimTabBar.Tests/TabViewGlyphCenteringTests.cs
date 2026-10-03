using System.Collections;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.VisualTree;
using SimTabBar.Controls;

using Xunit;
using Avalonia.Headless.XUnit;

namespace SimTabBar.Tests;

/// <summary>
/// 「盒子里只有一个字形」的两处按钮 —— 标签头关闭 ✕（20×20）与标签条尾部 ＋（28×28）
/// —— 的字形居中回归网。
///
/// <para><b>病根不在本库主题里，在 Fluent 的 Button 默认值</b>：
/// <c>HorizontalContentAlignment</c> / <c>VerticalContentAlignment</c> 都是
/// <c>Stretch</c>（Avalonia 12.1，实测），字符串内容被包成 <c>AccessText</c>
/// （<c>TextAlignment=Start</c>）铺满整个按钮盒 —— 于是 20×20 的 ✕ 画在盒子左上角：
/// 实测墨迹 8×9 落在 left=1 / right=11 / top=4 / bottom=7（2026-10-04，Windows/Skia）。
/// 自撑宽的按钮看不出这个病：那种情况下盒子本来就等于文字宽，撑满与居中恰好等价；
/// 一旦 Width/Height 被钉死，两者就分家了。所以主题里显式给了 Center，本网钉住两件事：
/// 内容**没有被拉伸**、内容盒的中心落在按钮盒的中心。</para>
///
/// <para><b>为什么断言布局盒而不是墨迹像素</b>：墨迹包围盒随字体/字号漂移
/// （连 ＋ 在默认字体下都天然偏下 1.5px），跨平台会假红；而「被 Stretch 拉伸」
/// 这一病根在任何字体下都把内容盒撑成按钮盒大小 —— 与字体无关，因此可以严格断言。
/// 字形真正落在哪，由中心对齐 + 字形自身的对称性保证，不再由布局引擎的默认值决定。</para>
/// </summary>
public class TabViewGlyphCenteringTests
{
    /// <summary>
    /// 内容元素必须①按自然尺寸测量（没被 Stretch 拉成按钮盒大小），
    /// ②其盒子中心与按钮盒中心重合。半像素容差留给布局取整。
    /// </summary>
    private static void AssertContentMeasuredAndCentered(Button button, string label)
    {
        var presenter = TestHelper.Part<ContentPresenter>(button, "PART_ContentPresenter");
        var content = presenter.Child as Control;

        Assert.True(content is not null, $"{label}：ContentPresenter 没有内容元素");
        Assert.True(content!.Bounds.Width < button.Bounds.Width,
            $"{label}：内容宽 {content.Bounds.Width} 被拉满按钮盒 {button.Bounds.Width}（Stretch 病）");
        Assert.True(content.Bounds.Height < button.Bounds.Height,
            $"{label}：内容高 {content.Bounds.Height} 被拉满按钮盒 {button.Bounds.Height}（Stretch 病）");

        var origin = content.TranslatePoint(new Point(0, 0), button);
        Assert.True(origin.HasValue, $"{label}：内容不在按钮的可视子树里");

        double centerX = origin!.Value.X + content.Bounds.Width / 2;
        double centerY = origin.Value.Y + content.Bounds.Height / 2;

        Assert.InRange(centerX, button.Bounds.Width / 2 - 0.5, button.Bounds.Width / 2 + 0.5);
        Assert.InRange(centerY, button.Bounds.Height / 2 - 0.5, button.Bounds.Height / 2 + 0.5);
    }

    [AvaloniaFact]
    public void CloseButton_Glyph_IsMeasuredNaturally_AndCentered()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(3);
        tabView.CloseButtonOverlayMode = TabBarCloseButtonOverlayMode.Always;
        tabView.SelectedIndex = 0;
        TestHelper.Pump(window);

        var tab = (TabBarItem)tabView.ContainerFromIndex(0)!;
        var close = TestHelper.Part<Button>(tab, "PART_CloseButton");

        // 显式的两轴 Center 是这条网的因 —— Fluent 的默认值是 Stretch。
        Assert.Equal(HorizontalAlignment.Center, close.HorizontalContentAlignment);
        Assert.Equal(VerticalAlignment.Center, close.VerticalContentAlignment);

        AssertContentMeasuredAndCentered(close, "关闭按钮 ✕");

        window.Close();
    }

    [AvaloniaFact]
    public void AddButton_Glyph_IsMeasuredNaturally_AndCentered()
    {
        var (tabView, window) = TestHelper.CreateTabBarWithTabs(3);
        tabView.SelectedIndex = 0;
        TestHelper.Pump(window);

        var add = TestHelper.Part<Button>(tabView, "PART_AddButton");

        Assert.Equal(HorizontalAlignment.Center, add.HorizontalContentAlignment);
        Assert.Equal(VerticalAlignment.Center, add.VerticalContentAlignment);

        AssertContentMeasuredAndCentered(add, "新建按钮 ＋");

        window.Close();
    }
}
