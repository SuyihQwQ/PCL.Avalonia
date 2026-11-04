Imports Avalonia.Controls.Documents
Imports Path = Avalonia.Controls.Shapes.Path
Imports Avalonia.Interactivity

Public Class MyListItem
    Inherits Grid
    Implements IMyRadio

    Public Event Click(sender As Object, e As PointerReleasedEventArgs)
    Public Event LogoClick(sender As Object, e As PointerReleasedEventArgs)
    Public Event Check(sender As Object, e As RouteEventArgs) Implements IMyRadio.Check
    Public Event Changed(sender As Object, e As RouteEventArgs) Implements IMyRadio.Changed

#Region "后加载控件"

    '指向背景
    Private _RectBack As Border = Nothing
    Public ReadOnly Property RectBack As Border
        Get
            If _RectBack Is Nothing Then
                Dim Rect As New Border With {
                    .CornerRadius = New CornerRadius(If(IsScaleAnimationEnabled OrElse Height > 40, 6, 0)),
                    .RenderTransform = If(IsScaleAnimationEnabled, New ScaleTransform(0.8, 0.8), New ScaleTransform(1, 1)),
                    .RenderTransformOrigin = New RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                    .BorderThickness = New Thickness(GetWPFSize(1)),
                    .IsHitTestVisible = False,
                    .Opacity = 0}
                Rect.SetResourceReference(Border.BackgroundProperty, "ColorBrush7")
                Rect.SetResourceReference(Border.BorderBrushProperty, "ColorBrush6")
                Grid.SetColumnSpan(Rect, 999)
                Grid.SetRowSpan(Rect, 999)
                Children.Insert(0, Rect)
                _RectBack = Rect
            End If
            Return _RectBack
        End Get
    End Property

    '按钮
    Public ButtonStack As Control

    '图标
    Public PathLogo As Control

    '勾选条
    Public RectCheck As Border

    '副文本
    Private _LabInfo As TextBlock = Nothing
    Public ReadOnly Property LabInfo As TextBlock
        Get
            If _LabInfo Is Nothing Then
                Dim Lab As New TextBlock With {
                    .HorizontalAlignment = HorizontalAlignment.Left,
                    .IsHitTestVisible = False,
                    .TextTrimming = TextTrimming.CharacterEllipsis,
                    .IsVisible = False,
                    .FontSize = 12,
                    .Margin = New Thickness(4, 0, 0, 0),
                    .Opacity = 0.6}
                Grid.SetColumn(Lab, 3)
                Grid.SetRow(Lab, 2)
                Children.Add(Lab)
                _LabInfo = Lab
            End If
            Return _LabInfo
        End Get
    End Property

#End Region

