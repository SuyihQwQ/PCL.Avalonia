Imports Avalonia.Controls
Imports Avalonia.Input
Imports Avalonia.Media.Imaging
Imports Avalonia.VisualTree

Public Class FormMain
    Inherits Window

    'Avalonia 的 XAML 不能给 Transform 命名，故在代码中维护
    Private ReadOnly TransformRotate As New RotateTransform(-4)
    Private ReadOnly TransformPos As New TranslateTransform(0, 60)
    Private ReadOnly TransformScale As New ScaleTransform(1, 1)

    '页面宿主
    Public PageLeft As MyPageLeft
    Public PageRight As MyPageRight
    Friend FrmOtherLeft As PageOtherLeft
    Friend FrmSetupLeft As PageSetupLeft
    Friend FrmDownloadLeft As PageDownloadLeft
    Friend FrmDownloadResourceDetail As PageDownloadResourceDetail
    Private CurrentTab As Integer = 4
    Private IsChangingPage As Boolean = False

    '次级页面（帮助详情）状态
    Private HelpDetailActive As Boolean = False
    Private HelpDetailPreviousPage As MyPageRight
    Private HelpDetailPreviousTab As Integer = 4

    Private Class HelpDetailFrame
        Public Page As MyPageRight
        Public Title As String
        Public Tab As Integer
    End Class
    Private ReadOnly HelpDetailHistory As New List(Of HelpDetailFrame)

    ''' <summary>
    ''' 次要页面种类。其数值必须与 StackPanel 中的下标一致。
    ''' 该枚举在自定义事件中使用，是公开 API 的一部分。
    ''' </summary>
    Public Enum PageSubType
        [Default] = 0
        DownloadInstall = 0
        DownloadMod = 2
        DownloadPack = 3
        DownloadDataPack = 4
        DownloadResourcePack = 5
        DownloadShader = 6
        SetupLaunch = 0
        SetupLink = 1
        SetupUI = 2
        SetupSystem = 3
        LinkMain = 0
        OtherHelp = 0
        OtherAbout = 1
        OtherTest = 2
        InstanceOverall = 0
        InstanceSetup = 1
        InstanceModify = 2
        InstanceMod = 3
        InstanceModDisabled = 4
        InstanceExport = 5
    End Enum

    ''' <summary>
    ''' 页面种类。
    ''' 该枚举在自定义事件中使用，是公开 API 的一部分。
    ''' </summary>
    Public Enum PageType
        ''' <summary>
        ''' 启动。
        ''' </summary>
        Launch = 0
        ''' <summary>
        ''' 下载。
        ''' </summary>
        Download = 1
        ''' <summary>
        ''' 联机。
        ''' </summary>
        Link = 2
        ''' <summary>
        ''' 设置。
        ''' </summary>
        Setup = 3
        ''' <summary>
        ''' 更多。
        ''' </summary>
        Other = 4
        ''' <summary>
        ''' 版本选择。这是一个副页面。
        ''' </summary>
        InstanceSelect = 5
        ''' <summary>
        ''' 任务管理。这是一个副页面。
        ''' </summary>
        TaskManager = 6
        ''' <summary>
        ''' 版本设置。这是一个副页面。
        ''' </summary>
        InstanceSetup = 7
        ''' <summary>
        ''' 社区资源详情。这是一个副页面。
        ''' </summary>
        ResourceDetail = 8
        ''' <summary>
        ''' 帮助详情。这是一个副页面。
        ''' </summary>
        HelpDetail = 9
    End Enum

    ''' <summary>
    ''' 页面状态。
    ''' </summary>
    Public Class PageStackData

        Public Page As PageType
        Public Additional As Object

        Public Overrides Function Equals(other As Object) As Boolean
            If other Is Nothing Then Return False
            If TypeOf other Is PageStackData Then
                Dim PageOther As PageStackData = other
                If Page <> PageOther.Page Then Return False
                If Additional Is Nothing Then
                    Return PageOther.Additional Is Nothing
                Else
                    Return PageOther.Additional IsNot Nothing AndAlso Additional.Equals(PageOther.Additional)
                End If
            ElseIf TypeOf other Is Integer Then
                If Page <> other Then Return False
                Return Additional Is Nothing
            Else
                Return False
            End If
        End Function
        Public Shared Operator =(left As PageStackData, right As PageStackData) As Boolean
            Return EqualityComparer(Of PageStackData).Default.Equals(left, right)
        End Operator
        Public Shared Operator <>(left As PageStackData, right As PageStackData) As Boolean
            Return Not left = right
        End Operator
        Public Shared Widening Operator CType(Value As PageType) As PageStackData
            Return New PageStackData With {.Page = Value}
        End Operator
        Public Shared Widening Operator CType(Value As PageStackData) As PageType
            Return Value.Page
        End Operator
    End Class

    ''' <summary>
    ''' 上层页面的编号堆栈，用于返回。
    ''' </summary>
    Public Property PageStack As New List(Of PageStackData)

    ''' <summary>
    ''' 获取次级页面的名称。若并非次级页面则返回空字符串，故可以以此判断是否为次级页面。
    ''' </summary>
    Private Function PageNameGet(Stack As PageStackData) As String
        If Stack Is Nothing Then Return ""
        Select Case Stack.Page
            Case PageType.InstanceSelect
                Return "版本选择"
            Case PageType.TaskManager
                Return "任务管理"
            Case PageType.InstanceSetup
                If FrmInstanceLeft Is Nothing Then FrmInstanceLeft = New PageInstanceLeft
                Return "版本设置 - " & If(PageInstanceLeft.Instance Is Nothing, "未知版本", PageInstanceLeft.Instance.Name)
            Case PageType.ResourceDetail
                Return CType(Stack.Additional(0), ResourceProject).TranslatedName
            Case PageType.HelpDetail
                Return CType(Stack.Additional(0), HelpEntry).Title
            Case Else
                Return ""
        End Select
    End Function

    ''' <summary>
    ''' 刷新次级页面的名称。
    ''' </summary>
    Public Sub PageNameRefresh(Type As PageStackData)
        LabTitleInner.Text = PageNameGet(Type)
    End Sub

    ''' <summary>
    ''' 刷新次级页面的名称。
    ''' </summary>
    Public Sub PageNameRefresh()
        PageNameRefresh(PageCurrent)
    End Sub

    ''' <summary>
    ''' 当前的主页面。
    ''' </summary>
    Public PageCurrent As PageStackData = PageType.Launch
    ''' <summary>
    ''' 上一个主页面。
    ''' </summary>
    Public PageLast As PageStackData = PageType.Launch
    ''' <summary>
    ''' 当前的子页面。
    ''' </summary>
    Public ReadOnly Property PageCurrentSub As PageSubType
        Get
            Select Case PageCurrent.Page
                Case PageType.Download
                    If FrmDownloadLeft Is Nothing Then FrmDownloadLeft = New PageDownloadLeft
                    Return FrmDownloadLeft.PageID
                Case PageType.Setup
                    If FrmSetupLeft Is Nothing Then FrmSetupLeft = New PageSetupLeft
                    Return CType(CInt(FrmSetupLeft.PageID), PageSubType)
                Case PageType.Other
                    If FrmOtherLeft Is Nothing Then FrmOtherLeft = New PageOtherLeft
                    Return FrmOtherLeft.PageID
                Case PageType.InstanceSetup
                    If FrmInstanceLeft Is Nothing Then FrmInstanceLeft = New PageInstanceLeft
                    Return FrmInstanceLeft.PageID
                Case Else
                    Return 0 '没有子页面
            End Select
        End Get
    End Property

    ''' <summary>
    ''' 切换页面，并引起对应选择 UI 的改变。
    ''' </summary>
    Public Sub PageChange(Stack As PageStackData, Optional SubType As PageSubType = PageSubType.Default)
        If Stack Is Nothing Then Return
        PageChangeActual(Stack, SubType)
    End Sub

    ''' <summary>
    ''' 切换现有页面的实际方法。
    ''' </summary>
    Private Sub PageChangeActual(Stack As PageStackData, Optional SubType As PageSubType = CType(-1, PageSubType))
        If Stack Is Nothing Then Return
        If IsChangingPage Then Return
        If PageCurrent = Stack AndAlso (PageCurrentSub = SubType OrElse SubType = CType(-1, PageSubType)) Then Return
        If Stack.Page = PageType.InstanceSetup AndAlso PageInstanceLeft.Instance IsNot Nothing AndAlso McInstanceLock.IsLocked(PageInstanceLeft.Instance) Then
            Hint("该版本正在被修改，暂时无法进入版本设置！", HintType.Red)
            Return
        End If
        If HelpDetailActive Then CloseHelpDetailChrome(False)
        AniControlEnabled += 1
        Try

