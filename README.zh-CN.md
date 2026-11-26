# PCL.Avalonia

[English](README.md) | 简体中文

[Plain Craft Launcher 2](https://github.com/Meloong-Git/PCL)（基于 PCL 2.14 源码）的第三方跨平台移植版本：**Avalonia 12.1.4 + .NET 10**，主体以 VB.NET 实现。正式支持 Windows 与 Linux（Wayland / X11），macOS 可构建运行（实验性），BSD 提供源码级支持。

- 当前版本：**Stable 14.2.2**（下载见 [Releases](https://github.com/SuyihQwQ/PCL.Avalonia/releases)）
- 本作品是**第三方基于 PCL 独立进行的移植**，不是官方作品，也与 Mojang / Microsoft 无关。

## 功能

- 原版与模组加载器（Forge / NeoForge / Fabric / Quilt / OptiFine 等）安装、启动与版本隔离
- 整合包安装（拖入 `zip` / `mrpack`）、实例导入导出
- Mod / 资源包 / 光影 / 数据包下载与更新（Modrinth、CurseForge，后者无 Key 时走 MCIM 镜像）
- 离线、Authlib-Injector、Nide 与微软正版登录，皮肤管理
- Java 自动搜索与下载（Azul Zulu）、内存与 JVM 参数配置
- 网络代理（系统代理 / SOCKS5 / HTTP）、内嵌字体、音乐播放器、主题与中英文界面

## 克隆与构建

```bash
# 子模块必须一起克隆（MeloongCore.Avalonia）
git clone --recursive https://github.com/SuyihQwQ/PCL.Avalonia.git
cd PCL.Avalonia

# 若克隆时未加 --recursive：
git submodule update --init --recursive

# 构建并运行
dotnet build PCL.sln
dotnet run --project PCL/PCL.vbproj
```

- 需要 **.NET 10 SDK**；Windows 无需额外依赖，Linux 需要 Wayland 或 X11 桌面，macOS 使用原生后端
- macOS / BSD 的构建与运行说明见下文「平台说明」
- 修改 `.axaml` 的 `x:Name` 后需运行 `python3 tools/XamlGen/gen_fields.py`（需要 Python 3）
- 测试：`dotnet test PCL.Tests/PCL.Tests.csproj`（xunit.v3 + Avalonia.Headless，无需显示服务器）

## 发布自包含版本

```bash
# Linux（可执行文件名：pcl-avalonia）
dotnet publish PCL/PCL.vbproj -c Release -r linux-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true -o dist/package/linux-x64

# Windows（可在 Linux 上交叉发布，产物：PCL.Avalonia.exe）
dotnet publish PCL/PCL.vbproj -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:EnableWindowsTargeting=true \
  -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true \
  -o dist/package/win-x64

# macOS（Apple Silicon / Intel，可跨平台发布；产物是可执行文件，暂无 .app 打包）
dotnet publish PCL/PCL.vbproj -c Release -r osx-arm64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true -o dist/package/osx-arm64

dotnet publish PCL/PCL.vbproj -c Release -r osx-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true -o dist/package/osx-x64
```

发布产物使用 GPG 签名：`sha256sum -c SHA256SUMS && gpg --verify SHA256SUMS.asc SHA256SUMS`，公钥见 `PCL.Avalonia-signing-key.asc`。

## 分支与凭据

- `main`：稳定版；Microsoft OAuth Client ID 写入源码，CurseForge API Key 仅通过构建期环境变量 `PCL_CURSEFORGE_API_KEY` 注入（不进入仓库）。
- `dev`：快照版；Client ID / CurseForge Key 分别读环境变量 `PCL_MS_CLIENT_ID`、`PCL_CURSEFORGE_API_KEY`，快照版不强制正版验证。

未注入 CurseForge Key 时，CurseForge 请求只走 MCIM 镜像（`mod.mcimirror.top`）。

## 项目结构

| 路径 | 说明 |
| --- | --- |
| `PCL/` | 启动器主工程（Avalonia 界面与业务逻辑） |
| `PCL/Controls/` | 自定义控件（`MyButton`、`MyListItem`、`MyCard` 等） |
| `PCL/Modules/` | 启动、下载、Java、资源搜索、代理、动画、本地化等模块 |
| `PCL/Pages/` | 页面视图与代码 |
| `PCL/Assets/` | 图片、内嵌字体与语言包 |
| `PCL/Generated/` | 由 XAML 生成的控件字段（勿手改） |
| `PCLCS/` | Minecraft 版本 JSON、启动参数与 Java 兼容处理 |
| `PCL.Compat/` | Avalonia / 平台兼容层 |
| `MeloongCore.Avalonia/` | 基础库（git submodule，项目专用 fork） |
| `PCL.Tests/` | Headless 回归测试 |
| `tools/XamlGen/` | 生成 `PCL/Generated/*.g.vb` |
| `reference/` | 原版 PCL 源码参考（只读，已被 `.gitignore` 排除） |

## 配置与数据

启动器默认便携运行，数据保存在程序旁（AppImage 为文件旁），也可用 `--working-dir <路径>` 覆盖基础目录：

- 数据文件夹：`<程序目录>/PCL.Avalonia/`
- 默认游戏文件夹：`<程序目录>/.minecraft/`
- Linux 设置：`~/.config/PCL.Avalonia/Registry.ini`、`config.json`
- Windows 设置：`HKCU\Software\PCL.Avalonia`
- 临时文件：系统临时目录下的 `PCL/`

界面语言默认跟随系统，目前提供简体中文与英文。稳定版需先登录微软正版账号，快照版不强制。

## 平台说明

### Windows

- 按《PCL 存储库合理使用指南》固定龙猫蓝主题，禁用主题配色设置
- 支持内存优化、Java Launch Wrapper、系统显卡首选项等 Windows 专属功能

### Linux

- 跟随 GTK / KDE / GSettings 的系统光标主题
- Wayland 使用自绘标题栏；X11 通过独立窗口监听实例目录变化
- 文件拖放统一使用 Copy 效果（Avalonia Wayland 后端限制）
- 生成并安装 `PCL.Avalonia.desktop` 桌面项
- 系统代理依次读取环境变量、KDE `kioslaverc`、GNOME `gsettings`
- 显卡首选项通过 PRIME 环境变量实现

### macOS（实验性）

- 配置保存在 `~/Library/Application Support/PCL.Avalonia/`（其他平台为 `<程序目录>/PCL.Avalonia/` 与 XDG 配置目录）
- 使用 Avalonia 原生后端；启动错误提示通过 `osascript` 弹出
- Java 搜索包含 `/Library/Java/JavaVirtualMachines`、`~/Library/Java/JavaVirtualMachines` 以及 Homebrew / MacPorts 的 JDK
- 支持 Azul Zulu 自动下载；系统信息读取使用 `sysctl` / `system_profiler`
- 暂未提供 `.app`/dmg 打包，直接运行可执行文件即可
- Windows 专属的内存优化、Java Launch Wrapper 与显卡首选项设置在此平台禁用

### BSD（实验性，仅源码支持）

- 支持 FreeBSD / OpenBSD / NetBSD / DragonFly：配置使用 XDG 目录，系统信息通过 `sysctl` 读取
- 启动时显式使用 Avalonia X11 后端（需要系统提供 X11 与 Skia 依赖）
- Java 搜索包含 `/usr/local/openjdk*`、`/usr/local/*jdk*`、`/usr/local/share/java`、`/usr/pkg/java` 等路径；Azul Zulu 不提供 BSD 构建，自动下载会跳过
- Minecraft 规则复用 `linux` 分支
- 与 Linux 相同，禁用 Windows 专属功能；当前 .NET 与 Avalonia 未在 NuGet 提供 BSD 运行时包，因此没有官方二进制，需在 BSD 上用社区 .NET 运行时自行构建

## 未来计划

1. 完善 macOS 支持（`.app`/dmg 打包、系统集成）
2. 将 VB.NET 部分重写为 C#
3. 添加由 [ConnectX](https://github.com/Corona-Studio/ConnectX) 提供的联机支持

> 仅为规划，尚未开始实施。

## 许可与致谢

- 原版 PCL：Copyright © 成都瓜皮龙科技有限公司，开发者 LTCat（龙腾猫跃）；本项目为非官方移植，遵守 [LICENCE](LICENCE) 与《PCL 存储库合理使用指南》，公开源代码，并在“关于”页首位保留原作者署名与赞助链接。
- PCL 派生代码（`PCL/`、`PCLCS/`）遵循 [LICENCE](LICENCE)；新写代码（`PCL.Compat/`、`MeloongCore.Avalonia/`、`PCL.Tests/`、`tools/XamlGen/`）使用 [Apache License 2.0](LICENSE-APACHE)，范围见 [NOTICE](NOTICE)。
- 第三方字体、图标、NuGet 组件与打包工具的许可与署名见 [THIRD-PARTY-LICENSES.md](THIRD-PARTY-LICENSES.md)（含 MiSans 许可说明）。
