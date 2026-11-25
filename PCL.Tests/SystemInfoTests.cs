// SPDX-License-Identifier: Apache-2.0
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace PCL.Tests;

/// <summary>
/// 百宝箱“系统信息”卡片与 ModSystemInfo 的 headless 测试。
/// 硬件相关字段只断言“不抛异常且返回非空文本（允许为未知）”，避免依赖具体运行环境。
/// </summary>
public class SystemInfoTests {

    private static readonly string[] AllValueLabels = {
        "LabSystemInfoHostname", "LabSystemInfoOperatingSystem", "LabSystemInfoArchitecture",
        "LabSystemInfoUptime", "LabSystemInfoDesktop", "LabSystemInfoCpu", "LabSystemInfoGpu",
        "LabSystemInfoMemory", "LabSystemInfoLauncher"
    };

    [Fact]
    public void Field_readers_never_throw_and_return_text() {
        var readers = new (string Name, Func<string> Read)[] {
            ("主机名", global::PCL.ModSystemInfo.GetHostname),
            ("操作系统", global::PCL.ModSystemInfo.GetOperatingSystemText),
            ("系统架构", global::PCL.ModSystemInfo.GetArchitectureText),
            ("运行时间", global::PCL.ModSystemInfo.GetUptimeText),
            ("桌面环境", global::PCL.ModSystemInfo.GetDesktopEnvironmentText),
            ("处理器", global::PCL.ModSystemInfo.GetCpuText),
            ("显卡", global::PCL.ModSystemInfo.GetGpuText),
            ("内存", global::PCL.ModSystemInfo.GetMemoryText),
            ("启动器版本", global::PCL.ModSystemInfo.GetLauncherVersionText),
            ("操作系统图标键", global::PCL.ModSystemInfo.GetOsLogoKey)
        };
        foreach (var (name, read) in readers) {
            var text = read();
            Assert.False(string.IsNullOrWhiteSpace(text), $"{name} 返回了空文本");
            Assert.DoesNotContain('\u0000', text);
        }
    }

    [Fact]
    public void Snapshot_is_complete_and_cached() {
        var info = global::PCL.ModSystemInfo.GetSystemInfo(ForceRefresh: true);
        Assert.NotNull(info);
        Assert.False(string.IsNullOrWhiteSpace(info.Hostname));
        Assert.False(string.IsNullOrWhiteSpace(info.OperatingSystem));
        Assert.False(string.IsNullOrWhiteSpace(info.Architecture));
        Assert.False(string.IsNullOrWhiteSpace(info.Uptime));
        Assert.False(string.IsNullOrWhiteSpace(info.DesktopEnvironment));
        Assert.False(string.IsNullOrWhiteSpace(info.Cpu));
        Assert.False(string.IsNullOrWhiteSpace(info.Gpu));
        Assert.False(string.IsNullOrWhiteSpace(info.Memory));
        Assert.False(string.IsNullOrWhiteSpace(info.Launcher));
        Assert.Contains(global::PCL.ModBase.VersionBaseName, info.Launcher);
        Assert.Contains(info.OsLogoKey, global::PCL.ModSystemInfo.GetOsLogoKeys());

        // 30 秒缓存内不应重新读取，直接返回同一实例
        Assert.Same(info, global::PCL.ModSystemInfo.GetSystemInfo());
    }

    [Fact]
    public void Every_os_logo_has_path_data() {
        var keys = global::PCL.ModSystemInfo.GetOsLogoKeys();
        Assert.NotEmpty(keys);
        foreach (var key in new[] { "tux", "macos", "windows11", "windows10", "windows8", "windows7", "ubuntu", "debian", "archlinux", "fedora", "opensuse", "linuxmint" }) {
            Assert.Contains(key, keys);
        }
        foreach (var key in keys) {
            var data = global::PCL.ModSystemInfo.GetOsLogoData(key);
            Assert.False(string.IsNullOrWhiteSpace(data), $"{key} 没有 Path Data");
            Assert.StartsWith("M", data);
        }
        // 未知键回退 Tux
        Assert.Equal(global::PCL.ModSystemInfo.GetOsLogoData("tux"), global::PCL.ModSystemInfo.GetOsLogoData("not-a-real-distro"));
        // 自动识别的图标键必须存在
        var current = global::PCL.ModSystemInfo.GetOsLogoData();
        Assert.False(string.IsNullOrWhiteSpace(current));
    }

    [AvaloniaFact]
    public void Os_logo_path_data_parses_to_geometry() {
        // Geometry.Parse 依赖 Avalonia 的渲染后端，只能在 Avalonia 测试里执行
        foreach (var key in global::PCL.ModSystemInfo.GetOsLogoKeys()) {
            var data = global::PCL.ModSystemInfo.GetOsLogoData(key);
            var geometry = Geometry.Parse(data);
            Assert.NotNull(geometry);
            Assert.True(geometry.Bounds.Width > 0 && geometry.Bounds.Height > 0, $"{key} 的 Path Data 无法得到有效几何形状");
        }
    }

