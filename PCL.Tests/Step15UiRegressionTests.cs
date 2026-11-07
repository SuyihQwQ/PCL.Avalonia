using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Text;
using Xunit;

namespace PCL.Tests;

public class UiRegressionTests {

    private static void Pump(int times = 12, int tickMs = 16) {
        for (var i = 0; i < times; i++) {
            global::PCL.ModAnimation.AniTimer(tickMs);
            Dispatcher.UIThread.RunJobs();
        }
    }

    [AvaloniaFact]
    public void Dropped_files_reach_the_file_drag_handler_even_with_text_format() {
        var missing = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "PCL-拖拽测试-" + Guid.NewGuid().ToString("N") + ".zip");
        System.IO.File.WriteAllBytes(missing, Array.Empty<byte>());
        var window = new global::PCL.FormMain();
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            var storageFile = window.StorageProvider.TryGetFileFromPathAsync(new Uri(missing))
                .GetAwaiter().GetResult();
            Assert.NotNull(storageFile);
            System.IO.File.Delete(missing);
            var data = new DataTransfer();
            data.Add(DataTransferItem.CreateText("https://example.com"));
            data.Add(DataTransferItem.CreateFile(storageFile));
            var target = (Avalonia.Interactivity.Interactive)window.PageRight;

            var enter = new DragEventArgs(DragDrop.DragEnterEvent, data, target, new Point(100, 100), KeyModifiers.None);
            target.RaiseEvent(enter);
            //Wayland 后端不支持 Link，必须反馈 Copy，否则拖动源会取消拖放
            var expectedEffect = OperatingSystem.IsLinux() && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"))
                ? DragDropEffects.Copy : DragDropEffects.Link;
            Assert.Equal(expectedEffect, enter.DragEffects);

