using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Xunit;

namespace PCL.Tests;

public class SetupPageTests {

    private static Window Show(Control content, double width = 900, double height = 700) {
        var window = new Window { Width = width, Height = height, Content = content };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static void Settle() {
        // 页面切换会在一帧内再启动元素进场动画，需要多帧才能稳定
        for (var i = 0; i < 6; i++) {
            global::PCL.ModAnimation.AniTimer(400);
            Dispatcher.UIThread.RunJobs();
        }
    }

    private static void Click(Window window, Control target) {
        var center = target.TranslatePoint(new Point(target.Bounds.Width / 2, target.Bounds.Height / 2), window);
        Assert.True(center.HasValue);
        window.MouseDown(center!.Value, MouseButton.Left);
        window.MouseUp(center.Value, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }

    private static global::PCL.MyListItem SelectedItem(global::PCL.PageSetupLeft left) => left.PageID switch {
        global::PCL.PageSetupLeft.SetupPageType.Launch => left.ItemLaunch,
        global::PCL.PageSetupLeft.SetupPageType.Link => left.ItemLink,
        global::PCL.PageSetupLeft.SetupPageType.UI => left.ItemUI,
        global::PCL.PageSetupLeft.SetupPageType.System => left.ItemSystem,
        _ => throw new ArgumentOutOfRangeException()
    };

    [AvaloniaFact]
    public void Setup_left_page_builds_items_and_lazily_creates_right_pages() {
        var left = new global::PCL.PageSetupLeft();
        var window = Show(left, 230, 520);
        try {
            Assert.NotNull(left.ItemLaunch);
            Assert.NotNull(left.ItemLink);
            Assert.NotNull(left.ItemUI);
            Assert.NotNull(left.ItemSystem);

            foreach (var item in new[] { left.ItemLaunch, left.ItemLink, left.ItemUI, left.ItemSystem }) {
                Assert.Equal(global::PCL.MyListItem.CheckType.RadioBox, item.Type);
                Assert.NotNull(item.Buttons);
                Assert.Single(item.Buttons);
            }
            Assert.False(left.ItemLink.IsVisible);
            Assert.True(left.PanItem.IsVisible);

            Assert.IsType<global::PCL.PageSetupLaunch>(
                left.PageGet(global::PCL.PageSetupLeft.SetupPageType.Launch));
            Assert.IsType<global::PCL.PageSetupUI>(
                left.PageGet(global::PCL.PageSetupLeft.SetupPageType.UI));
            Assert.IsType<global::PCL.PageSetupSystem>(
                left.PageGet(global::PCL.PageSetupLeft.SetupPageType.System));
            Assert.IsType<global::PCL.PageSetupLink>(
                left.PageGet(global::PCL.PageSetupLeft.SetupPageType.Link));

            Assert.True(SelectedItem(left).Checked);
            Assert.Single(new[] { left.ItemLaunch, left.ItemLink, left.ItemUI, left.ItemSystem },
                item => item.Checked);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Setup_system_page_loads_controls_and_slider_hints() {
        var page = new global::PCL.PageSetupSystem();
        var window = Show(page);
        try {
            Assert.NotNull(page.PanBack);
            Assert.NotNull(page.PanMain);
            Assert.NotNull(page.ComboDownloadSource);
            Assert.NotNull(page.ComboDownloadVersion);
            Assert.NotNull(page.SliderDownloadThread);
            Assert.NotNull(page.SliderDownloadSpeed);
            Assert.NotNull(page.ComboDownloadMod);
            Assert.NotNull(page.ComboDownloadTranslateV2);
            Assert.NotNull(page.ComboModLocalNameStyle);
            Assert.NotNull(page.CheckUpdateRelease);
            Assert.NotNull(page.CheckUpdateSnapshot);
            Assert.NotNull(page.CheckHelpChinese);
            Assert.NotNull(page.TextSystemCache);
            Assert.NotNull(page.BtnSystemSettingExp);
            Assert.NotNull(page.BtnSystemSettingImp);
            Assert.NotNull(page.CardDebug);
            Assert.NotNull(page.SliderDebugAnim);

            Assert.Equal("ToolDownloadThread", global::PCL.SettingService.GetKey(page.SliderDownloadThread));
            Assert.Equal("ToolDownloadSpeed", global::PCL.SettingService.GetKey(page.SliderDownloadSpeed));
            Assert.Equal("ToolUpdateRelease", global::PCL.SettingService.GetKey(page.CheckUpdateRelease));

            Assert.NotNull(page.SliderDownloadThread.GetHintText);
            Assert.NotNull(page.SliderDownloadSpeed.GetHintText);
            Assert.NotNull(page.SliderDebugAnim.GetHintText);
            Assert.Equal("5", (string)page.SliderDownloadThread.GetHintText.DynamicInvoke(4d)!);
            Assert.Equal("0.6 M/s", (string)page.SliderDownloadSpeed.GetHintText.DynamicInvoke(5d)!);
            Assert.Equal("2.0 M/s", (string)page.SliderDownloadSpeed.GetHintText.DynamicInvoke(15d)!);
            Assert.Equal("无限制", (string)page.SliderDownloadSpeed.GetHintText.DynamicInvoke(42d)!);
            Assert.Equal("1.0x", (string)page.SliderDebugAnim.GetHintText.DynamicInvoke(9d)!);
            Assert.Equal("关闭", (string)page.SliderDebugAnim.GetHintText.DynamicInvoke(30d)!);

            global::PCL.ModAnimation.AniTimer(200);
            Assert.True(page.PanBack.Extent.Height > 0);
            Assert.True(page.PanBack.Bounds.Width > 0);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Setup_system_checkbox_saves_and_reloads_setting() {
        var page = new global::PCL.PageSetupSystem();
        var window = Show(page);
        try {
            var original = global::PCL.Settings.Get<bool>("ToolUpdateRelease");
            try {
                page.CheckUpdateRelease.Checked = !original;
                global::PCL.ModAnimation.AniTimer(60);
                Assert.Equal(!original, global::PCL.Settings.Get<bool>("ToolUpdateRelease"));

                page.Reload();
                Assert.Equal(!original, page.CheckUpdateRelease.Checked);
            } finally {
                global::PCL.Settings.Set("ToolUpdateRelease", original);
                page.Reload();
            }
            Assert.Equal(original, page.CheckUpdateRelease.Checked);

            var savedThreads = global::PCL.Settings.Get<int>("ToolDownloadThread");
            Assert.Equal(savedThreads, page.SliderDownloadThread.Value);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Setup_link_page_builds_and_saves_custom_peer() {
        var page = new global::PCL.PageSetupLink();
        var window = Show(page);
        try {
            Assert.NotNull(page.PanBack);
            Assert.NotNull(page.CardArgument);
            Assert.NotNull(page.ComboLatencyMode);
            Assert.NotNull(page.TextCustomPeer);
            Assert.NotNull(page.BtnLinkContribute);
            Assert.NotNull(page.BtnLinkStatus);
            Assert.Equal(2, page.ComboLatencyMode.ItemCount);
            Assert.Equal("LinkLatencyMode", global::PCL.SettingService.GetKey(page.ComboLatencyMode));
            Assert.Equal("LinkCustomPeer", global::PCL.SettingService.GetKey(page.TextCustomPeer));

            const string peer = "tcp://public.easytier.cn:11010,tcp://public2.easytier.cn:54321";
            try {
                page.TextCustomPeer.Text = peer;
                Dispatcher.UIThread.RunJobs();
                global::PCL.ModAnimation.AniTimer(60);
                Assert.Equal(peer, page.TextCustomPeer.Text);
                Assert.True(page.TextCustomPeer.IsValidated, page.TextCustomPeer.ValidateResult);
                Assert.Equal(peer, global::PCL.Settings.Get<string>("LinkCustomPeer"));
            } finally {
                global::PCL.Settings.Reset("LinkCustomPeer");
                page.Reload();
            }
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Setup_placeholder_pages_build() {
        foreach (global::PCL.MyPageRight page in new global::PCL.MyPageRight[] {
                     new global::PCL.PageSetupLaunch()
                 }) {
            var window = Show(page);
            try {
                Assert.NotNull(page.FindControl<global::PCL.MyScrollViewer>("PanBack"));
                Assert.NotNull(page.FindControl<StackPanel>("PanMain"));
                Assert.NotNull(page.Content);
            } finally {
                window.Close();
            }
        }
    }

    [AvaloniaFact]
    public void Checkbox_pointer_click_toggles_and_syncs_visuals() {
        var check = new global::PCL.MyCheckBox {
            Text = "提示", Width = 220, Height = 32,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };
        var window = Show(check);
        try {
            Settle();
            Assert.False(check.Checked);
            var shape = check.FindControl<Avalonia.Controls.Shapes.Path>("ShapeCheck")!;
            Assert.True(ScaleOf(shape) < 0.1, $"initial scale={ScaleOf(shape)}");

            Click(window, check);
            Settle();
            Assert.True(check.Checked, $"first click did not check, scale={ScaleOf(shape)}");
            Assert.True(ScaleOf(shape) > 0.9, $"first click scale={ScaleOf(shape)}");

            Click(window, check);
            Settle();
            Assert.False(check.Checked, $"second click did not uncheck, scale={ScaleOf(shape)}");
            Assert.True(ScaleOf(shape) < 0.1, $"second click scale={ScaleOf(shape)}");
        } finally {
            window.Close();
        }
    }

    private static double ScaleOf(Avalonia.Controls.Shapes.Path path) =>
        global::PCL.ModAnimation.AniScaleX(path);

    [AvaloniaFact]
    public void FormMain_switches_between_setup_pages() {
        var window = new global::PCL.FormMain();
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            Settle();
            Click(window, window.FindControl<global::PCL.MyRadioButton>("BtnTitleSelect3")!);
            Settle();

            var left = Assert.IsType<global::PCL.PageSetupLeft>(window.PageLeft);
            var expected = left.PageGet(left.PageID);
            Assert.Same(expected, window.PageRight);
            Assert.Same(left, window.FindControl<Border>("PanMainLeft")!.Child);
            Assert.Same(expected, window.FindControl<Border>("PanMainRight")!.Child);

            Click(window, left.ItemSystem);
            Settle();
            Assert.Equal(global::PCL.PageSetupLeft.SetupPageType.System, left.PageID);
            var system = Assert.IsType<global::PCL.PageSetupSystem>(window.PageRight);
            Assert.Same(system, window.FindControl<Border>("PanMainRight")!.Child);
            Assert.True(left.ItemSystem.Checked);
            Assert.False(left.ItemLaunch.Checked);

            Click(window, window.FindControl<global::PCL.MyRadioButton>("BtnTitleSelect4")!);
            Settle();
            Assert.IsType<global::PCL.PageOtherLeft>(window.PageLeft);
            Assert.IsType<global::PCL.PageOtherHelp>(window.PageRight);
        } finally {
            window.Close();
        }
    }
}
