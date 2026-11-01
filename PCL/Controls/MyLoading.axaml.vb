Imports PCL.MyLoading
Imports Path = Avalonia.Controls.Shapes.Path
Imports Rectangle = Avalonia.Controls.Shapes.Rectangle

Public Class MyLoading
    Inherits Grid

    Public Event IsErrorChanged(sender As Object, isError As Boolean)
    Public Event StateChanged(sender As Object, newState As MyLoadingState, oldState As MyLoadingState)
    Public Event Click(sender As Object, e As PointerReleasedEventArgs)

    Public Property AutoRun As Boolean = True
    Public Uuid As Integer = GetUuid()

#Region "颜色"

    Public Shared ReadOnly ForegroundProperty As StyledProperty(Of IBrush) =
        AvaloniaProperty.Register(Of MyLoading, IBrush)("Foreground", Nothing)
    Public Property Foreground As IBrush
        Get
            Return GetValue(ForegroundProperty)
        End Get
        Set(value As IBrush)
            SetValue(ForegroundProperty, value)
        End Set
    End Property

    Public Sub New()
        InitControls()
        SetResourceReference(ForegroundProperty, "ColorBrush3")
        ApplyForeground()
        AddHandler PointerReleased, AddressOf Button_MouseUp
        RefreshText()
    End Sub

    Private Sub ApplyForeground()
        If PathPickaxe Is Nothing Then Return
        Dim Brush = Foreground
        If Brush Is Nothing Then
            PathPickaxe.SetResourceReference(Path.StrokeProperty, "ColorBrush3")
            PathLeft.SetResourceReference(Path.FillProperty, "ColorBrush3")
            PathRight.SetResourceReference(Path.FillProperty, "ColorBrush3")
            PathError.SetResourceReference(Path.FillProperty, "ColorBrush3")
            RectLine.SetResourceReference(Rectangle.FillProperty, "ColorBrush3")
        Else
            PathPickaxe.Stroke = Brush
            PathLeft.Fill = Brush
            PathRight.Fill = Brush
            PathError.Fill = Brush
            RectLine.Fill = Brush
        End If
        LabText.Foreground = Foreground
    End Sub

#End Region

