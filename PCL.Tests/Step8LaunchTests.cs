using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Xunit;

namespace PCL.Tests;

public class LaunchPageTests {

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

    /// <summary>
    /// 在测试中隐藏主窗体引用，避免 Java 自动搜索等后台行为写入用户数据。
    /// </summary>
    private static void WithoutMainWindow(Action action) {
        var saved = global::PCL.ModMain.FrmMain;
        global::PCL.ModMain.FrmMain = null;
        try {
            action();
        } finally {
            global::PCL.ModMain.FrmMain = saved;
        }
    }

    [AvaloniaFact]
    public void Launch_page_builds_cards_and_setting_bindings() {
        WithoutMainWindow(() => {
            var page = new global::PCL.PageSetupLaunch();
            var window = Show(page);
            try {
                Assert.NotNull(page.CardArgument);
                Assert.NotNull(page.CardRam);
                Assert.NotNull(page.CardSkin);
                Assert.NotNull(page.PanBack);
                Assert.NotNull(page.PanRamDisplay);

                Assert.Equal("LaunchArgumentIndieV2", global::PCL.SettingService.GetKey(page.ComboArgumentIndieV2));
                Assert.Equal("LaunchArgumentTitle", global::PCL.SettingService.GetKey(page.TextArgumentTitle));
                Assert.Equal("LaunchArgumentInfo", global::PCL.SettingService.GetKey(page.TextArgumentInfo));
                Assert.Equal("LaunchArgumentVisible", global::PCL.SettingService.GetKey(page.ComboArgumentVisibie));
                Assert.Equal("LaunchArgumentPriority", global::PCL.SettingService.GetKey(page.ComboArgumentPriority));
                Assert.Equal("LaunchArgumentWindowType", global::PCL.SettingService.GetKey(page.ComboArgumentWindowType));
                Assert.Equal("LaunchArgumentWindowWidth", global::PCL.SettingService.GetKey(page.TextArgumentWindowWidth));
                Assert.Equal("LaunchArgumentWindowHeight", global::PCL.SettingService.GetKey(page.TextArgumentWindowHeight));
                Assert.Equal("LaunchRamType", global::PCL.SettingService.GetKey(page.RadioRamType0));
                Assert.Equal("LaunchRamType", global::PCL.SettingService.GetKey(page.RadioRamType1));
                Assert.Equal("LaunchRamCustom", global::PCL.SettingService.GetKey(page.SliderRamCustom));
                Assert.Equal("LaunchArgumentRam", global::PCL.SettingService.GetKey(page.CheckArgumentRam));
                Assert.Equal("LaunchAdvanceJvm", global::PCL.SettingService.GetKey(page.TextAdvanceJvm));
                Assert.Equal("LaunchAdvanceGame", global::PCL.SettingService.GetKey(page.TextAdvanceGame));
                Assert.Equal("LaunchAdvanceRun", global::PCL.SettingService.GetKey(page.TextAdvanceRun));
                Assert.Equal("LaunchAdvanceRunWait", global::PCL.SettingService.GetKey(page.CheckAdvanceRunWait));
                Assert.Equal("LaunchAdvanceGC", global::PCL.SettingService.GetKey(page.ComboAdvanceGC));
                Assert.Equal("LaunchAdvanceDisableJLW", global::PCL.SettingService.GetKey(page.CheckAdvanceDisableJLW));
                Assert.Equal("LaunchAdvanceDisableLUA", global::PCL.SettingService.GetKey(page.CheckAdvanceDisableLUA));
                Assert.Equal("LaunchAdvanceGraphicCard", global::PCL.SettingService.GetKey(page.CheckAdvanceGraphicCard));
                Assert.Equal("ToolDownloadCert", global::PCL.SettingService.GetKey(page.CheckDownloadCert));

                Assert.Equal(5, page.ComboArgumentIndieV2.ItemCount);
                Assert.Equal(5, page.ComboArgumentVisibie.ItemCount);
                Assert.Equal(3, page.ComboArgumentPriority.ItemCount);
                Assert.Equal(5, page.ComboArgumentWindowType.ItemCount);
                Assert.Equal(5, page.ComboAdvanceGC.ItemCount);

                Assert.Single(page.TextArgumentInfo.ValidateRules);
                Assert.Single(page.TextArgumentWindowWidth.ValidateRules);
                Assert.Single(page.TextArgumentWindowHeight.ValidateRules);

                Assert.NotNull(page.SliderRamCustom.GetHintText);
                Assert.Equal("3.0 GB", (string)page.SliderRamCustom.GetHintText.DynamicInvoke(15)!);
                Assert.Equal("16.0 GB", (string)page.SliderRamCustom.GetHintText.DynamicInvoke(33)!);

                Assert.Equal(3, page.PanRamDisplay.ColumnDefinitions.Count);
                Assert.Equal(global::PCL.ModJava.JavaList.Count, page.ComboAdvanceJava.ItemCount);

                Settle();
                Assert.True(page.PanBack.Extent.Height > 0);
                Assert.True(page.PanBack.Bounds.Width > 0);
            } finally {
                window.Close();
            }
        });
    }

