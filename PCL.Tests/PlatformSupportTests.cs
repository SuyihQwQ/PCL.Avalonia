// SPDX-License-Identifier: Apache-2.0
using Xunit;

namespace PCL.Tests;

public class PlatformSupportTests {
    [Fact]
    public void Platform_detection_matches_current_host() {
        Assert.True(global::PCL.ModPlatform.IsLinux);
        Assert.False(global::PCL.ModPlatform.IsWindows);
        Assert.False(global::PCL.ModPlatform.IsMacOS);
        Assert.False(global::PCL.ModPlatform.IsBSD);
        Assert.Equal("linux", global::PCL.ModPlatform.PlatformName);
        Assert.Equal("linux", global::PCL.ModPlatform.MinecraftOsName);
        Assert.True(global::PCL.ModPlatform.IsUnixLike);
    }

    [Fact]
    public void Platform_capabilities_disable_windows_only_features() {
        // Windows 专属功能必须在 Linux / macOS / BSD 上禁用
        Assert.False(global::PCL.ModPlatform.SupportsRamOptimize);
        Assert.False(global::PCL.ModPlatform.SupportsJavaLaunchWrapper);
        // Linux 通过 PRIME 支持显卡首选项；macOS / BSD 不支持
        Assert.True(global::PCL.ModPlatform.SupportsGpuPreference);
        Assert.True(global::PCL.ModPlatform.CanCustomizeTheme);
    }

    [Fact]
    public void Platform_config_home_is_absolute() {
        var home = global::PCL.ModPlatform.UserConfigHome;
        Assert.False(string.IsNullOrWhiteSpace(home));
        Assert.True(System.IO.Path.IsPathRooted(home));
    }

    [Fact]
    public void Minecraft_os_name_uses_linux_for_bsd() {
        // Minecraft 只认识 windows / osx / linux，BSD 必须复用 linux
        Assert.Contains(global::PCL.ModPlatform.MinecraftOsName, new[] { "windows", "osx", "linux" });
    }
}
