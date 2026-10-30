using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Xunit;

[assembly: AvaloniaTestApplication(typeof(PCL.Tests.TestAppBuilder))]
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace PCL.Tests;

public static class TestAppBuilder {
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<global::PCL.App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}

public class AppSmokeTests {

    [AvaloniaFact]
    public void App_loads_with_resources() {
        Assert.IsType<global::PCL.App>(Application.Current);
        Assert.NotNull(Application.Current!.FindResource("PCLFont"));
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
