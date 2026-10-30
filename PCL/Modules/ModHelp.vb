Imports Avalonia.Markup.Xaml
Imports Avalonia.Platform

''' <summary>
''' 帮助系统：帮助条目的加载、内置帮助的解压与帮助 XAML 的运行时加载。
''' </summary>
Public Module ModHelp

#Region "帮助条目"

    Public Class HelpEntry
        ''' <summary>
        ''' 原始信息路径。用于刷新。
        ''' </summary>
        Public RawPath As String

        '基础
        Public Title As String
        Public Desc As String
        Public Search As String
        Public Types As List(Of String)

        '显示（可选）
        Public Logo As String = Nothing
        Public ShowInSearch As Boolean = True
        Public ShowInPublic As Boolean = True
        Public ShowInSnapshot As Boolean = True

        '动作
        Public IsEvent As Boolean
        Public EventType As CustomEvent.EventType
        Public EventData As String
        Public XamlContent As String

        ''' <summary>
        ''' 从文件初始化 HelpEntry 对象，失败会抛出异常。
        ''' </summary>
        Public Sub New(FilePath As String)
            RawPath = FilePath
            If Not FileUtils.Exists(FilePath) Then Throw New FileNotFoundException("未找到帮助文件：" & FilePath, FilePath)
            Dim JsonData As JObject = JObject.Parse(ArgumentReplace(FileUtils.ReadAsString(FilePath), AddressOf StringUtils.XmlEscape))
            '加载常规信息
            If JsonData("Title") IsNot Nothing Then
                Title = JsonData("Title")
            Else
                Throw New ArgumentException("未找到 Title 项")
            End If
            Desc = If(JsonData("Description"), "")
            Search = If(JsonData("Keywords"), "")
            Logo = JsonData("Logo") '为保持 Nothing，不要加 If
            ShowInSearch = If(JsonData("ShowInSearch"), ShowInSearch)
            ShowInPublic = If(JsonData("ShowInPublic"), ShowInPublic)
            ShowInSnapshot = If(JsonData("ShowInSnapshot"), ShowInSnapshot)
            Types = New List(Of String)
            For Each NameOfType As JToken In If(JsonData("Types"), New JArray)
                Types.Add(NameOfType.ToString())
            Next
            '加载事件信息
            If If(JsonData("IsEvent"), False) Then
                EventType = JsonData("EventType").ToObject(Of CustomEvent.EventType)()
                EventData = If(JsonData("EventData"), "")
                IsEvent = True
            Else
                Dim XamlAddress As String = Path.ChangeExtension(FilePath, ".xaml")
                If FileUtils.Exists(XamlAddress) Then
                    XamlContent = FileUtils.ReadAsString(XamlAddress)
                    IsEvent = False
                Else
                    Throw New FileNotFoundException("未找到帮助条目 .json 对应的 .xaml 文件（" & XamlAddress & "）")
                End If
            End If
        End Sub

        ''' <summary>
        ''' 获取该 HelpEntry 对应的 MyListItem。
        ''' </summary>
        Public Function ToListItem() As MyListItem
            Return SetToListItem(New MyListItem)
        End Function

        ''' <summary>
        ''' 将属性设置入一个现有的 ListItem。
        ''' </summary>
        Public Function SetToListItem(Item As MyListItem) As MyListItem
            Dim ItemLogo As String
            If IsEvent Then
                If EventType = CustomEvent.EventType.弹出窗口 Then
                    ItemLogo = PathImage & "Blocks/GrassPath.png"
                Else
                    ItemLogo = PathImage & "Blocks/CommandBlock.png"
                End If
            Else
                ItemLogo = PathImage & "Blocks/Grass.png"
            End If
            '先设置尺寸与类型，Logo 依赖 Height
            Item.Height = 42
            Item.Type = MyListItem.CheckType.Clickable
            With Item
                .Title = Title
                .Info = Desc
                .Logo = If(Me.Logo, ItemLogo)
                .Tag = Me
            End With
            CustomEventService.SetEventType(Item, CustomEvent.EventType.None) '清空自定义事件属性，它们会被下面的点击事件处理
            CustomEventService.SetEventData(Item, Nothing)
            '项目的点击事件
            AddHandler Item.Click, Sub(sender, e) PageOtherHelp.OnItemClick(CType(sender, MyListItem).Tag)
            Return Item
        End Function

    End Class

    Public HelpLoader As New LoaderTask(Of Integer, List(Of HelpEntry))("Help Page", AddressOf HelpLoad,, ThreadPriority.BelowNormal)
    Private ReadOnly HelpLoadLock As New Object

    ''' <summary>
    ''' 初始化帮助列表对象。
    ''' </summary>
    Private Sub HelpLoad(Loader As LoaderTask(Of Integer, List(Of HelpEntry)))
        SyncLock HelpLoadLock '避免重复解压文件导致出错
            Try
                '解压内置文件
                HelpTryExtract()

                '遍历文件
                Dim FileList As New List(Of String)
                Try
                    Dim IgnoreList As New List(Of String)
                    '读取自定义文件
                    Dim CustomHelpFolder As String = Path.Combine(PathData, "Help")
                    If DirectoryUtils.Exists(CustomHelpFolder) Then
                        For Each File As String In DirectoryUtils.EnumerateFiles(CustomHelpFolder, True)
                            Select Case PathUtils.GetExtension(File)
                                Case "helpignore"
                                    '加载忽略列表
                                    Logger.Info($"发现 .helpignore 文件：{File}")
                                    For Each Line As String In FileUtils.ReadAsLines(File, True)
                                        Dim RealString As String = Line.BeforeFirst("#").Trim
                                        If String.IsNullOrWhiteSpace(RealString) Then Continue For
                                        IgnoreList.Add(RealString)
                                        Logger.Trace($"> {RealString}")
                                    Next
                                Case "json"
                                    FileList.Add(File)
                            End Select
                        Next
                    End If
                    Logger.Info($"已扫描数据文件夹下的帮助文件，目前总计 {FileList.Count} 条")
                    '读取自带文件
                    Dim BuiltinHelpFolder As String = Path.Combine(PathTemp, "Help")
                    For Each File As String In DirectoryUtils.EnumerateFiles(BuiltinHelpFolder, True)
                        Dim RelativePath As String = Path.GetRelativePath(BuiltinHelpFolder, File).Replace("\"c, "/"c)
                        '跳过非 json 文件与以 . 开头的文件夹
                        If RelativePath.Split("/"c).Any(Function(s) s.StartsWithF(".", False)) OrElse PathUtils.GetExtension(File) <> "json" Then Continue For
                        '检查忽略列表
                        Dim IsIgnored As Boolean = False
                        For Each Ignore As String In IgnoreList
                            If RelativePath.RegexCheck(Ignore) OrElse RelativePath.RegexCheck(Ignore.Replace("\"c, "/"c)) Then
                                Logger.Trace($"已忽略 {RelativePath}：{Ignore}")
                                IsIgnored = True
                                Exit For
                            End If
                        Next
                        If IsIgnored Then Continue For
                        FileList.Add(File)
                    Next
                    Logger.Info($"已扫描缓存文件夹下的帮助文件，目前总计 {FileList.Count} 条")
                Catch ex As Exception
                    Logger.[Error](ex, "检查帮助文件夹失败", LogBehavior.Alert)
                End Try
                If Loader.IsCanceled Then Return

                '将文件实例化
                Dim Dict As New List(Of HelpEntry)
                For Each FilePath As String In FileList
                    Try
                        Dim Entry As New HelpEntry(FilePath)
                        Dict.Add(Entry)
                        Logger.Trace($"已加载的帮助条目：{Entry.Title} ← {FilePath}")
                    Catch ex As Exception
                        Logger.[Error](ex, $"初始化帮助条目失败（{FilePath}）", LogBehavior.Alert)
                    End Try
                Next

                '回设
                If Not Dict.Any() Then Throw New Exception("未找到可用的帮助；若不需要帮助页面，可以在 设置 → 个性化 → 功能隐藏 中将其隐藏")
                If Loader.IsCanceled Then Return
                Loader.Output = Dict

            Catch ex As Exception
                Logger.Warn(ex, "帮助列表初始化失败")
                Throw
            End Try
        End SyncLock
    End Sub

    ''' <summary>
    ''' 尝试解压内置帮助文件。
    ''' </summary>
    Public Sub HelpTryExtract()
        SyncLock HelpLoadLock '与帮助列表加载共用锁，避免并发解压
            Dim HelpFolder As String = Path.Combine(PathTemp, "Help")
            If Settings.Get(Of Integer)("SystemHelpVersion") <> VersionCode OrElse Not FileUtils.Exists(Path.Combine(HelpFolder, "启动器", "备份设置.xaml")) Then
                DirectoryUtils.Delete(HelpFolder)
                DirectoryUtils.Create(HelpFolder)
                Using Stream As IO.Stream = AssetLoader.Open(New Uri("avares://PCL/Assets/Help.zip"))
                    Using Archive As New ZipArchive(Stream, ZipArchiveMode.Read)
                        For Each Entry As ZipArchiveEntry In Archive.Entries
                            If String.IsNullOrEmpty(Entry.Name) Then Continue For
                            Dim TargetPath As String = Path.GetFullPath(Path.Combine(HelpFolder, Entry.FullName))
                            If Not PathUtils.IsParentOf(HelpFolder, TargetPath) Then Continue For
                            DirectoryUtils.Create(PathUtils.RemoveLastPart(TargetPath))
                            Entry.ExtractToFile(TargetPath, True)
                        Next
                    End Using
                End Using
                Settings.[Set]("SystemHelpVersion", VersionCode)
                Logger.Info($"已解压内置帮助文件，目前状态：{FileUtils.Exists(Path.Combine(HelpFolder, "启动器", "备份设置.xaml"))}")
            End If
        End SyncLock
    End Sub

#End Region

#Region "替换标记"

    ''' <summary>
    ''' 对替换标记进行处理。会对替换内容使用 EscapeHandler 进行转义。
    ''' </summary>
    Public Function ArgumentReplace(Text As String, Optional EscapeHandler As Func(Of String, String) = Nothing, Optional ReplaceTime As Boolean = True) As String
        If Text Is Nothing OrElse Not Text.Contains("{") Then Return Text
        '预处理（注意，文件夹必须以 \ 结尾）
        Dim Replacer As Func(Of String, String) =
            Function(s As String) As String
                If s Is Nothing Then Return ""
                If EscapeHandler Is Nothing Then Return s
                If s.Contains(":\") Then
                    Dim IsFolder = s.EndsWithF("\") OrElse s.EndsWithF("/")
                    s = PathUtils.ToShortPath(s)
                    If IsFolder Then s = PathUtils.AddSlashSuffix(s)
                End If
                Return EscapeHandler(s)
            End Function
        '基础
        Text = Text.Replace("{pcl_version}", Replacer(VersionBaseName))
        Text = Text.Replace("{pcl_version_code}", Replacer(VersionCode.ToString()))
        Text = Text.Replace("{pcl_version_branch}", Replacer(BuildTypeDisplay))
        Text = Text.Replace("{pcl_build_type}", Replacer(BuildType.ToString()))
        Text = Text.Replace("{pcl_branch}", Replacer(VersionBranchMain))
        Text = Text.Replace("{identify}", Replacer(Identify))
        Text = Text.Replace("{path}", Replacer(Paths.Base))
        Text = Text.Replace("{path_with_name}", Replacer(PathExe))
        Text = Text.Replace("{path_temp}", Replacer(PathTemp))
        If Text.Contains("{pcl_md5}") Then Text = Text.Replace("{pcl_md5}", Replacer(CryptographyUtils.ComputeFileHash(PathExe, CryptographyUtils.HashMethod.Md5)))
        If Text.Contains("{pcl_sha1}") Then Text = Text.Replace("{pcl_sha1}", Replacer(CryptographyUtils.ComputeFileHash(PathExe, CryptographyUtils.HashMethod.Sha1)))
        '时间
        If ReplaceTime Then '在窗口标题中，时间会被后续动态替换，所以此时不应该替换
            Text = Text.Replace("{date}", Replacer(Date.Now.ToString("yyyy'/'M'/'d")))
            Text = Text.Replace("{time}", Replacer(Date.Now.ToString("HH':'mm':'ss")))
        End If
        'Minecraft
        Text = Text.Replace("{java}", Replacer(McLaunchJavaSelected?.Folder))
        Text = Text.Replace("{minecraft}", Replacer(McFolderSelected))
        If McInstanceSelected IsNot Nothing AndAlso McInstanceSelected.IsLoaded Then
            Text = Text.Replace("{version_path}", Replacer(McInstanceSelected.PathVersion)) : Text = Text.Replace("{verpath}", Replacer(McInstanceSelected.PathVersion))
            Text = Text.Replace("{version_indie}", Replacer(McInstanceSelected.PathIndie)) : Text = Text.Replace("{verindie}", Replacer(McInstanceSelected.PathIndie))
            Text = Text.Replace("{name}", Replacer(McInstanceSelected.Name))
            If {"unknown", "old", "pending"}.Contains(McInstanceSelected.Version.VanillaName.Lower) Then
                Text = Text.Replace("{version}", Replacer(McInstanceSelected.Name))
            Else
                Text = Text.Replace("{version}", Replacer(McInstanceSelected.Version.VanillaName))
            End If
            Text = Text.Replace("{forge}", Replacer(If(McInstanceSelected.Version.ModLoaders.HasFlagF(ModLoaders.Forge), McInstanceSelected.Version.ModLoaderVersions(ModLoaders.Forge), "")))
            Text = Text.Replace("{fabric}", Replacer(If(McInstanceSelected.Version.ModLoaders.HasFlagF(ModLoaders.Fabric), McInstanceSelected.Version.ModLoaderVersions(ModLoaders.Fabric), "")))
            Text = Text.Replace("{neoforge}", Replacer(If(McInstanceSelected.Version.ModLoaders.HasFlagF(ModLoaders.NeoForge), McInstanceSelected.Version.ModLoaderVersions(ModLoaders.NeoForge), "")))
            Text = Text.Replace("{liteloader}", Replacer(If(McInstanceSelected.Version.ModLoaders.HasFlagF(ModLoaders.LiteLoader), "true", "")))
        Else
            Text = Text.Replace("{version_path}", Replacer(Nothing)) : Text = Text.Replace("{verpath}", Replacer(Nothing))
            Text = Text.Replace("{version_indie}", Replacer(Nothing)) : Text = Text.Replace("{verindie}", Replacer(Nothing))
            Text = Text.Replace("{name}", Replacer(Nothing))
            Text = Text.Replace("{version}", Replacer(Nothing))
            Text = Text.Replace("{forge}", Replacer(Nothing))
            Text = Text.Replace("{fabric}", Replacer(Nothing))
            Text = Text.Replace("{neoforge}", Replacer(Nothing))
            Text = Text.Replace("{liteloader}", Replacer(Nothing))
        End If
        '登录信息
        If McLoginLoader.State = LoadState.Finished Then
            Text = Text.Replace("{user}", Replacer(McLoginLoader.Output.Name))
            Text = Text.Replace("{uuid}", Replacer(McLoginLoader.Output.Uuid?.Lower))
            Select Case McLoginLoader.Output.Type
                Case "Legacy"
                    Text = Text.Replace("{login}", Replacer("离线"))
                Case "Ms"
                    Text = Text.Replace("{login}", Replacer("正版"))
                Case "Nide"
                    Text = Text.Replace("{login}", Replacer("统一通行证"))
                Case "Auth"
                    Text = Text.Replace("{login}", Replacer("Authlib-Injector"))
            End Select
        Else
            Text = Text.Replace("{user}", Replacer(Nothing))
            Text = Text.Replace("{uuid}", Replacer(Nothing))
            Text = Text.Replace("{login}", Replacer(Nothing))
        End If
        '高级
        Text = Text.RegexReplace("\{hint\}", Function() Replacer(PageOtherTest.GetRandomHint()))
        Text = Text.RegexReplace("\{cave\}", Function() Replacer(PageOtherTest.GetRandomCave()))
        Text = Text.RegexReplace("\{setup:([a-zA-Z0-9]+)\}", Function(m) Replacer(Settings.GetSafe(m.Groups(1).Value, McInstanceSelected)))
        Text = Text.RegexReplace("\{varible:([^:\}]+)(?::([^\}]+))?\}", Function(m) Replacer(RegistryUtils.TryRead($"HKEY_CURRENT_USER\Software\{RegFolder}", $"CustomEvent{m.Groups(1).Value}", m.Groups(2).Value)))
        Text = Text.RegexReplace("\{variable:([^:\}]+)(?::([^\}]+))?\}", Function(m) Replacer(RegistryUtils.TryRead($"HKEY_CURRENT_USER\Software\{RegFolder}", $"CustomEvent{m.Groups(1).Value}", m.Groups(2).Value)))
        Return Text
    End Function

#End Region

#Region "XAML 转换"

    ''' <summary>
    ''' 将旧版帮助 XAML 的内容转换为 Avalonia 控件。
    ''' 注意：性能较差，不应大量使用。
    ''' </summary>
    Public Function GetObjectFromXML(Str As String) As Object
        '兼容旧版自定义事件写法
        Str = Str.
            Replace("EventType=""", "local:CustomEventService.EventType=""").
            Replace("EventData=""", "local:CustomEventService.EventData=""").
            Replace("EventType='", "local:CustomEventService.EventType='").
            Replace("EventData='", "local:CustomEventService.EventData='").
            Replace("Property=""EventType""", "Property=""local:CustomEventService.EventType""").
            Replace("Property=""EventData""", "Property=""local:CustomEventService.EventData""").
            Replace("Property='EventType'", "Property='local:CustomEventService.EventType'").
            Replace("Property='EventData'", "Property='local:CustomEventService.EventData'")

        '安全性检查
        For Each Pattern In {"<\s*(Window|WebBrowser|Frame|MediaElement|ObjectDataProvider|XmlDataProvider|XamlReader)\b",
                             "x:Code\b", "\bFactoryMethod\s*=", "\bStatic\s*=",
                             "<\s*local:SettingService\b"}
            If Str.RegexCheck(Pattern) Then Throw New UnauthorizedAccessException($"基于安全考虑，不允许在自定义帮助中使用 {Pattern}。")
        Next

        Return AvaloniaRuntimeXamlLoader.Load(Str, GetType(MyButton).Assembly)
    End Function

    ''' <summary>
    ''' 将旧版（WPF 风格）自定义 XAML 中 Avalonia 不兼容的写法转换为 Avalonia 写法。
    ''' </summary>
    Public Function ConvertLegacyXaml(Content As String) As String
        '禁止声明命名空间
        Do While Content.Contains("xmlns")
            Content = Content.RegexReplace("xmlns[^""']*(""|')[^""']*(""|')", "").Replace("xmlns", "")
        Loop
        '不支持的属性
        Content = Content.RegexReplace("\s(SnapsToDevicePixels|UseLayoutRounding)\s*=\s*[""'][^""']*[""']", "")
        'ToolTip 属性改为附加属性
        Content = Content.RegexReplace("(?<![.\w])ToolTip(\s*)=", "ToolTip.Tip$1=")
        '图片路径
        Content = Content.Replace("pack://application:,,,/images/", PathImage)
        Content = Content.Replace("Blocks/Cobblestone.png", "Blocks/CobbleStone.png")
        '可见性
        Content = Content.Replace("Visibility=""Collapsed""", "IsVisible=""False""")
        Content = Content.Replace("Visibility=""Visible""", "IsVisible=""True""")
        Return Content
    End Function

    ''' <summary>
    ''' 将帮助条目的 XAML 内容转换为 Avalonia 控件。
    ''' </summary>
    Public Function ConvertHelpXaml(Content As String) As Control
        If String.IsNullOrWhiteSpace(Content) Then Throw New Exception("帮助 xaml 文件为空")
        Content = ArgumentReplace(Content, AddressOf StringUtils.XmlEscape)
        Content = ConvertLegacyXaml(Content)
        '外面套一层 StackPanel
        Content = "<StackPanel xmlns=""https://github.com/avaloniaui"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" xmlns:local=""using:PCL"">" & Content & "</StackPanel>"
        Return CType(GetObjectFromXML(Content), Control)
    End Function

#End Region

End Module