#Region "自定义属性"

    Public Uuid As Integer = GetUuid()

    ''' <summary>
    ''' 是否启用缩放动画。
    ''' </summary>
    Public Property IsScaleAnimationEnabled As Boolean
        Get
            Return _IsScaleAnimationEnabled
        End Get
        Set(value As Boolean)
            _IsScaleAnimationEnabled = value
            If _RectBack IsNot Nothing Then RectBack.CornerRadius = New CornerRadius(If(value, 6, 0))
        End Set
    End Property
    Private _IsScaleAnimationEnabled As Boolean = True

    '边距
    Public Property PaddingLeft As Integer
        Get
            Return CInt(ColumnDefinitions(1).Width.Value)
        End Get
        Set(value As Integer)
            ColumnDefinitions(1).Width = New GridLength(value)
        End Set
    End Property
    ''' <summary>
    ''' 右边距的最小值。
    ''' 在存在右侧按钮时，右边距会被自动设置为 5 + 按钮数 * 25。
    ''' </summary>
    Public Property MinPaddingRight As Integer = 4

    '按钮
    Private _Buttons As IEnumerable(Of MyIconButton)
    Public Property Buttons As IEnumerable(Of MyIconButton)
        Get
            Return _Buttons
        End Get
        Set(value As IEnumerable(Of MyIconButton))
            _Buttons = value
            If ButtonStack IsNot Nothing Then
                Children.Remove(ButtonStack)
                ButtonStack = Nothing
            End If
            Dim BtnList = If(value, New List(Of MyIconButton)).ToList()
            Select Case BtnList.Count
                Case 0
                Case 1
                    For Each Btn In BtnList
                        If Double.IsNaN(Btn.Height) Then Btn.Height = 25
                        If Double.IsNaN(Btn.Width) Then Btn.Width = 25
                        Btn.Opacity = 0
                        Btn.Margin = New Thickness(0, 0, 5, 0)
                        Btn.HorizontalAlignment = HorizontalAlignment.Right
                        Btn.VerticalAlignment = VerticalAlignment.Center
                        Grid.SetColumnSpan(Btn, 10)
                        Grid.SetRowSpan(Btn, 10)
                        Children.Add(Btn)
                        ButtonStack = Btn
                    Next
                Case Else
                    Dim Stack As New StackPanel With {
                        .Opacity = 0,
                        .Margin = New Thickness(0, 0, 5, 0),
                        .Orientation = Orientation.Horizontal,
                        .HorizontalAlignment = HorizontalAlignment.Right,
                        .VerticalAlignment = VerticalAlignment.Center}
                    Grid.SetColumnSpan(Stack, 10)
                    Grid.SetRowSpan(Stack, 10)
                    For Each Btn In BtnList
                        If Double.IsNaN(Btn.Height) Then Btn.Height = 25
                        If Double.IsNaN(Btn.Width) Then Btn.Width = 25
                        Stack.Children.Add(Btn)
                    Next
                    Children.Add(Stack)
                    ButtonStack = Stack
            End Select
        End Set
    End Property

    '标题
    Private _Inlines As InlineCollection
    Public ReadOnly Property Inlines As InlineCollection
        Get
            If LabTitle IsNot Nothing Then Return LabTitle.Inlines
            If _Inlines Is Nothing Then _Inlines = New InlineCollection
            Return _Inlines
        End Get
    End Property

    Public Shared ReadOnly TitleProperty As StyledProperty(Of String) =
        AvaloniaProperty.Register(Of MyListItem, String)("Title", "")
    Public Property Title As String
        Get
            Return GetValue(TitleProperty)
        End Get
        Set(value As String)
            SetValue(TitleProperty, If(value, "").ReplaceLineEndings(""))
        End Set
    End Property

    '字号
    Public Shared ReadOnly FontSizeProperty As StyledProperty(Of Double) =
        AvaloniaProperty.Register(Of MyListItem, Double)("FontSize", 14.0)
    Public Property FontSize As Double
        Get
            Return GetValue(FontSizeProperty)
        End Get
        Set(value As Double)
            SetValue(FontSizeProperty, value)
        End Set
    End Property

    '信息
    Public Shared ReadOnly InfoProperty As StyledProperty(Of String) =
        AvaloniaProperty.Register(Of MyListItem, String)(NameOf(Info), "")
    Public Property Info As String
        Get
            Return GetValue(InfoProperty)
        End Get
        Set(value As String)
            SetValue(InfoProperty, If(value, "").ReplaceLineEndings(""))
        End Set
    End Property

    '图片
    Private _Logo As String = ""
    Public Property Logo As String
        Get
            Return _Logo
        End Get
        Set(value As String)
            If _Logo = value Then Return
            _Logo = value
            If PathLogo IsNot Nothing Then Children.Remove(PathLogo)
            If Not String.IsNullOrEmpty(_Logo) Then
                If _Logo.StartsWithF("http", True) Then
                    PathLogo = New MyImage With {
                        .Tag = Me,
                        .IsHitTestVisible = LogoClickable,
                        .Source = _Logo,
                        .RenderTransformOrigin = New RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                        .RenderTransform = New ScaleTransform With {.ScaleX = LogoScale, .ScaleY = LogoScale}}
                ElseIf _Logo.EndsWithF(".png", True) OrElse _Logo.EndsWithF(".jpg", True) OrElse _Logo.EndsWithF(".webp", True) Then
                    PathLogo = New Border With {
                        .Tag = Me,
                        .IsHitTestVisible = LogoClickable,
                        .Background = CType(New MyBitmap(_Logo), ImageBrush),
                        .RenderTransformOrigin = New RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                        .RenderTransform = New ScaleTransform With {.ScaleX = LogoScale, .ScaleY = LogoScale},
                        .HorizontalAlignment = HorizontalAlignment.Stretch,
                        .VerticalAlignment = VerticalAlignment.Stretch}
                Else
                    Dim LogoPath As New Path With {
                        .Tag = Me,
                        .IsHitTestVisible = LogoClickable,
                        .HorizontalAlignment = HorizontalAlignment.Center,
                        .VerticalAlignment = VerticalAlignment.Center,
                        .Stretch = Stretch.Uniform,
                        .Data = Geometry.Parse(_Logo),
                        .RenderTransformOrigin = New RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                        .RenderTransform = New ScaleTransform With {.ScaleX = LogoScale, .ScaleY = LogoScale}}
                    LogoPath.Fill = Foreground
                    If LogoPath.Fill Is Nothing Then LogoPath.SetResourceReference(Path.FillProperty, "ColorBrush1")
                    PathLogo = LogoPath
                End If
                Grid.SetColumn(PathLogo, 2)
                Grid.SetRowSpan(PathLogo, 4)
                RefreshSize()
                Children.Add(PathLogo)
                If LogoClickable Then
                    AddHandler PathLogo.PointerExited, Sub(sender, e) IsLogoDown = False
                    AddHandler PathLogo.PointerPressed, Sub(sender, e) IsLogoDown = True
                    AddHandler PathLogo.PointerReleased,
                        Sub(sender, e)
                            If IsLogoDown Then
                                IsLogoDown = False
                                RaiseEvent LogoClick(Me, e)
                            End If
                        End Sub
                End If
            End If
            ColumnDefinitions(2).Width = New GridLength(If(_Logo = "", 0, 34) + If(Height < 40, 0, 4))
        End Set
    End Property
    Private _LogoScale As Double = 1
    Public Property LogoScale As Double
        Get
            Return _LogoScale
        End Get
        Set(value As Double)
            _LogoScale = value
            If PathLogo IsNot Nothing Then PathLogo.RenderTransform = New ScaleTransform With {.ScaleX = LogoScale, .ScaleY = LogoScale}
        End Set
    End Property

    '图标的点击
    ''' <summary>
    ''' 该 Logo 是否可用点击触发事件。需要在 Logo 属性之前设置。
    ''' </summary>
    Public Property LogoClickable As Boolean = False
    Private IsLogoDown As Boolean = False

    '勾选选项
    Public Enum CheckType
        None
        Clickable
        RadioBox
        CheckBox
    End Enum
    Private _Type As CheckType = CheckType.None
    Public Property Type As CheckType
        Get
            Return _Type
        End Get
        Set(value As CheckType)
            If _Type = value Then Return
            _Type = value
            ColumnDefinitions(0).Width = New GridLength(If(_Type = CheckType.None OrElse _Type = CheckType.Clickable, If(Height < 40, 4, 2), 6))
            If _Type = CheckType.None OrElse _Type = CheckType.Clickable Then
                If RectCheck IsNot Nothing Then
                    Children.Remove(RectCheck)
                    RectCheck = Nothing
                End If
                SetChecked(False, False, False)
            Else
                If RectCheck Is Nothing Then
                    RectCheck = New Border With {
                        .Width = 5,
                        .Height = If(Checked, Double.NaN, 0),
                        .CornerRadius = New CornerRadius(2),
                        .VerticalAlignment = If(Checked, VerticalAlignment.Stretch, VerticalAlignment.Center),
                        .HorizontalAlignment = HorizontalAlignment.Left,
                        .Margin = If(Checked, New Thickness(-1, 6, 0, 6), New Thickness(-1, 0, 0, 0))}
                    RectCheck.SetResourceReference(Border.BackgroundProperty, "ColorBrush3")
                    Grid.SetRowSpan(RectCheck, 4)
                    Children.Add(RectCheck)
                End If
            End If
        End Set
    End Property

    ''' <summary>
    ''' 标题文本的垂直偏移，用于校正拉丁字母与中文字形的基线差异。
    ''' </summary>
    Public Property TitleOffset As Double = 0

    '适应尺寸
    Private Sub RefreshSize()
        ColumnDefinitions(0).Width = New GridLength(If(_Type = CheckType.None OrElse _Type = CheckType.Clickable, If(Height < 40, 4, 2), 6))
        ColumnDefinitions(2).Width = New GridLength(If(_Logo = "", 0, 34) + If(Height < 40, 0, 4))
        If PathLogo IsNot Nothing Then
            If _Logo.EndsWithF(".png", True) OrElse _Logo.EndsWithF(".jpg", True) OrElse _Logo.EndsWithF(".webp", True) Then
                PathLogo.Margin = New Thickness(4, 5, 3, 5)
            Else
                PathLogo.Margin = New Thickness(If(Height < 40, 6, 8), 8, If(Height < 40, 4, 6), 8)
            End If
        End If
        LabTitle.Margin = New Thickness(4, TitleOffset, 0, If(Height < 40, 0, 2))
    End Sub

    '勾选状态
    Private _Checked As Boolean = False
    Public Property Checked As Boolean
        Get
            Return _Checked
        End Get
        Set(value As Boolean)
            SetChecked(value, False, value <> _Checked)
        End Set
    End Property

    ''' <summary>
    ''' 手动设置 Checked 属性。
    ''' </summary>
    Public Sub SetChecked(value As Boolean, user As Boolean, anime As Boolean)
        Try
            Dim ChangedEventArgs As New RouteEventArgs(user)
            Dim RawValue = _Checked
            If Type = CheckType.RadioBox Then
                If IsInitialized AndAlso Not value = _Checked Then
                    _Checked = value
                    RaiseEvent Changed(Me, ChangedEventArgs)
                    If ChangedEventArgs.Handled Then
                        _Checked = RawValue
                        Return
                    End If
                End If
                _Checked = value
            Else
                If value = _Checked Then Return
                _Checked = value
                If IsInitialized Then
                    RaiseEvent Changed(Me, ChangedEventArgs)
                    If ChangedEventArgs.Handled Then
                        _Checked = RawValue
                        Return
                    End If
                End If
            End If
            If value Then
                Dim CheckEventArgs As New RouteEventArgs(user)
                RaiseEvent Check(Me, CheckEventArgs)
                If CheckEventArgs.Handled Then Return
            End If

            '保证只有一个单选 ListItem 选中
            If Type = CheckType.RadioBox Then
                Dim ParentPanel = TryCast(Parent, Panel)
                If ParentPanel Is Nothing Then Return
                Dim RadioboxList = ParentPanel.Children.OfType(Of MyListItem).
                    Where(Function(c) c.Type = CheckType.RadioBox).ToList()
                If RadioboxList.Count = 0 Then Return
                Dim CheckedCount = RadioboxList.Where(Function(c) c.Checked).Count
                Select Case CheckedCount
                    Case 0
                        RadioboxList(0).Checked = True
                    Case Is > 1
                        If Me.Checked Then
                            For Each Control In RadioboxList
                                If Control.Checked AndAlso Not Control.Equals(Me) Then Control.Checked = False
                            Next
                        Else
                            Dim FirstChecked = False
                            For Each Control In RadioboxList
                                If Control.Checked Then
                                    If FirstChecked Then
                                        Control.Checked = False
                                    Else
                                        FirstChecked = True
                                    End If
                                End If
                            Next
                        End If
                End Select
            End If

            '更改动画
            '控件刚加入列表、尚未完成首次布局时 Bounds 为 0，此时按实际高度计算会得到负增量，直接把勾选条压成 0 高
            If IsControlLoaded AndAlso AniControlEnabled = 0 AndAlso anime AndAlso Bounds.Height > 0 Then
                Dim Anim As New List(Of AniData)
                If Checked Then
                    If RectCheck IsNot Nothing Then
                        Dim Delta = Bounds.Height - RectCheck.Bounds.Height - 12
                        Anim.Add(AaHeight(RectCheck, Delta * 0.4, 200, , New AniEaseOutFluent(AniEasePower.Weak)))
                        Anim.Add(AaHeight(RectCheck, Delta * 0.6, 300, , New AniEaseOutBack(AniEasePower.Weak)))
                        Anim.Add(AaOpacity(RectCheck, 1 - RectCheck.Opacity, 30))
                        RectCheck.VerticalAlignment = VerticalAlignment.Center
                        RectCheck.Margin = New Thickness(-1, 0, 0, 0)
                    End If
                    Anim.Add(AaColor(Me, ForegroundProperty, If(Height < 40, "ColorBrush3", "ColorBrush2"), 200))
                Else
                    If RectCheck IsNot Nothing Then
                        Anim.Add(AaHeight(RectCheck, -RectCheck.Bounds.Height, 120, , New AniEaseInFluent(AniEasePower.Weak)))
                        Anim.Add(AaOpacity(RectCheck, -RectCheck.Opacity, 70, 40))
                        RectCheck.VerticalAlignment = VerticalAlignment.Center
                    End If
                    Anim.Add(AaColor(Me, ForegroundProperty, "ColorBrush1", 120))
                End If
                AniStart(Anim, "MyListItem Checked " & Uuid)
            Else
                AniStop("MyListItem Checked " & Uuid)
                If Checked Then
                    If RectCheck IsNot Nothing Then
                        RectCheck.Height = Double.NaN
                        RectCheck.Margin = New Thickness(-1, 6, 0, 6)
                        RectCheck.Opacity = 1
                        RectCheck.VerticalAlignment = VerticalAlignment.Stretch
                    End If
                    SetResourceReference(ForegroundProperty, If(Height < 40, "ColorBrush3", "ColorBrush2"))
                Else
                    If RectCheck IsNot Nothing Then
                        RectCheck.Height = 0
                        RectCheck.Margin = New Thickness(-1, 0, 0, 0)
                        RectCheck.Opacity = 0
                        RectCheck.VerticalAlignment = VerticalAlignment.Center
                    End If
                    SetResourceReference(ForegroundProperty, "ColorBrush1")
                End If
            End If
        Catch ex As Exception
            Logger.Warn(ex, "设置 Checked 失败")
        End Try
    End Sub

    '前景色
    Public Shared ReadOnly ForegroundProperty As StyledProperty(Of IBrush) =
        AvaloniaProperty.Register(Of MyListItem, IBrush)("Foreground", Nothing)
    Public Property Foreground As IBrush
        Get
            Return GetValue(ForegroundProperty)
        End Get
        Set(value As IBrush)
            SetValue(ForegroundProperty, value)
        End Set
    End Property

    '菜单与按钮绑定
    Public ContentHandler As Action(Of MyListItem, EventArgs)

