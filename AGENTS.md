# AGENTS.md

## 项目概述

健康助手：Windows 桌面健康提醒小工具。WPF + .NET 10，像素风 UI（暖色纸面桌游风 8-bit，wpf-pixel-ui 技能规范），系统托盘常驻，低资源占用。

## 技术栈

- .NET 10 / C# / WPF（+ Windows Forms 托盘）
- 解决方案文件：`健康助手.slnx`（新版 XML 格式）
- 内嵌像素中文字体：Fusion Pixel（SIL OFL 1.1，许可证 `src/健康助手/Fonts/OFL.txt`）

## 常用命令

```powershell
# 构建
dotnet build 健康助手.slnx -c Debug

# 运行
.\src\健康助手\bin\Debug\net10.0-windows\健康助手.exe

# 重新生成像素图标（Python + Pillow）
python tools/generate_icon.py
```

## 项目约定

- 提醒调度：单一定时器按绝对时间调度，空闲时零轮询、无循环动画（性能红线）
- 用户配置：`%APPDATA%\健康助手\reminders.json`，删除即恢复默认提醒
- 发布产物输出到 `发布/`（已被 .gitignore 忽略，不进仓库）
- Git 钩子位于 `.githooks/`，用 `git config core.hooksPath .githooks` 启用（pre-commit 含敏感信息/大文件/空白检查）
- 与 GitHub 交互统一使用 gh CLI
