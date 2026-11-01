using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Runtime.InteropServices;
using Xunit;

namespace PCL.Tests;

public class CoreControlTests {

    private static Window Show(Control content) {
        var window = new Window { Width = 500, Height = 400, Content = content };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    [AvaloniaFact]
    public void Button_and_icon_button_raise_click() {
        var button = new global::PCL.MyButton {
            Text = "按钮", Width = 120, Height = 32,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };
        var icon = new global::PCL.MyIconButton {
            Width = 32, Height = 32,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };
        var panel = new StackPanel { Spacing = 10 };
        panel.Children.Add(button);
        panel.Children.Add(icon);
        var window = Show(panel);
        try {
            var buttonClicked = 0;
            var iconClicked = 0;
            button.Click += (_, _) => buttonClicked++;
            icon.Click += (_, _) => iconClicked++;
            window.MouseDown(new Point(30, 16), MouseButton.Left);
            window.MouseUp(new Point(30, 16), MouseButton.Left);
            window.MouseDown(new Point(16, 58), MouseButton.Left);
            window.MouseUp(new Point(16, 58), MouseButton.Left);
            global::PCL.ModAnimation.AniTimer(500);
            Assert.Equal(1, buttonClicked);
            Assert.Equal(1, iconClicked);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Text_button_shows_text() {
        var button = new global::PCL.MyTextButton { Text = "了解更多" };
        Assert.Equal("了解更多", button.Text);
        var window = Show(button);
        try {
            global::PCL.ModAnimation.AniTimer(300);
            Assert.True(button.IsVisible);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Card_swap_hides_content_and_fixes_height() {
        var content = new StackPanel();
        content.Children.Add(new Border { Height = 120, Child = new TextBlock { Text = "内容" } });
        var card = new global::PCL.MyCard { Title = "标题", CanSwap = true };
        card.Children.Add(content);
        var host = new StackPanel { VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top };
        host.Children.Add(card);
        var window = Show(host);
        try {
            Assert.NotNull(card.MainTextBlock);
            Assert.NotNull(card.MainSwap);
            Assert.Same(content, card.SwapControl);

            card.IsSwapped = true;
            Assert.True(content.IsVisible);
            Assert.NotNull(content.Clip);
            window.CaptureRenderedFrame();
            global::PCL.ModAnimation.AniTimer(30);
            window.CaptureRenderedFrame();
            global::PCL.ModAnimation.AniTimer(400);
            window.CaptureRenderedFrame();
            global::PCL.ModAnimation.AniTimer(10);
            window.CaptureRenderedFrame();
            Assert.Equal(40d, card.Height);
            Assert.False(content.IsVisible);
            Assert.Null(content.Clip);

            card.IsSwapped = false;
            Assert.True(double.IsNaN(card.Height));
            Assert.True(content.IsVisible);
            window.CaptureRenderedFrame();
            Dispatcher.UIThread.RunJobs();
            Assert.NotNull(content.Clip);
            global::PCL.ModAnimation.AniTimer(30);
            window.CaptureRenderedFrame();
            global::PCL.ModAnimation.AniTimer(400);
            window.CaptureRenderedFrame();
            global::PCL.ModAnimation.AniTimer(10);
            window.CaptureRenderedFrame();
            Assert.Null(content.Clip);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Hint_theme_sets_background() {
        var hint = new global::PCL.MyHint { Text = "提示", HintTheme = global::PCL.MyHint.Themes.Blue };
        var window = Show(hint);
        try {
            var brush = Assert.IsType<SolidColorBrush>(hint.Background);
            Assert.Equal(Color.FromRgb(0xD9, 0xEC, 0xFF), brush.Color);
            hint.HintTheme = global::PCL.MyHint.Themes.Yellow;
            Dispatcher.UIThread.RunJobs();
            brush = Assert.IsType<SolidColorBrush>(hint.Background);
            Assert.Equal(Color.FromRgb(0xFF, 0xEB, 0xD7), brush.Color);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void List_item_pointer_toggles_checkbox_and_raises_click() {
        var item = new global::PCL.MyListItem {
            Title = "选项",
            Type = global::PCL.MyListItem.CheckType.CheckBox,
            Width = 300,
            Height = 42,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };
        var window = Show(item);
        try {
            var clicks = 0;
            item.Click += (_, _) => clicks++;
            window.MouseDown(new Point(120, 21), MouseButton.Left);
            window.MouseUp(new Point(120, 21), MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(1, clicks);
            Assert.True(item.Checked);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Button_text_padding_widens_button() {
        var plain = new global::PCL.MyButton {
            Text = "版本设置", Height = 35,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left
        };
        var padded = new global::PCL.MyButton {
            Text = "版本设置", Height = 35, TextPadding = new Thickness(36, 0),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left
        };
        var panel = new StackPanel();
        panel.Children.Add(plain);
        panel.Children.Add(padded);
        var window = Show(panel);
        try {
            Assert.True(padded.Bounds.Width >= plain.Bounds.Width + 60,
                $"plain={plain.Bounds.Width}, padded={padded.Bounds.Width}");
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void List_item_checked_before_layout_shows_full_indicator_bar() {
        // 回归：列表重建时会在首次布局前恢复选中状态，此时按实际高度做动画会把勾选条压成 0 高
        var panel = new StackPanel();
        var window = Show(panel);
        try {
            var item = new global::PCL.MyListItem {
                Title = "文件夹",
                Type = global::PCL.MyListItem.CheckType.RadioBox,
                Height = 40
            };
            panel.Children.Add(item);
            item.Checked = true;
            Dispatcher.UIThread.RunJobs();
            Assert.NotNull(item.RectCheck);
            Assert.Equal(28, item.RectCheck!.Bounds.Height, 2);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Loading_state_drives_text_and_error_visuals() {
        var loading = new global::PCL.MyLoading { AutoRun = false, HasAnimation = false };
        var state = new global::PCL.MyLoadingStateSimulator();
        loading.State = state;
        var window = Show(loading);
        try {
            var label = loading.FindControl<TextBlock>("LabText")!;
            state.LoadingState = global::PCL.MyLoading.MyLoadingState.Run;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("加载中", label.Text);

            state.LoadingState = global::PCL.MyLoading.MyLoadingState.Error;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("加载失败", label.Text);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Scroll_viewer_and_scrollbar_construct() {
        var stack = new StackPanel();
        for (var i = 0; i < 40; i++) stack.Children.Add(new TextBlock { Text = $"行 {i}", Height = 20 });
        var viewer = new global::PCL.MyScrollViewer {
            Content = stack, Height = 100, Width = 200,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };
        var bar = new global::PCL.MyScrollBar {
            Height = 100, Width = 12, Orientation = Avalonia.Layout.Orientation.Vertical,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };
        var panel = new Grid();
        panel.Children.Add(viewer);
        panel.Children.Add(bar);
        var window = Show(panel);
        try {
            viewer.PerformVerticalOffsetDelta(50);
            global::PCL.ModAnimation.AniTimer(100);
            Assert.True(viewer.ScrollBarMaximum.Y > 0,
                $"Extent={viewer.Extent}, Viewport={viewer.Viewport}, Max={viewer.ScrollBarMaximum}");
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Page_left_show_and_hide_animation() {
        var page = new global::PCL.MyPageLeft();
        var window = Show(page);
        try {
            page.TriggerShowAnimation();
            global::PCL.ModAnimation.AniTimer(120);
            Assert.True(page.Opacity > 0);
            global::PCL.ModAnimation.AniTimer(500);
            global::PCL.ModAnimation.AniTimer(20);
            Assert.True(page.Opacity > 0.99);

            page.TriggerHideAnimation();
            global::PCL.ModAnimation.AniTimer(200);
            Assert.True(page.Opacity < 0.5, $"Opacity={page.Opacity}");
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Skin_hover_brightens_glow_without_shrinking() {
        var skin = new global::PCL.MySkin();
        var window = Show(skin);
        try {
            var effect = Assert.IsType<DropShadowEffect>(skin.Effect);
            Assert.InRange(effect.Opacity, 0.15, 0.25);

            window.MouseMove(new Point(250, 200));
            global::PCL.ModAnimation.AniTimer(400);
            Assert.True(effect.Opacity > 0.7, $"悬停后光晕透明度={effect.Opacity}");
            Assert.True(skin.RenderTransform is null or ScaleTransform { ScaleX: > 0.95, ScaleY: > 0.95 },
                "悬停不应缩小头像");

            window.MouseMove(new Point(5, 5));
            global::PCL.ModAnimation.AniTimer(400);
            Assert.InRange(effect.Opacity, 0.15, 0.25);
            var transform = skin.RenderTransform as ScaleTransform;
            if (transform is not null) {
                Assert.InRange(transform.ScaleX, 0.95, 1.05);
            }
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Page_right_enter_and_exit_transitions_states() {
        var content = new Border { Child = new TextBlock { Text = "页面内容" } };
        var page = new global::PCL.MyPageRight { Content = content };
        var window = Show(page);
        try {
            page.PageOnEnter();
            Assert.Equal(global::PCL.MyPageRight.PageStates.ContentEnter, page.PageState);
            Assert.True(content.IsVisible);
            global::PCL.ModAnimation.AniTimer(100);
            global::PCL.ModAnimation.AniTimer(600);
            global::PCL.ModAnimation.AniTimer(20);
            Assert.Equal(global::PCL.MyPageRight.PageStates.ContentStay, page.PageState);

            page.PageOnExit();
            Assert.Equal(global::PCL.MyPageRight.PageStates.PageExit, page.PageState);
            global::PCL.ModAnimation.AniTimer(100);
            global::PCL.ModAnimation.AniTimer(600);
            global::PCL.ModAnimation.AniTimer(20);
            Assert.Equal(global::PCL.MyPageRight.PageStates.Empty, page.PageState);
            Assert.False(content.IsVisible);

            page.PageOnEnter();
            global::PCL.ModAnimation.AniTimer(600);
            page.PageOnForceExit();
            Assert.Equal(global::PCL.MyPageRight.PageStates.Empty, page.PageState);
            Assert.False(content.IsVisible);
        } finally {
            window.Close();
        }
    }
    private static (int Row, int Left, int Right) FindGroundLine(WriteableBitmap bmp) {
        using var fb = bmp.Lock();
        var bytes = new byte[fb.RowBytes * fb.Size.Height];
        Marshal.Copy(fb.Address, bytes, 0, bytes.Length);
        var rows = new List<(int Row, int Left, int Right)>();
        for (var y = 55; y < 80; y++) {
            var runStart = -1;
            var best = (Left: 0, Right: 0);
            for (var x = 50; x < 160; x++) {
                var i = y * fb.RowBytes + x * 4;
                var r = bytes[i]; var g = bytes[i + 1]; var b = bytes[i + 2];
                var blue = b > 100 && b > r + 30 && b > g + 10;
                if (blue) {
                    if (runStart < 0) runStart = x;
                    if (x - runStart > best.Right - best.Left) best = (runStart, x);
                } else runStart = -1;
            }
            if (best.Right - best.Left >= 20) rows.Add((y, best.Left, best.Right));
        }
        Assert.NotEmpty(rows);
        return rows.OrderBy(r => r.Row).First();
    }

    private static int DeepestPickaxePixel(WriteableBitmap bmp, int lineRow) {
        using var fb = bmp.Lock();
        var bytes = new byte[fb.RowBytes * fb.Size.Height];
        Marshal.Copy(fb.Address, bytes, 0, bytes.Length);
        var deepest = -1;
        for (var y = 5; y < lineRow + 6; y++) {
            for (var x = 50; x < 160; x++) {
                var i = y * fb.RowBytes + x * 4;
                var r = bytes[i]; var g = bytes[i + 1]; var b = bytes[i + 2];
                if (b > 100 && b > r + 30 && b > g + 10) deepest = Math.Max(deepest, y);
            }
        }
        return deepest;
    }

    [AvaloniaFact]
    public void Loading_pickaxe_stops_on_ground_and_emits_splash_particles() {
        var loading = new global::PCL.MyLoading { Width = 135, Height = 65 };
        var window = new Window { Width = 200, Height = 120, Content = loading };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            var left = loading.FindControl<Avalonia.Controls.Shapes.Path>("PathLeft")!;
            var right = loading.FindControl<Avalonia.Controls.Shapes.Path>("PathRight")!;
            var sawParticles = false;
            var deepest = 0;
            var lineRow = 0;
            // 手动推进动画引擎，避免依赖平台计时器
            for (var tick = 0; tick < 70; tick++) {
                global::PCL.ModAnimation.AniTimer(16);
                if (tick is >= 36 and <= 47) {
                    using var bmp = window.CaptureRenderedFrame();
                    Assert.NotNull(bmp);
                    var line = FindGroundLine(bmp);
                    lineRow = line.Row;
                    deepest = Math.Max(deepest, DeepestPickaxePixel(bmp, line.Row));
                }
                if (left.Opacity > 0.5 && right.Opacity > 0.5) sawParticles = true;
            }
            Assert.True(lineRow > 0, "未找到地面横线");
            // 撞击瞬间镐刃会压在地面横线上，允许至多几像素的笔画重叠，但不能整个穿过
            Assert.True(deepest <= lineRow + 6, $"镐子穿过了地面：镐尖 {deepest}，地面 {lineRow}");
            Assert.True(sawParticles, "撞击时没有出现左右溅开的粒子");
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ComboBox_dropdown_light_dismisses_and_animates_arrow() {
        var combo = new global::PCL.MyComboBox { Width = 220, Height = 28 };
        combo.Items.Add(new global::PCL.MyComboBoxItem { Content = "选项一" });
        combo.Items.Add(new global::PCL.MyComboBoxItem { Content = "选项二" });
        var window = new Window { Width = 400, Height = 300, Content = combo };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            combo.IsDropDownOpen = true;
            Dispatcher.UIThread.RunJobs();
            global::PCL.ModAnimation.AniTimer(400);
            Dispatcher.UIThread.RunJobs();
            var popup = combo.GetVisualDescendants().OfType<Avalonia.Controls.Primitives.Popup>().FirstOrDefault();
            Assert.NotNull(popup);
            Assert.True(popup!.IsLightDismissEnabled, "下拉框弹出层未启用点击外部关闭");
            var arrow = combo.GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Path>().FirstOrDefault(p => p.Name == "PART_Arrow");
            Assert.NotNull(arrow);
            Assert.InRange(global::PCL.ModAnimation.AniAngle(arrow!), 179, 181);

            combo.IsDropDownOpen = false;
            Dispatcher.UIThread.RunJobs();
            global::PCL.ModAnimation.AniTimer(400);
            Dispatcher.UIThread.RunJobs();
            Assert.InRange(global::PCL.ModAnimation.AniAngle(arrow!), -1, 1);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Loading_error_stops_pickaxe_loop_and_returns_to_rest() {
        var loading = new global::PCL.MyLoading { AutoRun = false, Width = 135, Height = 65 };
        var state = new global::PCL.MyLoadingStateSimulator();
        loading.State = state;
        var window = new Window { Width = 200, Height = 120, Content = loading };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            var pickaxe = loading.FindControl<Avalonia.Controls.Shapes.Path>("PathPickaxe")!;
            var left = loading.FindControl<Avalonia.Controls.Shapes.Path>("PathLeft")!;
            var right = loading.FindControl<Avalonia.Controls.Shapes.Path>("PathRight")!;
            var group = "MyLoader Loop " + loading.Uuid;

            state.LoadingState = global::PCL.MyLoading.MyLoadingState.Run;
            Dispatcher.UIThread.RunJobs();
            Assert.True(global::PCL.ModAnimation.AniIsRun(group), "加载时镐子循环动画未启动");
            RunTicks(30);
            Assert.True(global::PCL.ModAnimation.AniIsRun(group));

            state.LoadingState = global::PCL.MyLoading.MyLoadingState.Error;
            Dispatcher.UIThread.RunJobs();
            RunTicks(40);
            Assert.False(global::PCL.ModAnimation.AniIsRun(group));
            Assert.InRange(global::PCL.ModAnimation.AniAngle(pickaxe), 54.5, 55.5);
            Assert.True(left.Opacity < 0.01 && right.Opacity < 0.01, "报错后地面粒子仍在显示");
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Radio_box_dot_stays_centered_after_uncheck_and_recheck() {
        var radio = new global::PCL.MyRadioBox { Text = "选项" };
        var window = Show(radio);
        try {
            var dot = radio.FindControl<Avalonia.Controls.Shapes.Ellipse>("ShapeDot")!;
            var border = radio.FindControl<Avalonia.Controls.Shapes.Ellipse>("ShapeBorder")!;

            void AssertCentered(string step) {
                var dotCenter = dot.TranslatePoint(new Point(dot.Bounds.Width / 2, dot.Bounds.Height / 2), radio);
                var borderCenter = border.TranslatePoint(new Point(border.Bounds.Width / 2, border.Bounds.Height / 2), radio);
                Assert.NotNull(dotCenter);
                Assert.NotNull(borderCenter);
                Assert.InRange(Math.Abs(dotCenter!.Value.X - borderCenter!.Value.X), 0, 1);
                Assert.InRange(Math.Abs(dotCenter.Value.Y - borderCenter.Value.Y), 0, 1);
                // 弹性缓动可能停在略有过冲的位置，只要保持在圆的直径范围内即可
                Assert.InRange(dot.Bounds.Width, 8, 11);
                Assert.InRange(border.Bounds.Width, 17, 19);
            }

            radio.Checked = true;
            RunTicks(40);
            AssertCentered("首次选中");

            radio.Checked = false;
            RunTicks(40);
            radio.Checked = true;
            RunTicks(40);
            AssertCentered("取消后重新选中");
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ComboBox_item_hover_has_no_gray_fill_in_dark_theme() {
        var originalMode = global::PCL.Settings.Get<int>("UiLauncherThemeMode");
        var item = new global::PCL.MyComboBoxItem {
            Content = "选项", Width = 200, Height = 30,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };
        var window = Show(item);
        try {
            global::PCL.Settings.Set("UiLauncherThemeMode", 2);
            Dispatcher.UIThread.RunJobs();
            RunTicks(20);

            window.MouseMove(new Point(100, 15));
            Dispatcher.UIThread.RunJobs();
            RunTicks(20);
            Assert.True(item.IsPointerOver, "未能模拟下拉项悬停");
            var brush = Assert.IsType<SolidColorBrush>(item.Background);
            Assert.Equal(0, brush.Color.A);
        } finally {
            global::PCL.Settings.Set("UiLauncherThemeMode", originalMode);
            Dispatcher.UIThread.RunJobs();
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Editable_combo_box_text_has_left_padding() {
        var combo = new global::PCL.MyComboBox {
            Width = 220, Height = 28, IsEditable = true
        };
        combo.Items.Add(new global::PCL.MyComboBoxItem { Content = "全部 (也可自行输入)" });
        combo.Text = "全部 (也可自行输入)";
        var window = new Window { Width = 400, Height = 300, Content = combo };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.CaptureRenderedFrame();
        Dispatcher.UIThread.RunJobs();
        try {
            var box = combo.GetVisualDescendants().OfType<global::PCL.MyTextBox>()
                .First(t => t.Name == "PART_EditableTextBox");
            Assert.Equal(new Thickness(7, 0, 0, 0), box.Padding);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Button_has_no_focus_adorner() {
        var button = new global::PCL.MyButton { Text = "版本设置" };
        var window = Show(button);
        try {
            Assert.Null(button.FocusAdorner);
            button.Focus();
            Dispatcher.UIThread.RunJobs();
            Assert.True(button.IsFocused);
            Assert.Null(button.FocusAdorner);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Icon_text_button_restores_semi_transparent_background_after_hover() {
        // 回归：原先使用不存在的资源名 ColorSemiTransparent，悬停离开时颜色动画会抛异常且背景无法还原
        var button = new global::PCL.MyIconTextButton {
            Text = "帮助",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };
        var window = Show(button);
        try {
            var expected = Assert.IsType<SolidColorBrush>(
                Application.Current!.FindResource("ColorBrushSemiTransparent"));

            window.MouseMove(new Point(30, 15));
            Dispatcher.UIThread.RunJobs();
            RunTicks(20);
            window.MouseMove(new Point(450, 350));
            Dispatcher.UIThread.RunJobs();
            RunTicks(20);

            var brush = Assert.IsType<SolidColorBrush>(button.Background);
            Assert.Equal(expected.Color, brush.Color);
        } finally {
            window.Close();
        }
    }

    private static void RunTicks(int count) {
        for (var i = 0; i < count; i++) {
            global::PCL.ModAnimation.AniTimer(20);
            Dispatcher.UIThread.RunJobs();
        }
    }

    [AvaloniaFact]
    public void MySkin_scales_around_center() {
        var skin = new global::PCL.MySkin {
            Width = 64,
            Height = 64,
            Background = Brushes.Red,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        var window = new Window { Width = 200, Height = 200, Content = skin, Background = Brushes.White };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            Assert.Equal(RelativeUnit.Relative, skin.RenderTransformOrigin.Unit);
            Assert.Equal(new Point(0.5, 0.5), skin.RenderTransformOrigin.Point);

            var before = skin.TranslatePoint(new Point(skin.Bounds.Width / 2, skin.Bounds.Height / 2), window);
            global::PCL.ModAnimation.AniStart(global::PCL.ModAnimation.AaScaleTransform(skin, -0.4, 10), "skin origin test");
            for (var i = 0; i < 4; i++) {
                global::PCL.ModAnimation.AniTimer(16);
                Dispatcher.UIThread.RunJobs();
            }
            Assert.InRange(global::PCL.ModAnimation.AniScaleX(skin), 0.55, 0.65);
            var after = skin.TranslatePoint(new Point(skin.Bounds.Width / 2, skin.Bounds.Height / 2), window);
            Assert.InRange(after!.Value.X, before!.Value.X - 1, before.Value.X + 1);
            Assert.InRange(after.Value.Y, before.Value.Y - 1, before.Value.Y + 1);
        } finally {
            window.Close();
        }
    }
}
