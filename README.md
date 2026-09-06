# SimTabBar

类似 VS Code 的标签页 Avalonia 控件库。

![演示截图](demo.jpg)

## 功能特性

- **宽度模式** — Equal / SizeToContent / Compact / Fixed 四种标签页宽度策略
- **标签头模板** — `HeaderTemplate` 让标签头渲染任意多元素（如状态圆点 + 标题），模板上下文为数据项，可对局部元素单独着色；`TabBar.HeaderTemplate` 在 ItemsSource 模式下生效
- **关闭按钮模式** — Auto / OnPointerOver / Always 三种显示方式
- **右键菜单** — 关闭 / 关闭其他 / 关闭全部
- **扩展区域** — TabStrip Header / Footer 可插入自定义内容
- **键盘导航** — Ctrl+Tab / Ctrl+Shift+Tab、Ctrl+F4（关闭）
- **固定标签页** — 支持不可关闭的固定标签
- **拖动排序** — `CanReorderTabs` 开启后按住标签拖动换位（实时让位空隙）；`TabDragStarting` 可取消，`TabReorderCompleted` 报告结果
- **主题支持** — 适配深色 / 浅色主题