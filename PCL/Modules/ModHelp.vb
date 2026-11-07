Imports Avalonia.Markup.Xaml
Imports Avalonia.Platform
Imports System.Text.RegularExpressions

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
                Using Stream As IO.Stream = AssetLoader.Open(New Uri("avares://PCL.Avalonia/Assets/Help.zip"))
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
        Text = Text.RegexReplace("\{setup:([a-zA-Z0-9]+)\}", Function(m) Replacer(Settings.GetSafe(m.Groups(1).Value, McInstanceSelected)))
        Text = Text.RegexReplace("\{varible:([^:\}]+)(?::([^\}]+))?\}", Function(m) Replacer(ReadRegistryValue($"HKEY_CURRENT_USER\Software\{RegFolder}", $"CustomEvent{m.Groups(1).Value}", m.Groups(2).Value)))
        Text = Text.RegexReplace("\{variable:([^:\}]+)(?::([^\}]+))?\}", Function(m) Replacer(ReadRegistryValue($"HKEY_CURRENT_USER\Software\{RegFolder}", $"CustomEvent{m.Groups(1).Value}", m.Groups(2).Value)))
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
        '设计时属性与代码后置
        Content = Content.RegexReplace("\s(x:Class|d:[A-Za-z]+|mc:[A-Za-z]+)\s*=\s*[""'][^""']*[""']", "")
        '不支持的属性
        Content = Content.RegexReplace("\s(SnapsToDevicePixels|UseLayoutRounding|ShowGridLines|CacheMode|BitmapEffect|BitmapEffectInput|Effect|LineStackingStrategy|TextOptions\.[A-Za-z]+|RenderOptions\.[A-Za-z]+)\s*=\s*[""'][^""']*[""']", "")
        'WPF 的 Effect（阴影、模糊等）无法直接映射，连同属性元素一起去掉
        Content = Content.RegexReplace("<\s*([A-Za-z]+:)?[A-Za-z]+\.Effect\s*>.*?<\s*/\s*([A-Za-z]+:)?[A-Za-z]+\.Effect\s*>", "", RegexOptions.Singleline)
        Content = Content.RegexReplace("<\s*DropShadowEffect\b[^>]*/\s*>", "")
        'ToolTip 属性改为附加属性
        Content = Content.RegexReplace("(?<![.\w])ToolTip(\s*)=", "ToolTip.Tip$1=")
        '图片路径
        Content = Content.Replace("pack://application:,,,/images/", PathImage)
        Content = Content.Replace("Blocks/Cobblestone.png", "Blocks/CobbleStone.png")
        '可见性
        Content = Content.Replace("Visibility=""Collapsed""", "IsVisible=""False""")
        Content = Content.Replace("Visibility=""Visible""", "IsVisible=""True""")
        Content = Content.Replace("Visibility=""Hidden""", "IsVisible=""False""")
        'WPF 样式、文档与模板等结构转换
        Content = ConvertWpfStyleMarkup(Content)
        Return Content
    End Function

    Private Const WpfXamlNs As String = "http://schemas.microsoft.com/winfx/2006/xaml"

    ''' <summary>
    ''' 将 WPF 特有的 Style、ControlTemplate、FlowDocument 等结构转换为 Avalonia 可加载的写法。
    ''' 复杂样式会被展开为元素属性，无法映射的属性会被忽略。
    ''' </summary>
    Private Class LegacyStyle
        Public TargetType As String = ""
        Public BasedOn As String = Nothing
        Public Setters As New List(Of KeyValuePair(Of String, String))
    End Class

    ''' <summary>
    ''' 将 WPF 特有的 Style、ControlTemplate、FlowDocument 等结构转换为 Avalonia 可加载的写法。
    ''' 复杂样式会被展开为元素属性，无法映射的属性会被忽略。
    ''' </summary>
    Private Function ConvertWpfStyleMarkup(Content As String) As String
        Try
            Dim Doc = XDocument.Parse("<root xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" xmlns:local=""using:PCL"" xmlns:sys=""using:System"">" & Content & "</root>")
            Dim Root = Doc.Root
            Dim ConvertedParagraphs As New HashSet(Of XElement)()
            '收集样式定义
            Dim KeyedStyles As New Dictionary(Of String, LegacyStyle)(StringComparer.Ordinal)
            Dim ImplicitStyles As New List(Of Tuple(Of String, List(Of KeyValuePair(Of String, String))))
            For Each StyleEl In Root.Descendants().Where(Function(e) e.Name.LocalName = "Style").ToList()
                Dim TargetType As String = If(CStr(StyleEl.Attribute("TargetType")), "")
                Dim Key As String = CStr(StyleEl.Attribute(XName.Get("Key", WpfXamlNs)))
                Dim BasedOn As String = ParseStaticResourceKey(CStr(StyleEl.Attribute("BasedOn")))
                Dim Setters As New List(Of KeyValuePair(Of String, String))
                For Each SetterEl In StyleEl.Elements().Where(Function(e) e.Name.LocalName = "Setter")
                    Dim Prop As String = CStr(SetterEl.Attribute("Property"))
                    Dim Val As String = CStr(SetterEl.Attribute("Value"))
                    If String.IsNullOrEmpty(Prop) OrElse Val Is Nothing Then Continue For
                    'WPF 的 Paragraph.LineHeight 常小于字号，Avalonia 会按行高裁切文字
                    If TargetType = "Paragraph" AndAlso Prop = "LineHeight" Then Continue For
                    Dim Mapped As String = MapWpfStyleProperty(Prop)
                    If Mapped Is Nothing Then Continue For
                    Setters.Add(New KeyValuePair(Of String, String)(Mapped, Val))
                Next
                If String.IsNullOrEmpty(Key) Then
                    ImplicitStyles.Add(Tuple.Create(TargetType, Setters))
                Else
                    KeyedStyles(Key) = New LegacyStyle With {.TargetType = TargetType, .BasedOn = BasedOn, .Setters = Setters}
                End If
                StyleEl.Remove()
            Next
            '移除模板与触发器
            For Each El In Root.Descendants().Where(Function(e) e.Name.LocalName.EndsWith(".Triggers") OrElse e.Name.LocalName = "ControlTemplate").ToList()
                El.Remove()
            Next
            '记录列表序号与标记颜色，供 ListItem 使用
            Dim ListMarkers As New Dictionary(Of XElement, String)()
            Dim ListMarkerColors As New Dictionary(Of XElement, String)()
            Dim ListForeground As String = GetImplicitStyleValue(ImplicitStyles, "List", "Foreground")
            For Each ListEl In Root.Descendants().Where(Function(e) e.Name.LocalName = "List").ToList()
                Dim MarkerStyle As String = If(CStr(ListEl.Attribute("MarkerStyle")), "Disc")
                Dim Index As Integer = 1
                Dim StartIndex As String = CStr(ListEl.Attribute("StartIndex"))
                If StartIndex IsNot Nothing Then Integer.TryParse(StartIndex, Index)
                For Each ItemEl In ListEl.Elements().Where(Function(e) e.Name.LocalName = "ListItem")
                    ListMarkers(ItemEl) = GetListMarker(MarkerStyle, Index)
                    If ListForeground IsNot Nothing Then ListMarkerColors(ItemEl) = ListForeground
                    Index += 1
                Next
            Next
            '元素类型映射
            For Each El In Root.Descendants().ToList()
                Select Case El.Name.LocalName
                    Case "FlowDocumentScrollViewer"
                        El.Name = XName.Get("ScrollViewer")
                    Case "FlowDocument"
                        El.Name = XName.Get("StackPanel")
                        ApplyImplicitStyleAttributes(El, "FlowDocument", ImplicitStyles)
                    Case "Section"
                        'Avalonia 没有 FlowDocument 的 Section，用它作为带边框的块容器
                        ConvertLegacySection(El)
                    Case "BlockUIContainer"
                        ConvertLegacyBlockUI(El)
                    Case "List"
                        'WPF FlowDocument 的列表在 Avalonia 中没有对应控件，映射为纵向容器
                        El.Name = XName.Get("StackPanel")
                        ApplyImplicitStyleAttributes(El, "List", ImplicitStyles)
                        El.Attribute("MarkerStyle")?.Remove()
                        El.Attribute("StartIndex")?.Remove()
                        If El.Attribute("Margin") Is Nothing Then El.SetAttributeValue("Margin", "0,4")
                    Case "ListItem"
                        ConvertLegacyListItem(El, ListMarkers, ListMarkerColors, ImplicitStyles)
                    Case "Paragraph"
                        El.Name = XName.Get("TextBlock")
                        ConvertedParagraphs.Add(El)
                        El.Attribute("LineHeight")?.Remove()
                        If El.Attribute("Style") Is Nothing AndAlso El.Attribute("Margin") Is Nothing Then
                            El.SetAttributeValue("Margin", "0,8")
                        End If
                    Case "ContentControl"
                        If El.Attribute("Template") IsNot Nothing Then
                            Dim RawText As String = If(CStr(El.Attribute("Content")), "")
                            For Each Attr In El.Attributes().Where(Function(a) a.Name.LocalName = "Content" OrElse a.Name.LocalName = "Template").ToList()
                                Attr.Remove()
                            Next
                            El.Name = XName.Get("TextBlock")
                            El.SetAttributeValue("Text", RawText)
                            El.SetAttributeValue("HorizontalAlignment", "Center")
                            El.SetAttributeValue("VerticalAlignment", "Center")
                            El.SetAttributeValue("FontSize", "15")
                            El.SetAttributeValue("Margin", "0,10")
                            If El.Attribute("Foreground") Is Nothing Then El.SetAttributeValue("Foreground", "{DynamicResource ColorBrush4}")
                        End If
                    Case "Image"
                        '普通 Image 无法直接加载网址，交给 MyImage 处理
                        Dim IsInlineImage As Boolean = El.Ancestors().Any(Function(a) a.Name.LocalName = "TextBlock")
                        El.Name = XName.Get("MyImage", "using:PCL")
                        If IsInlineImage Then
                            Dim ImageContainer As New XElement(XName.Get("InlineUIContainer"))
                            El.ReplaceWith(ImageContainer)
                            ImageContainer.Add(El)
                        End If
                    Case "MyImage"
                        '行内图片必须放进 InlineUIContainer，否则不能作为文本块的子元素
                        If El.Name.NamespaceName = "using:PCL" AndAlso El.Ancestors().Any(Function(a) a.Name.LocalName = "TextBlock") Then
                            Dim ImageContainer As New XElement(XName.Get("InlineUIContainer"))
                            El.ReplaceWith(ImageContainer)
                            ImageContainer.Add(El)
                        End If
                    Case "TextBox"
                        ConvertLegacyTextBox(El, KeyedStyles)
                    Case "Line"
                        Dim AttrX1 = El.Attribute("X1"), AttrY1 = El.Attribute("Y1"), AttrX2 = El.Attribute("X2"), AttrY2 = El.Attribute("Y2")
                        If AttrX1 IsNot Nothing AndAlso AttrX2 IsNot Nothing Then
                            El.SetAttributeValue("StartPoint", If(CStr(AttrX1), "0") & "," & If(CStr(AttrY1), "0"))
                            El.SetAttributeValue("EndPoint", CStr(AttrX2) & "," & If(CStr(AttrY2), "0"))
                            For Each Attr In {AttrX1, AttrY1, AttrX2, AttrY2}.Where(Function(a) a IsNot Nothing).ToList()
                                Attr.Remove()
                            Next
                        End If
                End Select
            Next
            '属性元素（如 Paragraph.Foreground）跟随所属元素改名
            For Each El In Root.Descendants().ToList()
                Dim LocalName As String = El.Name.LocalName
                If LocalName.StartsWith("Paragraph.", StringComparison.Ordinal) Then
                    El.Name = XName.Get("TextBlock." & LocalName.Substring(10))
                ElseIf LocalName.StartsWith("Section.", StringComparison.Ordinal) Then
                    El.Name = XName.Get("Border." & LocalName.Substring(8))
                ElseIf LocalName.StartsWith("ListItem.", StringComparison.Ordinal) Then
                    El.Name = XName.Get("StackPanel." & LocalName.Substring(9))
                ElseIf LocalName.StartsWith("List.", StringComparison.Ordinal) Then
                    El.Name = XName.Get("StackPanel." & LocalName.Substring(5))
                End If
            Next
            '文本块内的自定义按钮改用 InlineUIContainer 承载，保留点击与悬停效果
            For Each El In Root.Descendants().Where(Function(e) e.Name.LocalName = "MyTextButton" AndAlso e.Name.NamespaceName = "using:PCL").ToList()
                If Not El.Ancestors().Any(Function(a) a.Name.LocalName = "TextBlock") Then Continue For
                Dim InUnderline As Boolean = El.Ancestors().TakeWhile(Function(a) a.Name.LocalName <> "TextBlock").
                    Any(Function(a) a.Name.LocalName = "Underline")
                Dim LinkText As String = El.Value.Trim()
                El.RemoveNodes()
                Dim LinkContent As New XElement(XName.Get("TextBlock"),
                    New XAttribute("Text", LinkText),
                    New XAttribute("Foreground", "{Binding $parent[Label].Foreground}"))
                If InUnderline Then LinkContent.SetAttributeValue("TextDecorations", "Underline")
                El.Add(LinkContent)
                If El.Attribute("Cursor") Is Nothing Then El.SetAttributeValue("Cursor", "Hand")
                Dim UnderlineParent As XElement = If(InUnderline, TryCast(El.Parent, XElement), Nothing)
                Dim Container As New XElement(XName.Get("InlineUIContainer"))
                El.ReplaceWith(Container)
                Container.Add(El)
                If UnderlineParent IsNot Nothing AndAlso UnderlineParent.Name.LocalName = "Underline" AndAlso
                   UnderlineParent.Nodes().All(Function(n) n Is Container OrElse (TypeOf n Is XText AndAlso String.IsNullOrWhiteSpace(CType(n, XText).Value))) Then
                    Container.Remove()
                    UnderlineParent.ReplaceWith(Container)
                End If
            Next
            '展开显式引用的样式（支持 BasedOn 继承）
            For Each El In Root.Descendants().ToList()
                Dim StyleAttr = El.Attribute("Style")
                If StyleAttr Is Nothing Then Continue For
                Dim Key As String = ParseStaticResourceKey(StyleAttr.Value)
                If Key IsNot Nothing Then
                    Dim Values As New Dictionary(Of String, String)(StringComparer.Ordinal)
                    For Each Setter In ResolveStyleSetters(Key, KeyedStyles)
                        If ConvertedParagraphs.Contains(El) AndAlso Setter.Key = "LineHeight" Then Continue For
                        Values(Setter.Key) = Setter.Value
                    Next
                    For Each Pair In Values
                        If El.Attribute(Pair.Key) Is Nothing AndAlso IsWpfStylePropertySupported(El.Name.LocalName, Pair.Key) Then
                            El.SetAttributeValue(Pair.Key, Pair.Value)
                        End If
                    Next
                End If
                StyleAttr.Remove()
            Next
            '移除不可用属性
            For Each El In Root.Descendants().ToList()
                For Each Attr In El.Attributes().ToList()
                    Select Case Attr.Name.LocalName
                        Case "TextIndent", "IsSelectionEnabled", "MarkerStyle", "StartIndex", "PagePadding", "SnapsToDevicePixels", "UseLayoutRounding", "ShowGridLines", "CacheMode", "BitmapEffect", "BitmapEffectInput", "FontStretch", "ContentStringFormat", "UpdateSourceTrigger", "NotifyOnTargetUpdated", "NotifyOnSourceUpdated", "BindingGroupName"
                            Attr.Remove()
                        Case Else
                            If Attr.Name.LocalName.StartsWith("TextOptions.") OrElse Attr.Name.LocalName.StartsWith("RenderOptions.") Then Attr.Remove()
                    End Select
                Next
            Next
            '移除样式被清空后残留的空 Resources
            For Each El In Root.Descendants().Where(Function(e) e.Name.LocalName.EndsWith(".Resources") AndAlso Not e.HasElements).ToList()
                El.Remove()
            Next
            '隐式样式改为作用域样式
            Dim StyleXml As New Text.StringBuilder
            For Each Entry In ImplicitStyles
                Dim Selector As String = MapWpfImplicitSelector(Entry.Item1)
                If Selector Is Nothing Then Continue For
                Dim SettersXml As New Text.StringBuilder
                For Each Setter In Entry.Item2
                    If IsWpfStylePropertySupported(Selector, Setter.Key) Then
                        SettersXml.Append("<Setter Property=""").Append(Setter.Key).Append(""" Value=""").Append(Setter.Value.Replace("&", "&amp;").Replace("""", "&quot;")).Append("""/>")
                    End If
                Next
                If SettersXml.Length > 0 Then
                    StyleXml.Append("<Style Selector=""").Append(Selector).Append(""">").Append(SettersXml).Append("</Style>")
                End If
            Next
            Dim Result As New Text.StringBuilder
            If StyleXml.Length > 0 Then Result.Append("<StackPanel.Styles>").Append(StyleXml).Append("</StackPanel.Styles>")
            For Each Node In Root.Nodes()
                Result.Append(Node.ToString(SaveOptions.DisableFormatting))
            Next
            '命名空间由外层包装统一声明，XamlX 不允许在子元素上重复声明
            Return Result.ToString().RegexReplace("\sxmlns(:[A-Za-z0-9]+)?\s*=\s*[""'][^""']*[""']", "")
        Catch ex As Exception
            Logger.Warn(ex, "转换 WPF 样式失败")
            Return Content
        End Try
    End Function

    ''' <summary>
    ''' 解析 {StaticResource Key} 形式的样式引用。
    ''' </summary>
    Private Function ParseStaticResourceKey(Value As String) As String
        If String.IsNullOrWhiteSpace(Value) Then Return Nothing
        Value = Value.Trim()
        If Value.StartsWith("{StaticResource ") AndAlso Value.EndsWith("}") Then
            Return Value.Substring(16).TrimEnd("}"c).Trim()
        End If
        Return Nothing
    End Function

    ''' <summary>
    ''' 解析样式及其 BasedOn 链，按“基类在前、派生在后”返回所有 Setter。
    ''' </summary>
    Private Function ResolveStyleSetters(Key As String, Styles As Dictionary(Of String, LegacyStyle), Optional Visited As HashSet(Of String) = Nothing) As List(Of KeyValuePair(Of String, String))
        Dim Result As New List(Of KeyValuePair(Of String, String))
        Dim Info As LegacyStyle = Nothing
        If String.IsNullOrEmpty(Key) OrElse Not Styles.TryGetValue(Key, Info) Then Return Result
        If Visited Is Nothing Then Visited = New HashSet(Of String)(StringComparer.Ordinal)
        If Not Visited.Add(Key) Then Return Result
        If Info.BasedOn IsNot Nothing Then Result.AddRange(ResolveStyleSetters(Info.BasedOn, Styles, Visited))
        Result.AddRange(Info.Setters)
        Return Result
    End Function

    Private Function GetStyleValue(Setters As List(Of KeyValuePair(Of String, String)), Key As String) As String
        Dim Result As String = Nothing
        For Each Pair In Setters
            If String.Equals(Pair.Key, Key, StringComparison.Ordinal) Then Result = Pair.Value
        Next
        Return Result
    End Function

    ''' <summary>
    ''' Section、BlockUIContainer、ListItem 等 WPF 文档结构到 Avalonia 控件的转换。
    ''' </summary>
    Private Sub ConvertLegacySection(El As XElement)
        'Section 会被替换为 Border，相关绑定需要同步修改祖先类型
        For Each Desc In El.Descendants()
            For Each Attr In Desc.Attributes()
                If Attr.Value.Contains("AncestorType=Section") Then
                    Attr.Value = Attr.Value.Replace("AncestorType=Section", "AncestorType=Border")
                End If
            Next
        Next
        Dim BorderEl As New XElement(XName.Get("Border"))
        For Each Attr In El.Attributes().ToList()
            Attr.Remove()
            BorderEl.SetAttributeValue(Attr.Name, Attr.Value)
        Next
        Dim Panel As New XElement(XName.Get("StackPanel"))
        For Each Node In El.Nodes().ToList()
            Node.Remove()
            Panel.Add(Node)
        Next
        If Panel.HasElements Then BorderEl.Add(Panel)
        El.ReplaceWith(BorderEl)
    End Sub

    Private Sub ConvertLegacyBlockUI(El As XElement)
        Dim Children = El.Elements().ToList()
        Dim HasText As Boolean = El.Nodes().OfType(Of XText)().Any(Function(t) Not String.IsNullOrWhiteSpace(t.Value))
        If Children.Count = 1 AndAlso Not HasText Then
            Dim Child = Children(0)
            Child.Remove()
            For Each Name In {"Grid.Column", "Grid.Row", "Grid.ColumnSpan", "Grid.RowSpan", "VerticalAlignment"}
                Dim Attr = El.Attribute(Name)
                If Attr IsNot Nothing AndAlso Child.Attribute(Name) Is Nothing Then Child.SetAttributeValue(Name, Attr.Value)
            Next
            El.ReplaceWith(Child)
            Return
        End If
        Dim Panel As New XElement(XName.Get("StackPanel"))
        For Each Node In El.Nodes().ToList()
            Node.Remove()
            Panel.Add(Node)
        Next
        For Each Name In {"Grid.Column", "Grid.Row", "Grid.ColumnSpan", "Grid.RowSpan"}
            Dim Attr = El.Attribute(Name)
            If Attr IsNot Nothing Then Panel.SetAttributeValue(Name, Attr.Value)
        Next
        El.ReplaceWith(Panel)
    End Sub

    Private Sub ConvertLegacyListItem(El As XElement, Markers As Dictionary(Of XElement, String), MarkerColors As Dictionary(Of XElement, String), ImplicitStyles As List(Of Tuple(Of String, List(Of KeyValuePair(Of String, String)))))
        Dim Marker As String = Nothing
        If Not Markers.TryGetValue(El, Marker) Then Marker = "•"
        Dim Implicit As List(Of KeyValuePair(Of String, String)) = Nothing
        For Each Entry In ImplicitStyles
            If Entry.Item1 = "ListItem" Then Implicit = Entry.Item2
        Next
        '使用两列 Grid，让正文占据剩余宽度并正常换行（横向 StackPanel 会给无穷宽度）
        Dim Outer As New XElement(XName.Get("Grid"), New XAttribute("ColumnDefinitions", "Auto,*"))
        If Implicit IsNot Nothing Then
            For Each Pair In Implicit
                If {"Margin", "Padding", "Background", "MinWidth", "MinHeight", "MaxWidth", "MaxHeight"}.Contains(Pair.Key) Then
                    Outer.SetAttributeValue(Pair.Key, Pair.Value)
                End If
            Next
        End If
        Dim Bullet As New XElement(XName.Get("TextBlock"),
            New XAttribute("Text", Marker),
            New XAttribute("Margin", "0,0,6,0"),
            New XAttribute("VerticalAlignment", "Top"),
            New XAttribute("Grid.Column", "0"))
        Dim BulletForeground As String = Nothing
        If Not MarkerColors.TryGetValue(El, BulletForeground) Then
            BulletForeground = If(Implicit Is Nothing, Nothing, GetStyleValue(Implicit, "Foreground"))
        End If
        If BulletForeground IsNot Nothing Then Bullet.SetAttributeValue("Foreground", BulletForeground)
        Outer.Add(Bullet)
        Dim Nodes = El.Nodes().ToList()
        Dim InlineNames = {"Run", "Span", "Bold", "Italic", "Underline", "LineBreak", "MyTextButton", "MyImage"}
        Dim IsInlineOnly As Boolean = Nodes.Count > 0 AndAlso Nodes.All(
            Function(n)
                If TypeOf n Is XText Then Return True
                Dim E = TryCast(n, XElement)
                Return E IsNot Nothing AndAlso InlineNames.Contains(E.Name.LocalName)
            End Function)
        If IsInlineOnly Then
            Dim Content As New XElement(XName.Get("TextBlock"), New XAttribute("Grid.Column", "1"))
            For Each Node In Nodes
                Node.Remove()
                Content.Add(Node)
            Next
            Outer.Add(Content)
        ElseIf Nodes.Count = 1 AndAlso TypeOf Nodes(0) Is XElement Then
            Dim Child = DirectCast(Nodes(0), XElement)
            Child.Remove()
            Child.SetAttributeValue("Grid.Column", "1")
            Outer.Add(Child)
        Else
            Dim Content As New XElement(XName.Get("StackPanel"), New XAttribute("Grid.Column", "1"))
            For Each Node In Nodes
                Node.Remove()
                Content.Add(Node)
            Next
            Outer.Add(Content)
        End If
        '显式属性优先于隐式样式
        For Each Attr In El.Attributes().ToList()
            Attr.Remove()
            Outer.SetAttributeValue(Attr.Name, Attr.Value)
        Next
        El.ReplaceWith(Outer)
    End Sub

    ''' <summary>
    ''' 将 WPF 中当作代码块的 TextBox 转换为可选中文本，避免默认输入框样式与内联布局问题。
    ''' </summary>
    Private Sub ConvertLegacyTextBox(El As XElement, KeyedStyles As Dictionary(Of String, LegacyStyle))
        If El.Attribute("Text") Is Nothing AndAlso El.Nodes().Any() Then
            El.SetAttributeValue("Text", El.Value)
            El.RemoveNodes()
        End If
        Dim IsInline As Boolean = El.Ancestors().Any(Function(a) a.Name.LocalName = "TextBlock")
        Dim StyleKey As String = ParseStaticResourceKey(CStr(El.Attribute("Style")))
        Dim Setters As New List(Of KeyValuePair(Of String, String))
        If StyleKey IsNot Nothing Then Setters = ResolveStyleSetters(StyleKey, KeyedStyles)
        If StyleKey <> "InlineCode" AndAlso StyleKey <> "BlockCode" Then
            '未知样式的文本框仍然要保证能放在文本块内
            If IsInline Then
                Dim InnerContainer As New XElement(XName.Get("InlineUIContainer"))
                El.ReplaceWith(InnerContainer)
                InnerContainer.Add(El)
            End If
            Return
        End If
        Dim BorderEl As New XElement(XName.Get("Border"))
        For Each Name In {"Grid.Column", "Grid.Row", "Grid.ColumnSpan", "Grid.RowSpan", "VerticalAlignment"}
            Dim Attr = El.Attribute(Name)
            If Attr IsNot Nothing Then BorderEl.SetAttributeValue(Name, Attr.Value)
        Next
        Dim Background As String = GetStyleValue(Setters, "Background")
        If Background Is Nothing Then Background = If(StyleKey = "InlineCode", "{DynamicResource ColorBrush6}", "{DynamicResource ColorBrush7}")
        BorderEl.SetAttributeValue("Background", Background)
        Dim CornerRadius As String = GetStyleValue(Setters, "CornerRadius")
        If CornerRadius Is Nothing Then CornerRadius = If(StyleKey = "InlineCode", "5", "0,0,5,5")
        BorderEl.SetAttributeValue("CornerRadius", CornerRadius)
        Dim Padding As String = GetStyleValue(Setters, "Padding")
        If Padding Is Nothing Then Padding = If(StyleKey = "InlineCode", "4,0", "16,8,16,12")
        BorderEl.SetAttributeValue("Padding", Padding)
        Dim Margin As String = GetStyleValue(Setters, "Margin")
        If Margin IsNot Nothing Then BorderEl.SetAttributeValue("Margin", Margin)
        If StyleKey = "BlockCode" Then BorderEl.SetAttributeValue("Opacity", "0.8")
        If StyleKey = "InlineCode" Then BorderEl.SetAttributeValue("Opacity", "0.9")
        Dim TextEl As XElement = If(IsInline, New XElement(XName.Get("TextBlock")), New XElement(XName.Get("SelectableTextBlock")))
        TextEl.SetAttributeValue("Text", If(CStr(El.Attribute("Text")), ""))
        Dim FontFamily As String = GetStyleValue(Setters, "FontFamily")
        If FontFamily IsNot Nothing Then TextEl.SetAttributeValue("FontFamily", FontFamily)
        Dim FontSize As String = GetStyleValue(Setters, "FontSize")
        If FontSize IsNot Nothing Then TextEl.SetAttributeValue("FontSize", FontSize)
        Dim Foreground As String = GetStyleValue(Setters, "Foreground")
        If Foreground IsNot Nothing Then TextEl.SetAttributeValue("Foreground", Foreground)
        If Not IsInline Then TextEl.SetAttributeValue("TextWrapping", "NoWrap")
        For Each Attr In El.Attributes().Where(Function(a) a.Name.LocalName <> "Style" AndAlso a.Name.LocalName <> "Text").ToList()
            If Attr.Name.LocalName = "IsReadOnly" Then Continue For
            TextEl.SetAttributeValue(Attr.Name, Attr.Value)
        Next
        BorderEl.Add(TextEl)
        If IsInline Then
            Dim InlineContainer As New XElement(XName.Get("InlineUIContainer"), BorderEl)
            El.ReplaceWith(InlineContainer)
        Else
            El.ReplaceWith(BorderEl)
        End If
    End Sub

    Private Function GetImplicitStyleValue(ImplicitStyles As List(Of Tuple(Of String, List(Of KeyValuePair(Of String, String)))), TargetType As String, Key As String) As String
        For Each Entry In ImplicitStyles
            If Entry.Item1 = TargetType Then Return GetStyleValue(Entry.Item2, Key)
        Next
        Return Nothing
    End Function

    Private Sub ApplyImplicitStyleAttributes(El As XElement, TargetType As String, ImplicitStyles As List(Of Tuple(Of String, List(Of KeyValuePair(Of String, String)))))
        For Each Entry In ImplicitStyles
            If Entry.Item1 <> TargetType Then Continue For
            For Each Pair In Entry.Item2
                If {"Margin", "Padding", "Background", "MinWidth", "MinHeight", "MaxWidth", "MaxHeight"}.Contains(Pair.Key) AndAlso El.Attribute(Pair.Key) Is Nothing Then
                    El.SetAttributeValue(Pair.Key, Pair.Value)
                End If
            Next
        Next
    End Sub

    Private Function GetListMarker(Style As String, Index As Integer) As String
        Select Case Style
            Case "Decimal"
                Return Index.ToString & "."
            Case "Circle"
                Return "○"
            Case "Square", "Box"
                Return "▪"
            Case "LowerLatin"
                Return Chr(96 + ((Index - 1) Mod 26) + 1) & "."
            Case "UpperLatin"
                Return Chr(64 + ((Index - 1) Mod 26) + 1) & "."
            Case "LowerRoman"
                Return ToRoman(Index).ToLower & "."
            Case "UpperRoman"
                Return ToRoman(Index) & "."
            Case Else
                Return "•"
        End Select
    End Function

    Private Function ToRoman(Value As Integer) As String
        If Value <= 0 OrElse Value >= 4000 Then Return Value.ToString
        Dim Pairs = {
            New KeyValuePair(Of Integer, String)(1000, "M"), New KeyValuePair(Of Integer, String)(900, "CM"),
            New KeyValuePair(Of Integer, String)(500, "D"), New KeyValuePair(Of Integer, String)(400, "CD"),
            New KeyValuePair(Of Integer, String)(100, "C"), New KeyValuePair(Of Integer, String)(90, "XC"),
            New KeyValuePair(Of Integer, String)(50, "L"), New KeyValuePair(Of Integer, String)(40, "XL"),
            New KeyValuePair(Of Integer, String)(10, "X"), New KeyValuePair(Of Integer, String)(9, "IX"),
            New KeyValuePair(Of Integer, String)(5, "V"), New KeyValuePair(Of Integer, String)(4, "IV"),
            New KeyValuePair(Of Integer, String)(1, "I")}
        Dim Result As New Text.StringBuilder
        For Each Pair In Pairs
            Do While Value >= Pair.Key
                Result.Append(Pair.Value)
                Value -= Pair.Key
            Loop
        Next
        Return Result.ToString
    End Function

    ''' <summary>
    ''' 将 WPF 样式属性转换为 Avalonia 属性名，不支持时返回 Nothing。
    ''' </summary>
    Private Function MapWpfStyleProperty(Prop As String) As String
        Select Case Prop
            Case "ToolTip"
                Return "ToolTip.Tip"
            Case "Template", "MarkerStyle", "TextIndent", "IsSelectionEnabled", "PagePadding", "FontStretch", "ContentStringFormat", "Blocks"
                Return Nothing
            Case Else
                If Prop.StartsWith("TextOptions.") OrElse Prop.StartsWith("RenderOptions.") Then Return Nothing
                Return Prop
        End Select
    End Function

    Private Function MapWpfImplicitSelector(TargetType As String) As String
        Select Case TargetType
            Case "TextBlock", "Paragraph"
                Return "TextBlock"
            Case "FlowDocument"
                Return "StackPanel"
            Case "FlowDocumentScrollViewer"
                Return "ScrollViewer"
            Case Else
                Return Nothing
        End Select
    End Function

    ''' <summary>
    ''' 判断样式属性是否可以安全地应用到对应元素上。
    ''' </summary>
    Private Function IsWpfStylePropertySupported(ElementType As String, Prop As String) As Boolean
        Dim TypeName As String = ElementType
        Dim ColonIndex As Integer = TypeName.LastIndexOf(":"c)
        If ColonIndex >= 0 Then TypeName = TypeName.Substring(ColonIndex + 1)
        Select Case Prop
            Case "Margin", "Width", "Height", "MinWidth", "MinHeight", "MaxWidth", "MaxHeight", "HorizontalAlignment", "VerticalAlignment", "Opacity", "IsVisible", "IsEnabled", "ToolTip.Tip", "EventType", "EventData", "LogoScale", "Logo", "IsReadOnly"
                Return True
            Case "Padding"
                Return TypeName = "Border" OrElse TypeName = "TextBox" OrElse TypeName = "TextBlock" OrElse TypeName = "SelectableTextBlock" OrElse TypeName = "ScrollViewer" OrElse TypeName = "MyCard"
            Case "Background"
                Return TypeName = "StackPanel" OrElse TypeName = "Border" OrElse TypeName = "Grid" OrElse TypeName = "TextBox" OrElse TypeName = "TextBlock" OrElse TypeName = "SelectableTextBlock" OrElse TypeName = "ScrollViewer" OrElse TypeName = "MyCard" OrElse TypeName = "WrapPanel" OrElse TypeName = "ContentControl"
            Case "BorderThickness", "BorderBrush", "CornerRadius"
                Return TypeName = "Border" OrElse TypeName = "TextBox" OrElse TypeName = "SelectableTextBlock" OrElse TypeName = "MyCard"
            Case "FontSize", "FontWeight", "FontStyle", "FontFamily", "Foreground", "TextWrapping", "TextAlignment", "LineHeight", "Text", "TextTrimming"
                Return TypeName = "TextBlock" OrElse TypeName = "SelectableTextBlock" OrElse TypeName = "TextBox" OrElse TypeName = "MyTextBox" OrElse TypeName = "MyIconTextButton" OrElse TypeName = "MyTextButton" OrElse TypeName = "MyListItem" OrElse TypeName = "ContentControl"
            Case "Stretch"
                Return TypeName = "MyImage" OrElse TypeName = "Image"
            Case "VerticalScrollBarVisibility", "HorizontalScrollBarVisibility"
                Return TypeName = "ScrollViewer" OrElse TypeName = "FlowDocumentScrollViewer"
            Case "HorizontalContentAlignment", "VerticalContentAlignment"
                Return True
            Case Else
                Return False
        End Select
    End Function

    ''' <summary>
    ''' 将帮助条目的 XAML 内容转换为 Avalonia 控件。
    ''' </summary>
    Public Function ConvertHelpXaml(Content As String) As Control
        If String.IsNullOrWhiteSpace(Content) Then Throw New Exception("帮助 xaml 文件为空")
        Content = ArgumentReplace(Content, AddressOf StringUtils.XmlEscape)
        Content = ConvertLegacyXaml(Content)
        '外面套一层 StackPanel
        Content = "<StackPanel xmlns=""https://github.com/avaloniaui"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" xmlns:local=""using:PCL"" xmlns:sys=""using:System"">" & Content & "</StackPanel>"
        Return CType(GetObjectFromXML(Content), Control)
    End Function

#End Region

End Module
