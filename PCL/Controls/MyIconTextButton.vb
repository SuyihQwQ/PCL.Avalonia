Imports Avalonia.Controls.Shapes
Imports Avalonia.Controls.Documents

Public Class MyIconTextButton
    Inherits Border

    Public Event Click(sender As Object, e As PointerReleasedEventArgs)

    Public Uuid As Integer = GetUuid()

    Private ReadOnly ShapeLogo As New Path With {
        .HorizontalAlignment = HorizontalAlignment.Left,
        .VerticalAlignment = VerticalAlignment.Center,
        .Margin = New Thickness(12, 0, 0, 0),
        .Stretch = Stretch.Uniform,
        .MaxHeight = 16,
        .MaxWidth = 16,
        .RenderTransformOrigin = New RelativePoint(0.5, 0.5, RelativeUnit.Relative)
    }
    Private ReadOnly LabText As New TextBlock With {
        .VerticalAlignment = VerticalAlignment.Center,
        .Margin = New Thickness(7, 0, 12, 1)
    }

    '图标
    Public Property Logo As String
        Get
            Return If(ShapeLogo.Data Is Nothing, "", ShapeLogo.Data.ToString)
        End Get
        Set(value As String)
            If String.IsNullOrEmpty(value) Then
                ShapeLogo.Data = Nothing
            Else
                ShapeLogo.Data = Geometry.Parse(value)
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
            ShapeLogo.RenderTransform = New ScaleTransform(value, value)
        End Set
    End Property

    '文本
    Public ReadOnly Property Inlines As InlineCollection
        Get
            Return LabText.Inlines
        End Get
    End Property

    Public Shared ReadOnly TextProperty As StyledProperty(Of String) =
        AvaloniaProperty.Register(Of MyIconTextButton, String)("Text", "")
    Public Property Text As String
        Get
            Return GetValue(TextProperty)
        End Get
        Set(value As String)
            SetValue(TextProperty, value)
        End Set
    End Property

    '配色
    Public Enum ColorState
        Black = 0
        Highlight = 1
    End Enum
    Private _ColorType As ColorState = ColorState.Black
    Public Property ColorType As ColorState
        Get
            Return _ColorType
        End Get
        Set(value As ColorState)
            If _ColorType = value Then Return
            _ColorType = value
            RefreshColor()
        End Set
    End Property

    Private IsControlLoaded As Boolean = False
    Private IsMouseDown As Boolean = False
    Private ColorName As String

    Public Sub New()
        CornerRadius = New CornerRadius(13.5)
        MinHeight = 27
        HorizontalAlignment = HorizontalAlignment.Center
        VerticalAlignment = VerticalAlignment.Center
        SetResourceReference(BackgroundProperty, "ColorBrushSemiTransparent")
        ShapeLogo.SetResourceReference(Shape.FillProperty, "ColorBrush1")
        LabText.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrush1")
        Dim PanLogo As New StackPanel With {.Orientation = Orientation.Horizontal}
        PanLogo.Children.Add(ShapeLogo)
        PanLogo.Children.Add(LabText)
        Child = PanLogo
        LogoScale = 1
    End Sub

    Protected Overrides Sub OnAttachedToVisualTree(e As VisualTreeAttachmentEventArgs)
        MyBase.OnAttachedToVisualTree(e)
        IsControlLoaded = True
        RefreshColor()
    End Sub

    Protected Overrides Sub OnDetachedFromVisualTree(e As VisualTreeAttachmentEventArgs)
        MyBase.OnDetachedFromVisualTree(e)
        IsControlLoaded = False
        AniStop("MyIconTextButton Color " & Uuid)
        AniStop("MyIconTextButton Foreground " & Uuid)
    End Sub

    Protected Overrides Sub OnPropertyChanged(change As AvaloniaPropertyChangedEventArgs)
        MyBase.OnPropertyChanged(change)
        If change.Property Is TextProperty Then
            If LabText IsNot Nothing Then LabText.Text = If(Text, "")
        ElseIf change.Property Is IsEnabledProperty Then
            RefreshColor()
        End If
    End Sub

    '鼠标事件
    Protected Overrides Sub OnPointerPressed(e As PointerPressedEventArgs)
        MyBase.OnPointerPressed(e)
        If Not e.GetCurrentPoint(Me).Properties.IsLeftButtonPressed Then Return
        If Not IsEnabled Then Return
        IsMouseDown = True
        Focus()
        RefreshColor()
        e.Handled = True
    End Sub

    Protected Overrides Sub OnPointerReleased(e As PointerReleasedEventArgs)
        MyBase.OnPointerReleased(e)
        If Not IsMouseDown Then Return
        IsMouseDown = False
        If IsEnabled Then
            Logger.Info($"按下带图标按钮：{Text}")
            '提前释放隐式指针捕获，避免 Click 中打开的模态弹窗被本控件持续捕获指针
            e.Pointer.Capture(Nothing)
            RaiseEvent Click(Me, e)
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

    '颜色
    Private Const AnimationColorIn As Integer = 100
    Private Const AnimationColorOut As Integer = 150

    Public Sub RefreshColor()
        Try
            Dim ForeName As String
            Dim BackName As String
            If Not IsEnabled Then
                ForeName = "ColorBrushGray5"
                BackName = "ColorBrushSemiTransparent"
            ElseIf IsMouseDown Then
                ForeName = If(ColorType = ColorState.Highlight, "ColorBrush3", "ColorBrush1")
                BackName = "ColorBrush6"
            ElseIf IsPointerOver Then
                ForeName = "ColorBrush3"
                BackName = "ColorBrushBg1"
            ElseIf ColorType = ColorState.Highlight Then
                ForeName = "ColorBrush3"
                BackName = "ColorBrushSemiTransparent"
            Else
                ForeName = "ColorBrush1"
                BackName = "ColorBrushSemiTransparent"
            End If
            If ColorName = ForeName & "|" & BackName Then Return
            ColorName = ForeName & "|" & BackName
            If IsControlLoaded AndAlso AniControlEnabled = 0 Then
                AniStart({
                    AaColor(ShapeLogo, Shape.FillProperty, ForeName, AnimationColorIn),
                    AaColor(LabText, TextBlock.ForegroundProperty, ForeName, AnimationColorIn),
                    AaColor(Me, BackgroundProperty, BackName, If(IsMouseDown, 70, AnimationColorOut))
                }, "MyIconTextButton Foreground " & Uuid)
            Else
                AniStop("MyIconTextButton Foreground " & Uuid)
                ShapeLogo.SetResourceReference(Shape.FillProperty, ForeName)
                LabText.SetResourceReference(TextBlock.ForegroundProperty, ForeName)
                SetResourceReference(BackgroundProperty, BackName)
            End If
        Catch ex As Exception
            Logger.Warn(ex, "刷新带图标按钮颜色出错")
        End Try
    End Sub

End Class
