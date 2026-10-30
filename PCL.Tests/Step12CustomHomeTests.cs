using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Platform;
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
            <StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:local="clr-namespace:PCL;assembly=Plain Craft Launcher 2" ToolTip="tip" SnapsToDevicePixels="True">
                <local:MyImage Source="pack://application:,,,/images/Blocks/Cobblestone.png" Visibility="Collapsed" />
                <local:MyButton Visibility="Visible" />
            </StackPanel>
            """;
        var converted = global::PCL.ModHelp.ConvertLegacyXaml(source);
        Assert.DoesNotContain("xmlns", converted);
        Assert.DoesNotContain("SnapsToDevicePixels", converted);
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
}