    [AvaloniaFact]
    public void Launch_page_toggles_window_size_run_wait_and_jvm_reset() {
        WithoutMainWindow(() => {
            var originalWindowType = global::PCL.Settings.Get<int>("LaunchArgumentWindowType");
            var originalAdvanceGame = global::PCL.Settings.Get<string>("LaunchAdvanceGame");
            var originalAdvanceRun = global::PCL.Settings.Get<string>("LaunchAdvanceRun");
            var originalAdvanceJvm = global::PCL.Settings.Get<string>("LaunchAdvanceJvm");
            var page = new global::PCL.PageSetupLaunch();
            var window = Show(page);
            try {
                page.ComboArgumentWindowType.SelectedIndex = 1;
                Dispatcher.UIThread.RunJobs();
                Assert.False(page.TextArgumentWindowWidth.IsVisible);
                Assert.False(page.LabArgumentWindowMiddle.IsVisible);
                Assert.False(page.TextArgumentWindowHeight.IsVisible);

                page.ComboArgumentWindowType.SelectedIndex = 3;
                Dispatcher.UIThread.RunJobs();
                Assert.True(page.TextArgumentWindowWidth.IsVisible);
                Assert.True(page.LabArgumentWindowMiddle.IsVisible);
                Assert.True(page.TextArgumentWindowHeight.IsVisible);

                page.TextAdvanceRun.Text = "echo hello";
                Dispatcher.UIThread.RunJobs();
                Assert.True(page.CheckAdvanceRunWait.IsVisible);
                page.TextAdvanceRun.Text = "";
                Dispatcher.UIThread.RunJobs();
                Assert.False(page.CheckAdvanceRunWait.IsVisible);

                var jvmDefault = global::PCL.Settings.GetDefault("LaunchAdvanceJvm");
                page.TextAdvanceJvm.Text = jvmDefault;
                Dispatcher.UIThread.RunJobs();
                Assert.False(page.BtnAdvanceJvmReset.IsVisible);
                page.TextAdvanceJvm.Text = jvmDefault + " -Dpcl.test=1";
                Dispatcher.UIThread.RunJobs();
                Assert.True(page.BtnAdvanceJvmReset.IsVisible);
                page.TextAdvanceJvm.Text = jvmDefault;
                Dispatcher.UIThread.RunJobs();
                Assert.False(page.BtnAdvanceJvmReset.IsVisible);

                page.TextAdvanceGame.Text = "a\r\nb";
                Dispatcher.UIThread.RunJobs();
                Assert.Equal("a b", page.TextAdvanceGame.Text);
            } finally {
                global::PCL.Settings.Set("LaunchArgumentWindowType", originalWindowType);
                global::PCL.Settings.Set("LaunchAdvanceGame", originalAdvanceGame);
                global::PCL.Settings.Set("LaunchAdvanceRun", originalAdvanceRun);
                global::PCL.Settings.Set("LaunchAdvanceJvm", originalAdvanceJvm);
                page.Refresh();
                window.Close();
            }
        });
    }

    [AvaloniaFact]
    public void Launch_page_refreshes_ram_display_and_slider_state() {
        WithoutMainWindow(() => {
            var page = new global::PCL.PageSetupLaunch();
            var window = Show(page);
            try {
                Settle();
                Assert.True(page.LabRamTotal.Text?.EndsWith(" GB") == true, page.LabRamTotal.Text);
                Assert.Contains("GB", page.LabRamUsed.Text);
                Assert.Contains("GB", page.LabRamGame.Text);
                Assert.True(page.SliderRamCustom.MaxValue > 0);
                foreach (var column in page.PanRamDisplay.ColumnDefinitions) {
                    Assert.True(column.Width.Value > 0);
                    Assert.Equal(GridUnitType.Star, column.Width.GridUnitType);
                }
                Assert.Equal(global::PCL.Settings.Get<int>("LaunchRamType") == 1,
                    page.SliderRamCustom.IsEnabled);
            } finally {
                window.Close();
            }
        });
    }

