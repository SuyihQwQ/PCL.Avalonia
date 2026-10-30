Imports Avalonia.VisualTree

''' <summary>
''' 右侧页面容器，负责页面切换、加载环与内容进出动画的状态调度。
''' </summary>
Public Class MyPageRight
    Inherits ContentControl

    Public PageUuid As Integer = GetUuid()

    ''' <summary>
    ''' “返回顶部” 按钮检测的滚动区域。
    ''' </summary>
    Public Shared ReadOnly PanScrollProperty As StyledProperty(Of String) =
        AvaloniaProperty.Register(Of MyPageRight, String)("PanScroll", Nothing)
    Public Property PanScroll As String
        Get
            Return GetValue(PanScrollProperty)
        End Get
        Set(value As String)
            SetValue(PanScrollProperty, value)
        End Set
    End Property

    '当前状态
    Public Enum PageStates
        Empty '默认状态，页面全空
        LoaderWait '加载环初始等待
        LoaderEnter '加载环进入动画
        LoaderStayForce '加载环正常显示（强制等待）
        LoaderStay '加载环正常显示
        LoaderExit '加载环退出动画
        ContentEnter '内容进入动画
        ContentStay '内容正常显示
        ContentExit '刷新导致的全部退出动画，或页面内容退出（子页面更改）导致的全部退出动画
        PageExit '切换页面导致的全部退出动画
    End Enum
    Private _PageState As PageStates = PageStates.Empty
    Public Property PageState As PageStates
        Get
            Return _PageState
        End Get
        Set(value As PageStates)
            If _PageState = value Then Return
            _PageState = value
            Logger.Trace($"页面状态切换为 {value}")
        End Set
    End Property

    ''' <summary>
    ''' 加载结束后出现的内容容器。为 Nothing 时使用 Content。
    ''' </summary>
    Public Property PanContent As Control
    ''' <summary>
    ''' 无论是否在加载总是要显示的容器。可以为 Nothing。
    ''' </summary>
    Public Property PanAlways As Control
    ''' <summary>
    ''' 加载环对应的卡片。
    ''' </summary>
    Public Property PanLoader As Control

    Private ReadOnly Property ContentElement As Control
        Get
            If PanContent IsNot Nothing Then Return PanContent
            Return TryCast(Content, Control)
        End Get
    End Property

#Region "加载器"

    Private PageLoader As LoaderBase
    Private PageLoaderUi As MyLoading
    Private PageLoaderAutoRun As Boolean

    ''' <summary>
    ''' 表明页面存在需要在后台执行的加载器。
    ''' </summary>
    ''' <param name="LoaderUi">MyLoading 控件。</param>
    ''' <param name="PanLoader">MyLoading 控件对应的卡片。</param>
    ''' <param name="PanContent">加载结束后出现的内容容器。</param>
    ''' <param name="PanAlways">无论是否在加载总是要显示的容器。可以为 Nothing。</param>
    ''' <param name="RealLoader">在工作线程执行的加载器。</param>
    ''' <param name="FinishedInvoke">当加载器执行完成，在 UI 线程触发的 UI 初始化事件。</param>
    ''' <param name="InputInvoke">获取加载器输入的委托。</param>
    ''' <param name="AutoRun">是否在初始化时立即运行加载器。</param>
    Public Sub PageLoaderInit(Of TIn, TOut)(LoaderUi As MyLoading, PanLoader As Control, PanContent As Control, PanAlways As Control,
                              RealLoader As LoaderTask(Of TIn, TOut),
                              Optional FinishedInvoke As Action(Of LoaderTask(Of TIn, TOut)) = Nothing, Optional InputInvoke As Func(Of TIn) = Nothing,
                              Optional AutoRun As Boolean = True)
        Me.PanLoader = PanLoader
        Me.PanContent = PanContent
        Me.PanAlways = PanAlways
        PageLoader = RealLoader
        PageLoaderUi = LoaderUi
        PageLoaderAutoRun = AutoRun
        If FinishedInvoke IsNot Nothing Then
            AddHandler RealLoader.PreviewFinish,
            Sub()
                Do While PageState = PageStates.PageExit OrElse PageState = PageStates.ContentExit
                    Thread.Sleep(10) '不在退出动画时执行 UI 线程操作，避免退出动画被重置
                Loop
                RunInUiWait(Sub() FinishedInvoke(RealLoader))
                Thread.Sleep(20) '由于大量初始化控件会导致掉帧，延迟触发 State 改变事件
            End Sub
        End If
        AddHandler RealLoader.OnStateChangedUi, Sub(Loader As LoaderBase, NewState As LoadState, OldState As LoadState) RunInUi(Sub() PageLoaderState(Loader, NewState, OldState))
        PanLoader.IsVisible = False
        PanContent.IsVisible = False
        If PanAlways IsNot Nothing Then PanAlways.IsVisible = False
        If PageLoaderAutoRun Then
            RealLoader.Start(RealLoader.StartGetInput(Nothing, InputInvoke))
        End If
        If RealLoader.State = LoadState.Finished AndAlso FinishedInvoke IsNot Nothing Then
            RunInUiWait(Sub() FinishedInvoke(RealLoader))
        End If
        LoaderUi.State = RealLoader
        AddHandler LoaderUi.Click, Sub() If RealLoader.State = LoadState.Failed Then PageLoaderRestart()
    End Sub

    Public Sub PageLoaderInit(Of TIn)(LoaderUi As MyLoading, PanLoader As Control, PanContent As Control, PanAlways As Control,
                              RealLoader As LoaderCombo(Of TIn),
                              Optional FinishedInvoke As Action(Of LoaderCombo(Of TIn)) = Nothing, Optional InputInvoke As Func(Of TIn) = Nothing,
                              Optional AutoRun As Boolean = True)
        Me.PanLoader = PanLoader
        Me.PanContent = PanContent
        Me.PanAlways = PanAlways
        PageLoader = RealLoader
        PageLoaderUi = LoaderUi
        PageLoaderAutoRun = AutoRun
        If FinishedInvoke IsNot Nothing Then
            AddHandler RealLoader.PreviewFinish,
            Sub()
                Do While PageState = PageStates.PageExit OrElse PageState = PageStates.ContentExit
                    Thread.Sleep(10) '不在退出动画时执行 UI 线程操作，避免退出动画被重置
                Loop
                RunInUiWait(Sub() FinishedInvoke(RealLoader))
                Thread.Sleep(20) '由于大量初始化控件会导致掉帧，延迟触发 State 改变事件
            End Sub
        End If
        AddHandler RealLoader.OnStateChangedUi, Sub(Loader As LoaderBase, NewState As LoadState, OldState As LoadState) RunInUi(Sub() PageLoaderState(Loader, NewState, OldState))
        PanLoader.IsVisible = False
        PanContent.IsVisible = False
        If PanAlways IsNot Nothing Then PanAlways.IsVisible = False
        If PageLoaderAutoRun Then
            Dim Input As Object = Nothing
            If InputInvoke IsNot Nothing Then Input = InputInvoke()
            RealLoader.Start(Input)
        End If
        If RealLoader.State = LoadState.Finished AndAlso FinishedInvoke IsNot Nothing Then
            RunInUiWait(Sub() FinishedInvoke(RealLoader))
        End If
        LoaderUi.State = RealLoader
        AddHandler LoaderUi.Click, Sub() If RealLoader.State = LoadState.Failed Then PageLoaderRestart()
    End Sub

    ''' <summary>
    ''' 重新运行加载器。
    ''' </summary>
    Public Sub PageLoaderRestart(Optional Input As Object = Nothing, Optional IsForceRestart As Boolean = True)
        If Not PageLoaderAutoRun OrElse PageLoader Is Nothing Then Return
        PageLoader.Start(Input, IsForceRestart)
    End Sub

    ''' <summary>
    ''' 加载器的状态已改变。
    ''' </summary>
    Private Sub PageLoaderState(sender As Object, NewState As LoadState, OldState As LoadState)
        Select Case NewState
            Case LoadState.Failed, LoadState.Loading
                If OldState = LoadState.Failed OrElse OldState = LoadState.Loading Then Return
                Logger.Trace("已触发 PageLoaderState (Start/Refresh)")
                Select Case PageState
                    Case PageStates.ContentEnter, PageStates.ContentStay
                        PageState = PageStates.ContentExit
                        TriggerExitAnimation(ContentElement)
                    Case PageStates.LoaderExit
                        PageState = PageStates.ContentExit
                End Select
            Case LoadState.Finished, LoadState.Canceled, LoadState.Waiting
                If Not (OldState = LoadState.Failed OrElse OldState = LoadState.Loading) Then Return
                Logger.Trace("已触发 PageLoaderState (Stop/Canceled)")
                Select Case PageState
                    Case PageStates.LoaderWait
                        PageState = PageStates.ContentEnter
                        If PanAlways IsNot Nothing AndAlso Not PanAlways.IsVisible Then
                            TriggerEnterAnimation(PanAlways, ContentElement)
                        Else
                            TriggerEnterAnimation(ContentElement)
                        End If
                    Case PageStates.LoaderStay
                        PageState = PageStates.LoaderExit
                        TriggerExitAnimation(PanLoader)
                End Select
        End Select
    End Sub

