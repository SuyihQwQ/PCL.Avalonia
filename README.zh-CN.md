# PCL.Avalonia

[English](README.md) | 简体中文

[Plain Craft Launcher 2](https://github.com/Meloong-Git/PCL)（基于 PCL 2.14 源码）的跨平台移植版本，使用 **Avalonia 12.1.4 + .NET 10**，界面与业务逻辑以 VB.NET 重写，主要面向 Linux（Wayland / X11）。

当前版本：**稳定版 14.1.2（Stable 14.1.2，标签 `v14.1.2-Stable`）**。

> PCL.Avalonia 是第三方基于原版 PCL 独立进行的移植，并非官方作品。
> 本项目不是 Minecraft 官方产品，未经 Mojang 或 Microsoft 批准，也与它们无关联。

## 功能

- Minecraft 原版与模组加载器（Forge / NeoForge / Fabric / Quilt / OptiFine 等）安装、启动与版本隔离
- 整合包安装（支持拖入 `zip` / `mrpack` 文件）、实例导入导出
- Mod / 资源包 / 光影 / 数据包下载与更新，支持 Modrinth 与 CurseForge（CurseForge 使用 MCIM 镜像）
- 离线、Authlib-Injector、Nide 与 Microsoft 正版登录，皮肤管理
- Java 自动搜索与下载（Azul Zulu）、内存与 JVM 参数配置
- 音乐播放器、主题与界面设置、中英文语言切换（默认跟随系统语言）

## 分支

- `main`：公开的稳定版源码，Microsoft OAuth Client ID 写入源码。CurseForge API Key 可在构建时通过 `PCL_CURSEFORGE_API_KEY` 注入，不会进入仓库；未注入时 CurseForge API 请求只走 MCIM 镜像。稳定版发布产物由另一个私密仓库构建。
- `dev`：快照版，Microsoft OAuth Client ID 与可选的 CurseForge API Key 分别从环境变量 `PCL_MS_CLIENT_ID`、`PCL_CURSEFORGE_API_KEY` 读取，也支持构建时注入 CurseForge Key；快照版不强制正版验证。

## 未来计划

- 优化 macOS 支持。
- 完全重写为 C#。

## 环境要求

- .NET 10 SDK
- Linux 桌面环境（Wayland 或 X11）
- 构建时需要 Python 3（仅重新生成 XAML 字段时用到）

## 构建与运行

```bash
# 还原并构建全部工程
dotnet build PCL.sln

# 运行启动器
dotnet run --project PCL/PCL.vbproj
```

发布自包含单文件版本：

```bash
# Linux（独立可执行文件命名为 pcl-avalonia）
dotnet publish PCL/PCL.vbproj -c Release -r linux-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true -o dist/package/linux-x64

# Windows（可在 Linux 上交叉发布）
dotnet publish PCL/PCL.vbproj -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:EnableWindowsTargeting=true \
  -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true \
  -o dist/package/win-x64
```

Linux 独立发布的程序名与 AppImage 内部的程序名均为 `pcl-avalonia`；Windows 产物为 `PCL.Avalonia.exe`。

## 测试

测试基于 `xunit.v3` 与 `Avalonia.Headless`，无需显示服务器：

```bash
dotnet test PCL.Tests/PCL.Tests.csproj
```

## 项目结构

| 路径 | 说明 |
| --- | --- |
| `PCL/` | 启动器主工程（Avalonia 界面与业务逻辑） |
| `PCL/Controls/` | 自定义控件（`MyButton`、`MyListItem`、`MyHint` 等） |
| `PCL/Modules/` | 核心模块：启动、下载、Java、资源搜索、动画、本地化等 |
| `PCL/Pages/` | 各页面视图与代码 |
| `PCL/Assets/` | 图片、字体与语言包（`Assets/Language/*.json`） |
| `PCL/Generated/` | 由 XAML 生成的控件字段，请勿手改 |
| `PCL.Tests/` | Headless 界面与逻辑回归测试 |
| `PCLCS/` | Minecraft 版本 JSON、启动参数与 Java 兼容处理 |
| `PCL.Compat/` | 平台兼容层 |
| `MeloongCore.Shared/` | 日志、网络、加载器等基础库 |
| `tools/XamlGen/` | 生成 `PCL/Generated/*.g.vb` 的脚本 |
| `reference/` | 原版 PCL 源码参考（本地只读，已被 `.gitignore` 排除） |

## XAML 字段生成

界面控件字段由脚本统一生成，新增或重命名 `x:Name` 后需要重新生成：

```bash
python3 tools/XamlGen/gen_fields.py          # 重新生成
python3 tools/XamlGen/gen_fields.py --check  # 仅校验是否最新
```

## 配置与凭据

启动器默认以便携模式运行，数据保存在可执行文件旁（使用 AppImage 时为 AppImage 文件旁）：

- 数据文件夹：`<程序目录>/PCL.Avalonia/`
- 默认游戏文件夹：`<程序目录>/.minecraft/`
- 可执行文件：Windows 为 `PCL.Avalonia.exe`，Linux 独立版本与 AppImage 内部均为 `pcl-avalonia`
- Linux 设置（替代注册表）：`~/.config/PCL.Avalonia/Registry.ini`
- Java 扫描缓存与其他启动器配置：`~/.config/PCL.Avalonia/config.json`
- Windows 设置：`HKCU\Software\PCL.Avalonia`
- 临时文件：系统临时目录下的 `PCL/`

使用 `--working-dir <路径>` 或 `--working-dir=<路径>` 可以覆盖数据文件夹与默认 `.minecraft` 的基础目录，适用于启动器安装在 `/usr/bin`、`/opt` 等只读位置的情况。当前选中的游戏文件夹内会保存各自的 `PCL.ini`。

数据文件夹创建失败时（例如同名文件已存在、目录只读等），启动器会提示与原版相同的运行环境错误并退出，不会回退到其他目录。

### 构建时凭据

| 分支 | Microsoft Client ID | CurseForge API Key |
| --- | --- | --- |
| `main` | 写入源码 | 构建时通过 `PCL_CURSEFORGE_API_KEY` 注入 |
| `dev` | 运行时读取 `PCL_MS_CLIENT_ID` | 构建时注入或运行时读取 `PCL_CURSEFORGE_API_KEY` |

稳定版可以在不把 Key 写入仓库的情况下于构建时注入：

```bash
PCL_CURSEFORGE_API_KEY="<你的 Key>" dotnet publish PCL/PCL.vbproj -c Release ...
```

Key 只会写入 `obj/` 下的临时生成文件并编译进产物，不能提交到仓库。未提供 Key 时，CurseForge API 请求走 MCIM 镜像（`mod.mcimirror.top`）。`dev` 分支中构建时注入优先于运行时环境变量。构建稳定版的私密仓库也可以通过该环境变量提供 Key。旧的 `ModSecret.Private.vb` 文件不再被构建读取。

界面语言默认跟随系统，目前仅支持简体中文与英文，其他系统语言会回退为简体中文。

稳定版需要至少登录一个正版 Microsoft 账号，之后才能使用其他登录方式。快照版（`dev`）不强制该校验，其 Microsoft OAuth Client ID 与可选的 CurseForge API Key 从环境变量 `PCL_MS_CLIENT_ID`、`PCL_CURSEFORGE_API_KEY` 读取。

## 发布签名

发布产物使用 GPG 签名，可通过以下命令校验：

```bash
sha256sum -c SHA256SUMS
gpg --verify SHA256SUMS.asc SHA256SUMS
```

公钥随版本发布，文件名为 `PCL.Avalonia-signing-key.asc`。

## Linux 说明

- 启动时根据 GTK / KDE / GSettings 配置应用系统光标主题
- Wayland 下使用自绘标题栏，避免启动时闪现服务端标题栏
- Avalonia 的 Wayland 后端不支持 `DragDropEffects.Link`，因此文件拖放统一使用 `Copy` 效果
- X11 下通过独立的窗口监听实现实例文件夹变更监控
- 生成并安装名为 `PCL.Avalonia.desktop` 的桌面项

## 许可与致谢

- 原版 PCL：Copyright © 成都瓜皮龙科技有限公司（计算机软件著作权登记号：2020SR0875133），开发者 LTCat（龙腾猫跃）
- PCL.Avalonia：Copyright © 2026 SuyihQwQ
- PCL 派生代码（`PCL/`、`PCLCS/`）遵循 [LICENCE](LICENCE)（《PCL 分发有限许可》及其合理使用指南）。
- 完全新写的代码（`PCL.Compat/`、`MeloongCore.Shared/`、`PCL.Tests/`、`tools/XamlGen/`）使用 [Apache License 2.0](LICENSE-APACHE)，这些目录中的源文件带有 SPDX 标识；完整范围见 [NOTICE](NOTICE)。
- 本项目为公开源代码的第三方移植作品。
