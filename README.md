# Markdown Viewer

一个简洁的 Windows Markdown 查看和编辑工具，界面类似于 Windows 记事本。

## 功能特性

- **简洁界面** - 类似记事本的简洁设计
- **实时预览** - 支持 Markdown 实时渲染预览
- **Mermaid 图表** - 支持流程图、时序图等图表渲染
- **文件关联** - 一键注册为系统默认 .md 文件打开程序
- **拖拽支持** - 直接拖拽文件到窗口打开
- **状态栏** - 显示字数统计、词数、文件大小、编码、缩放级别
- **缩放控制** - 预览模式下支持 50%-200% 缩放（按钮）
- **完整菜单** - File/View/Help 标准化菜单结构
- **未保存提示** - 关闭或打开新文件时提示保存
- **回车换行** - 使用 RichTextBox 支持真正的回车换行
- **快捷键** - 支持 Ctrl+N/O/S/E/P/F 等快捷键
- **最近文件** - 快速访问最近打开的文件
- **查找替换** - 支持查找和批量替换功能
- **大纲导航** - 支持文档大纲导航

## 支持的 Markdown 语法

基于 **CommonMark** 标准（使用 Markdig 解析器）

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
| 表格 | \| \| |
| 分割线 | --- |

## 快捷键

| 快捷键 | 功能 |
|--------|------|
| Ctrl+N | 新建文件 |
| Ctrl+O | 打开文件 |
| Ctrl+S | 保存文件 |
| Ctrl+Shift+S | 另存为 |
| Ctrl+E | 切换到编辑模式 |
| Ctrl+P | 切换到预览模式 |
| Ctrl+F | 查找和替换 |
| F1 | 查看帮助 |
| Esc | 关闭帮助对话框 |

## 使用方法

### 打开文件
- 点击 **File → Open**
- 直接拖拽 .md 文件到窗口
- 命令行: `MarkdownViewer.exe file.md`
- 从 **File → Recent Files** 选择最近文件

### 保存文件
- 点击 **File → Save** / **File → Save As**

### 切换视图
- 点击工具栏的 **Edit/Preview** 按钮
- 或使用 **View → Edit Mode / View → Preview Mode**

### 缩放控制
- 点击状态栏的 **+/-** 按钮调整预览缩放
- 缩放范围: 50% - 200%

### 查找替换
- 使用 **Ctrl+F** 或 **View → Find/Replace**
- 支持查找下一个、替换、全部替换

### 文件关联
- 点击 **File → Associate .md → 注册.md 关联**
- 关联后双击 .md 文件自动用本程序打开
- 点击 **File → Associate .md → 取消.md 关联** 可取消关联

### 帮助
- 点击 **Help → Usage**
- 按 **F1** 可快速打开帮助

## 系统要求

- Windows 7 或更高版本
- .NET Framework 4.8 (通常系统已自带)

## 技术架构

- **语言**: C#
- **编译器**: .NET Framework csc.exe
- **UI框架**: WinForms
- **架构**: 多文件模块化设计
- **拖放**: 全局消息钩子 (WM_DROPFILES)
- **预览**: WebView2 (Edge 内核) + Mermaid.min.js
- **Markdown 解析**: Markdig 库 (CommonMark 标准)
- **图表**: Mermaid 支持流程图、时序图等

## 项目结构

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
├── Resources/        # 预览资源
│   ├── preview.html       # 预览 HTML 模板
│   ├── css/
│   │   ├── light.css      # 浅色主题 CSS
│   │   └── outline.css    # 大纲面板 CSS
│   └── js/
│       ├── preview.js     # 预览脚本
│       ├── highlight.min.js # 语法高亮库
│       └── mermaid.min.js     # Mermaid 图表库
├── E2E/              # E2E 测试
│   └── E2ETests.cs        # E2E 测试用例
├── lib/              # 第三方库
│   ├── Markdig.dll
│   ├── System.Memory.dll
│   ├── System.Buffers.dll
│   ├── System.Numerics.Vectors.dll
│   ├── System.Runtime.CompilerServices.Unsafe.dll
│   └── WebView2Loader.dll
├── Release/          # 发布版本
│   ├── MarkdownViewer.exe
│   ├── app.ico
│   ├── highlight.min.js
│   ├── mermaid.min.js
│   └── Resources/
│       ├── preview.html
│       ├── css/
│       │   ├── light.css
│       │   └── outline.css
│       └── js/
│           └── preview.js
├── Test.cs           # 单元测试 (30 项)
├── build.bat         # 构建脚本
├── test.md           # 测试文件
├── mermaid_test.md   # Mermaid 测试文件
├── README.md         # 项目说明
├── DESIGN.md         # 设计文档
└── AGENTS.md         # AI助手文档
```

## 构建

使用 `build.bat` 进行完整构建（包含单元测试和 E2E 测试）。

单独构建主程序：
```batch
build.bat
```

## 运行测试

### 单元测试

编译并运行 MarkdownParser 单元测试（30 项）：

```batch
build_test.bat
Test.exe
```

### E2E 测试

编译并运行端到端测试（11 项），测试应用启动、菜单、窗口等 UI 功能：

```batch
build_e2e.bat
E2ETest.exe
```

### 完整构建（含测试）

```batch
build.bat
```

E2E 测试需要 .NET Framework 4.0+，使用 Win32 API 进行窗口操作和 UI 自动化测试。

**E2E 测试用例 (11 项)**:
- TestLaunchApp - 应用启动
- TestWindowTitle - 窗口标题
- TestMenuBar - 菜单栏
- TestStatusBar - 状态栏
- TestToolbar - 工具栏
- TestCreateNewFile - 新建文件
- TestHelpDialog - 帮助对话框
- TestFindDialog - 查找对话框
- TestPreviewLoading - 预览加载测试
- TestOutlinePanel - 大纲面板测试
- TestFileTypeDetection - 文件类型检测