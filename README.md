# PCL.Avalonia

[Plain Craft Launcher 2](https://github.com/Meloong-Git/PCL)（PCL 2.14）的跨平台移植版本，使用 **Avalonia 12.1.4 + .NET 10**，界面与业务逻辑以 VB.NET 重写，主要面向 Linux（Wayland / X11）。

> PCL.Avalonia 是第三方基于原版 PCL 独立进行的移植，并非官方作品。
> 本项目不是 Minecraft 官方产品，未经 Mojang 或 Microsoft 批准，也与它们无关联。

## 功能

- Minecraft 原版与模组加载器（Forge / NeoForge / Fabric / Quilt / OptiFine 等）安装、启动与版本隔离
- 整合包安装（支持拖入 `zip` / `mrpack` 文件）、实例导入导出
- Mod / 资源包 / 光影 / 数据包下载与更新，支持 Modrinth 与 CurseForge
- 离线、Authlib-Injector、Nide 与 Microsoft 正版登录，皮肤管理
- Java 自动搜索与下载、内存与 JVM 参数配置
- 音乐播放器、主题与界面设置、中英文语言切换

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

发布单目录版本：

```bash
dotnet publish PCL/PCL.vbproj -c Release -r linux-x64 --self-contained false -o dist
```

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

Microsoft 登录与 CurseForge 需要自行提供凭据，通过环境变量读取：

| 环境变量 | 用途 |
| --- | --- |
| `PCL_MS_CLIENT_ID` | Microsoft OAuth 应用（客户端）ID |
| `PCL_CURSEFORGE_API_KEY` | CurseForge API Key |

未设置时对应功能不可用，其余功能不受影响。

用户数据保存在 `~/.local/share/PCL/`，临时文件位于系统临时目录下的 `PCL/`。

## Linux 说明

- 启动时根据 GTK / KDE / GSettings 配置应用系统光标主题
- Wayland 下使用自绘标题栏，避免启动时闪现服务端标题栏
- Avalonia 的 Wayland 后端不支持 `DragDropEffects.Link`，因此文件拖放统一使用 `Copy` 效果
- X11 下通过独立的窗口监听实现实例文件夹变更监控

## 许可与致谢

- 原版 PCL：Copyright © 成都瓜皮龙科技有限公司（计算机软件著作权登记号：2020SR0875133），开发者龙腾猫跃
- PCL.Avalonia：Copyright © 2026 SuyihQwQ
- 本项目遵循仓库中的 [LICENCE](LICENCE)（《PCL 分发有限许可》及其合理使用指南），并作为第三方移植作品公开源代码
