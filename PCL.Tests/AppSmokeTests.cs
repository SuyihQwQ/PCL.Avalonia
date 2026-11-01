using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Xunit;

[assembly: AvaloniaTestApplication(typeof(PCL.Tests.TestAppBuilder))]
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace PCL.Tests;

public static class TestAppBuilder {
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<global::PCL.App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .With(new FontManagerOptions {
                DefaultFamilyName = "avares://PCL/Assets/Fonts/PCLEnglish.ttf#PCL English, Noto Sans CJK SC, MiSans TC"
            });
}

public class AppSmokeTests {

    [AvaloniaFact]
    public void App_loads_with_resources() {
        Assert.IsType<global::PCL.App>(Application.Current);
        Assert.NotNull(Application.Current!.FindResource("PCLFont"));
    }

    [AvaloniaFact]
    public void Default_font_family_resolves_glyphs() {
        // 部分系统上 Skia 的默认字体是无法渲染的位图字体，必须确保 $Default 能解析到可用字体
        var ok = FontManager.Current.TryGetGlyphTypeface(
            new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Normal, FontStretch.Normal), out var glyphTypeface);
        Assert.True(ok, $"默认字体无法解析：{FontManager.Current.DefaultFontFamily}");
        Assert.False(string.IsNullOrEmpty(glyphTypeface!.FamilyName));
    }

    [AvaloniaFact]
    public void Window_can_be_shown_headless() {
        var window = new Window {
            Width = 400,
            Height = 300,
            Content = new TextBlock { Text = "hello" }
        };
        window.Show();
        Assert.True(window.IsVisible);
        window.Close();
    }

    [AvaloniaFact]
    public void FormMain_loads_generated_fields() {
        var window = new global::PCL.FormMain();
        Assert.NotNull(window.FindControl<Grid>("PanBack"));
        Assert.NotNull(window.FindControl<Grid>("PanTitle"));
        Assert.NotNull(window.FindControl<Grid>("PanMain"));
        Assert.NotNull(window.FindControl<TextBlock>("LabTitleLogo"));
    }

}
