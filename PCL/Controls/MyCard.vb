Imports Avalonia.Controls.Documents
Imports Avalonia.Threading
Imports Avalonia.VisualTree
Imports Path = Avalonia.Controls.Shapes.Path

Public Class MyCard
    Inherits Grid

    '控件
    Private ReadOnly MainGrid As Grid
    Public ReadOnly Property MainChrome As MyDropShadow
    Private ReadOnly MainBorder As Border
    Public Property BorderChild As Control
        Get
            Return MainBorder.Child
        End Get
        Set(value As Control)
            MainBorder.Child = value
        End Set
    End Property
    Private _MainTextBlock As TextBlock
    Public Property MainTextBlock As TextBlock
        Get
            Init()
            Return _MainTextBlock
        End Get
        Set(value As TextBlock)
            _MainTextBlock = value
        End Set
    End Property
    Private _MainSwap As Path
    Public Property MainSwap As Path
        Get
            Init()
            Return _MainSwap
        End Get
        Set(value As Path)
            _MainSwap = value
        End Set
    End Property

    '属性
    Public Uuid As Integer = GetUuid()
    Public ReadOnly Property Inlines As InlineCollection
        Get
            Return MainTextBlock.Inlines
        End Get
    End Property
    Private _CornerRadius As New CornerRadius(5)
    Public Property CornerRadius As CornerRadius
        Get
            Return _CornerRadius
        End Get
        Set(value As CornerRadius)
            _CornerRadius = value
            MainBorder.CornerRadius = value
            If MainChrome IsNot Nothing Then MainChrome.CornerRadius = value
        End Set
    End Property
    Public Property Title As String
        Get
            Return GetValue(TitleProperty)
        End Get
        Set(value As String)
            SetValue(TitleProperty, value)
        End Set
    End Property
    Public Shared ReadOnly TitleProperty As StyledProperty(Of String) =
        AvaloniaProperty.Register(Of MyCard, String)("Title", "")

    Private _TitleOffset As Double = 0
    ''' <summary>
    ''' 标题文字的垂直偏移，用于修正不同卡片高度的基线差异。
    ''' </summary>
    Public Property TitleOffset As Double
        Get
            Return _TitleOffset
        End Get
        Set(value As Double)
            _TitleOffset = value
            If _MainTextBlock IsNot Nothing Then _MainTextBlock.Margin = New Thickness(15, 10 + value, 0, 0)
        End Set
    End Property

    'UI 建立
    Public Sub New()
        'Avalonia 中背景为空的 Panel 不参与命中测试，会导致无法点击卡片标题栏
        Background = Brushes.Transparent
        '与原版一致：卡片四周始终有一层淡阴影，悬停时加深
        '阴影与卡片同尺寸，避免卡片边缘和阴影之间出现一圈透明间隙
        MainChrome = New MyDropShadow With {
            .Margin = New Thickness(0),
            .ShadowRadius = 3,
            .Opacity = DropShadowIdleOpacity,
            .CornerRadius = New CornerRadius(5)}
        MainChrome.SetResourceReference(MyDropShadow.ColorProperty, "ColorObject1")
        Children.Insert(0, MainChrome)
        MainBorder = New Border With {
            .CornerRadius = New CornerRadius(5),
            .IsHitTestVisible = False}
        MainBorder.SetResourceReference(Border.BackgroundProperty, "ColorBrushCard")
        Children.Insert(1, MainBorder)
        MainGrid = New Grid
        Children.Add(MainGrid)
        AddHandler PointerEntered, AddressOf MyCard_MouseEnter
        AddHandler PointerExited, AddressOf MyCard_MouseLeave
        AddHandler PointerExited, AddressOf MyCard_MouseLeave_Swap
        AddHandler PointerMoved, AddressOf UpdateCursorStyle
        AddHandler PointerMoved, AddressOf MyCard_PointerMovedWhileLocked
        AddHandler PointerPressed, AddressOf MyCard_MouseLeftButtonDown
        AddHandler PointerReleased, AddressOf MyCard_MouseLeftButtonUp
        AddHandler SizeChanged, AddressOf MySizeChanged
    End Sub

    '高度改变动画
    Private IsHeightAnimating As Boolean = False
    Private ActualUsedHeight As Double '回滚实际高度（例如 NaN）

    ''' <summary>
    ''' 高度动画期间是否已对内容启用裁剪。
    ''' </summary>
    Private ContentClipActive As Boolean = False

    ''' <summary>
    ''' 高度动画期间把内容裁剪到卡片当前范围内，否则内容会溢出卡片，直到动画结束才突然消失。
    ''' 只裁剪内容，不裁剪卡片背景。
    ''' </summary>
    Private Sub SetContentClipActive(Active As Boolean)
        ContentClipActive = Active
        If Active Then
            UpdateContentClips(Bounds.Width, Bounds.Height)
            Return
        End If
        For Each ChildControl As Control In Children
            If ChildControl Is MainChrome OrElse ChildControl Is MainBorder OrElse ChildControl Is MainGrid Then Continue For
            ChildControl.Clip = Nothing
        Next
    End Sub

    Private Sub UpdateContentClips(Width As Double, Height As Double)
        For Each ChildControl As Control In Children
            If ChildControl Is MainChrome OrElse ChildControl Is MainBorder OrElse ChildControl Is MainGrid Then Continue For
            Dim ChildBounds = ChildControl.Bounds
            ChildControl.Clip = New RectangleGeometry(New Rect(-ChildBounds.X, -ChildBounds.Y, Width, Height))
        Next
    End Sub

    Private Sub MySizeChanged(sender As Object, e As SizeChangedEventArgs)
        If ContentClipActive Then UpdateContentClips(e.NewSize.Width, e.NewSize.Height)
        If Not UseAnimation Then Return
        Dim DeltaHeight As Double = (If(IsSwapped, SwapedHeight, e.NewSize.Height)) - e.PreviousSize.Height
        '卡片的进入时动画已被页面通用切换动画替代
        If e.PreviousSize.Height = 0 OrElse IsHeightAnimating OrElse Math.Abs(DeltaHeight) < 1 OrElse Bounds.Height = 0 Then Return
        StartHeightAnimation(DeltaHeight, e.PreviousSize.Height)
    End Sub

    ''' <summary>
    ''' 停止当前高度动画，并把当前高度作为下一次动画的起点。
    ''' </summary>
    Private Sub StopHeightAnimation()
        AniStop("MyCard Height " & Uuid)
        IsHeightAnimating = False
        SetContentClipActive(False)
    End Sub

    Private Sub StartHeightAnimation(Delta As Double, PreviousHeight As Double)
        If IsHeightAnimating Then Return

        Dim AnimList As New List(Of AniData)
        Dim AbsDelta As Double = Math.Abs(Delta)

        If AbsDelta <= 800 Then
            '短距离，直接使用 150ms 的缓动动画
            AnimList.Add(AaHeight(Me, Delta, 150,, New AniEaseOutFluent(AniEasePower.ExtraStrong)))
        Else
            Dim EaseLength As Double
            Dim EaseTime As Integer
            Dim InitSpeed As Double '到达缓动区前的初速度
            If Delta < 0 AndAlso AbsDelta - EaseLength > 5000 * 0.1 Then
                '收回距离过长 (>0.1s)，强制以 100ms 完成匀速段，然后让减速段更长
                EaseLength = 200
                EaseTime = 150
                InitSpeed = (AbsDelta - EaseLength) / 0.1
            ElseIf Delta > 0 AndAlso AbsDelta - EaseLength > 5000 * 0.6 Then
                '展开距离过长 (>0.6s)，以 5000 速度展示 300ms 匀速段，剩下的距离全部归入减速段
                InitSpeed = 5000
                EaseLength = AbsDelta - InitSpeed * 0.3
                EaseTime = 400
            Else
                '中程，匀速地快速展开（或收回）
                EaseLength = 150
                EaseTime = 200
                InitSpeed = 4000
            End If
            '匀速段
            AnimList.Add(AaHeight(Me, (AbsDelta - EaseLength) * Math.Sign(Delta),
                CInt((AbsDelta - EaseLength) / InitSpeed * 1000)))
            '减速段
            AnimList.Add(AaHeight(Me, EaseLength * Math.Sign(Delta),
                EaseTime,, New AniEaseOutFluentWithInitial(InitSpeed, EaseTime / 1000, EaseLength), True))
        End If

        AnimList.Add(AaCode(
        Sub()
            IsHeightAnimating = False
            If IsSwapped Then
                '折叠时保留动画最终高度，避免再写入 40 造成最后 1px 的跳动
                If SwapControl IsNot Nothing Then SwapControl.IsVisible = False
            Else
                Height = ActualUsedHeight
            End If
            SetContentClipActive(False)
        End Sub,, True))
        AniStart(AnimList, "MyCard Height " & Uuid)
        IsHeightAnimating = True
        ActualUsedHeight = If(IsSwapped, SwapedHeight, Height)
        Height = PreviousHeight
        SetContentClipActive(True)
    End Sub

    Private IsLoad As Boolean = False
    Private Sub Init()
        If IsLoad Then Return
        If MainGrid Is Nothing Then Return
        IsLoad = True
        '初次加载限定
        If _MainTextBlock Is Nothing Then
            _MainTextBlock = New TextBlock With {
                .HorizontalAlignment = HorizontalAlignment.Left,
                .VerticalAlignment = VerticalAlignment.Top,
                .Margin = New Thickness(15, 10 + TitleOffset, 0, 0),
                .FontWeight = FontWeight.Bold,
                .FontSize = 13,
                .IsHitTestVisible = False,
                .Text = Title}
            _MainTextBlock.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrush1")
            MainGrid.Children.Add(_MainTextBlock)
        End If
        If CanSwap OrElse SwapControl IsNot Nothing Then
            If SwapControl Is Nothing AndAlso Children.Count > 3 Then SwapControl = Children(3)
            _MainSwap = New Path With {
                .HorizontalAlignment = HorizontalAlignment.Right,
                .Stretch = Stretch.Uniform,
                .Height = 6,
                .Width = 10,
                .VerticalAlignment = VerticalAlignment.Top,
                .Margin = New Thickness(0, 17, 16, 0),
                .Data = Geometry.Parse("M2,4 l-2,2 10,10 10,-10 -2,-2 -8,8 -8,-8 z"),
                .RenderTransform = New RotateTransform(180),
                .RenderTransformOrigin = New RelativePoint(0.5, 0.5, RelativeUnit.Relative)}
            _MainSwap.SetResourceReference(Path.FillProperty, "ColorBrush1")
            MainGrid.Children.Add(_MainSwap)
        End If
        '改变默认的折叠
        If IsSwapped AndAlso SwapControl IsNot Nothing Then
            If _MainSwap IsNot Nothing Then
                _MainSwap.RenderTransform = New RotateTransform(If(SwapLogoRight, 270, 0))
            End If
            SwapControl.IsVisible = False
            '取消由于高度变化被迫触发的高度动画
            Dim RawUseAnimation As Boolean = UseAnimation
            UseAnimation = False
            Height = SwapedHeight
            AniStop("MyCard Height " & Uuid)
            IsHeightAnimating = False
            RunInUi(Sub() UseAnimation = RawUseAnimation, True)
        End If
    End Sub

    Protected Overrides Sub OnAttachedToVisualTree(e As VisualTreeAttachmentEventArgs)
        MyBase.OnAttachedToVisualTree(e)
        Init()
    End Sub

    Protected Overrides Sub OnPropertyChanged(change As AvaloniaPropertyChangedEventArgs)
        MyBase.OnPropertyChanged(change)
        If change.Property Is TitleProperty AndAlso _MainTextBlock IsNot Nothing Then
            _MainTextBlock.Text = Title
        End If
    End Sub

    '填充
    Private ContentBuilder As Action(Of Panel)
    Public Sub SetContentBuilder(Builder As Action(Of Panel))
        ContentBuilder = Builder
    End Sub
    Public Sub SetContentBuilder(Of T)(Items As IEnumerable(Of T), ItemBuilder As Func(Of T, Control))
        SetContentBuilder(
        Sub(Stack)
            For Each ChildItem In Items
                Stack.Children.Add(ItemBuilder(ChildItem))
            Next
            Stack.Children.Add(New ContentControl With {.Height = 18})
        End Sub)
    End Sub
    ''' <summary>
    ''' 使用 ContentBuilder 生成卡片内容。
    ''' </summary>
    Public Sub BuildContent()
        If ContentBuilder Is Nothing Then Return
        Dim Stack = TryCast(SwapControl, Panel)
        If Stack Is Nothing Then Return
        Try
            ContentBuilder(Stack)
            ContentBuilder = Nothing
        Catch
            Stack.Children.Clear()
            Throw
        End Try
        TriggerForceResize()
    End Sub

    '动画
    Public Property HasMouseAnimation As Boolean = True

    Private Const DropShadowIdleOpacity As Double = 0.07
    Private Const DropShadowHoverOpacity As Double = 0.4

    Private HoverLeavePending As Boolean
    Private HoverRemovedDuringInteraction As Boolean

    Private Function IsPositionInsideCard(Position As Point) As Boolean
        Return Position.X >= 0 AndAlso Position.Y >= 0 AndAlso Position.X <= Bounds.Width AndAlso Position.Y <= Bounds.Height
    End Function

    Private Sub ApplyHoverShadow()
        AniStart({
            AaColor(MainChrome, MyDropShadow.ColorProperty, "ColorObject4", 90),
            AaOpacity(MainChrome, DropShadowHoverOpacity - MainChrome.Opacity, 90)
        }, "MyCard Shadow " & Uuid)
    End Sub

    Private Sub ApplyIdleShadow()
        AniStart({
            AaColor(MainChrome, MyDropShadow.ColorProperty, "ColorObject1", 90),
            AaOpacity(MainChrome, DropShadowIdleOpacity - MainChrome.Opacity, 90)
        }, "MyCard Shadow " & Uuid)
    End Sub

    Private Sub RestoreHoverState()
        HoverLeavePending = False
        HoverRemovedDuringInteraction = False
        AniStop("MyCard HoverLeave " & Uuid)
        ApplyHoverShadow()
        ApplyHoverColor()
    End Sub

    Private Sub ScheduleHoverLeave()
        If HoverLeavePending OrElse HoverRemovedDuringInteraction Then Return
        HoverLeavePending = True
        '指针捕获切换（如点击文本框）可能产生瞬时的离开事件，延迟确认避免标题与阴影闪烁
        AniStart(
            AaCode(
            Sub()
                HoverLeavePending = False
                If HoverRemovedDuringInteraction Then Return
                If IsPointerOver Then
                    HoverRemovedDuringInteraction = False
                    Return
                End If
                HoverRemovedDuringInteraction = True
                ApplyIdleShadow()
                '滑块拖动离开卡片时恢复标题颜色；下拉框等交互仍保持高亮
                If HoverLocks.Count = 0 OrElse Not HoverLocks.Values.Any(Function(v) v) Then RemoveHoverColor()
            End Sub, 60), "MyCard HoverLeave " & Uuid)
    End Sub

    Private Sub MyCard_MouseEnter(sender As Object, e As PointerEventArgs)
        RestoreHoverState()
    End Sub

    Private Sub MyCard_MouseLeave(sender As Object, e As PointerEventArgs)
        '指针被滑块等子控件捕获时，离开子控件也会触发卡片的离开事件；此时指针仍在卡片内，视同普通悬停
        If IsPositionInsideCard(e.GetPosition(Me)) Then
            RestoreHoverState()
            Return
        End If
        ScheduleHoverLeave()
    End Sub

    ''' <summary>
    ''' 拖动滑块等捕获指针的交互期间，卡片的悬停事件不再可靠，按真实指针位置更新悬停状态。
    ''' </summary>
    Private Sub MyCard_PointerMovedWhileLocked(sender As Object, e As PointerEventArgs)
        If HoverLocks.Count = 0 OrElse HoverLocks.Values.Any(Function(v) v) Then Return
        If IsPositionInsideCard(e.GetPosition(Me)) Then
            If HoverLeavePending OrElse HoverRemovedDuringInteraction Then RestoreHoverState()
        Else
            ScheduleHoverLeave()
        End If
    End Sub

    '拖动滑块或展开下拉框时指针可能离开卡片；值为 True 表示离开卡片后仍保持标题高亮
    Private ReadOnly HoverLocks As New Dictionary(Of Object, Boolean)()

    Friend Sub SetHoverLock(Key As Object, Active As Boolean, Optional KeepWhenPointerLeaves As Boolean = True)
        If Active Then
            AniStop("MyCard HoverRelease " & Uuid)
            AniStop("MyCard HoverLeave " & Uuid)
            HoverLeavePending = False
            HoverRemovedDuringInteraction = False
            HoverLocks(Key) = KeepWhenPointerLeaves
            ApplyHoverColor()
        Else
            HoverLocks.Remove(Key)
            If HoverLocks.Count = 0 Then
                '下拉框关闭后指针状态会短暂抖动，延迟一小段时间再决定是否取消高亮，避免标题颜色闪一下
                AniStop("MyCard HoverRelease " & Uuid)
                AniStart(
                    AaCode(
                    Sub()
                        If HoverLocks.Count = 0 AndAlso Not IsPointerOver Then RemoveHoverColor()
                    End Sub, 120), "MyCard HoverRelease " & Uuid)
            End If
        End If
    End Sub

    Friend Shared Sub SetCardHoverLock(Source As Visual, Key As Object, Active As Boolean, Optional KeepWhenPointerLeaves As Boolean = True)
        Dim Card = Source?.GetVisualAncestors().OfType(Of MyCard)().FirstOrDefault()
        Card?.SetHoverLock(Key, Active, KeepWhenPointerLeaves)
    End Sub

    Private Sub ApplyHoverColor()
        If Not HasMouseAnimation Then Return
        Dim AniList As New List(Of AniData)
        If _MainTextBlock IsNot Nothing Then AniList.Add(AaColor(_MainTextBlock, TextBlock.ForegroundProperty, "ColorBrush2", 90))
        If _MainSwap IsNot Nothing Then AniList.Add(AaColor(_MainSwap, Path.FillProperty, "ColorBrush2", 90))
        AniStart(AniList, "MyCard Mouse " & Uuid)
    End Sub

    Private Sub RemoveHoverColor()
        If Not HasMouseAnimation Then Return
        Dim AniList As New List(Of AniData)
        If _MainTextBlock IsNot Nothing Then AniList.Add(AaColor(_MainTextBlock, TextBlock.ForegroundProperty, "ColorBrush1", 90))
        If _MainSwap IsNot Nothing Then AniList.Add(AaColor(_MainSwap, Path.FillProperty, "ColorBrush1", 90))
        AniStart(AniList, "MyCard Mouse " & Uuid)
    End Sub

    '高度
    ''' <summary>
    ''' 是否启用高度改变动画。
    ''' </summary>
    Public Property UseAnimation As Boolean = True

    ''' <summary>
    ''' 通知 MyCard，控件内容已改变，需要瞬间更新高度。
    ''' </summary>
    Public Sub TriggerForceResize()
        Height = If(IsSwapped, SwapedHeight, Double.NaN)
        AniStop("MyCard Height " & Uuid)
        IsHeightAnimating = False
        SetContentClipActive(False)
    End Sub

    '折叠
    Public SwapControl As Control
    Public Property CanSwap As Boolean = False

    ''' <summary>
    ''' 是否已被折叠。
    ''' </summary>
    Public Property IsSwapped As Boolean
        Get
            Return _IsSwapped
        End Get
        Set(value As Boolean)
            If _IsSwapped = value Then Return
            _IsSwapped = value
            If SwapControl Is Nothing Then Return
            If Not IsLoad Then Return
            ApplySwap()
        End Set
    End Property
    Private _IsSwapped As Boolean = False

    ''' <summary>
    ''' IsSwaped 存在拼写错误，请换用 IsSwapped 属性。
    ''' </summary>
    <Obsolete("IsSwaped 存在拼写错误，请换用 IsSwapped 属性。")>
    Public Property IsSwaped As Boolean
        Get
            Return IsSwapped
        End Get
        Set(value As Boolean)
            IsSwapped = value
        End Set
    End Property

    Public Property SwapLogoRight As Boolean = False
    Private IsSwapMouseDown As Boolean = False
    Private IsCustomMouseDown As Boolean = False
    Public Event PreviewSwap(sender As Object, e As RouteEventArgs)
    Public Event Swap(sender As Object, e As RouteEventArgs)
    Public Const SwapedHeight As Integer = 40

    Private Sub ApplySwap()
        If IsHeightAnimating Then StopHeightAnimation()
        If Not IsSwapped Then BuildContent()
        Dim PreviousHeight As Double = Bounds.Height
        SwapControl.IsVisible = True
        '折叠时不要先按折叠高度重新布局，否则内容会在裁剪生效前露出一帧
        If IsSwapped Then
            If PreviousHeight > 0 Then
                StartHeightAnimation(SwapedHeight - PreviousHeight, PreviousHeight)
            Else
                TriggerForceResize()
                SwapControl.IsVisible = False
            End If
        Else
            TriggerForceResize()
        End If
        If _MainSwap IsNot Nothing Then
            Dim Target = If(_IsSwapped, If(SwapLogoRight, 270.0, 0.0), 180.0)
            AniStart(AaRotateTransform(_MainSwap, Target - AniAngle(_MainSwap), 250, , New AniEaseOutFluent(AniEasePower.ExtraStrong)), "MyCard Swap " & Uuid, True)
        End If
        UpdateCursorStyle(Me, Nothing)
    End Sub

    Private Sub MyCard_MouseLeftButtonDown(sender As Object, e As PointerPressedEventArgs)
        If Not e.GetCurrentPoint(Me).Properties.IsLeftButtonPressed Then Return
        Dim Pos As Double = e.GetPosition(Me).Y
        If Not IsSwapped AndAlso
            (Pos > SwapedHeight - 6 OrElse (Pos = 0 AndAlso Not IsPointerOver)) Then Return
        IsCustomMouseDown = True
        If Not IsSwapped AndAlso
            (SwapControl Is Nothing OrElse Pos > SwapedHeight - 6 OrElse (Pos = 0 AndAlso Not IsPointerOver)) Then Return
        IsSwapMouseDown = True
    End Sub

    Private Sub MyCard_MouseLeftButtonUp(sender As Object, e As PointerReleasedEventArgs)
        If Not IsCustomMouseDown Then Return
        IsCustomMouseDown = False
        RaiseCustomEvent()

        If Not IsSwapMouseDown Then Return
        IsSwapMouseDown = False

        Dim Pos As Double = e.GetPosition(Me).Y
        If Not IsSwapped AndAlso
            (SwapControl Is Nothing OrElse Pos > SwapedHeight - 6 OrElse (Pos = 0 AndAlso Not IsPointerOver)) Then Return

        Dim Args = New RouteEventArgs(True)
        RaiseEvent PreviewSwap(Me, Args)
        If Args.Handled Then Return

        IsSwapped = Not IsSwapped
        Logger.Info($"{If(IsSwapped, "折叠卡片", "展开卡片")}{If(Title Is Nothing, "", "：" & Title)}")
        RaiseEvent Swap(Me, Args)
    End Sub

    Private Sub MyCard_MouseLeave_Swap(sender As Object, e As PointerEventArgs)
        IsSwapMouseDown = False
    End Sub

    Private Sub UpdateCursorStyle(sender As Object, e As PointerEventArgs)
        If _MainSwap Is Nothing Then Cursor = Nothing : Return
        Dim Pos As Double = If(e Is Nothing, 0, e.GetPosition(Me).Y)
        If _MainSwap.IsVisible AndAlso (IsSwapped OrElse (Pos <= SwapedHeight - 6 AndAlso Not (Pos = 0 AndAlso Not IsPointerOver))) Then
            Cursor = New Cursor(StandardCursorType.Hand)
        Else
            Cursor = Nothing
        End If
    End Sub

End Class

Partial Public Module ModAnimation
    Public Sub AniDispose(Control As MyCard, RemoveFromChildren As Boolean, Optional CallBack As ParameterizedThreadStart = Nothing)
        If Control.IsHitTestVisible Then
            Control.IsHitTestVisible = False
            AniStart({
                AaScaleTransform(Control, -0.08, 200, , New AniEaseInFluent),
                AaOpacity(Control, -1, 200, , New AniEaseOutFluent),
                AaHeight(Control, -Control.Bounds.Height, 150, 100, New AniEaseOutFluent),
                AaCode(
                Sub()
                    If RemoveFromChildren Then
                        Dim ParentPanel = TryCast(Control.Parent, Panel)
                        ParentPanel?.Children.Remove(Control)
                    Else
                        Control.IsVisible = False
                    End If
                    If CallBack IsNot Nothing Then CallBack(Control)
                End Sub, , True)
            }, "MyCard Dispose " & Control.Uuid)
        Else
            If RemoveFromChildren Then
                Dim ParentPanel = TryCast(Control.Parent, Panel)
                ParentPanel?.Children.Remove(Control)
            Else
                Control.IsVisible = False
            End If
            If CallBack IsNot Nothing Then CallBack(Control)
        End If
    End Sub
End Module
