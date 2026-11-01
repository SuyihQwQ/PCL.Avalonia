Imports Avalonia.Controls.Documents
Imports Avalonia.Controls.Shapes

Public Class MyRadioButton
    Inherits Border

    '基础

    Public Uuid As Integer = GetUuid()
    Public Event Check(sender As Object, raiseByMouse As Boolean)

    '自定义属性

    Private _Logo As String = ""
    Public Property Logo As String
        Get
            Return _Logo
        End Get
        Set(value As String)
            _Logo = If(value, "")
            If ShapeLogo IsNot Nothing AndAlso Not String.IsNullOrEmpty(_Logo) Then ShapeLogo.Data = Geometry.Parse(_Logo)
        End Set
    End Property

    Private _LogoScale As Double = 1
    Public Property LogoScale As Double
        Get
            Return _LogoScale
        End Get
        Set(value As Double)
            _LogoScale = value
            If ShapeLogo IsNot Nothing Then ShapeLogo.RenderTransform = New ScaleTransform With {.ScaleX = LogoScale, .ScaleY = LogoScale}
        End Set
    End Property

    Private _Checked As Boolean = False
    Public Property Checked As Boolean
        Get
            Return _Checked
        End Get
        Set(value As Boolean)
            SetChecked(value, False, True)
        End Set
    End Property

    ''' <summary>
    ''' 手动设置 Checked 属性。
    ''' </summary>
    ''' <param name="value">新的 Checked 属性。</param>
    ''' <param name="raiseByMouse">是否由用户引发。</param>
    ''' <param name="anime">是否执行动画。</param>
    Public Sub SetChecked(value As Boolean, raiseByMouse As Boolean, anime As Boolean)
        Try

            '自定义属性基础

            Dim IsChanged As Boolean = False
            If _Checked <> value Then
                _Checked = value
                IsChanged = True
            End If

            '保证只有一个单选框选中

            Dim ParentPanel = TryCast(Parent, Panel)
            If ParentPanel Is Nothing Then Return
            Dim RadioboxList As New List(Of MyRadioButton)
            Dim CheckedCount As Integer = 0
            For Each Control In ParentPanel.Children
                If TypeOf Control Is MyRadioButton Then
                    RadioboxList.Add(Control)
                    If CType(Control, MyRadioButton).Checked Then CheckedCount += 1
                End If
            Next
            Select Case CheckedCount
                Case 0
                    RadioboxList(0).Checked = True
                Case Is > 1
                    If Checked Then
                        For Each Control As MyRadioButton In RadioboxList
                            If Control.Checked AndAlso Not Control.Equals(Me) Then Control.Checked = False
                        Next
                    Else
                        Dim FirstChecked = False
                        For Each Control As MyRadioButton In RadioboxList
                            If Control.Checked Then
                                If FirstChecked Then
                                    Control.Checked = False
                                Else
                                    FirstChecked = True
                                End If
                            End If
                        Next
                    End If
            End Select

            '更改动画

            If Not IsChanged Then Return
            RefreshColor(anime)

            '触发事件
            If Checked Then RaiseEvent Check(Me, raiseByMouse)
            RaiseCustomEvent()

        Catch ex As Exception
            Logger.Error(ex, "单选按钮勾选改变错误", LogBehavior.Toast)
        End Try
    End Sub

    Public ReadOnly Property Inlines As InlineCollection
        Get
            Return LabText.Inlines
        End Get
    End Property

    Public Shared ReadOnly TextProperty As StyledProperty(Of String) =
        AvaloniaProperty.Register(Of MyRadioButton, String)("Text", "")
    Public Property Text As String
        Get
            Return GetValue(TextProperty)
        End Get
        Set(value As String)
            SetValue(TextProperty, value)
        End Set
    End Property

    Public Enum ColorState
        White
        Highlight
    End Enum
    Private _ColorType As ColorState = ColorState.White
    Public Property ColorType As ColorState
        Get
            Return _ColorType
        End Get
        Set(value As ColorState)
            _ColorType = value
            RefreshColor()
        End Set
    End Property

    '点击事件

    Public Event PreviewClick(sender As Object, e As RouteEventArgs)
    Private IsMouseDown As Boolean = False

    Private Sub Radiobox_MouseUp(sender As Object, e As PointerReleasedEventArgs)
        If Checked Then Return
        If Not IsMouseDown Then Return
        Logger.Info($"按下单选按钮：{Text}")
        IsMouseDown = False
        '提前释放隐式指针捕获，避免 Check 回调中打开的模态弹窗被本控件持续捕获指针
        e.Pointer.Capture(Nothing)
        Dim ePreview As New RouteEventArgs(True)
        RaiseEvent PreviewClick(Me, ePreview)
        If ePreview.Handled Then Return
        SetChecked(True, True, True)
    End Sub

    Private Sub Radiobox_MouseDown(sender As Object, e As PointerPressedEventArgs)
        If Not e.GetCurrentPoint(Me).Properties.IsLeftButtonPressed Then Return
        If Checked Then Return
        IsMouseDown = True
        RefreshColor()
    End Sub

    Private Sub Radiobox_MouseLeave()
        IsMouseDown = False
    End Sub

    '动画

    Private Const AnimationTimeOfMouseIn As Integer = 90
    Private Const AnimationTimeOfMouseOut As Integer = 150
    Private Const AnimationTimeOfCheck As Integer = 120

    Public Sub RefreshColor(Optional anime As Boolean = True)
        Try
            If IsControlLoaded AndAlso AniControlEnabled = 0 AndAlso anime Then
                Select Case ColorType
                    Case ColorState.White
                        If Checked Then
                            '勾选
                            AniStart({
                                         AaColor(ShapeLogo, Shape.FillProperty, "ColorBrush3", AnimationTimeOfCheck),
                                         AaColor(LabText, TextBlock.ForegroundProperty, "ColorBrush3", AnimationTimeOfCheck)
                                    }, "MyRadioButton Checked " & Uuid)
                            AniStart(AaColor(Me, BackgroundProperty, New MyColor(255, 255, 255) - New MyColor(Background), AnimationTimeOfCheck), "MyRadioButton Color " & Uuid)
                        ElseIf IsMouseDown Then
                            '按下
                            AniStart(AaColor(Me, BackgroundProperty, New MyColor(120, Color8) - New MyColor(Background), 60), "MyRadioButton Color " & Uuid)
                        ElseIf IsPointerOver Then
                            '指向
                            AniStart({
                                         AaColor(ShapeLogo, Shape.FillProperty, New MyColor(255, 255, 255) - New MyColor(ShapeLogo.Fill), AnimationTimeOfMouseIn),
                                         AaColor(LabText, TextBlock.ForegroundProperty, New MyColor(255, 255, 255) - New MyColor(LabText.Foreground), AnimationTimeOfMouseIn)
                                    }, "MyRadioButton Checked " & Uuid)
                            AniStart(AaColor(Me, BackgroundProperty, New MyColor(50, Color8) - New MyColor(Background), AnimationTimeOfMouseIn), "MyRadioButton Color " & Uuid)
                        Else
                            '正常
                            AniStart({
                                         AaColor(ShapeLogo, Shape.FillProperty, New MyColor(255, 255, 255) - New MyColor(ShapeLogo.Fill), AnimationTimeOfMouseOut),
                                         AaColor(LabText, TextBlock.ForegroundProperty, New MyColor(255, 255, 255) - New MyColor(LabText.Foreground), AnimationTimeOfMouseOut)
                                    }, "MyRadioButton Checked " & Uuid)
                            AniStart(AaColor(Me, BackgroundProperty, ColorSemiTransparent - New MyColor(Background), AnimationTimeOfMouseOut), "MyRadioButton Color " & Uuid)
                        End If
                    Case ColorState.Highlight
                        If Checked Then
                            '勾选
                            AniStart({
                                         AaColor(ShapeLogo, Shape.FillProperty, New MyColor(255, 255, 255) - New MyColor(ShapeLogo.Fill), AnimationTimeOfCheck),
                                         AaColor(LabText, TextBlock.ForegroundProperty, New MyColor(255, 255, 255) - New MyColor(LabText.Foreground), AnimationTimeOfCheck)
                                    }, "MyRadioButton Checked " & Uuid)
                            AniStart(AaColor(Me, BackgroundProperty, "ColorBrush3", AnimationTimeOfCheck), "MyRadioButton Color " & Uuid)
                        ElseIf IsMouseDown Then
                            '按下
                            AniStart(AaColor(Me, BackgroundProperty, "ColorBrush6", AnimationTimeOfMouseIn), "MyRadioButton Color " & Uuid)
                        ElseIf IsPointerOver Then
                            '指向
                            AniStart({
                                         AaColor(ShapeLogo, Shape.FillProperty, "ColorBrush3", AnimationTimeOfMouseIn),
                                         AaColor(LabText, TextBlock.ForegroundProperty, "ColorBrush3", AnimationTimeOfMouseIn)
                                    }, "MyRadioButton Checked " & Uuid)
                            AniStart(AaColor(Me, BackgroundProperty, "ColorBrush7", AnimationTimeOfMouseIn), "MyRadioButton Color " & Uuid)
                        Else
                            '正常
                            AniStart({
                                         AaColor(ShapeLogo, Shape.FillProperty, "ColorBrush3", AnimationTimeOfMouseOut),
                                         AaColor(LabText, TextBlock.ForegroundProperty, "ColorBrush3", AnimationTimeOfMouseOut)
                                    }, "MyRadioButton Checked " & Uuid)
                            AniStart(AaColor(Me, BackgroundProperty, ColorSemiTransparent - New MyColor(Background), AnimationTimeOfMouseOut), "MyRadioButton Color " & Uuid)
                        End If
                End Select

            Else

                '不使用动画
                AniStop("MyRadioButton Checked " & Uuid)
                AniStop("MyRadioButton Color " & Uuid)
                Select Case ColorType
                    Case ColorState.White
                        If Checked Then
                            Background = CType(New MyColor(255, 255, 255), Brush)
                            ShapeLogo.SetResourceReference(Shape.FillProperty, "ColorBrush3")
                            LabText.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrush3")
                        Else
                            Background = CType(ColorSemiTransparent, Brush)
                            ShapeLogo.Fill = CType(New MyColor(255, 255, 255), Brush)
                            LabText.Foreground = CType(New MyColor(255, 255, 255), Brush)
                        End If
                    Case ColorState.Highlight
                        If Checked Then
                            SetResourceReference(BackgroundProperty, "ColorBrush3")
                            ShapeLogo.Fill = CType(New MyColor(255, 255, 255), Brush)
                            LabText.Foreground = CType(New MyColor(255, 255, 255), Brush)
                        Else
                            Background = CType(ColorSemiTransparent, Brush)
                            ShapeLogo.SetResourceReference(Shape.FillProperty, "ColorBrush3")
                            LabText.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrush3")
                        End If
                End Select

            End If
        Catch ex As Exception
            Logger.Warn(ex, "刷新单选按钮颜色出错")
        End Try
    End Sub

    '初始化

    Private IsControlLoaded As Boolean = False

    Public Sub New()
        InitControls()
        LabText.Text = If(Text, "")
        MyBase.AddHandler(InputElement.PointerPressedEvent, AddressOf Radiobox_MouseDown, RoutingStrategies.Tunnel)
        MyBase.AddHandler(InputElement.PointerReleasedEvent, AddressOf Radiobox_MouseUp, RoutingStrategies.Tunnel)
    End Sub

    Protected Overrides Sub OnAttachedToVisualTree(e As VisualTreeAttachmentEventArgs)
        MyBase.OnAttachedToVisualTree(e)
        IsControlLoaded = True
        RefreshColor()
    End Sub

    Protected Overrides Sub OnDetachedFromVisualTree(e As VisualTreeAttachmentEventArgs)
        MyBase.OnDetachedFromVisualTree(e)
        IsControlLoaded = False
        AniStop("MyRadioButton Checked " & Uuid)
        AniStop("MyRadioButton Color " & Uuid)
    End Sub

    Protected Overrides Sub OnPropertyChanged(change As AvaloniaPropertyChangedEventArgs)
        MyBase.OnPropertyChanged(change)
        If change.Property Is TextProperty Then
            If LabText IsNot Nothing Then LabText.Text = If(Text, "")
        End If
    End Sub

    Protected Overrides Sub OnPointerEntered(e As PointerEventArgs)
        MyBase.OnPointerEntered(e)
        RefreshColor()
    End Sub

    Protected Overrides Sub OnPointerExited(e As PointerEventArgs)
        MyBase.OnPointerExited(e)
        Radiobox_MouseLeave()
        RefreshColor()
    End Sub

End Class
