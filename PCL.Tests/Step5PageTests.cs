using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace PCL.Tests;

public class AboutPageTests {
    [AvaloniaFact]
    public void About_page_builds_all_named_fields() {
        var page = new global::PCL.PageOtherAbout();
        var window = new Window { Width = 900, Height = 600, Content = page };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            Assert.NotNull(page.PanBack);
            Assert.NotNull(page.PanMain);
            Assert.NotNull(page.ItemAboutPcl);

            Assert.Contains("2.14.0.0", page.ItemAboutPcl.Info);
            Assert.Contains("PCL.Avalonia 是原版 PCL 的跨平台移植版本。", page.ItemAboutPcl.Info);
            var items = page.PanMain.GetVisualDescendants().OfType<global::PCL.MyListItem>().ToList();
            Assert.Contains(items, i => i.Title == "SuyihQwQ" && i.Info == "PCL.Avalonia 的开发者");
            var texts = page.PanMain.GetVisualDescendants().OfType<TextBlock>().Where(t => t.Text is not null).Select(t => t.Text!).ToList();
            Assert.Contains(texts, t => t.Contains("PCL.Avalonia：Copyright © 2026 SuyihQwQ"));
            Assert.Contains(texts, t => t.Contains("原版 PCL：Copyright © 成都瓜皮龙科技有限公司"));
            Assert.DoesNotContain("检查更新", page.PanMain.GetVisualDescendants().OfType<global::PCL.MyButton>().Where(b => b.Text is not null).Select(b => b.Text!));
            Assert.Contains("赞助 PCL !", page.PanMain.GetVisualDescendants().OfType<global::PCL.MyButton>().Where(b => b.Text is not null).Select(b => b.Text!));

            global::PCL.ModAnimation.AniTimer(800);
            Assert.True(page.PanBack.Bounds.Width > 0 && page.PanBack.Bounds.Height > 0);
            Assert.True(page.PanMain.Children.Count >= 4);
            Assert.True(page.PanBack.Extent.Height > page.PanBack.Viewport.Height);
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Left_page_exposes_help_about_and_test() {
        var keys = new[] { "UiHiddenOtherHelp", "UiHiddenOtherAbout", "UiHiddenOtherTest", "UiHiddenOtherFeedback", "UiHiddenOtherVote" };
        var original = keys.ToDictionary(k => k, k => global::PCL.Settings.Get<bool>(k));
        foreach (var key in keys) global::PCL.Settings.Set(key, false);
        try {
            var left = new global::PCL.PageOtherLeft();
            var window = new Window { Width = 220, Height = 420, Content = left };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            try {
                Assert.True(left.ItemHelp.Checked);
                Assert.Equal(global::PCL.FormMain.PageSubType.OtherHelp, left.PageID);
                Assert.True(left.ItemHelp.IsEnabled);
                Assert.True(left.ItemAbout.IsEnabled);
                Assert.True(left.ItemTest.IsEnabled);
                Assert.IsType<global::PCL.PageOtherHelp>(left.PageGet(global::PCL.FormMain.PageSubType.OtherHelp));
                Assert.IsType<global::PCL.PageOtherAbout>(left.PageGet(global::PCL.FormMain.PageSubType.OtherAbout));
                Assert.IsType<global::PCL.PageOtherTest>(left.PageGet(global::PCL.FormMain.PageSubType.OtherTest));
                Assert.NotNull(left.ItemAbout.LabTitle.Foreground);
                Assert.NotNull(left.ItemAbout.RectCheck);
                Assert.NotNull(left.ItemAbout.RectCheck.Background);
                var logo = Assert.IsType<Avalonia.Controls.Shapes.Path>(left.ItemAbout.PathLogo);
                Assert.NotNull(logo.Fill);
            } finally {
                window.Close();
            }
        } finally {
            foreach (var key in keys) global::PCL.Settings.Set(key, original[key]);
        }
    }

    [AvaloniaFact]
    public void FormMain_hosts_help_page_by_default() {
        var keys = new[] { "UiHiddenOtherHelp", "UiHiddenOtherAbout", "UiHiddenOtherTest" };
        var original = keys.ToDictionary(k => k, k => global::PCL.Settings.Get<bool>(k));
        foreach (var key in keys) global::PCL.Settings.Set(key, false);
        try {
            var window = new global::PCL.FormMain();
            window.Show();
            Dispatcher.UIThread.RunJobs();
            try {
                window.PageChange(global::PCL.FormMain.PageType.Other, global::PCL.FormMain.PageSubType.OtherHelp);
                for (var i = 0; i < 5; i++) {
                    global::PCL.ModAnimation.AniTimer(300);
                    Dispatcher.UIThread.RunJobs();
                }
                var left = Assert.IsType<global::PCL.PageOtherLeft>(window.PageLeft);
                var help = Assert.IsType<global::PCL.PageOtherHelp>(window.PageRight);
                Assert.Same(left, window.FindControl<Border>("PanMainLeft")!.Child);
                Assert.Same(help, window.FindControl<Border>("PanMainRight")!.Child);
                Assert.True(help.PageState is global::PCL.MyPageRight.PageStates.LoaderWait
                    or global::PCL.MyPageRight.PageStates.LoaderEnter
                    or global::PCL.MyPageRight.PageStates.LoaderStay
                    or global::PCL.MyPageRight.PageStates.ContentEnter
                    or global::PCL.MyPageRight.PageStates.ContentStay);
            } finally {
                window.Close();
            }
        } finally {
            foreach (var key in keys) global::PCL.Settings.Set(key, original[key]);
        }
    }

    [AvaloniaFact]
    public void List_item_and_hint_template_children_are_parented() {
        var listItem = new global::PCL.MyListItem { Title = "标题" };
        var window = new Window { Width = 300, Height = 200, Content = listItem };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            Assert.Same(listItem, listItem.LabTitle.Parent);
            Assert.Same(listItem.LabTitle, listItem.FindControl<TextBlock>("LabTitle"));
            Assert.True(listItem.Children.Count > 1);
        } finally {
            window.Close();
        }

        var hint = new global::PCL.MyHint { Text = "提示" };
        var window2 = new Window { Width = 300, Height = 200, Content = hint };
        window2.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            Assert.NotNull(hint.LabText.Parent);
            Assert.Same(hint.LabText, hint.FindControl<TextBlock>("LabText"));
            Assert.Equal("提示", hint.LabText.Text);
            Assert.True(hint.LabText.Bounds.Width > 0);
        } finally {
            window2.Close();
        }
    }

    [AvaloniaFact]
    public void Link_page_builds_named_fields_and_default_state() {
        var page = new global::PCL.PageLinkMain();
        var window = new Window { Width = 800, Height = 600, Content = page };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            Assert.NotNull(page.PanSelect);
            Assert.NotNull(page.PanSelectCreate);
            Assert.NotNull(page.PanSelectJoin);
            Assert.NotNull(page.PanFinish);
            Assert.False(page.PanFinish.IsVisible);
            Assert.NotNull(page.PanProgress);
            Assert.Equal(2, page.PanProgress.ColumnDefinitions.Count);
            Assert.NotNull(page.LabLoadTitle);
            Assert.NotNull(page.LabLoadDesc);
            Assert.NotNull(page.LabFinishPing);
            Assert.NotNull(page.LabFinishPlayer);
            Assert.NotNull(page.BtnFinishCopy);
            Assert.NotNull(page.BtnFinishExit);
            Assert.NotNull(page.HintFinish);
            Assert.Equal(global::PCL.PageLinkMain.LinkStates.Waiting, global::PCL.PageLinkMain.LinkState);
            Assert.Null(global::PCL.PageLinkMain.ValidateCodeFormat("P1234-ABCDE-02001"));
        } finally {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Modify_page_reuses_install_xaml() {
        var instance = new global::PCL.ModMinecraft.McInstance(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "PCL-Test-Instance"));
        var original = global::PCL.PageInstanceLeft.Instance;
        global::PCL.PageInstanceLeft.Instance = instance;
        var page = new global::PCL.PageDownloadModify();
        var window = new Window { Width = 900, Height = 600, Content = page };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            Assert.NotNull(page.PanSelect);
            Assert.NotNull(page.BtnStart);
            Assert.NotNull(page.CardMinecraft);
        } finally {
            // 关闭页面触发 Unloaded 取消初始化加载器，避免其失败后从后台线程访问已结束的测试会话
            window.Close();
            global::PCL.PageInstanceLeft.Instance = original;
            for (var i = 0; i < 50; i++) {
                Dispatcher.UIThread.RunJobs();
                Thread.Sleep(10);
            }
        }
    }
}

