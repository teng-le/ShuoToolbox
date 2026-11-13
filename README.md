# 硕のtoolbox - AI 协作与开发指南 (v0.4.0 半成品开发中)

> 🤖 **致接手的 AI 助手：请先通读此文档，这是项目的核心上下文与沟通规则。**
> 本项目由用户（导游）与 AI 结对开发，目前处于半成品持续迭代阶段。

## 📌 沟通与协作规则（最高优先级，必须严格遵守）
1. **代码修改规则**：绝对不要教用户“如何修改某一段代码”。一旦涉及代码改动，必须直接输出该文件的**完整、可直接 Ctrl+A 全选替换的代码**。
2. **命令执行规则**：涉及 CMD 操作时，**严禁**把多行命令塞进一个代码块让用户一次复制。必须**一行一个代码块**，让用户一步一步执行（因为用户的魔改 CMD 多行粘贴会报错）。
3. **沟通风格**：务实、直接、不废话。像有经验的开发老搭档，遇到问题直接给出妥协方案，同一个问题提醒 3 遍以上即代表用户已确认，不要再啰嗦。

## 🏗️ 核心架构与技术栈
- **框架**：C# WinForms (.NET 10.0-windows)
- **工程**：MSBuild 配置了 `AfterTargets="Build"` 和 `AfterTargets="Publish"`，自动将 `Tools` 文件夹和 `tools.json` 复制到输出/发布目录。
- **权限**：`app.manifest` 强制 `requireAdministrator`（自带管理员权限）。
- **发布方式**：`dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None`（单文件自包含，解压即用）。

## 📂 核心文件结构与职责
- `Program.cs`：程序入口。包含全局异常捕获（`Application.ThreadException`），崩溃时写 `error.log` 防止无提示闪退。
- `Form1.cs`：主界面。无边框拖拽（`WM_NCHITTEST` 找回 Aero Snap）、自定义标题栏、左侧分类菜单、工具卡片渲染（手动双击判定）、工具启动逻辑（绝对路径解析 + UAC提权）。
- `Models.cs`：数据模型。`ToolItem`（包含 Name, Description, Icon, Path, Arguments, WorkingDirectory, RunAsAdmin），`CategoryItem`，`HardwareCard`。
- `CardPanel.cs`：自定义圆角卡片控件（GDI+ 绘制圆角边框），用于硬件信息展示。
- `NativeMethods.cs`：Win32 API 声明，专用于主窗体的底层圆角裁切（`SetWindowRgn`）。
- `HardwareInfoProvider.cs`：纯 WMI 查询层，后台线程（`Task.Run`）运行，不卡 UI，包含大量 try-catch 防止 WMI 崩溃。
- `tools.json`：数据驱动的工具配置文件，代码根据此文件动态生成右侧工具列表。
- `Tools/` 文件夹：存放所有绿色便携版第三方工具（Everything, Dism++, Geek, NDM, QtScrcpy 等）。

## ✅ 已实现功能
- 无边框窗口拖拽与大小调整、圆角裁切。
- WMI 硬件信息卡片页（异步加载）。
- 支持读取 `tools.json` 生成工具分类和卡片，支持双击启动、参数传递、工作目录设定、管理员权限启动。

## 🚧 下一步待办 (Todo)
1. **设置面板 (⚙️)**：点击标题栏 ⚙ 按钮弹出设置窗口（皮肤颜色、开机自启、静默检测等）。
2. **搞机模式 (📱)**：点击左下角按钮拉起搞机工具箱，未来可加入 ADB 检测连接。
3. **网络测速 (🌐)**：弹提示框，然后调用默认浏览器打开测速网。
4. **SpaceSniffer 汉化 (🇨🇳)**：启动时静默写入汉化配置。

## ⚠️ 历史踩坑记录
- 小黄鸭（LosslessScaling）因路径写死导致更换文件夹失效，已移除，不再追求绝对路径。
- 不要乱加工具，一次只加一个，跑通测试后再加下一个。
