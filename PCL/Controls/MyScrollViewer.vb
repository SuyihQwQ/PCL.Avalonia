Imports Avalonia.Controls.Presenters
Imports Avalonia.Controls.Primitives

Public Class MyScrollViewer
    Inherits ScrollViewer

    Public Property DeltaMult As Double = 1

    ''' <summary>
    ''' 是否始终为可能出现的滚动条预留空间。页面滚动通常需要预留以避免宽度跳动，短列表不需要。
    ''' </summary>
    Public Shared ReadOnly ReserveScrollBarSpaceProperty As StyledProperty(Of Boolean) =
        AvaloniaProperty.Register(Of MyScrollViewer, Boolean)(NameOf(ReserveScrollBarSpace), True)
    Public Property ReserveScrollBarSpace As Boolean
        Get
            Return GetValue(ReserveScrollBarSpaceProperty)
        End Get
        Set(value As Boolean)
            SetValue(ReserveScrollBarSpaceProperty, value)
        End Set
    End Property

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

    Private RealContentPresenter As ScrollContentPresenter
    Private RootGrid As Grid

    Protected Overrides Sub OnApplyTemplate(e As TemplateAppliedEventArgs)
        MyBase.OnApplyTemplate(e)
        RealContentPresenter = e.NameScope.Find(Of ScrollContentPresenter)("PART_ContentPresenter")
        RootGrid = e.NameScope.Find(Of Grid)("PART_Root")
        UpdateScrollCapability()
        UpdateScrollBarGutter()
    End Sub

    ''' <summary>
    ''' 为自动显示的滚动条预留固定宽度，避免内容高度变化时页面宽度来回跳动。
    ''' 未预留时使用 Auto，让短列表不占位、长列表仍能显示滚动条。
    ''' </summary>
    Private Sub UpdateScrollBarGutter()
        If RootGrid Is Nothing OrElse RootGrid.ColumnDefinitions.Count < 2 Then Return
        Dim Width As GridLength
        Select Case VerticalScrollBarVisibility
            Case ScrollBarVisibility.Auto, ScrollBarVisibility.Visible
                Width = If(ReserveScrollBarSpace, New GridLength(10), GridLength.Auto)
            Case Else
                Width = New GridLength(0)
        End Select
        RootGrid.ColumnDefinitions(1).Width = Width
    End Sub

    ''' <summary>
    ''' 自定义模板没有绑定滚动能力标志，需要手动同步，否则内容会按视口高度裁剪而无法滚动。
    ''' </summary>
    Private Sub UpdateScrollCapability()
        If RealContentPresenter Is Nothing Then Return
        RealContentPresenter.CanVerticallyScroll = VerticalScrollBarVisibility <> ScrollBarVisibility.Disabled
        RealContentPresenter.CanHorizontallyScroll = HorizontalScrollBarVisibility <> ScrollBarVisibility.Disabled
    End Sub

    Protected Overrides Function MeasureOverride(availableSize As Size) As Size
        'ScrollViewer 会在模板应用后按自身状态重写这两个标志，测量前再同步一次
        UpdateScrollCapability()
        UpdateScrollBarGutter()
        Return MyBase.MeasureOverride(availableSize)
    End Function

    Protected Overrides Sub OnPropertyChanged(change As AvaloniaPropertyChangedEventArgs)
        MyBase.OnPropertyChanged(change)
        If change.Property.Name = "CanVerticallyScroll" OrElse change.Property.Name = "CanHorizontallyScroll" Then UpdateScrollCapability()
        If change.Property Is VerticalScrollBarVisibilityProperty OrElse change.Property Is ReserveScrollBarSpaceProperty Then UpdateScrollBarGutter()
        If change.Property Is IsVisibleProperty Then RefreshExtraBack()
    End Sub

    Private Shared Sub RefreshExtraBack()
        If FrmMain IsNot Nothing AndAlso FrmMain.BtnExtraBack IsNot Nothing Then FrmMain.BtnExtraBack.ShowRefresh()
    End Sub

End Class
