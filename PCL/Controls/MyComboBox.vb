Imports Avalonia.Controls.Presenters
Imports Avalonia.Controls.Primitives

Public Class MyComboBox
    Inherits ComboBox
    Implements ISettingControl

    Public Event TextChanged(sender As Object, e As EventArgs)

    '基础

    Public Uuid As Integer = GetUuid()
    Private TextBox As MyTextBox
    Private Popup As Popup
    Private ArrowPath As Avalonia.Controls.Shapes.Path
    Private _contentPresenter As ContentPresenter
    Private IsTextChanging As Boolean = False
    Public Property HintText As String = ""

    Protected Overrides Sub OnApplyTemplate(e As TemplateAppliedEventArgs)
        MyBase.OnApplyTemplate(e)
        Popup = e.NameScope.Find(Of Popup)("PART_Popup")
        ArrowPath = e.NameScope.Find(Of Avalonia.Controls.Shapes.Path)("PART_Arrow")
        _contentPresenter = e.NameScope.Find(Of ContentPresenter)("PART_Content")
        If Not IsEditable Then Return
        Try
            TextBox = TryCast(e.NameScope.Find(Of Control)("PART_EditableTextBox"), MyTextBox)
            If TextBox Is Nothing Then Return
            TextBox.AddHandler(InputElement.GotFocusEvent, Sub(sender, args) RefreshColor())
            TextBox.AddHandler(InputElement.LostFocusEvent, Sub(sender, args) RefreshColor())
            AddHandler TextBox.ValidatedTextChanged, AddressOf TextBox_ValidatedTextChanged
            TextBox.Tag = Tag '有时需要用文本框的 Tag 来写入设置
            If String.IsNullOrEmpty(TextBox.Text) Then
                If Not String.IsNullOrEmpty(Text) Then TextBox.Text = Text
            Else
                RaiseEvent TextChanged(Me, EventArgs.Empty)
            End If
            If HintText.Length > 0 Then TextBox.HintText = HintText
        Catch ex As Exception
            Logger.Error(ex, $"初始化可编辑文本框失败（{If(Name, "")}）")
        End Try
    End Sub

    Private Sub TextBox_ValidatedTextChanged(sender As Object, e As EventArgs)
        If IsTextChanging Then Return
        RaiseEvent TextChanged(Me, e)
        '修复编辑文本后仍然选中此前选项的问题
        If Not IsEditable Then Return
        If SelectedItem Is Nothing OrElse Text = SelectedItem.ToString() Then Return
        Dim RawText As String = Text
        Dim RawCaret As Integer = If(TextBox Is Nothing, 0, TextBox.CaretIndex)
        IsTextChanging = True
        SelectedItem = Nothing
        Text = RawText
        If TextBox IsNot Nothing Then TextBox.CaretIndex = RawCaret
        IsTextChanging = False
    End Sub

    '鼠标按下接口

    Private IsMouseDown As Boolean = False

    Public Sub New()
        MyBase.AddHandler(Me.PointerPressedEvent, Sub(sender, e) IsMouseDown = True, RoutingStrategies.Tunnel)
        MyBase.AddHandler(Me.PointerReleasedEvent, Sub(sender, e) IsMouseDown = False)
        MyBase.AddHandler(Me.PointerEnteredEvent, Sub(sender, e) RefreshColor())
        MyBase.AddHandler(Me.PointerExitedEvent,
            Sub(sender, e)
                IsMouseDown = False
                RefreshColor()
            End Sub)
        AddHandler Me.DropDownOpened, Sub(sender, e) DropDownOpenedHandler()
        AddHandler Me.DropDownClosed,
            Sub(sender, e)
                RefreshColor()
                RefreshArrow()
            End Sub
        AddHandler Me.SelectionChanged,
            Sub(sender, e)
                If IsControlLoaded AndAlso AniControlEnabled = 0 Then RaiseCustomEvent()
            End Sub
    End Sub

    Private IsControlLoaded As Boolean = False

    Protected Overrides Sub OnAttachedToVisualTree(e As VisualTreeAttachmentEventArgs)
        MyBase.OnAttachedToVisualTree(e)
        IsControlLoaded = True
        RefreshColor()
    End Sub

    Protected Overrides Sub OnPropertyChanged(change As AvaloniaPropertyChangedEventArgs)
        MyBase.OnPropertyChanged(change)
        If change.Property Is IsEnabledProperty OrElse change.Property Is IsDropDownOpenProperty Then RefreshColor()
    End Sub

    Protected Overrides Sub OnGotFocus(e As FocusChangedEventArgs)
        MyBase.OnGotFocus(e)
        RefreshColor()
    End Sub

    Protected Overrides Sub OnLostFocus(e As FocusChangedEventArgs)
        MyBase.OnLostFocus(e)
        RefreshColor()
    End Sub

    '指向动画

    Private Sub RefreshColor()
        Try
            '判断当前颜色
            Dim ForeColorName As String
            Dim BackColorName As String
            Dim Time As Integer
            If IsEnabled Then
                If IsMouseDown OrElse IsDropDownOpen OrElse (IsEditable AndAlso TextBox IsNot Nothing AndAlso TextBox.IsFocused) Then
                    ForeColorName = "ColorBrush3"
                    BackColorName = "ColorBrush7"
                    Time = 10
                ElseIf IsPointerOver Then
                    ForeColorName = "ColorBrush4"
                    BackColorName = "ColorBrush7"
                    Time = 100
                Else
                    ForeColorName = "ColorBrushBg0"
                    BackColorName = "ColorBrushHalfWhite"
                    Time = 100
                End If
            Else
                ForeColorName = "ColorBrushGray5"
                BackColorName = "ColorBrushGray6"
                Time = 200
            End If
            '触发颜色动画
            If IsControlLoaded AndAlso AniControlEnabled = 0 Then '防止默认属性变更触发动画
                AniStart({
                         AaColor(Me, ForegroundProperty, ForeColorName, Time),
                         AaColor(Me, BackgroundProperty, BackColorName, Time)
                     }, "MyComboBox Color " & Uuid)
            Else
                AniStop("MyComboBox Color " & Uuid)
                SetResourceReference(ForegroundProperty, ForeColorName)
                SetResourceReference(BackgroundProperty, BackColorName)
            End If
        Catch ex As Exception
            Logger.Warn(ex, "下拉框颜色改变出错")
        End Try
    End Sub

    '下拉菜单

    Private Sub RefreshArrow()
        If ArrowPath Is Nothing Then Return
        Dim TargetAngle As Double = If(IsDropDownOpen, 180, 0)
        AniStart(AaRotateTransform(ArrowPath, TargetAngle - AniAngle(ArrowPath), 150, , New AniEaseOutFluent), "MyComboBox Arrow " & Uuid)
    End Sub

    Private Sub DropDownOpenedHandler()
        RefreshColor()
        RefreshArrow()
        '将下拉菜单宽度与透明度与主窗口同步
        Try
            If Popup IsNot Nothing Then
                Popup.MinWidth = Bounds.Width
                Dim Window = TryCast(TopLevel.GetTopLevel(Me), Window)
                If Window IsNot Nothing AndAlso TypeOf Popup.Child Is Control Then
                    DirectCast(Popup.Child, Control).Opacity = Window.Opacity
                End If
            End If
        Catch ex As Exception
            Logger.Error(ex, "设置下拉框透明度失败")
        End Try
    End Sub

    Public ReadOnly Property ContentPresenter As ContentPresenter
        Get
            Return _contentPresenter
        End Get
    End Property

