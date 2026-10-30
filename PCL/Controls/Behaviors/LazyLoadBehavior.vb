Imports Avalonia.VisualTree

Public Class LazyLoadBehavior

    ''' <summary>
    ''' 指定首次进入 ScrollViewer 的可视范围时执行的操作。
    ''' </summary>
    Public Shared Sub OnFirstEnterScrollViewerViewport(Obj As AvaloniaObject, Value As Action)
        Obj.SetValue(IsInViewportProperty, Value)
    End Sub

    Private Shared ReadOnly IsInViewportProperty As AttachedProperty(Of Action) =
        AvaloniaProperty.RegisterAttached(Of LazyLoadBehavior, Control, Action)("IsInViewport", Nothing)

    Shared Sub New()
        IsInViewportProperty.Changed.AddClassHandler(Of Control)(AddressOf OnIsInViewportChanged)
    End Sub

    Private Shared Sub OnIsInViewportChanged(Element As Control, e As AvaloniaPropertyChangedEventArgs)
        Dim Value = TryCast(e.NewValue, Action)
        If Element Is Nothing OrElse Value Is Nothing Then Return

        Dim Handled As Boolean = False
        Dim Handler As EventHandler =
        Sub()
            If Handled Then Return
            '判断元素是否可见
            If Element.Bounds.Width < Double.Epsilon Then Return
            If Not Element.IsEffectivelyVisible Then Return
            '判断是否在 ScrollViewer 的可视区域内
            Dim Scroll = Element.FindAncestorOfType(Of ScrollViewer)()
            If Scroll Is Nothing Then Return
            Dim Position = Element.TranslatePoint(New Point(0, 0), Scroll)
            If Position Is Nothing Then Return
            'ScrollViewer.Viewport 在所有情况下均为 0，因此使用其自身尺寸作为视口
            Dim ElementRect As New Rect(Position.Value, Element.Bounds.Size)
            Dim ViewportRect As New Rect(Scroll.Bounds.Size)
            If Not ViewportRect.Intersects(ElementRect) Then Return
            '执行
            Handled = True
            RemoveHandler Element.LayoutUpdated, Handler
            Value.Invoke()
        End Sub
        AddHandler Element.LayoutUpdated, Handler
    End Sub

End Class