#Region "子页面处理"

            Dim PageName As String = PageNameGet(Stack)
            If PageName = "" Then
                '即将切换到一个顶级页面
                PageChangeExit()
            Else
                '即将切换到一个子页面
                If PageStack.Any() Then
                    '子页面 → 另一个子页面，更新
                    AniStart({
                        AaOpacity(LabTitleInner, -LabTitleInner.Opacity, 130),
                        AaCode(Sub() LabTitleInner.Text = PageName,, True),
                        AaOpacity(LabTitleInner, 1, 150, 30)
                    }, "FrmMain Titlebar SubLayer")
                    If PageStack.Contains(Stack) Then
                        '返回到更上层的子页面
                        Do While PageStack.Contains(Stack)
                            PageStack.RemoveAt(0)
                        Loop
                    Else
                        '进入更深层的子页面
                        PageStack.Insert(0, PageCurrent)
                    End If
                Else
                    '主页面 → 子页面，进入
                    PanTitleInner.IsVisible = True
                    PanTitleMain.IsHitTestVisible = False
                    PanTitleInner.IsHitTestVisible = True
                    PageNameRefresh(Stack)
                    AniStart({
                        AaOpacity(PanTitleMain, -PanTitleMain.Opacity, 150),
                        AaX(PanTitleMain, 12 - PanTitleMain.Margin.Left, 150,, New AniEaseInFluent(AniEasePower.Weak)),
                        AaOpacity(PanTitleInner, 1 - PanTitleInner.Opacity, 150, 200),
                        AaX(PanTitleInner, -PanTitleInner.Margin.Left, 350, 200, New AniEaseOutBack),
                        AaCode(Sub() PanTitleMain.IsVisible = False,, True)
                    }, "FrmMain Titlebar FirstLayer")
                    PageStack.Insert(0, PageCurrent)
                End If
            End If

#End Region