#End Region

#Region "事件"

    ''' <summary>
    ''' 需要切换到当前页面，并且原本的 Loaded 事件已执行完成。
    ''' </summary>
    Public Sub PageOnEnter()
        Logger.Trace("已触发 PageOnEnter")
        RaiseEvent PageEnter()
        Select Case PageState
            Case PageStates.Empty
                If PageLoader Is Nothing OrElse PageLoader.State = LoadState.Finished OrElse PageLoader.State = LoadState.Waiting OrElse PageLoader.State = LoadState.Canceled Then
                    PageState = PageStates.ContentEnter
                    TriggerEnterAnimation(PanAlways, ContentElement)
                ElseIf PageLoader.State = LoadState.Loading Then
                    PageState = PageStates.LoaderWait
                    AniStart(AaCode(AddressOf PageOnLoaderWaitFinished, 400), "PageRight PageChange " & PageUuid)
                Else
                    PageState = PageStates.LoaderEnter
                    TriggerEnterAnimation(PanAlways, PanLoader)
                End If
            Case PageStates.ContentExit
                If PageLoader Is Nothing OrElse PageLoader.State = LoadState.Finished OrElse PageLoader.State = LoadState.Waiting OrElse PageLoader.State = LoadState.Canceled Then
                    PageState = PageStates.ContentEnter
                    TriggerEnterAnimation(ContentElement)
                ElseIf PageLoader.State = LoadState.Loading Then
                    PageState = PageStates.LoaderWait
                    AniStart(AaCode(AddressOf PageOnLoaderWaitFinished, 400), "PageRight PageChange " & PageUuid)
                Else
                    PageState = PageStates.LoaderEnter
                    TriggerEnterAnimation(PanLoader)
                End If
            Case PageStates.ContentEnter
                '重复调用 PageOnEnter，直接忽略
            Case Else
                Throw New Exception("在状态为 " & PageState.ToString() & " 时触发了 PageOnEnter 事件。")
        End Select
    End Sub
    Public Event PageEnter()
    ''' <summary>
    ''' 需要切换到其他页面。
    ''' </summary>
    Public Sub PageOnExit()
        Logger.Trace("已触发 PageOnExit")
        RaiseEvent PageExit()
        Select Case PageState
            Case PageStates.ContentEnter, PageStates.ContentStay
                PageState = PageStates.PageExit
                TriggerExitAnimation(PanAlways, ContentElement)
            Case PageStates.LoaderEnter, PageStates.LoaderStayForce, PageStates.LoaderStay
                PageState = PageStates.PageExit
                TriggerExitAnimation(PanAlways, PanLoader)
            Case PageStates.LoaderWait
                PageState = PageStates.PageExit
                TriggerExitAnimation(PanAlways)
            Case PageStates.LoaderExit, PageStates.ContentExit
                PageState = PageStates.PageExit
                If PanAlways IsNot Nothing Then TriggerExitAnimation(PanAlways, ContentElement)
            Case PageStates.PageExit, PageStates.Empty
        End Select
    End Sub
    Public Event PageExit()
    ''' <summary>
    ''' 即将切换到其他页面，需要强制完成页面状态清理。
    ''' </summary>
    Public Sub PageOnForceExit()
        If PageState = PageStates.Empty Then Return
        Logger.Trace("已触发 PageOnForceExit")
        PageState = PageStates.Empty
        AniStop("PageRight PageChange " & PageUuid)
        '由于动画会被强制中止，所以需要手动进行隐藏
        If ContentElement IsNot Nothing Then ContentElement.IsVisible = False
        If PanLoader IsNot Nothing Then PanLoader.IsVisible = False
        If PanAlways IsNot Nothing Then PanAlways.IsVisible = False
    End Sub
    ''' <summary>
    ''' PanContent 中的子页面改变，需要让当前内容退出，再显示新的内容。
    ''' </summary>
    Public Sub PageOnContentExit()
        Logger.Trace("已触发 PageOnContentExit")
        Select Case PageState
            Case PageStates.ContentEnter, PageStates.ContentStay
                PageState = PageStates.ContentExit
                TriggerExitAnimation(ContentElement)
            Case PageStates.LoaderExit
                PageState = PageStates.ContentExit
            Case PageStates.LoaderEnter, PageStates.LoaderStayForce, PageStates.LoaderStay
                PageState = PageStates.ContentExit
                TriggerExitAnimation(PanLoader)
            Case PageStates.LoaderWait
                PageOnEnter()
            Case PageStates.Empty
                '可能是在离开页面后，其他后台线程调用了本函数，此时不应触发 PageOnEnter()
                If IsLoaded Then PageOnEnter()
        End Select
    End Sub

    '内部触发的事件
    Private Sub PageOnEnterAnimationFinished()
        Logger.Trace("已触发 PageOnEnterAnimationFinished")
        Select Case PageState
            Case PageStates.ContentEnter
                PageState = PageStates.ContentStay
            Case PageStates.LoaderEnter
                PageState = PageStates.LoaderStayForce
                AniStart(AaCode(AddressOf PageOnLoaderStayFinished, 400), "PageRight PageChange " & PageUuid)
            Case Else
                Throw New Exception("在状态为 " & PageState.ToString() & " 时触发了 PageOnEnterAnimationFinished 事件。")
        End Select
    End Sub

    ''' <summary>
    ''' 加载环进入等待已结束。
    ''' </summary>
    Private Sub PageOnLoaderWaitFinished()
        Logger.Trace("已触发 PageOnLoaderWaitFinished")
        Select Case PageState
            Case PageStates.LoaderWait
                PageState = PageStates.LoaderEnter
                If PanAlways IsNot Nothing AndAlso Not PanAlways.IsVisible Then
                    TriggerEnterAnimation(PanAlways, PanLoader)
                Else
                    TriggerEnterAnimation(PanLoader)
                End If
            Case Else
                Throw New Exception("在状态为 " & PageState.ToString() & " 时触发了 PageOnLoaderWaitFinished 事件。")
        End Select
    End Sub

    ''' <summary>
    ''' 加载环强制展示已结束，若加载器仍未完成，则进入正常等待状态。
    ''' </summary>
    Private Sub PageOnLoaderStayFinished()
        Logger.Trace("已触发 PageOnLoaderStayFinished")
        Select Case PageState
            Case PageStates.LoaderStayForce
                If PageLoader.State = LoadState.Finished Then
                    PageState = PageStates.LoaderExit
                    TriggerExitAnimation(PanLoader)
                Else
                    PageState = PageStates.LoaderStay
                End If
            Case Else
                Throw New Exception("在状态为 " & PageState.ToString() & " 时触发了 PageOnLoaderStayFinished 事件。")
        End Select
    End Sub
    Private Sub PageOnExitAnimationFinished()
        Logger.Trace("已触发 PageOnExitAnimationFinished")
        Select Case PageState
            Case PageStates.PageExit
                PageState = PageStates.Empty
            Case PageStates.ContentExit
                PageOnEnter()
            Case PageStates.LoaderExit
                PageState = PageStates.ContentEnter
                TriggerEnterAnimation(ContentElement)
            Case Else
                Throw New Exception("在状态为 " & PageState.ToString() & " 时触发了 PageOnExitAnimationFinished 事件。")
        End Select
    End Sub

