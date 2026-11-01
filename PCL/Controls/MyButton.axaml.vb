Public Class MyButton
    Inherits Border

    Public Event Click(sender As Object, e As PointerReleasedEventArgs)

    Public Uuid As Integer = GetUuid()

    '属性穿透：Padding 作用于内部前景边框
    Public Shared Shadows ReadOnly PaddingProperty As StyledProperty(Of Thickness) =
        AvaloniaProperty.Register(Of MyButton, Thickness)("Padding")
    Public Shadows Property Padding As Thickness
        Get
            Return GetValue(PaddingProperty)
        End Get
        Set(value As Thickness)
            SetValue(PaddingProperty, value)
        End Set
    End Property

    Public Shared ReadOnly TextProperty As StyledProperty(Of String) =
        AvaloniaProperty.Register(Of MyButton, String)("Text", "")
    Public Property Text As String
        Get
            Return GetValue(TextProperty)
        End Get
        Set(value As String)
            SetValue(TextProperty, value)
        End Set
    End Property

    ''' <summary>
    ''' 文本的内边距。只会撑宽按钮，不影响按钮内部的其它留白。
    ''' </summary>
    Public Property TextPadding As Thickness
        Get
            Return LabText.Padding
        End Get
        Set(value As Thickness)
            If LabText IsNot Nothing Then LabText.Padding = value
        End Set
    End Property

    Public Enum ColorState
        Normal = 0
        Highlight = 1
        Red = 2
    End Enum
    Private _ColorType As ColorState = ColorState.Normal
    Public Property ColorType As ColorState
        Get
            Return _ColorType
        End Get
        Set(value As ColorState)
            _ColorType = value
            RefreshColor()
        End Set
    End Property

    Public Property RealRenderTransform As Transform
        Get
            Return PanFore.RenderTransform
        End Get
        Set(value As Transform)
            PanFore.RenderTransform = value
        End Set
    End Property

    Private IsControlLoaded As Boolean = False
    Private IsMouseDown As Boolean = False

    Public Sub New()
        InitControls()
        PanFore.Padding = Padding
        LabText.Text = If(Text, "")
    End Sub

    Protected Overrides Sub OnAttachedToVisualTree(e As VisualTreeAttachmentEventArgs)
        MyBase.OnAttachedToVisualTree(e)
        IsControlLoaded = True
        RefreshColor()
    End Sub

    Protected Overrides Sub OnDetachedFromVisualTree(e As VisualTreeAttachmentEventArgs)
        MyBase.OnDetachedFromVisualTree(e)
        IsControlLoaded = False
        AniStop("MyButton Color " & Uuid)
        AniStop("MyButton Background " & Uuid)
        AniStop("MyButton Scale " & Uuid)
        '动画被打断时可能停留在按下缩放状态，离开可视树时复位
        IsMouseDown = False
        PanFore.RenderTransform = Nothing
    End Sub

    Protected Overrides Sub OnPropertyChanged(change As AvaloniaPropertyChangedEventArgs)
        MyBase.OnPropertyChanged(change)
        If change.Property Is TextProperty Then
            If LabText IsNot Nothing Then LabText.Text = If(Text, "")
        ElseIf change.Property Is PaddingProperty Then
            If PanFore IsNot Nothing Then PanFore.Padding = Padding
        ElseIf change.Property Is IsEnabledProperty Then
            RefreshColor()
        End If
    End Sub

    '颜色
    Private Const AnimationColorIn As Integer = 100
    Private Const AnimationColorOut As Integer = 200

    Public Sub RefreshColor()
        Try
            If IsControlLoaded AndAlso AniControlEnabled = 0 Then
                If IsEnabled Then
                    AniStart(AaColor(PanFore, Border.BorderBrushProperty, GetBorderResource(), If(IsPointerOver, AnimationColorIn, AnimationColorOut)), "MyButton Color " & Uuid)
                Else
                    AniStart(AaColor(PanFore, Border.BorderBrushProperty, ColorGray4 - New MyColor(PanFore.BorderBrush), AnimationColorOut), "MyButton Color " & Uuid)
                End If
            Else
                AniStop("MyButton Color " & Uuid)
                If IsEnabled Then
                    PanFore.SetResourceReference(Border.BorderBrushProperty, GetBorderResource())
                Else
                    PanFore.BorderBrush = CType(ColorGray4, Brush)
                End If
            End If
        Catch ex As Exception
            Logger.Warn(ex, "刷新按钮颜色出错")
        End Try
    End Sub

    Private Function GetBorderResource() As String
        Select Case ColorType
            Case ColorState.Highlight
                Return If(IsPointerOver, "ColorBrush3", "ColorBrush2")
            Case ColorState.Red
                Return If(IsPointerOver, "ColorBrushRedLight", "ColorBrushRedDark")
            Case Else
                Return If(IsPointerOver, "ColorBrush3", "ColorBrush1")
        End Select
    End Function

    '鼠标事件
    Protected Overrides Sub OnPointerPressed(e As PointerPressedEventArgs)
        MyBase.OnPointerPressed(e)
        If Not e.GetCurrentPoint(Me).Properties.IsLeftButtonPressed Then Return
        If Not IsEnabled Then Return
        IsMouseDown = True
        Focus()
        AniStart({
            AaScaleTransform(PanFore, 0.955 - AniScaleX(PanFore), 80, , New AniEaseOutFluent(AniEasePower.ExtraStrong)),
            AaScaleTransform(PanFore, -0.01, 700, , New AniEaseOutFluent(AniEasePower.Middle))
        }, "MyButton Scale " & Uuid)
        e.Handled = True
    End Sub

    Protected Overrides Sub OnPointerReleased(e As PointerReleasedEventArgs)
        MyBase.OnPointerReleased(e)
        If Not IsMouseDown Then Return
        IsMouseDown = False
        AniStart(AaScaleTransform(PanFore, 1 - AniScaleX(PanFore), 300, 10, New AniEaseOutFluent(AniEasePower.Middle)), "MyButton Scale " & Uuid)
        If Not IsEnabled Then Return
        Logger.Info($"按下按钮：{Text}")
        'Avalonia 默认在事件处理结束后才释放隐式指针捕获；提前释放，避免 Click 中打开的模态弹窗被本按钮持续捕获指针
        e.Pointer.Capture(Nothing)
        RaiseEvent Click(Me, e)
        RaiseCustomEvent()
        e.Handled = True
    End Sub

    Protected Overrides Sub OnPointerEntered(e As PointerEventArgs)
        MyBase.OnPointerEntered(e)
        RefreshColor()
        AniStart(AaColor(PanFore, BackgroundProperty, If(ColorType = ColorState.Red, "ColorBrushRedBack", "ColorBrush7"), AnimationColorIn), "MyButton Background " & Uuid)
    End Sub

    Protected Overrides Sub OnPointerExited(e As PointerEventArgs)
        MyBase.OnPointerExited(e)
        RefreshColor()
        AniStart(AaColor(PanFore, BackgroundProperty, "ColorBrushHalfWhite", AnimationColorOut), "MyButton Background " & Uuid)
        If IsMouseDown Then
            IsMouseDown = False
            AniStart(AaScaleTransform(PanFore, 1 - AniScaleX(PanFore), 800, , New AniEaseOutFluent(AniEasePower.Strong)), "MyButton Scale " & Uuid)
        End If
    End Sub

    Protected Overrides Sub OnPointerCaptureLost(e As PointerCaptureLostEventArgs)
        MyBase.OnPointerCaptureLost(e)
        '指针捕获被系统或弹窗夺走时不会收到 PointerReleased，需在这里恢复按下的缩放
        If Not IsMouseDown Then Return
        IsMouseDown = False
        AniStart(AaScaleTransform(PanFore, 1 - AniScaleX(PanFore), 800, , New AniEaseOutFluent(AniEasePower.Strong)), "MyButton Scale " & Uuid)
    End Sub

End Class