#End Region

#Region "初始化"

    Private IsControlLoaded As Boolean = False

    Public Sub New()
        InitControls()
        LabTitle.Text = Title
        LabTitle.FontSize = FontSize
        MyBase.AddHandler(InputElement.PointerPressedEvent, AddressOf Button_MouseDown, RoutingStrategies.Tunnel)
        MyBase.AddHandler(InputElement.PointerReleasedEvent, AddressOf Button_MouseUp, RoutingStrategies.Tunnel)
        MyBase.AddHandler(Me.PointerEnteredEvent, AddressOf RefreshColor)
        MyBase.AddHandler(Me.PointerExitedEvent, AddressOf Button_MouseLeave)
        MyBase.AddHandler(Me.SizeChangedEvent, AddressOf SizeChangedHandler)
    End Sub

    Protected Overrides Sub OnAttachedToVisualTree(e As VisualTreeAttachmentEventArgs)
        MyBase.OnAttachedToVisualTree(e)
        IsControlLoaded = True
        If Checked Then
            SetResourceReference(ForegroundProperty, If(Height < 40, "ColorBrush3", "ColorBrush2"))
        Else
            SetResourceReference(ForegroundProperty, "ColorBrush1")
        End If
        ColumnDefinitions(4).Width = New GridLength(MinPaddingRight)
        RefreshSize()
        RefreshColor(Me, EventArgs.Empty)
    End Sub

    Protected Overrides Sub OnPropertyChanged(change As AvaloniaPropertyChangedEventArgs)
        MyBase.OnPropertyChanged(change)
        If change.Property Is TitleProperty AndAlso LabTitle IsNot Nothing Then
            LabTitle.Text = Title
        ElseIf change.Property Is FontSizeProperty AndAlso LabTitle IsNot Nothing Then
            LabTitle.FontSize = FontSize
        ElseIf change.Property Is ForegroundProperty AndAlso LabTitle IsNot Nothing Then
            LabTitle.Foreground = Foreground
            If TypeOf PathLogo Is Path Then
                DirectCast(PathLogo, Path).Fill = Foreground
            End If
        ElseIf change.Property Is InfoProperty AndAlso LabInfo IsNot Nothing Then
            Dim InfoText As String = If(Info, "").ReplaceLineEndings("")
            If InfoText <> Info Then SetCurrentValue(InfoProperty, InfoText)
            LabInfo.Text = InfoText
            LabInfo.IsVisible = InfoText <> ""
        End If
    End Sub

    Private Sub SizeChangedHandler(sender As Object, e As SizeChangedEventArgs)
        RefreshSize()
    End Sub

