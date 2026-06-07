# NetworkSpeedMonitor

一个轻量级的 Windows 桌面网速悬浮窗小工具，实时显示指定网卡的上传/下载速度。

![.NET](https://img.shields.io/badge/.NET-10.0-purple)
![Platform](https://img.shields.io/badge/Platform-Windows-blue)

## 功能特性

- 🖥️ 半透明悬浮窗，始终置顶显示
- ⬇️⬆️ 实时显示下载/上传速度（Mbps）
- 🖱️ 支持拖拽移动位置
- 👻 鼠标悬停自动淡出，不遮挡内容
- ⌨️ **Ctrl+Alt+N** 全局快捷键，一键隐藏/显示
- 🎯 可指定监控特定网卡（默认按名称精确匹配）
- 📦 单文件发布，无需安装，双击即用

## 环境要求

- Windows 10 1809+ (x64)
- .NET 8 Runtime（如果使用自包含发布版本则不需要）

## 构建与发布

```powershell
# 克隆项目
git clone https://github.com/zhangjing0413/NetworkSpeedMonitor.Windows.git

# 发布为单文件可执行程序
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish
```

发布后的 `NetworkSpeedMonitor.exe` 位于 `publish/` 目录下，可直接运行。

## 配置网卡

默认监控名称为 **以太网 2** 的网卡。如需修改，编辑 `NetworkSpeedMonitor.cs` 中的常量：

```csharp
private const string TargetInterfaceName = "以太网 2";
```

修改后重新构建即可。如果指定的网卡不存在，程序会自动回退到优先有线、其次 WiFi 的选择策略。

## 使用说明

- **启动**：直接双击 `NetworkSpeedMonitor.exe`
- **拖拽**：鼠标左键按住悬浮窗拖动到任意位置
- **退出**：右键点击悬浮窗 → 退出

## 技术栈

- **语言**：C# / .NET 8
- **UI 框架**：WPF
- **网络 API**：`System.Net.NetworkInformation`

## 项目结构

```
├── NetworkSpeedMonitor.csproj   # 项目文件
├── App.xaml / App.xaml.cs       # WPF 应用入口
├── MainWindow.xaml              # 悬浮窗 UI 定义
├── MainWindow.xaml.cs           # 窗口逻辑（定时器、拖拽、速度格式化）
└── NetworkSpeedMonitor.cs       # 核心网速采集逻辑
```

## License

MIT
