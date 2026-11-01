using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using Newtonsoft.Json.Linq;
using Xunit;

namespace PCL.Tests;

public class Step14JavaPlatformTests {

    [Fact]
    public void Java_executable_name_matches_platform() {
        Assert.Equal(OperatingSystem.IsWindows() ? "java.exe" : "java", global::PCLCS.Java.ExecutableName);
    }

    [Fact]
    public void Java_exe_path_uses_platform_executable_name() {
        var folder = Path.Combine(Path.GetTempPath(), "pcl_java_path_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try {
            var java = new global::PCLCS.Java(folder);
            Assert.Equal(global::PCLCS.Java.ExecutableName, Path.GetFileName(java.JavaExePath));
            Assert.EndsWith(global::PCLCS.Java.ExecutableName, java.JavaExePath, StringComparison.Ordinal);
        } finally {
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public async Task Java_check_and_search_find_extensionless_executable() {
        if (OperatingSystem.IsWindows()) return; // Unix 下 Java 可执行文件无扩展名

        var root = Path.Combine(Path.GetTempPath(), "pcl_java_search_" + Guid.NewGuid().ToString("N"));
        var bin = Path.Combine(root, "jdk-21", "bin");
        Directory.CreateDirectory(bin);
        var exe = Path.Combine(bin, "java");
        await File.WriteAllTextAsync(exe, """
            #!/bin/sh
            cat <<'EOF'
            openjdk version "21.0.1" 2023-10-17
            OpenJDK Runtime Environment (build 21.0.1+12)
            OpenJDK 64-Bit Server VM (build 21.0.1+12, mixed mode)
                java.version = 21.0.1
                file.encoding = UTF-8
                native.encoding = UTF-8
            EOF
            """, TestContext.Current.CancellationToken);
        File.SetUnixFileMode(exe, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        try {
            var java = new global::PCLCS.Java(bin);
            Assert.True(await java.CheckAsync(TestContext.Current.CancellationToken), $"Java 检查失败：{java}");
            Assert.Equal(21, java.Version!.Major);

            var found = await global::PCLCS.JavaUtils.SearchFoldersAsync(true, [root], TestContext.Current.CancellationToken);
            Assert.Contains(found, j => global::MeloongCore.PathUtils.IsSame(j.Folder, bin));
        } finally {
            Directory.Delete(root, true);
        }
    }
}

/// <summary>
/// 版本 JSON 中的支持库规则与 Natives 必须按当前平台筛选，否则 Linux 会误用 Windows 原生库。
/// </summary>
public class McPlatformLibraryTests {

    private static string CurrentOsName =>
        OperatingSystem.IsWindows() ? "windows" : OperatingSystem.IsMacOS() ? "osx" : "linux";

    private static JToken OsRule(string os, string? arch = null) {
        var osObject = new JObject { ["name"] = os };
        if (arch is not null) osObject["arch"] = arch;
        return new JArray(new JObject { ["action"] = "allow", ["os"] = osObject });
    }

    [AvaloniaFact]
    public void Rule_check_matches_current_platform() {
        var other = CurrentOsName == "windows" ? "linux" : "windows";
        Assert.True(global::PCL.ModMinecraft.McJsonRuleCheck(OsRule(CurrentOsName)));
        Assert.False(global::PCL.ModMinecraft.McJsonRuleCheck(OsRule(other)));

        var arch = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture;
        var archName = arch switch {
            System.Runtime.InteropServices.Architecture.X64 => "x86_64",
            System.Runtime.InteropServices.Architecture.Arm64 => "arm64",
            System.Runtime.InteropServices.Architecture.Arm => "arm32",
            _ => "x86"
        };
        var otherArch = archName == "x86_64" ? "x86" : "x86_64";
        Assert.True(global::PCL.ModMinecraft.McJsonRuleCheck(OsRule(CurrentOsName, archName)));
        Assert.False(global::PCL.ModMinecraft.McJsonRuleCheck(OsRule(CurrentOsName, otherArch)));
    }

    [AvaloniaFact]
    public void Library_list_selects_current_platform_natives() {
        var temp = Path.Combine(Path.GetTempPath(), "PCL-LibTest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try {
            var json = JObject.Parse("""
                {
                  "libraries": [
                    { "name": "org.lwjgl:lwjgl:3.3.3:natives-windows", "rules": [ { "action": "allow", "os": { "name": "windows" } } ] },
                    { "name": "org.lwjgl:lwjgl:3.3.3:natives-linux", "rules": [ { "action": "allow", "os": { "name": "linux" } } ] },
                    { "name": "org.lwjgl:lwjgl:3.3.3:natives-macos", "rules": [ { "action": "allow", "os": { "name": "osx" } } ] },
                    { "name": "com.google.code.gson:gson:2.10" }
                  ]
                }
                """);
            var libs = global::PCL.ModMinecraft.McLibListGetWithJson(json, false, temp + Path.DirectorySeparatorChar);
            var names = libs.Select(l => l.OriginalName).Where(n => n is not null).ToList();
            var expected = CurrentOsName switch {
                "windows" => "natives-windows",
                "osx" => "natives-macos",
                _ => "natives-linux"
            };
            Assert.Contains(names, n => n!.EndsWith(expected, StringComparison.Ordinal));
            Assert.DoesNotContain(names, n => n!.Contains(":natives-") && !n.EndsWith(expected, StringComparison.Ordinal));
            Assert.Contains("com.google.code.gson:gson:2.10", names);
        } finally {
            try { Directory.Delete(temp, true); } catch { /* 忽略清理失败 */ }
        }
    }
}
