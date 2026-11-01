using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.IO.Compression;
using System.Reflection;
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
                Assert.Equal(OperatingSystem.IsWindows(), page.CheckAdvanceDisableJLW.IsEnabled);
                Assert.Equal(!OperatingSystem.IsWindows(), page.CheckAdvanceDisableJLW.Checked);
                Assert.Equal(OperatingSystem.IsWindows() || OperatingSystem.IsLinux(), page.CheckAdvanceGraphicCard.IsEnabled);
                Assert.Equal(OperatingSystem.IsWindows(), page.CheckArgumentRam.IsEnabled);
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
                // 等待自动搜索结束，避免与后台 Java 刷新竞争
                for (var i = 0; i < 300 && global::PCL.ModJava.IsRefreshing; i++) {
                    Thread.Sleep(10);
                    Dispatcher.UIThread.RunJobs();
                }
                page.UpdateJavaList();
                // Java 项之后固定追加一个“导入电脑中已有的 Java…”项
                Assert.Equal(global::PCL.ModJava.JavaList.Count + 1, page.ComboAdvanceJava.ItemCount);
                var importItem = Assert.IsType<global::PCL.MyListItem>(page.ComboAdvanceJava.Items[^1]);
                Assert.Contains("导入", importItem.Title);
                // Java 项按扫描顺序排列，操作按钮位于菜单项内部
                var javaEntries = global::PCL.ModJava.JavaList;
                for (var i = 0; i < javaEntries.Count; i++) {
                    var javaItem = Assert.IsType<global::PCL.MyListItem>(page.ComboAdvanceJava.Items[i]);
                    Assert.Equal(javaEntries[i].ToString(), javaItem.Title);
                    Assert.Equal(2, javaItem.Buttons!.Count());
                }

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

    [AvaloniaFact]
    public void Launch_page_java_dropdown_realizes_items_with_menu_buttons() {
        WithoutMainWindow(() => {
            var page = new global::PCL.PageSetupLaunch();
            var window = Show(page);
            try {
                for (var i = 0; i < 300 && global::PCL.ModJava.IsRefreshing; i++) {
                    Thread.Sleep(10);
                    Dispatcher.UIThread.RunJobs();
                }
                page.UpdateJavaList();
                var entries = global::PCL.ModJava.JavaList;
                var card = page.PanAdvanceJava.FindAncestorOfType<global::PCL.MyCard>();
                Assert.NotNull(card);
                card!.IsSwapped = false;
                Settle();

                page.ComboAdvanceJava.IsDropDownOpen = true;
                Settle();
                Assert.True(page.ComboAdvanceJava.IsDropDownOpen);
                Assert.NotNull(page.ComboAdvanceJava.ItemsPanelRoot);
                var realized = page.ComboAdvanceJava.ItemsPanelRoot!.Children.OfType<ComboBoxItem>().ToList();
                Assert.Equal(entries.Count + 1, realized.Count);
                for (var i = 0; i < entries.Count; i++) {
                    var item = Assert.IsType<global::PCL.MyListItem>(realized[i].Content);
                    Assert.Equal(entries[i].ToString(), item.Title);
                    Assert.Equal(2, item.Buttons!.Count());
                    Assert.Equal(2, item.GetVisualDescendants().OfType<global::PCL.MyIconButton>().Count());
                }
                page.ComboAdvanceJava.IsDropDownOpen = false;
                Settle();
            } finally {
                window.Close();
            }
        });
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
        Assert.Equal(Path.Combine(java21.Folder, global::PCLCS.Java.ExecutableName), java21.JavaExePath);
        Assert.Contains("21.0.1", java21.ToString());

        var unknown = new global::PCLCS.Java("/opt/jdk");
        Assert.Null(unknown.Version);
        Assert.Equal(unknown, new global::PCLCS.Java("/opt/jdk"));
    }

    [AvaloniaFact]
    public void Launch_page_reentry_refreshes_stale_loading_button() {
        WithoutMainWindow(() => {
            var originalLoginType = global::PCL.Settings.Get<int>("LoginType");
            var originalPageType = global::PCL.Settings.Get<int>("LoginPageType");
            var page = new global::PCL.PageLaunchLeft();
            var fields = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(global::PCL.PageLaunchLeft).GetField("IsLoad", fields)!.SetValue(page, true);
            typeof(global::PCL.PageLaunchLeft).GetField("IsLoadFinished", fields)!.SetValue(page, true);
            var window = Show(page, 360, 500);
            try {
                Assert.True((bool)typeof(global::PCL.PageLaunchLeft).GetField("IsLoadFinished", fields)!.GetValue(page)!);
                Assert.True(page.BtnLaunch.IsLoaded);
                typeof(global::PCL.PageLaunchLeft).GetField("BtnLaunchState", fields)!.SetValue(page, 0);
                page.BtnVersion.IsEnabled = false;
                typeof(global::PCL.PageLaunchLeft)
                    .GetMethod("RefreshAfterPageEnter", BindingFlags.Instance | BindingFlags.Public)!
                    .Invoke(page, null);
                Assert.NotEqual(0, typeof(global::PCL.PageLaunchLeft).GetField("BtnLaunchState", fields)!.GetValue(page));
                Dispatcher.UIThread.RunJobs();
                Assert.True(page.BtnVersion.IsEnabled);
            } finally {
                global::PCL.Settings.Set("LoginType", originalLoginType);
                global::PCL.Settings.Set("LoginPageType", originalPageType);
                window.Close();
            }
        });
    }

    [AvaloniaFact]
    public void Launch_page_reentry_restores_button_text_after_dynamic_resource_reset() {
        WithoutMainWindow(() => {
            var temp = Path.Combine(Path.GetTempPath(), "pcl-reentry-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(temp, "versions", "KSNClient"));
            var instanceField = typeof(global::PCL.ModMinecraft).GetField(
                "_McInstanceSelected", BindingFlags.Static | BindingFlags.NonPublic)!;
            var savedInstance = instanceField.GetValue(null);
            try {
                instanceField.SetValue(null, new global::PCL.ModMinecraft.McInstance(
                    Path.Combine(temp, "versions", "KSNClient") + Path.DirectorySeparatorChar));
                var page = new global::PCL.PageLaunchLeft();
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(global::PCL.PageLaunchLeft).GetField("IsLoad", flags)!.SetValue(page, true);
                typeof(global::PCL.PageLaunchLeft).GetField("IsLoadFinished", flags)!.SetValue(page, true);
                var window = Show(page, 360, 500);
                try {
                    Assert.Equal("启动游戏", page.BtnLaunch.Text);
                    Assert.Equal("KSNClient", page.LabVersion.Text);
                    // 模拟从版本选择/版本设置返回：页面被重新挂载时 DynamicResource 会覆盖代码设置的文字
                    window.Content = null;
                    Dispatcher.UIThread.RunJobs();
                    window.Content = page;
                    Dispatcher.UIThread.RunJobs();
                    Assert.Equal("启动游戏", page.BtnLaunch.Text);
                    Assert.Equal("KSNClient", page.LabVersion.Text);
                } finally {
                    window.Close();
                }
            } finally {
                instanceField.SetValue(null, savedInstance);
                Directory.Delete(temp, true);
            }
        });
    }

    [AvaloniaFact]
    public void Launch_page_instance_label_scales_with_button_after_reentry() {
        WithoutMainWindow(() => {
            var temp = Path.Combine(Path.GetTempPath(), "pcl-scale-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(temp, "versions", "KSNClient"));
            var instanceField = typeof(global::PCL.ModMinecraft).GetField(
                "_McInstanceSelected", BindingFlags.Static | BindingFlags.NonPublic)!;
            var savedInstance = instanceField.GetValue(null);
            try {
                instanceField.SetValue(null, new global::PCL.ModMinecraft.McInstance(
                    Path.Combine(temp, "versions", "KSNClient") + Path.DirectorySeparatorChar));
                var page = new global::PCL.PageLaunchLeft();
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(global::PCL.PageLaunchLeft).GetField("IsLoad", flags)!.SetValue(page, true);
                typeof(global::PCL.PageLaunchLeft).GetField("IsLoadFinished", flags)!.SetValue(page, true);
                var window = Show(page, 360, 500);
                try {
                    Assert.Equal("启动游戏", page.BtnLaunch.Text);
                    PressAndCheckScale();

                    window.Content = null;
                    Dispatcher.UIThread.RunJobs();
                    window.Content = page;
                    Dispatcher.UIThread.RunJobs();
                    Assert.Equal("启动游戏", page.BtnLaunch.Text);
                    PressAndCheckScale();
                } finally {
                    window.Close();
                }

                void PressAndCheckScale() {
                    var center = page.BtnLaunch.TranslatePoint(
                        new Point(page.BtnLaunch.Bounds.Width / 2, page.BtnLaunch.Bounds.Height / 2), window)!.Value;
                    window.MouseDown(center, MouseButton.Left);
                    for (var i = 0; i < 5; i++) {
                        global::PCL.ModAnimation.AniTimer(30);
                        Dispatcher.UIThread.RunJobs();
                    }
                    var buttonScale = page.BtnLaunch.RealScaleTransform.ScaleX;
                    var labelScale = ((Avalonia.Media.ScaleTransform)page.LabVersion.RenderTransform!).ScaleX;
                    Assert.InRange(buttonScale, 0.9, 0.99);
                    Assert.InRange(labelScale, 0.9, 0.99);
                    Assert.Equal(buttonScale, labelScale, 3);
                    // 测试中没有主窗体，先禁用按钮再松开，避免触发启动点击
                    page.BtnLaunch.IsEnabled = false;
                    window.MouseUp(center, MouseButton.Left);
                    for (var i = 0; i < 20; i++) {
                        global::PCL.ModAnimation.AniTimer(50);
                        Dispatcher.UIThread.RunJobs();
                    }
                    page.BtnLaunch.IsEnabled = true;
                }
            } finally {
                instanceField.SetValue(null, savedInstance);
                Directory.Delete(temp, true);
            }
        });
    }

    [AvaloniaFact]
    public void Launch_page_reentry_restores_login_skin_text() {
        WithoutMainWindow(() => {
            var originalPageType = global::PCL.Settings.Get<int>("LoginPageType");
            var originalAccess = global::PCL.Settings.Get<string>("CacheAuthAccess");
            var originalName = global::PCL.Settings.Get<string>("CacheAuthName");
            var originalUuid = global::PCL.Settings.Get<string>("CacheAuthUuid");
            var instanceField = typeof(global::PCL.ModMinecraft).GetField(
                "_McInstanceSelected", BindingFlags.Static | BindingFlags.NonPublic)!;
            var savedInstance = instanceField.GetValue(null);
            try {
                instanceField.SetValue(null, null);
                global::PCL.Settings.Set("LoginPageType", 4);
                global::PCL.Settings.Set("CacheAuthAccess", "test");
                global::PCL.Settings.Set("CacheAuthName", "");
                global::PCL.Settings.Set("CacheAuthUuid", "");
                var page = new global::PCL.PageLaunchLeft();
                typeof(global::PCL.PageLaunchLeft)
                    .GetField("IsLoad", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .SetValue(page, true);
                var window = Show(page, 360, 500);
                try {
                    var login = global::PCL.ModMain.FrmLoginAuthSkin;
                    Assert.NotNull(login);
                    Assert.Equal("", login!.TextName.Text);
                    window.Content = null;
                    Dispatcher.UIThread.RunJobs();
                    window.Content = page;
                    Dispatcher.UIThread.RunJobs();
                    Assert.Equal("", login.TextName.Text);
                } finally {
                    window.Close();
                }
            } finally {
                instanceField.SetValue(null, savedInstance);
                global::PCL.Settings.Set("LoginPageType", originalPageType);
                global::PCL.Settings.Set("CacheAuthAccess", originalAccess);
                global::PCL.Settings.Set("CacheAuthName", originalName);
                global::PCL.Settings.Set("CacheAuthUuid", originalUuid);
            }
        });
    }

    [AvaloniaFact]
    public void Instance_setup_shows_java_launch_wrapper_disabled_on_non_windows() {
        var originalInstanceSetup = global::PCL.ModMain.FrmInstanceSetup;
        var page = new global::PCL.PageInstanceSetup();
        try {
            typeof(global::PCL.PageInstanceSetup)
                .GetMethod("RefreshJavaLaunchWrapperSetting", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(page, null);

            Assert.Equal(OperatingSystem.IsWindows(), page.CheckAdvanceDisableJLW.IsEnabled);
            Assert.Equal(!OperatingSystem.IsWindows(), page.CheckAdvanceDisableJLW.Checked);
        } finally {
            global::PCL.ModMain.FrmInstanceSetup = originalInstanceSetup;
        }
    }

    [AvaloniaFact]
    public void Install_page_back_arrow_uses_light_icon_theme() {
        var page = new global::PCL.PageDownloadInstall();
        Assert.Equal(global::PCL.MyIconButton.Themes.White, page.BtnBack.Theme);
    }

    [AvaloniaFact]
    public void Launch_page_reentry_keeps_selected_instance_authlib_login() {
        var temp = Path.Combine(Path.GetTempPath(), "pcl-authlib-reentry-" + Guid.NewGuid().ToString("N"));
        var instancePath = Path.Combine(temp, "versions", "AuthInstance") + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(Path.Combine(instancePath, "PCL"));

        var originalFolder = global::PCL.ModMinecraft.McFolderSelected;
        var originalInstance = global::PCL.ModMinecraft.McInstanceSelected;
        var originalLoginType = global::PCL.Settings.Get<int>("LoginType");
        var originalPageType = global::PCL.Settings.Get<int>("LoginPageType");
        var originalAuthAccess = global::PCL.Settings.Get<string>("CacheAuthAccess");
        var originalAuthServer = global::PCL.Settings.Get<string>("CacheAuthServerServer");
        var originalAuthName = global::PCL.Settings.Get<string>("CacheAuthServerName");
        var originalAuthRegister = global::PCL.Settings.Get<string>("CacheAuthServerRegister");
        var originalInstanceSetup = global::PCL.ModMain.FrmInstanceSetup;

        try {
            global::PCL.ModMain.FrmInstanceSetup = null;
            global::PCL.ModMinecraft.McFolderSelected = temp + Path.DirectorySeparatorChar;
            global::PCL.Settings.Set("VersionServerLogin", 4, instancePath);
            global::PCL.ModMinecraft.McInstanceSelected = new global::PCL.ModMinecraft.McInstance(instancePath);
            global::PCL.Settings.Set("CacheAuthAccess", "");

            var page = new global::PCL.PageLaunchLeft();
            var fields = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(global::PCL.PageLaunchLeft).GetField("IsLoad", fields)!.SetValue(page, true);
            typeof(global::PCL.PageLaunchLeft).GetField("IsLoadFinished", fields)!.SetValue(page, true);
            page.RefreshAfterPageEnter();

            Assert.Equal(4, global::PCL.Settings.Get<int>("LoginPageType"));
            Assert.Equal(global::PCL.ModLaunch.McLoginType.Auth,
                global::PCL.Settings.Get<global::PCL.ModLaunch.McLoginType>("LoginType"));
        } finally {
            global::PCL.ModMinecraft.McInstanceSelected = originalInstance;
            global::PCL.ModMinecraft.McFolderSelected = originalFolder;
            global::PCL.Settings.Set("LoginType", originalLoginType);
            global::PCL.Settings.Set("LoginPageType", originalPageType);
            global::PCL.Settings.Set("CacheAuthAccess", originalAuthAccess);
            global::PCL.Settings.Set("CacheAuthServerServer", originalAuthServer);
            global::PCL.Settings.Set("CacheAuthServerName", originalAuthName);
            global::PCL.Settings.Set("CacheAuthServerRegister", originalAuthRegister);
            global::PCL.ModMain.FrmInstanceSetup = originalInstanceSetup;
            foreach (var setting in global::PCL.Settings.Entries.Values)
                setting.ValueCache.TryRemove(instancePath, out _);
            Directory.Delete(temp, true);
        }
    }

    [AvaloniaFact]
    public void Instance_login_setting_survives_instance_metadata_write() {
        var temp = Path.Combine(Path.GetTempPath(), "pcl-instance-settings-" + Guid.NewGuid().ToString("N"));
        var instancePath = Path.Combine(temp, "versions", "TestInstance") + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(Path.Combine(instancePath, "PCL"));
        var originalInstanceSetup = global::PCL.ModMain.FrmInstanceSetup;
        try {
            global::PCL.ModMain.FrmInstanceSetup = null;
            // McInstance 使用 Windows 分隔符读写版本元数据
            var metadataPath = instancePath + "PCL\\Setup.ini";
            global::PCL.ModBase.WriteIni(metadataPath, "State", "9");
            // 用户在版本设置中选择第三方登录
            global::PCL.Settings.Set("VersionServerLogin", 4, instancePath);
            // 再次进入版本设置时 McInstance 会重写元数据，不能因此丢掉版本设置
            global::PCL.ModBase.WriteIni(metadataPath, "Info", "正式版 1.21.11");
            // 清空内存缓存，模拟重启后重新读取
            global::PCL.Settings.Entries["VersionServerLogin"].ValueCache.TryRemove(instancePath, out _);
            Assert.Equal(4, global::PCL.Settings.Get<int>("VersionServerLogin", instancePath));
            Assert.Contains("VersionServerLogin:4", File.ReadAllLines(Path.Combine(instancePath, "PCL", "Setup.ini")));
        } finally {
            global::PCL.ModMain.FrmInstanceSetup = originalInstanceSetup;
            Directory.Delete(temp, true);
        }
    }

    [AvaloniaFact]
    public void Launch_natives_extraction_keeps_files_when_instance_path_uses_backslashes() {
        var temp = Path.Combine(Path.GetTempPath(), "pcl-natives-" + Guid.NewGuid().ToString("N"));
        const string instanceName = "NativesTest";
        var instanceFolder = Path.Combine(temp, "versions", instanceName);
        Directory.CreateDirectory(instanceFolder);
        var platform = OperatingSystem.IsWindows() ? "natives-windows"
            : OperatingSystem.IsMacOS() ? "natives-osx" : "natives-linux";
        var libraryRelative = $"org/lwjgl/lwjgl/lwjgl-platform/2.9.0/lwjgl-platform-2.9.0-{platform}.jar";
        var libraryPath = Path.Combine(temp, "libraries", libraryRelative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(libraryPath)!);
        using (var zip = ZipFile.Open(libraryPath, ZipArchiveMode.Create)) {
            using var writer = new StreamWriter(zip.CreateEntry("liblwjgl.so").Open());
            writer.Write("stub");
        }
        File.WriteAllText(Path.Combine(instanceFolder, instanceName + ".json"), $$"""
            {
              "id": "{{instanceName}}",
              "type": "release",
              "libraries": [
                {
                  "name": "org.lwjgl.lwjgl:lwjgl-platform:2.9.0",
                  "natives": { "linux": "natives-linux", "windows": "natives-windows", "osx": "natives-osx" },
                  "downloads": {
                    "classifiers": {
                      "{{platform}}": { "path": "{{libraryRelative}}", "url": "https://example.invalid/x.jar", "size": 4, "sha1": "0000000000000000000000000000000000000000" }
                    }
                  }
                }
              ]
            }
            """);

        var originalFolder = global::PCL.ModMinecraft.McFolderSelected;
        var originalInstance = global::PCL.ModMinecraft.McInstanceSelected;
        try {
            // 生产环境中版本路径使用 Windows 分隔符，非 Windows 平台上解压与清理必须能正确匹配
            global::PCL.ModMinecraft.McFolderSelected = temp + "\\";
            global::PCL.ModMinecraft.McInstanceSelected = new global::PCL.ModMinecraft.McInstance(
                temp + "\\versions\\" + instanceName + "\\");
            var method = typeof(global::PCL.ModLaunch).GetMethod(
                "McLaunchNatives", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(method);
            method!.Invoke(null, new object?[] { null });

            var extracted = Path.Combine(instanceFolder, instanceName + "-natives", "liblwjgl.so");
            Assert.True(File.Exists(extracted), "解压后的 Natives 文件不应被清理步骤删除");
        } finally {
            global::PCL.ModMinecraft.McFolderSelected = originalFolder;
            global::PCL.ModMinecraft.McInstanceSelected = originalInstance;
            Directory.Delete(temp, true);
        }
    }

    [AvaloniaFact]
    public void Instance_custom_ram_radio_enables_slider_and_other_modes_disable_it() {
        var originalInstanceSetup = global::PCL.ModMain.FrmInstanceSetup;
        var page = new global::PCL.PageInstanceSetup();
        try {
            Assert.Same(page, global::PCL.ModMain.FrmInstanceSetup);
            page.RadioRamType1.SetChecked(true, true);
            Assert.True(page.SliderRamCustom.IsEnabled);

            page.RadioRamType2.SetChecked(true, true);
            Assert.False(page.SliderRamCustom.IsEnabled);
        } finally {
            global::PCL.ModMain.FrmInstanceSetup = originalInstanceSetup;
        }
    }

    [Fact]
    public void Legacy_versions_leave_window_wait_by_runtime_when_logs_lack_modern_markers() {
        var method = typeof(global::PCL.ModWatcher.Watcher)
            .GetMethod("IsLaunchReady", BindingFlags.Static | BindingFlags.NonPublic)!;
        var legacy = new DateTime(2009, 5, 14);
        var modern = new DateTime(2017, 6, 1);
        bool Ready(int logProgress, double seconds, DateTime releaseTime) =>
            (bool)method.Invoke(null, new object[] { logProgress, seconds, releaseTime })!;

        Assert.True(Ready(2, 0, modern));
        Assert.False(Ready(1, 100, modern));
        Assert.False(Ready(1, 100, new DateTime(1970, 1, 1)));
        Assert.False(Ready(1, 4.9, legacy));
        Assert.True(Ready(1, 5, legacy));
        Assert.False(Ready(0, 14.9, legacy));
        Assert.True(Ready(0, 15, legacy));
    }

    [Fact]
    public void X11_window_helper_enumerates_windows_when_display_available() {
        if (!OperatingSystem.IsLinux() || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY"))) return;
        var helper = typeof(global::PCL.ModWatcher).Assembly.GetType("PCL.X11WindowHelper")!;
        var open = helper.GetMethod("XOpenDisplay", BindingFlags.Public | BindingFlags.Static)!;
        var getList = helper.GetMethod("GetClientWindows", BindingFlags.Public | BindingFlags.Static)!;
        var isGame = helper.GetMethod("IsGameWindow", BindingFlags.Public | BindingFlags.Static)!;
        var close = helper.GetMethod("XCloseDisplay", BindingFlags.Public | BindingFlags.Static)!;
        var display = (IntPtr)open.Invoke(null, new object?[] { null })!;
        if (display == IntPtr.Zero) return;
        try {
            var windows = Assert.IsAssignableFrom<System.Collections.IEnumerable>(getList.Invoke(null, new object[] { display }));
            foreach (var id in windows.Cast<long>().Take(5))
                isGame.Invoke(null, new object[] { display, id, -1 });
        } finally {
            close.Invoke(null, new object[] { display });
        }
    }

}
