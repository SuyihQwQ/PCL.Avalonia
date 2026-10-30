using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
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
        content.Children.Add(new TextBlock { Text = "内容" });
        var card = new global::PCL.MyCard { Title = "标题", CanSwap = true };
        card.Children.Add(content);
        var window = Show(card);
        try {
            Assert.NotNull(card.MainTextBlock);
            Assert.NotNull(card.MainSwap);
            Assert.Same(content, card.SwapControl);

            card.IsSwapped = true;
            Assert.Equal(40d, card.Height);
            Assert.False(content.IsVisible);

            card.IsSwapped = false;
            Assert.True(double.IsNaN(card.Height));
            Assert.True(content.IsVisible);
            global::PCL.ModAnimation.AniTimer(400);
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
}
