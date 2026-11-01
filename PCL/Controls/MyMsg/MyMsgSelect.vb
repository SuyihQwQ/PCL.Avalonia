Imports Avalonia.Controls.Primitives
''' <summary>
''' 选择弹窗，显示在 FormMain.PanMsg 内。
''' </summary>
Public Class MyMsgSelect
    Inherits MyMsgBoxBase

    Private ReadOnly PanSelection As New StackPanel With {
        .Orientation = Orientation.Vertical}
    Private SelectedIndex As Integer = -1

    Public Sub New(Converter As MyMsgBoxConverter)
        MyBase.New(Converter)

        Dim PanCaption As New MyScrollViewer With {
            .Margin = New Thickness(0, 0, 0, 17),
            .Padding = New Thickness(7, 0, 15, 0),
            .HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            .VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            .DeltaMult = 0.7,
            .VerticalAlignment = VerticalAlignment.Top}
        PanCaption.Content = PanSelection
        Grid.SetRow(PanCaption, 3)
        PanMain.Children.Add(PanCaption)

        Btn1.IsEnabled = False
        For Each Content As Object In CType(Converter.Content, IEnumerable)
            Dim Element As Control = MyVirtualizingElement.TryInit(CType(Content, Control))
            Dim Selection As IMyRadio = CType(Element, IMyRadio)
            PanSelection.Children.Add(Element)
            AddHandler Selection.Check, AddressOf OnChecked
            If TypeOf Element Is MyListItem Then
                CType(Element, MyListItem).Type = MyListItem.CheckType.RadioBox
                CType(Element, MyListItem).MinHeight = 24
            ElseIf TypeOf Element Is MyRadioBox Then
                CType(Element, MyRadioBox).MinHeight = 24
            End If
        Next

        Logger.Info($"选择弹窗：{LabTitle.Text}")
    End Sub

    Private Sub OnChecked(sender As Object, e As RouteEventArgs)
        Btn1.IsEnabled = True
        SelectedIndex = PanSelection.Children.IndexOf(CType(sender, Control))
    End Sub

    Protected Overrides Function CanConfirm() As Boolean
        Return SelectedIndex >= 0
    End Function

    Protected Overrides Function GetButton1Result() As Object
        Return SelectedIndex
    End Function

    Protected Overrides Function GetButton2Result() As Object
        Return Nothing
    End Function

End Class
