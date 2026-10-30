using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Xunit;

namespace PCL.Tests;

public class WindowShellTests {

    private static global::PCL.FormMain CreateShownWindow() {
        var window = new global::PCL.FormMain();
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    [AvaloniaFact]
    public void Shell_contains_all_named_parts() {
        var window = CreateShownWindow();
        try {
            Assert.NotNull(window.FindControl<Grid>("PanBack"));
            Assert.NotNull(window.FindControl<Border>("BorderForm"));
            Assert.NotNull(window.FindControl<Grid>("PanForm"));
            Assert.NotNull(window.FindControl<Grid>("PanTitle"));
            Assert.NotNull(window.FindControl<Grid>("PanMain"));
            Assert.NotNull(window.FindControl<Border>("PanMainLeft"));
            Assert.NotNull(window.FindControl<Border>("PanMainRight"));
            Assert.NotNull(window.FindControl<TextBlock>("LabTitleLogo"));
            Assert.NotNull(window.FindControl<Button>("BtnTitleClose"));
            Assert.NotNull(window.FindControl<Button>("BtnTitleMin"));
            for (var i = 0; i < 5; i++) {
                Assert.NotNull(window.FindControl<Button>($"BtnTitleSelect{i}"));
            }
            foreach (var name in new[] { "ResizerT", "ResizerB", "ResizerL", "ResizerR", "ResizerLT", "ResizerRT", "ResizerLB", "ResizerRB" }) {
                Assert.NotNull(window.FindControl<Avalonia.Controls.Shapes.Rectangle>(name));
            }
            Assert.IsType<TransformGroup>(window.FindControl<Grid>("PanBack")!.RenderTransform);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Show_animation_clears_transform() {
        var window = CreateShownWindow();
        try {
            var panBack = window.FindControl<Grid>("PanBack")!;
            global::PCL.ModAnimation.AniTimer(700);
            global::PCL.ModAnimation.AniTimer(10);
            global::PCL.ModAnimation.AniTimer(10);
            Assert.True(window.Opacity > 0.99, $"Opacity={window.Opacity}");
            Assert.Null(panBack.RenderTransform);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Title_bar_and_resizers_accept_pointer() {
        var window = CreateShownWindow();
        try {
            // 标题栏空白处：触发拖动，不应抛异常
            window.MouseDown(new Point(120, 40), MouseButton.Left);
            window.MouseUp(new Point(120, 40), MouseButton.Left);
            // 四边与四角：触发缩放，不应抛异常
            foreach (var point in new[] {
                         new Point(400, 3), new Point(400, 496),
                         new Point(3, 250), new Point(846, 250),
                         new Point(4, 4), new Point(845, 4),
                         new Point(4, 495), new Point(845, 495) }) {
                window.MouseDown(point, MouseButton.Left);
                window.MouseUp(point, MouseButton.Left);
            }
            Assert.Equal(WindowState.Normal, window.WindowState);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void EndProgram_runs_close_animation_and_closes() {
        var window = CreateShownWindow();
        global::PCL.ModAnimation.AniTimer(700);
        window.EndProgram(false);
        Assert.True(global::PCL.ModAnimation.AniIsRun("Form Close"));
        global::PCL.ModAnimation.AniTimer(300);
        global::PCL.ModAnimation.AniTimer(10);
        global::PCL.ModAnimation.AniTimer(60);
        Assert.False(window.IsVisible);
        global::PCL.ModBase.IsProgramEnding = false;
    }

    [AvaloniaFact]
    public void Minimize_button_minimizes_window() {
        var window = CreateShownWindow();
        try {
            window.FindControl<Button>("BtnTitleMin")!.RaiseEvent(
                new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Assert.Equal(WindowState.Minimized, window.WindowState);
        } finally {
            window.WindowState = WindowState.Normal;
            window.Close();
        }
    }
}