#End Region

#Region "点击"

    Private Sub Button_MouseUp(sender As Object, e As PointerReleasedEventArgs)
        If Not IsMouseDown Then Return
        IsMouseDown = False
        If ButtonStack IsNot Nothing Then ButtonStack.IsHitTestVisible = True
        'Avalonia 默认在事件处理结束后才释放隐式指针捕获；提前释放，避免 Click 中打开的模态弹窗被本控件持续捕获指针
        e.Pointer.Capture(Nothing)
        RaiseEvent Click(sender, e)
        If e.Handled Then Return
        If CustomEventService.GetEventType(Me) <> CustomEvent.EventType.None OrElse
           CustomEventService.GetEvents(Me).Events.Count > 0 Then
            RaiseCustomEvent()
            e.Handled = True
        End If
        If e.Handled Then Return
        Select Case Type
            Case CheckType.Clickable
                Logger.Info($"按下单击列表项：{Title}")
            Case CheckType.RadioBox
                Logger.Info($"按下单选列表项：{Title}")
                If Not Checked Then SetChecked(True, True, True)
            Case CheckType.CheckBox
                Logger.Info($"按下复选列表项（{Not Checked}）：{Title}")
                SetChecked(Not Checked, True, True)
        End Select
    End Sub

    Private IsMouseDown As Boolean = False
    Private Sub Button_MouseDown(sender As Object, e As PointerPressedEventArgs)
        If Not e.GetCurrentPoint(Me).Properties.IsLeftButtonPressed Then Return
        If e.Source Is Me AndAlso Type <> CheckType.None Then
            IsMouseDown = True
            If ButtonStack IsNot Nothing Then ButtonStack.IsHitTestVisible = False
        End If
    End Sub

    Private Sub Button_MouseLeave(sender As Object, e As PointerEventArgs)
        IsMouseDown = False
        If ButtonStack IsNot Nothing Then ButtonStack.IsHitTestVisible = True
        RefreshColor(sender, e)
    End Sub