public class ResourcePageTests {

    [AvaloniaFact]
    public void Resource_search_source_only_offers_modrinth() {
        var page = new global::PCL.PageResource();
        var combo = page.FindControl<global::PCL.MyComboBox>("ComboSearchSource")!;
        Assert.Single(combo.Items.Cast<object>());
        var item = Assert.IsType<global::PCL.MyComboBoxItem>(combo.Items[0]);
        Assert.Equal("Modrinth", item.Content);
    }

    [AvaloniaFact]
    public void Resource_page_scrolls_with_wheel() {
        // 回归：PageResource 派生自 MyScrollViewer，若丢失自定义模板或没有同步滚动能力，内容会被裁剪且无法滚动
        var page = new global::PCL.PageResource();
        var loader = new global::PCL.ModLoader.LoaderTask<global::PCL.ResourceSearcher.SearchRequest, int>(
            "测试资源加载器", _ => { });
        page.Loader = loader;
        page.SearchTags = new List<global::PCL.MyComboBoxItem> {
            new() { Content = "全部", Tag = "0" }
        };
        var right = new global::PCL.MyPageRight { Content = page };
        var window = new Window { Width = 900, Height = 500, Content = right };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            for (var i = 0; i < 100 && loader.State == global::PCL.ModBase.LoadState.Loading; i++) {
                Dispatcher.UIThread.RunJobs();
                Thread.Sleep(10);
            }
            for (var i = 0; i < 100 && loader.LastRunningThread is { IsAlive: true }; i++) {
                Dispatcher.UIThread.RunJobs();
                Thread.Sleep(10);
            }
            Dispatcher.UIThread.RunJobs();
            page.PanLoad.IsVisible = false;
            page.PanAlways.IsVisible = true;
            page.PanContent.IsVisible = true;
            for (var i = 0; i < 40; i++) {
                page.PanProjects.Children.Add(new global::PCL.MyListItem { Title = "项目" + i, Height = 60 });
            }
            window.CaptureRenderedFrame();
            Dispatcher.UIThread.RunJobs();

            Assert.True(page.ScrollBarMaximum.Y > 1000,
                $"内容未能滚动：Extent={page.Extent}, Viewport={page.Viewport}, Max={page.ScrollBarMaximum}");
            var presenter = page.GetVisualDescendants()
                .OfType<Avalonia.Controls.Presenters.ScrollContentPresenter>().FirstOrDefault();
            Assert.NotNull(presenter);
            Assert.True(presenter!.CanVerticallyScroll, "滚动内容容器没有开启垂直滚动");

            var before = page.Offset.Y;
            window.MouseWheel(new Point(450, 400), new Vector(0, -3));
            for (var i = 0; i < 30; i++) {
                global::PCL.ModAnimation.AniTimer(30);
                Dispatcher.UIThread.RunJobs();
            }
            Assert.True(page.Offset.Y > before, $"滚轮未生效：before={before}, after={page.Offset.Y}");
        } finally {
            window.Close();
            page.Loader.Cancel();
        }
    }
}

