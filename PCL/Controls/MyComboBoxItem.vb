Public Class MyComboBoxItem
    Inherits ComboBoxItem

    '基础

    Public Uuid As Integer = GetUuid()

    '指向动画

    Private Const AnimationTimeIn As Integer = 100
    Private Const AnimationTimeOut As Integer = 300
    Private BackColorName As String
    Private FontOpacity As Double = 1
    Private IsControlLoaded As Boolean = False

    Public Sub New()
        MyBase.AddHandler(Me.PointerEnteredEvent, Sub(sender, e) RefreshColor())
        MyBase.AddHandler(Me.PointerExitedEvent, Sub(sender, e) RefreshColor())
        MyBase.AddHandler(Me.PointerReleasedEvent,
            Sub(sender, e)
                Logger.Info($"选择下拉列表项：{ToString()}")
            End Sub)
    End Sub

    Protected Overrides Sub OnAttachedToVisualTree(e As VisualTreeAttachmentEventArgs)
        MyBase.OnAttachedToVisualTree(e)
        IsControlLoaded = True
        RefreshColor()
    End Sub

    Protected Overrides Sub OnPropertyChanged(change As AvaloniaPropertyChangedEventArgs)
        MyBase.OnPropertyChanged(change)
        If change.Property Is IsSelectedProperty OrElse change.Property Is IsEnabledProperty Then RefreshColor()
    End Sub

    Private Sub RefreshColor()
        '判断当前颜色
        Dim NewBackColorName As String
        Dim NewFontOpacity As Double
        Dim Time As Integer
        If IsSelected Then
            NewBackColorName = "ColorBrush6"
            NewFontOpacity = 1
            Time = AnimationTimeIn
        ElseIf IsPointerOver Then
            NewBackColorName = "ColorBrush8"
            NewFontOpacity = 1
            Time = AnimationTimeIn
        ElseIf IsEnabled Then
            NewBackColorName = "ColorBrushTransparent"
            NewFontOpacity = 1
            Time = AnimationTimeOut
        Else
            NewBackColorName = "ColorBrushTransparent"
            NewFontOpacity = 0.4
            Time = AnimationTimeOut
        End If
        If BackColorName = NewBackColorName AndAlso FontOpacity = NewFontOpacity Then Return
        BackColorName = NewBackColorName
        FontOpacity = NewFontOpacity
        '触发颜色动画
        If IsControlLoaded AndAlso AniControlEnabled = 0 Then '防止默认属性变更触发动画
            '有动画
            AniStart({
                AaColor(Me, BackgroundProperty, BackColorName, Time),
                AaOpacity(Me, FontOpacity - Opacity, Time)
            }, "ComboBoxItem Color " & Uuid)
        Else
            '无动画
            AniStop("ComboBoxItem Color " & Uuid)
            SetResourceReference(BackgroundProperty, BackColorName)
            Opacity = FontOpacity
        End If
    End Sub

    Public Overrides Function ToString() As String
        Return If(Content?.ToString(), "")
    End Function

    Public Shared Widening Operator CType(Value As MyComboBoxItem) As String
        Return Value.ToString()
    End Operator

End Class
