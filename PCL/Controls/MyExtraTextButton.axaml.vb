Public Class MyExtraTextButton
    Inherits Grid

    Public Event Click(sender As Object, e As EventArgs)

    Public Uuid As Integer = GetUuid()

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

    '显示文本
    '不能标记为 Avalonia 的 Content 属性：该特性会让本控件 XAML 中的视觉子元素
    '被路由到 Inlines 而不是 Children，导致 AvaloniaXamlLoader.Load 直接失败。
    '需要设置文本时请使用 Text，或在代码中访问 Inlines。
    Public ReadOnly Property Inlines As Avalonia.Controls.Documents.InlineCollection
        Get
            Return LabText.Inlines
        End Get
    End Property

    Public Shared ReadOnly TextProperty As StyledProperty(Of String) =
        AvaloniaProperty.Register(Of MyExtraTextButton, String)("Text", "")
    Public Property Text As String
        Get
            Return GetValue(TextProperty)
        End Get
        Set(value As String)
            SetValue(TextProperty, value)
        End Set
    End Property

    '动画
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
                    Opacity = 0
                    AniStart({
                        AaOpacity(Me, 1 - Opacity, 80, 50),
                        AaScaleTransform(Me, 0.15 - AniScaleX(Me), 400, 50, New AniEaseOutBack),
                        AaScaleTransform(Me, 0.85, 160, 50, New AniEaseOutFluent(AniEasePower.Middle))
                    }, "MyExtraTextButton MainScale " & Uuid)
                Else
                    '没了
                    AniStart({
                        AaOpacity(Me, -Opacity, 50, 50),
                        AaScaleTransform(Me, -AniScaleX(Me), 100,, New AniEaseInFluent(AniEasePower.Weak))
                    }, "MyExtraTextButton MainScale " & Uuid)
                End If
                IsHitTestVisible = value '防止缩放动画中依然可以点进去
            End Sub)
        End Set
    End Property

    Private IsControlLoaded As Boolean = False

    Public Sub New()
        InitControls()
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
        AniStop("MyExtraTextButton MainScale " & Uuid)
        AniStop("MyExtraTextButton Scale " & Uuid)
        AniStop("MyExtraTextButton Color " & Uuid)
    End Sub

    Protected Overrides Sub OnPropertyChanged(change As AvaloniaPropertyChangedEventArgs)
        MyBase.OnPropertyChanged(change)
        If change.Property Is TextProperty Then
            If LabText IsNot Nothing Then LabText.Text = If(Text, "")
        ElseIf change.Property Is IsEnabledProperty Then
            RefreshColor()
        End If
    End Sub

    '鼠标点击判定
    Private IsLeftMouseHeld As Boolean = False

    Protected Overrides Sub OnPointerPressed(e As PointerPressedEventArgs)
        MyBase.OnPointerPressed(e)
        If Not IsEnabled Then Return
        If Not e.GetCurrentPoint(Me).Properties.IsLeftButtonPressed Then Return
        If Not IsLeftMouseHeld Then
            AniStart({
                AaScaleTransform(PanScale, 0.85 - AniScaleX(PanScale), 800,, New AniEaseOutFluent(AniEasePower.Strong)),
                AaScaleTransform(PanScale, -0.05, 60,, New AniEaseOutFluent)
            }, "MyExtraTextButton Scale " & Uuid)
        End If
        IsLeftMouseHeld = True
        Focus()
        e.Handled = True
    End Sub

    Protected Overrides Sub OnPointerReleased(e As PointerReleasedEventArgs)
        MyBase.OnPointerReleased(e)
        '提前释放隐式指针捕获，避免 Click 中打开的模态弹窗被本控件持续捕获指针
        e.Pointer.Capture(Nothing)
        If e.InitialPressMouseButton = MouseButton.Left AndAlso IsLeftMouseHeld Then
            AniStart(AaScaleTransform(PanScale, 1 - AniScaleX(PanScale), 300,, New AniEaseOutBack), "MyExtraTextButton Scale " & Uuid)
            IsLeftMouseHeld = False
            Logger.Info($"按下附加图标按钮：{Text}")
            RaiseEvent Click(Me, EventArgs.Empty)
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
        AniStart(AaScaleTransform(PanScale, 1 - AniScaleX(PanScale), 500,, New AniEaseOutFluent), "MyExtraTextButton Scale " & Uuid)
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
                    AniStart(AaColor(PanColor, Border.BackgroundProperty, "ColorBrushGray4", AnimationColorIn), "MyExtraTextButton Color " & Uuid)
                ElseIf IsPointerOver Then
                    AniStart(AaColor(PanColor, Border.BackgroundProperty, "ColorBrush4", AnimationColorIn), "MyExtraTextButton Color " & Uuid)
                Else
                    AniStart(AaColor(PanColor, Border.BackgroundProperty, "ColorBrush3", AnimationColorOut), "MyExtraTextButton Color " & Uuid)
                End If
            Else
                AniStop("MyExtraTextButton Color " & Uuid)
                PanColor.SetResourceReference(Border.BackgroundProperty,
                    If(Not IsEnabled, "ColorBrushGray4", If(IsPointerOver, "ColorBrush4", "ColorBrush3")))
            End If
        Catch ex As Exception
            Logger.Warn(ex, "刷新附加图标按钮颜色出错")
        End Try
    End Sub

End Class
