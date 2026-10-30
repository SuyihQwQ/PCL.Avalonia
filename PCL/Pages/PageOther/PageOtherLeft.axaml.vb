''' <summary>
''' “更多”页面的左侧导航。
''' </summary>
Public Class PageOtherLeft
    Inherits MyPageLeft

    Private FrmOtherHelp As PageOtherHelp
    Private FrmOtherAbout As PageOtherAbout
    Private FrmOtherTest As PageOtherTest
    Private IsPageInitialized As Boolean = False
    '如果在 Loaded 前切换到其他页面，会导致触发 Loaded 时再次切换一次
    Private IsPageSwitched As Boolean = False

    ''' <summary>
    ''' 当前显示的子页面。
    ''' </summary>
    Public PageID As FormMain.PageSubType

    Public Sub New()
        InitControls()
        InitHelpRefreshButton()
        PageID = GetDefaultPage()
    End Sub

    Private Shared Function GetDefaultPage() As FormMain.PageSubType
        If Not Settings.Get(Of Boolean)("UiHiddenOtherHelp") Then Return FormMain.PageSubType.OtherHelp
        If Not Settings.Get(Of Boolean)("UiHiddenOtherAbout") Then Return FormMain.PageSubType.OtherAbout
        Return FormMain.PageSubType.OtherTest
    End Function

    Private Sub InitHelpRefreshButton()
        Const RefreshLogo As String = "M512.0 838.3c-80.2 0-153.4-29.3-210.2-77.4l75.5-75.5c11.5-11.5 25.8-22.0 25.8-37.0a27.2 27.2 0 0 0-27.1-27.1H104.0c-27.1 0-27.1 23.9-27.1 27.1v271.9a27.1 27.1 0 0 0 27.1 27.1c15.0 0 27.8-16.6 42.5-31.2l77.9-77.9c76.6 67.7 177.1 108.9 287.4 108.9 221.7 0 404.5-166.0 431.2-380.6h-109.8c-25.9 154.2-159.7 271.9-321.3 271.9zM919.9 76.6c-15.0 0-27.8 16.6-42.5 31.3L799.5 185.8c-76.5-67.7-177.1-108.9-287.4-108.9-221.8 0-404.5 166.1-431.3 380.6H190.6c25.9-154.2 159.7-271.9 321.4-271.9 80.2 0 153.4 29.3 210.1 77.4l-75.5 75.5c-11.6 11.5-25.8 22.0-25.8 37.1a27.2 27.2 0 0 0 27.1 27.1h271.9c27.1 0 27.1-23.9 27.1-27.1V103.8a27.1 27.1 0 0 0-27.1-27.1z"
        Dim Btn As New MyIconButton With {.Tag = FormMain.PageSubType.OtherHelp, .Logo = RefreshLogo, .LogoScale = 0.85}
        ToolTip.SetTip(Btn, "刷新")
        AddHandler Btn.Click, AddressOf Refresh
        ItemHelp.Buttons = New MyIconButton() {Btn}
    End Sub

    Private Sub PageOtherLeft_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        '是否处于隐藏的子页面
        Dim IsHiddenPage As Boolean = False
        If ItemHelp.Checked AndAlso Settings.Get(Of Boolean)("UiHiddenOtherHelp") Then IsHiddenPage = True
        If ItemAbout.Checked AndAlso Settings.Get(Of Boolean)("UiHiddenOtherAbout") Then IsHiddenPage = True
        If ItemTest.Checked AndAlso Settings.Get(Of Boolean)("UiHiddenOtherTest") Then IsHiddenPage = True
        If PageSetupUI.HiddenForceShow Then IsHiddenPage = False
        '若页面错误，或尚未加载，则继续
        If IsPageInitialized AndAlso Not IsHiddenPage Then Return
        IsPageInitialized = True
        '刷新子页面隐藏情况
        PageSetupUI.HiddenRefresh()
        '选择第一个未被禁用的子页面
        If IsPageSwitched Then Return
        If Not Settings.Get(Of Boolean)("UiHiddenOtherHelp") Then
            ItemHelp.SetChecked(True, False, False)
        ElseIf Not Settings.Get(Of Boolean)("UiHiddenOtherAbout") Then
            ItemAbout.SetChecked(True, False, False)
        Else
            ItemTest.SetChecked(True, False, False)
        End If
    End Sub

    Private Sub PageOtherLeft_Unloaded(sender As Object, e As RoutedEventArgs) Handles Me.Unloaded
        IsPageSwitched = False
    End Sub

