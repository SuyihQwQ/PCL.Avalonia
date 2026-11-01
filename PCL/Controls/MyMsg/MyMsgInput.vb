Imports Avalonia.Controls.Primitives
''' <summary>
''' 输入弹窗，显示在 FormMain.PanMsg 内。
''' </summary>
Public Class MyMsgInput
    Inherits MyMsgBoxBase

    Private ReadOnly TextArea As MyTextBox

    Public Sub New(Converter As MyMsgBoxConverter)
        MyBase.New(Converter)

        Dim Stack As New StackPanel()
        If Not String.IsNullOrEmpty(Converter.Text) Then
            Dim PanText As New MyScrollViewer With {
                .Margin = New Thickness(0, 0, 0, 7),
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
            PanText.Content = Caption
            Stack.Children.Add(PanText)
        End If
        TextArea = New MyTextBox With {
            .Margin = New Thickness(7, 0, 7, 12),
            .MaxLength = 1000,
            .MinWidth = 450,
            .Text = If(Converter.Content, "")}
        TextArea.HintText = Converter.HintText
        For Each Rule As Validate In Converter.ValidateRules
            TextArea.ValidateRules.Add(Rule)
        Next
        AddHandler TextArea.TextChanged, Sub(sender, e)
                                             TextArea.Validate()
                                             Btn1.IsEnabled = TextArea.IsValidated
                                         End Sub
        Stack.Children.Add(TextArea)
        Grid.SetRow(Stack, 3)
        PanMain.Children.Add(Stack)

        Logger.Info($"输入弹窗：{LabTitle.Text}")
    End Sub

    Protected Overrides Sub OnAttachedToVisualTree(e As VisualTreeAttachmentEventArgs)
        MyBase.OnAttachedToVisualTree(e)
        TextArea.Focus()
        TextArea.CaretIndex = If(TextArea.Text, "").Length
    End Sub

    Protected Overrides Function CanConfirm() As Boolean
        TextArea.Validate()
        Return TextArea.IsValidated
    End Function

    Protected Overrides Function GetButton1Result() As Object
        Return TextArea.Text
    End Function

    Protected Overrides Function GetButton2Result() As Object
        Return Nothing
    End Function

End Class
