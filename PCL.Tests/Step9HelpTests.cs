using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Xunit;

namespace PCL.Tests;

public class Step9HelpTests {

    private static string HelpRoot => Path.Combine(global::PCL.ModBase.PathTemp, "Help");

    private static void PumpUntil(Func<bool> condition, string message, int timeoutMs = 20000) {
        var stopwatch = Stopwatch.StartNew();
        while (!condition()) {
            Assert.True(stopwatch.ElapsedMilliseconds < timeoutMs, message);
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(5);
        }
        Dispatcher.UIThread.RunJobs();
    }

    private static string EnsureBuiltinHelp() {
        global::PCL.ModHelp.HelpTryExtract();
        var path = Path.Combine(HelpRoot, "启动器", "备份设置.json");
        Assert.True(File.Exists(path), "内置帮助未解压：" + path);
        return path;
    }

    private static void Settle(int frames = 5) {
        for (var i = 0; i < frames; i++) {
            global::PCL.ModAnimation.AniTimer(400);
            Dispatcher.UIThread.RunJobs();
        }
    }

    private static void PumpHelpLoader() {
        if (global::PCL.ModHelp.HelpLoader.State != global::PCL.ModBase.LoadState.Finished) {
            global::PCL.ModHelp.HelpLoader.Start();
        }
        PumpUntil(() => global::PCL.ModHelp.HelpLoader.State is global::PCL.ModBase.LoadState.Finished
            or global::PCL.ModBase.LoadState.Failed, "帮助加载器未在限定时间内完成");
        Assert.Equal(global::PCL.ModBase.LoadState.Finished, global::PCL.ModHelp.HelpLoader.State);
        Assert.NotEmpty(global::PCL.ModHelp.HelpLoader.Output);
    }