#End Region

#Region "动画"

    '逐个进入动画
    Public Sub TriggerEnterAnimation(ParamArray Elements As Control())
        Dim RealElements = Elements.Where(Function(e) e IsNot Nothing).ToList()
        If RealElements.Count = 0 Then
            PageOnEnterAnimationFinished()
            Return
        End If
        For Each Element In RealElements
            Element.IsVisible = True '页面均处于默认的隐藏状态
        Next
        Dim AniList As New List(Of AniData)
        Dim Delay As Integer = 0
        '基础动画
        For Each Element In RealElements
            For Each Item As Control In GetAllAnimControls(Element, True)
                '还原被隐藏的卡片的消失动画
                Item.IsHitTestVisible = True
                If Item.RenderTransform Is Nothing OrElse TypeOf Item.RenderTransform Is TranslateTransform Then Item.RenderTransform = Nothing
            Next
            For Each Item As Control In GetAllAnimControls(Element)
                Item.Opacity = 0
                Item.RenderTransform = New TranslateTransform(0, -16)
                AniList.Add(AaOpacity(Item, 1, 100, Delay, New AniEaseOutFluent(AniEasePower.Weak)))
                AniList.Add(AaTranslateY(Item, 5, 250, Delay, New AniEaseOutFluent))
                AniList.Add(AaTranslateY(Item, 11, 350, Delay, New AniEaseOutBack))
                Delay += 25
            Next
        Next
        '滚动条动画
        Dim Scroll As MyScrollBar = GetFirstScrollBar(RealElements)
        If Scroll IsNot Nothing Then
            If TypeOf Scroll.RenderTransform IsNot TranslateTransform Then Scroll.RenderTransform = New TranslateTransform(10, 0)
            Dim CurrentX = DirectCast(Scroll.RenderTransform, TranslateTransform).X
            AniList.Add(AaTranslateX(Scroll, -CurrentX, 350, 0, New AniEaseOutFluent))
        End If
        '结束
        AniList.Add(AaCode(Sub() PageOnEnterAnimationFinished(),, True))
        AniStart(AniList, "PageRight PageChange " & PageUuid, True)
    End Sub

    '逐个退出动画
    Public Sub TriggerExitAnimation(ParamArray Elements As Control())
        Dim RealElements = Elements.Where(Function(e) e IsNot Nothing).ToList()
        If RealElements.Count = 0 Then
            PageOnExitAnimationFinished()
            Return
        End If
        Dim AniList As New List(Of AniData)
        Dim Delay As Integer = 0
        For Each Element In RealElements
            For Each Item As Control In GetAllAnimControls(Element)
                Item.IsHitTestVisible = False
                AniList.Add(AaOpacity(Item, -1, 70, Delay))
                AniList.Add(AaTranslateY(Item, -6, 70, Delay))
                Delay += 15
            Next
        Next
        '滚动条动画
        Dim Scroll As MyScrollBar = GetFirstScrollBar(RealElements)
        If Scroll IsNot Nothing Then
            If TypeOf Scroll.RenderTransform IsNot TranslateTransform Then Scroll.RenderTransform = New TranslateTransform
            Dim CurrentX = DirectCast(Scroll.RenderTransform, TranslateTransform).X
            AniList.Add(AaTranslateX(Scroll, 10 - CurrentX, 90, 0, New AniEaseInFluent))
        End If
        '结束
        AniList.Add(AaCode(
        Sub()
            For Each Element In RealElements
                Element.IsVisible = False
            Next
            PageOnExitAnimationFinished()
        End Sub,, True))
        AniStart(AniList, "PageRight PageChange " & PageUuid)
    End Sub

    ''' <summary>
    ''' 禁用页面切换动画的控件列表。
    ''' </summary>
    Public DisabledPageAnimControls As New List(Of Control)
    ''' <summary>
    ''' 遍历获取所有需要生成动画的控件。
    ''' </summary>
    Friend Function GetAllAnimControls(Element As Control, Optional IgnoreInvisibility As Boolean = False) As IEnumerable(Of Control)
        Dim AllControls As New List(Of Control)
        _GetAllAnimControls(Element, AllControls, IgnoreInvisibility)
        Return AllControls.Except(DisabledPageAnimControls)
    End Function
    Private Sub _GetAllAnimControls(Element As Control, ByRef AllControls As List(Of Control), IgnoreInvisibility As Boolean)
        If Element Is Nothing Then Return
        If Not IgnoreInvisibility AndAlso Not Element.IsVisible Then Return
        If TypeOf Element Is MyCard OrElse TypeOf Element Is MyHint OrElse TypeOf Element Is TextBlock OrElse TypeOf Element Is MyTextButton Then
            AllControls.Add(Element)
        ElseIf TypeOf Element Is ContentControl Then
            Dim Inner = CType(Element, ContentControl).Content
            If TypeOf Inner Is Control Then _GetAllAnimControls(Inner, AllControls, IgnoreInvisibility)
        ElseIf TypeOf Element Is Panel Then
            For Each Element2 In CType(Element, Panel).Children
                If TypeOf Element2 Is Control Then _GetAllAnimControls(Element2, AllControls, IgnoreInvisibility)
            Next
        End If
    End Sub

    '查找列表中的第一个滚动条
    Private Function GetFirstScrollBar(Elements As IEnumerable(Of Control)) As MyScrollBar
        For Each Element In Elements
            Dim Viewer = FindDescendant(Of MyScrollViewer)(Element)
            If Viewer Is Nothing Then Continue For
            Dim Bar = FindDescendant(Of MyScrollBar)(Viewer)
            If Bar IsNot Nothing Then Return Bar
        Next
        Return Nothing
    End Function
    Private Function FindDescendant(Of T As Control)(Root As Control) As T
        If Root Is Nothing Then Return Nothing
        If TypeOf Root Is T Then Return DirectCast(Root, T)
        For Each Child In Root.GetVisualChildren()
            Dim Result = FindDescendant(Of T)(TryCast(Child, Control))
            If Result IsNot Nothing Then Return Result
        Next
        Return Nothing
    End Function

#End Region

End Class
