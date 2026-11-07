using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using Newtonsoft.Json.Linq;
using Xunit;

namespace PCL.Tests;

public class Step14JavaPlatformTests {

    [Fact]
    public void Java_priority_sort_prefers_version_closest_to_21() {
        var list = new List<PCLCS.Java> {
            new("/tmp/pcl-java-8", new Version(8, 0)),
            new("/tmp/pcl-java-25", new Version(25, 0)),
            new("/tmp/pcl-java-21", new Version(21, 0)),
            new("/tmp/pcl-java-17", new Version(17, 0))
        };
        PCLCS.JavaUtils.SortByPriority(list);
        Assert.Equal(21, list[0].Version!.Major);
        Assert.Equal(8, list[3].Version!.Major);
        Assert.Equal(new[] { 17, 25 }, list.Skip(1).Take(2).Select(j => j.Version!.Major).OrderBy(x => x));
    }

    [Fact]
    public void Java_priority_sort_prefers_candidate_folders() {
        if (OperatingSystem.IsWindows()) return;
        var candidate = new PCLCS.Java("/usr/lib/jvm/pcl-test-jdk8", new Version(8, 0));
        var other = new PCLCS.Java("/tmp/pcl-test-jdk21", new Version(21, 0));
        var list = new List<PCLCS.Java> { other, candidate };
        PCLCS.JavaUtils.SortByPriority(list);
        Assert.Same(candidate, list[0]);
        Assert.Same(other, list[1]);
    }

