using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
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
            Assert.Equal(26, border.Height, 1);

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
    public void Loading_animation_returns_to_the_configured_rest_angle() {
        var loading = new global::PCL.MyLoading {
            Width = 160, Height = 120,
            PickaxeRestAngle = 40, PickaxeSwingAngle = -10, PickaxeReboundAngle = 25
        };
        var window = new Window { Width = 300, Height = 200, Content = loading };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            Pump(6);
            loading.State.LoadingState = global::PCL.MyLoading.MyLoadingState.Stop;
            global::PCL.ModAnimation.AniTimer(300);
            Dispatcher.UIThread.RunJobs();
            Pump(10);
            Assert.Equal(40, global::PCL.ModAnimation.AniAngle(loading.PathPickaxe), 2);
        } finally {
            window.Close();
        }
    }
}