#Region "实际更改页面框架 UI"

            PageLast = PageCurrent
            PageCurrent = Stack
            Select Case Stack.Page
                Case PageType.Launch '启动
                    If FrmLaunchLeft Is Nothing Then FrmLaunchLeft = New PageLaunchLeft
                    If FrmLaunchRight Is Nothing Then FrmLaunchRight = New PageLaunchRight
                    PageChangeAnim(FrmLaunchLeft, FrmLaunchRight, 0)
                Case PageType.Download '下载
                    If FrmDownloadLeft Is Nothing Then FrmDownloadLeft = New PageDownloadLeft
                    If SubType <> CType(-1, PageSubType) Then FrmDownloadLeft.PageChange(SubType)
                    If PageLeft IsNot FrmDownloadLeft OrElse HelpDetailActive Then
                        PageChangeAnim(FrmDownloadLeft, FrmDownloadLeft.PageGet(SubType), 1)
                    End If
                Case PageType.Link '联机
                    If FrmLinkMain Is Nothing Then FrmLinkMain = New PageLinkMain
                    PageChangeAnim(New MyPageLeft, FrmLinkMain, 2)
                Case PageType.Setup '设置
                    If FrmSetupLeft Is Nothing Then FrmSetupLeft = New PageSetupLeft
                    Dim SetupID As PageSetupLeft.SetupPageType = CType(CInt(FrmSetupLeft.PageID), PageSetupLeft.SetupPageType)
                    If SubType <> CType(-1, PageSubType) Then
                        SetupID = CType(CInt(SubType), PageSetupLeft.SetupPageType)
                        FrmSetupLeft.PageChange(SetupID)
                    End If
                    If PageLeft IsNot FrmSetupLeft OrElse HelpDetailActive Then
                        PageChangeAnim(FrmSetupLeft, FrmSetupLeft.PageGet(SetupID), 3)
                    End If
                Case PageType.Other '更多
                    If FrmOtherLeft Is Nothing Then FrmOtherLeft = New PageOtherLeft
                    If SubType <> CType(-1, PageSubType) Then FrmOtherLeft.PageChange(SubType)
                    If PageLeft IsNot FrmOtherLeft OrElse HelpDetailActive Then
                        PageChangeAnim(FrmOtherLeft, FrmOtherLeft.PageGet(SubType), 4)
                    End If
                Case PageType.InstanceSelect '版本选择
                    If FrmSelectLeft Is Nothing Then FrmSelectLeft = New PageSelectLeft
                    If FrmSelectRight Is Nothing Then FrmSelectRight = New PageSelectRight
                    PageChangeAnim(FrmSelectLeft, FrmSelectRight, CurrentTab)
                Case PageType.TaskManager '任务管理
                    If FrmSpeedLeft Is Nothing Then FrmSpeedLeft = New PageSpeedLeft
                    If FrmSpeedRight Is Nothing Then FrmSpeedRight = New PageSpeedRight
                    PageChangeAnim(FrmSpeedLeft, FrmSpeedRight, CurrentTab)
                Case PageType.InstanceSetup '版本设置
                    If FrmInstanceLeft Is Nothing Then FrmInstanceLeft = New PageInstanceLeft
                    If SubType <> CType(-1, PageSubType) Then FrmInstanceLeft.PageChange(SubType)
                    PageChangeAnim(FrmInstanceLeft, FrmInstanceLeft.PageGet(SubType), CurrentTab)
                Case PageType.ResourceDetail '社区资源详情
                    If FrmDownloadResourceDetail Is Nothing Then FrmDownloadResourceDetail = New PageDownloadResourceDetail
                    PageChangeAnim(New MyPageLeft, FrmDownloadResourceDetail, CurrentTab)
                Case PageType.HelpDetail '帮助详情
                    PageChangeAnim(New MyPageLeft, CType(Stack.Additional(1), MyPageRight), CurrentTab)
                Case Else
                    Hint("该页面尚未移植到 Avalonia 版本。")
                    Return
            End Select

#End Region

#Region "设置为最新状态"

            BtnExtraDownload.ShowRefresh()
            BtnExtraApril.ShowRefresh()

