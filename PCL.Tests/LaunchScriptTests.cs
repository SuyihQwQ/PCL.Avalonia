// SPDX-License-Identifier: Apache-2.0
using System;
using System.Diagnostics;
using System.IO;
using Avalonia.Headless.XUnit;
using Xunit;

namespace PCL.Tests;

/// <summary>
/// Linux/macOS 导出的 Bash 启动脚本（对应 Windows 的启动脚本 .bat）。
/// </summary>
public class LaunchScriptTests {
    private readonly Xunit.ITestOutputHelper _out;
    public LaunchScriptTests(Xunit.ITestOutputHelper o) => _out = o;

    [AvaloniaFact]
    public void Generated_bash_script_is_valid_shell() {
        var script = global::PCL.ModLaunch.BuildLaunchShellScript(
            "启动 - 测试版本",
            "/tmp/PCL Test/.minecraft/instances",
            "/usr/lib/jvm/zulu-21/bin/java",
            "-Xmx2G -Djava.library.path=/tmp/PCL Test/natives",
            "echo 全局自定义命令",
            "echo 版本自定义命令");
        _out.WriteLine(script);
        Assert.StartsWith("#!/usr/bin/env bash\n", script);
        Assert.Contains("cd '/tmp/PCL Test/.minecraft/instances' || exit 1", script);
        Assert.Contains("'/usr/lib/jvm/zulu-21/bin/java' -Xmx2G", script);
        Assert.Contains("echo 全局自定义命令", script);
        Assert.Contains("echo 版本自定义命令", script);
        Assert.Contains("echo \"游戏已退出。\"", script);
        Assert.DoesNotContain("\r", script);

        var path = Path.Combine(Path.GetTempPath(), "pcl-launch-" + Guid.NewGuid().ToString("N") + ".sh");
        try {
            File.WriteAllText(path, script);
            var psi = new ProcessStartInfo("bash") { RedirectStandardError = true, RedirectStandardOutput = true, UseShellExecute = false };
            psi.ArgumentList.Add("-n");
            psi.ArgumentList.Add(path);
            using var proc = Process.Start(psi)!;
            var err = proc.StandardError.ReadToEnd();
            proc.WaitForExit(5000);
            Assert.True(proc.ExitCode == 0, "bash -n 校验失败：" + err);
        } finally {
            try { File.Delete(path); } catch { }
        }
    }

    [AvaloniaFact]
    public void Generated_bash_script_escapes_single_quotes() {
        var script = global::PCL.ModLaunch.BuildLaunchShellScript(
            "启动 - 带'引号'的版本", "/tmp/it's here", "/usr/bin/java", "", null, null);
        Assert.Contains("cd '/tmp/it'\\''s here' || exit 1", script);
    }

    [AvaloniaFact]
    public void Generated_bash_script_includes_environment_and_wrapper() {
        var env = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, string>> {
            new("PCL_TEST_VAR", "a b'c"),
            new("INVALID-NAME", "x")
        };
        var script = global::PCL.ModLaunch.BuildLaunchShellScript(
            "启动 - 测试版本", "/tmp/mc", "/usr/lib/jvm/zulu-21/bin/java", "-Xmx2G",
            null, null, env, "mangohud --dlsym",
            "/usr/lib/jvm/zulu-21", "/tmp/.minecraft");
        _out.WriteLine(script);
        Assert.Contains("export PATH='/usr/lib/jvm/zulu-21':$PATH", script);
        Assert.Contains("export appdata='/tmp/.minecraft'", script);
        Assert.Contains("export PCL_TEST_VAR='a b'\\''c'", script);
        Assert.DoesNotContain("INVALID-NAME", script);
        Assert.Contains("mangohud --dlsym '/usr/lib/jvm/zulu-21/bin/java' -Xmx2G", script);

        var path = Path.Combine(Path.GetTempPath(), "pcl-launch-" + Guid.NewGuid().ToString("N") + ".sh");
        try {
            File.WriteAllText(path, script);
            var psi = new ProcessStartInfo("bash") { RedirectStandardError = true, UseShellExecute = false };
            psi.ArgumentList.Add("-n");
            psi.ArgumentList.Add(path);
            using var proc = Process.Start(psi)!;
            var err = proc.StandardError.ReadToEnd();
            proc.WaitForExit(5000);
            Assert.True(proc.ExitCode == 0, "bash -n 校验失败：" + err);
        } finally {
            try { File.Delete(path); } catch { }
        }
    }

    [AvaloniaFact]
    public void Environment_variables_and_wrapper_are_read_from_settings() {
        var oldEnvironment = global::PCL.Settings.Get<string>("LaunchEnvironment");
        var oldWrapper = global::PCL.Settings.Get<string>("LaunchAdvanceWrapper");
        var oldInstance = global::PCL.ModMinecraft.McInstanceSelected;
        try {
            global::PCL.ModMinecraft.McInstanceSelected = null;
            global::PCL.Settings.Set("LaunchEnvironment", "PCL_TEST_A=1;PCL_TEST_B=two words");
            var vars = global::PCL.ModLaunch.GetLaunchScriptEnvironment();
            var dict = vars.ToDictionary(p => p.Key, p => p.Value);
            Assert.Equal("1", dict["PCL_TEST_A"]);
            Assert.Equal("two words", dict["PCL_TEST_B"]);

            global::PCL.Settings.Set("LaunchAdvanceWrapper", " mangohud --dlsym ");
            Assert.Equal("mangohud --dlsym", global::PCL.ModLaunch.GetLaunchScriptWrapper());
        } finally {
            global::PCL.Settings.Set("LaunchEnvironment", oldEnvironment);
            global::PCL.Settings.Set("LaunchAdvanceWrapper", oldWrapper);
            global::PCL.ModMinecraft.McInstanceSelected = oldInstance;
        }
    }

    [AvaloniaFact]
    public void Latest_launch_script_extension_matches_platform() {
        var oldPathData = global::PCL.ModBase.PathData;
        var dir = Path.Combine(Path.GetTempPath(), "pcl-launch-dir-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try {
            global::PCL.ModBase.PathData = dir + Path.DirectorySeparatorChar;
            var expected = OperatingSystem.IsWindows() ? "LatestLaunch.bat" : "LatestLaunch.sh";
            Assert.Equal(Path.Combine(dir, expected), global::PCL.ModLaunch.LatestLaunchScriptPath);
        } finally {
            global::PCL.ModBase.PathData = oldPathData;
            try { Directory.Delete(dir, true); } catch { }
        }
    }
}
