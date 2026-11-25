// SPDX-License-Identifier: Apache-2.0
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Xunit;

namespace PCL.Tests;

/// <summary>
/// 主页/帮助 XAML 转换的保真度回归：这些构造在原版 WPF 中合法，转换器既不能崩，也不能丢掉显式样式。
/// </summary>
public class HomepageFidelityTests {
    private static string Convert(string wpfXaml) => global::PCL.ModHelp.ConvertLegacyXaml(wpfXaml);

    [AvaloniaFact]
    public void Explicit_inline_uicontainer_is_not_wrapped_twice() {
        const string page = "<FlowDocumentScrollViewer><FlowDocument><Paragraph>文字 <InlineUIContainer><local:MyImage Width=\"16\" Height=\"16\"/></InlineUIContainer> 结束</Paragraph></FlowDocument></FlowDocumentScrollViewer>";
        var converted = Convert(page);
        Assert.DoesNotContain("InlineUIContainer><InlineUIContainer", converted);
        var wrapped = "<StackPanel xmlns=\"https://github.com/avaloniaui\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" xmlns:local=\"using:PCL\">" + converted + "</StackPanel>";
        var root = global::PCL.ModHelp.GetObjectFromXML(wrapped);
        Assert.NotNull(root);
    }

    [AvaloniaFact]
    public void Paragraph_explicit_style_margin_is_kept() {
        const string page = "<StackPanel><StackPanel.Resources><Style TargetType=\"Paragraph\"><Setter Property=\"Margin\" Value=\"0,8\"/></Style><Style x:Key=\"H2\" TargetType=\"Paragraph\"><Setter Property=\"FontSize\" Value=\"22\"/><Setter Property=\"Margin\" Value=\"0,10,0,5\"/></Style></StackPanel.Resources>" +
                            "<FlowDocumentScrollViewer><FlowDocument><Paragraph Style=\"{StaticResource H2}\">标题</Paragraph><Paragraph>正文</Paragraph></FlowDocument></FlowDocumentScrollViewer></StackPanel>";
        var converted = Convert(page);
        Assert.Contains("Margin=\"0,10,0,5\"", converted);
        // 段落样式用类选择器，普通 TextBlock 不再吃段落 Margin
        Assert.Contains("Selector=\"TextBlock.PCLLegacyParagraph\"", converted);
        Assert.Contains("Classes=\"PCLLegacyParagraph\"", converted);
    }

    [AvaloniaFact]
    public void Paragraph_document_properties_do_not_break_page() {
        const string page = "<FlowDocumentScrollViewer><FlowDocument FontSize=\"15\" FontFamily=\"Microsoft YaHei UI\" TextAlignment=\"Left\">" +
                            "<Paragraph Background=\"#FFEEEEEE\" BorderThickness=\"1\" Padding=\"2\">段落</Paragraph></FlowDocument></FlowDocumentScrollViewer>";
        var converted = Convert(page);
        // 文档级文字属性继承到段落，容器上不再保留非法属性
        Assert.Contains("FontSize=\"15\"", converted);
        Assert.DoesNotContain("Padding=\"2\"", converted);
        var wrapped = "<StackPanel xmlns=\"https://github.com/avaloniaui\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" xmlns:local=\"using:PCL\">" + converted + "</StackPanel>";
        Assert.NotNull(global::PCL.ModHelp.GetObjectFromXML(wrapped));
    }

    [AvaloniaFact]
    public void Blockcode_keeps_style_text_wrapping() {
        const string page = "<StackPanel><StackPanel.Resources><Style x:Key=\"BlockCode\" TargetType=\"TextBox\"><Setter Property=\"TextWrapping\" Value=\"Wrap\"/><Setter Property=\"FontSize\" Value=\"14\"/></Style></StackPanel.Resources>" +
                            "<FlowDocumentScrollViewer><FlowDocument><TextBox Style=\"{StaticResource BlockCode}\">代码</TextBox></FlowDocument></FlowDocumentScrollViewer></StackPanel>";
        var converted = Convert(page);
        Assert.Contains("TextWrapping=\"Wrap\"", converted);
    }

    [AvaloniaFact]
    public void Hidden_keeps_layout_but_collapsed_hides() {
        const string page = "<StackPanel><Border Visibility=\"Hidden\" Height=\"20\"/><Border Visibility=\"Collapsed\" Height=\"20\"/></StackPanel>";
        var converted = Convert(page);
        Assert.Contains("Opacity=\"0\"", converted);
        Assert.Contains("IsHitTestVisible=\"False\"", converted);
        Assert.Contains("IsVisible=\"False\"", converted);
    }

    [AvaloniaFact]
    public void Simple_contentcontrol_template_renders_separator_lines() {
        const string page = "<StackPanel><StackPanel.Resources>" +
            "<ControlTemplate x:Key=\"Separator\" TargetType=\"ContentControl\">" +
            "<Grid Margin=\"0,10\"><Grid.ColumnDefinitions><ColumnDefinition Width=\"1*\"/><ColumnDefinition Width=\"100\"/><ColumnDefinition Width=\"1*\"/></Grid.ColumnDefinitions>" +
            "<Line X1=\"0\" X2=\"100\" Stroke=\"{DynamicResource ColorBrush4}\" StrokeThickness=\"1.5\" HorizontalAlignment=\"Center\" Stretch=\"Fill\" Grid.Column=\"0\"/>" +
            "<TextBlock Text=\"{TemplateBinding Content}\" HorizontalAlignment=\"Center\" FontSize=\"15\" Foreground=\"{DynamicResource ColorBrush4}\" Grid.Column=\"1\" VerticalAlignment=\"Center\"/>" +
            "<Line X1=\"0\" X2=\"100\" Stroke=\"{DynamicResource ColorBrush4}\" StrokeThickness=\"1.5\" HorizontalAlignment=\"Center\" Stretch=\"Fill\" Grid.Column=\"2\"/>" +
            "</Grid></ControlTemplate></StackPanel.Resources>" +
            "<ContentControl Content=\"周 边 新 闻\" Template=\"{StaticResource Separator}\"/></StackPanel>";
        var converted = Convert(page);
        Assert.Contains("<Rectangle", converted);
        Assert.Contains("周 边 新 闻", converted);
        Assert.DoesNotContain("<Line", converted);
        var wrapped = "<StackPanel xmlns=\"https://github.com/avaloniaui\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" xmlns:local=\"using:PCL\">" + converted + "</StackPanel>";
        Assert.NotNull(global::PCL.ModHelp.GetObjectFromXML(wrapped));
    }

    [AvaloniaFact]
    public void Hyperlink_and_table_are_downgraded_instead_of_crashing() {
        const string page = "<FlowDocumentScrollViewer><FlowDocument><Paragraph><Hyperlink NavigateUri=\"https://example.com\">链接</Hyperlink></Paragraph></FlowDocument></FlowDocumentScrollViewer>" +
                            "<Table><TableRowGroup><TableRow><TableCell><Paragraph>单元格</Paragraph></TableCell></TableRow></TableRowGroup></Table>";
        var converted = Convert(page);
        Assert.Contains("<Underline", converted);
        Assert.DoesNotContain("<Hyperlink", converted);
        Assert.DoesNotContain("<Table", converted);
        var wrapped = "<StackPanel xmlns=\"https://github.com/avaloniaui\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" xmlns:local=\"using:PCL\">" + converted + "</StackPanel>";
        Assert.NotNull(global::PCL.ModHelp.GetObjectFromXML(wrapped));
    }
}
