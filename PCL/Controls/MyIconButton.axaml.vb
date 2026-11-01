Public Class MyIconButton
    Inherits Border

    Public Event Click(sender As Object, e As EventArgs)

    Public Uuid As Integer = GetUuid()

    '图标
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

    Public Enum Themes
        Color
        White
        Black
        Red
        Custom
    End Enum

    Public Shared Shadows ReadOnly ThemeProperty As StyledProperty(Of Themes) =
        AvaloniaProperty.Register(Of MyIconButton, Themes)("Theme", Themes.Color)
    Public Shadows Property Theme As Themes
        Get
            Return GetValue(ThemeProperty)
        End Get
        Set(value As Themes)
            SetValue(ThemeProperty, value)
        End Set
    End Property

    Private _Foreground As Brush = New SolidColorBrush(Color.FromRgb(128, 128, 128))
    Public Property Foreground As Brush
        Get
            Return _Foreground
        End Get
        Set(value As Brush)
            _Foreground = value
            AniControlEnabled += 1
            RefreshAnim()
            AniControlEnabled -= 1
        End Set
    End Property

    Public Property CanClick As Boolean = True

    ''' <summary>
    ''' 鼠标悬停时显示的工具提示。
    ''' </summary>
    Public Property ToolTip As Object
        Get
            Return Global.Avalonia.Controls.ToolTip.GetTip(Me)
        End Get
        Set(value As Object)
            Global.Avalonia.Controls.ToolTip.SetTip(Me, value)
        End Set
    End Property

    Private IsMouseDown As Boolean = False
    Private IsControlLoaded As Boolean = False

    Public Sub New()
        InitControls()
        If Path.Data IsNot Nothing Then Path.RenderTransform = New ScaleTransform(LogoScale, LogoScale)
    End Sub

    Protected Overrides Sub OnAttachedToVisualTree(e As VisualTreeAttachmentEventArgs)
        MyBase.OnAttachedToVisualTree(e)
        IsControlLoaded = True
        RefreshAnim()
    End Sub

    Protected Overrides Sub OnDetachedFromVisualTree(e As VisualTreeAttachmentEventArgs)
        MyBase.OnDetachedFromVisualTree(e)
        IsControlLoaded = False
        AniStop("MyIconButton Color " & Uuid)
        AniStop("MyIconButton Scale " & Uuid)
    End Sub

    Protected Overrides Sub OnPropertyChanged(change As AvaloniaPropertyChangedEventArgs)
        MyBase.OnPropertyChanged(change)
        If change.Property Is ThemeProperty Then
            RefreshAnim()
        ElseIf change.Property Is IsEnabledProperty Then
            RefreshAnim()
        End If
    End Sub

    '鼠标点击
    Protected Overrides Sub OnPointerPressed(e As PointerPressedEventArgs)
        MyBase.OnPointerPressed(e)
        If Not CanClick OrElse Not IsEnabled Then Return
        If Not e.GetCurrentPoint(Me).Properties.IsLeftButtonPressed Then Return
        IsMouseDown = True
        Focus()
        AniStart(AaScaleTransform(PanBack, 0.8 - AniScaleX(PanBack), 400, , New AniEaseOutFluent(AniEasePower.Strong)), "MyIconButton Scale " & Uuid)
        e.Handled = True
    End Sub

    Protected Overrides Sub OnPointerReleased(e As PointerReleasedEventArgs)
        MyBase.OnPointerReleased(e)
        If Not CanClick OrElse Not IsMouseDown Then Return
        Dim WasDown = IsMouseDown
        IsMouseDown = False
        AniStart({
            AaScaleTransform(PanBack, 1.05 - AniScaleX(PanBack), 250, , New AniEaseOutBack(AniEasePower.Weak)),
            AaScaleTransform(PanBack, -0.05, 250, , New AniEaseOutFluent(AniEasePower.Strong))
        }, "MyIconButton Scale " & Uuid)
        If WasDown AndAlso IsEnabled Then
            Logger.Info($"按下图标按钮{If(String.IsNullOrEmpty(Name), "", "：" & Name)}")
            '提前释放隐式指针捕获，避免 Click 中打开的模态弹窗被本控件持续捕获指针
            e.Pointer.Capture(Nothing)
            RaiseEvent Click(Me, EventArgs.Empty)
            RaiseCustomEvent()
            e.Handled = True
        End If
        RefreshAnim()
    End Sub

    Protected Overrides Sub OnPointerEntered(e As PointerEventArgs)
        MyBase.OnPointerEntered(e)
        RefreshAnim()
    End Sub

    Protected Overrides Sub OnPointerExited(e As PointerEventArgs)
        MyBase.OnPointerExited(e)
        If Not CanClick Then Return
        IsMouseDown = False
        AniStart(AaScaleTransform(PanBack, 1 - AniScaleX(PanBack), 250, , New AniEaseOutFluent), "MyIconButton Scale " & Uuid)
        RefreshAnim()
    End Sub

    '颜色
    Private Const AnimationColorIn As Integer = 120
    Private Const AnimationColorOut As Integer = 150

    Public Sub RefreshAnim()
        Try
            If Not IsControlLoaded OrElse AniControlEnabled <> 0 Then
                AniStop("MyIconButton Color " & Uuid)
                If IsEnabled Then
                    Select Case Theme
                        Case Themes.Color : Path.SetResourceReference(Avalonia.Controls.Shapes.Shape.FillProperty, "ColorBrush4")
                        Case Themes.White : Path.SetResourceReference(Avalonia.Controls.Shapes.Shape.FillProperty, "ColorBrushWhite")
                        Case Themes.Red : Path.Fill = CType(New MyColor(160, 255, 76, 76), Brush)
                        Case Themes.Black : Path.Fill = CType(New MyColor(160, 0, 0, 0), Brush)
                        Case Themes.Custom : Path.Fill = CType(New MyColor(160, Foreground), Brush)
                    End Select
                Else
                    Path.SetResourceReference(Avalonia.Controls.Shapes.Shape.FillProperty, "ColorBrushGray5")
                End If
                PanBack.SetResourceReference(Border.BackgroundProperty, "ColorBrushTransparent")
                Return
            End If

            Dim AnimList As New List(Of AniData)
            If Not IsEnabled Then
                AnimList.Add(AaColor(Path, Avalonia.Controls.Shapes.Shape.FillProperty, "ColorBrushGray5", AnimationColorIn))
            ElseIf IsPointerOver Then
                Select Case Theme
                    Case Themes.Color
                        AnimList.Add(AaColor(Path, Avalonia.Controls.Shapes.Shape.FillProperty, "ColorBrush2", AnimationColorIn))
                    Case Themes.White
                        AnimList.Add(AaColor(PanBack, BackgroundProperty, New MyColor(50, 255, 255, 255) - New MyColor(PanBack.Background), AnimationColorIn))
                    Case Themes.Red
                        AnimList.Add(AaColor(Path, Avalonia.Controls.Shapes.Shape.FillProperty, New MyColor(255, 76, 76) - New MyColor(Path.Fill), AnimationColorIn))
                    Case Themes.Black
                        AnimList.Add(AaColor(Path, Avalonia.Controls.Shapes.Shape.FillProperty, New MyColor(230, 0, 0, 0) - New MyColor(Path.Fill), AnimationColorIn))
                    Case Themes.Custom
                        AnimList.Add(AaColor(Path, Avalonia.Controls.Shapes.Shape.FillProperty, New MyColor(255, Foreground) - New MyColor(Path.Fill), AnimationColorIn))
                End Select
            Else
                Select Case Theme
                    Case Themes.Color
                        AnimList.Add(AaColor(Path, Avalonia.Controls.Shapes.Shape.FillProperty, "ColorBrush4", AnimationColorOut))
                        PanBack.SetResourceReference(Border.BackgroundProperty, "ColorBrushTransparent")
                    Case Themes.White
                        AnimList.Add(AaColor(Path, Avalonia.Controls.Shapes.Shape.FillProperty, "ColorBrushWhite", AnimationColorOut))
                        AnimList.Add(AaColor(PanBack, BackgroundProperty, "ColorBrushTransparent", AnimationColorOut))
                    Case Themes.Red
                        AnimList.Add(AaColor(Path, Avalonia.Controls.Shapes.Shape.FillProperty, New MyColor(160, 255, 76, 76) - New MyColor(Path.Fill), AnimationColorOut))
                        PanBack.SetResourceReference(Border.BackgroundProperty, "ColorBrushTransparent")
                    Case Themes.Black
                        AnimList.Add(AaColor(Path, Avalonia.Controls.Shapes.Shape.FillProperty, New MyColor(160, 0, 0, 0) - New MyColor(Path.Fill), AnimationColorOut))
                        PanBack.SetResourceReference(Border.BackgroundProperty, "ColorBrushTransparent")
                    Case Themes.Custom
                        AnimList.Add(AaColor(Path, Avalonia.Controls.Shapes.Shape.FillProperty, New MyColor(160, Foreground) - New MyColor(Path.Fill), AnimationColorOut))
                        PanBack.SetResourceReference(Border.BackgroundProperty, "ColorBrushTransparent")
                End Select
            End If
            AniStart(AnimList, "MyIconButton Color " & Uuid)
        Catch ex As Exception
            Logger.Warn(ex, "刷新图标按钮动画状态出错")
        End Try
    End Sub

End Class

Partial Public Module ModAnimation

    Public Sub AniDispose(Control As MyIconButton, RemoveFromChildren As Boolean, Optional CallBack As ParameterizedThreadStart = Nothing)
        If Not Control.IsHitTestVisible Then Return
        Control.IsHitTestVisible = False
        AniStart({
            AaScaleTransform(Control, -1.5, 200, , New AniEaseInFluent),
            AaCode(
            Sub()
                If RemoveFromChildren Then
                    Dim ParentPanel = TryCast(Control.Parent, Panel)
                    If ParentPanel IsNot Nothing Then ParentPanel.Children.Remove(Control) Else Control.IsVisible = False
                Else
                    Control.IsVisible = False
                End If
                If CallBack IsNot Nothing Then CallBack(Control)
            End Sub, , True)
        }, "MyIconButton Dispose " & Control.Uuid)
    End Sub

End Module
