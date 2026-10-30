Imports Avalonia.Controls.Documents
Imports Avalonia.Controls.Shapes

Public Class MyCheckBox
    Inherits Grid
    Implements ISettingControl

    Public Uuid As Integer = GetUuid()

    ''' <summary>
    ''' 复选框勾选状态改变。
    ''' </summary>
    ''' <param name="user">是否为用户手动改变的勾选状态。</param>
    Public Event Change(sender As Object, user As Boolean)
    Public Event PreviewChange(sender As Object, e As RouteEventArgs)

    Public Shared ReadOnly CheckedProperty As StyledProperty(Of Boolean) =
        AvaloniaProperty.Register(Of MyCheckBox, Boolean)("Checked", False)
    Public Property Checked As Boolean
        Get
            Return GetValue(CheckedProperty)
        End Get
        Set(value As Boolean)
            SetChecked(value, False)
        End Set
    End Property

    Private Const AnimationTimeOfCheck As Integer = 150
    ''' <summary>
    ''' 手动设置 Checked 属性。
    ''' </summary>
    ''' <param name="value">新的 Checked 属性。</param>
    ''' <param name="user">是否由用户引发。</param>
    Public Sub SetChecked(value As Boolean, user As Boolean)
        Try
            If value = Checked Then Return
            If value AndAlso user Then
                Dim e As New RouteEventArgs(user)
                RaiseEvent PreviewChange(Me, e)
                If e.Handled Then
                    MouseDowned = True
                    Checkbox_MouseLeaveAnimation()
                    MouseDowned = False
                    Return
                End If
            End If
            SetValue(CheckedProperty, value)
            If IsControlLoaded Then RaiseEvent Change(Me, user)
            SyncUI()
            RaiseCustomEvent()
        Catch ex As Exception
            Logger.Warn(ex, "设置 Checked 失败")
        End Try
    End Sub

    Private Sub SyncUI()
        If AniControlEnabled = 0 AndAlso IsControlLoaded Then
            AllowMouseDown = False
            If Checked Then
                AniStart({
                      AaScale(ShapeBorder, 12 - ShapeBorder.Width, AnimationTimeOfCheck, , New AniEaseOutFluent, , True),
                      AaScaleTransform(ShapeCheck, 1 - AniScaleX(ShapeCheck), AnimationTimeOfCheck * 2, AnimationTimeOfCheck * 0.7, New AniEaseOutBack(AniEasePower.Weak)),
                      AaScale(ShapeBorder, 6, AnimationTimeOfCheck * 2, AnimationTimeOfCheck * 0.7, New AniEaseOutBack, , True)
                 }, "MyCheckBox Scale " & Uuid)
                AniStart({
                      AaColor(ShapeBorder, Border.BorderBrushProperty, If(IsEnabled, If(IsPointerOver, "ColorBrush3", "ColorBrush2"), "ColorBrushGray5"), AnimationTimeOfCheck)
                 }, "MyCheckBox BorderColor " & Uuid)
                AniStart({
                      AaCode(Sub() AllowMouseDown = True, AnimationTimeOfCheck * 2)
                 }, "MyCheckBox AllowMouseDown " & Uuid)
            Else
                AniStart({
                      AaScale(ShapeBorder, 12 - ShapeBorder.Width, AnimationTimeOfCheck, , New AniEaseOutFluent, , True),
                      AaScaleTransform(ShapeCheck, -AniScaleX(ShapeCheck), AnimationTimeOfCheck * 0.9, , New AniEaseInFluent(AniEasePower.Weak)),
                      AaScale(ShapeBorder, 6, AnimationTimeOfCheck * 2, AnimationTimeOfCheck * 0.7, New AniEaseOutBack, , True)
                 }, "MyCheckBox Scale " & Uuid)
                AniStart({
                      AaColor(ShapeBorder, Border.BorderBrushProperty, If(IsEnabled, If(IsPointerOver, "ColorBrush3", "ColorBrush1"), "ColorBrushGray5"), AnimationTimeOfCheck)
                 }, "MyCheckBox BorderColor " & Uuid)
                AniStart({
                      AaCode(Sub() AllowMouseDown = True, AnimationTimeOfCheck * 2)
                 }, "MyCheckBox AllowMouseDown " & Uuid)
            End If
        Else
            AniStop("MyCheckBox Scale " & Uuid)
            AniStop("MyCheckBox BorderColor " & Uuid)
            AniStop("MyCheckBox AllowMouseDown " & Uuid)
            If Checked Then
                AniSetScale(ShapeCheck, 1)
                ShapeBorder.SetResourceReference(Border.BorderBrushProperty, If(IsEnabled, "ColorBrush2", "ColorBrushGray5"))
            Else
                AniSetScale(ShapeCheck, 0)
                ShapeBorder.SetResourceReference(Border.BorderBrushProperty, If(IsEnabled, "ColorBrush1", "ColorBrushGray5"))
            End If
            AllowMouseDown = True
        End If
    End Sub

    Public ReadOnly Property Inlines As InlineCollection
        Get
            Return LabText.Inlines
        End Get
    End Property

    Public Shared ReadOnly TextProperty As StyledProperty(Of String) =
        AvaloniaProperty.Register(Of MyCheckBox, String)("Text", "")
    Public Property Text As String
        Get
            Return GetValue(TextProperty)
        End Get
        Set(value As String)
            SetValue(TextProperty, value)
        End Set
    End Property

    '点击事件

    Private MouseDowned As Boolean = False
    Private AllowMouseDown As Boolean = True

    Private Sub Checkbox_MouseUp(sender As Object, e As PointerReleasedEventArgs)
        If Not MouseDowned Then Return
        Logger.Info($"按下复选框（{Not Checked}）：{Text}")
        MouseDowned = False
        SetChecked(Not Checked, True)
        AniStart(AaColor(ShapeBorder, Border.BackgroundProperty, "ColorBrushHalfWhite", 100), "MyCheckBox Background " & Uuid)
    End Sub

    Private Sub Checkbox_MouseDown(sender As Object, e As PointerPressedEventArgs)
        If Not AllowMouseDown Then Return
        If Not e.GetCurrentPoint(Me).Properties.IsLeftButtonPressed Then Return
        MouseDowned = True
        Focus()
        AniStart(AaColor(ShapeBorder, Border.BackgroundProperty, "ColorBrushBg1", 100), "MyCheckBox Background " & Uuid)
        If Checked Then
            AniStart({
                     AaScale(ShapeBorder, 16.5 - ShapeBorder.Width, 1000, , New AniEaseOutFluent(AniEasePower.Strong), , True),
                     AaScaleTransform(ShapeCheck, 0.9 - AniScaleX(ShapeCheck), 1000, , New AniEaseOutFluent(AniEasePower.Strong))
                 }, "MyCheckBox Scale " & Uuid)
        Else
            AniStart(AaScale(ShapeBorder, 16.5 - ShapeBorder.Width, 1000, , New AniEaseOutFluent(AniEasePower.Strong), , True), "MyCheckBox Scale " & Uuid)
        End If
    End Sub

    Private Sub Checkbox_MouseLeave()
        If Not MouseDowned Then Return
        MouseDowned = False
        AniStart(AaColor(ShapeBorder, Border.BackgroundProperty, "ColorBrushHalfWhite", 100), "MyCheckBox Background " & Uuid)
        If Checked Then
            AniStart({
                     AaScale(ShapeBorder, 18 - ShapeBorder.Width, 400, , New AniEaseOutFluent(AniEasePower.Strong), , True),
                     AaScaleTransform(ShapeCheck, 1 - AniScaleX(ShapeCheck), 500, , New AniEaseOutFluent(AniEasePower.Strong))
                 }, "MyCheckBox Scale " & Uuid)
        Else
            AniStart(AaScale(ShapeBorder, 18 - ShapeBorder.Width, 400, , New AniEaseOutFluent(AniEasePower.Strong), , True), "MyCheckBox Scale " & Uuid)
        End If
    End Sub

    '指向动画

    Private Const AnimationTimeOfMouseIn As Integer = 100
    Private Const AnimationTimeOfMouseOut As Integer = 200

    Private Sub Checkbox_MouseEnterAnimation()
        AniStart({
                 AaColor(LabText, TextBlock.ForegroundProperty, "ColorBrush3", AnimationTimeOfMouseIn)
         }, "MyCheckBox TextColor " & Uuid)
        AniStart({
                 AaColor(ShapeBorder, Border.BorderBrushProperty, "ColorBrush3", AnimationTimeOfMouseIn)
         }, "MyCheckBox BorderColor " & Uuid)
    End Sub

    Private Sub Checkbox_MouseLeaveAnimation()
        If Not IsEnabled Then Return
        AniStart({
                 AaColor(LabText, TextBlock.ForegroundProperty, "ColorBrush1", AnimationTimeOfMouseOut)
         }, "MyCheckBox TextColor " & Uuid)
        AniStart({
                 AaColor(ShapeBorder, Border.BorderBrushProperty, If(Checked, "ColorBrush2", "ColorBrush1"), AnimationTimeOfMouseOut)
         }, "MyCheckBox BorderColor " & Uuid)
    End Sub

    Private Sub Checkbox_IsEnabledChanged()
        If IsControlLoaded AndAlso AniControlEnabled = 0 Then
            If IsEnabled Then
                Checkbox_MouseLeaveAnimation()
            Else
                AniStart({
                         AaColor(ShapeBorder, Border.BorderBrushProperty, ColorGray5 - New MyColor(ShapeBorder.BorderBrush), AnimationTimeOfMouseOut)
                 }, "MyCheckBox BorderColor " & Uuid)
                AniStart({
                         AaColor(LabText, TextBlock.ForegroundProperty, ColorGray5 - New MyColor(LabText.Foreground), AnimationTimeOfMouseOut)
                 }, "MyCheckBox TextColor " & Uuid)
            End If
        Else
            AniStop("MyCheckBox TextColor " & Uuid)
            AniStop("MyCheckBox BorderColor " & Uuid)
            LabText.SetResourceReference(TextBlock.ForegroundProperty, If(IsEnabled, "ColorBrush1", "ColorBrushGray5"))
            ShapeBorder.SetResourceReference(Border.BorderBrushProperty, If(IsEnabled, If(Checked, "ColorBrush2", "ColorBrush1"), "ColorBrushGray5"))
        End If
    End Sub

#Region "初始化"

    Private IsControlLoaded As Boolean = False

    Public Sub New()
        InitControls()
        LabText.Text = Text
        MyBase.AddHandler(InputElement.PointerPressedEvent, AddressOf Checkbox_MouseDown, RoutingStrategies.Tunnel)
        MyBase.AddHandler(InputElement.PointerReleasedEvent, AddressOf Checkbox_MouseUp, RoutingStrategies.Tunnel)
        MyBase.AddHandler(Me.PointerEnteredEvent, Sub(sender, e) Checkbox_MouseEnterAnimation())
        MyBase.AddHandler(Me.PointerExitedEvent,
            Sub(sender, e)
                Checkbox_MouseLeave()
                Checkbox_MouseLeaveAnimation()
            End Sub)
    End Sub

    Protected Overrides Sub OnAttachedToVisualTree(e As VisualTreeAttachmentEventArgs)
        MyBase.OnAttachedToVisualTree(e)
        IsControlLoaded = True
        SyncUI()
    End Sub

    Protected Overrides Sub OnPropertyChanged(change As AvaloniaPropertyChangedEventArgs)
        MyBase.OnPropertyChanged(change)
        If change.Property Is CheckedProperty Then
            If Not IsControlLoaded Then SyncUI()
        ElseIf change.Property Is TextProperty Then
            LabText.Text = Text
        ElseIf change.Property Is IsEnabledProperty Then
            Checkbox_IsEnabledChanged()
        End If
    End Sub

#End Region

#Region "设置"

    Private Sub RefreshSetting(NewValue As String) Implements ISettingControl.RefreshSetting
        Checked = Boolean.Parse(NewValue)
    End Sub

    Private Function GetCurrentSetting() As String Implements ISettingControl.GetCurrentSetting
        Return Checked.ToString
    End Function

    Private Sub SaveSetting() Handles Me.Change
        SettingService.SaveSetting(Me)
    End Sub

#End Region

End Class
