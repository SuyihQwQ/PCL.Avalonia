// SPDX-License-Identifier: Apache-2.0
using System;
using System.Net;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Headless.XUnit;
using Xunit;

namespace PCL.Tests;

/// <summary>
/// 内嵌字体与网络代理设置的回归测试。
/// </summary>
public class FontAndProxyTests {
    private static void WithSetting<T>(string key, T value, Action action) {
        var original = global::PCL.Settings.Get<T>(key);
        try {
            global::PCL.Settings.Set(key, value);
            action();
        } finally {
            global::PCL.Settings.Set(key, original);
        }
    }

    [AvaloniaFact]
    public void Embedded_font_assets_are_packaged() {
        var fonts = new[] {
            "avares://PCL.Avalonia/Assets/Fonts/PCLEnglish.ttf",
            "avares://PCL.Avalonia/Assets/Fonts/MiSans-Regular.ttf",
            "avares://PCL.Avalonia/Assets/Fonts/Twemoji.Mozilla.ttf"
        };
        foreach (var uri in fonts) {
            using var stream = AssetLoader.Open(new Uri(uri));
            Assert.True(stream.Length > 10_000, $"{uri} 大小异常：{stream.Length}");
        }
    }

    [AvaloniaFact]
    public void Embedded_font_families_resolve() {
        var fonts = new (string Uri, string Family)[] {
            ("avares://PCL.Avalonia/Assets/Fonts/PCLEnglish.ttf#PCL English", "PCL English"),
            ("avares://PCL.Avalonia/Assets/Fonts/MiSans-Regular.ttf#MiSans", "MiSans"),
            ("avares://PCL.Avalonia/Assets/Fonts/Twemoji.Mozilla.ttf#Twemoji Mozilla", "Twemoji Mozilla")
        };
        foreach (var (uri, family) in fonts) {
            var ok = FontManager.Current.TryGetGlyphTypeface(new Typeface(new FontFamily(uri)), out var glyph);
            Assert.True(ok, $"{family} 字体未能解析");
            Assert.Equal(family, glyph!.FamilyName);
        }
    }

    [AvaloniaFact]
    public void Font_family_follows_system_font_setting() {
        WithSetting("UiSystemFont", false, () => {
            Assert.Contains("MiSans", global::PCL.FontConfig.ActiveFamily());
            Assert.Contains("Twemoji", global::PCL.FontConfig.ActiveFamily());
        });
        WithSetting("UiSystemFont", true, () => {
            var family = global::PCL.FontConfig.ActiveFamily();
            Assert.DoesNotContain("avares://", family);
            Assert.Equal(global::PCL.FontConfig.GetSystemFamily(), family);
        });
    }

    [AvaloniaFact]
    public void Runtime_switch_updates_application_font_resource() {
        WithSetting("UiSystemFont", true, () => {
            global::PCL.FontConfig.ApplyRuntime();
            var resource = Avalonia.Application.Current!.Resources["PCLFont"];
            var family = Assert.IsType<FontFamily>(resource);
            Assert.Contains(global::PCL.FontConfig.GetSystemFamily().Split(',')[0], family.ToString());
        });
        WithSetting("UiSystemFont", false, () => {
            global::PCL.FontConfig.ApplyRuntime();
            var family = Assert.IsType<FontFamily>(Avalonia.Application.Current!.Resources["PCLFont"]);
            Assert.Contains("MiSans", family.ToString());
        });
    }

    [AvaloniaFact]
    public void Proxy_handler_respects_mode_and_categories() {
        WithSetting("NetProxyType", 1, () => {
            var handler = global::PCL.ModNet.CreateHttpHandler(global::PCL.ModNet.NetRequestCategory.Download);
            var sockets = Assert.IsType<System.Net.Http.SocketsHttpHandler>(handler);
            Assert.False(sockets.UseProxy, "“不使用代理”时不应使用代理");
        });
        WithSetting("NetProxyType", 3, () => {
            WithSetting("NetProxyAddress", "127.0.0.1", () => {
                WithSetting("NetProxyPort", 7890, () => {
                    var handler = Assert.IsType<System.Net.Http.SocketsHttpHandler>(
                        global::PCL.ModNet.CreateHttpHandler(global::PCL.ModNet.NetRequestCategory.Homepage));
                    Assert.True(handler.UseProxy);
                    var proxy = Assert.IsType<WebProxy>(handler.Proxy);
                    Assert.Equal("http://127.0.0.1:7890/", proxy.Address!.ToString());
                });
            });
        });
        WithSetting("NetProxyType", 0, () => {
            WithSetting("NetProxyDownload", false, () => {
                var handler = Assert.IsType<System.Net.Http.SocketsHttpHandler>(global::PCL.ModNet.CreateHttpHandler(global::PCL.ModNet.NetRequestCategory.Download));
                Assert.False(handler.UseProxy, "分类未勾选时不应使用代理");
            });
            WithSetting("NetProxyHomepage", true, () => {
                var handler = Assert.IsType<System.Net.Http.SocketsHttpHandler>(global::PCL.ModNet.CreateHttpHandler(global::PCL.ModNet.NetRequestCategory.Homepage));
                Assert.True(handler.UseProxy, "系统代理模式应允许使用系统代理");
            });
        });
    }

    [AvaloniaFact]
    public void Proxy_clients_are_rebuilt_after_reset() {
        var first = global::PCL.ModNet.GetSharedHttpClient("unit-test", global::PCL.ModNet.NetRequestCategory.Other, 5);
        var second = global::PCL.ModNet.GetSharedHttpClient("unit-test", global::PCL.ModNet.NetRequestCategory.Other, 5);
        Assert.Same(first, second);
        global::PCL.ModNet.ResetProxyClients();
        var third = global::PCL.ModNet.GetSharedHttpClient("unit-test", global::PCL.ModNet.NetRequestCategory.Other, 5);
        Assert.NotSame(first, third);
    }
}
