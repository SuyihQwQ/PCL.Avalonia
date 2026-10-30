using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using MeloongCore.Extensions;

namespace MeloongCore.Compat;

/// <summary>
/// 文件与文件夹选择弹窗。保持原 MeloongCore.Wpf.Dialogs 的同步语义，内部用 <see cref="ModalityHost"/> 泵消息等待。
/// </summary>
public static class Dialogs {

    /// <summary>
    /// 手动指定弹窗所属的窗口；为 <c>null</c> 时自动取当前活动窗口。
    /// </summary>
    public static TopLevel? Owner { get; set; }

    private static TopLevel? ResolveOwner() {
        if (Owner is { } manual) return manual;
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return null;
        foreach (var window in desktop.Windows) {
            if (window.IsActive) return window;
        }
        return desktop.MainWindow ?? desktop.Windows.FirstOrDefault();
    }

    /// <summary>
    /// 弹出 “保存” 弹窗，返回用户指定的路径，若取消则返回 <c>null</c>。
    /// <para/> <paramref name="filter"/> 中的扩展名不加 <c>.</c>。
    /// </summary>
    public static string? SaveFile(string title, string defaultFileName = "", string? defaultDirectory = null, IEnumerable<(string Extension, string Display)>? filter = null) {
        var owner = ResolveOwner();
        if (owner is null) {
            Logger.Warn("保存弹窗失败：当前没有可用窗口。");
            return null;
        }

        var filters = filter?.ToList();
        var file = ModalityHost.RunSync(() => SaveFileAsync(owner, title, defaultFileName, defaultDirectory, filters), int.MaxValue);
        if (file is null) return null;

        var result = file.TryGetLocalPath();
        Logger.Info($"保存弹窗返回：{result}");
        if (string.IsNullOrEmpty(result) || !result.Contains(Path.DirectorySeparatorChar)) return null;

        // AddExtension 在部分平台可能失效，需要手动补全
        if (filters is { Count: > 0 } && !filters.Any(f => result.EndsWithF("." + f.Extension, true))) {
            result += "." + filters[0].Extension;
            Logger.Warn($"选择文件的返回无扩展名，将会手动添加，修改后为：{result}");
        }
        return result;
    }

    /// <summary>
    /// 弹出 “选择文件” 弹窗，返回用户选择的文件路径，若取消则返回空数组。
    /// <para/> 若使用扩展名 <paramref name="filter"/>，则扩展名不加 <c>.</c>。
    /// </summary>
    public static List<string> SelectFile(string title, bool multiselect, string? defaultDirectory = null, OneOf<IEnumerable<(string[] Extensions, string Display)>, string>? filter = null) {
        var owner = ResolveOwner();
        if (owner is null) {
            Logger.Warn("选择文件弹窗失败：当前没有可用窗口。");
            return [];
        }

        var types = BuildFileTypes(filter);
        var files = ModalityHost.RunSync(() => OpenFilesAsync(owner, title, multiselect, defaultDirectory, types), int.MaxValue);
        var results = files
            .Select(f => f.TryGetLocalPath())
            .Where(f => !string.IsNullOrEmpty(f) && f!.Contains(Path.DirectorySeparatorChar))
            .Select(f => f!)
            .ToList();
        Logger.Info($"选择文件弹窗返回：{string.Join(",", results)}");
        return results;
    }

    /// <summary>
    /// 弹出 “选择文件夹” 弹窗，返回用户选择的文件夹路径（以分隔符结尾），若取消则返回空数组。
    /// </summary>
    public static List<string> SelectFolder(string title, bool multiselect, string? defaultDirectory = null) {
        var owner = ResolveOwner();
        if (owner is null) {
            Logger.Warn("选择文件夹弹窗失败：当前没有可用窗口。");
            return [];
        }

        var folders = ModalityHost.RunSync(() => OpenFoldersAsync(owner, title, multiselect, defaultDirectory), int.MaxValue);
        var results = folders
            .Select(f => f.TryGetLocalPath())
            .Where(f => !string.IsNullOrEmpty(f) && f!.Contains(Path.DirectorySeparatorChar))
            .Select(f => PathUtils.AddSlashSuffix(f!))
            .ToList();
        Logger.Info($"选择文件夹弹窗返回：{string.Join(",", results)}");
        return results;
    }

    #region 内部实现