    [Fact]
    public void Unknown_text_can_be_localized_with_fallback() {
        Assert.Equal(global::PCL.ModSystemInfo.UnknownText, global::PCL.ModSystemInfo.LocalizeUnknown(global::PCL.ModSystemInfo.UnknownText));
        Assert.Equal("some-value", global::PCL.ModSystemInfo.LocalizeUnknown("some-value"));
        Assert.Equal("fallback", global::PCL.ModSystemInfo.GetLangText("LangSystemInfoDefinitelyMissingKey", "fallback"));
    }

    [AvaloniaFact]
    public void Page_has_always_expanded_system_info_card_before_utilities() {
        var page = new global::PCL.PageOtherTest();
        var window = new Window { Width = 1000, Height = 700, Content = page };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            // 卡片位于“实用工具”卡片之前
            Assert.NotNull(page.CardSystemInfo);
            Assert.Equal(2, page.PanMain.Children.Count);
            Assert.Equal(0, page.PanMain.Children.IndexOf(page.CardSystemInfo));
            var utilityCard = Assert.IsType<global::PCL.MyCard>(page.PanMain.Children[1]);
            Assert.Contains(utilityCard.GetVisualDescendants().OfType<global::PCL.MyButton>(),
                button => button.Text == "检测你是不是中国用户");

            // “始终展开”：不允许折叠、没有折叠箭头、始终保持展开状态
            Assert.False(page.CardSystemInfo.CanSwap);
            Assert.Null(page.CardSystemInfo.SwapControl);
            Assert.False(page.CardSystemInfo.IsSwapped);
            Assert.Null(page.CardSystemInfo.MainSwap);

            // 标题与字段名在缺失译文时也有中文兜底，不为空
            Assert.False(string.IsNullOrWhiteSpace(page.CardSystemInfo.Title));
            var labelBlocks = page.CardSystemInfo.GetLogicalDescendants().OfType<TextBlock>()
                .Where(block => block.Tag is string).ToList();
            Assert.Equal(9, labelBlocks.Count);
            Assert.All(labelBlocks, block => Assert.False(string.IsNullOrWhiteSpace(block.Text)));
            Assert.Equal(9, AllValueLabels.Length);
            foreach (var labelName in AllValueLabels) {
                var value = page.FindControl<TextBlock>(labelName);
                Assert.NotNull(value);
                Assert.Contains(value!.Classes, cssClass => cssClass == "SystemInfoValue");
            }

            // 右侧 OS 图标区域存在
            Assert.NotNull(page.PathOsLogo);
            Assert.True(page.PathOsLogo.Bounds.Width > 0 || page.PathOsLogo.Width > 0);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Async_refresh_populates_values_and_logo() {
        var page = new global::PCL.PageOtherTest();
        var window = new Window { Width = 1000, Height = 700, Content = page };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            var valueLabels = AllValueLabels
                .Select(name => Assert.IsType<TextBlock>(page.FindControl<TextBlock>(name)))
                .ToList();

            // 等待后台读取完成并通过 RunInUi 写回 UI 线程
            var deadline = DateTime.UtcNow.AddSeconds(25);
            while (DateTime.UtcNow < deadline &&
                   valueLabels.Any(label => string.IsNullOrEmpty(label.Text) || label.Text == "…")) {
                Dispatcher.UIThread.RunJobs();
                Thread.Sleep(20);
            }

            foreach (var label in valueLabels) {
                Assert.False(string.IsNullOrWhiteSpace(label.Text));
                Assert.NotEqual("…", label.Text);
            }
            // 主机名与启动器版本一定能读到实际值
            Assert.NotNull(page.LabSystemInfoHostname.Text);
            Assert.NotEqual(global::PCL.ModSystemInfo.UnknownText, page.LabSystemInfoHostname.Text);
            Assert.Contains(global::PCL.ModBase.VersionBaseName, page.LabSystemInfoLauncher.Text);

            // 图标 Path Data 已填充
            Assert.NotNull(page.PathOsLogo.Data);
            Assert.True(page.PathOsLogo.Data!.Bounds.Width > 0);

            // 卡片内容自适应，高度大于折叠状态
            Assert.True(page.CardSystemInfo.Bounds.Height > global::PCL.MyCard.SwapedHeight);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Resolution_reader_works_on_ui_thread() {
        var window = new Window { Width = 800, Height = 600 };
        window.Show();
        try {
            var text = global::PCL.ModSystemInfo.GetResolutionText(TopLevel.GetTopLevel(window));
            Assert.False(string.IsNullOrWhiteSpace(text));
            // 读不到屏幕时允许返回未知
            Assert.True(text == global::PCL.ModSystemInfo.UnknownText || text.Contains('@'));
        } finally {
            window.Close();
        }
    }
}
