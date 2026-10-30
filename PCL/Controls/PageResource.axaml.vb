''' <summary>
''' 社区资源搜索页面，由各个资源类别页面复用。
''' </summary>
Public Class PageResource
    Inherits MyScrollViewer

#Region "属性"

    ''' <summary>
    ''' 用于 XAML 快速设置的 Tag 下拉框列表。
    ''' </summary>
    Private _SearchTags As New List(Of MyComboBoxItem)
    Public Property SearchTags As List(Of MyComboBoxItem)
        Get
            Return _SearchTags
        End Get
        Set(value As List(Of MyComboBoxItem))
            _SearchTags = If(value, New List(Of MyComboBoxItem))
            ApplySearchTags()
        End Set
    End Property

    Private Sub ApplySearchTags()
        If ComboSearchTag Is Nothing Then Return
        ComboSearchTag.Items.Clear()
        For Each TagItem As MyComboBoxItem In _SearchTags
            ComboSearchTag.Items.Add(TagItem)
        Next
        If ComboSearchTag.SelectedIndex < 0 AndAlso ComboSearchTag.Items.Count > 0 Then ComboSearchTag.SelectedIndex = 0
    End Sub

    ''' <summary>
    ''' 英文前后不含空格的可读资源类型名，例如 "Mod"、"整合包"。
    ''' </summary>
    Private _TypeName As String = ""
    Public Property TypeName As String
        Get
            Return _TypeName
        End Get
        Set(Value As String)
            If _TypeName = Value Then Return
            _TypeName = Value
            If Loader IsNot Nothing Then Loader.Name = $"社区资源获取：{Value}"
        End Set
    End Property

    ''' <summary>
    ''' 英文前后含一个空格的可读资源类型名，例如 " Mod "、"整合包"。
    ''' </summary>
    Private _TypeNameSpaced As String = ""
    Public Property TypeNameSpaced As String
        Get
            Return _TypeNameSpaced
        End Get
        Set(Value As String)
            If _TypeNameSpaced = Value Then Return
            _TypeNameSpaced = Value
            RefreshTypeNameUi()
        End Set
    End Property

    ''' <summary>
    ''' 该页面对应的资源类型。
    ''' </summary>
    Private _Type As ResourceTypes = -1
    Public Property PageType As ResourceTypes
        Get
            Return _Type
        End Get
        Set(Value As ResourceTypes)
            If _Type = Value Then Return
            _Type = Value
            RefreshTypeNameUi()
        End Set
    End Property

    Private Sub RefreshTypeNameUi()
        If PanAlways IsNot Nothing Then PanAlways.Title = $"搜索{TypeNameSpaced}"
        If Load IsNot Nothing Then Load.Text = $"正在获取{TypeNameSpaced}列表"
        If BtnSearchInstallModPack IsNot Nothing Then BtnSearchInstallModPack.IsVisible = PageType = ResourceTypes.ModPack
    End Sub

#End Region

#Region "加载"

    ''' <summary>
    ''' 在切换到页面时，应自动将筛选项设置为与该目标 MC 版本和加载器相同。
    ''' </summary>
    Public Shared TargetInstance As McInstance = Nothing
    ''' <summary>
    ''' 在切换到该页面时自动设置的搜索框内的内容。
    ''' </summary>
    Public Shared TargetName As String = Nothing

    '在点击 MyResourceItem 时会获取 Loader 的输入，以使资源详情页面可以应用相同的筛选项
    Public Loader As LoaderTask(Of ResourceSearcher.SearchRequest, Integer) = ResourceSearcher.GetLoader(AddressOf LoaderInput)

    Public Sub New()
        InitControls()
    End Sub

    Private Sub PageResource_Loaded() Handles Me.Loaded
        Static IsFirstLoaded As Boolean = False
        RefreshTypeNameUi()
        ApplySearchTags()
        If ComboSearchSource.SelectedIndex < 0 Then ComboSearchSource.SelectedIndex = 0
        If TextSearchVersion.SelectedIndex < 0 Then TextSearchVersion.SelectedIndex = 0
        If ComboSearchLoader.SelectedIndex < 0 Then ComboSearchLoader.SelectedIndex = 0
        If TargetInstance IsNot Nothing Then
            '设置目标
            ResetFilter() '重置筛选器
            TextSearchVersion.Text = TargetInstance.Version.VanillaName
            Dim GetTargetItemByName =
            Function(Name As String) As MyComboBoxItem
                For Each Item As MyComboBoxItem In ComboSearchLoader.Items
                    If Item.Content = Name Then Return Item
                Next
                Return ComboSearchLoader.Items(0)
            End Function
            If TargetInstance.Version.ModLoaders.HasFlagF(ModLoaders.Forge) Then
                ComboSearchLoader.SelectedItem = GetTargetItemByName("Forge")
            ElseIf TargetInstance.Version.ModLoaders.HasFlagF(ModLoaders.Fabric) Then
                ComboSearchLoader.SelectedItem = GetTargetItemByName("Fabric")
            ElseIf TargetInstance.Version.ModLoaders.HasFlagF(ModLoaders.NeoForge) Then
                ComboSearchLoader.SelectedItem = GetTargetItemByName("NeoForge")
            End If
            TargetInstance = Nothing
            If TargetName IsNot Nothing Then
                TextSearchName.Text = TargetName
                TargetName = Nothing
            End If
            '如果已经完成请求，则重新开始
            If IsFirstLoaded Then StartNewSearch()
            ScrollToHome()
        End If
        '加载器初始化
        If IsFirstLoaded Then Return
        IsFirstLoaded = True
        CType(Parent, MyPageRight).PageLoaderInit(Load, PanLoad, PanContent, PanAlways, Loader, AddressOf Load_OnFinish, AddressOf LoaderInput)
        '将最高 Drop 加入筛选
        If AllDrops IsNot Nothing AndAlso AllDrops.First > 250 Then
            Dim HighestVersion As String = McVersion.DropToVersion(AllDrops.First)
            If CType(TextSearchVersion.Items(1), MyComboBoxItem).Content.ToString <> HighestVersion Then '0 是全部
                TextSearchVersion.Items.Insert(1, New MyComboBoxItem With {.Content = HighestVersion})
            End If
        End If
    End Sub

    Private Function LoaderInput() As ResourceSearcher.SearchRequest
        Dim Request As New ResourceSearcher.SearchRequest(PageType, Storage, (Page + 1) * PAGE_SIZE)
        Dim GameVersion As String = If(TextSearchVersion.Text = "全部 (也可自行输入)", Nothing,
            If(TextSearchVersion.Text.Contains(".") OrElse TextSearchVersion.Text.Contains("w"), TextSearchVersion.Text, Nothing))
        With Request
            .SearchText = TextSearchName.Text
            .GameVersion = GameVersion
            .Tag = ComboSearchTag.SelectedItem.Tag
            .ModLoaders = If(PageType = ResourceTypes.Mod, Val(ComboSearchLoader.SelectedItem.Tag), ModLoaders.None)
            .Sources = CType(Val(ComboSearchSource.SelectedItem.Tag), ResourcePlatforms)
        End With
        Return Request
    End Function

#End Region

    Public Storage As New ResourceSearcher.SearchResult
    ''' <summary>
    ''' 每页展示的结果数量。
    ''' </summary>
    Public Const PAGE_SIZE = 40
    Public Page As Integer = 0

    '结果 UI 化
    Private Sub Load_OnFinish()
        Try
            Logger.Info($"开始可视化{TypeNameSpaced}列表，已储藏 {Storage.Results.Count} 个结果，当前在第 {Page + 1} 页")
            '列表项
            PanProjects.Children.Clear()
            Dim Index As Integer = Math.Min(Page * PAGE_SIZE, Storage.Results.Count - 1)
            For Each Result In Storage.Results.GetRange(Index, Math.Min(Storage.Results.Count - Index, PAGE_SIZE))
                PanProjects.Children.Add(Result.ToResourceItem(
                    ShowMcVersionDesc:=Loader.Input.GameVersion Is Nothing,
                    ShowLoaderDesc:=Loader.Input.ModLoaders = ModLoaders.None AndAlso (PageType = ResourceTypes.Mod OrElse PageType = ResourceTypes.ModPack)))
            Next
            '页码
            CardPages.IsVisible = Storage.Results.Count > 40 OrElse
                                  Storage.CurseForgeOffset < Storage.CurseForgeTotal OrElse Storage.ModrinthOffset < Storage.ModrinthTotal
            LabPage.Text = Page + 1
            BtnPageFirst.IsEnabled = Page > 1
            BtnPageFirst.Opacity = If(Page > 1, 1, 0.2)
            BtnPageLeft.IsEnabled = Page > 0
            BtnPageLeft.Opacity = If(Page > 0, 1, 0.2)
            Dim IsRightEnabled As Boolean =
                Storage.Results.Count > PAGE_SIZE * (Page + 1) OrElse
                Storage.CurseForgeOffset < Storage.CurseForgeTotal OrElse Storage.ModrinthOffset < Storage.ModrinthTotal
            BtnPageRight.IsEnabled = IsRightEnabled
            BtnPageRight.Opacity = If(IsRightEnabled, 1, 0.2)
            '错误信息
            If Storage.ErrorMessage Is Nothing Then
                HintError.IsVisible = False
            Else
                HintError.IsVisible = True
                HintError.Text = Storage.ErrorMessage
            End If
            '强制返回顶部
            ScrollToHome()
        Catch ex As Exception
            Logger.Error(ex, $"可视化{TypeNameSpaced}列表出错")
        End Try
    End Sub

    '自动重试
    Private Sub Load_State() Handles Load.StateChanged
        Select Case Loader.State
            Case LoadState.Failed
                Dim ErrorMessage As String = ""
                If Loader.Error IsNot Nothing Then ErrorMessage = Loader.Error.Message
                If ErrorMessage.Contains("不是有效的 json 文件") Then
                    Logger.Warn($"下载的{TypeNameSpaced}列表 json 文件损坏，已自动重试")
                    CType(Parent, MyPageRight).PageLoaderRestart()
                End If
        End Select
    End Sub

    '切换页码
    Private Sub BtnPageFirst_Click() Handles BtnPageFirst.Click
        ChangePage(0)
    End Sub
    Private Sub BtnPageLeft_Click() Handles BtnPageLeft.Click
        ChangePage(Page - 1)
    End Sub
    Private Sub BtnPageRight_Click() Handles BtnPageRight.Click
        ChangePage(Page + 1)
    End Sub
    Private Sub ChangePage(NewPage As Integer)
        CardPages.IsEnabled = False
        Page = NewPage
        FrmMain.BackToTop()
        Logger.Info($"{TypeName}：切换到第 {Page + 1} 页")
        RunInThread(
        Sub()
            Thread.Sleep(100) '等待向上滚的动画结束
            RunInUi(Sub() CardPages.IsEnabled = True)
            Loader.Start()
        End Sub)
    End Sub

