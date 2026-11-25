# PCL 2.14 → Avalonia 12 / .NET 10：里程碑 1 迁移方案（存档）

> 来源：qodercli 会话 `267922d7-…`（2026-10-25）规划模式产出的方案文档，原文存于 `~/.qoder/plans/`（7 份内容相同的快照）。
> 这是整个移植工程的架构蓝图，当前代码结构基本沿用该方案。原文未做内容改动（路径均为相对路径）。
> 迁移存档时间：2026-11-23。

---

# PCL 2.14 → Avalonia 12 / .NET 10：里程碑 1（可运行骨架 + 首个纵向切片）

## Context

`PCL-2.14.0.0` 是 PCL 2.14 的源码快照：VB.NET + WPF + .NET Framework 4.8，113 个 `.vb` / 63 个 `.xaml` / 约 4.9 万行，UI 全部自绘（51 个自定义控件 + 自研动画引擎），无 MVVM、无数据绑定，弹窗用嵌套消息泵做同步模态。本机是 Linux，原项目无法编译运行。

目标是把主程序迁移到 **Avalonia 12.1.4 + .NET 10，保留 VB.NET**。本轮只交付**可运行骨架 + 首个页面切片**，为后续全量迁移打通所有高风险基础设施。

### 已实测确认（决定方案可行性）

| 验证项 | 结果 |
|---|---|
| VB 项目引用 Avalonia 12.1.4 并编译 `.axaml` | 通过（XamlX 在 IL 层织入，不需要 C# 代码生成） |
| VB 类作为 `x:DataType` 的编译绑定 | 通过 |
| `AvaloniaXamlLoader.Load(Me)` + `FindControl(Of T)` 运行时 | 通过（headless 输出 `TEXT=hello-from-vb`） |
| `Dispatcher.RunJobs(DispatcherPriority?)` | 存在（嵌套泵方案可行） |
| `Window.BeginMoveDrag` / `BeginResizeDrag` / `WindowDecorationProperties.ElementRole` | 存在（自绘窗口有原生支持） |
| MeloongCore | Apache-2.0；`Shared` 27 文件可直接复用，`Wpf` 6 文件需重写 |
| PCLCS | 零 WPF 引用，去掉 `MeloongCore.Wpf` 即可 |
| 本机环境 | Wayland (`DISPLAY=:0`) + Noto Sans CJK SC，可真机截图验收 |

### 关键限制

- Avalonia 名称生成器只支持 C#，不会生成 `x:Name` 字段 → **必须用生成器补上**，否则 448 处 `Handles` 和大量字段引用全断。
- Avalonia 12 没有 `DispatcherFrame` / `ComponentDispatcher`，`MyMsgBox` 的同步模态需要自建泵。
- Avalonia 12 官方只发布 net8.0 / net10.0 资产，本轮用 net10.0。

## 目标结构

原 `Plain Craft Launcher 2/` 与 `PCLCS/` 保持不动，作为只读参考。新树独立成 fork：

```
PCL-2.14.0.0/
├─ Plain Craft Launcher 2/          # 原 WPF 项目，只读参考
├─ PCL-2.14.0.0/
└─ Avalonia/
   ├─ PCL.sln
   ├─ Directory.Build.props          # TFM / LangVersion / Avalonia 版本统一
   ├─ MeloongCore.Shared/            # C# net10.0，从子模块复制（Apache-2.0，保留 LICENSE）
   ├─ PCLCS/                         # C# net10.0，从根目录复制后去 WPF 依赖
   ├─ PCL.Compat/                    # C# net10.0，替代 MeloongCore.Wpf
   ├─ PCL/                           # VB net10.0，主程序
   │  ├─ Program.vb / App.axaml(.vb)
   │  ├─ Themes/                     # 颜色资源 + 基础样式（由 Application.xaml 移植）
   │  ├─ Controls/ Pages/ Modules/   # 逐批移植
   │  ├─ Generated/                  # XamlGen 产物（入库）
   │  └─ Assets/                     # 图标、图片、字体
   ├─ tools/XamlGen/                 # Python 工具：.xaml→.axaml 转换辅助 + 字段生成
   └─ PCL.Tests/                     # C# + Avalonia.Headless.XUnit
```

