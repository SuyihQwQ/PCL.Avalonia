using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Xunit;

namespace PCL.Tests;

public class PersonalizationPageTests {

    private static Window Show(Control content, double width = 900, double height = 700) {
        var window = new Window { Width = width, Height = height, Content = content };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static void Settle() {
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

    [AvaloniaFact]
    public void Personalization_page_builds_all_cards_and_bindings() {
        var page = new global::PCL.PageSetupUI();
        var window = Show(page);
        try {
            Assert.Same(page, global::PCL.PageSetupUI.Instance);
            Assert.NotNull(page.CardLauncher);
            Assert.NotNull(page.CardBackground);
            Assert.NotNull(page.CardMusic);
            Assert.NotNull(page.CardLogo);
            Assert.NotNull(page.CardCustom);
            Assert.NotNull(page.CardSwitch);
            Assert.NotNull(page.PanLauncherTheme);
            Assert.Equal(16, page.PanLauncherTheme.Children.OfType<global::PCL.MyRadioBox>().Count());

            Assert.Equal("UiLauncherTransparent", global::PCL.SettingService.GetKey(page.SliderLauncherTransparent));
            Assert.Equal("UiLauncherHue", global::PCL.SettingService.GetKey(page.SliderLauncherHue));
            Assert.Equal("UiBackgroundOpacity", global::PCL.SettingService.GetKey(page.SliderBackgroundOpacity));
            Assert.Equal("UiLogoType", global::PCL.SettingService.GetKey(page.RadioLogoType3));
            Assert.Equal("UiCustomType", global::PCL.SettingService.GetKey(page.RadioCustomType1));
            Assert.Equal("UiHiddenSetupUi", global::PCL.SettingService.GetKey(page.CheckHiddenSetupUI));
            Assert.Equal("UiHiddenFunctionInstanceModify", global::PCL.SettingService.GetKey(page.CheckHiddenFunctionModify));

            Assert.NotNull(page.SliderMusicVolume.GetHintText);
            Assert.NotNull(page.SliderLauncherTransparent.GetHintText);
            Assert.NotNull(page.SliderLauncherHue.GetHintText);
            Assert.NotNull(page.SliderLauncherSat.GetHintText);
            Assert.NotNull(page.SliderLauncherDelta.GetHintText);
            Assert.NotNull(page.SliderLauncherLight.GetHintText);
            Assert.NotNull(page.SliderBackgroundOpacity.GetHintText);
            Assert.NotNull(page.SliderBackgroundBlur.GetHintText);
            Assert.Equal("100%", (string)page.SliderLauncherTransparent.GetHintText.DynamicInvoke(600)!);
            Assert.Equal("50%", (string)page.SliderMusicVolume.GetHintText.DynamicInvoke(500)!);
            Assert.Equal("+10", (string)page.SliderLauncherDelta.GetHintText.DynamicInvoke(100)!);
            Assert.Equal("-10", (string)page.SliderLauncherDelta.GetHintText.DynamicInvoke(80)!);
            Assert.Equal("0", (string)page.SliderLauncherLight.GetHintText.DynamicInvoke(20)!);
            Assert.Equal("5 像素", (string)page.SliderBackgroundBlur.GetHintText.DynamicInvoke(5)!);

            Settle();
            Assert.True(page.PanBack.Extent.Height > 0);
            Assert.True(page.PanBack.Bounds.Width > 0);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Personalization_refresh_updates_title_bar_panels() {
        var page = new global::PCL.PageSetupUI();
        var window = Show(page);
        var original = global::PCL.Settings.Get<int>("UiLogoType");
        try {
            global::PCL.Settings.Set("UiLogoType", 0);
            page.Refresh();
            Assert.True(page.CheckLogoLeft.IsVisible);
            Assert.False(page.PanLogoText.IsVisible);
            Assert.False(page.PanLogoChange.IsVisible);

            global::PCL.Settings.Set("UiLogoType", 2);
            page.Refresh();
            Assert.False(page.CheckLogoLeft.IsVisible);
            Assert.True(page.PanLogoText.IsVisible);
            Assert.False(page.PanLogoChange.IsVisible);

            global::PCL.Settings.Set("UiLogoType", 3);
            page.Refresh();
            Assert.False(page.CheckLogoLeft.IsVisible);
            Assert.False(page.PanLogoText.IsVisible);
            Assert.True(page.PanLogoChange.IsVisible);
            Assert.True(page.RadioLogoType3.Checked);
        } finally {
            global::PCL.Settings.Set("UiLogoType", original);
            page.Refresh();
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Personalization_custom_type_switches_panels() {
        var page = new global::PCL.PageSetupUI();
        var window = Show(page);
        var original = global::PCL.Settings.Get<int>("UiCustomType");
        try {
            global::PCL.Settings.Set("UiCustomType", 1);
            Assert.True(page.PanCustomLocal.IsVisible);
            Assert.False(page.PanCustomNet.IsVisible);
            Assert.False(page.PanCustomPreset.IsVisible);
            Assert.True(page.HintCustom.IsVisible);
            Assert.Contains("Custom.xaml", page.HintCustom.Text);

            global::PCL.Settings.Set("UiCustomType", 2);
            Assert.False(page.PanCustomLocal.IsVisible);
            Assert.True(page.PanCustomNet.IsVisible);
            Assert.False(page.PanCustomPreset.IsVisible);
            Assert.Equal(
                (object)global::PCL.CustomEvent.EventType.打开网页,
                global::PCL.CustomEventService.GetEventType(page.HintCustom));

            global::PCL.Settings.Set("UiCustomType", 3);
            Assert.False(page.PanCustomLocal.IsVisible);
            Assert.False(page.PanCustomNet.IsVisible);
            Assert.True(page.PanCustomPreset.IsVisible);
            Assert.False(page.HintCustom.IsVisible);

            global::PCL.Settings.Set("UiCustomType", 0);
            Assert.False(page.PanCustomLocal.IsVisible);
            Assert.False(page.PanCustomNet.IsVisible);
            Assert.False(page.PanCustomPreset.IsVisible);
            Assert.False(page.HintCustomWarn.IsVisible);
        } finally {
            global::PCL.Settings.Set("UiCustomType", original);
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Personalization_hidden_setup_checkboxes_stay_consistent() {
        var page = new global::PCL.PageSetupUI();
        var window = Show(page);
        var keys = new[] {
            "UiHiddenPageSetup", "UiHiddenSetupLaunch", "UiHiddenSetupUi",
            "UiHiddenSetupSystem", "UiHiddenSetupLink"
        };
        var original = keys.ToDictionary(key => key, key => global::PCL.Settings.Get<bool>(key));
        try {
            foreach (var key in keys) global::PCL.Settings.Set(key, false);

            page.CheckHiddenPageSetup.Checked = true;
            Dispatcher.UIThread.RunJobs();
            Assert.True(page.CheckHiddenSetupLaunch.Checked);
            Assert.True(page.CheckHiddenSetupUI.Checked);
            Assert.True(page.CheckHiddenSetupSystem.Checked);
            Assert.True(page.CheckHiddenSetupLink.Checked);
            foreach (var key in keys.Skip(1)) Assert.True(global::PCL.Settings.Get<bool>(key), key);

            page.CheckHiddenPageSetup.Checked = false;
            Dispatcher.UIThread.RunJobs();
            Assert.False(page.CheckHiddenSetupLaunch.Checked);
            Assert.False(page.CheckHiddenSetupUI.Checked);
            Assert.False(page.CheckHiddenSetupSystem.Checked);
            Assert.False(page.CheckHiddenSetupLink.Checked);

            page.CheckHiddenSetupLaunch.Checked = true;
            page.CheckHiddenSetupUI.Checked = true;
            page.CheckHiddenSetupSystem.Checked = true;
            page.CheckHiddenSetupLink.Checked = true;
            Dispatcher.UIThread.RunJobs();
            Assert.True(page.CheckHiddenPageSetup.Checked);
        } finally {
            foreach (var pair in original) global::PCL.Settings.Set(pair.Key, pair.Value);
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Personalization_background_refresh_loads_image_into_card() {
        var oldPathData = global::PCL.ModBase.PathData;
        var temp = Path.Combine(Path.GetTempPath(), "pcl-personalization-" + Guid.NewGuid().ToString("N"));
        var pictures = Path.Combine(temp, "Pictures");
        Directory.CreateDirectory(pictures);
        // 1x1 透明 PNG
        File.WriteAllBytes(Path.Combine(pictures, "bg.png"), Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg=="));
        File.WriteAllText(Path.Combine(pictures, "thumbs.db"), "ignored");
        global::PCL.ModBase.PathData = temp + Path.DirectorySeparatorChar;
        var page = new global::PCL.PageSetupUI();
        var window = Show(page);
        try {
            global::PCL.PageSetupUI.BackgroundRefresh(false, true);
            Assert.Equal("背景图片（1 张）", page.CardBackground.Title);
            Assert.True(page.PanBackgroundSuit.IsVisible);
            Assert.True(page.PanBackgroundOpacity.IsVisible);
            Assert.True(page.PanBackgroundBlur.IsVisible);
            Assert.True(page.BtnBackgroundClear.IsVisible);

            File.Delete(Path.Combine(pictures, "bg.png"));
            global::PCL.PageSetupUI.BackgroundRefresh(false, true);
            Assert.Equal("背景图片", page.CardBackground.Title);
            Assert.False(page.PanBackgroundSuit.IsVisible);
            Assert.False(page.PanBackgroundOpacity.IsVisible);
            Assert.False(page.BtnBackgroundClear.IsVisible);
        } finally {
            window.Close();
            global::PCL.ModBase.PathData = oldPathData;
            try { Directory.Delete(temp, true); } catch { /* 忽略清理失败 */ }
        }
    }

    [AvaloniaFact]
    public void F12_force_show_reveals_hidden_setup_pages() {
        var window = new global::PCL.FormMain();
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            Settle();
            Click(window, window.FindControl<Button>("BtnTitleSelect3")!);
            Settle();
            var left = Assert.IsType<global::PCL.PageSetupLeft>(window.PageLeft);
            var original = global::PCL.Settings.Get<bool>("UiHiddenSetupUi");
            try {
                global::PCL.Settings.Set("UiHiddenSetupUi", true);
                Assert.False(left.ItemUI.IsVisible);
                Assert.True(left.ItemLaunch.IsVisible);
                Assert.True(left.PanItem.IsVisible);

                global::PCL.PageSetupUI.HiddenForceShow = true;
                Assert.True(left.ItemUI.IsVisible);

                global::PCL.PageSetupUI.HiddenForceShow = false;
                Assert.False(left.ItemUI.IsVisible);
            } finally {
                global::PCL.Settings.Set("UiHiddenSetupUi", original);
                global::PCL.PageSetupUI.HiddenForceShow = false;
            }

            var originalPageSetup = global::PCL.Settings.Get<bool>("UiHiddenPageSetup");
            try {
                global::PCL.Settings.Set("UiHiddenPageSetup", true);
                Assert.False(window.FindControl<Button>("BtnTitleSelect3")!.IsVisible);
            } finally {
                global::PCL.Settings.Set("UiHiddenPageSetup", originalPageSetup);
            }
            Assert.True(window.FindControl<Button>("BtnTitleSelect3")!.IsVisible);
        } finally {
            window.Close();
        }
    }
}