#End Region

    Private StateLast As String
    Public Property IsMouseOverAnimationEnabled As Boolean = True

    Public Sub RefreshColor(sender As Object, e As EventArgs)
        If ContentHandler IsNot Nothing Then
            ContentHandler(Me, e)
            ContentHandler = Nothing
        End If
        Dim StateNew As String
        Dim Time As Integer
        If IsMouseDown AndAlso Not (Type = CheckType.RadioBox AndAlso Checked) Then
            StateNew = "MouseDown"
            Time = 120
        ElseIf IsPointerOver AndAlso IsMouseOverAnimationEnabled Then
            StateNew = "MouseOver"
            Time = 120
        Else
            StateNew = "Idle"
            Time = 180
        End If
        If StateLast = StateNew Then Return
        StateLast = StateNew
        If IsControlLoaded AndAlso AniControlEnabled = 0 Then
            Dim Ani As New List(Of AniData)
            If StateNew <> "Idle" Then
                If ButtonStack IsNot Nothing Then
                    Ani.Add(AaOpacity(ButtonStack, 1 - ButtonStack.Opacity, Time * 0.7, Time * 0.3))
                    Dim TargetPadding = Math.Max(MinPaddingRight, 5 + If(_Buttons Is Nothing, 0, _Buttons.Count) * 25)
                    Ani.Add(AaDouble(Sub(i As Object)
                                         ColumnDefinitions(4).Width = New GridLength(Math.Max(0, ColumnDefinitions(4).Width.Value + CDbl(i)))
                                     End Sub, TargetPadding - ColumnDefinitions(4).Width.Value, Time * 0.3, Time * 0.7))
                End If
                Ani.AddRange({
                    AaColor(RectBack, Border.BackgroundProperty, If(StateNew = "MouseDown", "ColorBrush6", "ColorBrushBg1"), Time),
                    AaOpacity(RectBack, 1 - RectBack.Opacity, Time, , New AniEaseOutFluent)})
                If IsScaleAnimationEnabled Then
                    Ani.Add(AaScaleTransform(RectBack, 1 - AniScaleX(RectBack), Time * 1.6, , New AniEaseOutFluent))
                    Ani.Add(AaScaleTransform(Me, If(StateNew = "MouseDown", 0.98, 1.0) - AniScaleX(Me), Time * 0.9, , New AniEaseOutFluent))
                End If
            Else
                If ButtonStack IsNot Nothing Then
                    Ani.Add(AaOpacity(ButtonStack, -ButtonStack.Opacity, Time * 0.4))
                    Ani.Add(AaDouble(Sub(i As Object)
                                         ColumnDefinitions(4).Width = New GridLength(Math.Max(0, ColumnDefinitions(4).Width.Value + CDbl(i)))
                                     End Sub, MinPaddingRight - ColumnDefinitions(4).Width.Value, Time * 0.4))
                End If
                Ani.Add(AaOpacity(RectBack, -RectBack.Opacity, Time))
                If IsScaleAnimationEnabled Then
                    Ani.AddRange({
                        AaColor(RectBack, Border.BackgroundProperty, "ColorBrush7", Time),
                        AaScaleTransform(Me, 1 - AniScaleX(Me), Time * 3, , New AniEaseOutFluent),
                        AaScaleTransform(RectBack, 0.75 - AniScaleX(RectBack), Time, , New AniEaseOutFluent)})
                End If
            End If
            AniStart(Ani, "ListItem Color " & Uuid)
        Else
            If StateNew <> "Idle" Then
                If ButtonStack IsNot Nothing Then
                    ButtonStack.Opacity = 1
                    ColumnDefinitions(4).Width = New GridLength(Math.Max(MinPaddingRight, 5 + If(_Buttons Is Nothing, 0, _Buttons.Count) * 25))
                End If
                RectBack.SetResourceReference(Border.BackgroundProperty, "ColorBrushBg1")
                RectBack.Opacity = 1
                RectBack.RenderTransform = New ScaleTransform(1, 1)
                Me.RenderTransform = New ScaleTransform(1, 1)
            Else
                If ButtonStack IsNot Nothing Then
                    ButtonStack.Opacity = 0
                    ColumnDefinitions(4).Width = New GridLength(MinPaddingRight)
                End If
                Me.RenderTransform = New ScaleTransform(1, 1)
                If _RectBack IsNot Nothing Then
                    If IsScaleAnimationEnabled Then RectBack.RenderTransform = New ScaleTransform(0.75, 0.75)
                    RectBack.SetResourceReference(Border.BackgroundProperty, "ColorBrush7")
                    RectBack.Opacity = 0
                End If
            End If
            AniStop("ListItem Color " & Uuid)
        End If
    End Sub

    Private Sub MyListItem_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        '帮助中的“打开帮助”条目未设置标题时，自动从目标帮助项读取
        If CustomEventService.GetEventType(Me) = CustomEvent.EventType.打开帮助 AndAlso Not (Title <> "" AndAlso Info <> "") Then
            Try
                Dim Entry As New HelpEntry(CustomEvent.GetAbsoluteUrls(CustomEventService.GetEventData(Me), CustomEvent.EventType.打开帮助)(0))
                Entry.SetToListItem(Me)
            Catch ex As Exception
                Logger.Error(ex, "设置帮助 MyListItem 失败", LogBehavior.Alert)
                CustomEventService.SetEventType(Me, CustomEvent.EventType.None)
                CustomEventService.SetEventData(Me, "")
            End Try
        End If
    End Sub

    Public Overrides Function ToString() As String
        Return Title
    End Function

End Class
