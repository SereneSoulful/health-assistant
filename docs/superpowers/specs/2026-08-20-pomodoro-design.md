# 健康助手 · 番茄钟功能设计

日期：2026-08-20
状态：已批准（经 brainstorming 确认）

## 目标

在现有 WPF 像素风健康助手中新增番茄钟：独立置顶小窗、半自动阶段衔接（阶段结束弹窗确认）、可自定义时长、后台继续计时、xUnit 单元测试，并刷新单文件发布。核心逻辑做成可注入时钟的纯状态机，空闲零轮询红线不变。

## 关键行为

- 阶段：空闲 → 专注 → 短休息/长休息循环。
- 专注自然结束：完成数 +1；达到 `FocusCyclesBeforeLongBreak`（默认 4）后下一阶段为长休；长休自然结束后完成数归零，下一阶段为专注。
- 阶段自然结束不自动开始：弹出阶段结束确认窗，点「开始下一阶段」「跳过休息」或「停止」。
- 暂停/继续：暂停冻结剩余时间，继续时按剩余时间重算绝对结束点。
- 跳过：跳过专注 → 立即短休且不计入完成数；跳过休息 → 立即专注且不重置周期。
- 停止：结束会话回空闲，完成数清零。
- 运行中关闭小窗 = 隐藏、后台继续计时；重启应用后会话回空闲（仅设置持久化）。

## 界面

- `PomodoroWindow`：独立置顶像素小窗（约 460×400），可拖动；显示阶段名、MM:SS 大字、20 格像素进度块（专注红 `AccentBrush` / 短休绿 `GreenBrush` / 长休深绿 `DeepGreenBrush`）、「第 N 个番茄」状态行；按钮：开始/暂停/继续（单按钮切换）、跳过、停止、设置；窗口可见时才运行 1 秒 `DispatcherTimer`，隐藏即停。
- `PomodoroSettingsWindow`：工作 1–120、短休 1–30、长休 1–60、长休周期 1–12 的整数输入；保存即写入配置，只影响之后开始的阶段。
- `PomodoroPhaseWindow`：仿 `ReminderPopupWindow` 的阶段结束确认弹窗（右下角堆叠），按钮与文案由 App 按下一阶段生成。

## 配置与兼容

- `AppConfig.Version` 2→3；新增 `Models/PomodoroSettings.cs`（`WorkMinutes=25`、`ShortBreakMinutes=5`、`LongBreakMinutes=15`、`FocusCyclesBeforeLongBreak=4`）。
- `ReminderStore.Migrate` 对 `Version < 3` 补默认番茄设置并保存一次；`CreateDefault()` 直接 `Version=3`。
- 运行中的番茄会话不持久化，仅时长设置持久化。

## 集成与性能

- `PomodoroEngine` 仿 `ReminderScheduler`：注入 `Func<DateTime> clock`，暴露绝对结束点 `PhaseEndAt`，事件 `StateChanged` / `PhaseEnded(PomodoroPhaseEndedInfo)`。
- App 持有一只仅按 `PhaseEndAt` 到点的 `System.Threading.Timer`，空闲为 `Timeout.Infinite`，不引入轮询。
- `PhaseEnded` → 播放提示音 + 右下角堆叠弹窗；`StateChanged` → 刷新窗口与托盘文案、重新武装定时器。
- 入口：主窗口工具行「番茄钟」按钮 + 托盘菜单「番茄钟」；运行中托盘提示显示「专注中 mm:ss」。
- `ReminderScheduler` 不改动。

## 测试

- xUnit 测试项目 `tests/健康助手.Tests`：引擎状态机全用例 + 配置迁移用例（注入假时钟 / 临时数据目录）。
- 手动验收：构建与测试全绿；四轮 1 分钟专注 → 长休 → 归零；关窗后台到点弹窗；重启设置保留；原有提醒照常；发布目录刷新为单文件 EXE。

## 假设

- 跳过的专注不计入长休周期；跳过的休息不重置周期。
- 阶段结束音复用「提醒音」开关与 `PlayCountdownDone`。
- 番茄弹窗与提醒弹窗各自堆叠，同时出现允许重叠。
- 番茄钟使用独立的「睡到阶段结束」定时器，与提醒调度器并存；如需严格合并为单一 Timer 可留作后续重构。