## 技术决策

**1. 语言：保留 VB.NET。** C# 只用于 `PCL.Compat`、`MeloongCore.Shared`、tests、工具。业务代码只改 API 不改语法。

**2. `x:Name` 字段：XamlGen 生成 VB partial。**
- 解析 `.axaml`，输出 `*.g.vb`：`Friend WithEvents X As T` + `InitControls()`（调用 `AvaloniaXamlLoader.Load(Me)` 后逐项 `FindControl(Of T)`，缺失即抛）。
- 方法名用 `InitControls`，**不要用 `InitializeComponent`**，避免与 XamlX 织入成员冲突。
- 字段必须 `Friend`（`FrmMain.PanMsg` 等跨类访问）。
- 448 处 `Handles`：其中 `Handles Me.*` 原样可用；控件事件只需改事件名/参数类型（`MouseLeftButtonUp`→`PointerReleased`，`MouseButtonEventArgs`→`PointerPressedEventArgs`），方法体不动。
- 产物入库，`XamlGen --check` 纳入验证，防止漂移。

**3. 动画引擎：保留公开 API，换驱动。**
- `AniStart` / `AaDouble` / `AaOpacity` / `AaColor` / `AaTranslateY` / `AaScaleTransform` / `AaRotateTransform` / `AaCode` 签名全部保留（226 处调用点不动）。
- 自建线程 + `RunInUiWait` → 改为 `DispatcherTimer`（~16ms）在 UI 线程推进 `AniTimer`，消除跨线程写入与竞态。
- `DependencyProperty` → `AvaloniaProperty`；`FrameworkElement` → `Control`；`RenderTransformOrigin` → `RelativePoint.Center`。
- Transform：Avalonia 的 `RenderTransform` 只能挂一个对象，需要 `EnsureTransformGroup` 把 Scale/Rotate/Translate 合并进 `TransformGroup`，避免互相覆盖。
- 颜色：`AaColor` 结束时用 `Bind(prop, New DynamicResourceExtension(key))` 恢复主题引用，避免覆盖 `{DynamicResource}`。
- `DropShadowEffect` 不存在 → 阴影改用 `Border.BoxShadow`，`AaOpacity(DropShadowEffect)` 映射到阴影层的 Opacity。

**4. 同步模态弹窗：`RunJobs` 嵌套泵。**
- 在 `PCL.Compat/ModalityHost` 集中实现：`Do While Not done: Dispatcher.UIThread.RunJobs(DispatcherPriority.Input); Thread.Sleep(1); Loop`，配重入计数与 5s 看门狗。
- 保留 `MyMsgBox` 的同步返回值语义，104 处调用点不改。后续里程碑再评估是否异步化。

**5. 窗口自绘。**
- `WindowDecorations="None"` + `ExtendClientAreaToDecorationsHint="True"` + `TransparencyLevelHint="Transparent"` + `TransparencyBackgroundFallback` 纯色兜底。
- 八向缩放改用 `WindowDecorationProperties.ElementRole`（`TitleBar` / `ResizeN` / `ResizeSE` …）声明式处理；特殊场景才用 `BeginMoveDrag(e)` / `BeginResizeDrag(WindowEdge.X, e)`。
- 圆角 + 阴影：根 `Border CornerRadius="10" + BoxShadow`，外层留 10px 透明边（与原 `PanBack Margin=10` 一致）。
- 背景模糊：Linux 无跨平台 Acrylic，仅做半透明 + `BlurEffect` 贴图，不追求真模糊。
- 开关窗动画动内部容器，不动 `Window.Opacity`。

