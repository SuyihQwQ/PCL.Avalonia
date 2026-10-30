Imports Avalonia.Controls.Documents
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
            MainChrome.CornerRadius = value
            MainBorder.CornerRadius = value
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

    'UI 建立
    Public Sub New()
        'Avalonia 中背景为空的 Panel 不参与命中测试，会导致无法点击卡片标题栏
        Background = Brushes.Transparent
        MainChrome = New MyDropShadow With {
            .Margin = New Thickness(-3, -3, -3, -3 - GetWPFSize(1)),
            .ShadowRadius = 3,
            .Opacity = DropShadowIdleOpacity,
            .CornerRadius = New CornerRadius(5)}
        MainChrome.SetResourceReference(MyDropShadow.ColorProperty, "ColorObject1")
        Children.Insert(0, MainChrome)
        MainBorder = New Border With {
            .Background = New SolidColorBrush(Color.FromArgb(245, 255, 255, 255)),
            .CornerRadius = New CornerRadius(5),
            .IsHitTestVisible = False}
        Children.Insert(1, MainBorder)
        MainGrid = New Grid
        Children.Add(MainGrid)
        AddHandler PointerEntered, AddressOf MyCard_MouseEnter
        AddHandler PointerExited, AddressOf MyCard_MouseLeave
        AddHandler PointerExited, AddressOf MyCard_MouseLeave_Swap
        AddHandler PointerMoved, AddressOf UpdateCursorStyle
        AddHandler PointerPressed, AddressOf MyCard_MouseLeftButtonDown
        AddHandler PointerReleased, AddressOf MyCard_MouseLeftButtonUp
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
                .Margin = New Thickness(15, 12, 0, 0),
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
            Height = SwapedHeight
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
    Private Const DropShadowIdleOpacity As Double = 0.07
    Private Const DropShadowHoverOpacity As Double = 0.4
    Public Property HasMouseAnimation As Boolean = True

    Private Sub MyCard_MouseEnter(sender As Object, e As PointerEventArgs)
        If Not HasMouseAnimation Then Return
        Dim AniList As New List(Of AniData)
        If _MainTextBlock IsNot Nothing Then AniList.Add(AaColor(_MainTextBlock, TextBlock.ForegroundProperty, "ColorBrush2", 90))
        If _MainSwap IsNot Nothing Then AniList.Add(AaColor(_MainSwap, Path.FillProperty, "ColorBrush2", 90))
        AniList.AddRange({
            AaColor(MainChrome, MyDropShadow.ColorProperty, "ColorObject4", 90),
            AaOpacity(MainChrome, DropShadowHoverOpacity - MainChrome.Opacity, 90)
        })
        AniStart(AniList, "MyCard Mouse " & Uuid)
    End Sub

    Private Sub MyCard_MouseLeave(sender As Object, e As PointerEventArgs)
        If Not HasMouseAnimation Then Return
        Dim AniList As New List(Of AniData)
        If _MainTextBlock IsNot Nothing Then AniList.Add(AaColor(_MainTextBlock, TextBlock.ForegroundProperty, "ColorBrush1", 90))
        If _MainSwap IsNot Nothing Then AniList.Add(AaColor(_MainSwap, Path.FillProperty, "ColorBrush1", 90))
        AniList.AddRange({
            AaColor(MainChrome, MyDropShadow.ColorProperty, "ColorObject1", 90),
            AaOpacity(MainChrome, DropShadowIdleOpacity - MainChrome.Opacity, 90)
        })
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
        If Not IsSwapped Then BuildContent()
        SwapControl.IsVisible = True
        If IsSwapped Then
            SwapControl.IsVisible = False
            Height = SwapedHeight
        Else
            Height = Double.NaN
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
