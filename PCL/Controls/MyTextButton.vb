Public Class MyTextButton
    Inherits Label

    Public Event Click(sender As Object, e As EventArgs)

    Public Uuid As Integer = GetUuid()

    Public Shared ReadOnly TextProperty As StyledProperty(Of String) =
        AvaloniaProperty.Register(Of MyTextButton, String)("Text", "")
    Public Property Text As String
        Get
            Return GetValue(TextProperty)
        End Get
        Set(value As String)
            SetValue(TextProperty, value)
        End Set
    End Property

    Public IsMouseDown As Boolean = False

    Private IsControlLoaded As Boolean = False
    Private ColorName As String

    Public Sub New()
        SetResourceReference(ForegroundProperty, "ColorBrush1")
        Background = Brushes.Transparent
    End Sub

    Protected Overrides Sub OnAttachedToVisualTree(e As VisualTreeAttachmentEventArgs)
        MyBase.OnAttachedToVisualTree(e)
        IsControlLoaded = True
        RefreshColor()
    End Sub

    Protected Overrides Sub OnDetachedFromVisualTree(e As VisualTreeAttachmentEventArgs)
        MyBase.OnDetachedFromVisualTree(e)
        IsControlLoaded = False
        AniStop("MyTextButton Color " & Uuid)
        AniStop("MyTextButton Text " & Uuid)
    End Sub

    Protected Overrides Sub OnPropertyChanged(change As AvaloniaPropertyChangedEventArgs)
        MyBase.OnPropertyChanged(change)
        If change.Property Is TextProperty Then
            If IsControlLoaded Then
                AniStart({
                    AaOpacity(Me, -Opacity, 50),
                    AaCode(Sub() Content = Text, , True),
                    AaOpacity(Me, 1, 170)
                }, "MyTextButton Text " & Uuid)
            Else
                Content = Text
            End If
        ElseIf change.Property Is IsEnabledProperty Then
            RefreshColor()
        End If
    End Sub

    '鼠标点击
    Protected Overrides Sub OnPointerPressed(e As PointerPressedEventArgs)
        MyBase.OnPointerPressed(e)
        If Not e.GetCurrentPoint(Me).Properties.IsLeftButtonPressed Then Return
        If Not IsEnabled Then Return
        IsMouseDown = True
        RefreshColor()
        e.Handled = True
    End Sub

    Protected Overrides Sub OnPointerReleased(e As PointerReleasedEventArgs)
        MyBase.OnPointerReleased(e)
        If Not IsMouseDown Then Return
        IsMouseDown = False
        If IsEnabled Then
            Logger.Info($"按下文本按钮：{Text}")
            '提前释放隐式指针捕获，避免 Click 中打开的模态弹窗被本控件持续捕获指针
            e.Pointer.Capture(Nothing)
            RaiseEvent Click(Me, EventArgs.Empty)
            RaiseCustomEvent()
        End If
        RefreshColor()
        e.Handled = True
    End Sub

    Protected Overrides Sub OnPointerEntered(e As PointerEventArgs)
        MyBase.OnPointerEntered(e)
        RefreshColor()
    End Sub

    Protected Overrides Sub OnPointerExited(e As PointerEventArgs)
        MyBase.OnPointerExited(e)
        IsMouseDown = False
        RefreshColor()
    End Sub

    '指向动画
    Private Const AnimationTimeIn As Integer = 100
    Private Const AnimationTimeOut As Integer = 200

    Public Sub RefreshColor(Optional sender As Object = Nothing, Optional e As EventArgs = Nothing)
        Dim ForeName As String
        Dim Time As Integer
        If IsMouseDown Then
            ForeName = "ColorBrush4"
            Time = 30
        ElseIf IsPointerOver Then
            ForeName = "ColorBrush3"
            Time = AnimationTimeIn
        Else
            ForeName = "ColorBrush1"
            Time = AnimationTimeOut
        End If
        If ColorName = ForeName Then Return
        ColorName = ForeName
        If IsControlLoaded AndAlso AniControlEnabled = 0 Then
            AniStart(AaColor(Me, ForegroundProperty, ForeName, Time), "MyTextButton Color " & Uuid)
        Else
            AniStop("MyTextButton Color " & Uuid)
            SetResourceReference(ForegroundProperty, ForeName)
        End If
    End Sub

End Class
