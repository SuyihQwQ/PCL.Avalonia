Imports Avalonia.Controls.Shapes

''' <summary>
''' 窗口内弹窗的公共结构：标题、分隔线、按钮，以及进入与退出动画。
''' 继承该类的控件会被放入 FormMain.PanMsg 遮罩中显示，而不是新建窗口。
''' </summary>
Public MustInherit Class MyMsgBoxBase
    Inherits Grid

    Public ReadOnly Property Converter As MyMsgBoxConverter
    Public ReadOnly Property Uuid As Integer = GetUuid()

    Protected ReadOnly PanBorder As Border
    Protected ReadOnly PanMain As Grid
    Protected ReadOnly LabTitle As TextBlock
    Protected ReadOnly ShapeLine As Rectangle
    Protected ReadOnly PanBtn As StackPanel
    Public ReadOnly Btn1 As MyButton
    Public ReadOnly Btn2 As MyButton
    Public ReadOnly Btn3 As MyButton

    Private ReadOnly TransformRotate As New RotateTransform(-4)
    Private ReadOnly TransformPos As New TranslateTransform(0, 40)
    Private IsShown As Boolean

    Public Sub New(MsgConverter As MyMsgBoxConverter)
        Converter = MsgConverter
        MinWidth = 400
        HorizontalAlignment = HorizontalAlignment.Center
        VerticalAlignment = VerticalAlignment.Center
        Margin = New Thickness(25)
        RenderTransformOrigin = New RelativePoint(0, 0.5, RelativeUnit.Relative)
        Dim Group As New TransformGroup()
        Group.Children.Add(TransformRotate)
        Group.Children.Add(TransformPos)
        RenderTransform = Group

        '阴影由外侧绘制，必须与本体重合，否则中间会空出一圈半透明边框
        Dim Shadow As New MyDropShadow With {
            .Margin = New Thickness(0),
            .CornerRadius = New CornerRadius(7),
            .ShadowRadius = 20,
            .Opacity = 0.35}

        PanBorder = New Border With {
            .CornerRadius = New CornerRadius(7)}
        PanBorder.SetResourceReference(Border.BackgroundProperty, "ColorBrushCard")
        PanMain = New Grid With {
            .Margin = New Thickness(22, 22, 22, 23),
            .VerticalAlignment = VerticalAlignment.Top,
            .RowDefinitions = RowDefinitions.Parse("Auto,2,13,*,Auto")}
        PanBorder.Child = PanMain

        LabTitle = New TextBlock With {
            .FontSize = 23,
            .HorizontalAlignment = HorizontalAlignment.Left,
            .VerticalAlignment = VerticalAlignment.Top,
            .Margin = New Thickness(7, -1, 70, 9),
            .Text = Converter.Title}
        Grid.SetRow(LabTitle, 0)
        PanMain.Children.Add(LabTitle)

        ShapeLine = New Rectangle With {.Height = 2}
        Grid.SetRow(ShapeLine, 1)
        PanMain.Children.Add(ShapeLine)
        If Converter.IsWarn Then
            LabTitle.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushRedLight")
            ShapeLine.SetResourceReference(Shape.FillProperty, "ColorBrushRedLight")
        Else
            LabTitle.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrush2")
            ShapeLine.SetResourceReference(Shape.FillProperty, "ColorBrush2")
        End If

        PanBtn = New StackPanel With {
            .Orientation = Orientation.Horizontal,
            .HorizontalAlignment = HorizontalAlignment.Right,
            .VerticalAlignment = VerticalAlignment.Top,
            .Margin = New Thickness(150, 0, 8, 0)}
        Grid.SetRow(PanBtn, 4)
        PanMain.Children.Add(PanBtn)

        Btn1 = CreateButton(Converter.Button1)
        Btn2 = CreateButton(Converter.Button2)
        Btn3 = CreateButton(Converter.Button3)
        If Converter.IsWarn Then Btn1.ColorType = MyButton.ColorState.Red
        PanBtn.Children.Add(Btn1)
        PanBtn.Children.Add(Btn2)
        PanBtn.Children.Add(Btn3)

        AddHandler Btn1.Click, Sub(sender, e) Btn1_Click()
        AddHandler Btn2.Click, Sub(sender, e) Btn2_Click()
        AddHandler Btn3.Click, Sub(sender, e) Btn3_Click()
        AddHandler PointerPressed, AddressOf Drag

        Children.Add(Shadow)
        Children.Add(PanBorder)
    End Sub

    Private Function CreateButton(Text As String) As MyButton
        Return New MyButton With {
            .Text = Text,
            .Height = 26,
            .Margin = New Thickness(12, 0, 0, 0),
            .Padding = New Thickness(12, 0),
            .IsVisible = Not String.IsNullOrEmpty(Text)}
    End Function

    ''' <summary>
    ''' 有多个按钮时高亮第一个按钮。
    ''' </summary>
    Protected Overridable Sub RefreshHighlight()
        If Btn2.IsVisible AndAlso Btn1.ColorType <> MyButton.ColorState.Red Then Btn1.ColorType = MyButton.ColorState.Highlight
    End Sub

    Protected Overrides Sub OnAttachedToVisualTree(e As VisualTreeAttachmentEventArgs)
        MyBase.OnAttachedToVisualTree(e)
        If IsShown Then Return
        IsShown = True
        RefreshHighlight()
        If FrmMain Is Nothing OrElse FrmMain.PanMsg Is Nothing Then Return
        AniStart(AaColor(FrmMain.PanMsg, Panel.BackgroundProperty,
            If(Converter.IsWarn, New MyColor(140, 80, 0, 0), New MyColor(90, 0, 0, 0)) - New MyColor(FrmMain.PanMsg.Background), 200), "PanMsg Background")
        Opacity = 0
        AniStart({
            AaOpacity(Me, 1, 120, 60),
            AaDouble(Sub(i) TransformPos.Y += i, -TransformPos.Y, 300, 60, New AniEaseOutBack(AniEasePower.Weak)),
            AaDouble(Sub(i) TransformRotate.Angle += i, -TransformRotate.Angle, 300, 60, New AniEaseOutFluent(AniEasePower.Weak))
        }, "MyMsgBox " & Uuid)
    End Sub

    Protected Overridable Function CanConfirm() As Boolean
        Return True
    End Function

    Protected Overridable Function GetButton1Result() As Object
        Return 1
    End Function

    Protected Overridable Function GetButton2Result() As Object
        Return 2
    End Function

    Public Overridable Sub Btn1_Click()
        If Converter.IsExited Then Return
        If Converter.Button1Action IsNot Nothing Then
            Converter.Button1Action()
            Return
        End If
        If Not CanConfirm() Then Return
        Converter.Result = GetButton1Result()
        Converter.IsExited = True
        Close()
    End Sub

    Public Overridable Sub Btn2_Click()
        If Converter.IsExited Then Return
        If Converter.Button2Action IsNot Nothing Then
            Converter.Button2Action()
            Return
        End If
        Converter.Result = GetButton2Result()
        Converter.IsExited = True
        Close()
    End Sub

    Public Overridable Sub Btn3_Click()
        If Converter.IsExited Then Return
        If Converter.Button3Action IsNot Nothing Then
            Converter.Button3Action()
            Return
        End If
        Converter.Result = 3
        Converter.IsExited = True
        Close()
    End Sub

    ''' <summary>
    ''' 以按钮编号响应弹窗。测试与自动化流程也通过该方法触发。
    ''' </summary>
    Public Sub Respond(Index As Integer)
        Select Case Index
            Case 1
                Btn1_Click()
            Case 2
                Btn2_Click()
            Case 3
                Btn3_Click()
        End Select
    End Sub

    ''' <summary>
    ''' 播放退出动画并从 PanMsg 中移除。
    ''' </summary>
    Public Sub Close()
        If FrmMain Is Nothing OrElse FrmMain.PanMsg Is Nothing Then
            RemoveSelf()
            Return
        End If
        AniStart({
            AaCode(
            Sub()
                If Not WaitingMyMsgBox.Any Then
                    AniStart(AaColor(FrmMain.PanMsg, Panel.BackgroundProperty, New MyColor(0, 0, 0, 0) - New MyColor(FrmMain.PanMsg.Background), 200, Ease:=New AniEaseOutFluent(AniEasePower.Weak)), "PanMsg Background")
                End If
            End Sub, 30),
            AaOpacity(Me, -Opacity, 80, 20),
            AaDouble(Sub(i) TransformPos.Y += i, 20 - TransformPos.Y, 150, 0, New AniEaseOutFluent),
            AaDouble(Sub(i) TransformRotate.Angle += i, 6 - TransformRotate.Angle, 150, 0, New AniEaseInFluent(AniEasePower.Weak)),
            AaCode(AddressOf RemoveSelf, 180)
        }, "MyMsgBox " & Uuid)
    End Sub

    Private Sub RemoveSelf()
        Dim ParentPanel = TryCast(Parent, Panel)
        ParentPanel?.Children.Remove(Me)
        If FrmMain IsNot Nothing Then
            If ActiveMsgBoxControl Is Me Then ActiveMsgBoxControl = Nothing
            If FrmMain.PanMsg IsNot Nothing Then
                If WaitingMyMsgBox.Any Then
                    MyMsgBoxTick()
                Else
                    '等待遮罩淡出后再隐藏
                    AniStart(AaCode(
                    Sub()
                        If Not WaitingMyMsgBox.Any AndAlso FrmMain IsNot Nothing AndAlso FrmMain.PanMsg IsNot Nothing AndAlso FrmMain.PanMsg.Children.Count = 0 Then
                            FrmMain.PanMsg.IsVisible = False
                        End If
                    End Sub, 220), "PanMsg Hide " & Uuid)
                End If
            End If
        End If
    End Sub

    Private Sub Drag(sender As Object, e As PointerPressedEventArgs)
        If Not e.GetCurrentPoint(Me).Properties.IsLeftButtonPressed Then Return
        If FrmMain Is Nothing Then Return
        If e.GetPosition(ShapeLine).Y <= 2 Then FrmMain.BeginMoveDrag(e)
    End Sub

End Class
