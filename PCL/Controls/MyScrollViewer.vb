Public Class MyScrollViewer
    Inherits ScrollViewer

    Public Property DeltaMult As Double = 1

    Private RealOffset As Double

    Public Sub New()
        AddHandler ScrollChanged, AddressOf MyScrollViewer_ScrollChanged
    End Sub

    Protected Overrides Sub OnPointerWheelChanged(e As PointerWheelEventArgs)
        If e.Delta.Y = 0 OrElse Bounds.Height = 0 OrElse ScrollBarMaximum.Y = 0 Then
            MyBase.OnPointerWheelChanged(e)
            Return
        End If
        If IsInsideScrollableControl(e.Source) Then
            MyBase.OnPointerWheelChanged(e)
            Return
        End If
        e.Handled = True
        PerformVerticalOffsetDelta(-e.Delta.Y * 50)
    End Sub

    Private Shared Function IsInsideScrollableControl(Source As Object) As Boolean
        Dim Ctrl = TryCast(Source, Control)
        If Ctrl Is Nothing Then Return False
        If TypeOf Ctrl Is ComboBox Then Return DirectCast(Ctrl, ComboBox).IsDropDownOpen
        If TypeOf Ctrl Is TextBox Then Return DirectCast(Ctrl, TextBox).AcceptsReturn
        If TypeOf Ctrl Is ComboBoxItem Then Return True
        Return False
    End Function

    Public Sub PerformVerticalOffsetDelta(Delta As Double)
        If ScrollBarMaximum.Y = 0 Then Return
        AniStart(
            AaDouble(
            Sub(AnimDelta As Object)
                RealOffset = Math.Clamp(RealOffset + CDbl(AnimDelta), 0, ScrollBarMaximum.Y)
                Offset = New Vector(Offset.X, RealOffset)
            End Sub, Delta * DeltaMult, 300, , New AniEaseOutFluent(6)))
    End Sub

    Private Sub MyScrollViewer_ScrollChanged(sender As Object, e As ScrollChangedEventArgs)
        RealOffset = Offset.Y
        If e.OffsetDelta.Y <> 0 OrElse e.ViewportDelta.Y <> 0 Then RefreshExtraBack()
    End Sub

    Protected Overrides Sub OnPropertyChanged(change As AvaloniaPropertyChangedEventArgs)
        MyBase.OnPropertyChanged(change)
        If change.Property Is IsVisibleProperty Then RefreshExtraBack()
    End Sub

    Private Shared Sub RefreshExtraBack()
        If FrmMain IsNot Nothing AndAlso FrmMain.BtnExtraBack IsNot Nothing Then FrmMain.BtnExtraBack.ShowRefresh()
    End Sub

End Class