public class PageLoaderTests {

    [AvaloniaFact]
    public void Page_loader_restart_keeps_input_from_input_invoke() {
        // 回归：重试时若把输入重置为 Nothing，页面加载器会在读取输入时抛出空引用异常
        var loader = new global::PCL.ModLoader.LoaderTask<object, object>("测试加载器", _ => { });
        var loading = new global::PCL.MyLoading { AutoRun = false, HasAnimation = false };
        var panLoader = new Border();
        var panContent = new Border();
        var page = new global::PCL.MyPageRight { Content = panContent };
        try {
            page.PageLoaderInit<object, object>(loading, panLoader, panContent, null!, loader,
                null!, new Func<object>(() => "输入"), true);
            PumpUntil(() => loader.State == global::PCL.ModBase.LoadState.Finished, "首次加载未完成");
            Assert.Equal("输入", loader.Input);
            page.PageLoaderRestart();
            Assert.Equal("输入", loader.Input);
            PumpUntil(() => loader.State == global::PCL.ModBase.LoadState.Finished, "重试加载未完成");
            DrainLoader(loader);
        } finally {
            loader.Cancel();
        }
    }

    /// <summary>
    /// 等待加载线程结束并清空它投递到 UI 线程的待处理回调，避免测试结束后残留线程访问控件。
    /// </summary>
    private static void DrainLoader<TIn, TOut>(global::PCL.ModLoader.LoaderTask<TIn, TOut> loader) {
        for (var i = 0; i < 100 && loader.LastRunningThread is { IsAlive: true }; i++) {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(10);
        }
        Dispatcher.UIThread.RunJobs();
    }

