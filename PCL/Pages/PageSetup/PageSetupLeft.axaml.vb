Public Class PageSetupLeft
    Inherits MyPageLeft

    ''' <summary>
    ''' 设置页面的子页面种类。
    ''' </summary>
    Public Enum SetupPageType
        Launch = 0
        Link = 1
        UI = 2
        System = 3
    End Enum

    Friend FrmSetupLaunch As PageSetupLaunch
    Private FrmSetupUI As PageSetupUI
    Private FrmSetupLink As PageSetupLink
    Private FrmSetupSystem As PageSetupSystem
    Private IsPageInitialized As Boolean = False
    '如果在 Loaded 前切换到其他页面，会导致触发 Loaded 时再次切换一次
    Private IsPageSwitched As Boolean = False

    ''' <summary>
    ''' 当前显示的子页面。
    ''' </summary>
    Public PageID As SetupPageType

    Public Sub New()
        InitControls()
        InitResetButtons()
        PageID = GetDefaultPage()
    End Sub

    Private Shared Function GetDefaultPage() As SetupPageType
        If Not Settings.Get(Of Boolean)("UiHiddenSetupLaunch") Then Return SetupPageType.Launch
        If Not Settings.Get(Of Boolean)("UiHiddenSetupUi") Then Return SetupPageType.UI
        If Not Settings.Get(Of Boolean)("UiHiddenSetupSystem") Then Return SetupPageType.System
        If Not Settings.Get(Of Boolean)("UiHiddenSetupLink") Then Return SetupPageType.Link
        Return SetupPageType.Launch
    End Function

    Private Sub PageSetupLeft_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        '是否处于隐藏的子页面
        Dim IsHiddenPage As Boolean =
            (ItemLaunch.Checked AndAlso Settings.Get(Of Boolean)("UiHiddenSetupLaunch")) OrElse
            (ItemUI.Checked AndAlso Settings.Get(Of Boolean)("UiHiddenSetupUi")) OrElse
            (ItemSystem.Checked AndAlso Settings.Get(Of Boolean)("UiHiddenSetupSystem"))
        '若页面错误，或尚未加载，则继续
        If IsPageInitialized AndAlso Not IsHiddenPage Then Return
        IsPageInitialized = True
        RefreshHidden()
        '选择第一个未被禁用的子页面
        If IsPageSwitched Then Return
        If Not Settings.Get(Of Boolean)("UiHiddenSetupLaunch") Then
            ItemLaunch.SetChecked(True, False, False)
        ElseIf Not Settings.Get(Of Boolean)("UiHiddenSetupUi") Then
            ItemUI.SetChecked(True, False, False)
        ElseIf Not Settings.Get(Of Boolean)("UiHiddenSetupSystem") Then
            ItemSystem.SetChecked(True, False, False)
        Else
            ItemLaunch.SetChecked(True, False, False)
        End If
    End Sub

    Private Sub PageSetupLeft_Unloaded(sender As Object, e As RoutedEventArgs) Handles Me.Unloaded
        IsPageSwitched = False
    End Sub

    ''' <summary>
    ''' 更新功能隐藏带来的显示变化。
    ''' </summary>
    Public Sub RefreshHidden()
        ItemLaunch.IsVisible = PageSetupUI.HiddenForceShow OrElse Not Settings.Get(Of Boolean)("UiHiddenSetupLaunch")
        ItemUI.IsVisible = PageSetupUI.HiddenForceShow OrElse Not Settings.Get(Of Boolean)("UiHiddenSetupUi")
        ItemSystem.IsVisible = PageSetupUI.HiddenForceShow OrElse Not Settings.Get(Of Boolean)("UiHiddenSetupSystem")
        ItemLink.IsVisible = False
        '隐藏左边选择卡
        Dim AvailableCount As Integer = 0
        If ItemLaunch.IsVisible Then AvailableCount += 1
        If ItemUI.IsVisible Then AvailableCount += 1
        If ItemSystem.IsVisible Then AvailableCount += 1
        PanItem.IsVisible = AvailableCount >= 2 OrElse PageSetupUI.HiddenForceShow
    End Sub

#Region "页面切换"

    Private Sub PageCheck(sender As Object, e As RouteEventArgs) Handles ItemLaunch.Check, ItemLink.Check, ItemUI.Check, ItemSystem.Check
        Dim NewID As SetupPageType = CInt(Val(sender.Tag))
        If NewID = PageID Then Return
        PageChange(NewID)
    End Sub

    ''' <summary>
    ''' 获取当前导航指定的右页面。
    ''' </summary>
    Public Function PageGet(ID As SetupPageType) As MyPageRight
        Select Case ID
            Case SetupPageType.Launch
                If FrmSetupLaunch Is Nothing Then FrmSetupLaunch = New PageSetupLaunch
                Return FrmSetupLaunch
            Case SetupPageType.UI
                If FrmSetupUI Is Nothing Then FrmSetupUI = New PageSetupUI
                Return FrmSetupUI
            Case SetupPageType.Link
                If FrmSetupLink Is Nothing Then FrmSetupLink = New PageSetupLink
                Return FrmSetupLink
            Case SetupPageType.System
                If FrmSetupSystem Is Nothing Then FrmSetupSystem = New PageSetupSystem
                Return FrmSetupSystem
            Case Else
                Throw New Exception("未知的设置子页面种类：" & ID)
        End Select
    End Function

    ''' <summary>
    ''' 切换现有页面。
    ''' </summary>
    Public Sub PageChange(ID As SetupPageType)
        If PageID = ID Then Return
        AniControlEnabled += 1
        IsPageSwitched = True
        Try
            PageChangeRun(PageGet(ID))
            PageID = ID
        Catch ex As Exception
            Logger.Error(ex, $"切换设置分页面失败（ID {ID}）")
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
            End Sub, 130),
            AaCode(
            Sub()
                Target.Opacity = 1
                Target.PageOnEnter()
            End Sub, 30, True)
        }, "PageLeft PageChange")
    End Sub

#End Region

#Region "初始化设置"

    Private Sub InitResetButtons()
        Const ResetLogo As String = "M530 0c287 0 521 229 521 511s-233 511-521 511c-233 0-436-151-500-368a63 63 0 0 1 44-79 65 65 0 0 1 80 43c48 162 200 276 375 276 215 0 390-171 390-383s-174-383-390-383c-103 0-199 39-270 106l21-5a63 63 0 0 1 33 123l-157 42a65 65 0 0 1-90-42l-49-183a65 65 0 1 1 126-33l6 26A524 524 0 0 1 530 0z"
        For Each Item As MyListItem In New MyListItem() {ItemLaunch, ItemLink, ItemUI, ItemSystem}
            Dim Btn As New MyIconButton With {.Tag = Item.Tag, .Logo = ResetLogo, .LogoScale = 0.9}
            ToolTip.SetTip(Btn, "初始化本页设置")
            AddHandler Btn.Click, AddressOf Reset
            Item.Buttons = New MyIconButton() {Btn}
        Next
    End Sub

    Public Sub Reset(sender As Object, e As EventArgs)
        Select Case CInt(Val(sender.Tag))
            Case SetupPageType.Launch
                If MyMsgBox("是否要初始化 启动 页面的所有设置？该操作不可撤销。", "初始化确认", "确定", "取消", IsWarn:=True) = 1 Then
                    If FrmSetupLaunch Is Nothing Then FrmSetupLaunch = New PageSetupLaunch
                    FrmSetupLaunch.Reset()
                    ItemLaunch.Checked = True
                End If
            Case SetupPageType.UI
                If MyMsgBox("是否要初始化 个性化 页面的所有设置？该操作不可撤销。" & vbCrLf &
                            "（背景图片与音乐、主页等外部文件不会被删除）", "初始化确认", "确定", "取消", IsWarn:=True) = 1 Then
                    If FrmSetupUI Is Nothing Then FrmSetupUI = New PageSetupUI
                    FrmSetupUI.Reset()
                    ItemUI.Checked = True
                End If
            Case SetupPageType.System
                If MyMsgBox("是否要初始化 其他 页面的所有设置？该操作不可撤销。", "初始化确认", "确定", "取消", IsWarn:=True) = 1 Then
                    If FrmSetupSystem Is Nothing Then FrmSetupSystem = New PageSetupSystem
                    FrmSetupSystem.Reset()
                    ItemSystem.Checked = True
                End If
            Case SetupPageType.Link
                If MyMsgBox("是否要初始化 联机 页面的所有设置？该操作不可撤销。", "初始化确认", "确定", "取消", IsWarn:=True) = 1 Then
                    If FrmSetupLink Is Nothing Then FrmSetupLink = New PageSetupLink
                    FrmSetupLink.Reset()
                    ItemLink.Checked = True
                End If
        End Select
    End Sub

#End Region

End Class