            target.RaiseEvent(new DragEventArgs(DragDrop.DropEvent, data, target, new Point(100, 100), KeyModifiers.None));
            string? hintText = null;
            for (var i = 0; i < 100 && hintText == null; i++) {
                Pump(2);
                var hint = window.PanHint.Children.OfType<Border>().LastOrDefault();
                hintText = (hint?.Child as TextBlock)?.Text;
                Thread.Sleep(10);
            }
            Assert.NotNull(hintText);
            Assert.Contains("拖入的文件不存在", hintText);
        } finally {
            window.Close();
            Dispatcher.UIThread.RunJobs();
            try { System.IO.File.Delete(missing); } catch { }
        }
    }

    [AvaloniaFact]
    public void Hints_use_pcl_gradient_style_and_animation() {
        var window = new global::PCL.FormMain();
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            global::PCL.ModMain.Hint("测试提示", global::PCL.ModMain.HintType.Green, false);
            Dispatcher.UIThread.RunJobs();
            var panel = window.PanHint;
            var border = Assert.IsType<Border>(Assert.Single(panel.Children));
            var text = Assert.IsType<TextBlock>(border.Child);
            Assert.Equal("测试提示", text.Text);
            Assert.Same(Avalonia.Media.Brushes.White, text.Foreground);
            Assert.Equal(new CornerRadius(0, 6, 6, 0), border.CornerRadius);
            var gradient = Assert.IsType<Avalonia.Media.LinearGradientBrush>(border.Background);
            Assert.True(gradient.GradientStops[0].Color.G > gradient.GradientStops[0].Color.R);

            Pump(40);
            Assert.Equal(1, border.Opacity, 2);
            Assert.Equal(-20, border.Margin.Left, 1);
            Assert.Equal(30, border.Height, 1);
            Assert.Equal(18, text.LineHeight, 1);

            //重复提示复用同一条提示条
            global::PCL.ModMain.Hint("测试提示", global::PCL.ModMain.HintType.Green, false);
            Dispatcher.UIThread.RunJobs();
            Assert.Single(panel.Children);

            Pump(220);
            Assert.Empty(panel.Children);
        } finally {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }

    [AvaloniaFact]
    public void Combo_items_keep_their_own_padding_while_wrapped_items_stay_unpadded() {
        var combo = new global::PCL.MyComboBox { Width = 220, Height = 28 };
        var comboItem = new global::PCL.MyComboBoxItem { Content = "选项" };
        var listItem = new global::PCL.MyListItem { Title = "Java 8", Type = global::PCL.MyListItem.CheckType.Clickable };
        combo.Items.Add(comboItem);
        combo.Items.Add(listItem);
        var window = new Window { Width = 400, Height = 300, Content = combo };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            combo.IsDropDownOpen = true;
            Dispatcher.UIThread.RunJobs();
            var own = Assert.IsType<global::PCL.MyComboBoxItem>(combo.ContainerFromIndex(0));
            Assert.Same(comboItem, own);
            Assert.Equal(new Thickness(6, 4), own.Padding);
            var wrapped = Assert.IsType<ComboBoxItem>(combo.ContainerFromIndex(1));
            Assert.NotSame(listItem, wrapped);
            Assert.Equal(new Thickness(0), wrapped.Padding);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Combo_item_hover_uses_the_theme_hover_brush() {
        var originalMode = global::PCL.Settings.Get<int>("UiLauncherThemeMode");
        global::PCL.Settings.Set("UiLauncherThemeMode", 2);
        Dispatcher.UIThread.RunJobs();
        var item = new global::PCL.MyComboBoxItem {
            Content = "选项", Width = 200, Height = 30,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };
        var window = new Window { Width = 400, Height = 300, Content = item };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            window.MouseMove(new Point(100, 15));
            Dispatcher.UIThread.RunJobs();
            Pump(20);
            Assert.True(item.IsPointerOver);
            var expected = Assert.IsType<Avalonia.Media.SolidColorBrush>(
                Application.Current!.FindResource("ColorBrush8")).Color;
            Assert.Equal(expected, Assert.IsType<Avalonia.Media.SolidColorBrush>(item.Background).Color);
        } finally {
            window.Close();
            global::PCL.Settings.Set("UiLauncherThemeMode", originalMode);
            Dispatcher.UIThread.RunJobs();
        }
    }

    [AvaloniaFact]
    public void Button_hover_fades_monotonically_without_a_brightness_dip() {
        var originalMode = global::PCL.Settings.Get<int>("UiLauncherThemeMode");
        global::PCL.Settings.Set("UiLauncherThemeMode", 2);
        Dispatcher.UIThread.RunJobs();
        var button = new global::PCL.MyButton {
            Text = "启动游戏", Width = 260, Height = 54, ColorType = global::PCL.MyButton.ColorState.Highlight,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };
        var window = new Window {
            Width = 400, Height = 200, Content = button,
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(27, 35, 44))
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            Pump(10);
            var idle = SampleLuminance(window, button);
            window.MouseMove(new Point(130, 27));
            Dispatcher.UIThread.RunJobs();
            Assert.True(button.IsPointerOver);
            var enter = new List<double>();
            for (var i = 0; i < 8; i++) {
                global::PCL.ModAnimation.AniTimer(16);
                Dispatcher.UIThread.RunJobs();
                enter.Add(SampleLuminance(window, button));
            }
            global::PCL.ModAnimation.AniTimer(120);
            Dispatcher.UIThread.RunJobs();
            var hovered = SampleLuminance(window, button);
            Assert.True(Math.Abs(hovered - idle) > 1, $"悬停与静止状态没有区别：{idle} → {hovered}");
            const double Epsilon = 2;
            var enterMin = Math.Min(idle, hovered) - Epsilon;
            var enterMax = Math.Max(idle, hovered) + Epsilon;
            foreach (var value in enter) {
                Assert.InRange(value, enterMin, enterMax);
            }

            window.MouseMove(new Point(5, 180));
            Dispatcher.UIThread.RunJobs();
            var leave = new List<double>();
            for (var i = 0; i < 8; i++) {
                global::PCL.ModAnimation.AniTimer(16);
                Dispatcher.UIThread.RunJobs();
                leave.Add(SampleLuminance(window, button));
            }
            global::PCL.ModAnimation.AniTimer(200);
            Dispatcher.UIThread.RunJobs();
            var settled = SampleLuminance(window, button);
            var leaveMin = Math.Min(hovered, settled) - Epsilon;
            var leaveMax = Math.Max(hovered, settled) + Epsilon;
            foreach (var value in leave) {
                Assert.InRange(value, leaveMin, leaveMax);
            }
            Assert.InRange(settled, idle - Epsilon, idle + Epsilon);
        } finally {
            window.Close();
            global::PCL.Settings.Set("UiLauncherThemeMode", originalMode);
            Dispatcher.UIThread.RunJobs();
        }
    }

    private static double SampleLuminance(Window window, Control button) {
        var topLeft = button.TranslatePoint(new Point(0, 0), window);
        Assert.True(topLeft.HasValue);
        using var frame = Assert.IsType<Avalonia.Media.Imaging.WriteableBitmap>(window.CaptureRenderedFrame());
        using var locked = frame.Lock();
        var pixels = new byte[locked.RowBytes * locked.Size.Height];
        System.Runtime.InteropServices.Marshal.Copy(locked.Address, pixels, 0, pixels.Length);
        var x0 = (int)topLeft!.Value.X + 10;
        var y0 = (int)topLeft.Value.Y + 10;
        var x1 = (int)(topLeft.Value.X + button.Bounds.Width) - 10;
        var y1 = (int)(topLeft.Value.Y + button.Bounds.Height) - 10;
        double sum = 0;
        var count = 0;
        for (var y = y0; y < y1; y++) {
            for (var x = x0; x < x1; x++) {
                var offset = y * locked.RowBytes + x * 4;
                sum += 0.299 * pixels[offset + 2] + 0.587 * pixels[offset + 1] + 0.114 * pixels[offset];
                count++;
            }
        }
        Assert.True(count > 0);
        return sum / count;
    }

    [AvaloniaFact]
    public void Instance_setup_page_keeps_authlib_login_selection() {
        var root = Path.Combine(Path.GetTempPath(), "PCL-LoginRepro-" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(Path.Combine(root, "PCL"));
        System.IO.File.WriteAllText(Path.Combine(root, "PCL", "Setup.ini"), string.Join("\n", new[] {
            "VersionServerLogin:4",
            "VersionServerAuthServer:https://littleskin.cn/api/yggdrasil",
            "VersionServerAuthRegister:https://littleskin.cn/auth/register",
            "VersionServerAuthName:LittleSkin"
        }), Encoding.UTF8);

        var instance = new global::PCL.ModMinecraft.McInstance(root);
        var original = global::PCL.PageInstanceLeft.Instance;
        var originalSetup = global::PCL.ModMain.FrmInstanceSetup;
        global::PCL.PageInstanceLeft.Instance = instance;
        global::PCL.ModMain.FrmInstanceSetup = null;
        global::PCL.FormMain? window = null;
        try {
            window = new global::PCL.FormMain();
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.PageChange(global::PCL.FormMain.PageType.InstanceSetup, global::PCL.FormMain.PageSubType.InstanceSetup);
            for (var i = 0; i < 100; i++) {
                global::PCL.ModAnimation.AniTimer(16);
                Dispatcher.UIThread.RunJobs();
                Thread.Sleep(5);
            }
            var stored = global::PCL.Settings.Get<int>("VersionServerLogin", instance);
            Assert.True(stored == 4, $"进入版本设置后登录方式被改写为 {stored}");
            var page = Assert.IsType<global::PCL.PageInstanceSetup>(window.PageRight);
            Assert.Equal(5, page.ComboServerLogin.ItemCount);
            Assert.Equal(4, page.ComboServerLogin.SelectedIndex);

            global::PCL.Settings.Set("VersionServerLogin", 1, instance);
            //设置变化会触发版本缓存重载，测试环境里把选中版本换回临时实例
            global::PCL.PageInstanceLeft.Instance = instance;
            page.Reload();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(1, page.ComboServerLogin.SelectedIndex);
            Assert.Equal(1, global::PCL.Settings.Get<int>("VersionServerLogin", instance));
        } finally {
            window?.Close();
            global::PCL.PageInstanceLeft.Instance = original;
            global::PCL.ModMain.FrmInstanceSetup = originalSetup;
            try { System.IO.Directory.Delete(root, true); } catch { }
        }
    }

    [AvaloniaFact]
    public void Loading_stop_lets_current_cycle_finish_like_original() {
        var loading = new global::PCL.MyLoading {
            Width = 160, Height = 120,
            PickaxeRestAngle = 40, PickaxeSwingAngle = -10, PickaxeReboundAngle = 25
        };
        var window = new Window { Width = 300, Height = 200, Content = loading };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var group = "MyLoader Loop " + loading.Uuid;
        try {
            Pump(6);
            var angleBeforeStop = global::PCL.ModAnimation.AniAngle(loading.PathPickaxe);
            loading.State.LoadingState = global::PCL.MyLoading.MyLoadingState.Stop;
            Dispatcher.UIThread.RunJobs();
            //原版停止时不会立即中止当前循环
            Assert.True(global::PCL.ModAnimation.AniIsRun(group), "停止时不应立即掐断挥镐循环");
            Pump(160);
            Assert.False(global::PCL.ModAnimation.AniIsRun(group));
            //当前循环自然结束后停在配置的收尾角度
            Assert.Equal(40, global::PCL.ModAnimation.AniAngle(loading.PathPickaxe), 1);
            Assert.NotEqual(angleBeforeStop, global::PCL.ModAnimation.AniAngle(loading.PathPickaxe), 1);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Textbox_hint_is_visible_only_when_text_is_empty() {
        var box = new global::PCL.MyTextBox { HintText = "跟随全局设置" };
        var window = new Window { Width = 200, Height = 100, Content = box };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            var hint = box.GetVisualDescendants().OfType<TextBlock>().First(t => t.Name == "labHint");
            Assert.True(hint.IsVisible);
            Assert.Equal("跟随全局设置", hint.Text);
            box.Text = "abc";
            Dispatcher.UIThread.RunJobs();
            Assert.False(hint.IsVisible);
            box.Text = "";
            Dispatcher.UIThread.RunJobs();
            Assert.True(hint.IsVisible);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Radio_dot_stays_centered_when_checked_and_unchecked() {
        var radio = new global::PCL.MyRadioBox { Text = "测试" };
        var window = new Window { Width = 200, Height = 100, Content = radio };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            radio.Checked = true;
            Pump(60);
            Assert.Equal(10, radio.ShapeDot.Bounds.X + radio.ShapeDot.Bounds.Width / 2, 2);
            radio.Checked = false;
            Pump(60);
            Assert.Equal(0, global::PCL.ModAnimation.AniScaleX(radio.ShapeDot), 2);
            Assert.Equal(10, radio.ShapeDot.Bounds.X + radio.ShapeDot.Bounds.Width / 2, 2);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Combo_dropdown_keeps_card_title_highlighted_until_closed() {
        var card = new global::PCL.MyCard { Title = "测试卡片", Width = 300, Height = 200, Margin = new Thickness(50) };
        var combo = new global::PCL.MyComboBox {
            Width = 120, Margin = new Thickness(20, 80, 0, 0),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left
        };
        combo.Items.Add(new global::PCL.MyComboBoxItem { Content = "选项" });
        card.Children.Add(combo);
        var window = new Window { Width = 500, Height = 400, Content = card };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            var title = card.MainTextBlock;
            var blue = ((ISolidColorBrush)Application.Current!.FindResource("ColorBrush2")!).Color;
            var normal = ((ISolidColorBrush)Application.Current!.FindResource("ColorBrush1")!).Color;
            combo.IsDropDownOpen = true;
            Pump(20);
            Assert.Equal(blue, ((ISolidColorBrush)title.Foreground!).Color);
            window.MouseMove(new Point(450, 350));
            Pump(20);
            Assert.Equal(blue, ((ISolidColorBrush)title.Foreground!).Color);
            combo.IsDropDownOpen = false;
            Pump(20);
            Assert.Equal(normal, ((ISolidColorBrush)title.Foreground!).Color);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Jvm_textboxes_keep_a_little_top_padding() {
        var original = global::PCL.ModMain.FrmInstanceSetup;
        try {
            var instancePage = new global::PCL.PageInstanceSetup();
            Assert.Equal(7, instancePage.TextAdvanceJvm.Padding.Top);
            var globalPage = new global::PCL.PageSetupLaunch();
            Assert.Equal(7, globalPage.TextAdvanceJvm.Padding.Top);
        } finally {
            global::PCL.ModMain.FrmInstanceSetup = original;
        }
    }

    [AvaloniaFact]
    public void Launch_environment_lines_are_applied_to_the_game_process() {
        var oldEnv = global::PCL.Settings.Get<string>("LaunchEnvironment");
        var oldInstance = global::PCL.ModMinecraft.McInstanceSelected;
        try {
            global::PCL.ModMinecraft.McInstanceSelected = null;
            global::PCL.Settings.Set("LaunchEnvironment", "PCL_TEST_ENV=bar;# comment;PCL_TEST_ENV2=a=b");
            var startInfo = new System.Diagnostics.ProcessStartInfo();
            var method = typeof(global::PCL.ModLaunch).GetMethod(
                "ApplyLaunchEnvironment", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(method);
            method!.Invoke(null, new object[] { startInfo });
            Assert.Equal("bar", startInfo.Environment["PCL_TEST_ENV"]);
            Assert.Equal("a=b", startInfo.Environment["PCL_TEST_ENV2"]);
        } finally {
            global::PCL.Settings.Set("LaunchEnvironment", oldEnv);
            global::PCL.ModMinecraft.McInstanceSelected = oldInstance;
        }
    }

    [AvaloniaFact]
    public void New_launch_settings_are_registered() {
        Assert.Equal("", global::PCL.Settings.GetDefault("LaunchEnvironment"));
        Assert.Equal("", global::PCL.Settings.GetDefault("LaunchAdvanceWrapper"));
        Assert.Equal("", global::PCL.Settings.GetDefault("LaunchAdvanceAfterExit"));
        Assert.Equal("", global::PCL.Settings.GetDefault("VersionEnvironment"));
        Assert.Equal("", global::PCL.Settings.GetDefault("VersionAdvanceWrapper"));
        Assert.Equal("", global::PCL.Settings.GetDefault("VersionAdvanceAfterExit"));
    }

    [AvaloniaFact]
    public void Wrapper_command_is_prepended_to_the_java_command() {
        var oldWrapper = global::PCL.Settings.Get<string>("LaunchAdvanceWrapper");
        var oldInstance = global::PCL.ModMinecraft.McInstanceSelected;
        try {
            global::PCL.ModMinecraft.McInstanceSelected = null;
            global::PCL.Settings.Set("LaunchAdvanceWrapper", "mangohud --dlsym");
            var command = global::PCL.ModLaunch.GetLaunchProcessCommand("/usr/bin/java", "-Xmx2G -jar game.jar");
            Assert.Equal("mangohud", command.FileName);
            Assert.Equal("--dlsym \"/usr/bin/java\" -Xmx2G -jar game.jar", command.Arguments);
        } finally {
            global::PCL.Settings.Set("LaunchAdvanceWrapper", oldWrapper);
            global::PCL.ModMinecraft.McInstanceSelected = oldInstance;
        }
    }

    [AvaloniaFact]
    public void Authlib_login_falls_back_to_cached_server_for_instance_without_own_server() {
        var temp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pcl-auth-fallback-" + Guid.NewGuid().ToString("N"));
        var instancePath = System.IO.Path.Combine(temp, "versions", "Test") + System.IO.Path.DirectorySeparatorChar;
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(instancePath, "PCL"));
        var oldInstance = global::PCL.ModMinecraft.McInstanceSelected;
        var oldServer = global::PCL.Settings.Get<string>("CacheAuthServerServer");
        try {
            global::PCL.Settings.Set("CacheAuthServerServer", "https://littleskin.cn/api/yggdrasil");
            global::PCL.Settings.Set("VersionServerLogin", 4, instancePath);
            global::PCL.Settings.Set("VersionServerAuthServer", "", instancePath);
            global::PCL.ModMinecraft.McInstanceSelected = new global::PCL.ModMinecraft.McInstance(instancePath);
            var data = global::PCL.PageLoginAuthSkin.GetLoginData();
            Assert.Equal("https://littleskin.cn/api/yggdrasil/authserver", data.BaseUrl);
        } finally {
            global::PCL.ModMinecraft.McInstanceSelected = oldInstance;
            global::PCL.Settings.Set("CacheAuthServerServer", oldServer);
            foreach (var setting in global::PCL.Settings.Entries.Values) setting.ValueCache.TryRemove(instancePath, out _);
            try { System.IO.Directory.Delete(temp, true); } catch { }
        }
    }

    [AvaloniaFact]
    public void Authlib_server_address_persists_across_reload() {
        var temp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pcl-auth-save-" + Guid.NewGuid().ToString("N"));
        var mcFolder = System.IO.Path.Combine(temp, ".minecraft") + System.IO.Path.DirectorySeparatorChar;
        var versionPath = System.IO.Path.Combine(mcFolder, "versions", "Test") + System.IO.Path.DirectorySeparatorChar;
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(versionPath, "PCL"));
        var oldFolder = global::PCL.ModMinecraft.McFolderSelected;
        var oldInstance = global::PCL.ModMinecraft.McInstanceSelected;
        var oldPageInstance = global::PCL.PageInstanceLeft.Instance;
        var oldSetup = global::PCL.ModMain.FrmInstanceSetup;
        global::PCL.FormMain? main = null;
        Window? window = null;
        string? instancePath = null;
        try {
            global::PCL.ModMinecraft.McFolderSelected = mcFolder;
            //新主窗体会把 FrmMain 重置到启动页，避免其他用例残留的页面状态影响 RefreshRam
            main = new global::PCL.FormMain();
            var instance = new global::PCL.ModMinecraft.McInstance("Test");
            instancePath = instance.PathVersion;
            global::PCL.ModMinecraft.McInstanceSelected = instance;
            global::PCL.PageInstanceLeft.Instance = instance;
            global::PCL.ModMain.FrmInstanceSetup = null;
            var page = new global::PCL.PageInstanceSetup();
            window = new Window { Width = 900, Height = 700, Content = page };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            Assert.True(page.IsLoaded, "版本设置页未能挂载到视觉树");
            page.ComboServerLogin.SelectedIndex = 4;
            Dispatcher.UIThread.RunJobs();
            page.TextServerAuthServer.Focus();
            Dispatcher.UIThread.RunJobs();
            //粘贴时携带的空白应当被清理并正常保存
            page.TextServerAuthServer.Text = "  https://example.com/api/yggdrasil  ";
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("https://example.com/api/yggdrasil", page.TextServerAuthServer.Text);
            Assert.Equal("https://example.com/api/yggdrasil", global::PCL.Settings.Get<string>("VersionServerAuthServer", instancePath));
            //未填写协议头的地址在离开输入框时自动补全
            page.TextServerAuthServer.Text = "littleskin.cn/api/yggdrasil";
            typeof(global::PCL.PageInstanceSetup)
                .GetMethod("TextServerAuthServer_LostFocus", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(page, new object[] { page.TextServerAuthServer, new Avalonia.Interactivity.RoutedEventArgs() });
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("https://littleskin.cn/api/yggdrasil", global::PCL.Settings.Get<string>("VersionServerAuthServer", instancePath));
            page.Reload();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("https://littleskin.cn/api/yggdrasil", page.TextServerAuthServer.Text);
        } finally {
            window?.Close();
            main?.Close();
            global::PCL.ModMinecraft.McFolderSelected = oldFolder;
            global::PCL.ModMinecraft.McInstanceSelected = oldInstance;
            global::PCL.PageInstanceLeft.Instance = oldPageInstance;
            global::PCL.ModMain.FrmInstanceSetup = oldSetup;
            if (instancePath != null) {
                foreach (var setting in global::PCL.Settings.Entries.Values) setting.ValueCache.TryRemove(instancePath, out _);
            }
            try { System.IO.Directory.Delete(temp, true); } catch { }
        }
    }

    [AvaloniaFact]
    public void Closed_combobox_wheel_does_not_change_selection() {
        var combo = new global::PCL.MyComboBox();
        combo.Items.Add(new global::PCL.MyComboBoxItem { Content = "A" });
        combo.Items.Add(new global::PCL.MyComboBoxItem { Content = "B" });
        var window = new Window { Width = 240, Height = 120, Content = combo };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            combo.SelectedIndex = 0;
            combo.Focus();
            Dispatcher.UIThread.RunJobs();
            Assert.True(combo.IsFocused);
            window.MouseWheel(new Point(120, 60), new Vector(0, -1), RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(0, combo.SelectedIndex);
            window.MouseWheel(new Point(120, 60), new Vector(0, 1), RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(0, combo.SelectedIndex);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Scrollviewer_can_skip_reserved_scrollbar_space_for_short_lists() {
        var viewer = new global::PCL.MyScrollViewer {
            ReserveScrollBarSpace = false,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = new TextBlock { Text = "short" }
        };
        var window = new Window { Width = 200, Height = 100, Content = viewer };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            var root = viewer.GetVisualDescendants().OfType<Grid>().First(g => g.Name == "PART_Root");
            Assert.True(root.ColumnDefinitions[1].Width.IsAuto);
            viewer.ReserveScrollBarSpace = true;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(10, root.ColumnDefinitions[1].Width.Value);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Long_list_still_shows_scrollbar_without_reserved_space() {
        var viewer = new global::PCL.MyScrollViewer {
            ReserveScrollBarSpace = false,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Height = 40,
            Content = new Border { Height = 200 }
        };
        var window = new Window { Width = 200, Height = 100, Content = viewer };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            var bar = viewer.GetVisualDescendants().OfType<global::PCL.MyScrollBar>()
                .First(b => b.Orientation == Avalonia.Layout.Orientation.Vertical);
            Assert.True(bar.IsVisible, "长列表应显示纵向滚动条");
            Assert.True(bar.Bounds.Width > 0, "滚动条被列宽裁掉了");
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Card_title_does_not_flash_when_dropdown_closes() {
        var card = new global::PCL.MyCard { Title = "卡片", Width = 300, Height = 150 };
        var window = new Window { Width = 400, Height = 300, Content = card };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            var title = card.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == "卡片");
            card.SetHoverLock(card, true);
            Pump(10);
            var hoverColor = Assert.IsAssignableFrom<ISolidColorBrush>(title.Foreground).Color;
            //模拟下拉框展开时指针位于弹层上，关闭后指针状态短暂抖动
            window.MouseMove(new Point(350, 250));
            Dispatcher.UIThread.RunJobs();
            card.SetHoverLock(card, false);
            Dispatcher.UIThread.RunJobs();
            global::PCL.ModAnimation.AniTimer(40);
            Dispatcher.UIThread.RunJobs();
            //释放悬停锁后不应立即取消高亮，否则重新进入卡片时标题会闪一下
            Assert.Equal(hoverColor, Assert.IsAssignableFrom<ISolidColorBrush>(title.Foreground).Color);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Card_hover_does_not_flash_on_transient_pointer_exit() {
        var card = new global::PCL.MyCard { Title = "卡片", Width = 300, Height = 150 };
        var window = new Window { Width = 400, Height = 300, Content = card };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            var title = card.MainTextBlock;
            var blue = ((ISolidColorBrush)Application.Current!.FindResource("ColorBrush2")!).Color;
            var inside = card.TranslatePoint(new Point(100, 40), window)!.Value;
            window.MouseMove(inside);
            Dispatcher.UIThread.RunJobs();
            Pump(10);
            Assert.Equal(blue, ((ISolidColorBrush)title.Foreground!).Color);

            //指针捕获切换会短暂触发离开事件，延迟确认期间标题不应开始淡出
            window.MouseMove(new Point(380, 280));
            Dispatcher.UIThread.RunJobs();
            global::PCL.ModAnimation.AniTimer(30);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(blue, ((ISolidColorBrush)title.Foreground!).Color);

            window.MouseMove(inside);
            Dispatcher.UIThread.RunJobs();
            Pump(10);
            Assert.Equal(blue, ((ISolidColorBrush)title.Foreground!).Color);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Card_hover_shows_shadow() {
        var card = new global::PCL.MyCard { Title = "卡片", Width = 300, Height = 150 };
        var window = new Window { Width = 400, Height = 300, Content = card };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            var chrome = card.MainChrome;
            Assert.Equal(new Thickness(0), chrome.Margin);
            Assert.Equal(card.Bounds.Width, chrome.Bounds.Width, 1);
            Assert.Equal(card.Bounds.Height, chrome.Bounds.Height, 1);
            Assert.InRange(chrome.Opacity, 0.06, 0.08);
            var hoverColor = (Color)Application.Current!.FindResource("ColorObject4")!;

            window.MouseMove(card.TranslatePoint(new Point(100, 40), window)!.Value);
            Dispatcher.UIThread.RunJobs();
            Pump(10);
            Assert.InRange(chrome.Opacity, 0.39, 0.41);
            Assert.Equal(hoverColor, chrome.Color);

            window.MouseMove(new Point(380, 280));
            Dispatcher.UIThread.RunJobs();
            Pump(10);
            Assert.InRange(chrome.Opacity, 0.06, 0.08);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Card_rapid_toggle_does_not_leave_hidden_content_in_expanded_card() {
        var card = new global::PCL.MyCard { Title = "卡片", CanSwap = true, Width = 300 };
        var content = new StackPanel { Margin = new Thickness(25, 44, 25, 15) };
        content.Children.Add(new TextBox { Height = 100, Text = "内容" });
        card.Children.Add(content);
        var window = new Window { Width = 400, Height = 400, Content = card };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            Pump(10);
            card.IsSwapped = true;
            Pump(3);
            card.IsSwapped = false;
            Pump(20);
            Assert.True(card.SwapControl.IsVisible);
            Assert.True(card.Bounds.Height > 80);

            card.IsSwapped = true;
            Pump(2);
            card.IsSwapped = false;
            Pump(2);
            card.IsSwapped = true;
            Pump(30);
            Assert.Equal(40, card.Height, 1.5);
            Assert.False(card.SwapControl.IsVisible);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Tooltips_follow_pointer_movement() {
        var window = new global::PCL.FormMain { Width = 900, Height = 600 };
        var button = new Button {
            Width = 120, Height = 40, Content = "悬停",
            Margin = new Thickness(50, 100, 0, 0),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };
        ToolTip.SetTip(button, "详细信息");
        ((Panel)window.Content!).Children.Add(button);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            Assert.False(ToolTip.GetServiceEnabled(button));
            var start = button.TranslatePoint(new Point(60, 20), window)!.Value;
            window.MouseMove(start);
            Dispatcher.UIThread.RunJobs();
            System.Threading.Thread.Sleep(150);
            Dispatcher.UIThread.RunJobs();
            typeof(global::PCL.FormMain).GetMethod("ShowHoverTip", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(window, null);
            Assert.True(window.BorderToolTip.IsVisible);
            Assert.Equal("详细信息", window.LabToolTip.Text);
            var left = Canvas.GetLeft(window.BorderToolTip);
            window.MouseMove(start + new Vector(30, 0));
            Dispatcher.UIThread.RunJobs();
            Assert.InRange(Canvas.GetLeft(window.BorderToolTip) - left, 28, 32);
            window.MouseMove(new Point(700, 500));
            Dispatcher.UIThread.RunJobs();
            Pump(10);
            Assert.False(window.BorderToolTip.IsVisible);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Hover_tip_closes_when_dropdown_opens_and_returns_after_close() {
        var window = new global::PCL.FormMain { Width = 900, Height = 600 };
        var combo = new global::PCL.MyComboBox {
            Width = 160, Height = 28, Margin = new Thickness(50, 100, 0, 0),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };
        combo.Items.Add(new global::PCL.MyComboBoxItem { Content = "选项" });
        ToolTip.SetTip(combo, "下拉提示");
        ((Panel)window.Content!).Children.Add(combo);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            var showHoverTip = typeof(global::PCL.FormMain).GetMethod("ShowHoverTip", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            var start = combo.TranslatePoint(new Point(30, 14), window)!.Value;
            window.MouseMove(start);
            Dispatcher.UIThread.RunJobs();
            showHoverTip.Invoke(window, null);
            Assert.True(window.BorderToolTip.IsVisible);

            combo.IsDropDownOpen = true;
            Dispatcher.UIThread.RunJobs();
            Pump(10);
            Assert.False(window.BorderToolTip.IsVisible, "打开下拉菜单后悬停提示应关闭");

            //展开期间移动鼠标也不应重新显示
            window.MouseMove(start + new Vector(2, 0));
            Dispatcher.UIThread.RunJobs();
            showHoverTip.Invoke(window, null);
            Pump(10);
            Assert.False(window.BorderToolTip.IsVisible);

            combo.IsDropDownOpen = false;
            Dispatcher.UIThread.RunJobs();
            //关闭后重新悬停可以再次显示
            window.MouseMove(start + new Vector(4, 0));
            Dispatcher.UIThread.RunJobs();
            showHoverTip.Invoke(window, null);
            Assert.True(window.BorderToolTip.IsVisible);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Clicking_textbox_keeps_hover_tip_and_blank_click_exits_input() {
        var window = new global::PCL.FormMain { Width = 900, Height = 600 };
        var box = new global::PCL.MyTextBox {
            Width = 180, Height = 28, Text = "abc",
            Margin = new Thickness(60, 120, 0, 0),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };
        ToolTip.SetTip(box, "输入提示");
        ((Panel)window.Content!).Children.Add(box);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            var inside = box.TranslatePoint(new Point(40, 14), window)!.Value;
            var outside = new Point(700, 500);
            window.MouseMove(inside);
            Dispatcher.UIThread.RunJobs();
            typeof(global::PCL.FormMain).GetMethod("ShowHoverTip", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(window, null);
            Assert.True(window.BorderToolTip.IsVisible);

            //点击输入框本身不会关闭悬停提示，也不会导致需要重新进入才能显示
            window.MouseDown(inside, MouseButton.Left, RawInputModifiers.None);
            window.MouseUp(inside, MouseButton.Left, RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();
            Assert.True(box.IsFocused, "点击输入框应保持输入状态");
            Pump(10);
            Assert.True(window.BorderToolTip.IsVisible, "点击输入框后悬停提示应保持显示");

            //点击输入框以外的地方：退出输入状态并关闭悬停提示
            window.MouseDown(outside, MouseButton.Left, RawInputModifiers.None);
            window.MouseUp(outside, MouseButton.Left, RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();
            Assert.False(box.IsFocused, "点击输入框以外应退出输入状态");
            Pump(10);
            Assert.False(window.BorderToolTip.IsVisible);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Slider_drag_outside_card_restores_title_and_suppresses_hover_tip() {
        var window = new global::PCL.FormMain { Width = 900, Height = 600 };
        var card = new global::PCL.MyCard {
            Title = "拖动卡片", Width = 400, Height = 250,
            Margin = new Thickness(40, 40, 0, 0),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };
        var slider = new global::PCL.MySlider {
            Width = 200, Height = 16, MaxValue = 100, Value = 0,
            Margin = new Thickness(40, 120, 0, 0),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };
        card.Children.Add(slider);
        var button = new Button {
            Width = 120, Height = 40, Content = "提示",
            Margin = new Thickness(620, 420, 0, 0),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };
        ToolTip.SetTip(button, "其他提示");
        ((Panel)window.Content!).Children.Add(card);
        ((Panel)window.Content!).Children.Add(button);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            var title = card.MainTextBlock;
            var blue = ((ISolidColorBrush)Application.Current!.FindResource("ColorBrush2")!).Color;
            var normal = ((ISolidColorBrush)Application.Current!.FindResource("ColorBrush1")!).Color;
            var sliderPoint = slider.TranslatePoint(new Point(100, 8), window)!.Value;
            var outside = new Point(700, 500);
            window.MouseMove(sliderPoint);
            Dispatcher.UIThread.RunJobs();
            Pump(10);
            Assert.Equal(blue, ((ISolidColorBrush)title.Foreground!).Color);

            window.MouseDown(sliderPoint, MouseButton.Left, RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();
            //拖动中离开移动点但仍在卡片内时，悬停状态应与普通悬停一致
            var insideCard = card.TranslatePoint(new Point(220, 60), window)!.Value;
            window.MouseMove(insideCard);
            Dispatcher.UIThread.RunJobs();
            Pump(10);
            Assert.Equal(blue, ((ISolidColorBrush)title.Foreground!).Color);
            window.MouseMove(outside);
            Dispatcher.UIThread.RunJobs();
            Pump(10);
            //拖动滑块离开卡片后，标题恢复普通颜色
            Assert.Equal(normal, ((ISolidColorBrush)title.Foreground!).Color);

            //拖动期间即使移到其他带提示的控件上也不显示悬停提示
            typeof(global::PCL.FormMain).GetMethod("ShowHoverTip", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(window, null);
            Pump(10);
            Assert.False(window.BorderToolTip.IsVisible);

            window.MouseUp(outside, MouseButton.Left, RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Legacy_inline_link_still_receives_pointer_clicks() {
        const string input = "<TextBlock><Underline><local:MyTextButton>链接</local:MyTextButton></Underline></TextBlock>";
        var converted = global::PCL.ModHelp.ConvertLegacyXaml(input);
        var wrapped = "<StackPanel xmlns=\"https://github.com/avaloniaui\" xmlns:local=\"using:PCL\">" + converted + "</StackPanel>";
        var root = Assert.IsAssignableFrom<Control>(global::PCL.ModHelp.GetObjectFromXML(wrapped));
        var window = new Window { Width = 300, Height = 100, Content = root };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            var link = root.GetVisualDescendants().OfType<global::PCL.MyTextButton>().First();
            var clicked = false;
            link.Click += (_, _) => clicked = true;
            var center = link.TranslatePoint(new Point(link.Bounds.Width / 2, link.Bounds.Height / 2), window)!.Value;
            window.MouseDown(center, MouseButton.Left);
            window.MouseUp(center, MouseButton.Left);
            Pump(4);
            Assert.True(clicked);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Legacy_paragraph_line_height_does_not_leak_to_other_text_blocks() {
        const string input =
            "<StackPanel.Resources>" +
            "<Style TargetType=\"Paragraph\"><Setter Property=\"LineHeight\" Value=\"12\"/></Style>" +
            "<Style TargetType=\"TextBlock\"><Setter Property=\"FontSize\" Value=\"14\"/></Style>" +
            "</StackPanel.Resources><Paragraph>段落</Paragraph><TextBlock>普通文本</TextBlock>";
        var converted = global::PCL.ModHelp.ConvertLegacyXaml(input);
        Assert.DoesNotContain("LineHeight", converted);
        Assert.Contains("Margin=\"0,8\"", converted);
    }

    [AvaloniaFact]
    public void Launching_view_keeps_its_scale_when_reentering_the_page() {
        var window = new global::PCL.FormMain { Width = 900, Height = 600 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            var page = global::PCL.ModMain.FrmLaunchLeft;
            page.PageChangeToLaunching();
            Pump(40);
            Assert.Equal(1, global::PCL.ModAnimation.AniScaleX(page.PanLaunching), 2);
            var loaded = typeof(global::PCL.PageLaunchLeft).GetMethod(
                "PageLaunchLeft_Loaded", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(loaded);
            loaded!.Invoke(page, new object?[] { null, new Avalonia.Interactivity.RoutedEventArgs() });
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(1, global::PCL.ModAnimation.AniScaleX(page.PanLaunching), 2);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Authlib_login_keeps_skin_page_when_only_cached_name_remains() {
        var oldPageType = global::PCL.Settings.Get<int>("LoginPageType");
        var oldLoginType = global::PCL.Settings.Get<global::PCL.ModLaunch.McLoginType>("LoginType");
        var oldAccess = global::PCL.Settings.Get<string>("CacheAuthAccess");
        var oldName = global::PCL.Settings.Get<string>("CacheAuthName");
        var oldUuid = global::PCL.Settings.Get<string>("CacheAuthUuid");
        var oldUser = global::PCL.Settings.Get<string>("CacheAuthUsername");
        var oldPass = global::PCL.Settings.Get<string>("CacheAuthPass");
        var window = new global::PCL.FormMain { Width = 900, Height = 600 };
        try {
            global::PCL.Settings.Set("LoginPageType", 4);
            global::PCL.Settings.Set("CacheAuthAccess", "");
            global::PCL.Settings.Set("CacheAuthName", "TestUser");
            global::PCL.Settings.Set("CacheAuthUuid", "00000000-0000-0000-0000-000000000000");
            global::PCL.Settings.Set("CacheAuthUsername", "");
            global::PCL.Settings.Set("CacheAuthPass", "");
            window.Show();
            Dispatcher.UIThread.RunJobs();
            var page = global::PCL.ModMain.FrmLaunchLeft;
            page.RefreshPage(false, false, ForceReload: true);
            Dispatcher.UIThread.RunJobs();
            Assert.IsType<global::PCL.PageLoginAuthSkin>(page.PanLogin.Children[0]);
            page.PageChangeToLogin();
            Dispatcher.UIThread.RunJobs();
            Assert.IsType<global::PCL.PageLoginAuthSkin>(page.PanLogin.Children[0]);
        } finally {
            window.Close();
            global::PCL.Settings.Set("LoginPageType", oldPageType);
            global::PCL.Settings.Set("LoginType", oldLoginType);
            global::PCL.Settings.Set("CacheAuthAccess", oldAccess);
            global::PCL.Settings.Set("CacheAuthName", oldName);
            global::PCL.Settings.Set("CacheAuthUuid", oldUuid);
            global::PCL.Settings.Set("CacheAuthUsername", oldUser);
            global::PCL.Settings.Set("CacheAuthPass", oldPass);
        }
    }

    [AvaloniaFact]
    public void After_exit_command_runs_in_the_selected_minecraft_folder() {
        var temp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pcl-after-exit-" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(temp);
        var oldFolder = global::PCL.ModMinecraft.McFolderSelected;
        var oldInstance = global::PCL.ModMinecraft.McInstanceSelected;
        try {
            global::PCL.ModMinecraft.McFolderSelected = temp + System.IO.Path.DirectorySeparatorChar;
            global::PCL.ModMinecraft.McInstanceSelected = null;
            var method = typeof(global::PCL.ModLaunch).GetMethod(
                "RunAfterExitCommand", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(method);
            method!.Invoke(null, new object[] { "echo done>pcl-after-exit.txt", "测试", 0 });
            var marker = System.IO.Path.Combine(temp, "pcl-after-exit.txt");
            Assert.True(System.IO.File.Exists(marker));
            Assert.Contains("done", System.IO.File.ReadAllText(marker));
        } finally {
            global::PCL.ModMinecraft.McFolderSelected = oldFolder;
            global::PCL.ModMinecraft.McInstanceSelected = oldInstance;
            try { System.IO.Directory.Delete(temp, true); } catch { }
        }
    }
}
