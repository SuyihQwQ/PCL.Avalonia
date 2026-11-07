using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace PCL.Tests;

public class Step12CustomHomeTests {

    private static string ReadTutorialAsset() {
        using var stream = AssetLoader.Open(new Uri("avares://PCL.Avalonia/Assets/Custom.xaml.txt"));
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
        Assert.Contains("avares://PCL.Avalonia/Assets/Images/Blocks/CobbleStone.png", converted);
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
    public void Homepage_builder_output_loads_with_document_markup() {
        var imagePath = Path.Combine(Path.GetTempPath(), "pcl-builder-" + Guid.NewGuid().ToString("N") + ".png");
        File.WriteAllBytes(imagePath, Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII="));
        try {
            var source = """
                <StackPanel Margin="0,-10,0,0" xmlns:sys="clr-namespace:System;assembly=mscorlib" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" xmlns:local="clr-namespace:PCL;assembly=Plain Craft Launcher 2">
                <StackPanel.Triggers></StackPanel.Triggers>
                <StackPanel.Resources>
                <Style TargetType="local:MyCard" x:Key="Card"><Setter Property="Margin" Value="0,5" /></Style>
                <Style TargetType="Image" x:Key="InnerImage"><Setter Property="MaxHeight" Value="500" /><Setter Property="HorizontalAlignment" Value="Center" /></Style>
                <Style TargetType="TextBlock"><Setter Property="TextWrapping" Value="Wrap" /><Setter Property="FontSize" Value="14" /><Setter Property="Foreground" Value="{DynamicResource ColorBrush1}" /></Style>
                <Style TargetType="Paragraph"><Setter Property="LineHeight" Value="12" /><Setter Property="TextIndent" Value="0" /><Setter Property="Margin" Value="0,8" /></Style>
                <Style TargetType="Paragraph" x:Key="H1"><Setter Property="FontSize" Value="24" /><Setter Property="FontWeight" Value="Bold" /></Style>
                <Style TargetType="Paragraph" x:Key="Lp"><Setter Property="Margin" Value="0,8" /></Style>
                <Style TargetType="Section" x:Key="Quote"><Setter Property="BorderThickness" Value="5,0,0,0" /><Setter Property="BorderBrush" Value="{DynamicResource ColorBrush4}" /><Setter Property="Padding" Value="10,0" /><Setter Property="Margin" Value="0,12" /></Style>
                <Style TargetType="Section" x:Key="typedQuote" BasedOn="{StaticResource Quote}"><Setter Property="Padding" Value="10,2" /></Style>
                <Style TargetType="Section" x:Key="Hr"><Setter Property="Margin" Value="0,4,0,0" /><Setter Property="BorderThickness" Value="0,1.5,0,0" /><Setter Property="BorderBrush" Value="{StaticResource ColorBrush4}" /></Style>
                <Style TargetType="Border" x:Key="BlockCodeBorder"><Setter Property="Background" Value="{DynamicResource ColorBrush4}" /><Setter Property="CornerRadius" Value="5" /><Setter Property="Margin" Value="2" /></Style>
                <Style TargetType="TextBox" x:Key="InlineCode"><Setter Property="FontSize" Value="14" /><Setter Property="Foreground" Value="{DynamicResource ColorBrush1}" /><Setter Property="Margin" Value="2,0,2,-4" /></Style>
                <Style TargetType="TextBox" x:Key="BlockCode"><Setter Property="Foreground" Value="White" /><Setter Property="FontSize" Value="14" /><Setter Property="IsReadOnly" Value="True" /></Style>
                </StackPanel.Resources>
                <local:MyCard Title="Builder" CanSwap="True" IsSwaped="False" Style="{StaticResource Card}">
                <FlowDocumentScrollViewer><FlowDocument>
                <Paragraph Style="{StaticResource H1}">Heading</Paragraph>
                <Paragraph>Text <Bold>bold</Bold> and <Underline><local:MyTextButton EventType="打开网页" EventData="https://example.com">link</local:MyTextButton></Underline> and <TextBox Style="{StaticResource InlineCode}">code</TextBox></Paragraph>
                <List MarkerStyle="Decimal"><ListItem><Paragraph Style="{StaticResource Lp}">one</Paragraph></ListItem><ListItem>two</ListItem></List>
                <Section Style="{StaticResource typedQuote}"><Paragraph FontWeight="Bold">Quote<Paragraph.Foreground><Binding Path="BorderBrush" RelativeSource="{RelativeSource Mode=FindAncestor, AncestorType=Section}" /></Paragraph.Foreground></Paragraph><Paragraph>quote body</Paragraph></Section>
                <Section Style="{StaticResource Hr}" />
                <BlockUIContainer><local:MyHint Margin="0,4,0,8" IsWarn="False" Text="hint" /></BlockUIContainer>
                <BlockUIContainer><Border Style="{StaticResource BlockCodeBorder}"><StackPanel><TextBlock Foreground="White" FontSize="14" Text="txt" Margin="10,4" /><TextBox Style="{StaticResource BlockCode}" Text="echo hi" /></StackPanel></Border></BlockUIContainer>
                <Paragraph><Image Style="{StaticResource InnerImage}" Source="__IMAGE__" /></Paragraph>
                </FlowDocument></FlowDocumentScrollViewer>
                </local:MyCard>
                </StackPanel>
                """.Replace("__IMAGE__", imagePath);
            var converted = global::PCL.ModHelp.ConvertLegacyXaml(source);
            Assert.DoesNotContain("<Section", converted);
            Assert.DoesNotContain("BlockUIContainer", converted);
            Assert.Contains("local:MyImage", converted);
            Assert.Contains("AncestorType=Border", converted);
            var wrapped = "<StackPanel xmlns=\"https://github.com/avaloniaui\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" xmlns:local=\"using:PCL\" xmlns:sys=\"using:System\">" + converted + "</StackPanel>";
            var control = global::PCL.ModHelp.GetObjectFromXML(wrapped) as Control;
            Assert.NotNull(control);
            var panel = Assert.IsType<StackPanel>(control);
            var window = new Window { Width = 800, Height = 600, Content = panel };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            try {
                Assert.Contains(panel.GetVisualDescendants().OfType<global::PCL.MyCard>(), c => c.Title == "Builder");
                var texts = panel.GetVisualDescendants().OfType<TextBlock>().ToList();
                var heading = Assert.Single(texts, t => t.Text == "Heading");
                Assert.Equal(24, heading.FontSize);
                Assert.Contains(texts, t => t.Text == "1.");
                Assert.Contains(texts, t => t.Text == "2.");
                Assert.Contains(texts, t => t.Text == "code");
                Assert.Contains(texts, t => t.Text == "link");
                Assert.Single(panel.GetVisualDescendants().OfType<SelectableTextBlock>(), t => t.Text == "echo hi");
                var hint = Assert.Single(panel.GetVisualDescendants().OfType<global::PCL.MyHint>());
                Assert.Contains("hint", hint.Text);
                var image = Assert.Single(panel.GetVisualDescendants().OfType<global::PCL.MyImage>());
                Assert.Equal(500, image.MaxHeight);
                var quote = panel.GetVisualDescendants().OfType<Border>().First(b => b.BorderThickness.Left == 5);
                Assert.Equal(new Thickness(10, 2, 10, 2), quote.Padding);
                var quoteText = texts.First(t => t.Text == "Quote");
                Assert.Same(quote.BorderBrush, quoteText.Foreground);
                //列表项使用两列 Grid：标记在左列，正文在右列并占满剩余宽度
                var listGrid = panel.GetVisualDescendants().OfType<Grid>().First(
                    g => g.ColumnDefinitions.Count == 2 && g.Children.OfType<TextBlock>().Any(t => t.Text == "1."));
                var marker = listGrid.Children.OfType<TextBlock>().First(t => t.Text == "1.");
                var listContent = listGrid.Children.OfType<TextBlock>().First(t => t.Text == "one");
                Assert.Equal(0, Grid.GetColumn(marker));
                Assert.Equal(1, Grid.GetColumn(listContent));
                Assert.Contains(panel.GetVisualDescendants().OfType<Border>(),
                    b => b.BorderThickness.Left == 0 && Math.Abs(b.BorderThickness.Top - 1.5) < 0.01);
            } finally {
                window.Close();
            }
        } finally {
            try { File.Delete(imagePath); } catch { }
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
