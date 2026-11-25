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
    Public Shared ReadOnly HintTextProperty As StyledProperty(Of String) =
        AvaloniaProperty.Register(Of MyComboBox, String)(NameOf(HintText), "")
    Public Property HintText As String
        Get
            Return GetValue(HintTextProperty)
        End Get
        Set(value As String)
            SetValue(HintTextProperty, value)
        End Set
    End Property
    '下拉弹层是否展开。与 IsDropDownOpen 分开，以便收起时先播完淡出动画再真正关闭弹层。
    Public Shared ReadOnly PopupOpenProperty As StyledProperty(Of Boolean) =
        AvaloniaProperty.Register(Of MyComboBox, Boolean)(NameOf(PopupOpen))
    Public Property PopupOpen As Boolean
        Get
            Return GetValue(PopupOpenProperty)
        End Get
        Set(value As Boolean)
            SetValue(PopupOpenProperty, value)
        End Set
    End Property

    Protected Overrides Sub PrepareContainerForItemOverride(container As Control, item As Object, index As Integer)
        MyBase.PrepareContainerForItemOverride(container, item, index)
        'ItemContainerTheme 是给自动生成的包装容器用的，会覆盖 MyComboBoxItem 自身的 Padding 与模板
        If TypeOf container Is MyComboBoxItem Then container.ClearValue(Avalonia.StyledElement.ThemeProperty)
    End Sub

    Protected Overrides Sub OnApplyTemplate(e As TemplateAppliedEventArgs)
        MyBase.OnApplyTemplate(e)
        Popup = e.NameScope.Find(Of Popup)("PART_Popup")
        ArrowPath = e.NameScope.Find(Of Avalonia.Controls.Shapes.Path)("PART_Arrow")
        _contentPresenter = e.NameScope.Find(Of ContentPresenter)("PART_Content")
        If Popup IsNot Nothing Then
            '点外部（轻触关闭）时弹层会自行关闭，需要同步回 IsDropDownOpen
            RemoveHandler Popup.Closed, AddressOf Popup_Closed
            AddHandler Popup.Closed, AddressOf Popup_Closed
        End If
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
    '本次按下是否发生在这个下拉框上，用于关闭下拉菜单后恢复悬停高亮
    Private CloseBySelfClick As Boolean = False

    Public Sub New()
        MyBase.AddHandler(Me.PointerPressedEvent,
            Sub(sender, e)
                IsMouseDown = True
                CloseBySelfClick = True
            End Sub, RoutingStrategies.Tunnel)
        MyBase.AddHandler(Me.PointerReleasedEvent,
            Sub(sender, e)
                IsMouseDown = False
                RefreshColor(ForceHover:=CloseBySelfClick OrElse IsPointerOver)
                CloseBySelfClick = False
            End Sub)
        MyBase.AddHandler(Me.PointerEnteredEvent, Sub(sender, e) RefreshColor())
        MyBase.AddHandler(Me.PointerExitedEvent,
            Sub(sender, e)
                IsMouseDown = False
                RefreshColor()
            End Sub)
        AddHandler Me.DropDownOpened, Sub(sender, e) DropDownOpenedHandler()
        AddHandler Me.DropDownClosed,
            Sub(sender, e)
                OpenDropDowns.Remove(Me)
                MyCard.SetCardHoverLock(Me, Me, False)
                RefreshArrow()
            End Sub
        AddHandler Me.Unloaded, Sub() OpenDropDowns.Remove(Me)
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
        If change.Property Is IsEnabledProperty Then RefreshColor()
        If change.Property Is IsDropDownOpenProperty Then
            Dim OpenNow As Boolean = change.NewValue IsNot Nothing AndAlso CBool(change.NewValue)
            '展开时直接打开弹层；收起时先淡出再真正关闭（对齐原版 PopupAnimation="Fade"）
            SyncPopupOpen(OpenNow)
            '箭头跟随逻辑上的开关立即转动（弹层淡出期间也保持正确方向）
            RefreshArrow()
            If OpenNow Then
                RefreshColor()
            Else
                RefreshColor(ForceHover:=CloseBySelfClick OrElse IsPointerOver)
            End If
        End If
        If change.Property Is HintTextProperty AndAlso TextBox IsNot Nothing Then TextBox.HintText = HintText
    End Sub

    Protected Overrides Sub OnPointerWheelChanged(e As PointerWheelEventArgs)
        '下拉框收起时，滚轮用于滚动页面，不切换选项；展开时交给默认处理
        If Not IsDropDownOpen Then Return
        MyBase.OnPointerWheelChanged(e)
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

    Private Sub RefreshColor(Optional ForceHover As Boolean = False)
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
                ElseIf ForceHover OrElse IsPointerOver Then
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

    ''' <summary>
    ''' 当前展开着下拉菜单的控件，用于在展开期间关闭悬停提示。
    ''' </summary>
    Friend Shared ReadOnly OpenDropDowns As New HashSet(Of MyComboBox)()

    Private Sub SyncPopupOpen(Open As Boolean)
        Dim FadeName As String = "MyComboBox Popup FadeOut " & Uuid
        If Open Then
            AniStop(FadeName)
            PopupOpen = True
            If Popup IsNot Nothing AndAlso TypeOf Popup.Child Is Control Then
                DirectCast(Popup.Child, Control).IsHitTestVisible = True
            End If
            Return
        End If
        '弹层没开着（例如轻触外部已自行关闭）时直接同步状态
        If Popup Is Nothing OrElse Not Popup.IsOpen Then
            AniStop(FadeName)
            If PopupOpen Then PopupOpen = False
            If Popup IsNot Nothing AndAlso TypeOf Popup.Child Is Control Then
                DirectCast(Popup.Child, Control).IsHitTestVisible = True
            End If
            Return
        End If
        If AniControlEnabled <> 0 OrElse TypeOf Popup.Child IsNot Control Then
            PopupOpen = False
            Return
        End If
        Dim Child = DirectCast(Popup.Child, Control)
        '淡出期间不再响应点击
        Child.IsHitTestVisible = False
        AniStop(FadeName)
        AniStart({
            AaOpacity(Child, -Child.Opacity, 150, , New AniEaseOutFluent),
            AaCode(
            Sub()
                PopupOpen = False
                Child.Opacity = 1
                Child.IsHitTestVisible = True
            End Sub, , True)
        }, FadeName)
    End Sub

    Private Sub Popup_Closed(sender As Object, e As EventArgs)
        AniStop("MyComboBox Popup FadeOut " & Uuid)
        If PopupOpen Then PopupOpen = False
        If IsDropDownOpen Then IsDropDownOpen = False
        If Popup IsNot Nothing AndAlso TypeOf Popup.Child Is Control Then
            Dim Child = DirectCast(Popup.Child, Control)
            Child.Opacity = 1
            Child.IsHitTestVisible = True
        End If
    End Sub

    Private Sub DropDownOpenedHandler()
        OpenDropDowns.Add(Me)
        '与原版一致：打开下拉菜单时关闭现有的悬停提示
        FrmMain?.HideHoverTip()
        MyCard.SetCardHoverLock(Me, Me, True)
        RefreshColor()
        RefreshArrow()
        '将下拉菜单宽度与透明度与主窗口同步
        '与原版 PCL 的 PopupAnimation="Fade" 一致：下拉菜单从透明淡入
        Try
            If Popup IsNot Nothing Then
                Popup.MinWidth = Bounds.Width
                If TypeOf Popup.Child Is Control Then
                    Dim Child = DirectCast(Popup.Child, Control)
                    Dim TargetOpacity As Double = 1
                    Dim Window = TryCast(TopLevel.GetTopLevel(Me), Window)
                    If Window IsNot Nothing Then TargetOpacity = Window.Opacity
                    If AniControlEnabled = 0 Then
                        Child.Opacity = 0
                        AniStart(AaOpacity(Child, TargetOpacity, 150, , New AniEaseOutFluent), "MyComboBox Popup " & Uuid)
                    Else
                        Child.Opacity = TargetOpacity
                    End If
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