#Region "页面切换"

    ''' <summary>
    ''' 勾选事件改变页面。
    ''' </summary>
    Private Sub PageCheck(sender As Object, e As RouteEventArgs) Handles ItemHelp.Check, ItemAbout.Check, ItemTest.Check
        '尚未初始化控件属性时，sender.Tag 为 Nothing
        If sender.Tag IsNot Nothing Then PageChange(CInt(Val(sender.Tag)))
    End Sub

    ''' <summary>
    ''' 获取指定编号的子页面，未指定时使用当前页面。
    ''' </summary>
    Public Function PageGet(Optional ID As FormMain.PageSubType = -1) As MyPageRight
        If ID = -1 Then ID = PageID
        Select Case ID
            Case FormMain.PageSubType.OtherHelp
                If FrmOtherHelp Is Nothing Then FrmOtherHelp = New PageOtherHelp
                Return FrmOtherHelp
            Case FormMain.PageSubType.OtherAbout
                If FrmOtherAbout Is Nothing Then FrmOtherAbout = New PageOtherAbout
                Return FrmOtherAbout
            Case FormMain.PageSubType.OtherTest
                If FrmOtherTest Is Nothing Then FrmOtherTest = New PageOtherTest
                Return FrmOtherTest
            Case Else
                Throw New Exception("未知的更多子页面种类：" & ID)
        End Select
    End Function

    ''' <summary>
    ''' 切换现有页面。
    ''' </summary>
    Public Sub PageChange(ID As FormMain.PageSubType)
        If PageID = ID Then Return
        AniControlEnabled += 1
        IsPageSwitched = True
        Try
            PageChangeRun(PageGet(ID))
            PageID = ID
        Catch ex As Exception
            Logger.Error(ex, $"切换更多分页面失败（ID {ID}）")
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

    ''' <summary>
    ''' 更新功能隐藏带来的显示变化。
    ''' </summary>
    Public Sub RefreshHidden()
        ItemHelp.IsVisible = PageSetupUI.HiddenForceShow OrElse Not Settings.Get(Of Boolean)("UiHiddenOtherHelp")
        ItemAbout.IsVisible = PageSetupUI.HiddenForceShow OrElse Not Settings.Get(Of Boolean)("UiHiddenOtherAbout")
        ItemTest.IsVisible = PageSetupUI.HiddenForceShow OrElse Not Settings.Get(Of Boolean)("UiHiddenOtherTest")
        ItemFeedback.IsVisible = PageSetupUI.HiddenForceShow OrElse Not Settings.Get(Of Boolean)("UiHiddenOtherFeedback")
        ItemVote.IsVisible = PageSetupUI.HiddenForceShow OrElse Not Settings.Get(Of Boolean)("UiHiddenOtherVote")
        Dim AvailableCount As Integer = 0
        If ItemHelp.IsVisible Then AvailableCount += 1
        If ItemAbout.IsVisible Then AvailableCount += 1
        If ItemTest.IsVisible Then AvailableCount += 1
        If ItemFeedback.IsVisible Then AvailableCount += 1
        If ItemVote.IsVisible Then AvailableCount += 1
        PanItem.IsVisible = AvailableCount >= 2 OrElse PageSetupUI.HiddenForceShow
    End Sub

    '强制刷新
    Public Sub Refresh(sender As Object, e As EventArgs)
        Select Case CInt(Val(sender.Tag))
            Case FormMain.PageSubType.OtherHelp
                RefreshHelp()
                ItemHelp.Checked = True
        End Select
        Hint("正在刷新……", Log:=False)
    End Sub

    Public Shared Sub RefreshHelp()
        Settings.[Set]("SystemHelpVersion", 0) '强制重新解压文件
        If FrmMain Is Nothing OrElse FrmMain.FrmOtherLeft Is Nothing Then Return
        Dim FrmHelp = CType(FrmMain.FrmOtherLeft.PageGet(FormMain.PageSubType.OtherHelp), PageOtherHelp)
        FrmHelp.PageLoaderRestart()
        FrmHelp.SearchBox.Text = ""
    End Sub

    '打开网页
    Private Sub TryFeedback(sender As Object, e As RouteEventArgs) Handles ItemFeedback.Changed
        If Not ItemFeedback.Checked Then Return
        TryFeedback()
        e.Handled = True
    End Sub

    Public Shared Sub TryFeedback()
        If False.Equals(PageSetupSystem.IsLauncherNewest) Then
            If MyMsgBox($"你的 PCL 不是最新版，因此无法提交反馈。{vbCrLf}请在更新后，确认该问题在最新版中依然存在，然后再提交反馈。", "无法提交反馈", "更新", "取消") = 1 Then
                UpdateCheckByButton()
            End If
            Return
        End If
        Select Case MyMsgBox("在提交新反馈前，建议先搜索反馈列表，以避免重复提交。" & vbCrLf & "如果无法打开该网页，请使用 VPN 改善网络环境。",
                    "反馈", "提交新反馈", "查看反馈列表", "取消")
            Case 1
                Hint("正在打开新反馈页面……")
                OpenWebsite("https://github.com/Meloong-Git/PCL/issues/new/choose")
            Case 2
                OpenWebsite("https://github.com/Meloong-Git/PCL/issues/")
        End Select
    End Sub

    Private Sub TryVote(sender As Object, e As RouteEventArgs) Handles ItemVote.Changed
        If Not ItemVote.Checked Then Return
        TryVote()
        e.Handled = True
    End Sub

    Public Shared Sub TryVote()
        If MyMsgBox("是否要打开新功能投票网页？" & vbCrLf & "如果无法打开该网页，请使用 VPN 改善网络环境。",
                    "新功能投票", "打开", "取消") = 2 Then Return
        OpenWebsite("https://github.com/Meloong-Git/PCL/discussions/categories/%E5%8A%9F%E8%83%BD%E6%8A%95%E7%A5%A8?discussions_q=category%3A%E5%8A%9F%E8%83%BD%E6%8A%95%E7%A5%A8+sort%3Adate_created")
    End Sub

End Class
