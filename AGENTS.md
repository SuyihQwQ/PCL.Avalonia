# PCL.Avalonia — 编码代理指南

本文件由 DSH 等编码代理自动加载。人类文档见 [README.md](README.md)（英文）与 [README.zh-CN.md](README.zh-CN.md)。

## 项目速览

- PCL 2.14（VB.NET / WPF / .NET Framework 4.8）的跨平台移植：**Avalonia 12.1.4 + .NET 10**，主程序保持 **VB.NET**（`Option Strict Off`）。C# 仅用于 `PCL.Compat/`、`MeloongCore.Avalonia/`(submodule)、`PCLCS/`、`PCL.Tests/`、`tools/XamlGen/`。
- 当前公开版本：**Stable 14.2.2**（标签 `v14.2.2-Stable`）；`main` = stable，`dev` = snapshot。
- 本仓库是**第三方非官方移植**，不是 Mojang / Microsoft 官方产品。

## 功能区速览（2026-11 新增）

- **字体**：`PCL/Assets/Fonts/` 内嵌 `PCLEnglish.ttf`、`MiSans-Regular.ttf`、`Twemoji.Mozilla.ttf`（许可说明见同目录 `FontLicenses.txt`）；`PCL/FontConfig.vb` 统一管理内嵌字体链、emoji 回退（Twemoji）与“完全使用系统字体”（设置项 `UiSystemFont`，运行时通过改写 `PCLFont` 动态资源生效，emoji 回退在重启后完全生效）。改字体时同时改 `FontConfig.EmbeddedFamily` 与 `App.axaml` 的 `PCLFont`。
- **启动脚本导出**：版本设置 → 概览 → 高级选项的“导出启动脚本”按平台生成：Windows 为 `.bat`（与原版一致），Linux/macOS 为可执行 Bash `.sh`（`ModLaunch.BuildLaunchShellScript`/`LatestLaunchScriptPath`，UTF-8 + 执行权限）。导出内容包含 PATH、appdata、全局/实例环境变量、Linux 独显环境变量、包装命令（mangohud 等）与启动前自定义命令。崩溃报告附带最近一次启动脚本，Windows 名为 `启动脚本.bat`、Unix 为 `启动脚本.sh`。
- **网络代理**：`PCL/Modules/Base/ModProxy.vb`（类型 `NetProxyType`：0 系统代理/1 无/2 SOCKS5/3 HTTP；地址端口用户名密码；系统代理在 Linux 依次读环境变量、KDE `kioslaverc`、GNOME `gsettings`，macOS 读 `scutil --proxy`，Windows 读系统代理）+ `PCL/Modules/Base/ModNet.vb`（`NetRequestCategory` + `CreateHttpHandler`/`GetSharedHttpClient`，SOCKS5 用 `PCL.Compat/Socks5Proxy.cs` 的 ConnectCallback）。五个分类开关（下载/主页/更新/登录/其他，默认全开）；**默认类型为“不使用代理”**；代理仅作用于启动器自身，游戏需自行设置 JVM 参数。设置页在“设置 → 其他 → 网络代理”，自定义类型才显示地址/认证。
- **Windows 主题限制**：按《PCL 存储库合理使用指南》，Windows 版固定龙猫蓝、不允许修改主题配色（`ModSecret.ThemeRefresh` 强制 0；`PageSetupUI` 在 Windows 下禁用主题单选框并显示提示）。不要移除该限制。
- **百宝箱系统信息卡片**：`PCL/Modules/Base/ModSystemInfo.vb` + `PageOtherTest.axaml`，位于“实用工具”之前、始终展开，fastfetch 风格；OS logo 为 simple-icons（CC0）Path Data，Linux 发行版按 `/etc/os-release` 匹配、未知回退 Tux。跨平台信息读取全部有“未知”兜底与超时，不要在 UI 线程读磁盘。
- **帮助页**：`PageOtherHelp`/`PageOtherHelpDetail` 实现 `IRefreshable`（刷新重解压/重载），非简体中文界面显示“帮助库目前只支持简体中文”提示。帮助内容 `Help.zip` 与原版 2.14 逐字节一致，不需要“迁移内容”。
- **翻译**：`PCL/Assets/Language/zh-CN.json` / `en-US.json` 两侧 key 必须一致、en-US 不得含 CJK；新增界面文案必须同步加两个 JSON（`LocalizationCoverageTests` 会检查 XAML/VB 里引用的 `Lang*` 是否都存在）。大量硬编码中文尚未迁移，清单在迁移报告的 `/tmp/translation-hardcoded.md`（临时文件，可重新生成）。

## 常用命令

```bash
dotnet build PCL.sln                              # 全量构建
dotnet run --project PCL/PCL.vbproj               # 运行启动器
dotnet test PCL.Tests/PCL.Tests.csproj            # xunit.v3 + Avalonia.Headless，无需显示服务器
python3 tools/XamlGen/gen_fields.py [--check]     # 改 XAML x:Name 后重新生成 PCL/Generated/*.g.vb
```

发布/打包命令见 README 的 *Publish self-contained single-file builds* 一节；产物落在 `dist/`（已 gitignore）。

## 硬约束

