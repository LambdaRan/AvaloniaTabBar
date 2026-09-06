namespace SimTabBar.Controls;

public enum TabBarWidthMode
{
    /// <summary>每条标签平分视口宽度，钳制在 [MinWidth, MaxWidth]。随数量与窗口尺寸变化。</summary>
    Equal,

    /// <summary>每条标签随自身内容宽度。</summary>
    SizeToContent,

    /// <summary>选中页膨胀、未选页收缩成图标宽。</summary>
    Compact,

    /// <summary>
    /// 每条标签固定像素宽（取自主题资源 SimTabBarItemFixedWidth，默认 160），
    /// 不随数量与窗口尺寸变化 —— 即「互不影响」。总宽超出视口时走标签条的
    /// ScrollViewer 与悬浮滚动条。
    /// 追加在末尾以免移动既有成员的数值。
    /// </summary>
    Fixed
}
