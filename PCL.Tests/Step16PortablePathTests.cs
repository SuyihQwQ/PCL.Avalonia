// SPDX-License-Identifier: Apache-2.0
using System.Reflection;
using Xunit;

namespace PCL.Tests;

public class PortablePathTests {

    [Fact]
    public void Data_folder_is_next_to_writable_program_folder() {
        var expected = global::PCL.ModBase.GetWritableBasePath() + "PCL.Avalonia" + System.IO.Path.DirectorySeparatorChar;
        Assert.Equal(expected, global::PCL.ModBase.GetDefaultPathData());
    }

    [Fact]
    public void Default_minecraft_folder_is_under_writable_program_folder() {
        var expected = System.IO.Path.Combine(
            global::PCL.ModBase.GetWritableBasePath(), ".minecraft") + System.IO.Path.DirectorySeparatorChar;
        Assert.Equal(expected, global::PCL.ModMinecraft.McDefaultFolder);
    }

    [Fact]
    public void Linux_registry_values_are_stored_in_user_config_folder() {
        if (OperatingSystem.IsWindows()) return;
        var root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrWhiteSpace(root)) {
            root = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
        }
        var expectedFolder = System.IO.Path.Combine(root, "PCL.Avalonia") + System.IO.Path.DirectorySeparatorChar;
        Assert.Equal(expectedFolder, global::PCL.ModBase.PathConfigFolder);
        Assert.Equal(System.IO.Path.Combine(expectedFolder, "Registry.ini"), global::PCL.ModBase.PathRegistryIni);
    }

    [Fact]
    public void Empty_folder_selection_targets_minecraft_and_writes_pcl_ini_there() {
        var temp = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), "PCL-Select-" + Guid.NewGuid().ToString("N"));
        var cacheField = typeof(global::PCL.ModMinecraft).GetField(
            "_McFolderSelected", BindingFlags.Static | BindingFlags.NonPublic)!;
        var originalCache = cacheField.GetValue(null);
        var originalOverride = global::PCL.ModBase.WorkingDirectoryOverride;
        var originalSetting = global::PCL.Settings.Get<string>("LaunchFolderSelect");
        try {
            global::PCL.ModBase.WorkingDirectoryOverride = temp + System.IO.Path.DirectorySeparatorChar;
            global::PCL.ModMinecraft.McFolderSelected = "";
            var folder = global::PCL.ModMinecraft.McFolderSelected;
            Assert.Equal(global::PCL.ModMinecraft.McDefaultFolder, folder);
            Assert.EndsWith(".minecraft" + System.IO.Path.DirectorySeparatorChar, folder);

            Directory.CreateDirectory(folder);
            global::PCL.ModBase.WriteIni(folder + "PCL.ini", "Version", "TestVersion");
            var iniFile = System.IO.Path.Combine(folder, "PCL.ini");
            Assert.True(File.Exists(iniFile), $"PCL.ini 未写入 .minecraft：{iniFile}");
            Assert.Contains("Version:TestVersion", File.ReadAllText(iniFile));
            global::PCL.ModBase.IniClearCache(iniFile);
        } finally {
            cacheField.SetValue(null, originalCache);
            global::PCL.ModBase.WorkingDirectoryOverride = originalOverride;
            global::PCL.Settings.Set("LaunchFolderSelect", originalSetting);
            if (Directory.Exists(temp)) Directory.Delete(temp, true);
        }
    }
}
