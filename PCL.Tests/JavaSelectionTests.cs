// SPDX-License-Identifier: Apache-2.0
using System;
using System.IO;
using System.Linq;
using Avalonia.Headless.XUnit;
using MeloongCore;
using Newtonsoft.Json;
using PCLCS;
using Xunit;

namespace PCL.Tests;

/// <summary>
/// Java 配置测试会临时改写全局 Java 列表与单实例配置，禁止与其它测试并行，避免相互踩踏。
/// </summary>
[CollectionDefinition("JavaConfig", DisableParallelization = true)]
public class JavaConfigCollection { }

/// <summary>
/// “使用指定的 Java”在实例页面与启动流程之间的加载回归：
/// 1) 同一版本目录的配置提供器必须共享缓存，避免页面刚写入、启动时用新对象读不到；
/// 2) Java 列表尚未加载完成时，读取已保存的指定 Java 不能被误清空。
/// </summary>
[Collection("JavaConfig")]
public class JavaSelectionTests {
    private readonly Xunit.ITestOutputHelper _out;
    public JavaSelectionTests(Xunit.ITestOutputHelper o) => _out = o;

    private static string FindJavaBin() {
        foreach (var root in new[] {"/usr/lib/jvm", "/usr/java", "/opt/java"}) {
            if (!Directory.Exists(root)) continue;
            foreach (var dir in Directory.GetDirectories(root).OrderByDescending(d => d)) {
                var bin = Path.Combine(dir, "bin");
                if (File.Exists(Path.Combine(bin, "java"))) return bin + "/";
            }
        }
        return null;
    }

    [AvaloniaFact]
    public void Shared_config_provider_sees_value_written_by_page_object() {
        var javaBin = FindJavaBin();
        if (javaBin is null) return;
        var root = Path.Combine(Path.GetTempPath(), "pcl-javasel-" + Guid.NewGuid().ToString("N"));
        try {
            var versionDir = Path.Combine(root, "versions", "inst");
            Directory.CreateDirectory(Path.Combine(versionDir, "PCL"));
            File.WriteAllText(Path.Combine(versionDir, "inst.json"),
                "{\"id\":\"inst\",\"mainClass\":\"net.minecraft.client.main.Main\"}");
            File.WriteAllText(Path.Combine(versionDir, "PCL", "Setup.ini"), "VersionArgumentJavaV2:3\r\n");

            var pageInstance = new global::PCL.ModMinecraft.McInstance(versionDir);
            var launchInstance = new global::PCL.ModMinecraft.McInstance(versionDir);
            Assert.Same(pageInstance.Config, launchInstance.Config);

            var java = new Java(javaBin, new Version(21, 0, 0, 0));
            var originalList = Configs.JavaList.Get();
            try {
                Configs.JavaList.Set(new ConcurrentList<Java> { java });
                // 页面里选择“使用指定的 Java”（不等待异步落盘）
                Configs.JavaForced.Set(java, pageInstance.Config);
                // 启动流程用另一个对象读取
                var loaded = Configs.JavaForced.Get(launchInstance.Config);
                Assert.NotNull(loaded);
                Assert.Equal(java.Folder, loaded!.Folder);
                // 启动流程使用的选择逻辑读取同一提供器，能拿到页面刚写入的 Java
                Assert.True(Configs.JavaList.Get().Contains(loaded));
                Assert.Equal(3, Settings.Get<int>("VersionArgumentJavaV2", launchInstance));
            } finally {
                Configs.JavaList.Set(originalList);
                Configs.JavaForced.Reset(pageInstance.Config);
            }
        } finally {
            try { Directory.Delete(root, true); } catch { }
        }
    }

    [AvaloniaFact]
    public void Saved_forced_java_survives_empty_java_list() {
        var originalList = Configs.JavaList.Get();
        var root = Path.Combine(Path.GetTempPath(), "pcl-javasel-" + Guid.NewGuid().ToString("N"));
        try {
            Directory.CreateDirectory(root);
            var file = Path.Combine(root, "config.json");
            var provider = JsonConfigProvider.GetShared(file);
            var java = new Java("/tmp/example-java/bin/", new Version(17, 0, 0, 0));
            try {
                // 模拟启动早期：Java 列表还是空的
                Configs.JavaList.Set(new ConcurrentList<Java>());
                Configs.JavaForced.Set(java, provider);
                var stillThere = Configs.JavaForced.Get(provider);
                Assert.NotNull(stillThere);
                Assert.Equal(java.Folder, stillThere!.Folder);

                // 列表加载完成后确实不包含该 Java，才允许清空
                Configs.JavaList.Set(new ConcurrentList<Java> { new("/tmp/other-java/bin/", new Version(21, 0, 0, 0)) });
                Assert.Null(Configs.JavaForced.Get(provider));
            } finally {
                Configs.JavaList.Set(originalList);
                Configs.JavaForced.Reset(provider);
            }
        } finally {
            try { Directory.Delete(root, true); } catch { }
        }
    }
}
