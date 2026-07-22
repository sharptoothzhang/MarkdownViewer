# Markdown Viewer 设计文档

## 1. 概述

Markdown Viewer 是一个轻量级的 Windows Markdown 查看和编辑工具，旨在提供快速、简洁的 Markdown 文件编辑体验。

### 1.1 设计目标

- **简洁** - 界面类似 Windows 记事本，不增加额外复杂度
- **快速** - 启动快、响应快、渲染快
- **兼容** - 最小依赖，跨 Windows 版本兼容

### 1.2 技术选型

| 选择 | 原因 |
|------|------|
| C# | 快速开发、Windows 原生支持 |
| WinForms | 轻量、部署简单、csc.exe 可直接编译 |
| WebView2 | Edge 内核，支持现代 JavaScript 和 Mermaid |
| Markdig | CommonMark 标准，支持 20+ 扩展，性能优秀 |
| highlight.js | 代码块语法高亮，支持 190+ 语言 |

## 2. 架构设计

### 2.1 代码结构

```
MarkdownViewer/
├── Program.cs          # 入口点
├── Core/              # 核心功能
│   ├── NativeMethods.cs   # P/Invoke 方法
│   ├── MarkdownParser.cs   # Markdown 解析器
│   ├── RecentFiles.cs     # 最近文件管理
│   └── Icons.cs           # 图标生成
├── Forms/             # UI 窗体
│   ├── MainForm.cs        # 主窗体
│   ├── HelpForm.cs        # 帮助对话框
│   └── FindReplaceDialog.cs # 查找替换对话框
├── Hooks/            # 系统钩子
│   ├── DropHook.cs        # 拖放文件钩子
│   └── KeyHook.cs         # 键盘钩子
├── Resources/        # 资源
│   └── HelpContent.cs     # 帮助内容 HTML
├── Scripts/          # 前端脚本
│   ├── highlight.min.js   # 语法高亮库
│   ├── hljs-github.min.css # 浅色主题
│   ├── hljs-github-dark.min.css # 深色主题
│   └── mermaid.min.js     # Mermaid 图表库
├── E2E/               # E2E 测试
│   └── E2ETests.cs        # E2E 测试用例
├── lib/               # 第三方库
│   ├── Markdig.dll
│   ├── System.Memory.dll
│   ├── System.Buffers.dll
│   ├── System.Numerics.Vectors.dll
│   └── System.Runtime.CompilerServices.Unsafe.dll
├── Test.cs           # 单元测试
├── build.bat         # 构建脚本
└── app.ico          # 程序图标
```

### 2.2 模块职责

| 模块 | 职责 |
|------|------|
| Program | 应用入口，初始化 |
| MainForm | 主窗体，UI 事件处理 |
| HelpForm | 帮助对话框 |
| FindReplaceDialog | 查找替换对话框 |
| MarkdownParser | Markdown 文本解析为 HTML |
| RecentFiles | 最近文件列表管理（注册表） |
| DropHook | 全局消息钩子，拦截拖放文件 |
| Icons | 工具栏图标生成 |
| NativeMethods | Win32 API P/Invoke 封装 |

### 2.3 状态管理

| 变量 | 类型 | 说明 |
|------|------|------|
| CurrentFile | string | 当前文件路径 |
| IsDirty | bool | 是否有未保存修改 |
| IsPreviewMode | bool | 当前是否为预览模式 |
| ZoomLevel | float | 缩放级别 (50-200) |

## 3. UI 设计

### 3.1 窗体布局

```
┌─────────────────────────────────────────────────┐
│ File  View  Help                    [预览]     │  <- MenuStrip + ToolStripButton
├─────────────────────────────────────────────────┤
│                                                 │
│                                                 │
│            RichTextBox / WebBrowser              │  <- 主内容区
│                                                 │
│                                                 │
├─────────────────────────────────────────────────┤
│ 字数: 0  词数: 0  大小: 0.0 KB  UTF-8  [-][+]│  <- StatusStrip
└─────────────────────────────────────────────────┘
```

### 3.2 组件说明