#End Region

            Logger.Info($"切换主要页面：{Stack.Page}, {SubType}")
        Catch ex As Exception
            Logger.Error(ex, $"切换主要页面失败（ID {PageCurrent.Page}）")
        Finally
            AniControlEnabled -= 1
        End Try
    End Sub

    ''' <summary>
    ''' 退出子界面。
    ''' </summary>
    Private Sub PageChangeExit()
        If PageStack.Any Then
            '子页面 → 主页面，退出
            PanTitleMain.IsVisible = True
            PanTitleMain.IsHitTestVisible = True
            PanTitleInner.IsHitTestVisible = False
            AniStart({
                AaOpacity(PanTitleInner, -PanTitleInner.Opacity, 150),
                AaX(PanTitleInner, -18 - PanTitleInner.Margin.Left, 150,, New AniEaseInFluent),
                AaOpacity(PanTitleMain, 1 - PanTitleMain.Opacity, 150, 200),
                AaX(PanTitleMain, -PanTitleMain.Margin.Left, 350, 200, New AniEaseOutBack(AniEasePower.Weak)),
                AaCode(Sub() PanTitleInner.IsVisible = False,, True)
            }, "FrmMain Titlebar FirstLayer")
            PageStack.Clear()
        Else
            '主页面 → 主页面，无事发生
        End If
    End Sub

    ''' <summary>
    ''' 当前显示的背景图片。
    ''' </summary>
    Private _BackgroundImage As Bitmap

    Public Sub New()
        InitControls()
        FrmMain = Me
        EnsurePanBackTransform()
        Opacity = 0
    End Sub

    '窗口加载
    Private Sub FormMain_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        ThemeRefreshMain()
        UpdateBackgroundAndTitleBar()
        PageSetupUI.HiddenRefresh()
        PageSetupUI.BackgroundRefresh(False, True)
        MusicRefreshPlay(False, True)
        '扩展按钮
        BtnExtraDownload.ShowCheck = AddressOf BtnExtraDownload_ShowCheck
        BtnExtraBack.ShowCheck = AddressOf BtnExtraBack_ShowCheck
        BtnExtraApril.ShowCheck = AddressOf BtnExtraApril_ShowCheck
        BtnExtraShutdown.ShowCheck = AddressOf BtnExtraShutdown_ShowCheck
        BtnExtraApril.ShowRefresh()
        InitPages()
        AniStart()
        AniStart({
            AaOpacity(Me, Settings.Get(Of Integer)("UiLauncherTransparent") / 1000 + 0.4, 250, 100),
            AaDouble(Sub(i) TransformPos.Y += i, -TransformPos.Y, 600, 100, New AniEaseOutBack(AniEasePower.Weak)),
            AaDouble(Sub(i) TransformRotate.Angle += i, -TransformRotate.Angle, 500, 100, New AniEaseOutBack(AniEasePower.Weak)),
            AaCode(
            Sub()
                PanBack.RenderTransform = Nothing
                Logger.Info("窗口加载完成")
            End Sub, , True)
        }, "Form Show")
    End Sub

    '关闭窗口
    ''' <summary>
    ''' 正常关闭程序，播放关闭动画后退出。
    ''' </summary>
    ''' <param name="SendWarning">是否在还有下载任务未完成时发出警告。当前骨架阶段暂无下载任务。</param>
    Public Sub EndProgram(SendWarning As Boolean)
        If IsProgramEnding Then Return
        '关闭联机？
        If FrmLinkMain IsNot Nothing AndAlso FrmLinkMain.TryExit(Not SendWarning, True) Then Return
        IsProgramEnding = True
        Logger.Info("收到关闭指令")
        RunInUiWait(
        Sub()
            IsHitTestVisible = False
            EnsurePanBackTransform()
            AniStart({
                AaOpacity(Me, -Opacity, 140, 40, New AniEaseOutFluent(AniEasePower.Weak)),
                AaDouble(Sub(i) TransformScale.ScaleX += i, 0.88 - TransformScale.ScaleX, 180),
                AaDouble(Sub(i) TransformScale.ScaleY += i, 0.88 - TransformScale.ScaleY, 180),
                AaDouble(Sub(i) TransformPos.Y += i, 20 - TransformPos.Y, 180, 0, New AniEaseOutFluent(AniEasePower.Weak)),
                AaDouble(Sub(i) TransformRotate.Angle += i, 0.6 - TransformRotate.Angle, 180, 0, New AniEaseInoutFluent(AniEasePower.Weak)),
                AaCode(AddressOf Close, 40, True)
            }, "Form Close")
        End Sub)
    End Sub

    Private Sub EnsurePanBackTransform()
        If PanBack.RenderTransform IsNot Nothing Then Return
        Dim Group As New TransformGroup()
        Group.Children.Add(TransformRotate)
        Group.Children.Add(TransformPos)
        Group.Children.Add(TransformScale)
        PanBack.RenderTransform = Group
    End Sub

    '强制关闭程序
    Private Shared IsLogShown As Boolean = False
    Public Shared Sub EndProgramForce(Optional ReturnCode As ProcessReturnValues = ProcessReturnValues.Success)
        Try
            IsProgramEnding = True
            AniControlEnabled += 1
            If IsUpdateWaitingRestart Then UpdateRestart(False)
            If ReturnCode = ProcessReturnValues.Exception Then
                If Not IsLogShown Then
                    FeedbackInfo()
                    Logger.Info("请在 https://github.com/Meloong-Git/PCL/issues 提交错误报告，以便于作者解决此问题！")
                    IsLogShown = True
                    StartProcess(Paths.Base & "PCL\Log1.txt")
                End If
                Thread.Sleep(500) '防止 PCL 在记事本打开前就被掐掉
            End If
            Logger.Info($"程序已退出，返回值：{ReturnCode}")
            ConfigUtils.SaveAll()
            Logger.Instance.Flush()
            If ReturnCode <> ProcessReturnValues.Success Then Environment.Exit(ReturnCode)
            Process.GetCurrentProcess().Kill()
        Catch ex As Exception
            Logger.Error(ex, "强制关闭程序失败")
            Environment.Exit(1)
        End Try
    End Sub

    '标题栏按钮
    Private Sub BtnTitleClose_Click(sender As Object, e As RoutedEventArgs) Handles BtnTitleClose.Click
        EndProgram(True)
    End Sub

    Private Sub BtnTitleMin_Click(sender As Object, e As RoutedEventArgs) Handles BtnTitleMin.Click
        WindowState = WindowState.Minimized
    End Sub

    '移动窗口
    Private Sub FormDragMove(sender As Object, e As PointerPressedEventArgs) Handles PanTitle.PointerPressed, PanMsg.PointerPressed
        If Not e.GetCurrentPoint(Me).Properties.IsLeftButtonPressed Then Return
        Dim SourceVisual = TryCast(e.Source, Visual)
        If SourceVisual IsNot Nothing AndAlso SourceVisual.FindAncestorOfType(Of Button)(True) IsNot Nothing Then Return
        BeginMoveDrag(e)
    End Sub

    '改变大小
    Private Sub Resizer_PointerPressed(sender As Object, e As PointerPressedEventArgs) Handles ResizerT.PointerPressed, ResizerB.PointerPressed,
            ResizerL.PointerPressed, ResizerR.PointerPressed, ResizerLT.PointerPressed, ResizerRT.PointerPressed,
            ResizerLB.PointerPressed, ResizerRB.PointerPressed
        If Not e.GetCurrentPoint(Me).Properties.IsLeftButtonPressed Then Return
        If WindowState <> WindowState.Normal Then Return
        Select Case sender.Name
            Case "ResizerT" : BeginResizeDrag(WindowEdge.North, e)
            Case "ResizerB" : BeginResizeDrag(WindowEdge.South, e)
            Case "ResizerL" : BeginResizeDrag(WindowEdge.West, e)
            Case "ResizerR" : BeginResizeDrag(WindowEdge.East, e)
            Case "ResizerLT" : BeginResizeDrag(WindowEdge.NorthWest, e)
            Case "ResizerRT" : BeginResizeDrag(WindowEdge.NorthEast, e)
            Case "ResizerLB" : BeginResizeDrag(WindowEdge.SouthWest, e)
            Case "ResizerRB" : BeginResizeDrag(WindowEdge.SouthEast, e)
        End Select
    End Sub

    '页面导航
    Private Sub InitPages()
        '与原版 2.14 一致，联机页面暂不显示顶部入口
        BtnTitleSelect2.IsVisible = False
        '启动页
        If FrmLaunchLeft Is Nothing Then FrmLaunchLeft = New PageLaunchLeft
        If FrmLaunchRight Is Nothing Then FrmLaunchRight = New PageLaunchRight
        PageLeft = FrmLaunchLeft
        PageRight = FrmLaunchRight
        PageLeft.Opacity = 1
        PageRight.Opacity = 1
        '测试中会反复创建窗口，此时页面可能仍挂在旧窗口上，需要先解除挂载
        DetachFromParent(FrmLaunchLeft)
        DetachFromParent(FrmLaunchRight)
        PanMainLeft.Child = FrmLaunchLeft
        PanMainRight.Child = FrmLaunchRight
        FrmLaunchRight.PageState = MyPageRight.PageStates.ContentStay
        PageCurrent = New PageStackData With {.Page = PageType.Launch}
        PageLast = New PageStackData With {.Page = PageType.Launch}
        CurrentTab = 0
        RefreshTitleTabs()
        '预创建其他主页面
        FrmOtherLeft = New PageOtherLeft()
    End Sub

    ''' <summary>
    ''' 把控件从它当前的父容器中移除。
    ''' </summary>
    Private Shared Sub DetachFromParent(Target As Control)
        If Target.Parent Is Nothing Then Return
        If TypeOf Target.Parent Is ContentControl Then
            DirectCast(Target.Parent, ContentControl).Content = Nothing
        ElseIf TypeOf Target.Parent Is Decorator Then
            DirectCast(Target.Parent, Decorator).Child = Nothing
        ElseIf TypeOf Target.Parent Is Panel Then
            DirectCast(Target.Parent, Panel).Children.Remove(Target)
        End If
    End Sub

    Private Sub BtnTitleSelect_Click(sender As Object, e As RoutedEventArgs) Handles BtnTitleSelect0.Click, BtnTitleSelect1.Click,
            BtnTitleSelect2.Click, BtnTitleSelect3.Click, BtnTitleSelect4.Click
        If IsChangingPage Then Return
        Dim TargetId As Integer = Val(sender.Tag)
        If TargetId < 0 OrElse TargetId > 4 Then Return
        PageChangeActual(TargetId, CType(-1, PageSubType))
    End Sub

    Private Sub PageChangeAnim(TargetLeft As MyPageLeft, TargetRight As MyPageRight, TargetTab As Integer)
        If IsChangingPage Then Return
        IsChangingPage = True
        If HelpDetailActive Then CloseHelpDetailChrome(False)
        CurrentTab = TargetTab
        RefreshTitleTabs()
        AniStop("FrmMain PageChangeLeft")
        AniStop("FrmMain PageChangeRight")
        AniStop("PageLeft PageChange")
        '清除新页面关联性
        If TypeOf TargetLeft.Parent Is ContentControl Then DirectCast(TargetLeft.Parent, ContentControl).Content = Nothing
        If TypeOf TargetRight.Parent Is ContentControl Then DirectCast(TargetRight.Parent, ContentControl).Content = Nothing
        AniControlEnabled += 1
        If PageLeft IsNot Nothing Then PageLeft.TriggerHideAnimation()
        If PageRight IsNot Nothing Then PageRight.PageOnExit()
        PageLeft = TargetLeft
        PageRight = TargetRight
        AniControlEnabled -= 1
        AniStart({
            AaCode(
            Sub()
                PanMainLeft.Child = TargetLeft
                TargetLeft.Opacity = 0
            End Sub, 110),
            AaCode(
            Sub()
                TargetLeft.Opacity = 1
                TargetLeft.TriggerShowAnimation()
            End Sub, 30, True)
        }, "FrmMain PageChangeLeft")
        AniStart({
            AaCode(
            Sub()
                Dim Previous = TryCast(PanMainRight.Child, MyPageRight)
                If Previous IsNot Nothing Then Previous.PageOnForceExit()
                PanMainRight.Child = TargetRight
                TargetRight.Opacity = 0
                BtnExtraBack.ShowRefresh()
            End Sub, 110),
            AaCode(
            Sub()
                TargetRight.Opacity = 1
                TargetRight.PageOnEnter()
                IsChangingPage = False
            End Sub, 30, True)
        }, "FrmMain PageChangeRight")
    End Sub

    Private Sub RefreshTitleTabs()
        Dim Buttons = {BtnTitleSelect0, BtnTitleSelect1, BtnTitleSelect2, BtnTitleSelect3, BtnTitleSelect4}
        For i = 0 To Buttons.Length - 1
            If i = CurrentTab Then
                If Not Buttons(i).Classes.Contains("active") Then Buttons(i).Classes.Add("active")
            Else
                Buttons(i).Classes.Remove("active")
            End If
        Next
    End Sub

    '次级页面
    ''' <summary>
    ''' 进入帮助详情等次级页面，隐藏左侧导航并在标题栏显示返回按钮。
    ''' </summary>
    Public Sub PageChangeHelpDetail(Entry As HelpEntry, Detail As MyPageRight)
        If Entry Is Nothing OrElse Detail Is Nothing Then Return
        If PageStack.Any() Then PageChangeExit()
        If Not HelpDetailActive Then
            HelpDetailActive = True
            HelpDetailPreviousPage = PageRight
            HelpDetailPreviousTab = CurrentTab
            '隐藏左侧导航
            PanMainLeft.IsVisible = False
            '切换到次级标题栏
            LabTitleInner.Text = Entry.Title
            PanTitleMain.IsVisible = False
            PanTitleMain.IsHitTestVisible = False
            PanTitleInner.IsVisible = True
            PanTitleInner.IsHitTestVisible = True
            PanTitleInner.Margin = New Thickness(-16, 0, 0, 0)
            PanTitleInner.Opacity = 0
            AniStart({
                AaOpacity(PanTitleInner, 1, 150, 100),
                AaX(PanTitleInner, 16, 350, 100, New AniEaseOutBack(AniEasePower.Weak))
            }, "FrmMain Titlebar HelpDetail")
        Else
            HelpDetailHistory.Add(New HelpDetailFrame With {
                .Page = PageRight,
                .Title = LabTitleInner.Text,
                .Tab = CurrentTab
            })
            LabTitleInner.Text = Entry.Title
        End If
        '切换右侧页面
        SwitchHelpDetailPage(Detail)
        Logger.Info($"切换次级页面：帮助详情 - {Entry.Title}")
    End Sub

    Private Sub SwitchHelpDetailPage(Target As MyPageRight)
        If PageRight IsNot Nothing AndAlso PageRight IsNot Target Then PageRight.PageOnExit()
        PageRight = Target
        IsChangingPage = False
        AniStop("FrmMain PageChangeRight")
        AniStart({
            AaCode(
            Sub()
                Dim Previous = TryCast(PanMainRight.Child, MyPageRight)
                If Previous IsNot Nothing Then Previous.PageOnForceExit()
                PanMainRight.Child = Target
                Target.Opacity = 0
                BtnExtraBack.ShowRefresh()
            End Sub, 110),
            AaCode(
            Sub()
                Target.Opacity = 1
                Target.PageOnEnter()
            End Sub, 30, True)
        }, "FrmMain PageChangeRight")
    End Sub

    ''' <summary>
    ''' 从次级页面返回。
    ''' </summary>
    Public Sub PageBack()
        If HelpDetailActive Then
            '返回到上一层帮助详情
            If HelpDetailHistory.Count > 0 Then
                Dim Last = HelpDetailHistory(HelpDetailHistory.Count - 1)
                HelpDetailHistory.RemoveAt(HelpDetailHistory.Count - 1)
                LabTitleInner.Text = Last.Title
                CurrentTab = Last.Tab
                SwitchHelpDetailPage(Last.Page)
                Return
            End If
            Dim Target = HelpDetailPreviousPage
            Dim TargetTab = HelpDetailPreviousTab
            CloseHelpDetailChrome(True)
            If Target Is Nothing Then Return
            CurrentTab = TargetTab
            RefreshTitleTabs()
            If PageLeft IsNot Nothing Then
                PageLeft.Opacity = 1
                PageLeft.TriggerShowAnimation()
            End If
            SwitchHelpDetailPage(Target)
            Return
        End If
        If PageStack.Any() Then
            PageChangeActual(PageStack(0))
        Else
            PageChange(PageType.Launch)
        End If
    End Sub

    Private Sub TriggerPageBack()
        PageBack()
    End Sub

    Private Sub BtnTitleInner_Click(sender As Object, e As EventArgs) Handles BtnTitleInner.Click
        PageBack()
    End Sub

    ''' <summary>
    ''' 退出次级页面的界面状态，恢复左侧导航与主标题栏。
    ''' </summary>
    Private Sub CloseHelpDetailChrome(Animate As Boolean)
        If Not HelpDetailActive Then Return
        HelpDetailActive = False
        HelpDetailPreviousPage = Nothing
        HelpDetailHistory.Clear()
        PanMainLeft.IsVisible = True
        PanTitleMain.IsVisible = True
        PanTitleMain.IsHitTestVisible = True
        PanTitleInner.IsHitTestVisible = False
        If Animate Then
            AniStart({
                AaOpacity(PanTitleInner, -PanTitleInner.Opacity, 150),
                AaX(PanTitleInner, -16 - PanTitleInner.Margin.Left, 150,, New AniEaseInFluent(AniEasePower.Weak)),
                AaCode(
                Sub()
                    PanTitleInner.IsVisible = False
                    PanTitleInner.Opacity = 0
                    PanTitleInner.Margin = New Thickness(-16, 0, 0, 0)
                End Sub,, True)
            }, "FrmMain Titlebar HelpDetail")
        Else
            AniStop("FrmMain Titlebar HelpDetail")
            PanTitleInner.IsVisible = False
            PanTitleInner.Opacity = 0
            PanTitleInner.Margin = New Thickness(-16, 0, 0, 0)
        End If
    End Sub

    '功能隐藏
    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        MyBase.OnKeyDown(e)
        '按 ESC 取消启动，或返回上一级
        If e.Key = Key.Escape Then
            If PageCurrent = PageType.Launch AndAlso McLaunchLoader.State = LoadState.Loading Then
                FrmLaunchLeft.BtnCancel_Click()
                e.Handled = True
                Return
            End If
            TriggerPageBack()
            e.Handled = True
            Return
        End If
        '更改隐藏版本可见性
        If e.Key = Key.F11 AndAlso PageCurrent = PageType.InstanceSelect Then
            If FrmSelectRight IsNot Nothing Then
                FrmSelectRight.ShowHidden = Not FrmSelectRight.ShowHidden
                LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\")
            End If
            e.Handled = True
            Return
        End If
        '更改功能隐藏可见性
        If e.Key = Key.F12 Then
            PageSetupUI.HiddenForceShow = Not PageSetupUI.HiddenForceShow
            If PageSetupUI.HiddenForceShow Then
                Hint("功能隐藏设置已暂时关闭！", HintType.Green)
            Else
                Hint("功能隐藏设置已重新开启！", HintType.Green)
            End If
            PageSetupUI.HiddenRefresh()
            e.Handled = True
            Return
        End If
        '按 F5 刷新页面
        If e.Key = Key.F5 Then
            If TypeOf PageLeft Is IRefreshable Then CType(PageLeft, IRefreshable).Refresh()
            If TypeOf PageRight Is IRefreshable Then CType(PageRight, IRefreshable).Refresh()
            Return
        End If
        '调用启动游戏
        If e.Key = Key.Enter AndAlso PageCurrent = PageType.Launch Then
            If IsAprilEnabled AndAlso Not IsAprilGiveup Then
                Hint("木大！")
            Else
                FrmLaunchLeft.LaunchButtonClick()
            End If
        End If
    End Sub

    '鼠标侧键返回上一级
    Protected Overrides Sub OnPointerPressed(e As PointerPressedEventArgs)
        MyBase.OnPointerPressed(e)
        If WaitingMyMsgBox.Any Then Return
        Dim Kind = e.GetCurrentPoint(Me).Properties.PointerUpdateKind
        If Kind = PointerUpdateKind.XButton1Pressed OrElse Kind = PointerUpdateKind.XButton2Pressed Then
            TriggerPageBack()
        End If
    End Sub

    '背景图片

    ''' <summary>
    ''' 设置显示的背景图片。
    ''' </summary>
    Public Sub SetBackgroundImage(Pic As Bitmap)
        _BackgroundImage = Pic
        UpdateBackgroundAndTitleBar()
    End Sub

    ''' <summary>
    ''' 清除显示的背景图片。
    ''' </summary>
    Public Sub ClearBackgroundImage()
        _BackgroundImage = Nothing
        ImgBack.Background = Nothing
        ImgBack.IsVisible = False
    End Sub

    ''' <summary>
    ''' 从设置更新背景图片与标题栏样式。
    ''' </summary>
    Public Shared Sub UpdateBackgroundAndTitleBar(Value As Object)
        If FrmMain Is Nothing OrElse Not FrmMain.IsLoaded Then Return
        FrmMain.UpdateBackgroundAndTitleBar()
    End Sub

    ''' <summary>
    ''' 从设置更新背景图片与标题栏样式。
    ''' </summary>
    Public Sub UpdateBackgroundAndTitleBar()
        If Not IsLoaded Then Return
        Try
            '背景图片透明度
            ImgBack.Opacity = Settings.Get(Of Integer)("UiBackgroundOpacity") / 1000
            '背景图片模糊
            Dim BlurRadius As Double = Settings.Get(Of Integer)("UiBackgroundBlur") + 1
            If BlurRadius <= 1 Then
                ImgBack.Effect = Nothing
            Else
                ImgBack.Effect = New BlurEffect With {.Radius = BlurRadius}
            End If
            ImgBack.Margin = New Thickness(-BlurRadius / 1.8)
            '背景图片适应方式
            If _BackgroundImage IsNot Nothing Then
                Dim BackgroundType As Integer = Settings.Get(Of Integer)("UiBackgroundSuit")
                If BackgroundType = 0 Then
                    '智能：当图片较小时平铺，较大时适应
                    BackgroundType = If(_BackgroundImage.Size.Width < PanMain.Bounds.Width / 2 AndAlso _BackgroundImage.Size.Height < PanMain.Bounds.Height / 2, 4, 2)
                End If
                Dim Brush As New ImageBrush(_BackgroundImage) With {
                    .Stretch = Stretch.UniformToFill,
                    .TileMode = TileMode.None,
                    .AlignmentX = AlignmentX.Center,
                    .AlignmentY = AlignmentY.Center}
                Select Case BackgroundType
                    Case 1 '居中
                        Brush.Stretch = Stretch.None
                    Case 3 '拉伸
                        Brush.Stretch = Stretch.Fill
                    Case 4 '平铺
                        Brush.Stretch = Stretch.None
                        Brush.TileMode = TileMode.Tile
                        Brush.DestinationRect = New RelativeRect(0, 0, _BackgroundImage.Size.Width, _BackgroundImage.Size.Height, RelativeUnit.Absolute)
                    Case 2 '居中适应
                    Case 5 '左上
                        Brush.AlignmentX = AlignmentX.Left
                        Brush.AlignmentY = AlignmentY.Top
                    Case 6 '右上
                        Brush.AlignmentX = AlignmentX.Right
                        Brush.AlignmentY = AlignmentY.Top
                    Case 7 '左下
                        Brush.AlignmentX = AlignmentX.Left
                        Brush.AlignmentY = AlignmentY.Bottom
                    Case 8 '右下
                        Brush.AlignmentX = AlignmentX.Right
                        Brush.AlignmentY = AlignmentY.Bottom
                    Case 9 '左中
                        Brush.AlignmentX = AlignmentX.Left
                        Brush.AlignmentY = AlignmentY.Center
                    Case 10 '右中
                        Brush.AlignmentX = AlignmentX.Right
                        Brush.AlignmentY = AlignmentY.Center
                    Case 11 '中上
                        Brush.AlignmentX = AlignmentX.Center
                        Brush.AlignmentY = AlignmentY.Top
                    Case 12 '中下
                        Brush.AlignmentX = AlignmentX.Center
                        Brush.AlignmentY = AlignmentY.Bottom
                End Select
                ImgBack.Background = Brush
                ImgBack.IsVisible = True
            Else
                ImgBack.Background = Nothing
                ImgBack.IsVisible = False
            End If
            '标题栏显示类型
            Dim LogoType As Integer = Settings.Get(Of Integer)("UiLogoType")
            ShapeTitleLogo.IsVisible = LogoType = 1
            LabTitleLogo.IsVisible = LogoType = 2
            ImageTitleLogo.IsVisible = LogoType = 3
            If LogoType = 2 Then LabTitleLogo.Text = Settings.Get(Of String)("UiLogoText")
            If LogoType = 3 Then RefreshTitleLogo()
            '标题栏文本是否居左
            PanTitleMain.ColumnDefinitions(0).Width = New GridLength(If(Settings.Get(Of Boolean)("UiLogoLeft") AndAlso LogoType = 0, 0, 1), GridUnitType.Star)            '个性化设置页联动
            If PageSetupUI.Instance IsNot Nothing Then
                PageSetupUI.Instance.CheckLogoLeft.IsVisible = LogoType = 0
                PageSetupUI.Instance.PanLogoText.IsVisible = LogoType = 2
                PageSetupUI.Instance.PanLogoChange.IsVisible = LogoType = 3
                PageSetupUI.Instance.CardLogo.TriggerForceResize()
            End If
        Catch ex As Exception
            Logger.Error(ex, "从设置更新背景图片与标题栏样式失败")
        End Try
    End Sub

    ''' <summary>
    ''' 重新从数据文件夹加载标题栏图片。
    ''' </summary>
    Public Sub RefreshTitleLogo()
        Dim TargetPath As String = Path.Combine(PathData, "Logo.png")
        Try
            ImageTitleLogo.Source = Nothing '防止因为 Source 属性前后的值相同而不更新
            If File.Exists(TargetPath) Then ImageTitleLogo.Source = TargetPath
        Catch ex As Exception
            ImageTitleLogo.Source = Nothing
            Logger.Error(ex, "显示标题栏图片失败", LogBehavior.Alert)
        End Try
    End Sub

#Region "附加按钮"

    ''' <summary>
    ''' 刷新右下角任务管理按钮的进度与显示状态。
    ''' </summary>
    Public Sub RefreshExtraDownload()
        BtnExtraDownload.Progress = LoaderTaskbarProgress
        BtnExtraDownload.ShowRefresh()
    End Sub

    '返回顶部
    Public Sub BackToTop()
        Dim RealScroll = BtnExtraBack_GetRealChild()
        If RealScroll IsNot Nothing Then
            RealScroll.PerformVerticalOffsetDelta(-RealScroll.Offset.Y)
        Else
            Logger.Error("无法返回顶部，未找到合适的 RealScroll", LogBehavior.Toast)
        End If
    End Sub
    Private Sub BtnExtraBack_Click(sender As Object, e As EventArgs) Handles BtnExtraBack.Click
        BackToTop()
    End Sub
    Private Function BtnExtraBack_ShowCheck() As Boolean
        Dim RealScroll = BtnExtraBack_GetRealChild()
        Return RealScroll IsNot Nothing AndAlso RealScroll.IsVisible AndAlso
            RealScroll.Offset.Y > Height + If(BtnExtraBack.Show, 0, 700)
    End Function
    Private Function BtnExtraBack_GetRealChild() As MyScrollViewer
        Dim Page = TryCast(PanMainRight.Child, MyPageRight)
        If Page Is Nothing OrElse String.IsNullOrEmpty(Page.PanScroll) Then Return Nothing
        Return Page.FindControl(Of MyScrollViewer)(Page.PanScroll)
    End Function

    '任务管理
    Private Sub BtnExtraDownload_Click(sender As Object, e As EventArgs) Handles BtnExtraDownload.Click
        PageChange(PageType.TaskManager)
    End Sub
    Private Function BtnExtraDownload_ShowCheck() As Boolean
        Return HasDownloadingTask() AndAlso Not PageCurrent = PageType.TaskManager
    End Function

    '音乐
    Private Sub BtnExtraMusic_Click(sender As Object, e As EventArgs) Handles BtnExtraMusic.Click
        MusicControlPause()
    End Sub
    Private Sub BtnExtraMusic_RightClick(sender As Object, e As EventArgs) Handles BtnExtraMusic.RightClick
        MusicControlNext()
    End Sub

    '投降
    Public Sub AprilGiveup()
        If IsAprilEnabled AndAlso Not IsAprilGiveup Then
            Hint("=D", HintType.Green)
            IsAprilGiveup = True
            If FrmLaunchLeft IsNot Nothing Then
                FrmLaunchLeft.AprilScaleTrans.ScaleX = 1
                FrmLaunchLeft.AprilScaleTrans.ScaleY = 1
            End If
            BtnExtraApril.ShowRefresh()
        End If
    End Sub
    Private Sub BtnExtraApril_Click(sender As Object, e As EventArgs) Handles BtnExtraApril.Click
        AprilGiveup()
    End Sub
    Private Function BtnExtraApril_ShowCheck() As Boolean
        Return IsAprilEnabled AndAlso Not IsAprilGiveup AndAlso PageCurrent = PageType.Launch
    End Function

    '关闭 Minecraft
    Public Sub BtnExtraShutdown_Click()
        Try
            If McLaunchLoaderReal IsNot Nothing Then McLaunchLoaderReal.Cancel()
            For Each Watcher In McWatcherList
                Watcher.Kill()
            Next
            Hint("已关闭运行中的 Minecraft！", HintType.Green)
        Catch ex As Exception
            Logger.Error(ex, "强制关闭所有 Minecraft 失败")
        End Try
    End Sub
    Private Sub BtnExtraShutdown_ClickHandler(sender As Object, e As EventArgs) Handles BtnExtraShutdown.Click
        BtnExtraShutdown_Click()
    End Sub
    Private Function BtnExtraShutdown_ShowCheck() As Boolean
        Return HasRunningMinecraft
    End Function

