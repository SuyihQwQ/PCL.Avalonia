Imports Avalonia
Imports Avalonia.Controls
Imports Avalonia.Controls.ApplicationLifetimes
Imports Avalonia.Input
Imports Avalonia.Media
Imports Avalonia.Media.Imaging
Imports Avalonia.Platform.Storage
Imports Avalonia.Threading
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
                    Logger.Error($"未知的页面类型：{Stack.Page}")
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
        'Skia 在 Linux 上默认用轻度 hinting，13px 汉字最后一行抗锯齿会被压掉，字形底部看起来像被裁平了；
        '改用完整 hinting 并固定灰度抗锯齿（窗口透明时子像素渲染会导致彩色边缘）
        TextOptions.SetTextHintingMode(Me, TextHintingMode.Strong)
        TextOptions.SetTextRenderingMode(Me, TextRenderingMode.Antialias)
        EnsurePanBackTransform()
        DragDrop.SetAllowDrop(Me, True)
        '主窗口使用自绘提示层，关闭系统 ToolTip 服务，避免出现两个提示框
        ToolTip.SetServiceEnabled(Me, False)
        '与原版一致：子控件可能先处理拖拽事件，这里必须监听已处理事件
        MyBase.AddHandler(DragDrop.DragEnterEvent, AddressOf FormMain_HandleDrag, RoutingStrategies.Bubble, True)
        MyBase.AddHandler(DragDrop.DragOverEvent, AddressOf FormMain_HandleDrag, RoutingStrategies.Bubble, True)
        MyBase.AddHandler(DragDrop.DropEvent, AddressOf FormMain_Drop, RoutingStrategies.Bubble, True)
        MyBase.AddHandler(InputElement.PointerMovedEvent, AddressOf FormMain_PointerMoved, RoutingStrategies.Tunnel, True)
        MyBase.AddHandler(InputElement.PointerExitedEvent, AddressOf FormMain_PointerExited, RoutingStrategies.Tunnel, True)
        '与原版一致：点击（包括右键打开菜单）时关闭悬停提示，点击输入框以外的地方退出输入状态
        MyBase.AddHandler(InputElement.PointerPressedEvent,
            Sub(sender, e)
                Dim Pressed = TryCast(e.Source, Visual)
                '点击当前悬停的控件本身时保留提示（例如点击输入框后仍保持悬停信息）
                Dim PressedOwnTip As Boolean = HoverTipControl IsNot Nothing AndAlso Pressed IsNot Nothing AndAlso
                                               Pressed.GetSelfAndVisualAncestors().Contains(HoverTipControl)
                If Not PressedOwnTip Then HideHoverTip()
                ExitTextInputOnBlankClick(Pressed)
            End Sub, RoutingStrategies.Tunnel, True)
        MyBase.AddHandler(InputElement.PointerReleasedEvent,
            Sub(sender, e) PointerCaptureActive = False, RoutingStrategies.Tunnel, True)
        HoverTipTimer = New DispatcherTimer With {.Interval = TimeSpan.FromMilliseconds(100)}
        AddHandler HoverTipTimer.Tick, AddressOf HoverTipTimer_Tick
        AddHandler Closed, AddressOf FormMain_Closed
        HadSplashOnStart = FrmStart IsNot Nothing
        Opacity = 0
        PanBack.Opacity = 0
    End Sub

    Private HadSplashOnStart As Boolean
    Private ShowAnimationScheduled As Boolean

    '自定义提示框：直接跟随鼠标绘制，避免系统 ToolTip 位置计算带来的偏移
    Private HoverTipControl As Control
    Private HoverTipObject As Object
    Private HoverTipPointer As Point
    '指针被控件捕获（如拖动滑块）期间不显示悬停提示
    Private PointerCaptureActive As Boolean
    Private HoverTipHiding As Boolean
    Private ReadOnly HoverTipTimer As DispatcherTimer

    ''' <summary>
    ''' 点击文本框以外的区域时退出输入状态，与原版一致。
    ''' </summary>
    Private Sub ExitTextInputOnBlankClick(Source As Visual)
        If Source Is Nothing Then Return
        Dim Top = TopLevel.GetTopLevel(Me)
        If Top Is Nothing OrElse Top.FocusManager Is Nothing Then Return
        Dim Focused = TryCast(Top.FocusManager.GetFocusedElement(), Visual)
        If Focused Is Nothing Then Return
        If TypeOf Focused IsNot TextBox AndAlso TypeOf Focused IsNot MyComboBox Then Return
        Dim Chain = Source.GetSelfAndVisualAncestors().ToList()
        '点击仍在当前输入控件内部（例如移动光标）时保持焦点
        If Chain.Contains(Focused) Then Return
        '可编辑下拉框的焦点在内部文本框上，点击同一控件的箭头等区域也不应退出
        Dim Combo = Focused.GetSelfAndVisualAncestors().OfType(Of MyComboBox)().FirstOrDefault()
        If Combo IsNot Nothing AndAlso Chain.Contains(Combo) Then Return
        Top.FocusManager.Focus(Nothing)
    End Sub

    Private Sub FormMain_PointerMoved(sender As Object, e As PointerEventArgs)
        '拖动滑块等捕获指针的操作期间，指针移到其他控件也不显示悬停提示
        PointerCaptureActive = e.Pointer.Captured IsNot Nothing
        '下拉菜单展开期间不显示悬停提示
        If PointerCaptureActive OrElse MyComboBox.OpenDropDowns.Count > 0 Then
            HideHoverTip()
            Return
        End If
        Dim Pointer = e.GetPosition(Me)
        Dim TipControl = FindToolTipControl(TryCast(Me.InputHitTest(Pointer), Visual))
        If TipControl Is Nothing Then
            HideHoverTip()
            Return
        End If
        If TipControl IsNot HoverTipControl Then
            HideHoverTip()
            HoverTipControl = TipControl
            HoverTipObject = ToolTip.GetTip(TipControl)
            HoverTipPointer = Pointer
            If HoverTipObject IsNot Nothing Then HoverTipTimer.Start()
            Return
        End If
        HoverTipPointer = Pointer
        If BorderToolTip.IsVisible Then PositionHoverTip(Pointer)
    End Sub

    Private Shared Function FindToolTipControl(Source As Visual) As Control
        If Source Is Nothing Then Return Nothing
        For Each Ancestor In Source.GetSelfAndVisualAncestors().OfType(Of Control)()
            If ToolTip.GetTip(Ancestor) IsNot Nothing AndAlso Ancestor.IsEffectivelyVisible Then Return Ancestor
        Next
        Return Nothing
    End Function

    Private Sub HoverTipTimer_Tick(sender As Object, e As EventArgs)
        ShowHoverTip()
    End Sub

    Private Sub FormMain_PointerExited(sender As Object, e As PointerEventArgs)
        PointerCaptureActive = False
        HideHoverTip()
    End Sub

    Private Sub ShowHoverTip()
        HoverTipTimer.Stop()
        '计时器触发前可能已开始拖动或展开下拉菜单
        If PointerCaptureActive OrElse MyComboBox.OpenDropDowns.Count > 0 Then
            HideHoverTip()
            Return
        End If
        If HoverTipControl Is Nothing OrElse Not HoverTipControl.IsEffectivelyVisible Then
            HideHoverTip()
            Return
        End If
        Dim Text As String = If(TypeOf HoverTipObject Is String, CStr(HoverTipObject), HoverTipObject?.ToString())
        If String.IsNullOrWhiteSpace(Text) Then
            HideHoverTip()
            Return
        End If
        LabToolTip.Text = Text
        If Not BorderToolTip.IsVisible Then
            BorderToolTip.Opacity = 0
            BorderToolTip.IsVisible = True
        End If
        HoverTipHiding = False
        AniStop("Form HoverTip")
        PositionHoverTip(HoverTipPointer)
        AniStart(AaOpacity(BorderToolTip, 1 - BorderToolTip.Opacity, 100, , New AniEaseOutFluent), "Form HoverTip")
    End Sub

    Private Sub PositionHoverTip(Pointer As Point)
        BorderToolTip.Measure(New Size(380, Double.PositiveInfinity))
        Dim Desired = BorderToolTip.DesiredSize
        Dim X = Pointer.X + 16
        Dim Y = Pointer.Y + 20
        If X + Desired.Width > Bounds.Width - 8 Then X = Pointer.X - Desired.Width - 12
        If Y + Desired.Height > Bounds.Height - 8 Then Y = Pointer.Y - Desired.Height - 12
        Canvas.SetLeft(BorderToolTip, Math.Clamp(X, 8, Math.Max(8, Bounds.Width - Desired.Width - 8)))
        Canvas.SetTop(BorderToolTip, Math.Clamp(Y, 8, Math.Max(8, Bounds.Height - Desired.Height - 8)))
    End Sub

    Friend Sub HideHoverTip()
        HoverTipTimer.Stop()
        HoverTipControl = Nothing
        HoverTipObject = Nothing
        If BorderToolTip Is Nothing OrElse Not BorderToolTip.IsVisible OrElse HoverTipHiding Then Return
        HoverTipHiding = True
        Dim Tip = BorderToolTip
        AniStop("Form HoverTip")
        AniStart({
            AaOpacity(Tip, -Tip.Opacity, 100, , New AniEaseOutFluent),
            AaCode(
            Sub()
                Tip.IsVisible = False
                Tip.Opacity = 0
                HoverTipHiding = False
            End Sub, , True)
        }, "Form HoverTip")
    End Sub

    Private Sub FormMain_Closed(sender As Object, e As EventArgs)
        HoverTipTimer.Stop()
        AniStop("Form HoverTip")
        HoverTipHiding = False
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
        '尽早执行的加载池：下载的已存在文件检测与版本选择页都依赖该列表
        McFolderListLoader.Start(0)
        AniStart()
        CloseSplash()
        '兜底：某些平台可能不触发 Activated，也要保证界面最终显示
        AniStart({AaCode(AddressOf StartWindowShowAnimation, 800)}, "Form Show Fallback")
        '首次启动或协议更新提示（与原版一致，仅桌面运行环境触发）
        If TypeOf Application.Current?.ApplicationLifetime Is IClassicDesktopStyleApplicationLifetime Then
            RunInNewThread(AddressOf CheckEula, "Eula Check", ThreadPriority.Lowest)
        End If
    End Sub

    ''' <summary>
    ''' 首次启动或用户协议更新时显示协议授权弹窗。
    ''' </summary>
    Private Shared Sub CheckEula()
        Const EulaVersion As Integer = 2
        If Settings.Get(Of Integer)("SystemEulaVersion") >= EulaVersion Then Return
        Dim Text As String = Localization.GetText(If(Settings.Get(Of Integer)("SystemEulaVersion") = 0,
                                                     "LangDialogPolicyContent", "LangDialogPolicyContentModified"))
        Select Case MyMsgBox(Text, Localization.GetText("LangDialogPolicyTitle"),
                             Localization.GetText("LangDialogBtnAgree"),
                             Localization.GetText("LangDialogBtnDeny"),
                             Localization.GetText("LangDialogBtnPolicyContent"),
                             Button3Action:=Sub() OpenWebsite("https://shimo.im/docs/rGrd8pY8xWkt6ryW"))
            Case 1
                Settings.Set("SystemEulaVersion", EulaVersion)
            Case 2
                RunInUi(Sub() FrmMain?.EndProgram(False))
        End Select
    End Sub

    Private ShowAnimationStarted As Boolean

    ''' <summary>
    ''' 播放窗口淡入与位置、旋转回正动画。
    ''' </summary>
    Private Sub StartWindowShowAnimation()
        If ShowAnimationStarted OrElse IsProgramEnding OrElse Not IsVisible Then Return
        ShowAnimationStarted = True
        AniResetClock()
        Dim TargetOpacity As Double = (Settings.Get(Of Integer)("UiLauncherTransparent") / 1000 + 0.4).Clamp(0, 1)
        Opacity = TargetOpacity
        PanBack.Opacity = 0
        AniStart()
        AniStart({
            AaOpacity(PanBack, 1, 500),
            AaDouble(Sub(i) TransformPos.Y += i, -TransformPos.Y, 600, , New AniEaseOutBack(AniEasePower.Weak)),
            AaDouble(Sub(i) TransformRotate.Angle += i, -TransformRotate.Angle, 500, , New AniEaseOutBack(AniEasePower.Weak)),
            AaCode(
            Sub()
                PanBack.RenderTransform = Nothing
                Logger.Info("窗口加载完成")
            End Sub, , True)
        }, "Form Show")
    End Sub

    ''' <summary>
    ''' 关闭启动时显示的 PCL 图标，与原版一致在 400ms 内淡出。
    ''' </summary>
    Private Sub CloseSplash()
        Dim Splash = FrmStart
        If Splash Is Nothing Then Return
        FrmStart = Nothing
        AniStart({
            AaDouble(Sub(i) Splash.Opacity += i, -Splash.Opacity, 400),
            AaCode(Sub() Splash.Close(), After:=True)
        }, "Splash Close")
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
                AaCode(
                Sub()
                    IsHitTestVisible = False
                    Hide()
                End Sub, 210),
                AaCode(AddressOf EndProgramForce, 230)
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
        '无桌面生命周期（测试等场景）时只播放关闭动画，不结束进程
        If TypeOf Application.Current?.ApplicationLifetime IsNot IClassicDesktopStyleApplicationLifetime Then Return
        Try
            IsProgramEnding = True
            AniControlEnabled += 1
            If IsUpdateWaitingRestart Then UpdateRestart(False)
            If ReturnCode = ProcessReturnValues.Exception Then
                If Not IsLogShown Then
                    FeedbackInfo()
                    Logger.Info("请在 https://github.com/SuyihQwQ/PCL.Avalonia/issues 提交错误报告，以便于作者解决此问题！")
                    IsLogShown = True
                    StartProcess(PathData & "Log1.txt")
                End If
                Thread.Sleep(500) '防止 PCL 在记事本打开前就被掐掉
            End If
            Logger.Info($"程序已退出，返回值：{ReturnCode}")
            ConfigUtils.SaveAll()
            Logger.Instance.Flush()
            If ReturnCode <> ProcessReturnValues.Success Then Environment.Exit(ReturnCode)
            If OperatingSystem.IsWindows() Then
                Process.GetCurrentProcess().Kill()
            Else
                'Process.Kill 在 Linux 上会以 SIGKILL 结束进程，改用 Environment.Exit 正常退出
                Environment.Exit(0)
            End If
        Catch ex As Exception
            Logger.Error(ex, "强制关闭程序失败")
            Environment.Exit(1)
        End Try
    End Sub

    '标题栏按钮
    Private Sub BtnTitleClose_Click(sender As Object, e As RoutedEventArgs) Handles BtnTitleClose.Click
        EndProgram(True)
    End Sub

    '关闭窗口
    Private Sub FormMain_Closing(sender As Object, e As WindowClosingEventArgs) Handles Me.Closing
        '测试等无桌面生命周期的场景直接关闭；程序正在退出时也不再拦截
        If TypeOf Application.Current?.ApplicationLifetime IsNot IClassicDesktopStyleApplicationLifetime Then Return
        If IsProgramEnding Then Return
        e.Cancel = True
        EndProgram(True)
    End Sub

    Private Sub BtnTitleMin_Click(sender As Object, e As RoutedEventArgs) Handles BtnTitleMin.Click
        WindowState = WindowState.Minimized
    End Sub

    '移动窗口
    Private Sub FormDragMove(sender As Object, e As PointerPressedEventArgs) Handles PanTitle.PointerPressed, PanMsg.PointerPressed
        If Not e.GetCurrentPoint(Me).Properties.IsLeftButtonPressed Then Return
        '对应原版的 sender.IsMouseDirectlyOver：仅当按在标题栏/消息层本身而不是其子控件上时才拖动窗口
        If e.Source IsNot sender Then Return
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

    '左边栏背景
    Private Sub PanMainLeft_SizeChanged(sender As Object, e As SizeChangedEventArgs) Handles PanMainLeft.SizeChanged
        If e.WidthChanged Then PanMainLeft_Resize(e.NewSize.Width)
    End Sub
    Private Sub PanMainLeft_Resize(NewWidth As Double)
        Dim Delta As Double = NewWidth - RectLeftBackground.Width
        If Math.Abs(Delta) > 0.1 AndAlso AniControlEnabled = 0 AndAlso IsLoaded Then
            AniStop("FrmMain LeftChange")
            If NewWidth > 0 Then
                '宽度足够，显示
                AniStart({
                    AaWidth(RectLeftBackground, Delta, 180,, New AniEaseOutFluent(AniEasePower.ExtraStrong)),
                    AaOpacity(RectLeftShadow, 1 - RectLeftShadow.Opacity, 180)
                }, "FrmMain LeftChange", True)
            Else
                '宽度不足，隐藏
                AniStart({
                    AaWidth(RectLeftBackground, -RectLeftBackground.Width, 180,, New AniEaseOutFluent),
                    AaOpacity(RectLeftShadow, -RectLeftShadow.Opacity, 180)
                }, "FrmMain LeftChange", True)
            End If
        Else
            AniStop("FrmMain LeftChange")
            RectLeftBackground.Width = NewWidth
            RectLeftShadow.Opacity = If(NewWidth > 0, 1, 0)
        End If
    End Sub

    Private Sub BtnTitleSelect_Check(sender As Object, raiseByMouse As Boolean) Handles BtnTitleSelect0.Check, BtnTitleSelect1.Check,
            BtnTitleSelect2.Check, BtnTitleSelect3.Check, BtnTitleSelect4.Check
        If IsChangingPage Then Return
        Dim TargetId As Integer = Val(DirectCast(sender, MyRadioButton).Tag)
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
                If TypeOf TargetLeft Is PageLaunchLeft Then DirectCast(TargetLeft, PageLaunchLeft).RefreshAfterPageEnter()
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
        Dim RawIsChanging = IsChangingPage
        IsChangingPage = True '防止勾选触发 PageChangeActual 造成递归
        Buttons(CurrentTab).SetChecked(True, False, True)
        IsChangingPage = RawIsChanging
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
        '在有弹窗时：回车选择第一个，Esc 选择最后一个
        If PanMsg.Children.Count > 0 Then
            Dim Msg = TryCast(PanMsg.Children(0), MyMsgBoxBase)
            If Msg IsNot Nothing Then
                If e.Key = Key.Enter Then
                    Msg.Btn1_Click()
                    e.Handled = True
                    Return
                ElseIf e.Key = Key.Escape Then
                    If TypeOf Msg IsNot MyMsgInput AndAlso TypeOf Msg IsNot MyMsgSelect AndAlso Msg.Btn3.IsVisible Then
                        Msg.Btn3_Click()
                    ElseIf Msg.Btn2.IsVisible Then
                        Msg.Btn2_Click()
                    Else
                        Msg.Btn1_Click()
                    End If
                    e.Handled = True
                    Return
                End If
            End If
        End If
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
            '标题栏显示类型（默认的 PCL 标识使用 PCL English 字体渲染）
            Dim LogoType As Integer = Settings.Get(Of Integer)("UiLogoType")
            LabTitleLogo.IsVisible = LogoType = 1 OrElse LogoType = 2
            LabTitleLogo.Text = If(LogoType = 1, "PCL.Avalonia", If(LogoType = 2, Settings.Get(Of String)("UiLogoText"), ""))
            ImageTitleLogo.IsVisible = LogoType = 3
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

