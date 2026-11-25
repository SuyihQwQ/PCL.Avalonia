// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using System.Text.RegularExpressions;
using Avalonia.Headless.XUnit;
using Xunit;

namespace PCL.Tests;

/// <summary>
/// 语言资源覆盖率测试：保证 zh-CN.json / en-US.json 合法、成对、英文侧不残留中文，
/// 并保证 XAML 与 VB 中静态引用的 Lang* 键都能在 JSON 中找到。
/// </summary>
public class LocalizationCoverageTests {

    private static readonly Regex HanPattern = new(@"[\u4e00-\u9fff]", RegexOptions.Compiled);
    private static readonly Regex DynamicResourcePattern =
        new(@"\{\s*(?:Dynamic|Static)Resource\s+(Lang[A-Za-z0-9_]+)\s*\}", RegexOptions.Compiled);
    private static readonly Regex QuotedLangPattern = new(@"""(Lang[A-Za-z0-9_]+)""", RegexOptions.Compiled);

    /// <summary>
    /// Localization.Initialize 会在运行时把 overrides.json 的条目展开成 LangAvalonia0000、LangAvalonia0001…
    /// 这些键由代码动态拼接（见 Localization.vb 的 $"LangAvalonia{Index:D4}"），不可能出现在语言 JSON 中，
    /// 因此静态扫描时按前缀跳过，不视为缺失键。
    /// </summary>
    private static readonly Regex DynamicOverrideKeyPattern = new(@"^LangAvalonia\d+$", RegexOptions.Compiled);

    [Fact]
    public void Language_json_files_have_same_key_set() {
        var zh = LoadLanguageFile("zh-CN.json");
        var en = LoadLanguageFile("en-US.json");

        Assert.NotEmpty(zh);
        Assert.Equal(zh.Keys.OrderBy(k => k, StringComparer.Ordinal),
            en.Keys.OrderBy(k => k, StringComparer.Ordinal));
    }

    [Fact]
    public void English_language_file_has_no_cjk_characters() {
        var en = LoadLanguageFile("en-US.json");
        var offenders = en
            .Where(pair => HanPattern.IsMatch(pair.Value))
            .Select(pair => $"{pair.Key}: {pair.Value}")
            .OrderBy(text => text, StringComparer.Ordinal)
            .ToList();

        Assert.True(offenders.Count == 0,
            $"en-US.json 中有 {offenders.Count} 条仍为中文，需要翻译成英文：{Environment.NewLine}" +
            string.Join(Environment.NewLine, offenders.Take(30)));
    }

