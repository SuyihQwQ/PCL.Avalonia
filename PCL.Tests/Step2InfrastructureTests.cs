using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using SkiaSharp;
using Xunit;

namespace PCL.Tests;

public class ThemeTests {

    [AvaloniaFact]
    public void ThemeRefresh_replaces_resource_objects() {
        global::PCL.ModSecret.ThemeRefresh();
        var beforeBrush = Application.Current!.FindResource("ColorBrush1");
        var beforeColor = Application.Current!.FindResource("ColorObject1");
        Assert.IsType<SolidColorBrush>(beforeBrush);
        Assert.IsType<Color>(beforeColor);

        global::PCL.ModSecret.ThemeRefresh();

        var afterBrush = Application.Current!.FindResource("ColorBrush1");
        var afterColor = Application.Current!.FindResource("ColorObject1");
        Assert.NotSame(beforeBrush, afterBrush);
        Assert.Equal((Color)beforeColor!, (Color)afterColor!);
        Assert.Equal(((SolidColorBrush)beforeBrush!).Color, (Color)beforeColor!);
    }

    [AvaloniaFact]
    public void ThemeRefresh_updates_main_window_title_bar() {
        var window = new global::PCL.FormMain();
        window.Show();
        global::PCL.ModSecret.ThemeRefresh();
        Dispatcher.UIThread.RunJobs();
        Assert.IsType<LinearGradientBrush>(window.FindControl<Grid>("PanTitle")!.Background);
        window.Close();
    }
}

public class AnimationTests {

    [AvaloniaFact]
    public void Opacity_animation_advances_per_frame() {
        var border = new Border { Opacity = 0 };
        global::PCL.ModAnimation.AniStart(
            global::PCL.ModAnimation.AaOpacity(border, 1, 100), "test-opacity");

        global::PCL.ModAnimation.AniTimer(50);
        Assert.InRange(border.Opacity, 0.3, 0.7);

        global::PCL.ModAnimation.AniTimer(60);
        Assert.True(border.Opacity > 0.99, $"实际值：{border.Opacity}");
        Assert.False(global::PCL.ModAnimation.AniIsRun("test-opacity"));
    }

    [AvaloniaFact]
    public void Color_animation_restores_resource_binding() {
        var border = new Border { Background = new SolidColorBrush(Colors.Black) };
        global::PCL.ModAnimation.AniStart(
            global::PCL.ModAnimation.AaColor(border, Border.BackgroundProperty, "ColorBrush2", 100), "test-color");

        global::PCL.ModAnimation.AniTimer(50);
        var middle = Assert.IsType<SolidColorBrush>(border.Background);
        Assert.NotEqual(Colors.Black, middle.Color);

        global::PCL.ModAnimation.AniTimer(60);
        var expected = Assert.IsType<SolidColorBrush>(Application.Current!.FindResource("ColorBrush2"));
        var actual = Assert.IsType<SolidColorBrush>(border.Background);
        Assert.Equal(expected.Color, actual.Color);
        Assert.False(global::PCL.ModAnimation.AniIsRun("test-color"));
    }

    [AvaloniaFact]
    public void Delayed_animation_waits_for_delay() {
        var border = new Border { Opacity = 1 };
        global::PCL.ModAnimation.AniStart(
            global::PCL.ModAnimation.AaOpacity(border, -1, 100, 50), "test-delay");

        global::PCL.ModAnimation.AniTimer(20);
        Assert.Equal(1, border.Opacity);

        global::PCL.ModAnimation.AniTimer(200);
        Assert.True(border.Opacity < 0.01);
        Assert.False(global::PCL.ModAnimation.AniIsRun("test-delay"));
    }
}

public class ModalityTests {

    [AvaloniaFact]
    public void MyMsgBox_returns_clicked_button_index() {
        var pressed = false;
        Dispatcher.UIThread.Post(() => {
            var dialog = global::PCL.ModMain.ActiveMsgBoxWindow as global::PCL.MyMsgBoxDialog;
            Assert.NotNull(dialog);
            dialog!.Respond(2);
            pressed = true;
        });

        var result = global::PCL.ModMain.MyMsgBox("测试内容", "测试标题", "确定", "取消");

        Assert.True(pressed);
        Assert.Equal(2, result);
        Assert.Null(global::PCL.ModMain.ActiveMsgBoxWindow);
    }
}

public class BitmapTests {

    [AvaloniaFact]
    public void Clip_crops_and_converts() {
        using var sk = new SKBitmap(4, 4);
        using (var canvas = new SKCanvas(sk)) {
            canvas.Clear(SKColors.Red);
            canvas.DrawRect(new SKRect(2, 2, 4, 4), new SKPaint { Color = SKColors.Blue });
        }

        var my = new global::PCL.MyBitmap(sk);
        var clipped = my.Clip(2, 2, 2, 2);
        Assert.Equal(2, clipped.Pic.Width);
        Assert.Equal(SKColors.Blue, clipped.Pic.GetPixel(0, 0));

        var avaloniaBitmap = clipped.ToBitmap();
        Assert.NotNull(avaloniaBitmap);
        Assert.Equal(2, avaloniaBitmap!.PixelSize.Width);
        Assert.Equal(2, avaloniaBitmap.PixelSize.Height);
    }

    [AvaloniaFact]
    public void RotateFlip_swaps_dimensions() {
        using var sk = new SKBitmap(4, 2);
        using (var canvas = new SKCanvas(sk)) canvas.Clear(SKColors.Green);

        var rotated = new global::PCL.MyBitmap(sk).RotateFlip(global::PCL.RotateFlipType.Rotate90FlipNone);
        Assert.Equal(2, rotated.Pic.Width);
        Assert.Equal(4, rotated.Pic.Height);
    }

    [AvaloniaFact]
    public void Save_and_load_roundtrip() {
        var path = Path.Combine(Path.GetTempPath(), $"pcl-test-{Guid.NewGuid():N}.png");
        try {
            using var sk = new SKBitmap(3, 1);
            using (var canvas = new SKCanvas(sk)) canvas.Clear(SKColors.Orange);

            var my = new global::PCL.MyBitmap(sk);
            my.Save(path);

            var loaded = new global::PCL.MyBitmap(path);
            Assert.Equal(3, loaded.Pic.Width);
            Assert.Equal(1, loaded.Pic.Height);
            Assert.Equal(SKColors.Orange, loaded.Pic.GetPixel(0, 0));
        } finally {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [AvaloniaFact]
    public void MyImage_loads_local_file_when_attached() {
        var path = Path.Combine(Path.GetTempPath(), $"pcl-test-{Guid.NewGuid():N}.png");
        try {
            using (var sk = new SKBitmap(2, 2))
            using (var canvas = new SKCanvas(sk)) {
                canvas.Clear(SKColors.Purple);
                new global::PCL.MyBitmap(sk).Save(path);
            }

            var image = new global::PCL.MyImage { Source = path };
            var window = new Window { Width = 100, Height = 100, Content = image };
            window.Show();
            Dispatcher.UIThread.RunJobs();

            Assert.NotNull(image.Source);
            Assert.Equal(path, image.Source);
            Assert.NotNull(((Image)image).Source);
            window.Close();
        } finally {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