#Region "搜索"

    '搜索按钮
    Private Sub StartNewSearch() Handles BtnSearchRun.Click
        Page = 0
        If Loader.ShouldStart(LoaderInput()) Then Storage = New ResourceSearcher.SearchResult '避免连续搜索两次导致 ProjectStorage 引用丢失（#1311）
        Loader.Start()
    End Sub
    Private Sub EnterTrigger(sender As Object, e As KeyEventArgs) Handles TextSearchName.KeyDown, TextSearchVersion.KeyDown
        If e.Key = Key.Enter Then StartNewSearch()
    End Sub

    '重置按钮
    Private Sub ResetFilter() Handles BtnSearchReset.Click
        TextSearchName.Text = ""
        TextSearchVersion.Text = "全部 (也可自行输入)"
        TextSearchVersion.SelectedIndex = 0
        ComboSearchSource.SelectedIndex = 0
        ComboSearchTag.SelectedIndex = 0
        ComboSearchLoader.SelectedIndex = 0
        Loader.LastFinishedTime = 0 '要求强制重新开始
    End Sub

    '版本选择
    Private Sub TextSearchVersion_TextChanged() Handles TextSearchVersion.TextChanged
        If Not TextSearchVersion.IsDropDownOpen Then UpdateSearchLoaderVisibility()
    End Sub
    Private Sub UpdateSearchLoaderVisibility() Handles TextSearchVersion.DropDownClosed
        If PageType = ResourceTypes.Mod AndAlso (TextSearchVersion.Text.Contains(".") OrElse TextSearchVersion.Text.Contains("w")) Then
            ComboSearchLoader.IsVisible = True
            Grid.SetColumnSpan(TextSearchVersion, 1)
        Else
            ComboSearchLoader.IsVisible = False
            Grid.SetColumnSpan(TextSearchVersion, 2)
            ComboSearchLoader.SelectedIndex = 0
        End If
    End Sub

#End Region

    '安装已有整合包按钮
    Private Sub BtnSearchInstallModPack_Click() Handles BtnSearchInstallModPack.Click
        ModpackInstall()
    End Sub

End Class
