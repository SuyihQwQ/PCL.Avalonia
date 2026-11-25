// SPDX-License-Identifier: Apache-2.0
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace PCL.Tests;

/// <summary>
/// 悬停高亮/提示的回归测试：下拉关闭与输入框点击不应产生闪烁，IReallyNeedOffline 从 Setup.ini 读取。
/// </summary>
public class HoverAndOfflineTests {

    private static void Pump(int times = 12, int tickMs = 16) {
        for (var i = 0; i < times; i++) {
            global::PCL.ModAnimation.AniTimer(tickMs);
            Dispatcher.UIThread.RunJobs();
        }
    }

    private static Color ResourceColor(string key) =>
        ((ISolidColorBrush)Application.Current!.FindResource(key)!).Color;

    [AvaloniaFact]
    public void Textbox_keeps_hover_tip_while_pointer_is_captured() {
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
            window.MouseMove(inside);
            Dispatcher.UIThread.RunJobs();
            typeof(global::PCL.FormMain).GetMethod("ShowHoverTip", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(window, null);
            Assert.True(window.BorderToolTip.IsVisible);

            // 点击输入框后按住并移动：TextBox 会捕获指针（用于选择文本），但这不是拖动，提示必须保持
            window.MouseDown(inside, MouseButton.Left, RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();
            Assert.True(box.IsFocused);
            window.MouseMove(inside + new Vector(1, 0));
            Dispatcher.UIThread.RunJobs();
            Pump(6);
            Assert.True(window.BorderToolTip.IsVisible, "输入框捕获指针时悬停提示不应消失");

            // 松开后继续在输入框内移动，提示仍在
            window.MouseUp(inside + new Vector(1, 0), MouseButton.Left, RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();
            window.MouseMove(inside + new Vector(2, 0));
            Dispatcher.UIThread.RunJobs();
            Pump(6);
            Assert.True(window.BorderToolTip.IsVisible, "松开后在输入框内移动，悬停提示应保持显示");
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Combobox_keeps_hover_color_when_closed_by_self_click() {
        var window = new global::PCL.FormMain { Width = 900, Height = 600 };
        var combo = new global::PCL.MyComboBox {
            Width = 160, Height = 28, Margin = new Thickness(50, 100, 0, 0),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };
        combo.Items.Add(new global::PCL.MyComboBoxItem { Content = "选项" });
        ((Panel)window.Content!).Children.Add(combo);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            var hover = ResourceColor("ColorBrush4");
            var point = combo.TranslatePoint(new Point(80, 14), window)!.Value;
            window.MouseMove(point);
            Dispatcher.UIThread.RunJobs();
            Pump(10);
            Assert.Equal(hover, ((ISolidColorBrush)combo.Foreground!).Color);

            // 点击展开
            window.MouseDown(point, MouseButton.Left, RawInputModifiers.None);
            window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();
            Pump(10);
            Assert.True(combo.IsDropDownOpen, "第一次点击应展开下拉菜单");

            // 再次点击自身收起：悬停高亮应直接恢复，而不是先变灰再变蓝
            window.MouseDown(point, MouseButton.Left, RawInputModifiers.None);
            window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();
            Pump(10);
            Assert.False(combo.IsDropDownOpen, "第二次点击应收起下拉菜单");
            Assert.Equal(hover, ((ISolidColorBrush)combo.Foreground!).Color);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Combobox_popup_fades_in_when_opened() {
        var window = new global::PCL.FormMain { Width = 900, Height = 600, Opacity = 1 };
        var combo = new global::PCL.MyComboBox {
            Width = 160, Height = 28, Margin = new Thickness(50, 100, 0, 0),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };
        combo.Items.Add(new global::PCL.MyComboBoxItem { Content = "选项" });
        ((Panel)window.Content!).Children.Add(combo);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            combo.IsDropDownOpen = true;
            Dispatcher.UIThread.RunJobs();
            Pump(12);
            var popup = (Popup?)typeof(global::PCL.MyComboBox)
                .GetField("Popup", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .GetValue(combo);
            Assert.NotNull(popup);
            Assert.True(popup!.IsOpen);
            var child = (Control)popup.Child!;
            // 复位后直接调用展开处理，验证它是从 0 开始淡入而不是直接显示
            child.Opacity = 1;
            typeof(global::PCL.MyComboBox)
                .GetMethod("DropDownOpenedHandler", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(combo, null);
            Assert.True(child.Opacity < 0.05, $"展开时应从透明开始淡入，实际 Opacity={child.Opacity}");
            Pump(12);
            Assert.True(child.Opacity > 0.9, $"淡入结束后应接近不透明，实际 Opacity={child.Opacity}");
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Combobox_popup_fades_out_before_closing() {
        var window = new global::PCL.FormMain { Width = 900, Height = 600, Opacity = 1 };
        var combo = new global::PCL.MyComboBox {
            Width = 160, Height = 28, Margin = new Thickness(50, 100, 0, 0),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };
        combo.Items.Add(new global::PCL.MyComboBoxItem { Content = "选项" });
        ((Panel)window.Content!).Children.Add(combo);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            combo.IsDropDownOpen = true;
            Dispatcher.UIThread.RunJobs();
            Pump(12);
            var popup = (Popup?)typeof(global::PCL.MyComboBox)
                .GetField("Popup", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .GetValue(combo);
            Assert.NotNull(popup);
            Assert.True(popup!.IsOpen);

            // 收起：先保留弹层并播放淡出，动画结束后才真正关闭
            combo.IsDropDownOpen = false;
            Assert.True(combo.PopupOpen, "收起时应先保留弹层播放淡出");
            Assert.False(((Control)popup.Child!).IsHitTestVisible, "淡出期间弹层不应再响应点击");
            Pump(20);
            Assert.False(combo.PopupOpen, "淡出结束后应关闭弹层");
            Assert.False(popup.IsOpen);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Card_keeps_hover_title_when_dropdown_closes() {
        var window = new global::PCL.FormMain { Width = 900, Height = 600 };
        var card = new global::PCL.MyCard {
            Title = "卡片", Width = 400, Height = 220, Margin = new Thickness(40, 40, 0, 0),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };
        var combo = new global::PCL.MyComboBox {
            Width = 160, Height = 28, Margin = new Thickness(30, 120, 0, 0),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };
        combo.Items.Add(new global::PCL.MyComboBoxItem { Content = "选项" });
        card.Children.Add(combo);
        ((Panel)window.Content!).Children.Add(card);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            var blue = ResourceColor("ColorBrush2");
            var point = combo.TranslatePoint(new Point(80, 14), window)!.Value;
            window.MouseMove(point);
            Dispatcher.UIThread.RunJobs();
            Pump(10);
            Assert.Equal(blue, ((ISolidColorBrush)card.MainTextBlock.Foreground!).Color);

            window.MouseDown(point, MouseButton.Left, RawInputModifiers.None);
            window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();
            Pump(10);
            Assert.True(combo.IsDropDownOpen);

            window.MouseDown(point, MouseButton.Left, RawInputModifiers.None);
            window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();
            Pump(12);
            Assert.False(combo.IsDropDownOpen);
            Assert.Equal(blue, ((ISolidColorBrush)card.MainTextBlock.Foreground!).Color);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Text_button_hover_color_reaches_rendered_text() {
        var button = new global::PCL.MyTextButton { Text = "»  购买正版" };
        var window = new Window { Width = 300, Height = 100, Content = button };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            var hover = ResourceColor("ColorBrush3");
            var point = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window)!.Value;
            window.MouseMove(point);
            Dispatcher.UIThread.RunJobs();
            Pump(12);
            var text = button.GetVisualDescendants().OfType<TextBlock>().First();
            Assert.Equal("»  购买正版", text.Text);
            Assert.Equal(hover, ((ISolidColorBrush)text.Foreground!).Color);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Homepage_legacy_xaml_is_converted_and_loads() {
        const string input = "<StackPanel Margin=\"0\" Padding=\"0\">" +
            "<Rectangle Width=\"10\" Height=\"10\" Panel.ZIndex=\"2\" Fill=\"Red\" />" +
            "<Border Width=\"20\" Height=\"20\"><Border.Background>" +
            "<ImageBrush ImageSource=\"https://example.com/a.png\" Stretch=\"UniformToFill\" />" +
            "</Border.Background></Border>" +
            "<StackPanel HorizontalAlignment=\"right\"><local:MyCard IsSwaped=\"false\" Title=\"卡片\" /></StackPanel>" +
            "</StackPanel>";
        var converted = global::PCL.ModHelp.ConvertLegacyXaml(input);
        Assert.Contains("ZIndex=\"2\"", converted);
        Assert.DoesNotContain("Panel.ZIndex", converted);
        Assert.Contains("Source=\"{local:RemoteImage Url='https://example.com/a.png'}\"", converted);
        Assert.Contains("IsSwapped=\"false\"", converted);
        Assert.Contains("HorizontalAlignment=\"Right\"", converted);
        Assert.DoesNotContain("Padding=", converted);

        // 实际加载一份不含网络图片的片段，验证转换后的 XAML 能被 Avalonia 解析
        const string loadable = "<StackPanel Padding=\"0\">" +
            "<Rectangle Width=\"10\" Height=\"10\" Panel.ZIndex=\"2\" Fill=\"Red\" />" +
            "<StackPanel HorizontalAlignment=\"right\"><local:MyCard IsSwaped=\"false\" Title=\"卡片\" /></StackPanel>" +
            "</StackPanel>";
        var wrapped = "<StackPanel xmlns=\"https://github.com/avaloniaui\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" " +
            "xmlns:local=\"using:PCL\" xmlns:sys=\"using:System\">" +
            global::PCL.ModHelp.ConvertLegacyXaml(loadable) + "</StackPanel>";
        var loaded = global::PCL.ModHelp.GetObjectFromXML(wrapped);
        Assert.NotNull(loaded);
        Assert.IsType<StackPanel>(loaded);
    }

    [AvaloniaFact]
    public void Homepage_implicit_style_padding_is_not_applied_to_stackpanel() {
        // 真实主页里存在 <Style TargetType="StackPanel"><Setter Property="Padding" Value="0"/></Style>
        const string input = "<StackPanel Margin=\"20,6,0,6\" Padding=\"0\">" +
            "<StackPanel.Resources><Style TargetType=\"StackPanel\"><Setter Property=\"Padding\" Value=\"0\"/></Style></StackPanel.Resources>" +
            "<TextBlock Text=\"x\" /></StackPanel>";
        var converted = global::PCL.ModHelp.ConvertLegacyXaml(input);
        Assert.DoesNotMatch("<StackPanel[^>]*\\sPadding=", converted);
        var wrapped = "<StackPanel xmlns=\"https://github.com/avaloniaui\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" " +
            "xmlns:local=\"using:PCL\" xmlns:sys=\"using:System\">" + converted + "</StackPanel>";
        Assert.IsType<StackPanel>(global::PCL.ModHelp.GetObjectFromXML(wrapped));
    }

    [AvaloniaFact]
    public void Help_xaml_text_starting_with_brace_is_escaped() {
        // 帮助内容里的内联代码形如 {type:"minecraft:slot_range",slots:"..."}，直接放进 Text 属性会被当成标记扩展
        const string input = "<TextBlock>示例：<TextBox Style=\"{StaticResource InlineCode}\">{type:&quot;minecraft:slot_range&quot;}</TextBox></TextBlock>";
        var converted = global::PCL.ModHelp.ConvertLegacyXaml(input);
        Assert.Contains("Text=\"{}{type:&quot;", converted);
        var wrapped = "<StackPanel xmlns=\"https://github.com/avaloniaui\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" " +
            "xmlns:local=\"using:PCL\" xmlns:sys=\"using:System\">" + converted + "</StackPanel>";
        Assert.NotNull(global::PCL.ModHelp.GetObjectFromXML(wrapped));
    }

    [AvaloniaFact]
    public void Homepage_remote_image_brush_loads_valid_source() {
        // 本地图片走 RemoteImage 的异步加载路径；这里回归此前“MyBitmap 不是 IImageBrushSource”导致的崩溃
        var icon = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "../../../../PCL/Assets/Images/icon.png"));
        Assert.True(System.IO.File.Exists(icon), icon);
        var xaml = "<StackPanel xmlns=\"https://github.com/avaloniaui\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" " +
            "xmlns:local=\"using:PCL\" xmlns:sys=\"using:System\"><Border Width=\"20\" Height=\"20\">" +
            "<Border.Background><ImageBrush Source=\"{local:RemoteImage Url='" + icon + "'}\" /></Border.Background></Border></StackPanel>";
        var root = (StackPanel)global::PCL.ModHelp.GetObjectFromXML(xaml)!;
        var border = (Border)root.Children[0];
        var brush = (ImageBrush)border.Background!;
        for (var i = 0; i < 40 && brush.Source is null; i++) {
            System.Threading.Thread.Sleep(50);
            Dispatcher.UIThread.RunJobs();
        }
        Assert.NotNull(brush.Source);
        Assert.IsAssignableFrom<IImageBrushSource>(brush.Source);
    }

    [AvaloniaFact]
    public void Offline_flag_is_read_from_setup_ini() {
        var oldPathData = global::PCL.ModBase.PathData;
        var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "PCL-flag-" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(dir);
        try {
            global::PCL.ModBase.PathData = dir + System.IO.Path.DirectorySeparatorChar;
            Assert.False(global::PCL.ModBase.IsOfflineModeAllowed(), "没有 Setup.ini 时不应开启");

            System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "Setup.ini"), "IReallyNeedOffline:True" + System.Environment.NewLine);
            Assert.True(global::PCL.ModBase.IsOfflineModeAllowed(), "Setup.ini 中写 IReallyNeedOffline:True 应开启");

            var ini = System.IO.Path.Combine(dir, "Setup.ini");
            System.IO.File.WriteAllText(ini, "IReallyNeedOffline:0" + System.Environment.NewLine);
            global::PCL.ModBase.IniClearCache(ini);
            Assert.False(global::PCL.ModBase.IsOfflineModeAllowed(), "0 应视为关闭");
        } finally {
            global::PCL.ModBase.PathData = oldPathData;
            try { System.IO.Directory.Delete(dir, true); } catch { }
        }
    }
}
