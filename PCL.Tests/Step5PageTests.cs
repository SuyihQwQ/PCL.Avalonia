using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
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
            Assert.NotNull(page.BtnAboutUpdate);
            Assert.NotNull(page.BtnDonateOutput);
            Assert.NotNull(page.BtnDonateDonate);
            Assert.NotNull(page.BtnDonateCopy);
            Assert.NotNull(page.BtnDonateInput);

            Assert.Contains("2.14.0.0", page.ItemAboutPcl.Info);
            Assert.False(page.BtnDonateDonate.IsVisible);
            Assert.True(page.BtnDonateOutput.IsVisible);

            global::PCL.ModAnimation.AniTimer(800);
            Assert.True(page.PanBack.Bounds.Width > 0 && page.PanBack.Bounds.Height > 0);
            Assert.True(page.PanMain.Children.Count >= 5);
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
        try {
            var page = new global::PCL.PageDownloadModify();
            Assert.NotNull(page.PanSelect);
            Assert.NotNull(page.BtnStart);
            Assert.NotNull(page.CardMinecraft);
        } finally {
            global::PCL.PageInstanceLeft.Instance = original;
        }
    }
}

