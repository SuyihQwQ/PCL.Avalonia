// SPDX-License-Identifier: Apache-2.0
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace PCL.Tests;

/// <summary>
/// 帮助迁移回归测试。
/// 原版 PCL 2.14.0.0 的 Resources/Help.zip 与移植版 PCL/Assets/Help.zip 逐字节一致
/// （md5 3e15690ac5fa756575f57268b242b7ae，70 个 entry = 40 个 json + 30 个 xaml），
/// 因此这里固化 2.14 的 topic 清单，确保迁移后的帮助不会丢条目、改标题或退化成无法加载。
/// </summary>
public class HelpMigrationTests {

    /// <summary>
    /// 原版 PCL 2.14.0.0 Help.zip 的 40 个 topic：Json 相对路径（不含扩展名）→ Title。
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> OriginalTopics = new Dictionary<string, string>(StringComparer.Ordinal) {
        ["Minecraft/全面教程"] = "Minecraft 教程",
        ["Minecraft/加入服务器"] = "多人游戏常见问题",
        ["Minecraft/存档损坏"] = "存档损坏的解决方案",
        ["Minecraft/安装 Mod"] = "安装 Mod",
        ["Minecraft/安装世界"] = "安装存档",
        ["Minecraft/安装光影"] = "安装光影",
        ["Minecraft/导出整合包"] = "导出整合包",
        ["Minecraft/微软账号登录失败"] = "微软登录卡死或失败的解决方案",
        ["Minecraft/数据包安装"] = "安装数据包",
        ["Minecraft/整合包安装"] = "安装整合包",
        ["Minecraft/更新 Mod"] = "更新 Mod",
        ["Minecraft/联机"] = "使用 PCL 进行多人游戏",
        ["Minecraft/购买正版"] = "购买 Minecraft 正版",
        ["Minecraft/资源包安装"] = "安装资源包",
        ["个性化/XAML 格式"] = "自定义主页与帮助 XAML 格式",
        ["个性化/替换标记"] = "替换标记",
        ["个性化/自定义事件"] = "自定义事件列表",
        ["启动器/LittleSkin 外置登录使用教程"] = "设置 LittleSkin 登录",
        ["启动器/Microsoft Defender 添加排除项"] = "将 PCL 添加到 Windows 安全中心白名单",
        ["启动器/备份设置"] = "备份启动器设置",
        ["启动器/密码登录正版"] = "使用密码登录正版账号",
        ["启动器/手动选择 Java 路径"] = "手动选择 Java",
        ["启动器/指定登录方式"] = "设置第三方登录、限制登录方式",
        ["启动器/角色皮肤导出"] = "导出角色皮肤",
        ["启动器/解锁主题 - Public"] = "解锁主题使用",
        ["启动器/解锁主题 - Snapshot"] = "解锁隐藏主题",
        ["帮助/提交帮助 - GitHub Desktop"] = "使用 GitHub Desktop 获取帮助库",
        ["帮助/提交帮助 - Intellij Idea"] = "使用 IntelliJ IDEA 获取帮助库",
        ["帮助/提交帮助 - VSCode"] = "使用 VSCode 获取帮助库",
        ["帮助/提交帮助 - 编写规范"] = "PCL 帮助库编写规范",
        ["帮助/提交帮助"] = "参与完善 PCL 帮助库",
        ["帮助/自定义帮助 - JSON 格式"] = "帮助 JSON 格式",
        ["帮助/自定义帮助"] = "自定义帮助页面",
        ["指南/整合包制作 - Public"] = "整合包制作指南",
        ["指南/整合包制作 - Snapshot"] = "整合包制作指南",
        ["指南/资源安装指南"] = "资源安装指南",
        ["指南/赞助与内测指南"] = "赞助与内测指南",
        ["百科/MCMOD"] = "MC 百科",
        ["百科/Minecraft Wiki"] = "原版百科",
        ["百科/Mineplugin Wiki"] = "插件百科",
    };

    /// <summary>原版 Help.zip 的 json / xaml 数量。</summary>
    private const int OriginalJsonCount = 40;
    private const int OriginalXamlCount = 30;

    /// <summary>帮助条目允许出现的分类（原版 Help.zip 只用了这 5 个）。</summary>
    private static readonly string[] KnownTypes = ["Minecraft", "个性化", "启动器", "指南", "百科"];

    /// <summary>
    /// 没有 Types 的 5 个帮助条目不进入分类列表，只能从其他帮助页内联链接进入（与原版一致）。
    /// </summary>
    private static readonly string[] LinkOnlyTopics = [
        "帮助/提交帮助 - GitHub Desktop",
        "帮助/提交帮助 - Intellij Idea",
        "帮助/提交帮助 - VSCode",
        "帮助/提交帮助 - 编写规范",
        "帮助/自定义帮助 - JSON 格式",
    ];