    private static async Task<IStorageFile?> SaveFileAsync(TopLevel owner, string title, string defaultFileName, string? defaultDirectory, List<(string Extension, string Display)>? filter) {
        var options = new FilePickerSaveOptions {
            Title = title,
            SuggestedFileName = string.IsNullOrWhiteSpace(defaultFileName) ? null : defaultFileName,
            DefaultExtension = filter is { Count: > 0 } ? filter[0].Extension : null,
            ShowOverwritePrompt = true,
            FileTypeChoices = filter is { Count: > 0 }
                ? filter.Select(f => new FilePickerFileType(f.Display) { Patterns = [$"*.{f.Extension}"] }).ToList()
                : null,
            SuggestedStartLocation = await GetStartFolderAsync(owner, defaultDirectory)
        };
        var filterText = filter is null
            ? ""
            : string.Join("|", filter.Select(f => f.Display + "(*." + f.Extension + ")|*." + f.Extension));
        Logger.Info($"保存弹窗：{title}（FileName={options.SuggestedFileName}, Filter={filterText}）");
        return await owner.StorageProvider.SaveFilePickerAsync(options);
    }

    private static async Task<IReadOnlyList<IStorageFile>> OpenFilesAsync(TopLevel owner, string title, bool multiselect, string? defaultDirectory, List<FilePickerFileType>? types) {
        var options = new FilePickerOpenOptions {
            Title = title,
            AllowMultiple = multiselect,
            FileTypeFilter = types is { Count: > 0 } ? types : null,
            SuggestedStartLocation = await GetStartFolderAsync(owner, defaultDirectory)
        };
        var filterText = types is null
            ? ""
            : string.Join("|", types.Select(t => t.Name + "(" + string.Join(";", t.Patterns ?? []) + ")"));
        Logger.Info($"选择文件弹窗：{title}（Multiselect={multiselect}, Filter={filterText}）");
        return await owner.StorageProvider.OpenFilePickerAsync(options) ?? [];
    }

    private static async Task<IReadOnlyList<IStorageFolder>> OpenFoldersAsync(TopLevel owner, string title, bool multiselect, string? defaultDirectory) {
        var options = new FolderPickerOpenOptions {
            Title = title,
            AllowMultiple = multiselect,
            SuggestedStartLocation = await GetStartFolderAsync(owner, defaultDirectory)
        };
        Logger.Info($"选择文件夹弹窗：{title}（Multiselect={multiselect}）");
        return await owner.StorageProvider.OpenFolderPickerAsync(options) ?? [];
    }

    private static async Task<IStorageFolder?> GetStartFolderAsync(TopLevel owner, string? defaultDirectory) {
        if (string.IsNullOrEmpty(defaultDirectory) || !DirectoryUtils.Exists(defaultDirectory)) return null;
        try {
            return await owner.StorageProvider.TryGetFolderFromPathAsync(PathUtils.ToShortPath(defaultDirectory));
        } catch (Exception ex) {
            Logger.Warn(ex, $"设置弹窗初始目录失败：{defaultDirectory}");
            return null;
        }
    }

    private static List<FilePickerFileType>? BuildFileTypes(OneOf<IEnumerable<(string[] Extensions, string Display)>, string>? filter) {
        if (filter is not { } value) return null;
        if (value.Is<string>()) return ParseFilterString(value.As<string>());
        return value.As<IEnumerable<(string[] Extensions, string Display)>>()
            .Select(f => new FilePickerFileType(f.Display) {
                Patterns = f.Extensions.Select(e => $"*.{e}").ToList()
            })
            .ToList();
    }

    /// <summary>
    /// 解析 Win32 风格的过滤器字符串，如 <c>"java.exe|java.exe"</c> 或 <c>"图片|*.png;*.jpg"</c>。
    /// </summary>
    private static List<FilePickerFileType>? ParseFilterString(string filter) {
        var parts = filter.Split('|');
        if (parts.Length < 2) return null;

        var types = new List<FilePickerFileType>();
        for (var i = 0; i + 1 < parts.Length; i += 2) {
            var patterns = parts[i + 1]
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(p => p.Contains('*') || p.Contains('?') ? p : $"*{p}")
                .ToList();
            if (patterns.Count == 0) continue;
            types.Add(new FilePickerFileType(parts[i]) { Patterns = patterns });
        }
        return types.Count == 0 ? null : types;
    }

    #endregion

}