#Region "设置"

    Private Sub RefreshSetting(NewValue As String) Implements ISettingControl.RefreshSetting
        Dim TargetItem = Items.OfType(Of Control)().FirstOrDefault(
            Function(i) CInt(Val(SettingService.GetValue(i))) = CInt(Val(NewValue)))
        If TargetItem IsNot Nothing Then
            SelectedItem = TargetItem
        ElseIf Items.Count > CInt(Val(NewValue)) Then
            SelectedIndex = CInt(Val(NewValue))
        Else
            Logger.Warn($"尝试显示设置 {SettingService.GetKey(Me)}，但没有找到设置值 {NewValue} 的对应项")
            SelectedIndex = -1
            SelectedItem = Nothing
        End If
    End Sub

    Private Function GetCurrentSetting() As String Implements ISettingControl.GetCurrentSetting
        If SelectedItem Is Nothing Then Return Nothing
        If TypeOf SelectedItem Is Control Then
            Return If(SettingService.GetValue(DirectCast(SelectedItem, Control)), SelectedIndex.ToString())
        Else
            Return SelectedIndex.ToString()
        End If
    End Function

    Private Sub SaveSetting() Handles Me.SelectionChanged
        If SelectedItem Is Nothing Then Return
        SettingService.SaveSetting(Me)
    End Sub

#End Region

End Class