| 组件 | 类型 | 说明 |
|------|------|------|
| 菜单栏 | MenuStrip | File/View/Help 标准化菜单 |
| 编辑器 | RichTextBox | 多行文本编辑，Consolas 字体，支持回车换行 |
| 预览器 | WebView2 | Edge 内核渲染，支持 Mermaid 图表 |
| 切换按钮 | ToolStripButton | Edit/Preview 切换，带图标 |
| 状态栏 | StatusStrip | SpringLayout 分布，显示字数/词数/编码/缩放 |
| 缩放控制 | ToolStripButton | +/- 按钮控制预览缩放 (50%-200%) |

### 3.3 颜色主题

| 元素 | 颜色 | 说明 |
|------|------|------|
| 主色调 | #407040 | 绿色，Markdown 主题色 |
| 标题 | #333 | 深灰色 |
| 边框 | #ddd | 浅灰色分隔线 |
| 背景 | #fff | 白色 |
| 代码背景 | #f5f5f5 | 浅灰背景 |

## 4. Markdown 解析器设计

### 4.1 解析策略

使用 **Markdig 库** 解析 Markdown 为 HTML：
- Markdig 是 .NET 生态中最成熟的 Markdown 解析器
- 支持 CommonMark 标准和 20+ 扩展
- 内置 Mermaid 图表支持（通过 Diagrams 扩展）

### 4.2 Mermaid 语法自动修正

解析器会自动修正常见的 Mermaid 语法错误：
- `A<--B: msg` → `B-->>A: msg` (反向虚线箭头)
- `Node[text<br/>...]` → `Node["text<br/>..."]` (节点文本含 HTML 时自动加引号)

### 4.3 支持的 Markdown 语法

| 功能 | 语法 |
|------|------|
| 标题 | # ## ### |
| 粗体 | **text** |
| 斜体 | *text* |
| 粗斜体 | ***text*** |
| 删除线 | ~~text~~ |
| 行内代码 | `code` |
| 代码块 | ``` |
| 引用 | > text |
| 无序列表 | - * + |
| 有序列表 | 1. |
| 任务列表 | - [x] - [ ] |
| 链接 | [text](url) |
| 图片 | ![alt](url) |
| 表格 | \\|\\| |
| 分割线 | --- |

## 5. 文件关联

### 5.1 注册表结构

```
HKEY_CURRENT_USER\Software\Classes\
├── .md -> MarkdownViewer.Markdown
└── MarkdownViewer.Markdown
    ├── (Default) = "Markdown 文档"
    └── shell\open\command -> "MarkdownViewer.exe" "%1"
