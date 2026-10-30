Imports Avalonia.Controls.Documents
Imports Avalonia.Controls.Shapes

Public Class MyRadioBox
    Inherits Grid
    Implements IMyRadio, ISettingControl

    Public Uuid As Integer = GetUuid()

    Public Event PreviewCheck(sender As Object, e As RouteEventArgs)
    Public Event PreviewChange(sender As Object, e As RouteEventArgs)
    Public Event Check(sender As Object, e As RouteEventArgs) Implements IMyRadio.Check
    Public Event Changed(sender As Object, e As RouteEventArgs) Implements IMyRadio.Changed

    Public Shared ReadOnly CheckedProperty As StyledProperty(Of Boolean) =
        AvaloniaProperty.Register(Of MyRadioBox, Boolean)("Checked", False)
    Public Property Checked As Boolean
        Get
            Return GetValue(CheckedProperty)
        End Get
        Set(value As Boolean)
            SetChecked(value, False)
        End Set
    End Property

    ''' <summary>
    ''' 手动设置 Checked 属性。
    ''' </summary>
    ''' <param name="value">新的 Checked 属性。</param>
    ''' <param name="user">是否由用户引发。</param>
    Public Sub SetChecked(value As Boolean, user As Boolean)
        Try
            If value AndAlso user Then
                Dim e As New RouteEventArgs(user)
                RaiseEvent PreviewCheck(Me, e)
                If e.Handled Then
                    Radiobox_MouseLeave()
                    Return
                End If
            End If

            Dim IsChanged As Boolean = False
            If IsControlLoaded AndAlso Not value = Checked Then RaiseEvent PreviewChange(Me, New RouteEventArgs(user))
            If value <> Checked Then
                SetValue(CheckedProperty, value)
                IsChanged = True
            End If

            '保证最多只有一个单选框选中
            Dim ParentPanel = TryCast(Parent, Panel)
            If ParentPanel Is Nothing Then
                If IsChanged Then
                    If Checked Then RaiseEvent Check(Me, New RouteEventArgs(user))
                    RaiseEvent Changed(Me, New RouteEventArgs(user))
                    SyncUI()
                End If
                Return
            End If
            Dim RadioboxList = ParentPanel.Children.OfType(Of MyRadioBox)().ToList()
            Dim CheckedCount As Integer = RadioboxList.Where(Function(c) c.Checked).Count
            If CheckedCount > 1 Then
                If Checked Then
                    For Each Control In RadioboxList
                        If Control.Checked AndAlso Not Control.Equals(Me) Then Control.Checked = False
                    Next
                Else
                    Dim FirstChecked = False
                    For Each Control In RadioboxList
                        If Control.Checked Then
                            If FirstChecked Then
                                Control.Checked = False
                            Else
                                FirstChecked = True
                            End If
                        End If
                    Next
                End If
            End If

            If IsChanged Then
                If Checked Then RaiseEvent Check(Me, New RouteEventArgs(user))
                RaiseEvent Changed(Me, New RouteEventArgs(user))
                RaiseCustomEvent()
                SyncUI()
            End If
        Catch ex As Exception
            Logger.Error(ex, "单选框勾选改变错误", LogBehavior.Toast)
        End Try
    End Sub

    Private Sub SyncUI()
        If AniControlEnabled = 0 AndAlso IsControlLoaded Then
            If Checked Then
                If ShapeDot.Opacity < 0.01 Then ShapeDot.Opacity = 1
                AniStart({
                          AaScale(ShapeBorder, 10 - ShapeBorder.Width, AnimationTimeOfCheck, , New AniEaseOutFluent(AniEasePower.Weak), , True),
                          AaScale(ShapeBorder, 8, AnimationTimeOfCheck * 2, AnimationTimeOfCheck * 0.6, New AniEaseOutBack, , True)
                     }, "MyRadioBox Border " & Uuid)
                AniStart({
                          AaScale(ShapeDot, 9 - ShapeDot.Width, AnimationTimeOfCheck * 2.6, , New AniEaseOutBack(AniEasePower.Weak), , True),
                          AaOpacity(ShapeDot, 1 - ShapeDot.Opacity, AnimationTimeOfCheck * 0.5, AnimationTimeOfCheck * 0.6)
                     }, "MyRadioBox Dot " & Uuid)
                AniStart({
                          AaColor(ShapeBorder, Shape.StrokeProperty, If(IsPointerOver, "ColorBrush3", If(IsEnabled, "ColorBrush2", "ColorBrushGray5")), AnimationTimeOfCheck)
                     }, "MyRadioBox BorderColor " & Uuid)
            Else
                AniStart({
                          AaScale(ShapeBorder, 18 - ShapeBorder.Width, AnimationTimeOfCheck, , New AniEaseOutFluent, , True)
                     }, "MyRadioBox Border " & Uuid)
                AniStart({
                          AaScale(ShapeDot, -ShapeDot.Width, AnimationTimeOfCheck, , New AniEaseInFluent, , True),
                          AaOpacity(ShapeDot, -ShapeDot.Opacity, AnimationTimeOfCheck * 0.5, AnimationTimeOfCheck * 0.2)
                     }, "MyRadioBox Dot " & Uuid)
                AniStart({
                          AaColor(ShapeBorder, Shape.StrokeProperty, If(IsPointerOver, "ColorBrush3", If(IsEnabled, "ColorBrush1", "ColorBrushGray5")), AnimationTimeOfCheck)
                     }, "MyRadioBox BorderColor " & Uuid)
            End If
        Else
            AniStop("MyRadioBox Border " & Uuid)
            AniStop("MyRadioBox Dot " & Uuid)
            AniStop("MyRadioBox BorderColor " & Uuid)
            If Checked Then
                ShapeDot.Width = 9
                ShapeDot.Height = 9
                ShapeDot.Opacity = 1
                ShapeDot.Margin = New Thickness(5.5, 0, 0, 0)
                ShapeBorder.SetResourceReference(Shape.StrokeProperty, If(IsEnabled, "ColorBrush2", "ColorBrushGray5"))
            Else
                ShapeDot.Width = 0
                ShapeDot.Height = 0
                ShapeDot.Opacity = 0
                ShapeDot.Margin = New Thickness(10, 0, 0, 0)
                ShapeBorder.SetResourceReference(Shape.StrokeProperty, If(IsEnabled, "ColorBrush1", "ColorBrushGray5"))
            End If
        End If
    End Sub

    Public ReadOnly Property Inlines As InlineCollection
        Get
            Return LabText.Inlines
        End Get
    End Property

    Public Shared ReadOnly TextProperty As StyledProperty(Of String) =
        AvaloniaProperty.Register(Of MyRadioBox, String)("Text", "")
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

    Private Sub Radiobox_MouseUp(sender As Object, e As PointerReleasedEventArgs)
        If Not MouseDowned Then Return
        Logger.Info($"按下单选框：{Text}")
        SetChecked(True, True)
        MouseDowned = False
        AniStart(AaColor(ShapeBorder, Shape.FillProperty, "ColorBrushHalfWhite", 100), "MyRadioBox Background " & Uuid)
    End Sub

    Private Sub Radiobox_MouseDown(sender As Object, e As PointerPressedEventArgs)
        If Not e.GetCurrentPoint(Me).Properties.IsLeftButtonPressed Then Return
        MouseDowned = True
        Focus()
        AniStart(AaColor(ShapeBorder, Shape.FillProperty, "ColorBrushBg1", 100), "MyRadioBox Background " & Uuid)
        If Not Checked Then
            AniStart(AaScale(ShapeBorder, 16.5 - ShapeBorder.Width, 1000, , New AniEaseOutFluent(AniEasePower.Strong), , True), "MyRadioBox Border " & Uuid)
        End If
    End Sub

    Private Sub Radiobox_MouseLeave()
        If Not MouseDowned Then Return
        MouseDowned = False
        AniStart(AaColor(ShapeBorder, Shape.FillProperty, "ColorBrushHalfWhite", 100), "MyRadioBox Background " & Uuid)
        If Not Checked Then
            AniStart(AaScale(ShapeBorder, 18 - ShapeBorder.Width, 400, , New AniEaseOutFluent(AniEasePower.Strong), , True), "MyRadioBox Border " & Uuid)
        End If
    End Sub

    '指向动画

    Private Const AnimationTimeOfMouseIn As Integer = 100
    Private Const AnimationTimeOfMouseOut As Integer = 200
    Private Const AnimationTimeOfCheck As Integer = 150

    Private Sub Radiobox_MouseEnterAnimation()
        AniStart(AaColor(ShapeBorder, Shape.StrokeProperty, "ColorBrush3", AnimationTimeOfMouseIn), "MyRadioBox BorderColor " & Uuid)
        AniStart(AaColor(LabText, TextBlock.ForegroundProperty, "ColorBrush3", AnimationTimeOfMouseIn), "MyRadioBox TextColor " & Uuid)
    End Sub

    Private Sub Radiobox_MouseLeaveAnimation()
        If Not IsEnabled Then Return
        If IsControlLoaded AndAlso AniControlEnabled = 0 Then
            AniStart(AaColor(ShapeBorder, Shape.StrokeProperty, If(Checked, "ColorBrush2", "ColorBrush1"), AnimationTimeOfMouseOut), "MyRadioBox BorderColor " & Uuid)
            AniStart(AaColor(LabText, TextBlock.ForegroundProperty, "ColorBrush1", AnimationTimeOfMouseOut), "MyRadioBox TextColor " & Uuid)
        Else
            AniStop("MyRadioBox BorderColor " & Uuid)
            AniStop("MyRadioBox TextColor " & Uuid)
            ShapeBorder.SetResourceReference(Shape.StrokeProperty, If(Checked, "ColorBrush2", "ColorBrush1"))
            LabText.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrush1")
        End If
    End Sub

    Private Sub Radiobox_IsEnabledChanged()
        If IsControlLoaded AndAlso AniControlEnabled = 0 Then
            If IsEnabled Then
                Radiobox_MouseLeaveAnimation()
            Else
                AniStart(AaColor(ShapeBorder, Shape.StrokeProperty, ColorGray5 - New MyColor(ShapeBorder.Stroke), AnimationTimeOfMouseOut), "MyRadioBox BorderColor " & Uuid)
                AniStart(AaColor(LabText, TextBlock.ForegroundProperty, ColorGray5 - New MyColor(LabText.Foreground), AnimationTimeOfMouseOut), "MyRadioBox TextColor " & Uuid)
            End If
        Else
            AniStop("MyRadioBox BorderColor " & Uuid)
            AniStop("MyRadioBox TextColor " & Uuid)
            LabText.SetResourceReference(TextBlock.ForegroundProperty, If(IsEnabled, "ColorBrush1", "ColorBrushGray5"))
            ShapeBorder.SetResourceReference(Shape.StrokeProperty, If(IsEnabled, If(Checked, "ColorBrush2", "ColorBrush1"), "ColorBrushGray5"))
        End If
    End Sub

#Region "初始化"

    Private IsControlLoaded As Boolean = False

    Public Sub New()
        InitControls()
        LabText.Text = Text
        MyBase.AddHandler(InputElement.PointerPressedEvent, AddressOf Radiobox_MouseDown, RoutingStrategies.Tunnel)
        MyBase.AddHandler(InputElement.PointerReleasedEvent, AddressOf Radiobox_MouseUp, RoutingStrategies.Tunnel)
        MyBase.AddHandler(Me.PointerEnteredEvent, Sub(sender, e) Radiobox_MouseEnterAnimation())
        MyBase.AddHandler(Me.PointerExitedEvent,
            Sub(sender, e)
                Radiobox_MouseLeave()
                Radiobox_MouseLeaveAnimation()
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
            Radiobox_IsEnabledChanged()
        End If
    End Sub

#End Region

#Region "设置"

    Private Sub RefreshSetting(NewValue As String) Implements ISettingControl.RefreshSetting
        Checked = NewValue = SettingService.GetValue(Me)
    End Sub

    Private Function GetCurrentSetting() As String Implements ISettingControl.GetCurrentSetting
        Return If(Checked, SettingService.GetValue(Me), Nothing)
    End Function

    Private Sub SaveSetting() Handles Me.Check
        SettingService.SaveSetting(Me)
    End Sub

#End Region

End Class