    [Fact]
    public void Language_files_are_valid_flat_string_dictionaries_without_duplicate_keys() {
        foreach (var fileName in new[] { "zh-CN.json", "en-US.json" }) {
            var path = LanguageFilePath(fileName);
            Assert.True(File.Exists(path), $"找不到语言资源文件：{path}");

            using var document = JsonDocument.Parse(File.ReadAllText(path));
            Assert.Equal(JsonValueKind.Object, document.RootElement.ValueKind);

            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in document.RootElement.EnumerateObject()) {
                Assert.True(keys.Add(property.Name), $"{fileName} 中存在重复键：{property.Name}");
                Assert.True(property.Value.ValueKind == JsonValueKind.String,
                    $"{fileName} 中 {property.Name} 的值不是字符串");
                Assert.False(string.IsNullOrWhiteSpace(property.Value.GetString()),
                    $"{fileName} 中 {property.Name} 的值为空");
                Assert.StartsWith("Lang", property.Name);
            }
        }
    }

    [Fact]
    public void Every_static_lang_reference_used_by_xaml_and_vb_exists_in_language_json() {
        var keys = LoadLanguageFile("zh-CN.json").Keys.ToHashSet(StringComparer.Ordinal);
        var dynamicKeys = new SortedSet<string>(StringComparer.Ordinal);
        var missing = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
        var referenced = 0;

        foreach (var file in EnumerateSourceFiles()) {
            var content = File.ReadAllText(file);
            var relative = Path.GetRelativePath(RepositoryRoot, file);

            // XAML：{DynamicResource LangXxx}
            foreach (Match match in DynamicResourcePattern.Matches(content)) {
                referenced++;
                Check(match.Groups[1].Value, relative);
            }

            // VB：GetText("LangXxx") / GetLangText("LangXxx", ...) 等字符串字面量引用
            foreach (Match match in QuotedLangPattern.Matches(content)) {
                referenced++;
                Check(match.Groups[1].Value, relative);
            }
        }

        Assert.True(referenced > 0, "没有扫描到任何 Lang* 引用，源码路径可能不正确：" + RepositoryRoot);
        Assert.True(missing.Count == 0,
            $"以下 {missing.Count} 个 Lang* 键在 zh-CN.json / en-US.json 中不存在（界面会直接显示键名或回退）：{Environment.NewLine}" +
            string.Join(Environment.NewLine, missing.Select(pair => $"{pair.Key}  <-  {string.Join(", ", pair.Value)}")));

        return;

        void Check(string key, string sourcePath) {
            if (DynamicOverrideKeyPattern.IsMatch(key)) {
                dynamicKeys.Add(key);
                return;
            }
            if (keys.Contains(key)) return;
            if (!missing.TryGetValue(key, out var paths)) {
                paths = new SortedSet<string>(StringComparer.Ordinal);
                missing.Add(key, paths);
            }
            paths.Add(sourcePath);
        }
    }

    [AvaloniaFact]
    public void Localization_loads_language_json_and_sample_keys_are_translated_on_both_sides() {
        var samples = new[] {
            "LangSetTheme",           // 常规界面文案
            "LangSystemInfoTitle",    // PCL.Avalonia 新增：系统信息
            "LangNetProxyTitle",      // PCL.Avalonia 新增：网络代理
            "LangSetFontTitle",       // PCL.Avalonia 新增：字体
            "LangDialogBtnCancel",    // 通用按钮
            "LangPageOtherHelpSearchSimilarity" // 代码中通过 GetText 使用
        };
        var original = global::PCL.Localization.CurrentLanguage;
        try {
            // Initialize 会从 avares 资源加载 zh-CN.json / en-US.json；JSON 非法时这里会抛异常
            global::PCL.Localization.Initialize("zh-CN");

            global::PCL.Localization.SetLanguage("zh-CN");
            foreach (var key in samples) {
                var text = global::PCL.Localization.GetText(key);
                Assert.False(string.IsNullOrWhiteSpace(text), $"zh-CN 缺少 {key}");
                Assert.NotEqual(key, text);
            }

            global::PCL.Localization.SetLanguage("en-US");
            foreach (var key in samples) {
                var text = global::PCL.Localization.GetText(key);
                Assert.False(string.IsNullOrWhiteSpace(text), $"en-US 缺少 {key}");
                Assert.NotEqual(key, text);
                Assert.False(HanPattern.IsMatch(text), $"en-US 的 {key} 仍是中文：{text}");
            }
        } finally {
            global::PCL.Localization.SetLanguage(original);
        }
    }

    private static string RepositoryRoot => _repositoryRoot ??= FindRepositoryRoot();

    private static string? _repositoryRoot;

    private static string LanguageFilePath(string fileName) =>
        Path.Combine(RepositoryRoot, "PCL", "Assets", "Language", fileName);

    private static Dictionary<string, string> LoadLanguageFile(string fileName) {
        var path = LanguageFilePath(fileName);
        Assert.True(File.Exists(path), $"找不到语言资源文件：{path}");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var property in document.RootElement.EnumerateObject()) {
            // 重复键在上面单独测试；这里的 Add 可以额外兜底
            result[property.Name] = property.Value.GetString() ?? string.Empty;
        }
        return result;
    }

    private static IEnumerable<string> EnumerateSourceFiles() {
        var projectRoot = Path.Combine(RepositoryRoot, "PCL");
        return Directory.EnumerateFiles(projectRoot, "*.axaml", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(projectRoot, "*.vb", SearchOption.AllDirectories))
            .Where(path => !IsBuildOutput(path))
            .OrderBy(path => path, StringComparer.Ordinal);
    }

    private static bool IsBuildOutput(string path) {
        var relative = Path.GetRelativePath(RepositoryRoot, path);
        var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return parts.Any(part => part is "bin" or "obj");
    }

    private static string FindRepositoryRoot() {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null) {
            if (File.Exists(Path.Combine(directory.FullName, "PCL", "Assets", "Language", "zh-CN.json"))) {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("无法从测试输出目录向上找到仓库根目录（含 PCL/Assets/Language/zh-CN.json）");
    }
}
