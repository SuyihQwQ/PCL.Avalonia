''' <summary>
''' “下载”页面的左侧导航。
''' </summary>
Public Class PageDownloadLeft
    Inherits MyPageLeft
    Implements IRefreshable

    ''' <summary>
    ''' 当前显示的子页面。
    ''' </summary>
    Public PageID As FormMain.PageSubType = FormMain.PageSubType.DownloadInstall

    Private FrmDownloadInstall As PageDownloadInstall
    Private FrmDownloadMod As PageDownloadMod
    Private FrmDownloadPack As PageDownloadPack
    Private FrmDownloadDataPack As PageDownloadDataPack
    Private FrmDownloadResourcePack As PageDownloadResourcePack
    Private FrmDownloadShader As PageDownloadShader

    Public Sub New()
        InitControls()
        InitRefreshButtons()
        ItemInstall.SetChecked(True, False, False)
    End Sub

    Private Sub InitRefreshButtons()
        ItemInstall.Buttons = CreateRefreshButton(FormMain.PageSubType.DownloadInstall)
        ItemMod.Buttons = CreateRefreshButton(FormMain.PageSubType.DownloadMod)
        ItemPack.Buttons = CreateRefreshButton(FormMain.PageSubType.DownloadPack)
        ItemDataPack.Buttons = CreateRefreshButton(FormMain.PageSubType.DownloadDataPack)
        ItemResourcePack.Buttons = CreateRefreshButton(FormMain.PageSubType.DownloadResourcePack)
        ItemShader.Buttons = CreateRefreshButton(FormMain.PageSubType.DownloadShader)
    End Sub

    Private Function CreateRefreshButton(SubType As FormMain.PageSubType) As IEnumerable(Of MyIconButton)
        Const RefreshLogo As String = "M512.0 838.3c-80.2 0-153.4-29.3-210.2-77.4l75.5-75.5c11.5-11.5 25.8-22.0 25.8-37.0a27.2 27.2 0 0 0-27.1-27.1H104.0c-27.1 0-27.1 23.9-27.1 27.1v271.9a27.1 27.1 0 0 0 27.1 27.1c15.0 0 27.8-16.6 42.5-31.2l77.9-77.9c76.6 67.7 177.1 108.9 287.4 108.9 221.7 0 404.5-166.0 431.2-380.6h-109.8c-25.9 154.2-159.7 271.9-321.3 271.9zM919.9 76.6c-15.0 0-27.8 16.6-42.5 31.3L799.5 185.8c-76.5-67.7-177.1-108.9-287.4-108.9-221.8 0-404.5 166.1-431.3 380.6H190.6c25.9-154.2 159.7-271.9 321.4-271.9 80.2 0 153.4 29.3 210.1 77.4l-75.5 75.5c-11.6 11.5-25.8 22.0-25.8 37.1a27.2 27.2 0 0 0 27.1 27.1h271.9c27.1 0 27.1-23.9 27.1-27.1V103.8a27.1 27.1 0 0 0-27.1-27.1z"
        Dim Btn As New MyIconButton With {.Tag = SubType, .Logo = RefreshLogo, .LogoScale = 0.85}
        ToolTip.SetTip(Btn, "刷新")
        AddHandler Btn.Click, AddressOf Refresh_Click
        Return New MyIconButton() {Btn}
    End Function