#End Region

#Region "窗口隐藏与置顶"

    Private _Hidden As Boolean = False
    ''' <summary>
    ''' 窗口是否处于隐藏状态。
    ''' </summary>
    Public Property Hidden As Boolean
        Get
            Return _Hidden
        End Get
        Set(value As Boolean)
            If _Hidden = value Then Return
            _Hidden = value
            If value Then
                '隐藏
                Position = New PixelPoint(Position.X - 10000, Position.Y)
                ShowInTaskbar = False
                Hide()
                Logger.Info($"窗口已隐藏，位置：({Position.X},{Position.Y})")
            Else
                '取消隐藏
                If Position.X < -2000 Then Position = New PixelPoint(Position.X + 10000, Position.Y)
                ShowWindowToTop()
            End If
        End Set
    End Property

    ''' <summary>
    ''' 把当前窗口拖到最前面。
    ''' </summary>
    Public Sub ShowWindowToTop()
        RunInUi(
        Sub()
            '这一坨乱七八糟的，别改，改了指不定就炸了，自己电脑还复现不出来
            Show()
            ShowInTaskbar = True
            WindowState = WindowState.Normal
            Hidden = False
            Topmost = True '偶尔 Activate 失效
            Topmost = False
            Activate()
            Focus()
            Logger.Info($"窗口已置顶，位置：({Position.X},{Position.Y}), {Width} x {Height}")
        End Sub)
    End Sub

#End Region

    '切回窗口
    Private Sub FormMain_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        '切回窗口时自动刷新
        Try
            If PageCurrent = PageType.InstanceSetup AndAlso PageCurrentSub = PageSubType.InstanceMod Then
                'Mod 管理
                If FrmInstanceMod IsNot Nothing Then FrmInstanceMod.LoaderRun(LoaderFolderRunType.RunOnUpdated)
            ElseIf PageCurrent = PageType.InstanceSetup AndAlso (PageCurrentSub = PageSubType.InstanceSetup OrElse PageCurrentSub = PageSubType.InstanceExport) Then
                '更新当前选用的 Java
                PageInstanceLeft.ReloadCurrentJava()
            ElseIf PageCurrent = PageType.InstanceSelect Then
                '版本选择
                LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.RunOnUpdated, MaxDepth:=1, ExtraPath:="versions\")
            End If
        Catch ex As Exception
            Logger.Error(ex, "切回窗口时出错")
        End Try
    End Sub

End Class