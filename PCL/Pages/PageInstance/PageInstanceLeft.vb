''' <summary>
''' 版本设置页面的左侧导航。
''' </summary>
Public Class PageInstanceLeft
    Inherits MyPageLeft
    Implements IRefreshable

    ''' <summary>
    ''' 当前显示设置的 MC 版本。
    ''' </summary>
    Public Shared Instance As McInstance = Nothing

    Private FrmInstanceOverall As PageInstanceOverall
    Private FrmInstanceMod As PageInstanceMod
    Private FrmInstanceModDisabled As PageInstanceModDisabled
    Private FrmInstanceSetup As PageInstanceSetup
    Private FrmInstanceExport As PageInstanceExport
    Private FrmDownloadModify As PageDownloadModify
    Private IsUpdatingSelection As Boolean

    Public Sub New()
        InitControls()
        InitButtons()
        ItemOverall.SetChecked(True, False, False)
    End Sub

    Private Sub InitButtons()
        Const RefreshLogo As String = "M512.0 838.3c-80.2 0-153.4-29.3-210.2-77.4l75.5-75.5c11.5-11.5 25.8-22.0 25.8-37.0a27.2 27.2 0 0 0-27.1-27.1H104.0c-27.1 0-27.1 23.9-27.1 27.1v271.9a27.1 27.1 0 0 0 27.1 27.1c15.0 0 27.8-16.6 42.5-31.2l77.9-77.9c76.6 67.7 177.1 108.9 287.4 108.9 221.7 0 404.5-166.0 431.2-380.6h-109.8c-25.9 154.2-159.7 271.9-321.3 271.9zM919.9 76.6c-15.0 0-27.8 16.6-42.5 31.3L799.5 185.8c-76.5-67.7-177.1-108.9-287.4-108.9-221.8 0-404.5 166.1-431.3 380.6H190.6c25.9-154.2 159.7-271.9 321.4-271.9 80.2 0 153.4 29.3 210.1 77.4l-75.5 75.5c-11.6 11.5-25.8 22.0-25.8 37.1a27.2 27.2 0 0 0 27.1 27.1h271.9c27.1 0 27.1-23.9 27.1-27.1V103.8a27.1 27.1 0 0 0-27.1-27.1z"
        Dim BtnReset As New MyIconButton With {
            .Tag = FormMain.PageSubType.InstanceSetup,
            .Logo = "M530 0c287 0 521 229 521 511s-233 511-521 511c-233 0-436-151-500-368a63 63 0 0 1 44-79 65 65 0 0 1 80 43c48 162 200 276 375 276 215 0 390-171 390-383s-174-383-390-383c-103 0-199 39-270 106l21-5a63 63 0 0 1 33 123l-157 42a65 65 0 0 1-90-42l-49-183a65 65 0 1 1 126-33l6 26A524 524 0 0 1 530 0z",
            .LogoScale = 0.9}
        ToolTip.SetTip(BtnReset, "初始化")
        AddHandler BtnReset.Click, AddressOf Reset
        ItemSetup.Buttons = {BtnReset}
        ItemModify.Buttons = {CreateRefreshButton(FormMain.PageSubType.InstanceModify, RefreshLogo)}
        ItemMod.Buttons = {CreateRefreshButton(FormMain.PageSubType.InstanceMod, RefreshLogo)}
        ItemExport.Buttons = {CreateRefreshButton(FormMain.PageSubType.InstanceExport, RefreshLogo)}
    End Sub

    Private Function CreateRefreshButton(SubType As FormMain.PageSubType, LogoPath As String) As MyIconButton
        Dim Btn As New MyIconButton With {.Tag = SubType, .Logo = LogoPath, .LogoScale = 0.85}
        ToolTip.SetTip(Btn, "刷新")
        AddHandler Btn.Click, AddressOf Refresh_Click
        Return Btn
    End Function

    Private Sub PageInstanceLeft_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        PageSetupUI.HiddenRefresh()
        RefreshModDisabled()
        ReloadCurrentJava()
    End Sub

    Public Sub RefreshModDisabled() Handles Me.Loaded
        If Instance IsNot Nothing AndAlso Instance.Modable Then
            ItemMod.IsVisible = True
            ItemModDisabled.IsVisible = False
        Else
            ItemMod.IsVisible = False
            ItemModDisabled.IsVisible = True
        End If
    End Sub

