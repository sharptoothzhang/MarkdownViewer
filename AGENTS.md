# AGENTS.md

## 项目概述

Markdown Viewer 是一个 Windows 平台上的 Markdown 文件查看和编辑工具，使用 C# / WinForms 开发。

## 首要原则

1. 用中文思考
2. 不确定的东西先问，别瞎猜
3. 代码能写短就不要写太长
4. 没让改的地方千万不要碰
5. 给目标别给步骤
6. 任务结束记得将修改的内容同步到文档中

## 开发环境

- **编译器**: .NET Framework csc.exe (`C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe`)
- **语言**: C#
- **目标框架**: .NET Framework 4.8
- **UI框架**: WinForms
- **预览引擎**: WebView2 (Edge 内核)
- **Markdown解析**: Markdig 库 (CommonMark 标准)

## 项目结构

详见 [README.md](README.md#项目结构)

## 构建

```batch
build.bat              # 完整构建（单元测试 + E2E 测试）
build_test.bat         # 仅编译单元测试
build_e2e.bat          # 仅编译 E2E 测试
```

## 测试

详见 [README.md](README.md#运行测试)

## 代码规范

### 命名约定

| 类型 | 约定 | 示例 |
|------|------|------|
| 类 | PascalCase | `MarkdownParser` |
| 方法 | PascalCase | `OpenFile()`, `ParseBlocks()` |
| 变量 | CamelCase | `currentFile`, `isDirty` |
| 常量 | PascalCase | `HELLO` |

### 代码风格

- 使用显式类型声明（.NET 2.0 不支持 `var`）
- 使用 `delegate(object s, EventArgs e)` 而非 lambda 表达式
- 属性访问省略括号：`Editor.Text` 而非 `Editor.get_Text()`
- 字符串拼接使用 StringBuilder

### 关键代码模式

```csharp
// 事件处理 (.NET 2.0 不支持 lambda)
Editor.TextChanged += delegate(object s, EventArgs e) { /* ... */ };

// 文件操作
using (OpenFileDialog d = new OpenFileDialog()) {
    if (d.ShowDialog() == DialogResult.OK) {
        // ...
    }
}
```

## 常见问题

详见 [README.md](README.md#常见问题)

## 开发流程

1. 修改源代码文件 (Core/, Forms/, Hooks/, Resources/)
2. 运行单元测试: `build_test.bat && Test.exe`
3. 编译: `build.bat`
4. 测试功能
5. 更新相关文档 (README.md)

## 已知限制

- 变量名不能与外层作用域的事件参数名冲突（如 `e`）
- 预览模式下编辑不会同步到源文本
- RichTextBox 不支持 `AcceptsReturn` 属性

## 发布

1. 确保 `MarkdownViewer.exe`, `app.ico` 在同一目录
2. 可选: 创建 zip 打包
3. 用户运行程序后可点击 "File → Associate .md" 注册文件关联