    private static string HelpRoot => Path.Combine(global::PCL.ModBase.PathTemp, "Help");

    private static string TopicJsonPath(string topic) =>
        Path.Combine(HelpRoot, topic.Replace('/', Path.DirectorySeparatorChar) + ".json");

    /// <summary>所有 topic 的路径，作为 Theory 数据（30 个内容 topic + 10 个事件 topic）。</summary>
    public static IEnumerable<object[]> AllTopicPaths =>
        OriginalTopics.Keys.OrderBy(key => key, StringComparer.Ordinal).Select(key => new object[] { key });

    private static void EnsureExtracted() {
        global::PCL.ModHelp.HelpTryExtract();
        Assert.True(File.Exists(Path.Combine(HelpRoot, "启动器", "备份设置.json")), "内置帮助未解压：" + HelpRoot);
    }

    private static void PumpUntil(Func<bool> condition, string message, int timeoutMs = 20000) {
        var stopwatch = Stopwatch.StartNew();
        while (!condition()) {
            Assert.True(stopwatch.ElapsedMilliseconds < timeoutMs, message);
            global::PCL.ModAnimation.AniTimer(60);
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(3);
        }
        Dispatcher.UIThread.RunJobs();
    }

    private static void PumpHelpLoader() {
        if (global::PCL.ModHelp.HelpLoader.State != global::PCL.ModBase.LoadState.Finished) {
            global::PCL.ModHelp.HelpLoader.Start();
        }
        PumpUntil(() => global::PCL.ModHelp.HelpLoader.State is global::PCL.ModBase.LoadState.Finished
            or global::PCL.ModBase.LoadState.Failed, "帮助加载器未在限定时间内完成");
        Assert.Equal(global::PCL.ModBase.LoadState.Finished, global::PCL.ModHelp.HelpLoader.State);
    }

    private static void Settle(int frames = 4) {
        for (var i = 0; i < frames; i++) {
            global::PCL.ModAnimation.AniTimer(400);
            Dispatcher.UIThread.RunJobs();
        }
    }

    /// <summary>当前构建类型下应当显示在帮助列表里的条目数（条目自身带 ShowInPublic / ShowInSnapshot 过滤）。</summary>
    private static int ExpectedVisibleCount(IReadOnlyList<global::PCL.ModHelp.HelpEntry> entries) =>
        entries.Count(entry => global::PCL.ModBase.BuildType == global::PCL.ModBase.BuildTypes.Release
            ? entry.ShowInPublic
            : entry.ShowInSnapshot);

    /// <summary>
    /// 当前构建类型下分类列表里的条目数：可见条目的分类数之和（同一 topic 属于多个分类时会重复出现；
    /// 没有 Types 的条目只能通过内联链接进入，不计入列表）。
    /// </summary>
    private static int ExpectedListItemCount(IReadOnlyList<global::PCL.ModHelp.HelpEntry> entries) =>
        entries
            .Where(entry => global::PCL.ModBase.BuildType == global::PCL.ModBase.BuildTypes.Release
                ? entry.ShowInPublic
                : entry.ShowInSnapshot)
            .Sum(entry => entry.Types.Count);