#Region "文件拖放"

    ''' <summary>
    ''' 文件拖放时反馈给系统的效果。
    ''' Wayland 后端只支持 Copy/Move，Link 会被映射为“无动作”，导致拖动源取消拖放、收不到 Drop 事件。
    ''' </summary>
    Private Shared ReadOnly Property FileDragEffect As DragDropEffects
        Get
            If OperatingSystem.IsLinux() AndAlso Not String.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")) Then
                Return DragDropEffects.Copy
            End If
            Return DragDropEffects.Link
        End Get
    End Property

    Private Sub FormMain_HandleDrag(sender As Object, e As DragEventArgs)
        Try
            If e.Handled AndAlso e.DragEffects <> DragDropEffects.None Then Return
            e.Handled = True
            e.DragEffects = DragDropEffects.None
            If e.DataTransfer.Formats.Contains(DataFormat.File) Then
                e.DragEffects = FileDragEffect
            ElseIf e.DataTransfer.Formats.Contains(DataFormat.Text) Then
                Dim Str As String = Nothing
                Try
                    Str = e.DataTransfer.TryGetText()
                Catch
                End Try
                If Not String.IsNullOrEmpty(Str) Then
                    If Str.StartsWithF("authlib-injector:yggdrasil-server:") OrElse
                       Str.StartsWithF("http://") OrElse Str.StartsWithF("https://") OrElse
                       Str.StartsWithF("file:///") Then
                        e.DragEffects = DragDropEffects.Copy
                    End If
                End If
            End If
            Logger.Info($"设置拖放类型：{e.DragEffects}")
        Catch ex As Exception
            Logger.Error(ex, "处理拖放时出错")
        End Try
    End Sub

    Private Sub FormMain_Drop(sender As Object, e As DragEventArgs)
        Try
            ShowWindowToTop()
            '文件拖拽优先：部分平台同时提供文本与文件两种格式，按文件处理才能安装整合包
            If e.DataTransfer.Formats.Contains(DataFormat.File) Then
                e.Handled = True
                e.DragEffects = FileDragEffect
                Dim Files = e.DataTransfer.TryGetFiles()
                If Files Is Nothing Then
                    Hint("请将文件解压后再拖入！", HintType.Red)
                    Return
                End If
                Dim PathList As New List(Of String)
                For Each FileItem In Files
                    Dim LocalPath = FileItem.TryGetLocalPath()
                    If Not String.IsNullOrEmpty(LocalPath) Then PathList.Add(LocalPath)
                Next
                If PathList.Count = 0 Then
                    Hint("请将文件解压后再拖入！", HintType.Red)
                    Return
                End If
                FileDrag(PathList)
            ElseIf e.DataTransfer.Formats.Contains(DataFormat.Text) Then
                Dim Str As String = ""
                Try
                    Str = e.DataTransfer.TryGetText()
                    Logger.Info($"接受文本拖拽：{Str}")
                    If Str.StartsWithF("authlib-injector:yggdrasil-server:") Then
                        e.Handled = True
                        e.DragEffects = DragDropEffects.Copy
                        ApplyAuthlibServer(WebUtility.UrlDecode(Str.Substring("authlib-injector:yggdrasil-server:".Length)))
                    ElseIf Str.StartsWithF("http://") OrElse Str.StartsWithF("https://") Then
                        '直接拖入网址时也按第三方登录服务器处理
                        e.Handled = True
                        e.DragEffects = DragDropEffects.Copy
                        ApplyAuthlibServer(Str)
                    ElseIf Str.StartsWithF("file:///") Then
                        e.Handled = True
                        e.DragEffects = DragDropEffects.Copy
                        Dim FileUri As Uri = Nothing
                        Dim FilePath As String = If(Uri.TryCreate(Str, UriKind.Absolute, FileUri) AndAlso FileUri.IsFile,
                                                     FileUri.LocalPath,
                                                     Uri.UnescapeDataString(Str.Substring("file:///".Length)))
                        FileDrag(New List(Of String) From {FilePath})
                    End If
                Catch ex As Exception
                    Logger.Warn(ex, "无法接取文本拖拽事件")
                End Try
            End If
        Catch ex As Exception
            Logger.Error(ex, "接取拖拽事件失败")
        End Try
    End Sub

    ''' <summary>
    ''' 将拖入的 Authlib 链接应用到当前版本。
    ''' </summary>
    Private Sub ApplyAuthlibServer(AuthlibServer As String)
        Logger.Info($"Authlib 拖拽：{AuthlibServer}")
        If String.IsNullOrWhiteSpace(AuthlibServer) Then Return
        If Not String.IsNullOrEmpty(New ValidateHttp().Validate(AuthlibServer)) Then
            Hint($"输入的 Authlib 验证服务器不符合网址格式（{AuthlibServer}）！", HintType.Red)
            Return
        End If
        Dim Target As McInstance = If(PageCurrent = PageType.InstanceSetup, PageInstanceLeft.Instance, McInstanceSelected)
        If Target Is Nothing Then
            Hint("请先下载游戏，再设置第三方登录！", HintType.Red)
            Return
        End If
        If AuthlibServer = "https://littleskin.cn/api/yggdrasil" Then
            If MyMsgBox($"是否要在版本 {Target.Name} 中开启 LittleSkin 登录？" & vbCrLf &
                        "你可以在 版本设置 → 设置 → 服务器选项 中修改登录方式。", "第三方登录开启确认", "确定", "取消") = 2 Then Return
            Settings.Set("VersionServerLogin", 4, Instance:=Target)
            Settings.Set("VersionServerAuthServer", "https://littleskin.cn/api/yggdrasil", Instance:=Target)
            Settings.Set("VersionServerAuthRegister", "https://littleskin.cn/auth/register", Instance:=Target)
            Settings.Set("VersionServerAuthName", "LittleSkin 登录", Instance:=Target)
        Else
            If MyMsgBox($"是否要在版本 {Target.Name} 中开启第三方登录？" & vbCrLf &
                        $"登录服务器：{AuthlibServer}" & vbCrLf & vbCrLf &
                        "你可以在 版本设置 → 设置 → 服务器选项 中修改登录方式。", "第三方登录开启确认", "确定", "取消") = 2 Then Return
            Settings.Set("VersionServerLogin", 4, Instance:=Target)
            Settings.Set("VersionServerAuthServer", AuthlibServer, Instance:=Target)
            Settings.Set("VersionServerAuthRegister", AuthlibServer.Replace("api/yggdrasil", "auth/register"), Instance:=Target)
            Settings.Set("VersionServerAuthName", "", Instance:=Target)
        End If
        If PageCurrent = PageType.InstanceSetup AndAlso PageCurrentSub = PageSubType.InstanceSetup Then
            Dim SetupPage = TryCast(PageRight, PageInstanceSetup)
            SetupPage?.Reload()
        ElseIf PageCurrent = PageType.Launch Then
            FrmLaunchLeft.RefreshPage(True, False)
        End If
    End Sub

    Private Sub FileDrag(FilePathList As IEnumerable(Of String))
        RunInNewThread(
        Sub()
            Try
                Dim Files As List(Of String) = FilePathList.ToList()
                Dim FilePath As String = Files.First
                Logger.Info($"接受文件拖拽：{FilePath}{If(Files.Count > 1, $" 等 {Files.Count} 个文件", "")}")
                '基础检查
                If DirectoryUtils.Exists(FilePath) AndAlso Not FileUtils.Exists(FilePath) Then
                    Hint("请拖入一个文件，而非文件夹！", HintType.Red)
                    Return
                ElseIf Not FileUtils.Exists(FilePath) Then
                    Hint("拖入的文件不存在：" & FilePath, HintType.Red)
                    Return
                End If
                '多文件拖拽
                If Files.Count > 1 Then
                    For Each File In Files
                        If Not {"jar", "litemod", "disabled", "old"}.Contains(PathUtils.GetExtension(File)) Then
                            Hint("一次请只拖入一个文件！", HintType.Red)
                            Return
                        End If
                    Next
                End If
                '主页
                Dim Extension As String = PathUtils.GetExtension(FilePath)
                If Extension = "xaml" Then
                    Logger.Info("文件后缀为 XAML，作为主页加载")
                    If FileUtils.Exists(PathData & "Custom.xaml") Then
                        If MyMsgBox("已存在一个主页文件，是否要将它覆盖？", "覆盖确认", "覆盖", "取消") = 2 Then Return
                    End If
                    FileUtils.Copy(FilePath, PathData & "Custom.xaml")
                    RunInUi(
                    Sub()
                        Settings.Set("UiCustomType", 1)
                        FrmLaunchRight.ForceRefresh()
                        Hint("已加载主页自定义文件！", HintType.Green)
                    End Sub)
                    Return
                End If
                '安装 Mod
                If PageInstanceMod.InstallMods(Files) Then Return
                '安装整合包
                If {"zip", "rar", "mrpack"}.Contains(Extension) Then
                    Hint("正在读取压缩包……")
                    Try
                        Logger.Info("文件为压缩包，尝试作为整合包安装")
                        ModpackInstall(FilePath)
                        RunInUi(Sub() FrmMain.PageChange(FormMain.PageType.TaskManager))
                        Return
                    Catch ex As Exception
                        ex.ThrowIfCanceled()
                    End Try
                End If
                'RAR 处理
                If Extension = "rar" Then
                    Hint("PCL 无法处理 rar 格式的压缩包，请在解压后重新压缩为 zip 格式再试！", HintType.Red)
                    Return
                End If
                '错误报告分析
                Try
                    Logger.Info("尝试进行错误报告分析")
                    Dim Analyzer As New CrashAnalyzer
                    Analyzer.Import(FilePath)
                    If Not Analyzer.Prepare() Then Return
                    Analyzer.Analyze()
                    Analyzer.Output(True, New List(Of String))
                    Return
                Catch ex As Exception
                    Logger.Error(ex, "自主错误报告分析失败")
                End Try
                Hint("PCL 无法确定适合的操作……", HintType.Red)
            Catch ex As Exception
                Logger.Error(ex, "处理拖入文件失败", LogBehavior.Toast)
            End Try
        End Sub, "文件拖拽")
    End Sub

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
        '窗口真正显示并获得焦点时才开始淡入，避免动画在 Show 返回前就播放完毕
        If Not ShowAnimationScheduled AndAlso Not ShowAnimationStarted Then
            ShowAnimationScheduled = True
            If HadSplashOnStart Then
                AniStart({AaCode(AddressOf StartWindowShowAnimation, 250)}, "Form Show Delay")
            Else
                StartWindowShowAnimation()
            End If
        End If
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