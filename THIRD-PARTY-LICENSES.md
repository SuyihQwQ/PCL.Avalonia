# 第三方组件与许可 / Third-Party Components and Licenses

本文件随源代码与发行包提供，用于满足本项目所使用第三方内容的署名与许可要求。
This file is provided with both the source tree and the release binaries to satisfy attribution and license requirements for third-party content used by this project.

> `PCL/`、`PCLCS/` 中的 PCL 派生代码适用 [LICENCE](LICENCE)（PCL 分发有限许可）；`PCL.Compat/`、`PCL.Tests/`、`tools/XamlGen/` 与 `MeloongCore.Avalonia/` 适用 [Apache License 2.0](LICENSE-APACHE)。明确标注为第三方内容的文件不纳入 PCL 许可范围。
> Code derived from PCL in `PCL/` and `PCLCS/` is covered by [LICENCE](LICENCE) (PCL Limited Distribution License). `PCL.Compat/`, `PCL.Tests/`, `tools/XamlGen/` and `MeloongCore.Avalonia/` are Apache-2.0. Files explicitly identified here as third-party content are excluded from the PCL license scope.

## 1. 基础库 / Core library

| 组件 | 来源 | 许可 | 说明 |
|---|---|---|---|
| MeloongCore.Avalonia | https://github.com/SuyihQwQ/MeloongCore.Avalonia （上游 https://github.com/Meloong-Git/MeloongCore ） | Apache-2.0 | 本项目专用的 MeloongCore fork（裁剪/适配版，git submodule），保留上游 LICENSE 与 NOTICE |

## 2. 字体 / Fonts

| 文件 | 版权与许可 | 来源 |
|---|---|---|
| `PCL/Assets/Fonts/PCLEnglish.ttf` | 原版 PCL 附带的字体资源，随 PCL 派生内容适用 PCL 许可 | 原版 PCL |
| `PCL/Assets/Fonts/MiSans-Regular.ttf` | Copyright © 小米科技有限责任公司（Xiaomi Inc.）。MiSans 允许个人及商业免费使用，**允许在软件中内嵌使用**；许可全文见 `LICENSES/MiSans-License.pdf`，说明见 `LICENSES/MiSans.md` | https://hyperos.mi.com/font |
| `PCL/Assets/Fonts/Twemoji.Mozilla.ttf` | Twemoji 图形 Copyright © Twitter, Inc. and contributors，按 CC-BY 4.0（https://creativecommons.org/licenses/by/4.0/）许可；字体由 twemoji-colr 构建（https://github.com/mozilla/twemoji-colr ）。本项目未修改字形，仅作为 emoji 回退字体嵌入 | Twemoji / twemoji-colr |

## 3. 图标与系统信息 / Icons

| 内容 | 许可 | 说明 |
|---|---|---|
| 系统信息卡片中的 OS / Linux 发行版图标 Path Data | simple-icons，CC0 1.0（https://creativecommons.org/publicdomain/zero/1.0/ ） | 仅用于标识当前操作系统；各系统与发行版图标为其各自所有者的商标，本项目不主张任何商标权利 |
| 帮助库 `PCL/Assets/Help.zip` | 来自原版 PCL 帮助库（其版权归原作者，随 PCL 内容分发） | 内容未修改，按 PCL 许可随附 |

## 4. NuGet 依赖 / NuGet dependencies（运行时随二进制分发）

| 包 | 许可 | 版权 |
|---|---|---|
| Avalonia / Avalonia.Desktop / Avalonia.Themes.Fluent / Avalonia.Wayland / Avalonia.Markup.Xaml.Loader / Avalonia.Headless.XUnit | MIT | Copyright © AvaloniaUI OÜ and contributors |
| SkiaSharp / HarfBuzzSharp | MIT | Copyright © Microsoft Corporation |
| Newtonsoft.Json | MIT | Copyright © James Newton-King |
| Microsoft.Win32.Registry | MIT | Copyright © .NET Foundation and contributors |
| PolySharp | MIT | Copyright © Sergio Pedri |
| SoundFlow | MIT | Copyright © 2025 LSXPrime |
| ThrottleDebounce | Apache-2.0 | Copyright © Ben Hutchison（见项目内 LICENSE-APACHE） |
| xunit.v3 / xunit.runner.visualstudio / Microsoft.NET.Test.Sdk | Apache-2.0 / MIT | 仅用于测试工程，不随发行包分发 |

MIT 许可全文与各组件版权行见 `LICENSES/MIT.md`；其他许可全文见 `LICENSES/` 目录（CC-BY-4.0、MiSans、Help 库、GPL-2.0 等）。

## 5. 打包工具 / Packaging tools

- Linux AppImage 由 **appimagetool** 与 **AppImage type-2 runtime**（AppImage 项目，**GPL-2.0**）生成；runtime 嵌入生成的 AppImage 产物中。GPL-2.0 全文见 `LICENSES/GPL-2.0.txt`，说明见 `LICENSES/AppImage-runtime-GPL-2.0.md`；对应源码可从 https://github.com/AppImage/AppImageKit 获取。
- .NET SDK（MIT）仅用于构建，不随产物分发。

## 6. 第三方内容的商标与免责声明 / Trademarks and disclaimer

Windows、Apple/macOS、Linux 及各 Linux 发行版名称与图标是其各自所有者的商标，仅用于表示运行环境。本项目不是 Mojang、Microsoft、Mojang Studios 或 PCL 官方产品，与上述主体无隶属关系。
Windows, Apple/macOS, Linux and Linux distribution names/logos are trademarks of their respective owners and are used only to identify the running environment. This project is not an official product of Mojang, Microsoft, Mojang Studios or the PCL team and is not affiliated with them.