    [AvaloniaFact]
    public void Builtin_help_extracts_and_converts_xaml() {
        var path = EnsureBuiltinHelp();
        var jsonCount = Directory.GetFiles(HelpRoot, "*.json", SearchOption.AllDirectories).Length;
        var xamlCount = Directory.GetFiles(HelpRoot, "*.xaml", SearchOption.AllDirectories).Length;
        Assert.True(jsonCount >= 30, $"帮助条目数量异常：{jsonCount}");
        Assert.True(xamlCount >= 20, $"帮助 XAML 数量异常：{xamlCount}");

        var entry = new global::PCL.ModHelp.HelpEntry(path);
        Assert.False(string.IsNullOrWhiteSpace(entry.Title));
        Assert.Contains("启动器", entry.Types);
        Assert.False(string.IsNullOrWhiteSpace(entry.XamlContent));
        Assert.False(entry.IsEvent);

        var panel = Assert.IsType<StackPanel>(global::PCL.ModHelp.ConvertHelpXaml(entry.XamlContent));
        Assert.True(panel.Children.Count > 0);
        var window = new Window { Width = 900, Height = 700, Content = panel };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            Assert.True(panel.Bounds.Height > 0);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Custom_help_entry_round_trips_and_resolves_path() {
        var dir = Path.Combine(Path.GetTempPath(), "pcl-help-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try {
            var json = Path.Combine(dir, "测试条目.json");
            File.WriteAllText(json, """
                {"Title":"测试","Description":"描述","Keywords":"关键词","Types":["帮助","测试"],"IsEvent":true,"EventType":"弹出提示","EventData":"内容"}
                """);
            var entry = new global::PCL.ModHelp.HelpEntry(json);
            Assert.Equal("测试", entry.Title);
            Assert.Equal("描述", entry.Desc);
            Assert.True(entry.IsEvent);
            Assert.Equal(global::PCL.CustomEvent.EventType.弹出提示, entry.EventType);
            Assert.Equal(new[] { "帮助", "测试" }, entry.Types);
            var item = entry.ToListItem();
            Assert.Equal("测试", item.Title);
            Assert.Equal(42, item.Height);
            Assert.Same(entry, item.Tag);
        } finally {
            Directory.Delete(dir, true);
        }

        EnsureBuiltinHelp();
        var resolved = global::PCL.CustomEvent.GetAbsoluteUrls("启动器/备份设置.json", global::PCL.CustomEvent.EventType.打开帮助);
        Assert.True(File.Exists(resolved[0]), resolved[0]);
        Assert.StartsWith(HelpRoot, resolved[0]);
        // 联网帮助在 UI 线程中要求调用方自行设置 Title、Info 属性
        var uiError = Assert.Throws<Exception>(() =>
            global::PCL.CustomEvent.GetAbsoluteUrls("https://example.com/help.json", global::PCL.CustomEvent.EventType.打开帮助));
        Assert.Contains("Title、Info", uiError.Message);
        // 联网帮助必须指向 .json 文件
        Exception? netError = null;
        var worker = new Thread(() => {
            try {
                global::PCL.CustomEvent.GetAbsoluteUrls("https://example.com/help.xaml", global::PCL.CustomEvent.EventType.打开帮助);
            } catch (Exception ex) {
                netError = ex;
            }
        });
        worker.Start();
        worker.Join();
        Assert.NotNull(netError);
        Assert.Contains(".json", netError.Message);
    }

    [AvaloniaFact]
    public void Help_page_builds_grouped_cards() {
        PumpHelpLoader();
        var page = new global::PCL.PageOtherHelp();
        var window = new Window { Width = 900, Height = 700, Content = page };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            PumpUntil(() => page.PanList.Children.Count > 0, "帮助列表未生成", 10000);
            page.PageOnEnter();
            Settle();
            Assert.True(page.PanBack.IsVisible);
            Assert.True(page.PanList.IsVisible);
            Assert.True(page.PanList.Children.Count >= 2);

            var first = Assert.IsType<global::PCL.MyCard>(page.PanList.Children[0]);
            Assert.Equal("指南", first.Title);
            Assert.False(first.IsSwapped);
            var folded = page.PanList.Children.Skip(1).Cast<global::PCL.MyCard>().ToList();
            Assert.All(folded, card => Assert.True(card.IsSwapped, card.Title));
            Assert.All(folded, card => Assert.NotNull(card.SwapControl));
            Assert.True(page.PanSearch.IsVisible == false);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Help_search_shows_results_and_restores_list() {
        PumpHelpLoader();
        var page = new global::PCL.PageOtherHelp();
        var window = new Window { Width = 900, Height = 700, Content = page };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            PumpUntil(() => page.PanList.Children.Count > 0, "帮助列表未生成", 10000);
            page.PageOnEnter();
            Settle();

            page.SearchBox.Text = "备份设置";
            Settle(3);
            Assert.True(page.PanSearch.IsVisible);
            Assert.Equal("搜索结果", page.PanSearch.Title);
            Assert.True(page.PanSearchList.IsVisible);
            Assert.True(page.PanSearchList.Children.Count > 0);
            Assert.Contains(page.PanSearchList.Children.Cast<global::PCL.MyListItem>(),
                item => item.Title.Contains("备份"));
            Assert.False(page.PanList.IsVisible);

            page.SearchBox.Text = "";
            Settle(3);
            Assert.False(page.PanSearch.IsVisible);
            Assert.True(page.PanList.IsVisible);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Help_detail_navigation_shows_back_button_and_restores_list() {
        PumpHelpLoader();
        var entry = new global::PCL.ModHelp.HelpEntry(EnsureBuiltinHelp());
        var window = new global::PCL.FormMain();
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            window.PageChange(global::PCL.FormMain.PageType.Other, global::PCL.FormMain.PageSubType.OtherHelp);
            Settle();
            var help = Assert.IsType<global::PCL.PageOtherHelp>(window.PageRight);
            PumpUntil(() => help.PanList.Children.Count > 0, "帮助列表未生成", 10000);
            Settle();

            var detail = new global::PCL.PageOtherHelpDetail();
            Assert.True(detail.Init(entry));
            Assert.Same(entry, detail.Entry);
            Assert.True(detail.PanCustom.Children.Count > 0);

            var titleInner = window.FindControl<Grid>("PanTitleInner")!;
            var leftPanel = window.FindControl<Border>("PanMainLeft")!;
            var mainRight = window.FindControl<Border>("PanMainRight")!;
            window.PageChangeHelpDetail(entry, detail);
            Settle();
            Dispatcher.UIThread.RunJobs();

            Assert.True(titleInner.IsVisible);
            Assert.Equal(entry.Title, window.FindControl<TextBlock>("LabTitleInner")!.Text);
            Assert.False(leftPanel.IsVisible);
            Assert.Same(detail, window.PageRight);
            Assert.Same(detail, mainRight.Child);
            Assert.True(detail.PageState is global::PCL.MyPageRight.PageStates.ContentEnter
                or global::PCL.MyPageRight.PageStates.ContentStay);
            Assert.True(detail.IsEffectivelyVisible);
            Assert.Equal(1, detail.Opacity);
            Assert.True(detail.PanBack.IsVisible);
            Assert.True(detail.PanBack.Bounds.Width > 0, $"PanBack 尺寸：{detail.PanBack.Bounds}");
            Assert.True(detail.PanCustom.Bounds.Height > 0, $"PanCustom 尺寸：{detail.PanCustom.Bounds}");
            Assert.All(detail.PanCustom.Children, child => Assert.True(child.Opacity > 0.9, $"子控件透明度：{child.Opacity}"));

            window.PageBack();
            Settle();
            Dispatcher.UIThread.RunJobs();

            Assert.False(titleInner.IsVisible);
            Assert.True(leftPanel.IsVisible);
            Assert.Same(help, window.PageRight);
            Assert.Same(help, mainRight.Child);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Help_nested_detail_back_returns_to_previous_detail() {
        PumpHelpLoader();
        var entryA = new global::PCL.ModHelp.HelpEntry(EnsureBuiltinHelp());
        var entryBPath = Path.Combine(HelpRoot, "Minecraft", "安装 Mod.json");
        Assert.True(File.Exists(entryBPath), "未找到嵌套帮助：" + entryBPath);
        var entryB = new global::PCL.ModHelp.HelpEntry(entryBPath);
        var window = new global::PCL.FormMain();
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            window.PageChange(global::PCL.FormMain.PageType.Other, global::PCL.FormMain.PageSubType.OtherHelp);
            Settle();
            var help = Assert.IsType<global::PCL.PageOtherHelp>(window.PageRight);
            PumpUntil(() => help.PanList.Children.Count > 0, "帮助列表未生成", 10000);
            Settle();

            var detailA = new global::PCL.PageOtherHelpDetail();
            Assert.True(detailA.Init(entryA));
            var detailB = new global::PCL.PageOtherHelpDetail();
            Assert.True(detailB.Init(entryB));

            var titleInner = window.FindControl<Grid>("PanTitleInner")!;
            var leftPanel = window.FindControl<Border>("PanMainLeft")!;
            var mainRight = window.FindControl<Border>("PanMainRight")!;
            var titleText = window.FindControl<TextBlock>("LabTitleInner")!;

            window.PageChangeHelpDetail(entryA, detailA);
            Settle();
            window.PageChangeHelpDetail(entryB, detailB);
            Settle();
            Assert.Same(detailB, window.PageRight);
            Assert.Equal(entryB.Title, titleText.Text);

            window.PageBack();
            Settle();
            Assert.Same(detailA, window.PageRight);
            Assert.Same(detailA, mainRight.Child);
            Assert.True(titleInner.IsVisible);
            Assert.False(leftPanel.IsVisible);
            Assert.Equal(entryA.Title, titleText.Text);

            window.PageBack();
            Settle();
            Assert.Same(help, window.PageRight);
            Assert.True(leftPanel.IsVisible);
            Assert.False(titleInner.IsVisible);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void MyColor_accepts_immutable_theme_brushes() {
        var brush = Assert.IsAssignableFrom<Avalonia.Media.ISolidColorBrush>(Avalonia.Application.Current!.FindResource("ColorBrush1"));
        var color = new global::PCL.ModBase.MyColor(brush);
        Assert.Equal(brush.Color.A, (double)color.A, 0.5);
        Assert.Equal(brush.Color.R, (double)color.R, 0.5);
        Assert.Equal(brush.Color.G, (double)color.G, 0.5);
        Assert.Equal(brush.Color.B, (double)color.B, 0.5);
    }

    [AvaloniaFact]
    public void Search_ranks_exact_title_first() {
        var entries = new List<global::PCL.ModSearch.SearchEntry<string>> {
            new() { Item = "安装 Mod", SearchSource = new() { new global::PCL.ModSearch.SearchSource("安装 Mod") } },
            new() { Item = "启动器", SearchSource = new() { new global::PCL.ModSearch.SearchSource("启动器") } },
            new() { Item = "备份设置", SearchSource = new() { new global::PCL.ModSearch.SearchSource("备份设置") } }
        };
        var result = global::PCL.ModSearch.Search(entries, "启动器");
        Assert.Equal("启动器", result[0].Item);
        Assert.True(result[0].AbsoluteRight);
    }

}
