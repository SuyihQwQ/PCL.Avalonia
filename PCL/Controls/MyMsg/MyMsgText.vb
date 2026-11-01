Imports Avalonia.Controls.Primitives
''' <summary>
''' 普通文本弹窗，显示在 FormMain.PanMsg 内。
''' </summary>
Public Class MyMsgText
    Inherits MyMsgBoxBase

    Public Sub New(Converter As MyMsgBoxConverter)
        MyBase.New(Converter)

        Dim PanCaption As New MyScrollViewer With {
            .Margin = New Thickness(0, 0, 0, 17),
            .Padding = New Thickness(7, 0, 15, 0),
            .HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            .VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            .DeltaMult = 0.7,
            .VerticalAlignment = VerticalAlignment.Top}
        Dim Caption As New TextBlock With {
            .Text = Converter.Text,
            .FontSize = 15,
            .LineHeight = 18,
            .TextWrapping = TextWrapping.Wrap,
            .TextTrimming = TextTrimming.None,
            .Padding = New Thickness(1),
            .VerticalAlignment = VerticalAlignment.Top}
        Caption.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushGray2")
        PanCaption.Content = Caption
        Grid.SetRow(PanCaption, 3)
        PanMain.Children.Add(PanCaption)

        Logger.Info($"普通弹窗：{LabTitle.Text}{vbCrLf}{Converter.Text}")
    End Sub

End Class
