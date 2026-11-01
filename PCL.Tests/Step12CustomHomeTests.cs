using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace PCL.Tests;

public class Step12CustomHomeTests {

    private static string ReadTutorialAsset() {
        using var stream = AssetLoader.Open(new Uri("avares://PCL/Assets/Custom.xaml.txt"));
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    [AvaloniaFact]
    public void Legacy_xaml_conversion_handles_wpf_syntax() {
        const string source = """
            <StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:local="clr-namespace:PCL;assembly=Plain Craft Launcher 2" xmlns:d="http://schemas.microsoft.com/expression/blend/2008" xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006" mc:Ignorable="d" d:DesignWidth="600" x:Class="PCL.CustomHome" ToolTip="tip" SnapsToDevicePixels="True" TextOptions.TextFormattingMode="Display">
                <local:MyImage Source="pack://application:,,,/images/Blocks/Cobblestone.png" Visibility="Collapsed" />
                <local:MyButton Visibility="Visible" />
                <Border Effect="{DynamicResource Shadow}">
                    <Border.Effect>
                        <DropShadowEffect BlurRadius="8" Opacity="0.4" />
                    </Border.Effect>
                </Border>
            </StackPanel>
            """;
        var converted = global::PCL.ModHelp.ConvertLegacyXaml(source);
        Assert.DoesNotContain("xmlns", converted);
        Assert.DoesNotContain("SnapsToDevicePixels", converted);
        Assert.DoesNotContain("x:Class", converted);
        Assert.DoesNotContain("DesignWidth", converted);
        Assert.DoesNotContain("Ignorable", converted);
        Assert.DoesNotContain("TextFormattingMode", converted);
        Assert.DoesNotContain("DropShadowEffect", converted);
        Assert.DoesNotContain("Effect", converted);
        Assert.Contains("ToolTip.Tip=", converted);
        Assert.Contains("avares://PCL/Assets/Images/Blocks/CobbleStone.png", converted);
        Assert.Contains("IsVisible=\"False\"", converted);
        Assert.Contains("IsVisible=\"True\"", converted);
    }

    [AvaloniaFact]
    public void Custom_homepage_tutorial_builds_control_tree() {
        var content = ReadTutorialAsset();
        Assert.Contains("MyCard", content);
        content = global::PCL.ModHelp.ArgumentReplace(content, MeloongCore.StringUtils.XmlEscape);
        content = global::PCL.ModHelp.ConvertLegacyXaml(content);
        content = "<StackPanel xmlns=\"https://github.com/avaloniaui\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" xmlns:local=\"using:PCL\">" + content + "</StackPanel>";
        var control = global::PCL.ModHelp.GetObjectFromXML(content) as Control;
        Assert.NotNull(control);
        var panel = Assert.IsType<StackPanel>(control);
        Assert.True(panel.Children.Count >= 5, $"教学卡片数量异常：{panel.Children.Count}");
        Assert.NotNull(panel.Children.OfType<global::PCL.MyCard>().FirstOrDefault());
    }

    [AvaloniaFact]
    public void Legacy_xaml_conversion_handles_wpf_styles_templates_and_flow_documents() {
        const string source = """
            <StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" xmlns:sys="clr-namespace:System;assembly=mscorlib">
                <StackPanel.Resources>
                    <sys:String x:Key="Tip">提示</sys:String>
                </StackPanel.Resources>
                <StackPanel.Styles>
                    <Style TargetType="TextBlock">
                        <Setter Property="Margin" Value="0,4" />
                        <Setter Property="TextOptions.TextFormattingMode" Value="Display" />
                    </Style>
                    <Style x:Key="Accent" TargetType="TextBlock">
                        <Setter Property="Foreground" Value="{DynamicResource ColorBrush3}" />
                    </Style>
                </StackPanel.Styles>
                <TextBlock Style="{StaticResource Accent}" Text="标题" />
                <FlowDocumentScrollViewer>
                    <FlowDocument>
                        <Paragraph>段落</Paragraph>
                    </FlowDocument>
                </FlowDocumentScrollViewer>
                <ContentControl Template="{StaticResource SomeTemplate}" Content="模板" />
                <Line X1="0" Y1="0" X2="10" Y2="10" Stroke="Gray" />
            </StackPanel>
            """;
        var converted = global::PCL.ModHelp.ConvertLegacyXaml(source);
        Assert.DoesNotContain("FlowDocument", converted);
        Assert.DoesNotContain("Paragraph", converted);
        Assert.DoesNotContain("TextFormattingMode", converted);
        Assert.DoesNotContain("StaticResource Accent", converted);
        Assert.Contains("Text=\"标题\"", converted);
        Assert.Contains("Foreground=\"{DynamicResource ColorBrush3}\"", converted);
        Assert.Contains("StartPoint=", converted);
        Assert.Contains("EndPoint=", converted);
        Assert.Contains("Selector=\"TextBlock\"", converted);

        var wrapped = "<StackPanel xmlns=\"https://github.com/avaloniaui\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" xmlns:local=\"using:PCL\" xmlns:sys=\"using:System\">" + converted + "</StackPanel>";
        var control = global::PCL.ModHelp.GetObjectFromXML(wrapped) as Control;
        Assert.NotNull(control);
        var panel = Assert.IsType<StackPanel>(control);
        var window = new Window { Width = 600, Height = 400, Content = panel };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            var texts = panel.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();
            Assert.Contains("标题", texts);
            Assert.Contains("段落", texts);
            Assert.Contains("模板", texts);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Homepage_preset_change_clears_saved_homepage_cache() {
        // 回归：切换主页预设后若不清缓存，启动页会继续展示旧主页或停留在加载状态
        var originalUrl = global::PCL.Settings.Get<string>("CacheSavedPageUrl");
        var originalVersion = global::PCL.Settings.Get<string>("CacheSavedPageVersion");
        var originalRight = global::PCL.ModMain.FrmLaunchRight;
        var originalMain = global::PCL.ModMain.FrmMain;
        var page = new global::PCL.PageLaunchRight();
        try {
            global::PCL.ModMain.FrmMain = null!;
            global::PCL.ModMain.FrmLaunchRight = page;
            global::PCL.Settings.Set("CacheSavedPageUrl", "https://example.com/custom.xaml");
            global::PCL.Settings.Set("CacheSavedPageVersion", "123456");

            global::PCL.PageLaunchRight.OnHomepagePresetChanged();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("", global::PCL.Settings.Get<string>("CacheSavedPageUrl"));
            Assert.Equal("", global::PCL.Settings.Get<string>("CacheSavedPageVersion"));
        } finally {
            global::PCL.Settings.Set("CacheSavedPageUrl", originalUrl);
            global::PCL.Settings.Set("CacheSavedPageVersion", originalVersion);
            global::PCL.ModMain.FrmLaunchRight = originalRight;
            global::PCL.ModMain.FrmMain = originalMain;
        }
    }
}
