Imports Avalonia.Controls.Shapes

Public Class MySlider
    Inherits Border
    Implements ISettingControl

    Public Uuid As Integer = GetUuid()
    Public Event Change(sender As Object, user As Boolean)
    Public Event PreviewChange(sender As Object, e As RouteEventArgs)

    '自定义属性

    Private _MaxValue As Integer = 100
    Public Property MaxValue As Integer
        Get
            Return _MaxValue
        End Get
        Set(value As Integer)
            If value = _MaxValue Then Return
            _MaxValue = value
            RefreshWidth()
        End Set
    End Property

    Private ChangeByKey As Boolean = False
    Private _Value As Integer = 0
    Public Property Value As Integer
        Get
            Return _Value
        End Get
        Set(newValue As Integer)
            Try
                newValue = Math.Max(0, Math.Min(newValue, MaxValue))
                If _Value = newValue Then Return

                Dim OldValue = _Value
                _Value = newValue
                If AniControlEnabled = 0 Then
                    Dim e As New RouteEventArgs(False)
                    RaiseEvent PreviewChange(Me, e)
                    If e.Handled Then
                        _Value = OldValue
                        DragStop()
                        Return
                    End If
                End If

                If IsControlLoaded AndAlso AniControlEnabled = 0 AndAlso MaxValue > 0 Then
                    If Bounds.Width < ShapeDot.Width Then Return
                    Dim NewWidth As Double = _Value / MaxValue * (Bounds.Width - ShapeDot.Width)
                    Dim DeltaProcess As Double = Math.Abs(LineFore.Width / (Bounds.Width - ShapeDot.Width) - _Value / MaxValue)
                    Dim Time As Double = (1 - Math.Pow(1 - DeltaProcess, 3)) * 300 + If(ChangeByKey, 100, 0)
                    AniStart({
                            AaWidth(LineFore, Math.Max(0, NewWidth + If(NewWidth < 0.5, 0, 0.5)) - LineFore.Width, Time, , If(Time > 50, New AniEaseOutFluent, New AniEaseLinear)),
                            AaWidth(LineBack, Math.Max(0, Bounds.Width - ShapeDot.Width - NewWidth + If(Bounds.Width - ShapeDot.Width - NewWidth < 0.5, 0, 0.5)) - LineBack.Width, Time, , If(Time > 50, New AniEaseOutFluent, New AniEaseLinear)),
                            AaX(ShapeDot, NewWidth - ShapeDot.Margin.Left, Time, , If(Time > 50, New AniEaseOutFluent, New AniEaseLinear))
                         }, "MySlider Progress " & Uuid)
                Else
                    RefreshWidth()
                End If
                If AniControlEnabled = 0 Then RaiseEvent Change(Me, False)
            Catch ex As Exception
                Logger.Error(ex, "滑动条进度改变出错", LogBehavior.Toast)
            End Try
        End Set
    End Property

    Private Sub RefreshWidth()
        AniStop("MySlider Progress " & Uuid)
        If MaxValue <= 0 Then Return
        Dim WidthAvailable As Double = Math.Max(0, Bounds.Width - ShapeDot.Width)
        Dim NewWidth As Double = _Value / MaxValue * WidthAvailable
        LineFore.Width = Math.Max(0, NewWidth + If(NewWidth < 0.5, 0, 0.5))
        LineBack.Width = Math.Max(0, WidthAvailable - NewWidth + If(WidthAvailable - NewWidth < 0.5, 0, 0.5))
        ShapeDot.Margin = New Thickness(NewWidth, ShapeDot.Margin.Top, ShapeDot.Margin.Right, ShapeDot.Margin.Bottom)
    End Sub

    Protected Overrides Sub OnSizeChanged(e As SizeChangedEventArgs)
        MyBase.OnSizeChanged(e)
        PanMain.Width = Bounds.Width
        RefreshWidth()
    End Sub

    '拖动

    Public GetHintText As [Delegate]
    Private IsDragging As Boolean = False

    Private Sub DragStart(sender As Object, e As PointerPressedEventArgs)
        If Not e.GetCurrentPoint(Me).Properties.IsLeftButtonPressed Then Return
        e.Handled = True
        IsDragging = True
        MyCard.SetCardHoverLock(Me, Me, True, KeepWhenPointerLeaves:=False)
        e.Pointer.Capture(Me)
        RefreshColor()
        AniStart({
                 AaScaleTransform(ShapeDot, 1.3 - AniScaleX(ShapeDot), 40, , New AniEaseOutFluent)
            }, "MySlider Scale " & Uuid)
        RefreshPopup()
        AniStop("MySlider KeyPopup " & Uuid)
        DragDoing(e.GetPosition(PanMain))
    End Sub

    Private Sub DragDoing(Position As Point)
        If Not IsDragging OrElse MaxValue <= 0 Then Return
        Dim WidthAvailable As Double = Math.Max(1, Bounds.Width - ShapeDot.Width)
        Dim Percent As Double = Math.Max(0, Math.Min((Position.X - ShapeDot.Width / 2) / WidthAvailable, 1))
        Dim NewValue As Integer = CInt(Percent * MaxValue)
        If NewValue <> Value Then Value = NewValue
        RefreshPopup()
    End Sub

    Private Sub DragStop()
        IsDragging = False
        MyCard.SetCardHoverLock(Me, Me, False)
        RefreshColor()
        AniStart({
                 AaScaleTransform(ShapeDot, 1 - AniScaleX(ShapeDot), 200, , New AniEaseOutFluent)
            }, "MySlider Scale " & Uuid)
        Popup.IsOpen = False
    End Sub

    Public Sub RefreshPopup()
        If GetHintText Is Nothing Then Return
        Popup.IsOpen = True
        TextHint.Text = CStr(GetHintText.DynamicInvoke(Value))
    End Sub

    '指向动画

    Private Sub RefreshColor()
        Try
            Dim ForegroundName As String, DotFillName As String
            Dim AnimationTime As Integer
            If IsEnabled Then
                If IsDragging OrElse IsPointerOver Then
                    ForegroundName = "ColorBrush3"
                    DotFillName = "ColorBrush3"
                    AnimationTime = 40
                Else
                    ForegroundName = "ColorBrushBg0"
                    DotFillName = "ColorBrushBg0"
                    AnimationTime = 100
                End If
            Else
                ForegroundName = "ColorBrushGray5"
                DotFillName = "ColorBrushGray5"
                AnimationTime = 200
            End If
            If IsControlLoaded AndAlso AniControlEnabled = 0 Then
                AniStart({
                            AaColor(Me, Border.BorderBrushProperty, ForegroundName, AnimationTime),
                            AaColor(ShapeDot, Shape.FillProperty, DotFillName, AnimationTime)
                         }, "MySlider Color " & Uuid)
            Else
                AniStop("MySlider Color " & Uuid)
                SetResourceReference(Border.BorderBrushProperty, ForegroundName)
                ShapeDot.SetResourceReference(Shape.FillProperty, DotFillName)
            End If
        Catch ex As Exception
            Logger.Warn(ex, "滑动条颜色改变出错")
        End Try
    End Sub

    '按键改变

    Public Property ValueByKey As UInteger = 1

    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        MyBase.OnKeyDown(e)
        If IsDragging Then Return
        If e.Key = Key.Left Then
            ChangeByKey = True
            Value -= CInt(ValueByKey)
            ChangeByKey = False
            e.Handled = True
        ElseIf e.Key = Key.Right Then
            ChangeByKey = True
            Value += CInt(ValueByKey)
            ChangeByKey = False
            e.Handled = True
        Else
            Return
        End If
        If GetHintText IsNot Nothing Then
            RefreshPopup()
            AniStop("MySlider KeyPopup " & Uuid)
            AniStart(AaCode(Sub() Popup.IsOpen = False, 700 * AniSpeed), "MySlider KeyPopup " & Uuid)
        End If
    End Sub