#Region "页面切换"

    ''' <summary>
    ''' 当前页面的编号。从 0 开始计算。
    ''' </summary>
    Public PageID As FormMain.PageSubType = FormMain.PageSubType.Default

    ''' <summary>
    ''' 勾选事件改变页面。
    ''' </summary>
    Private Sub PageCheck(sender As Object, e As RouteEventArgs) Handles ItemOverall.Check, ItemMod.Check, ItemModDisabled.Check, ItemSetup.Check, ItemModify.Check, ItemExport.Check
        If IsUpdatingSelection Then Return
        '尚未初始化控件属性时，sender.Tag 为 Nothing，会导致切换到页面 0
        If sender.Tag IsNot Nothing Then PageChange(CInt(Val(sender.Tag)))
    End Sub

    Public Function PageGet(Optional ID As FormMain.PageSubType = -1) As MyPageRight
        If ID = -1 Then ID = PageID
        Select Case ID
            Case FormMain.PageSubType.InstanceOverall
                If FrmInstanceOverall Is Nothing Then FrmInstanceOverall = New PageInstanceOverall
                Return FrmInstanceOverall
            Case FormMain.PageSubType.InstanceMod
                If FrmInstanceMod Is Nothing Then FrmInstanceMod = New PageInstanceMod
                Return FrmInstanceMod
            Case FormMain.PageSubType.InstanceModDisabled
                If FrmInstanceModDisabled Is Nothing Then FrmInstanceModDisabled = New PageInstanceModDisabled
                Return FrmInstanceModDisabled
            Case FormMain.PageSubType.InstanceSetup
                If FrmInstanceSetup Is Nothing Then FrmInstanceSetup = New PageInstanceSetup
                Return FrmInstanceSetup
            Case FormMain.PageSubType.InstanceExport
                If FrmInstanceExport Is Nothing Then FrmInstanceExport = New PageInstanceExport
                Return FrmInstanceExport
            Case FormMain.PageSubType.InstanceModify
                If FrmDownloadModify Is Nothing Then FrmDownloadModify = New PageDownloadModify
                Return FrmDownloadModify
            Case Else
                Throw New Exception("未知的版本设置子页面种类：" & ID)
        End Select
    End Function

    ''' <summary>
    ''' 切换现有页面。
    ''' </summary>
    Public Sub PageChange(ID As FormMain.PageSubType)
        If PageID = ID Then
            RefreshSelection(ID)
            Return
        End If
        If Instance IsNot Nothing AndAlso McInstanceLock.IsLocked(Instance) Then
            RefreshSelection(PageID)
            Hint("该版本正在被修改，请稍后再切换页面！", HintType.Red)
            Return
        End If
        AniControlEnabled += 1
        Try
            PageChangeRun(PageGet(ID))
            PageID = ID
        Catch ex As Exception
            Logger.Error(ex, $"切换分页面失败（ID {ID}）")
        Finally
            AniControlEnabled -= 1
            RefreshSelection(PageID)
        End Try
    End Sub

    Private Sub RefreshSelection(ID As FormMain.PageSubType)
        IsUpdatingSelection = True
        Try
            For Each Entry As MyListItem In {ItemOverall, ItemSetup, ItemModify, ItemMod, ItemModDisabled, ItemExport}
                Entry.SetChecked(CInt(Val(Entry.Tag)) = CInt(ID), False, False)
            Next
        Finally
            IsUpdatingSelection = False
        End Try
    End Sub
    Private Shared Sub PageChangeRun(Target As MyPageRight)
        If FrmMain Is Nothing Then Return
        If FrmMain.PageRight Is Target Then Return
        AniStop("FrmMain PageChangeRight") '停止主页面的右页面切换动画，防止它与本动画一起触发多次 PageOnEnter
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
                '延迟触发页面通用动画，以使得在 Loaded 事件中加载的控件得以处理
                Target.Opacity = 1
                Target.PageOnEnter()
            End Sub, 30, True)
        }, "PageLeft PageChange")
    End Sub

#End Region

    Public Sub Refresh_Click(sender As Object, e As EventArgs) '由边栏按钮匿名调用
        Refresh(CInt(Val(sender.Tag)))
    End Sub
    Public Sub Refresh() Implements IRefreshable.Refresh
        Refresh(FrmMain.PageCurrentSub)
    End Sub
    Public Sub Refresh(SubType As FormMain.PageSubType)
        If Instance IsNot Nothing AndAlso McInstanceLock.IsLocked(Instance) Then
            Hint("该版本正在被修改，请稍后再刷新！", HintType.Red)
            Return
        End If
        Select Case SubType
            Case FormMain.PageSubType.InstanceMod
                PageInstanceMod.Refresh()
                ItemMod.SetChecked(True, False, False)
            Case FormMain.PageSubType.InstanceExport
                FrmInstanceExport?.RefreshAll()
                ItemExport.SetChecked(True, False, False)
            Case FormMain.PageSubType.InstanceModify
                FrmDownloadModify?.PageLoaderRestart()
                ItemModify.SetChecked(True, False, False)
        End Select
    End Sub

    Public Sub Reset(sender As Object, e As EventArgs)
        If MyMsgBox("是否要初始化该版本的版本独立设置？该操作不可撤销。", "初始化确认",, "取消", IsWarn:=True) = 1 Then
            If FrmInstanceSetup Is Nothing Then FrmInstanceSetup = New PageInstanceSetup
            FrmInstanceSetup.Reset()
            ItemSetup.SetChecked(True, False, False)
        End If
    End Sub

    '加载当前版本将要选择的 Java
    Public Shared CurrentJavaWorker As New RedoableWorker(Of Java)(Function(c, p) SelectOrDownloadJava(PageInstanceLeft.Instance, False, c, p))
    Public Shared Sub ReloadCurrentJava()
        If AniControlEnabled <> 0 Then Return
        If PageInstanceLeft.Instance Is Nothing Then Return
        CurrentJavaWorker.Start()
    End Sub

End Class