#Region "页面切换"

    ''' <summary>
    ''' 勾选事件改变页面。
    ''' </summary>
    Private Sub PageCheck(sender As Object, e As RouteEventArgs) Handles ItemInstall.Check, ItemMod.Check, ItemPack.Check,
            ItemDataPack.Check, ItemResourcePack.Check, ItemShader.Check
        '尚未初始化控件属性时，sender.Tag 为 Nothing
        If sender.Tag IsNot Nothing Then PageChange(CInt(Val(sender.Tag)))
    End Sub

    '再次点击“安装”时返回安装初始页面
    Private Sub ItemInstall_Click(sender As Object, e As PointerReleasedEventArgs) Handles ItemInstall.Click
        If Not ItemInstall.Checked Then Return
        FrmDownloadInstall?.ExitSelectPage()
    End Sub

    ''' <summary>
    ''' 获取指定编号的子页面，未指定时使用当前页面。
    ''' </summary>
    Public Function PageGet(Optional ID As FormMain.PageSubType = -1) As MyPageRight
        If ID = -1 Then ID = PageID
        Select Case ID
            Case FormMain.PageSubType.DownloadInstall
                If FrmDownloadInstall Is Nothing Then FrmDownloadInstall = New PageDownloadInstall
                Return FrmDownloadInstall
            Case FormMain.PageSubType.DownloadMod
                If FrmDownloadMod Is Nothing Then FrmDownloadMod = New PageDownloadMod
                Return FrmDownloadMod
            Case FormMain.PageSubType.DownloadPack
                If FrmDownloadPack Is Nothing Then FrmDownloadPack = New PageDownloadPack
                Return FrmDownloadPack
            Case FormMain.PageSubType.DownloadDataPack
                If FrmDownloadDataPack Is Nothing Then FrmDownloadDataPack = New PageDownloadDataPack
                Return FrmDownloadDataPack
            Case FormMain.PageSubType.DownloadResourcePack
                If FrmDownloadResourcePack Is Nothing Then FrmDownloadResourcePack = New PageDownloadResourcePack
                Return FrmDownloadResourcePack
            Case FormMain.PageSubType.DownloadShader
                If FrmDownloadShader Is Nothing Then FrmDownloadShader = New PageDownloadShader
                Return FrmDownloadShader
            Case Else
                Throw New Exception("未知的下载子页面种类：" & ID)
        End Select
    End Function

    ''' <summary>
    ''' 切换现有页面。
    ''' </summary>
    Public Sub PageChange(ID As FormMain.PageSubType)
        If PageID = ID Then Return
        AniControlEnabled += 1
        Try
            PageChangeRun(PageGet(ID))
            PageID = ID
        Catch ex As Exception
            Logger.Error(ex, $"切换下载分页面失败（ID {ID}）")
        Finally
            AniControlEnabled -= 1
        End Try
    End Sub

    Private Shared Sub PageChangeRun(Target As MyPageRight)
        If FrmMain Is Nothing Then Return
        If FrmMain.PageRight Is Target Then Return
        AniStop("FrmMain PageChangeRight")
        AniStop("PageLeft PageChange")
        If TypeOf Target.Parent Is ContentControl Then
            DirectCast(Target.Parent, ContentControl).Content = Nothing
        End If
        FrmMain.PageRight = Target
        Dim OldPage = TryCast(FrmMain.PanMainRight.Child, MyPageRight)
        If OldPage IsNot Nothing Then OldPage.PageOnExit()
        AniStart({
            AaCode(
            Sub()
                Dim Previous = TryCast(FrmMain.PanMainRight.Child, MyPageRight)
                If Previous IsNot Nothing Then Previous.PageOnForceExit()
                FrmMain.PanMainRight.Child = Target
                Target.Opacity = 0
                FrmMain.BtnExtraBack.ShowRefresh()
            End Sub, 130),
            AaCode(
            Sub()
                Target.Opacity = 1
                Target.PageOnEnter()
            End Sub, 30, True)
        }, "PageLeft PageChange")
    End Sub

#End Region

    '强制刷新
    Public Sub Refresh_Click(sender As Object, e As EventArgs)
        Refresh(CInt(Val(sender.Tag)))
    End Sub

    Public Sub Refresh() Implements IRefreshable.Refresh
        Refresh(FrmMain.PageCurrentSub)
    End Sub

    Public Sub Refresh(SubType As FormMain.PageSubType)
        ResourceProject.Cache.Clear()
        ResourceVersion.ProjectFilesCache.Clear()
        Select Case SubType
            Case FormMain.PageSubType.DownloadInstall
                DlClientListLoader.Start(IsForceRestart:=True)
                DlOptiFineListLoader.Start(IsForceRestart:=True)
                DlForgeListLoader.Start(IsForceRestart:=True)
                DlNeoForgeListLoader.Start(IsForceRestart:=True)
                DlLiteLoaderListLoader.Start(IsForceRestart:=True)
                DlFabricListLoader.Start(IsForceRestart:=True)
                DlFabricApiLoader.Start(IsForceRestart:=True)
                DlOptiFabricLoader.Start(IsForceRestart:=True)
                ItemInstall.SetChecked(True, False, False)
            Case FormMain.PageSubType.DownloadMod
                If FrmDownloadMod IsNot Nothing Then
                    FrmDownloadMod.Content.Storage = New ResourceSearcher.SearchResult
                    FrmDownloadMod.Content.Page = 0
                    FrmDownloadMod.PageLoaderRestart()
                End If
                ItemMod.SetChecked(True, False, False)
            Case FormMain.PageSubType.DownloadPack
                If FrmDownloadPack IsNot Nothing Then
                    FrmDownloadPack.Content.Storage = New ResourceSearcher.SearchResult
                    FrmDownloadPack.Content.Page = 0
                    FrmDownloadPack.PageLoaderRestart()
                End If
                ItemPack.SetChecked(True, False, False)
            Case FormMain.PageSubType.DownloadDataPack
                If FrmDownloadDataPack IsNot Nothing Then
                    FrmDownloadDataPack.Content.Storage = New ResourceSearcher.SearchResult
                    FrmDownloadDataPack.Content.Page = 0
                    FrmDownloadDataPack.PageLoaderRestart()
                End If
                ItemDataPack.SetChecked(True, False, False)
            Case FormMain.PageSubType.DownloadResourcePack
                If FrmDownloadResourcePack IsNot Nothing Then
                    FrmDownloadResourcePack.Content.Storage = New ResourceSearcher.SearchResult
                    FrmDownloadResourcePack.Content.Page = 0
                    FrmDownloadResourcePack.PageLoaderRestart()
                End If
                ItemResourcePack.SetChecked(True, False, False)
            Case FormMain.PageSubType.DownloadShader
                If FrmDownloadShader IsNot Nothing Then
                    FrmDownloadShader.Content.Storage = New ResourceSearcher.SearchResult
                    FrmDownloadShader.Content.Page = 0
                    FrmDownloadShader.PageLoaderRestart()
                End If
                ItemShader.SetChecked(True, False, False)
        End Select
        Hint("正在刷新……", Log:=False)
    End Sub

End Class