    [Fact]
    public void Launch_ram_allocation_math_matches_original_curve() {
        Assert.Equal(0.3, global::PCL.PageSetupLaunch.GetRamByIndex(0), 5);
        Assert.Equal(1.5, global::PCL.PageSetupLaunch.GetRamByIndex(12), 5);
        Assert.Equal(8.0, global::PCL.PageSetupLaunch.GetRamByIndex(25), 5);
        Assert.Equal(16.0, global::PCL.PageSetupLaunch.GetRamByIndex(33), 5);
        Assert.Equal(18.0, global::PCL.PageSetupLaunch.GetRamByIndex(34), 5);
        for (var i = 1; i <= 49; i++) {
            Assert.True(global::PCL.PageSetupLaunch.GetRamByIndex(i) >
                        global::PCL.PageSetupLaunch.GetRamByIndex(i - 1));
        }

        var originalType = global::PCL.Settings.Get<int>("LaunchRamType");
        var originalCustom = global::PCL.Settings.Get<int>("LaunchRamCustom");
        try {
            global::PCL.Settings.Set("LaunchRamType", 1);
            global::PCL.Settings.Set("LaunchRamCustom", 30);
            Assert.Equal(13.0, global::PCL.PageSetupLaunch.GetRam(), 5);
            global::PCL.Settings.Set("LaunchRamCustom", 0);
            Assert.Equal(0.3, global::PCL.PageSetupLaunch.GetRam(), 5);

            global::PCL.Settings.Set("LaunchRamType", 0);
            var automatic = global::PCL.PageSetupLaunch.GetRam();
            Assert.InRange(automatic, 0.5, 8.0);
        } finally {
            global::PCL.Settings.Set("LaunchRamType", originalType);
            global::PCL.Settings.Set("LaunchRamCustom", originalCustom);
        }
    }

    [AvaloniaFact]
    public void Launch_page_construction_does_not_write_slider_default_to_settings() {
        var original = global::PCL.Settings.Get<int>("LaunchRamCustom");
        try {
            global::PCL.Settings.Set("LaunchRamCustom", 12);
            var page = new global::PCL.PageSetupLaunch();
            Assert.Equal(12, global::PCL.Settings.Get<int>("LaunchRamCustom"));
            Assert.Equal(13, page.SliderRamCustom.Value);
        } finally {
            global::PCL.Settings.Set("LaunchRamCustom", original);
        }
    }

    [AvaloniaFact]
    public void MyCard_header_click_toggles_swap() {
        var card = new global::PCL.MyCard {
            CanSwap = true, IsSwapped = true, Title = "测试卡片", Width = 320,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };
        var content = new Border { Height = 160 };
        card.Children.Add(content);
        var window = Show(card);
        try {
            Settle();
            Assert.Equal(40, card.Bounds.Height);
            Assert.False(content.IsVisible);

            Click(window, card);
            Settle();
            Assert.False(card.IsSwapped);
            Assert.True(content.IsVisible);
            Assert.True(card.Bounds.Height > 100, $"height={card.Bounds.Height}");
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void MyComboBox_realizes_dynamically_added_items() {
        var combo = new global::PCL.MyComboBox { Width = 300 };
        var window = Show(combo);
        try {
            combo.Items.Add(new global::PCL.MyComboBoxItem { Content = "Java 17（64 位）：/opt/java" });
            combo.Items.Add(new global::PCL.MyComboBoxItem { Content = "Java 21（64 位）：/opt/java21" });
            combo.SelectedIndex = 1;
            Settle();
            Assert.Equal(2, combo.ItemCount);
            Assert.Equal(1, combo.SelectedIndex);
            var selected = Assert.IsType<global::PCL.MyComboBoxItem>(combo.SelectedItem);
            Assert.Equal("Java 21（64 位）：/opt/java21", selected.Content);

            combo.IsDropDownOpen = true;
            Settle();
            Assert.True(combo.IsDropDownOpen);
            Assert.NotNull(combo.ItemsPanelRoot);
            var realized = combo.ItemsPanelRoot!.Children.OfType<global::PCL.MyComboBoxItem>().ToList();
            Assert.Equal(2, realized.Count);
            combo.IsDropDownOpen = false;
            Settle();
        } finally {
            window.Close();
        }
    }

    [Fact]
    public void SystemInfo_reports_plausible_physical_memory() {
        var total = global::MeloongCore.SystemInfo.TotalPhysicalMemory;
        var available = global::MeloongCore.SystemInfo.AvailablePhysicalMemory;
        Assert.True(total > 256L * 1024 * 1024, $"total={total}");
        Assert.True(available > 0, $"available={available}");
        Assert.True(available <= total, $"available={available}, total={total}");
    }

    [Fact]
    public void Java_entry_reports_folder_and_exe_path() {
        var java21 = new global::PCLCS.Java("/opt/jdk21", new Version(21, 0, 1));
        Assert.EndsWith(Path.DirectorySeparatorChar.ToString(), java21.Folder);
        Assert.Equal(Path.Combine(java21.Folder, "java.exe"), java21.JavaExePath);
        Assert.Contains("21.0.1", java21.ToString());

        var unknown = new global::PCLCS.Java("/opt/jdk");
        Assert.Null(unknown.Version);
        Assert.Equal(unknown, new global::PCLCS.Java("/opt/jdk"));
    }
}