**6. 图片管线：Skia 重写 `MyBitmap`，保持接口。**
- `Bitmap`（Skia 解码，原生支持 WebP）替代 `System.Drawing` + `Imazen.WebP` + `libwebp64.dll` 提取逻辑。
- 裁剪/最近邻缩放/旋转用 `SKBitmap` + `SKCanvas`；保存用 `Bitmap.Save`（PNG）。
- 保持 `MyBitmap` 的构造签名与 `ImageSource`/`ImageBrush` 属性，7 个消费点无需改。

**7. 弹窗与提示。**
- `MeloongCore.Wpf.Dialogs` 23 个调用点 → `PCL.Compat/Dialogs.cs`，内部用 `TopLevel.StorageProvider` + 同步等待 shim。
- `WindowsUtils.HasAdminRole` 4 处 → 平台条件编译，Linux 返回 `false`。
- 4 个 Converter 用 C# 重写为 `IValueConverter`。

**8. XAML 方言转换规则（XamlGen 规则表）。**
`Visibility`→`IsVisible`；`Style TargetType`→`Style Selector`；`Style x:Key`→`ControlTheme`；`Triggers`→伪类选择器；`{Binding ElementName=X, Path=Y}`→`{Binding #X.Y}`；`RelativeSource`→`$parent[Type]` / `$self`；`pack://application:,,,/`→`avares://PCL/`；`Storyboard`→`Transitions`；`AdornerDecorator`→`ContentControl`；`DropShadowEffect`→`BoxShadow`；`x:Class` 补全命名空间。

## 实施步骤

### Step 0 — 工程基础（先绿）
- 建 `Avalonia/` 目录、`Directory.Build.props`、`PCL.sln`。
- 复制 `MeloongCore.Shared` → net10.0（保留 Apache LICENSE；`RegistryUtils` 加 Windows 守卫）。
- 复制 `PCLCS` → net10.0：删 `MeloongCore.Wpf` project ref/using、`Microsoft.VisualBasic` 引用；`Newtonsoft.Json` 改 PackageReference。
- 新建 `PCL.Compat`：`Dialogs.cs`、`Converters.cs`、`WindowsUtils.cs`、`ModalityHost.cs`、`PlatformInterop.cs`。
- `tools/XamlGen/gen_fields.py` + `convert_rules.py`。
- **验证**：`dotnet build Avalonia/PCL.sln` 全绿。

### Step 1 — 可启动骨架
- `PCL/PCL.vbproj`（VB net10.0，Avalonia 12.1.4，`AvaloniaNameGeneratorIsEnabled=false`，`AvaloniaUseCompiledBindingsByDefault=true`）。
- `Program.vb`、`App.axaml(.vb)`、`Assets/`（icon、PCL 字体）。
- 字体回退链：`PCL English, Noto Sans CJK SC, MiSans TC`。
- **验证**：`dotnet run` 打开空窗口；`PCL.Tests` 用 Headless 断言窗口可实例化。

### Step 2 — 基础设施
- `Modules/Base/ModBase.vb`：`RunInUi*`→`Dispatcher.UIThread`、`MyColor`→Avalonia `Color`/`SolidColorBrush`、剪贴板、DPI、文本测量、`Visibility`→`IsVisible`。
- `Modules/Base/ModAnimation.vb`：按决策 3 改造。
- `Modules/ModSecret.vb`：`ThemeRefresh` 写 `Application.Current.Resources`（`ColorBrush1..8`、`ColorObject1..8`）。
- `Themes/Colors.axaml` + `Themes/Controls.axaml`：由 `Application.xaml` 移植基础样式。
- `Modules/ModMain.vb`：先移植 `MyMsgBox` 队列 + `ModalityHost`。
- `Modules/Base/MyBitmap.vb`、`Controls/MyImage.vb`：Skia 重写。
- **验证**：Headless 测试断言主题切换后资源对象变化、动画 3 帧后 Opacity/颜色变化、模态弹窗能返回按钮序号。