#Region "初始化"

    Private IsControlLoaded As Boolean = False

    Public Sub New()
        InitControls()
        MyBase.AddHandler(InputElement.PointerPressedEvent, AddressOf DragStart, RoutingStrategies.Tunnel)
        MyBase.AddHandler(Me.PointerMovedEvent, Sub(sender, e) If IsDragging Then DragDoing(e.GetPosition(PanMain)))
        MyBase.AddHandler(Me.PointerReleasedEvent,
            Sub(sender, e)
                If IsDragging Then
                    e.Pointer.Capture(Nothing)
                    DragStop()
                End If
            End Sub)
        MyBase.AddHandler(Me.PointerEnteredEvent,
            Sub(sender, e)
                Focus()
                RefreshColor()
            End Sub)
        MyBase.AddHandler(Me.PointerExitedEvent, Sub(sender, e) If Not IsDragging Then RefreshColor())
        MyBase.AddHandler(Me.PointerCaptureLostEvent, Sub(sender, e) If IsDragging Then DragStop())
    End Sub

    Protected Overrides Sub OnAttachedToVisualTree(e As VisualTreeAttachmentEventArgs)
        MyBase.OnAttachedToVisualTree(e)
        IsControlLoaded = True
        RefreshWidth()
        RefreshColor()
    End Sub

    Protected Overrides Sub OnPropertyChanged(change As AvaloniaPropertyChangedEventArgs)
        MyBase.OnPropertyChanged(change)
        If change.Property Is IsEnabledProperty Then RefreshColor()
    End Sub

#End Region

#Region "设置"

    Private Sub RefreshSetting(NewValue As String) Implements ISettingControl.RefreshSetting
        Value = CInt(Val(NewValue))
    End Sub

    Private Function GetCurrentSetting() As String Implements ISettingControl.GetCurrentSetting
        Return Value.ToString()
    End Function

    Private Sub SaveSetting() Handles Me.Change
        SettingService.SaveSetting(Me)
    End Sub

#End Region

End Class
