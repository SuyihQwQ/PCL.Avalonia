Imports Avalonia.Layout

''' <summary>
''' 独立窗口形式的提示弹窗。后续里程碑会替换为窗口内的 PanMsg 遮罩。
''' </summary>
Public Class MyMsgBoxDialog
    Inherits Window

    Public ReadOnly Property Converter As MyMsgBoxConverter

    Private _InputBox As MyTextBox
    Private _SelectionPanel As StackPanel
    Private _SelectedIndex As Integer = -1

    Public Sub New(Converter As MyMsgBoxConverter)
        Me.Converter = Converter

        Title = Converter.Title
        Width = 460
        SizeToContent = SizeToContent.Height
        CanResize = False
        ShowInTaskbar = False
        WindowStartupLocation = WindowStartupLocation.CenterOwner
        Background = New SolidColorBrush(Color.Parse("#F7F9FC"))

        Dim TitleText As New TextBlock With {
            .Text = Converter.Title,
            .FontSize = 17,
            .FontWeight = FontWeight.SemiBold,
            .Foreground = New SolidColorBrush(If(Converter.IsWarn, Color.Parse("#CE2111"), Color.Parse("#343D4A")))
        }
        Dim Stack As New StackPanel With {.Spacing = 18}
        Stack.Children.Add(TitleText)
        Select Case Converter.Type
            Case MyMsgBoxType.Input
                If Not String.IsNullOrEmpty(Converter.Text) Then
                    Stack.Children.Add(New TextBlock With {
                        .Text = Converter.Text,
                        .FontSize = 13,
                        .TextWrapping = TextWrapping.Wrap,
                        .Foreground = New SolidColorBrush(Color.Parse("#343D4A"))
                    })
                End If
                _InputBox = New MyTextBox With {
                    .Text = If(Converter.Content, ""),
                    .MaxLength = 1000,
                    .MinWidth = 400
                }
                _InputBox.HintText = Converter.HintText
                For Each Rule As Validate In Converter.ValidateRules
                    _InputBox.ValidateRules.Add(Rule)
                Next
                Stack.Children.Add(_InputBox)
            Case MyMsgBoxType.Select
                _SelectionPanel = New StackPanel With {.Spacing = 2}
                For Each Content As Object In CType(Converter.Content, IEnumerable)
                    Dim Element As Control = MyVirtualizingElement.TryInit(CType(Content, Control))
                    Dim Selection As IMyRadio = CType(Element, IMyRadio)
                    _SelectionPanel.Children.Add(Element)
                    AddHandler Selection.Check, AddressOf Selection_Checked
                    If TypeOf Element Is MyListItem Then
                        CType(Element, MyListItem).Type = MyListItem.CheckType.RadioBox
                        CType(Element, MyListItem).MinHeight = 24
                    ElseIf TypeOf Element Is MyRadioBox Then
                        CType(Element, MyRadioBox).MinHeight = 24
                    End If
                Next
                Stack.Children.Add(_SelectionPanel)
        End Select

        Dim ButtonPanel As New StackPanel With {
            .Orientation = Orientation.Horizontal,
            .HorizontalAlignment = HorizontalAlignment.Right,
            .Spacing = 10
        }
        AddButton(ButtonPanel, Converter.Button1, 1, Converter.HighLight)
        AddButton(ButtonPanel, Converter.Button2, 2, False)
        AddButton(ButtonPanel, Converter.Button3, 3, False)
        Stack.Children.Add(ButtonPanel)

        Content = New Border With {
            .Padding = New Thickness(26, 22, 26, 22),
            .Child = Stack
        }

        AddHandler Closed, AddressOf OnDialogClosed
        AddHandler Opened, AddressOf OnDialogOpened
    End Sub

    Private Sub OnDialogOpened(sender As Object, e As EventArgs)
        If _InputBox IsNot Nothing Then
            _InputBox.Focus()
            _InputBox.CaretIndex = If(_InputBox.Text, "").Length
        End If
    End Sub

    Private Sub Selection_Checked(sender As Object, e As RouteEventArgs)
        If _SelectionPanel Is Nothing Then Return
        _SelectedIndex = _SelectionPanel.Children.IndexOf(CType(sender, Control))
        If _Button1 IsNot Nothing Then _Button1.IsEnabled = True
    End Sub

    Private _Button1 As Button

    Private Sub AddButton(Panel As StackPanel, Text As String, Index As Integer, HighLight As Boolean)
        If String.IsNullOrEmpty(Text) Then Return
        Dim Button As New Button With {
            .Content = Text,
            .MinWidth = 88,
            .Padding = New Thickness(16, 6, 16, 6),
            .HorizontalContentAlignment = HorizontalAlignment.Center
        }
        If HighLight Then
            Button.Background = New SolidColorBrush(Color.Parse("#1370F3"))
            Button.Foreground = New SolidColorBrush(Colors.White)
        End If
        Dim ButtonIndex As Integer = Index
        AddHandler Button.Click, Sub(sender, e) Respond(ButtonIndex)
        If Index = 1 Then _Button1 = Button
        Panel.Children.Add(Button)
    End Sub

    ''' <summary>
    ''' 以按钮编号响应弹窗。测试与自动化流程也通过该方法触发。
    ''' </summary>
    Public Sub Respond(Index As Integer)
        Dim ButtonAction As Action = Nothing
        Select Case Index
            Case 1 : ButtonAction = Converter.Button1Action
            Case 2 : ButtonAction = Converter.Button2Action
            Case 3 : ButtonAction = Converter.Button3Action
        End Select
        If ButtonAction IsNot Nothing Then
            ButtonAction()
            Return
        End If
        Select Case Converter.Type
            Case MyMsgBoxType.Input
                If Index = 1 Then
                    _InputBox.Validate()
                    If Not _InputBox.IsValidated Then Return
                    Converter.Result = _InputBox.Text
                Else
                    Converter.Result = Nothing
                End If
            Case MyMsgBoxType.Select
                If Index = 1 Then
                    If _SelectedIndex < 0 Then Return
                    Converter.Result = _SelectedIndex
                Else
                    Converter.Result = Nothing
                End If
            Case Else
                Converter.Result = Index
        End Select
        Converter.IsExited = True
        Close()
    End Sub

    Private Sub OnDialogClosed(sender As Object, e As EventArgs)
        If Not Converter.IsExited Then
            Select Case Converter.Type
                Case MyMsgBoxType.Input, MyMsgBoxType.Select
                    Converter.Result = Nothing
                Case Else
                    Converter.Result = 0
            End Select
            Converter.IsExited = True
        End If
        If ActiveMsgBoxWindow Is Me Then ActiveMsgBoxWindow = Nothing
        MyMsgBoxTick()
    End Sub

End Class