### Step 3 — 窗口壳
- `FormMain.axaml(.vb)`：`PanBack` / `PanTitle` / `PanForm` / 8 个 Resizer（改 ElementRole）/ 标题栏按钮 / 页面宿主。
- `Controls/MyPageLeft.vb`、`Controls/MyPageRight.vb`（`AdornerDecorator`→`ContentControl`）。
- `Controls/MyResizer.vb`：删除，能力由 ElementRole + BeginResizeDrag 承担。
- **验证**：真机启动截图（圆角/透明/标题栏）；headless 模拟拖拽不抛异常。

### Step 4 — 核心控件（8 个）
`MyButton`、`MyTextButton`、`MyIconButton`、`MyCard`、`MyHint`、`MyListItem`、`MyScrollViewer`+`MyScrollBar`、`MyLoading`。
- 改 XAML 模板 + 事件名；动画沿用 `AniStart`。
- **验证**：逐控件 headless 实例化、点击/悬停状态、视觉截图。

### Step 5 — 首个页面切片
- `Pages/PageOther/PageOtherAbout`（911 行 XAML，静态卡片为主）+ `PageOtherLeft`（仅 About 项启用）+ 标题栏 5 个导航按钮中 About 可点。
- **验证**：`dotnet run` → 可点击导航 → About 页渲染完整 → 截图；Headless 断言页面字段全部非 `Nothing`。

## 不在本轮范围

- 其余 29 个页面、设置系统（`SettingService`）、下载/启动/资源/崩溃分析等业务模块（`ModNet`、`ModDownload`、`ModLaunch`、`ModMinecraft` 等）。
- `ModMusic` 的 NAudio（Linux 需换 LibVLCSharp/ManagedBass）、`ModWatcher` 的窗口枚举、`DragHelper` 的 OLE 拖放、任务栏进度。
- 完整本地化、MVVM 重构。

## 风险与对策

| 风险 | 对策 |
|---|---|
| XAML 方言转换遗漏（`x:Static`、`DataTrigger`、`AdornerDecorator`） | 规则表 + 逐文件编译错误驱动，先转 1 个页面验证规则再批量 |
| 同步泵重入/死锁（`Thread.Sleep` 在 UI 线程） | Step 2 就做模态 spike，加看门狗；不行则降级 TCS 异步 |
| 动画“不动/不刷新”（颜色绑定被覆盖、Transform 被覆盖、Effect 属性非 Styled） | 单元测试逐项断言；`EnsureTransformGroup` + DynamicResource 恢复 |
| Linux 透明/窗口装饰差异 | 真机 + `TransparencyBackgroundFallback` 兜底色 |
| 字体/换行错位 | 全局 FontFamily 回退链 + 首屏截图比对 |
| 名称生成漂移 | `Generated/` 入库 + `XamlGen --check` 在构建前校验 |

## 验证清单

1. `dotnet build Avalonia/PCL.sln -warnaserror` 通过
2. `dotnet test Avalonia/PCL.Tests` 通过（Headless：窗口实例化、主题切换、动画推进、模态返回值、控件交互）
3. `python3 tools/XamlGen/gen_fields.py --check` 无差异
4. `dotnet run --project Avalonia/PCL` 在 Wayland 下启动，截图确认：圆角窗口、标题栏、About 页面完整渲染、中文字体正常
5. 关闭/最小化/拖拽/八向缩放可用

## 许可提醒

原项目使用《PCL 分发有限许可》：二次创作并分发时，名称须以 "Plain Craft Launcher (PCL)" 开头并加第三方后缀、关于页首位署名龙腾猫跃、公开源代码、保留该许可文件、Windows 上主色相须为蓝色。个人自用不受此限。MeloongCore 为 Apache-2.0，复制时保留其 LICENSE。
