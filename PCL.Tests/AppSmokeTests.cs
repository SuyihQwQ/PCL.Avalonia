using System.Reflection;
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
                DefaultFamilyName = "avares://PCL.Avalonia/Assets/Fonts/PCLEnglish.ttf#PCL English, Noto Sans CJK SC, MiSans TC"
            });
}

public class AppSmokeTests {

    [AvaloniaFact]
    public void App_loads_with_resources() {
        Assert.IsType<global::PCL.App>(Application.Current);
        Assert.NotNull(Application.Current!.FindResource("PCLFont"));
    }

    [Fact]
    public void Assembly_name_and_metadata_use_pcl_avalonia() {
        var assembly = typeof(global::PCL.FormMain).Assembly;
        Assert.Equal("PCL.Avalonia", assembly.GetName().Name);
        Assert.Equal("PCL.Avalonia", assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title);
        Assert.Equal("PCL.Avalonia", assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product);
    }

    [AvaloniaFact]
    public void Language_auto_uses_a_supported_language() {
        var original = global::PCL.Localization.CurrentLanguage;
        try {
            global::PCL.Localization.SetLanguage("auto");
            Assert.Contains(global::PCL.Localization.CurrentLanguage, new[] { "zh-CN", "en-US" });
            global::PCL.Localization.SetLanguage("fr-FR");
            Assert.Equal("zh-CN", global::PCL.Localization.CurrentLanguage);
        } finally {
            global::PCL.Localization.SetLanguage(original);
        }
    }

    [AvaloniaFact]
    public void Localization_switches_between_chinese_and_english() {
        global::PCL.Localization.Initialize("zh-CN");
        Assert.Equal("取消", global::PCL.Localization.GetText("LangDialogBtnCancel"));

        var text = new TextBlock();
        text.SetResourceReference(TextBlock.TextProperty, "LangDialogBtnCancel");
        var window = new Window { Content = text };
        window.Show();
        Assert.Equal("取消", text.Text);

        global::PCL.Localization.SetLanguage("en-US");
        Assert.Equal("Cancel", global::PCL.Localization.GetText("LangDialogBtnCancel"));
        Assert.Equal("Cancel", global::PCL.Localization.TranslateText("取消"));
        Assert.Equal("龙腾猫跃 (Long Teng Maoyue)", global::PCL.Localization.TranslateText("龙腾猫跃"));
        Assert.Equal("Cancel", text.Text);

        global::PCL.Localization.SetLanguage("zh-CN");
        Assert.Equal("取消", global::PCL.Localization.TranslateText("取消"));
        Assert.Equal("取消", text.Text);
        window.Close();
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

    [AvaloniaFact]
    public void FormMain_has_window_icon_and_wayland_app_id() {
        var window = new global::PCL.FormMain();
        Assert.NotNull(window.Icon);
        Assert.Equal("PCL.Avalonia", global::Avalonia.Controls.WaylandProperties.GetAppId(window));
    }

    [AvaloniaFact]
    public void Splash_window_shows_icon_without_taskbar_entry() {
        var splash = new global::PCL.FormSplash();
        Assert.False(splash.ShowInTaskbar);
        var image = Assert.IsType<Image>(splash.Content);
        Assert.NotNull(image.Source);
    }

    [AvaloniaFact]
    public void Linux_desktop_entry_installs_launcher_and_icon() {
        if (!OperatingSystem.IsLinux()) return;
        var oldDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        var oldDataDirs = Environment.GetEnvironmentVariable("XDG_DATA_DIRS");
        var root = Path.Combine(Path.GetTempPath(), "pcl-desktop-" + Guid.NewGuid().ToString("N"));
        try {
            Environment.SetEnvironmentVariable("XDG_DATA_HOME", Path.Combine(root, "data"));
            Environment.SetEnvironmentVariable("XDG_DATA_DIRS", Path.Combine(root, "system"));
            global::PCL.Program.EnsureLinuxDesktopEntry();
            var desktop = Path.Combine(root, "data", "applications", "PCL.Avalonia.desktop");
            Assert.True(File.Exists(desktop));
            var text = File.ReadAllText(desktop);
            var icon = Path.Combine(root, "data", "icons", "hicolor", "256x256", "apps", "PCL.Avalonia.png");
            Assert.Contains("Icon=" + icon, text);
            Assert.Contains("StartupWMClass=PCL.Avalonia", text);
            Assert.True(File.Exists(icon));
        } finally {
            Environment.SetEnvironmentVariable("XDG_DATA_HOME", oldDataHome);
            Environment.SetEnvironmentVariable("XDG_DATA_DIRS", oldDataDirs);
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

}