```

### 5.2 Shell 通知

注册后调用 `SHChangeNotify` (0x08000000) 通知 Shell 更新图标缓存。

## 6. 拖放处理

### 6.1 问题背景

WebBrowser 是 ActiveX 控件，会拦截 `WM_DROPFILES` 消息，导致拖放到预览区域无法触发文件打开。

### 6.2 解决方案

使用 `DropHook` 全局消息钩子拦截 `WM_DROPFILES`：
- 在 `Application.Run` 之前安装钩子
- 在消息循环中拦截 `WM_DROPFILES`
- 调用 `DragQueryFile` 获取文件路径
- 最后调用 `MainForm.OpenFile()` 打开文件

## 7. 快捷键支持

| 快捷键 | 功能 |
|--------|------|
| Ctrl+N | 新建文件 |
| Ctrl+O | 打开文件 |
| Ctrl+S | 保存文件 |
| Ctrl+Shift+S | 另存为 |
| Ctrl+E | 切换到编辑模式 |
| Ctrl+P | 切换到预览模式 |
| Ctrl+F | 打开查找替换对话框 |
| F1 | 显示帮助 |
| Esc | 关闭帮助对话框 |

## 8. 最近文件

最近文件存储在注册表 `HKEY_CURRENT_USER\Software\MarkdownViewer\RecentFiles`，最多保存 10 个文件。

## 9. 查找替换

FindReplaceDialog 支持：
- 查找下一个
- 替换
- 全部替换
- 区分大小写选项

## 10. 性能优化

### 10.1 避免重复渲染

- TextChanged 事件触发时检查 `IsPreviewMode` 状态
- 仅在预览模式下更新 WebBrowser

### 10.2 缓存机制

- Markdown 解析结果直接传递给 WebBrowser，不做额外缓存
- 解析过程使用 StringBuilder 避免字符串拼接开销

### 10.3 延迟加载

- 文件内容在 OpenFile 时一次性读取
- 预览内容在切换到预览模式时渲染

## 11. 错误处理

| 场景 | 处理方式 |
|------|----------|
| 文件不存在 | 提示错误，继续运行 |
| 文件读取失败 | MessageBox 提示，Abort |
| 文件写入失败 | MessageBox 提示，保留内容 |
| 注册表写入失败 | 捕获异常，提示错误 |

## 12. 测试框架

### 12.1 单元测试

位于 `Test.cs`，使用 C# 控制台程序实现，测试 MarkdownParser 的解析功能。

**测试用例 (28项)**:
- 空字符串处理
- 标题 (H1-H6)
- 行内格式 (粗体、斜体、粗斜体、删除线、行内代码)
- 链接和图片
- 列表 (无序、有序、任务列表)
- 代码块、引用、表格、分割线
- HTML 特殊字符编码

### 12.2 E2E 测试

位于 `E2E/` 目录，使用 Win32 API 实现 UI 自动化测试。

**测试用例 (10项)**:
- TestLaunchApp - 应用启动
- TestWindowTitle - 窗口标题验证
- TestMenuBar - 菜单栏存在
- TestStatusBar - 状态栏存在
- TestToolbar - 工具栏存在
- TestCreateNewFile - Ctrl+N 新建文件
- TestHelpDialog - F1 打开帮助
- TestFindDialog - Ctrl+F 打开查找
- TestPreviewLoading - 预览加载测试

**技术实现**:
- `FindWindow` / `FindWindowEx` - 窗口查找
- `GetWindowText` - 获取窗口标题
- `SendKeys.SendWait` - 发送键盘事件
- `SetForegroundWindow` - 确保窗口获得焦点

## 13. 已完成功能

- [x] 基础编辑/预览切换
- [x] 文件打开/保存/另存
- [x] 拖拽支持 (全局钩子)
- [x] 文件关联注册
- [x] 帮助对话框 (Esc 关闭)
- [x] 状态栏 (字数、词数、大小、编码)
- [x] 缩放控制 (50%-200%，按钮)
- [x] 窗体图标
- [x] RichTextBox 回车换行
- [x] 快捷键支持
- [x] 最近文件列表
- [x] 查找替换功能
- [x] 单元测试 (28项)
- [x] E2E 测试 (11项)
- [x] 关联文件图标
- [x] Ctrl+滚轮缩放状态栏同步
- [x] Markdown 解析缓存优化
- [x] 深色模式切换
- [x] 查找替换改进 (上一个/下一个、非模态、ESC关闭)
- [x] 窗口焦点管理 (SetForegroundWindow)
- [x] JavaScript 错误捕获和日志记录
- [x] 升级到 .NET Framework 4.8
- [x] Markdig 替代手写 MarkdownParser
- [x] Mermaid 中文编码修复 (临时文件 + UTF-8)
- [x] Mermaid.js 虚拟主机映射加载
- [x] Mermaid 语法自动修正
- [x] OnTextChanged 防抖机制 (300ms)
- [x] 全局异常处理 (ThreadException + UnhandledException)
- [x] WebView2 初始化失败友好提示
- [x] 修复 WebView2 快捷键冲突 (全局钩子)
- [x] 窗口标题显示当前模式 [编辑]/[预览]
- [x] DLL 移到 lib/ 目录
- [x] 代码块语法高亮 (highlight.js, 190+ 语言, 浅色/深色主题)

## 14. 未来改进方向

- [ ] 自动保存
- [ ] 多标签页支持
- [ ] 自定义主题
- [ ] 打印功能
- [ ] 导出 PDF
