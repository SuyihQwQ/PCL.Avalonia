Public Class MyExtraButton
    Inherits Grid

    Public Event Click(sender As Object, e As EventArgs)
    Public Event RightClick(sender As Object, e As EventArgs)

    Public Uuid As Integer = GetUuid()

    '进度条
    Private ReadOnly ProgressRect As New RectangleGeometry(New Rect(0, 0, 0, 0))
    Private _Progress As Double = 0
    Public Property Progress As Double
        Get
            Return _Progress
        End Get
        Set(value As Double)
            If _Progress = value Then Return
            _Progress = value
            If value < 0.0001 Then
                PanProgress.IsVisible = False
            Else
                PanProgress.IsVisible = True
                ProgressRect.Rect = New Rect(0, 40 * (1 - value), 40, 40 * value)
            End If
        End Set
    End Property

    '自定义属性
    Public Property Logo As String
        Get
            Return If(Path.Data Is Nothing, "", Path.Data.ToString)
        End Get
        Set(value As String)
            If String.IsNullOrEmpty(value) Then
                Path.Data = Nothing
            Else
                Path.Data = Geometry.Parse(value)
            End If
        End Set
    End Property

    Private _LogoScale As Double = 1
    Public Property LogoScale As Double
        Get
            Return _LogoScale
        End Get
        Set(value As Double)
            _LogoScale = value
            If Path IsNot Nothing Then Path.RenderTransform = New ScaleTransform(value, value)
        End Set
    End Property

    Private _Show As Boolean = False
    Public Property Show As Boolean
        Get
            Return _Show
        End Get
        Set(value As Boolean)
            If _Show = value Then Return
            _Show = value
            RunInUi(
            Sub()
                If value Then
                    '有了
                    IsVisible = True
                    AniStart({
                        AaScaleTransform(Me, 0.3 - AniScaleX(Me), 500, 60, New AniEaseOutFluent(AniEasePower.Weak)),
                        AaScaleTransform(Me, 0.7, 500, 60, New AniEaseOutBack(AniEasePower.Weak)),
                        AaHeight(Me, 50 - Height, 200,, New AniEaseOutFluent(AniEasePower.Weak))
                    }, "MyExtraButton MainScale " & Uuid)
                Else
                    '没了
                    AniStart({
                        AaScaleTransform(Me, -AniScaleX(Me), 100,, New AniEaseInFluent(AniEasePower.Weak)),
                        AaHeight(Me, -Height, 400, 100, New AniEaseOutFluent()),
                        AaCode(
                        Sub()
                            IsVisible = False
                            AniSetScale(Me, 0)
                        End Sub,, True)
                    }, "MyExtraButton MainScale " & Uuid)
                End If
                IsHitTestVisible = value '防止缩放动画中依然可以点进去
            End Sub)
        End Set
    End Property

    Public Delegate Function ShowCheckDelegate() As Boolean
    Public ShowCheck As ShowCheckDelegate = Nothing
    Public Sub ShowRefresh()
        If ShowCheck IsNot Nothing Then Show = ShowCheck()
    End Sub

    Public Property CanRightClick As Boolean = False

    Private IsControlLoaded As Boolean = False

    Private ReadOnly Property ToolTipText As String
        Get
            Return TryCast(ToolTip.GetTip(Me), String)
        End Get
    End Property

    Public Sub New()
        InitControls()
        PanProgress.Clip = ProgressRect
        If Path.Data IsNot Nothing Then Path.RenderTransform = New ScaleTransform(LogoScale, LogoScale)
    End Sub

    Protected Overrides Sub OnAttachedToVisualTree(e As VisualTreeAttachmentEventArgs)
        MyBase.OnAttachedToVisualTree(e)
        IsControlLoaded = True
        RefreshColor()
    End Sub

    Protected Overrides Sub OnDetachedFromVisualTree(e As VisualTreeAttachmentEventArgs)
        MyBase.OnDetachedFromVisualTree(e)
        IsControlLoaded = False
        AniStop("MyExtraButton MainScale " & Uuid)
        AniStop("MyExtraButton Scale " & Uuid)
        AniStop("MyExtraButton Color " & Uuid)
    End Sub

    Protected Overrides Sub OnPropertyChanged(change As AvaloniaPropertyChangedEventArgs)
        MyBase.OnPropertyChanged(change)
        If change.Property Is IsEnabledProperty Then RefreshColor()
    End Sub

    '鼠标点击判定
    Private IsLeftMouseHeld As Boolean = False
    Private IsRightMouseHeld As Boolean = False

    Protected Overrides Sub OnPointerPressed(e As PointerPressedEventArgs)
        MyBase.OnPointerPressed(e)
        If Not IsEnabled Then Return
        Dim Properties = e.GetCurrentPoint(Me).Properties
        If Properties.IsLeftButtonPressed Then
            If Not IsLeftMouseHeld AndAlso Not IsRightMouseHeld Then
                AniStart({
                    AaScaleTransform(PanScale, 0.85 - AniScaleX(PanScale), 800,, New AniEaseOutFluent(AniEasePower.Strong)),
                    AaScaleTransform(PanScale, -0.05, 60,, New AniEaseOutFluent)
                }, "MyExtraButton Scale " & Uuid)
            End If
            IsLeftMouseHeld = True
            Focus()
            e.Handled = True
        ElseIf Properties.IsRightButtonPressed AndAlso CanRightClick Then
            If Not IsLeftMouseHeld AndAlso Not IsRightMouseHeld Then
                AniStart({
                    AaScaleTransform(PanScale, 0.85 - AniScaleX(PanScale), 800,, New AniEaseOutFluent(AniEasePower.Strong)),
                    AaScaleTransform(PanScale, -0.05, 60,, New AniEaseOutFluent)
                }, "MyExtraButton Scale " & Uuid)
            End If
            IsRightMouseHeld = True
            Focus()
            e.Handled = True
        End If
    End Sub

    Protected Overrides Sub OnPointerReleased(e As PointerReleasedEventArgs)
        MyBase.OnPointerReleased(e)
        If e.InitialPressMouseButton = MouseButton.Left AndAlso IsLeftMouseHeld Then
            If Not IsRightMouseHeld Then
                AniStart(AaScaleTransform(PanScale, 1 - AniScaleX(PanScale), 300,, New AniEaseOutBack), "MyExtraButton Scale " & Uuid)
            End If
            Logger.Info($"按下附加按钮{If(String.IsNullOrEmpty(ToolTipText), "", "：" & ToolTipText)}")
            IsLeftMouseHeld = False
            RaiseEvent Click(Me, EventArgs.Empty)
            RaiseCustomEvent()
            RefreshColor()
            e.Handled = True
        ElseIf e.InitialPressMouseButton = MouseButton.Right AndAlso IsRightMouseHeld Then
            If Not IsLeftMouseHeld Then
                AniStart(AaScaleTransform(PanScale, 1 - AniScaleX(PanScale), 300,, New AniEaseOutBack), "MyExtraButton Scale " & Uuid)
            End If
            Logger.Info($"右键按下附加按钮{If(String.IsNullOrEmpty(ToolTipText), "", "：" & ToolTipText)}")
            IsRightMouseHeld = False
            RaiseEvent RightClick(Me, EventArgs.Empty)
            RaiseCustomEvent()
            RefreshColor()
            e.Handled = True
        End If
    End Sub

    Protected Overrides Sub OnPointerEntered(e As PointerEventArgs)
        MyBase.OnPointerEntered(e)
        RefreshColor()
    End Sub

    Protected Overrides Sub OnPointerExited(e As PointerEventArgs)
        MyBase.OnPointerExited(e)
        IsLeftMouseHeld = False
        IsRightMouseHeld = False
        AniStart(AaScaleTransform(PanScale, 1 - AniScaleX(PanScale), 500,, New AniEaseOutFluent), "MyExtraButton Scale " & Uuid)
        RefreshColor()
    End Sub

    '颜色
    Private Const AnimationColorIn As Integer = 120
    Private Const AnimationColorOut As Integer = 150

    Public Sub RefreshColor()
        Try
            If IsControlLoaded AndAlso AniControlEnabled = 0 Then
                '防止默认属性变更触发动画
                If Not IsEnabled Then
                    AniStart(AaColor(PanColor, Border.BackgroundProperty, "ColorBrushGray4", AnimationColorIn), "MyExtraButton Color " & Uuid)
                ElseIf IsPointerOver Then
                    AniStart(AaColor(PanColor, Border.BackgroundProperty, "ColorBrush4", AnimationColorIn), "MyExtraButton Color " & Uuid)
                Else
                    AniStart(AaColor(PanColor, Border.BackgroundProperty, "ColorBrush3", AnimationColorOut), "MyExtraButton Color " & Uuid)
                End If
            Else
                AniStop("MyExtraButton Color " & Uuid)
                PanColor.SetResourceReference(Border.BackgroundProperty,
                    If(Not IsEnabled, "ColorBrushGray4", If(IsPointerOver, "ColorBrush4", "ColorBrush3")))
            End If
        Catch ex As Exception
            Logger.Warn(ex, "刷新图标按钮颜色出错")
        End Try
    End Sub

    ''' <summary>
    ''' 发出一圈波浪效果提示。
    ''' </summary>
    Public Sub Ribble()
        RunInUi(
        Sub()
            Dim Shape As New Border With {
                .CornerRadius = New CornerRadius(1000),
                .BorderThickness = New Thickness(0.001),
                .Opacity = 0.5,
                .RenderTransformOrigin = New RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                .RenderTransform = New ScaleTransform()}
            Shape.SetResourceReference(Border.BackgroundProperty, "ColorBrush5")
            PanScale.Children.Insert(0, Shape)
            AniStart({
                AaScaleTransform(Shape, 13, 1000, Ease:=New AniEaseInoutFluent(AniEasePower.Strong, 0.3)),
                AaOpacity(Shape, -Shape.Opacity, 1000),
                AaCode(
                Sub()
                    PanScale.Children.Remove(Shape)
                End Sub, After:=True)
            }, "ExtraButton Ribble " & GetUuid())
        End Sub)
    End Sub

End Class