    private static void PumpUntil(Func<bool> condition, string message, int timeoutMs = 5000) {
        var start = Environment.TickCount64;
        while (Environment.TickCount64 - start < timeoutMs) {
            Dispatcher.UIThread.RunJobs();
            if (condition()) return;
            Thread.Sleep(20);
        }
        Dispatcher.UIThread.RunJobs();
        Assert.True(condition(), message);
    }
}

public class VersionSelectFolderTests {

    [AvaloniaFact]
    public void Newly_added_empty_folder_appears_in_version_select_list() {
        var originalFolders = global::PCL.Settings.Get<string>("LaunchFolders");
        var originalSelected = global::PCL.Settings.Get<string>("LaunchFolderSelect");
        var temp = Path.Combine(Path.GetTempPath(), "PCL-AddFolder-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        global::PCL.Settings.Set("LaunchFolders", "");
        var page = new global::PCL.PageSelectLeft();
        global::PCL.ModMain.FrmSelectLeft = page;
        var window = new Window { Width = 600, Height = 800, Content = page };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            global::PCL.ModMinecraft.McFolderListLoader.Start(null, true);
            PumpUntil(() => global::PCL.ModMinecraft.McFolderListLoader.State != global::PCL.ModBase.LoadState.Loading, "文件夹列表初始加载超时");
            PumpUntil(() => page.PanList.Children.OfType<global::PCL.MyListItem>().Any(item => item.Title == "添加已有文件夹"), "文件夹列表初始 UI 未生成");

            global::PCL.PageSelectLeft.AddFolder(temp, "新文件夹", false);
            var found = false;
            for (var i = 0; i < 300; i++) {
                Dispatcher.UIThread.RunJobs();
                Thread.Sleep(20);
                if (page.PanList.Children.OfType<global::PCL.MyListItem>().Any(item => item.Title == "新文件夹")) {
                    found = true;
                    break;
                }
            }
            var itemDump = string.Join(";", page.PanList.Children.OfType<global::PCL.MyListItem>().Select(i => i.Title + "@" + i.Info));
            var folderDump = string.Join(";", global::PCL.ModMinecraft.McFolderList.Select(f => f.Name + "@" + f.Location + "@" + f.Type));
            var savedFoldersNow = global::PCL.Settings.Get<string>("LaunchFolders");
            var stateNow = global::PCL.ModMinecraft.McFolderListLoader.State;
            Assert.True(found, $"添加的文件夹未出现在版本选择列表中；items={itemDump}；folders={folderDump}；saved={savedFoldersNow}；state={stateNow}");
            var entry = page.PanList.Children.OfType<global::PCL.MyListItem>().First(item => item.Title == "新文件夹");
            Assert.EndsWith(Path.DirectorySeparatorChar.ToString(), entry.Info);
            Assert.Contains(global::PCL.ModMinecraft.McFolderList,
                folder => folder.Type == global::PCL.ModMinecraft.McFolder.Types.Custom && folder.Name == "新文件夹");
            Assert.Contains("新文件夹>" + temp + Path.DirectorySeparatorChar,
                global::PCL.Settings.Get<string>("LaunchFolders"));
        } finally {
            global::PCL.Settings.Set("LaunchFolders", originalFolders);
            global::PCL.Settings.Set("LaunchFolderSelect", originalSelected);
            global::PCL.ModMinecraft.McFolderListLoader.Start(null, true);
            PumpUntil(() => global::PCL.ModMinecraft.McFolderListLoader.State != global::PCL.ModBase.LoadState.Loading, "文件夹列表清理加载超时");
            Dispatcher.UIThread.RunJobs();
            window.Close();
            global::PCL.ModMain.FrmSelectLeft = null!;
            global::PCL.ModMain.FrmSelectRight = null!;
            try { Directory.Delete(temp, true); } catch { /* 忽略清理失败 */ }
        }
    }

    private static void PumpUntil(Func<bool> condition, string message, int timeoutMs = 10000) {
        var start = Environment.TickCount64;
        while (Environment.TickCount64 - start < timeoutMs) {
            Dispatcher.UIThread.RunJobs();
            if (condition()) return;
            Thread.Sleep(20);
        }
        Dispatcher.UIThread.RunJobs();
        Assert.True(condition(), message);
    }
}

