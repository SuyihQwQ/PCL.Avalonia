Imports Avalonia.Controls.Primitives

Public Class MyScrollBar
    Inherits ScrollBar

    Public Uuid As Integer = GetUuid()

    Private IsCaptured As Boolean = False
    Private IsControlLoaded As Boolean = False

    Public Sub New()
        AddHandler PointerEntered, AddressOf RefreshColorHandler
        AddHandler PointerExited, AddressOf RefreshColorHandler
        AddHandler PointerPressed, AddressOf MyScrollBar_PointerPressed
        AddHandler PointerReleased, AddressOf MyScrollBar_PointerReleased
        AddHandler PointerCaptureLost, AddressOf MyScrollBar_PointerCaptureLost
    End Sub

    Protected Overrides Sub OnAttachedToVisualTree(e As VisualTreeAttachmentEventArgs)
        MyBase.OnAttachedToVisualTree(e)
        IsControlLoaded = True
        RefreshColor()
    End Sub

    Protected Overrides Sub OnDetachedFromVisualTree(e As VisualTreeAttachmentEventArgs)
        MyBase.OnDetachedFromVisualTree(e)
        IsControlLoaded = False
        AniStop("MyScrollBar Color " & Uuid)
    End Sub

    Protected Overrides Sub OnPropertyChanged(change As AvaloniaPropertyChangedEventArgs)
        MyBase.OnPropertyChanged(change)
        If change.Property Is IsEnabledProperty OrElse change.Property Is IsVisibleProperty Then
            RefreshColor()
        End If
    End Sub

    Private Sub MyScrollBar_PointerPressed(sender As Object, e As PointerPressedEventArgs)
        IsCaptured = True
        RefreshColor()
    End Sub

    Private Sub MyScrollBar_PointerReleased(sender As Object, e As PointerReleasedEventArgs)
        IsCaptured = False
        RefreshColor()
    End Sub

    Private Sub MyScrollBar_PointerCaptureLost(sender As Object, e As PointerCaptureLostEventArgs)
        IsCaptured = False
        RefreshColor()
    End Sub

    Private Sub RefreshColorHandler(sender As Object, e As PointerEventArgs)
        RefreshColor()
    End Sub

    Private Sub RefreshColor()
        Try
            Dim NewOpacity As Double
            Dim NewColor As String
            Dim Time As Integer
            If Not IsVisible Then
                NewOpacity = 0
                NewColor = "ColorBrush4"
                Time = 20 '防止错误的尺寸判断导致闪烁
            ElseIf IsCaptured Then
                NewOpacity = 1
                NewColor = "ColorBrush4"
                Time = 50
            ElseIf IsPointerOver Then
                NewOpacity = 0.9
                NewColor = "ColorBrush3"
                Time = 130
            Else
                NewOpacity = 0.5
                NewColor = "ColorBrush4"
                Time = 180
            End If
            If IsControlLoaded AndAlso AniControlEnabled = 0 Then
                AniStart({
                    AaColor(Me, ForegroundProperty, NewColor, Time),
                    AaOpacity(Me, NewOpacity - Opacity, Time)
                }, "MyScrollBar Color " & Uuid)
            Else
                AniStop("MyScrollBar Color " & Uuid)
                SetResourceReference(ForegroundProperty, NewColor)
                Opacity = NewOpacity
            End If
        Catch ex As Exception
            Logger.Warn(ex, "滚动条颜色改变出错")
        End Try
    End Sub

End Class
