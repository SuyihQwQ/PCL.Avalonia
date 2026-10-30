using System.IO;
using MeloongCore;
using Xunit;

namespace PCL.Tests;

/// <summary>
/// PathUtils 的跨平台分隔符处理测试。
/// PCL 的路径字符串按 Windows 习惯使用 \，即使在非 Windows 平台上也要按分隔符处理。
/// </summary>
public class Step11PathUtilsTests {

    [Fact]
    public void RemoveSlashSuffix_trims_both_separators() {
        Assert.Equal(@"versions\Foo", PathUtils.RemoveSlashSuffix(@"versions\Foo\"));
        Assert.Equal("versions/Foo", PathUtils.RemoveSlashSuffix("versions/Foo/"));
        Assert.Equal("versions/Foo", PathUtils.RemoveSlashSuffix("versions/Foo"));
    }

    [Fact]
    public void GetLastPart_handles_windows_style_trailing_separator() {
        Assert.Equal("Fabulously Optimized", PathUtils.GetLastPart(@"versions\Fabulously Optimized\"));
        Assert.Equal("Fabulously Optimized", PathUtils.GetLastPart("versions/Fabulously Optimized/"));
        Assert.Equal("Fabulously Optimized", PathUtils.GetLastPart("/home/user/.minecraft/versions/Fabulously Optimized/"));
    }

    [Fact]
    public void AddSlashSuffix_recognizes_backslash_suffix() {
        Assert.Equal(@"versions\Foo\", PathUtils.AddSlashSuffix(@"versions\Foo\"));
        Assert.Equal("versions/Foo/", PathUtils.AddSlashSuffix("versions/Foo/"));
        Assert.Equal(@"versions\Foo" + Path.DirectorySeparatorChar, PathUtils.AddSlashSuffix(@"versions\Foo"));
    }
}
