# 健康助手

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET](https://img.shields.io/badge/.NET-10-512BD4.svg)
![C# WPF](https://img.shields.io/badge/C%23-WPF-239120.svg)
![Platform](https://img.shields.io/badge/platform-Windows-0078D6.svg)

一个用 **C# / .NET 10 WPF** 编写的 **Windows 桌面健康提醒小工具**：默认每 20 分钟提醒你远眺 20 秒（20-20-20 护眼法则），支持自由添加自定义提醒（比如喝水、伸展），**系统托盘常驻**、低资源占用，适合长期挂在后台保护视力与健康。

## 功能

- 默认提醒「远眺」（每 20 分钟，倒计时 20 秒）和「喝水」（每 60 分钟，按钮「我喝完了」）
- 自定义提醒：可设置名称、提醒内容、间隔（1–1440 分钟）、按钮文字（如「我喝完了」）和可选倒计时（0–600 秒，0 表示无倒计时）
- 提醒弹窗一直停留直到处理：点按钮（有倒计时则先倒计时）、或点 × 都视为已处理并重新计时
- 系统托盘常驻：关闭主窗口自动隐藏到托盘，提醒不中断；可选开机自启
- 单实例运行：重复启动只会唤出已有窗口
- 低资源占用：单一定时器按绝对时间调度，空闲时无任何轮询和循环动画
- 像素风 UI：暖色纸面桌游风 8-bit，内嵌开源像素中文字体 Fusion Pixel（SIL OFL 1.1）
- 像素风图标：暖纸卡片上的「远眺之眼」望向青色远山与琥珀暖阳，呼应 20-20-20 护眼提醒（`tools/generate_icon.py` 可重新生成多尺寸 ICO）

## 快速开始

### 方式一：直接使用

从 [Releases](../../releases) 下载最新的 `健康助手.exe`（自包含单文件，无需安装 .NET 运行时），双击运行即可。

### 方式二：源码构建

需要 .NET 10 SDK（或 Visual Studio 2026）。

```powershell
dotnet build 健康助手.slnx -c Debug
.\src\健康助手\bin\Debug\net10.0-windows\健康助手.exe
```

也可以直接用 Visual Studio 打开 `健康助手.slnx` 构建运行。

## 技术栈

| 项 | 说明 |
|---|---|
| 语言 / 框架 | C#，.NET 10（`net10.0-windows`） |
| UI | WPF（XAML），自绘像素风控件 |
| 调度 | 单定时器按绝对时间调度，空闲零轮询 |
| 持久化 | `%APPDATA%\健康助手\reminders.json` |
| 字体 | 内嵌开源像素中文字体 [Fusion Pixel](https://github.com/TakWolf/fusion-pixel-font) |

## 目录结构

```
健康助手.slnx        解决方案文件
src/健康助手/         主程序（WPF 应用）
tools/              图标生成脚本（generate_icon.py）
```

## 配置

提醒事项保存在：

```
%APPDATA%\健康助手\reminders.json
```

删除该文件会在下次启动时重新生成默认的「远眺」提醒。开机自启开关写入当前用户的 `HKCU\...\Run` 注册表键，不需要管理员权限。

## 资源与许可

- 本项目采用 [MIT License](LICENSE)
- 字体：[Fusion Pixel Font](https://github.com/TakWolf/fusion-pixel-font)（SIL Open Font License 1.1），许可证见 `src/健康助手/Fonts/OFL.txt`
- 像素风视觉规范来自 `wpf-pixel-ui` 设计技能
