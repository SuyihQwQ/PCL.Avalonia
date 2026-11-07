// SPDX-License-Identifier: Apache-2.0
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
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
            Assert.Equal(8, page.PanLauncherTheme.Children.OfType<global::PCL.MyRadioBox>().Count());
            Assert.Equal("UiLauncherTheme", global::PCL.SettingService.GetKey(page.RadioLauncherTheme0));
            Assert.Equal("UiLauncherThemeMode", global::PCL.SettingService.GetKey(page.RadioThemeModeFollow));
            Assert.Equal("UiLauncherThemeMode", global::PCL.SettingService.GetKey(page.RadioThemeModeDark));

            Assert.Equal("UiLauncherTransparent", global::PCL.SettingService.GetKey(page.SliderLauncherTransparent));
            Assert.Equal("UiBackgroundOpacity", global::PCL.SettingService.GetKey(page.SliderBackgroundOpacity));
            Assert.Equal("UiLogoType", global::PCL.SettingService.GetKey(page.RadioLogoType3));
            Assert.Equal("UiCustomType", global::PCL.SettingService.GetKey(page.RadioCustomType1));
            Assert.Equal("UiHiddenSetupUi", global::PCL.SettingService.GetKey(page.CheckHiddenSetupUI));
            Assert.Equal("UiHiddenFunctionInstanceModify", global::PCL.SettingService.GetKey(page.CheckHiddenFunctionModify));

            Assert.NotNull(page.SliderMusicVolume.GetHintText);
            Assert.NotNull(page.SliderLauncherTransparent.GetHintText);
            Assert.NotNull(page.SliderBackgroundOpacity.GetHintText);
            Assert.NotNull(page.SliderBackgroundBlur.GetHintText);
            Assert.Equal("100%", (string)page.SliderLauncherTransparent.GetHintText.DynamicInvoke(600)!);
            Assert.Equal("50%", (string)page.SliderMusicVolume.GetHintText.DynamicInvoke(500)!);
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
            Click(window, window.FindControl<global::PCL.MyRadioButton>("BtnTitleSelect3")!);
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
                Assert.False(window.FindControl<global::PCL.MyRadioButton>("BtnTitleSelect3")!.IsVisible);
            } finally {
                global::PCL.Settings.Set("UiHiddenPageSetup", originalPageSetup);
            }
            Assert.True(window.FindControl<global::PCL.MyRadioButton>("BtnTitleSelect3")!.IsVisible);
        } finally {
            window.Close();
        }
    }
    private static Avalonia.Media.Color BrushColor(string key) =>
        ((Avalonia.Media.ISolidColorBrush)Application.Current!.FindResource(key)!).Color;

    [AvaloniaFact]
    public void Theme_mode_switches_between_light_and_dark_palettes() {
        var originalMode = global::PCL.Settings.Get<int>("UiLauncherThemeMode");
        var originalTheme = global::PCL.Settings.Get<int>("UiLauncherTheme");
        try {
            global::PCL.Settings.Set("UiLauncherTheme", 0);
            global::PCL.Settings.Set("UiLauncherThemeMode", 1);
            var lightText = BrushColor("ColorBrush1");
            var lightBack = BrushColor("ColorBrush8");
            var lightCard = BrushColor("ColorBrushCard");
            var lightGray = BrushColor("ColorBrushGray1");
            Assert.True(lightText.R < 100, $"浅色主题文字应为深色：{lightText}");
            Assert.True(lightBack.R > 200, $"浅色主题背景应为亮色：{lightBack}");
            Assert.True(lightCard.R > 200, $"浅色主题卡片应为亮色：{lightCard}");

            global::PCL.Settings.Set("UiLauncherThemeMode", 2);
            var darkText = BrushColor("ColorBrush1");
            var darkBack = BrushColor("ColorBrush8");
            var darkCard = BrushColor("ColorBrushCard");
            var darkGray = BrushColor("ColorBrushGray1");
            Assert.True(darkText.R > 200, $"深色主题文字应为亮色：{darkText}");
            Assert.True(darkBack.R < 80, $"深色主题背景应为暗色：{darkBack}");
            Assert.True(darkCard.R < 80, $"深色主题卡片应为暗色：{darkCard}");
            Assert.True(darkGray.R > 150, $"深色主题灰色文字应提亮：{darkGray}");

            global::PCL.Settings.Set("UiLauncherThemeMode", 0);
            var followText = BrushColor("ColorBrush1");
            Assert.Equal(
                Application.Current!.ActualThemeVariant == Avalonia.Styling.ThemeVariant.Dark ? darkText : lightText,
                followText);
        } finally {
            global::PCL.Settings.Set("UiLauncherTheme", originalTheme);
            global::PCL.Settings.Set("UiLauncherThemeMode", originalMode);
        }
    }

    [AvaloniaFact]
    public void Theme_radio_text_stays_resource_bound_after_switching() {
        var originalTheme = global::PCL.Settings.Get<int>("UiLauncherTheme");
        var page = new global::PCL.PageSetupUI();
        var window = Show(page);
        try {
            page.RadioLauncherTheme0.BringIntoView();
            Dispatcher.UIThread.RunJobs();
            var target = page.RadioLauncherTheme1;
            var center = target.TranslatePoint(new Point(target.Bounds.Width / 2, target.Bounds.Height / 2), window);
            Assert.True(center.HasValue);
            window.MouseMove(center!.Value);
            Dispatcher.UIThread.RunJobs();
            Click(window, target);
            Settle();
            var label = target.GetVisualDescendants().OfType<TextBlock>().First();
            var brush = Assert.IsType<Avalonia.Media.SolidColorBrush>(label.Foreground);
            Assert.Equal(BrushColor("ColorBrush1"), brush.Color);
            Assert.Equal(1, global::PCL.Settings.Get<int>("UiLauncherTheme"));
        } finally {
            window.Close();
            global::PCL.Settings.Set("UiLauncherTheme", originalTheme);
        }
    }

    [AvaloniaFact]
    public void Title_tab_checked_pill_survives_theme_switch() {
        var originalTheme = global::PCL.Settings.Get<int>("UiLauncherTheme");
        var originalMode = global::PCL.Settings.Get<int>("UiLauncherThemeMode");
        var window = new global::PCL.FormMain { Width = 1200, Height = 800 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        Settle();
        try {
            global::PCL.Settings.Set("UiLauncherThemeMode", 1);
            foreach (var theme in new[] { 0, 1, 2, 3, 4 }) {
                global::PCL.Settings.Set("UiLauncherTheme", theme);
                Settle();
                var tab = window.FindControl<global::PCL.MyRadioButton>("BtnTitleSelect0")!;
                Assert.True(tab.Checked);
                var background = Assert.IsType<Avalonia.Media.SolidColorBrush>(tab.Background);
                Assert.Equal(Avalonia.Media.Colors.White, background.Color);
                var text = tab.FindControl<TextBlock>("LabText")!;
                var foreground = Assert.IsType<Avalonia.Media.SolidColorBrush>(text.Foreground);
                Assert.Equal(BrushColor("ColorBrush3"), foreground.Color);
                Assert.Equal(1d, text.Opacity);
                Assert.Equal(1d, tab.Opacity);
            }

            global::PCL.Settings.Set("UiLauncherThemeMode", 2);
            global::PCL.Settings.Set("UiLauncherTheme", 0);
            Settle();
            var darkTab = window.FindControl<global::PCL.MyRadioButton>("BtnTitleSelect0")!;
            var darkBackground = Assert.IsType<Avalonia.Media.SolidColorBrush>(darkTab.Background);
            Assert.Equal(Avalonia.Media.Colors.White, darkBackground.Color);
            var darkText = Assert.IsType<Avalonia.Media.SolidColorBrush>(darkTab.FindControl<TextBlock>("LabText")!.Foreground);
            Assert.Equal(BrushColor("ColorBrush3"), darkText.Color);
        } finally {
            global::PCL.Settings.Set("UiLauncherTheme", originalTheme);
            global::PCL.Settings.Set("UiLauncherThemeMode", originalMode);
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Dark_mode_launch_button_has_no_gray_fill() {
        var originalMode = global::PCL.Settings.Get<int>("UiLauncherThemeMode");
        var originalTheme = global::PCL.Settings.Get<int>("UiLauncherTheme");
        var window = new global::PCL.FormMain { Width = 1200, Height = 800 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        Settle();
        try {
            global::PCL.Settings.Set("UiLauncherTheme", 0);
            global::PCL.Settings.Set("UiLauncherThemeMode", 2);
            Settle();
            var launch = window.GetVisualDescendants().OfType<global::PCL.MyButton>().First(b => b.Name == "BtnLaunch");
            var fore = launch.GetVisualChildren().OfType<Border>().First(b => b.Name == "PanFore");
            var brush = Assert.IsType<Avalonia.Media.SolidColorBrush>(fore.Background);
            Assert.Equal(0, brush.Color.A);
        } finally {
            global::PCL.Settings.Set("UiLauncherTheme", originalTheme);
            global::PCL.Settings.Set("UiLauncherThemeMode", originalMode);
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Theme_and_appearance_radio_groups_are_independent() {
        var originalTheme = global::PCL.Settings.Get<int>("UiLauncherTheme");
        var originalMode = global::PCL.Settings.Get<int>("UiLauncherThemeMode");
        global::PCL.Settings.Set("UiLauncherTheme", 0);
        global::PCL.Settings.Set("UiLauncherThemeMode", 1);
        var page = new global::PCL.PageSetupUI();
        var window = Show(page);
        try {
            Settle();
            Assert.True(page.RadioLauncherTheme0.Checked);
            Assert.True(page.RadioThemeModeLight.Checked);

            page.RadioLauncherTheme2.SetChecked(true, true);
            Settle();
            Assert.True(page.RadioLauncherTheme2.Checked);
            Assert.False(page.RadioLauncherTheme0.Checked);
            Assert.True(page.RadioThemeModeLight.Checked);
            Assert.False(page.RadioThemeModeDark.Checked);

            page.RadioThemeModeDark.SetChecked(true, true);
            Settle();
            Assert.True(page.RadioThemeModeDark.Checked);
            Assert.False(page.RadioThemeModeLight.Checked);
            Assert.True(page.RadioLauncherTheme2.Checked);
            Assert.Equal(2, global::PCL.Settings.Get<int>("UiLauncherTheme"));
            Assert.Equal(2, global::PCL.Settings.Get<int>("UiLauncherThemeMode"));

            page.RadioLauncherTheme0.SetChecked(true, true);
            Settle();
            Assert.True(page.RadioLauncherTheme0.Checked);
            Assert.False(page.RadioLauncherTheme2.Checked);
            Assert.True(page.RadioThemeModeDark.Checked);
        } finally {
            global::PCL.Settings.Set("UiLauncherTheme", originalTheme);
            global::PCL.Settings.Set("UiLauncherThemeMode", originalMode);
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Theme_mode_updates_application_theme_variant() {
        var originalMode = global::PCL.Settings.Get<int>("UiLauncherThemeMode");
        try {
            global::PCL.Settings.Set("UiLauncherThemeMode", 1);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(Avalonia.Styling.ThemeVariant.Light, Application.Current!.ActualThemeVariant);
            global::PCL.Settings.Set("UiLauncherThemeMode", 2);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(Avalonia.Styling.ThemeVariant.Dark, Application.Current!.ActualThemeVariant);
            global::PCL.Settings.Set("UiLauncherThemeMode", 0);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(Avalonia.Styling.ThemeVariant.Default, Application.Current!.RequestedThemeVariant);
        } finally {
            global::PCL.Settings.Set("UiLauncherThemeMode", originalMode);
            Dispatcher.UIThread.RunJobs();
        }
    }
}