    [Fact]
    public void Zulu_archive_extracts_and_flattens_single_folder() {
        var temp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pcl-zulu-" + Guid.NewGuid().ToString("N"));
        var source = System.IO.Path.Combine(temp, "src", "zulu-test");
        var archive = System.IO.Path.Combine(temp, "zulu-test.tar.gz");
        var target = System.IO.Path.Combine(temp, "target");
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(source, "bin"));
        System.IO.File.WriteAllText(System.IO.Path.Combine(source, "bin", "java"), "x");
        try {
            using (var file = System.IO.File.Create(archive))
            using (var gzip = new System.IO.Compression.GZipStream(file, System.IO.Compression.CompressionLevel.SmallestSize)) {
                System.Formats.Tar.TarFile.CreateFromDirectory(System.IO.Path.Combine(temp, "src"), gzip, includeBaseDirectory: false);
            }
            var extract = typeof(global::PCL.ModJava).GetMethod("ExtractJavaArchive", BindingFlags.NonPublic | BindingFlags.Static)!;
            extract.Invoke(null, new object?[] { archive, target });
            var flatten = typeof(global::PCL.ModJava).GetMethod("FlattenSingleJavaFolder", BindingFlags.NonPublic | BindingFlags.Static)!;
            flatten.Invoke(null, new object?[] { target });
            Assert.True(System.IO.File.Exists(System.IO.Path.Combine(target, "bin", "java")));
        } finally {
            try { System.IO.Directory.Delete(temp, true); } catch { }
        }
    }

    [Fact]
    public void Java_priority_can_be_reordered_manually() {
        var a = new PCLCS.Java("/tmp/pcl-java-a", new Version(21, 0));
        var b = new PCLCS.Java("/tmp/pcl-java-b", new Version(17, 0));
        var c = new PCLCS.Java("/tmp/pcl-java-c", new Version(8, 0));
        var list = new List<PCLCS.Java> { a, b, c };
        Assert.True(PCLCS.JavaUtils.MoveInList(list, c, -1));
        Assert.Equal(new[] { a, c, b }, list);
        Assert.False(PCLCS.JavaUtils.MoveInList(list, a, -1));
        Assert.False(PCLCS.JavaUtils.MoveInList(list, b, 1));
        Assert.Equal(new[] { a, c, b }, list);
    }

    [Fact]
    public void Java_launch_wrapper_is_never_used_or_config_read_on_non_windows() {
        var configReads = 0;

        var useWrapper = global::PCL.ModLaunch.ShouldUseJavaLaunchWrapper(
            false,
            false,
            () => { configReads++; return false; },
            () => { configReads++; return false; });

        Assert.False(useWrapper);
        Assert.Equal(0, configReads);
    }

    [Theory]
    [InlineData(false, false, false, true)]
    [InlineData(true, false, false, false)]
    [InlineData(false, true, false, false)]
    [InlineData(false, false, true, false)]
    public void Java_launch_wrapper_respects_windows_configuration(
        bool globalDisabled,
        bool instanceDisabled,
        bool isGbkEncoding,
        bool expected) {
        var useWrapper = global::PCL.ModLaunch.ShouldUseJavaLaunchWrapper(
            true,
            isGbkEncoding,
            () => globalDisabled,
            () => instanceDisabled);

        Assert.Equal(expected, useWrapper);
    }

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
    public void Java_path_search_uses_the_platform_path_separator() {
        var root = Path.Combine(Path.GetTempPath(), "pcl_java_path_search_" + Guid.NewGuid().ToString("N"));
        var first = Path.Combine(root, "first");
        var second = Path.Combine(root, "second");
        Directory.CreateDirectory(first);
        Directory.CreateDirectory(second);
        File.WriteAllText(Path.Combine(first, global::PCLCS.Java.ExecutableName), "");
        File.WriteAllText(Path.Combine(second, global::PCLCS.Java.ExecutableName), "");
        try {
            var path = string.Join(Path.PathSeparator, $"\"{first}\"", second);
            var candidates = global::PCLCS.JavaUtils.GetPathJavaCandidates(path);

            Assert.Equal(2, candidates.Count);
            Assert.Contains(candidates, java => global::MeloongCore.PathUtils.IsSame(java.Folder, first));
            Assert.Contains(candidates, java => global::MeloongCore.PathUtils.IsSame(java.Folder, second));
        } finally {
            Directory.Delete(root, true);
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

    [Fact]
    public void Linux_gpu_offload_environment_selects_vendor_specific_variables() {
        var root = Path.Combine(Path.GetTempPath(), "pcl_gpu_sysfs_" + Guid.NewGuid().ToString("N"));
        var marker = Path.Combine(root, "nvidia-version");
        Directory.CreateDirectory(root);
        File.WriteAllText(marker, "NVRM version: NVIDIA UNIX x86_64 Kernel Module");
        try {
            AddGpu(root, "0000:00:02.0", "0x030000", "0x8086", bootVga: true, vram: 128L * 1024 * 1024);
            AddGpu(root, "0000:01:00.0", "0x030200", "0x10de", bootVga: false, vram: 8L * 1024 * 1024 * 1024);

            var environment = global::PCL.ModLaunch.GetLinuxGpuOffloadEnvironment(root, marker);

            Assert.Equal("1", environment["__NV_PRIME_RENDER_OFFLOAD"]);
            Assert.Equal("nvidia", environment["__GLX_VENDOR_LIBRARY_NAME"]);
            Assert.Equal("NVIDIA_only", environment["__VK_LAYER_NV_optimus"]);
            Assert.DoesNotContain("DRI_PRIME", environment.Keys);
        } finally {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Linux_gpu_offload_environment_falls_back_to_dri_prime_without_nvidia_driver() {
        var root = Path.Combine(Path.GetTempPath(), "pcl_gpu_sysfs_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try {
            AddGpu(root, "0000:00:02.0", "0x030000", "0x8086", bootVga: true, vram: 128L * 1024 * 1024);
            AddGpu(root, "0000:01:00.0", "0x030200", "0x10de", bootVga: false, vram: 8L * 1024 * 1024 * 1024);

            var environment = global::PCL.ModLaunch.GetLinuxGpuOffloadEnvironment(
                root, Path.Combine(root, "missing-nvidia-version"));

            Assert.Equal("pci-0000_01_00_0", environment["DRI_PRIME"]);
            Assert.DoesNotContain("__GLX_VENDOR_LIBRARY_NAME", environment.Keys);
        } finally {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Linux_gpu_offload_environment_uses_dri_prime_pci_address_for_non_nvidia_hybrid() {
        var root = Path.Combine(Path.GetTempPath(), "pcl_gpu_sysfs_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try {
            AddGpu(root, "0000:00:02.0", "0x030000", "0x8086", bootVga: true, vram: 128L * 1024 * 1024);
            AddGpu(root, "0000:04:00.0", "0x030200", "0x1002", bootVga: false, vram: 8L * 1024 * 1024 * 1024);

            var environment = global::PCL.ModLaunch.GetLinuxGpuOffloadEnvironment(root);

            Assert.Equal("pci-0000_04_00_0", environment["DRI_PRIME"]);
        } finally {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Linux_gpu_offload_environment_ignores_integrated_gpu_even_if_it_has_more_memory_capability() {
        var root = Path.Combine(Path.GetTempPath(), "pcl_gpu_sysfs_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try {
            AddGpu(root, "0000:00:02.0", "0x030000", "0x8086", bootVga: true, vram: 16L * 1024 * 1024 * 1024);
            AddGpu(root, "0000:03:00.0", "0x030200", "0x1002", bootVga: false, vram: 2L * 1024 * 1024 * 1024);

            var environment = global::PCL.ModLaunch.GetLinuxGpuOffloadEnvironment(root);

            Assert.Equal("pci-0000_03_00_0", environment["DRI_PRIME"]);
        } finally {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Linux_gpu_offload_environment_does_not_override_when_nvidia_driver_is_the_display_gpu() {
        var root = Path.Combine(Path.GetTempPath(), "pcl_gpu_sysfs_" + Guid.NewGuid().ToString("N"));
        var marker = Path.Combine(root, "nvidia-version");
        Directory.CreateDirectory(root);
        File.WriteAllText(marker, "NVRM version: NVIDIA UNIX x86_64 Kernel Module");
        try {
            // NVIDIA 专有驱动的显卡是默认显示卡，游戏本就会使用它渲染
            AddGpu(root, "0000:00:02.0", "0x030000", "0x10de", bootVga: true, vram: 8L * 1024 * 1024 * 1024);
            AddGpu(root, "0000:01:00.0", "0x030200", "0x8086", bootVga: false, vram: 128L * 1024 * 1024);

            var environment = global::PCL.ModLaunch.GetLinuxGpuOffloadEnvironment(root, marker);

            Assert.Empty(environment);
        } finally {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Linux_gpu_offload_environment_does_not_override_single_gpu_systems() {
        var root = Path.Combine(Path.GetTempPath(), "pcl_gpu_sysfs_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try {
            AddGpu(root, "0000:00:02.0", "0x030000", "0x1002", bootVga: true);

            var environment = global::PCL.ModLaunch.GetLinuxGpuOffloadEnvironment(root);

            Assert.Empty(environment);
        } finally {
            Directory.Delete(root, true);
        }
    }

    private static void AddGpu(string root, string address, string deviceClass, string vendor, bool? bootVga = null, long vram = 0) {
        var device = Path.Combine(root, address);
        Directory.CreateDirectory(device);
        File.WriteAllText(Path.Combine(device, "class"), deviceClass);
        File.WriteAllText(Path.Combine(device, "vendor"), vendor);
        if (bootVga is not null) File.WriteAllText(Path.Combine(device, "boot_vga"), bootVga.Value ? "1" : "0");
        if (vram > 0) File.WriteAllText(Path.Combine(device, "mem_info_vram_total"), vram.ToString());
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
