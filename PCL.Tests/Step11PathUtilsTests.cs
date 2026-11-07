// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics;
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

/// <summary>
/// 启动游戏时 PCL 传入的路径仍带 Windows 分隔符，非 Windows 平台必须先转换，否则工作目录会被当成字面量路径。
/// </summary>
public class StartProcessTests {

    [Fact]
    public void StartProcess_normalizes_windows_style_paths_for_other_platforms() {
        if (OperatingSystem.IsWindows()) return;
        var temp = Path.Combine(Path.GetTempPath(), "PCL-StartProcess-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try {
            var info = new ProcessStartInfo {
                FileName = "/bin\\sh",
                Arguments = "-c pwd",
                WorkingDirectory = temp.Replace('/', '\\') + "\\",
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            using var process = global::PCL.ModBase.StartProcess(info);
            Assert.NotNull(process);
            var output = process!.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(5000);
            Assert.Equal(Path.GetFullPath(temp), Path.GetFullPath(output));
        } finally {
            try { Directory.Delete(temp, true); } catch { /* 忽略清理失败 */ }
        }
    }
}