#Region "文本"

    Private Property _ShowProgress As Boolean = False
    Public Property ShowProgress As Boolean
        Get
            Return _ShowProgress
        End Get
        Set(value As Boolean)
            If _ShowProgress = value Then Return
            _ShowProgress = value
            RefreshText()
        End Set
    End Property

    Private _Text As String = "加载中"
    Public Property Text As String
        Get
            Return _Text
        End Get
        Set(value As String)
            _Text = value
            RefreshText()
        End Set
    End Property

    Private _TextError As String = "加载失败"
    Public Property TextError As String
        Get
            Return _TextError
        End Get
        Set(value As String)
            _TextError = value
            RefreshText()
        End Set
    End Property

    ''' <summary>
    ''' 是否在使用 Loader 时使用 Loader 的错误输出来替换默认的错误文本显示。
    ''' </summary>
    Public Property TextErrorInherit As Boolean = True

    Private Sub RefreshText()
        If LabText Is Nothing Then Return
        RunInUi(
        Sub()
            If InnerState = MyLoadingState.Error Then
                If TextErrorInherit AndAlso State IsNot Nothing AndAlso State.IsLoader Then
                    Dim ErrorText As String = Nothing
                    Try
                        Dim Ex = CType(CType(State, Object).Error, Exception)
                        ErrorText = If(Ex Is Nothing, "未知错误", Ex.Message)
                    Catch
                        ErrorText = TextError
                    End Try
                    LabText.Text = ErrorText
                Else
                    LabText.Text = TextError
                End If
            Else
                If ShowProgress AndAlso State IsNot Nothing AndAlso State.IsLoader Then
                    Try
                        Dim Progress = CDbl(CType(State, Object).Progress)
                        LabText.Text = Text & " - " & Math.Floor(Progress * 100) & "%"
                    Catch
                        LabText.Text = Text
                    End Try
                Else
                    LabText.Text = Text
                End If
            End If
        End Sub)
    End Sub

#End Region

#Region "状态改变"

    Public Enum MyLoadingState
        Unloaded = -1
        Run = 0
        [Stop] = 1
        [Error] = 2
    End Enum

    Private _State As ILoadingTrigger
    Public Property State As ILoadingTrigger
        Get
            InitState()
            Return _State
        End Get
        Set(value As ILoadingTrigger)
            UnhookState()
            _State = value
            HookState()
            RefreshState()
        End Set
    End Property

    Private IsControlLoaded As Boolean = False

    Private Sub InitState()
        If _State IsNot Nothing Then Return
        _State = New MyLoadingStateSimulator
        HookState()
        If AutoRun Then _State.LoadingState = MyLoadingState.Run
    End Sub

    Private Sub HookState()
        If _State Is Nothing Then Return
        AddHandler _State.LoadingStateChanged, AddressOf State_LoadingStateChanged
        AddHandler _State.ProgressChanged, AddressOf State_ProgressChanged
    End Sub

    Private Sub UnhookState()
        If _State Is Nothing Then Return
        RemoveHandler _State.LoadingStateChanged, AddressOf State_LoadingStateChanged
        RemoveHandler _State.ProgressChanged, AddressOf State_ProgressChanged
    End Sub

    Private Sub State_LoadingStateChanged(NewState As MyLoadingState, OldState As MyLoadingState)
        RefreshState()
    End Sub

    Private Sub State_ProgressChanged(NewProgress As Double, OldProgress As Double)
        RefreshText()
    End Sub

    Protected Overrides Sub OnAttachedToVisualTree(e As VisualTreeAttachmentEventArgs)
        MyBase.OnAttachedToVisualTree(e)
        IsControlLoaded = True
        InitState()
        RefreshState()
        RefreshText()
    End Sub

    Protected Overrides Sub OnDetachedFromVisualTree(e As VisualTreeAttachmentEventArgs)
        MyBase.OnDetachedFromVisualTree(e)
        IsControlLoaded = False
        RefreshState()
        AniStop("MyLoader Loop " & Uuid)
        IsLooping = False
    End Sub

    Protected Overrides Sub OnPropertyChanged(change As AvaloniaPropertyChangedEventArgs)
        MyBase.OnPropertyChanged(change)
        If change.Property Is ForegroundProperty Then ApplyForeground()
    End Sub

    Private Sub RefreshState()
        If _State Is Nothing Then Return
        If _State.LoadingState = MyLoadingState.Run AndAlso Not IsControlLoaded Then InnerState = MyLoadingState.Stop
        InnerState = _State.LoadingState
        OuterState = _State.LoadingState
        AniLoop()
    End Sub

    '用于引发外部事件的状态
    Private Property _OuterState As MyLoadingState = MyLoadingState.Unloaded
    Private Property OuterState As MyLoadingState
        Get
            Return _OuterState
        End Get
        Set(value As MyLoadingState)
            If _OuterState = value Then Return
            Dim OldValue = _OuterState
            _OuterState = value
            RaiseEvent StateChanged(Me, value, OldValue)
            If (OldValue = MyLoadingState.Error) <> (value = MyLoadingState.Error) Then RaiseEvent IsErrorChanged(Me, value = MyLoadingState.Error)
        End Set
    End Property

    '用于引发内部动画事件的状态
    Private Property _InnerState As MyLoadingState = MyLoadingState.Unloaded
    Private Property InnerState As MyLoadingState
        Get
            Return _InnerState
        End Get
        Set(value As MyLoadingState)
            If _InnerState = value Then Return
            Dim OldValue = _InnerState
            _InnerState = value
            AniLoop()
            If (OldValue = MyLoadingState.Error) <> (value = MyLoadingState.Error) Then ErrorAnimation(Me, value = MyLoadingState.Error)
            RefreshText()
        End Set
    End Property

#End Region

#Region "动画"

    ''' <summary>
    ''' 是否需要动画。
    ''' </summary>
    Public Property HasAnimation As Boolean = True

    ''' <summary>
    ''' 主动画循环是否正在运行中。
    ''' </summary>
    Private IsLooping As Boolean = False

    Private Sub AniLoop()
        If Not HasAnimation OrElse IsLooping OrElse Not InnerState = MyLoadingState.Run OrElse AniSpeed > 10 OrElse Not IsControlLoaded Then Return
        IsLooping = True
        ErrorAnimationWaiting = True
        AniStart({
            AaRotateTransform(PathPickaxe, -20 - AniAngle(PathPickaxe), 350, 250, New AniEaseInBack(AniEasePower.Weak)),
            AaRotateTransform(PathPickaxe, 50, 900, , New AniEaseOutFluent, True),
            AaRotateTransform(PathPickaxe, 25, 900, , New AniEaseOutElastic(AniEasePower.Weak)),
            AaCode(
            Sub()
                PathLeft.Opacity = 1
                PathLeft.Margin = New Thickness(7, 41, 0, 0)
                PathRight.Opacity = 1
                PathRight.Margin = New Thickness(14, 41, 0, 0)
                ErrorAnimationWaiting = False
            End Sub),
            AaOpacity(PathLeft, -1, 100, 50),
            AaX(PathLeft, -5, 180, , New AniEaseOutFluent),
            AaY(PathLeft, -6, 180, , New AniEaseOutFluent),
            AaOpacity(PathRight, -1, 100, 50),
            AaX(PathRight, 5, 180, , New AniEaseOutFluent),
            AaY(PathRight, -6, 180, , New AniEaseOutFluent),
            AaCode(
            Sub()
                IsLooping = False
                AniLoop()
            End Sub, , True)
        }, "MyLoader Loop " & Uuid)
    End Sub

    ''' <summary>
    ''' 镐子是否还没挥下去，要求错误动画等待。
    ''' </summary>
    Private ErrorAnimationWaiting As Boolean = False

    ''' <summary>
    ''' 停止挥动循环，并让镐子回到静止姿态。错误或停止时不应继续播放敲击动画。
    ''' </summary>
    Private Sub StopLoop()
        AniStop("MyLoader Loop " & Uuid)
        IsLooping = False
        ErrorAnimationWaiting = False
        PathLeft.Opacity = 0
        PathRight.Opacity = 0
        If Math.Abs(AniAngle(PathPickaxe) - 55) > 0.5 Then
            AniStart(AaRotateTransform(PathPickaxe, 55 - AniAngle(PathPickaxe), 150, , New AniEaseOutFluent), "MyLoader Loop " & Uuid)
        End If
    End Sub

    Private Sub ErrorAnimation(sender As Object, isError As Boolean)
        If isError Then
            Dim Wait As Integer = If(ErrorAnimationWaiting, 400, 0)
            StopLoop()
            AniStart({
                AaColor(Me, ForegroundProperty, "ColorBrushRedLight", 300),
                AaOpacity(PathError, 1 - PathError.Opacity, 100, 300 + Wait),
                AaScaleTransform(PathError, 1 - AniScaleX(PathError), 400, 300 + Wait, New AniEaseOutBack)
            }, "MyLoader Error " & Uuid)
        Else
            AniStart({
                AaOpacity(PathError, -PathError.Opacity, 100),
                AaScaleTransform(PathError, 0.5 - AniScaleX(PathError), 200),
                AaColor(Me, ForegroundProperty, "ColorBrush3", 300)
            }, "MyLoader Error " & Uuid)
        End If
    End Sub

#End Region

#Region "点击事件"

    Private Sub Button_MouseUp(sender As Object, e As PointerReleasedEventArgs)
        '提前释放隐式指针捕获，避免 Click 中打开的模态弹窗被本控件持续捕获指针
        e.Pointer.Capture(Nothing)
        RaiseEvent Click(sender, e)
    End Sub

#End Region

End Class

Public Interface ILoadingTrigger
    ReadOnly Property IsLoader As Boolean
    Property LoadingState As MyLoadingState
    Event LoadingStateChanged(NewState As MyLoadingState, OldState As MyLoadingState)
    Event ProgressChanged(NewProgress As Double, OldProgress As Double)
End Interface

Public Class MyLoadingStateSimulator
    Implements ILoadingTrigger

    Private Property _LoadingState As MyLoadingState = MyLoadingState.Unloaded
    Public Property LoadingState As MyLoadingState Implements ILoadingTrigger.LoadingState
        Get
            Return _LoadingState
        End Get
        Set(value As MyLoadingState)
            If _LoadingState = value Then Return
            Dim OldState = _LoadingState
            _LoadingState = value
            RaiseEvent LoadingStateChanged(value, OldState)
        End Set
    End Property
    Public ReadOnly Property IsLoader As Boolean = False Implements ILoadingTrigger.IsLoader

    Public Event LoadingStateChanged(NewState As MyLoadingState, OldState As MyLoadingState) Implements ILoadingTrigger.LoadingStateChanged
    Public Event ProgressChanged(NewProgress As Double, OldProgress As Double) Implements ILoadingTrigger.ProgressChanged
End Class