- **未经用户明确要求不要 `git push`**；用户说“提交”时只做本地 commit。
- 不要提交 `bin/obj`、`dist/`、`reference/`、任何密钥或 token；日志/错误输出分享前脱敏。
- `reference/` 是原版 PCL 源码，只读，用于对照移植，不要修改。
- `PCL/Generated/*.g.vb` 由脚本生成，不要手改。
- 不要做实时桌面截图/录屏，不要向用户桌面注入模拟输入（除非用户明确授权）。
- 许可证是分范围的：`PCL/`、`PCLCS/` 归 `LICENCE`（PCL 有限分发许可）；`PCL.Compat/`、`MeloongCore.Avalonia/`(submodule)、`PCL.Tests/`、`tools/XamlGen/` 归 Apache-2.0（文件头 SPDX），范围见 `NOTICE`。不要给 PCL 派生文件换许可证，也不要删掉原版署名与“非官方”声明。

## 凭据与版本策略

- `main`：Microsoft OAuth Client ID 直接写在 `PCL/Modules/ModSecret.vb`；CurseForge API Key 只能构建期通过 `PCL_CURSEFORGE_API_KEY` 注入（只写 `obj/` 下的生成文件），**绝不进入受版本控制的文件**。无 Key 时 CurseForge 走 MCIM 镜像（`mod.mcimirror.top`）。
- `dev`：Client ID 和 CF Key 读环境变量 `PCL_MS_CLIENT_ID` / `PCL_CURSEFORGE_API_KEY`（构建期注入优先）。
- `IReallyNeedOffline`：在数据文件夹的 `Setup.ini`（如 `<程序目录>/PCL.Avalonia/Setup.ini`）写 `IReallyNeedOffline:True`，可让稳定版跳过“必须登录正版”的强制校验。购买正版提示完全沿用原版 `ModLaunch` 逻辑、不区分构建类型：非 MS 登录且未 `HintBuy` 时，中文系统在 `SystemLaunchCount` 为 3/8/15/30/… 弹提示；非中文系统的离线（Legacy）登录会弹“购买正版/试玩(`--demo`)/返回”对话框；登录过一次正版账号后写 `HintBuy=True`。
- 稳定版（`BuildTypes.Release`）强制至少登录一个微软正版账号（EULA 之后 `RequirePremiumLogin`，启动游戏前再校验）；快照版不强制。改动启动/登录流程时不要破坏这条。

## 技术坑

- `Imports System.Runtime.InteropServices` 会撞 VB 的 `Java` / `Version` 类型名（BC30182），请用全限定名。
- 资源 URI 形如 `avares://PCL.Avalonia/...`，依赖程序集名 `PCL.Avalonia`；改名需全量替换。
- 非单文件构建用 `UseAppHost=false`；Linux 发布后 apphost 由 `RenameLinuxExecutable` 改名为 `pcl-avalonia`。配置目录固定为 `<程序目录>/PCL.Avalonia/`，**没有回退目录**。
- `MyCard`/`MyComboBox`/`MyTextBox`/`MySlider`/`MyScrollViewer`/`MyLoading` 承担悬停、阴影、下拉、焦点联动，改动后跑对应 headless 测试；指针捕获会让 `MouseLeave` 误触发，卡片内有 `SetHoverLock` 与延迟补偿。
- 固定用 `dotnet`（.NET 10）。测试总数 **289**，headless 并行清理偶发失败，重跑一般即过。
- `PCL/Pages/PageSetup/Settings.vb` 的 `New List(Of Setting) From {...}` 集合初始化器内**不能出现独占一行的注释**（VB 会 BC30201 并级联一堆 BC30035），注释请挂在条目行尾。
- 内嵌字体使程序集变大，`dotnet publish` 单文件压缩会明显变慢（首次几分钟），属正常。
- 改 `.axaml` 的 `x:Name` 后必须 `python3 tools/XamlGen/gen_fields.py`，否则运行期 `FindControl` 抛“找不到控件”。
- 新建 `JsonConfigProvider` 一律用 `JsonConfigProvider.GetShared(path)`：同路径共享缓存，否则同一版本的多个 `McInstance` 会各持过期副本，导致页面里刚设置的“使用指定的 Java”等单实例配置在启动时读不到。
- 网络请求新增入口时，记得用 `ModNet.CreateHttpHandler(Category)` / `NetRequestByClient(..., Category:=...)` 传入合适分类，否则代理开关对它不生效。启动器自身请求失败的提示应引导用户到“设置 → 其他 → 网络代理”；会打开浏览器（反馈、网页登录）的提示才保留 VPN 说法。

## 历史交接

本项目从 qodercli 的两个连续会话迁移而来（2026-11-23 迁移到 DSH）：

- **起点会话** `267922d7-…`（2026-10-25 → 11-04）：可行性验证、里程碑 1 方案、批量移植、Linux 适配、GPG 签名与打包。
  - `.agents/handoff/qodercli-267922d7.md` — 时间线、架构决策、结束状态、踩坑
  - `.agents/handoff/qodercli-267922d7-compaction-summary.md` — 逐文件细节
  - `.agents/handoff/pcl-avalonia-milestone1-plan.md` — 移植方案原文（架构蓝图）
- **接续会话** `43f9dc11-…`（2026-11-04 → 11-07）：Snapshot 14.1.0、界面细节、Zulu、语言跟随、Stable 14.1.2、分支/凭据/许可证、AppID 申诉。
  - `.agents/handoff/qodercli-43f9dc11.md` — 时间线、决策、待办、迁移过程
  - `.agents/handoff/qodercli-43f9dc11-compaction-summary.md` — 逐文件细节

更高的细节在各自 `-compaction-summary.md`；这些是本地迁移存档，尚未提交，不需要时可直接删除。