    [AvaloniaFact]
    public void Original_2_14_help_topics_are_all_present() {
        EnsureExtracted();

        var jsonFiles = Directory.GetFiles(HelpRoot, "*.json", SearchOption.AllDirectories);
        var xamlFiles = Directory.GetFiles(HelpRoot, "*.xaml", SearchOption.AllDirectories);
        Assert.Equal(OriginalJsonCount, jsonFiles.Length);
        Assert.Equal(OriginalXamlCount, xamlFiles.Length);

        // 不允许缺失 topic，也不允许出现清单之外的 topic 造成列表漂移
        var actualTopics = jsonFiles
            .Select(file => Path.GetRelativePath(HelpRoot, file))
            .Select(relative => relative.Replace(Path.DirectorySeparatorChar, '/'))
            .Select(relative => relative[..^".json".Length])
            .OrderBy(topic => topic, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(OriginalTopics.Keys.OrderBy(topic => topic, StringComparer.Ordinal).ToArray(), actualTopics);

        // 每个 topic 的 Title 必须与原版一致；内容 topic 必须有非空 xaml，事件 topic 必须有事件数据
        foreach (var (topic, expectedTitle) in OriginalTopics) {
            var entry = new global::PCL.ModHelp.HelpEntry(TopicJsonPath(topic));
            Assert.Equal(expectedTitle, entry.Title);
            Assert.All(entry.Types, type => Assert.Contains(type, KnownTypes));
            if (entry.IsEvent) {
                Assert.False(string.IsNullOrWhiteSpace(entry.EventData), $"{topic} 的事件数据为空");
            } else {
                Assert.False(string.IsNullOrWhiteSpace(entry.XamlContent), $"{topic} 缺少 xaml 内容");
            }
        }

        // 无分类的帮助条目只作为内联链接目标，不进分类列表；清单必须与原版一致
        var topicsWithoutTypes = OriginalTopics.Keys
            .Where(topic => new global::PCL.ModHelp.HelpEntry(TopicJsonPath(topic)).Types.Count == 0)
            .OrderBy(topic => topic, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(LinkOnlyTopics.OrderBy(topic => topic, StringComparer.Ordinal).ToArray(), topicsWithoutTypes);
    }

    [AvaloniaTheory]
    [MemberData(nameof(AllTopicPaths))]
    public void Every_original_help_topic_loads_in_headless(string topic) {
        EnsureExtracted();
        var entry = new global::PCL.ModHelp.HelpEntry(TopicJsonPath(topic));
        Assert.Equal(OriginalTopics[topic], entry.Title);

        // 事件 topic 没有 xaml，只需要能解析出合法事件；内容 topic 必须能真正转换并挂到窗口上渲染
        if (entry.IsEvent) {
            Assert.False(string.IsNullOrWhiteSpace(entry.EventData), $"{topic} 的事件数据为空");
            return;
        }

        var content = global::PCL.ModHelp.ConvertHelpXaml(entry.XamlContent);
        var window = new Window { Width = 900, Height = 700, Content = content };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            Assert.True(content.IsEffectivelyVisible, $"{topic} 转换结果不可见");
            Assert.True(content.Bounds.Width > 0, $"{topic} 渲染宽度为 0：{content.Bounds}");
            Assert.True(content.Bounds.Height > 0, $"{topic} 渲染高度为 0：{content.Bounds}");
            Assert.True(content.GetVisualDescendants().Count() >= 3, $"{topic} 渲染后的视觉树过小");
        } finally {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }

    [AvaloniaFact]
    public void Help_list_shows_every_visible_topic_from_the_zip() {
        EnsureExtracted();
        PumpHelpLoader();
        var entries = global::PCL.ModHelp.HelpLoader.Output!;

        // 加载器输出必须与 zip 内的 topic 数一致（40 个，含事件条目）
        Assert.Equal(OriginalJsonCount, entries.Count);
        var expectedVisible = ExpectedVisibleCount(entries);
        Assert.InRange(expectedVisible, 1, OriginalJsonCount);

        // 列表页需要挂在主窗口里才能触发展开动画与内容构建，这里走真实的页面切换路径
        var window = new global::PCL.FormMain();
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            window.PageChange(global::PCL.FormMain.PageType.Other, global::PCL.FormMain.PageSubType.OtherHelp);
            Settle();
            var page = Assert.IsType<global::PCL.PageOtherHelp>(window.PageRight);
            PumpUntil(() => page.PanList.Children.Count > 0, "帮助列表未生成", 10000);
            Settle();
            var cards = page.PanList.Children.OfType<global::PCL.MyCard>().ToList();
            Assert.Equal(5, cards.Count); // 指南、启动器、Minecraft、个性化、百科
            Assert.Equal("指南", cards[0].Title);
            Assert.False(cards[0].IsSwapped);
            Assert.Equal(
                KnownTypes.OrderBy(type => type, StringComparer.Ordinal).ToArray(),
                cards.Select(card => card.Title).OrderBy(title => title, StringComparer.Ordinal).ToArray());

            // 折叠的分类卡片在展开时才构建列表项，这里全部展开后统计条目总数
            foreach (var card in cards) {
                card.IsSwapped = false;
                Dispatcher.UIThread.RunJobs();
            }
            Settle();
            var itemCount = cards
                .Select(card => card.SwapControl as Panel)
                .Where(panel => panel is not null)
                .Sum(panel => panel!.Children.OfType<global::PCL.MyListItem>().Count());
            // 40 个 topic 中：3 个被构建类型标记过滤（联机 / 两个 Public 条目），5 个无分类只作为内联链接目标
            Assert.Equal(ExpectedListItemCount(entries), itemCount);
            Assert.True(itemCount > 0);
        } finally {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }

    [AvaloniaFact]
    public void Help_pages_support_refresh_and_detail_reloads_from_disk() {
        EnsureExtracted();
        Assert.True(typeof(global::PCL.IRefreshable).IsAssignableFrom(typeof(global::PCL.PageOtherHelp)));
        Assert.True(typeof(global::PCL.IRefreshable).IsAssignableFrom(typeof(global::PCL.PageOtherHelpDetail)));

        // 详情页：手动编辑自定义帮助文件后，Refresh（IRefreshable）应重新从 RawPath 加载内容
        var dir = Path.Combine(Path.GetTempPath(), "pcl-help-refresh-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try {
            var jsonPath = Path.Combine(dir, "刷新测试.json");
            var xamlPath = Path.Combine(dir, "刷新测试.xaml");
            File.WriteAllText(jsonPath, """{"Title":"刷新测试","Description":"用于回归测试","Types":["帮助"]}""");
            File.WriteAllText(xamlPath, "<TextBlock Text=\"刷新前的内容\" />");

            var detail = new global::PCL.PageOtherHelpDetail();
            Assert.IsAssignableFrom<global::PCL.IRefreshable>(detail);
            Assert.True(detail.Init(new global::PCL.ModHelp.HelpEntry(jsonPath)));
            Assert.Contains(detail.PanCustom.GetVisualDescendants().OfType<TextBlock>(),
                text => text.Text == "刷新前的内容");

            File.WriteAllText(xamlPath, "<TextBlock Text=\"刷新后的内容\" />");
            detail.Refresh();
            Dispatcher.UIThread.RunJobs();
            Assert.Contains(detail.PanCustom.GetVisualDescendants().OfType<TextBlock>(),
                text => text.Text == "刷新后的内容");
        } finally {
            Directory.Delete(dir, true);
        }
    }

    [AvaloniaFact]
    public void Help_page_hints_that_help_content_is_simplified_chinese_only() {
        EnsureExtracted();
        var originalLanguage = global::PCL.Localization.CurrentLanguage;
        try {
            global::PCL.Localization.SetLanguage("zh-CN");
            var chinesePage = new global::PCL.PageOtherHelp();
            var chineseWindow = new Window { Width = 900, Height = 700, Content = chinesePage };
            chineseWindow.Show();
            Dispatcher.UIThread.RunJobs();
            try {
                Assert.False(chinesePage.HintSCOnly.IsVisible);
            } finally {
                chineseWindow.Close();
                Dispatcher.UIThread.RunJobs();
            }

            global::PCL.Localization.SetLanguage("en-US");
            var englishPage = new global::PCL.PageOtherHelp();
            var englishWindow = new Window { Width = 900, Height = 700, Content = englishPage };
            englishWindow.Show();
            Dispatcher.UIThread.RunJobs();
            try {
                Assert.True(englishPage.HintSCOnly.IsVisible, "英文界面下应提示帮助内容仅支持简体中文");
            } finally {
                englishWindow.Close();
                Dispatcher.UIThread.RunJobs();
            }
        } finally {
            global::PCL.Localization.SetLanguage(originalLanguage);
            Dispatcher.UIThread.RunJobs();
        }
    }

    [AvaloniaFact]
    public void Help_detail_renders_with_skia() {
        EnsureExtracted();
        var entry = new global::PCL.ModHelp.HelpEntry(TopicJsonPath("个性化/XAML 格式"));
        var window = new global::PCL.FormMain();
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try {
            window.PageChange(global::PCL.FormMain.PageType.Other, global::PCL.FormMain.PageSubType.OtherHelp);
            Settle();
            var detail = new global::PCL.PageOtherHelpDetail();
            Assert.True(detail.Init(entry));
            window.PageChangeHelpDetail(entry, detail);
            Settle();
            Dispatcher.UIThread.RunJobs();

            using var frame = window.CaptureRenderedFrame();
            Assert.NotNull(frame);
            var colorCount = CountSampledColors(frame!, 10);
            // 文字、卡片背景、阴影等至少会产生若干个不同颜色；全透明或单色说明渲染管线没有真正画出帮助内容
            Assert.True(colorCount >= 8, $"帮助详情页渲染结果的颜色数过少（{colorCount}），可能没有真正渲染内容");
        } finally {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }

    private static int CountSampledColors(Avalonia.Media.Imaging.WriteableBitmap bitmap, int step) {
        var distinct = new HashSet<int>();
        using var buffer = bitmap.Lock();
        var bytesPerPixel = Equals(buffer.Format, Avalonia.Platform.PixelFormat.Bgra8888) || Equals(buffer.Format, Avalonia.Platform.PixelFormat.Rgba8888) ? 4 : 0;
        Assert.True(bytesPerPixel > 0, $"渲染帧的像素格式不支持：{buffer.Format}");
        var width = buffer.Size.Width;
        var height = buffer.Size.Height;
        for (var y = 0; y < height; y += step) {
            for (var x = 0; x < width; x += step) {
                distinct.Add(System.Runtime.InteropServices.Marshal.ReadInt32(
                    buffer.Address + y * buffer.RowBytes + x * bytesPerPixel));
            }
        }
        return distinct.Count;
    }

}
