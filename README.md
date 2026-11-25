# PCL.Avalonia

English | [简体中文](README.zh-CN.md)

A third-party cross-platform port of [Plain Craft Launcher 2](https://github.com/Meloong-Git/PCL) (based on PCL 2.14), built with **Avalonia 12.1.4 + .NET 10**. The UI and business logic are implemented in VB.NET, targeting Linux (Wayland / X11) and Windows.

- Current release: **Stable 14.2.2** ([Releases](https://github.com/SuyihQwQ/PCL.Avalonia/releases))
- This is an **independent third-party port**, not an official PCL release and not affiliated with Mojang or Microsoft.

## Features

- Install, launch and isolate vanilla Minecraft and mod loaders (Forge / NeoForge / Fabric / Quilt / OptiFine, etc.)
- Modpack installation (drop `zip` / `mrpack` files), instance import/export
- Mods, resource packs, shaders and datapacks from Modrinth and CurseForge (MCIM mirror when no API key is available)
- Offline, Authlib-Injector, Nide and Microsoft login; skin management
- Automatic Java detection and download (Azul Zulu), memory and JVM argument configuration
- Network proxy (system / SOCKS5 / HTTP), bundled fonts, music player, themes and Chinese/English UI

## Clone and build

```bash
# The submodule (MeloongCore.Avalonia) must be cloned as well
git clone --recursive https://github.com/SuyihQwQ/PCL.Avalonia.git
cd PCL.Avalonia

# If you cloned without --recursive:
git submodule update --init --recursive

# Build and run
dotnet build PCL.sln
dotnet run --project PCL/PCL.vbproj
```

- Requires the **.NET 10 SDK**; Linux needs a Wayland or X11 desktop
- After changing an `x:Name` in `.axaml`, run `python3 tools/XamlGen/gen_fields.py` (Python 3 required)
- Tests: `dotnet test PCL.Tests/PCL.Tests.csproj` (xunit.v3 + Avalonia.Headless, no display server needed)

## Publish self-contained builds

```bash
# Linux (executable: pcl-avalonia)
dotnet publish PCL/PCL.vbproj -c Release -r linux-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true -o dist/package/linux-x64

# Windows (can be cross-published from Linux; produces PCL.Avalonia.exe)
dotnet publish PCL/PCL.vbproj -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:EnableWindowsTargeting=true \
  -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true \
  -o dist/package/win-x64
```

Release binaries are GPG-signed: `sha256sum -c SHA256SUMS && gpg --verify SHA256SUMS.asc SHA256SUMS`; public key: `PCL.Avalonia-signing-key.asc`.

## Branches and credentials

- `main`: stable. The Microsoft OAuth Client ID is included in the source; the CurseForge API key is injected at build time via `PCL_CURSEFORGE_API_KEY` and never committed.
- `dev`: snapshot builds. Client ID and the optional CurseForge key are read from `PCL_MS_CLIENT_ID` and `PCL_CURSEFORGE_API_KEY`; snapshot builds do not enforce the genuine-account requirement.

Without a CurseForge key, CurseForge requests use the MCIM mirror (`mod.mcimirror.top`).

## Project layout

| Path | Description |
| --- | --- |
| `PCL/` | Launcher application (Avalonia UI and business logic) |
| `PCL/Controls/` | Custom controls (`MyButton`, `MyListItem`, `MyCard`, etc.) |
| `PCL/Modules/` | Launching, downloads, Java, resource search, proxy, animation, localization |
| `PCL/Pages/` | Page views and code-behind |
| `PCL/Assets/` | Images, bundled fonts and language packs |
| `PCL/Generated/` | Control fields generated from XAML (do not edit manually) |
| `PCLCS/` | Minecraft version JSON, launch arguments and Java compatibility |
| `PCL.Compat/` | Avalonia / platform compatibility layer |
| `MeloongCore.Avalonia/` | Shared infrastructure (git submodule, project-specific fork) |
| `PCL.Tests/` | Headless regression tests |
| `tools/XamlGen/` | Generates `PCL/Generated/*.g.vb` |
| `reference/` | Original PCL sources for reference (read-only, ignored by git) |

## Configuration and data

The launcher is portable by default and stores data next to the executable (or next to the AppImage). Use `--working-dir <path>` to override the base directory:

- Data folder: `<program folder>/PCL.Avalonia/`
- Default Minecraft folder: `<program folder>/.minecraft/`
- Linux settings: `~/.config/PCL.Avalonia/Registry.ini` and `config.json`
- Windows settings: `HKCU\Software\PCL.Avalonia`
- Temporary files: `PCL/` under the system temp directory

The UI language follows the system language by default; Simplified Chinese and English are available. Stable builds require a genuine Microsoft account login first; snapshot builds do not.

## Linux notes

- Follows the GTK / KDE / GSettings system cursor theme
- Client-side decorations on Wayland; instance folders watched through a separate window listener on X11
- File drag-and-drop uses the `Copy` effect (Avalonia Wayland backend limitation)
- Installs a generated `PCL.Avalonia.desktop` entry

## Future plans

1. macOS support (builds, `.app`/dmg packaging and system integration)
2. Rewrite the VB.NET part in C#
3. ConnectX-powered multiplayer support ([ConnectX](https://github.com/Corona-Studio/ConnectX))

> Plans only; implementation has not started.

## License and credits

- Original PCL: Copyright © Chengdu Guapilong Technology Co., Ltd., developed by LTCat (龙腾猫跃). This is a non-official port; it follows the [LICENCE](LICENCE) and the PCL Repository Reasonable Use Guide, publishes its source, and keeps the original author's attribution and sponsorship link first on the About page.
- PCL-derived code (`PCL/`, `PCLCS/`) is covered by [LICENCE](LICENCE); new code (`PCL.Compat/`, `MeloongCore.Avalonia/`, `PCL.Tests/`, `tools/XamlGen/`) is under the [Apache License 2.0](LICENSE-APACHE) — see [NOTICE](NOTICE) for the full scope.
- Licenses and attribution for third-party fonts, icons, NuGet packages and packaging tools are listed in [THIRD-PARTY-LICENSES.md](THIRD-PARTY-LICENSES.md) (including the MiSans license notice).
