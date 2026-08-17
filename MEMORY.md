# MEMORY.md

## 项目进展

- 完成健康提醒核心功能：远眺（20 分钟）+ 喝水（60 分钟）默认提醒、自定义提醒、倒计时弹窗、托盘常驻、单实例、开机自启
- 像素风 UI 改造完成：暖色纸面桌游风 8-bit，内嵌 Fusion Pixel 中文字体
- 像素风图标定稿：暖纸卡片「远眺之眼」（青色远山 + 琥珀暖阳），`tools/generate_icon.py` 可重新生成，旧版备份在 `tools/app.ico.bak-20260816`

## 踩坑记录

- 字体文件较大（约 6.8MB），作为 Resource 内嵌进程序集，用 `<Resource Include>` 声明
