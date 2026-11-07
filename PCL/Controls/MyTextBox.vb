Imports System.Collections.ObjectModel
Imports Avalonia.Controls.Documents
Imports Avalonia.Controls.Primitives

Public Class MyTextBox
    Inherits TextBox
    Implements ISettingControl

    '自定义属性

    Public Property HasBackground As Boolean = True
    Public Property ShowValidateResult As Boolean = True

    '额外控件

    Private _labWrong As TextBlock = Nothing
    Private ReadOnly Property labWrong As TextBlock
        Get
            Return _labWrong
        End Get
    End Property

    '事件

    Public Uuid As Integer = GetUuid()
    Public Shared Event ValidateChanged(sender As Object, e As EventArgs)
    Public Event ValidatedTextChanged(sender As Object, e As TextChangedEventArgs)

    '输入验证

    Public Shared ReadOnly ValidateResultProperty As StyledProperty(Of String) =
        AvaloniaProperty.Register(Of MyTextBox, String)("ValidateResult", "")
    ''' <summary>
    ''' 输入验证结果。若为空字符串则无错误，否则为第一个错误原因。
    ''' </summary>
    Public Property ValidateResult As String
        Get
            Return GetValue(ValidateResultProperty)
        End Get
        Set(value As String)
            SetValue(ValidateResultProperty, value)
        End Set
    End Property

    Private _IsValidated As Boolean = True
    ''' <summary>
    ''' 是否通过了输入验证。
    ''' </summary>
    Public ReadOnly Property IsValidated As Boolean
        Get
            Return _IsValidated
        End Get
    End Property

    ''' <summary>
    ''' 输入验证的规则。
    ''' </summary>
    Public ReadOnly Property ValidateRules As New Collection(Of Validate)

    ''' <summary>
    ''' 进行输入验证。
    ''' </summary>
    Public Sub Validate()
        ValidateResult = ModValidate.Validate(Text, ValidateRules)
        If ShownValidateResult <> If(IsValidated, ValidateState.Success, ValidateState.FailedAndShowDetail) Then
            If IsControlLoaded AndAlso labWrong IsNot Nothing Then
                ChangeValidateResult(IsValidated, True)
            Else
                RunInNewThread(
                Sub()
                    Thread.Sleep(30)
                    RunInUi(Sub() ChangeValidateResult(IsValidated, False))
                End Sub, "DelayedValidate Change")
            End If
        End If
        If ShowValidateResult AndAlso Not IsValidated Then
            If IsControlLoaded AndAlso labWrong IsNot Nothing Then
                labWrong.Text = ValidateResult
            Else
                RunInNewThread(
                Sub()
                    Dim IsFinished As Boolean = False
                    Do Until IsFinished
                        Thread.Sleep(20)
                        RunInUiWait(
                        Sub()
                            If labWrong IsNot Nothing Then
                                labWrong.Text = ValidateResult
                                IsFinished = True
                            End If
                            If Not IsControlLoaded Then IsFinished = True
                        End Sub)
                    Loop
                End Sub, "DelayedValidate Text")
            End If
        End If
    End Sub

    ''' <summary>
    ''' 强制显示结果为正常，类似尚未输入过文本的状态。不影响实际的检查结果。
    ''' </summary>
    Public Sub ForceShowAsSuccess()
        IsTextChanged = False
        ChangeValidateResult(IsValidated, True)
    End Sub

    Private ShownValidateResult As ValidateState = ValidateState.NotInited

    Private Sub ChangeValidateResult(IsSuccessful As Boolean, LoadedNow As Boolean)
        If LoadedNow AndAlso AniControlEnabled = 0 AndAlso labWrong IsNot Nothing Then
            If IsSuccessful OrElse Not IsTextChanged Then
                ShownValidateResult = If(IsSuccessful, ValidateState.Success, ValidateState.FailedButTextNotChanged)
                AniStart({
                     AaOpacity(labWrong, -labWrong.Opacity, 150),
                     AaHeight(labWrong, -labWrong.Height, 150, , New AniEaseOutFluent),
                     AaCode(Sub() labWrong.IsVisible = False, , True)
                }, "MyTextBox Validate " & Uuid)
            ElseIf ShowValidateResult Then
                ShownValidateResult = ValidateState.FailedAndShowDetail
                labWrong.IsVisible = True
                AniStart({
                     AaOpacity(labWrong, 1 - labWrong.Opacity, 150),
                     AaHeight(labWrong, 24 - labWrong.Height, 150, , New AniEaseOutFluent)
                }, "MyTextBox Validate " & Uuid)
            Else
                ShownValidateResult = ValidateState.FailedAndHideDetail
            End If
        Else
            ShownValidateResult = ValidateState.NotLoaded
        End If
        RefreshColor()
        RaiseEvent ValidateChanged(Me, New EventArgs)
    End Sub

    Private Enum ValidateState
        NotInited
        Success
        FailedButTextNotChanged
        FailedAndShowDetail
        FailedAndHideDetail
        NotLoaded
    End Enum

    '提示文本

    ''' <summary>
    ''' 是否已经由用户输入过文本，若尚未输入过，则不显示输入检查的失败。
    ''' </summary>
    Private IsTextChanged As Boolean = False

    Public Shared ReadOnly HintTextProperty As StyledProperty(Of String) =
        AvaloniaProperty.Register(Of MyTextBox, String)(NameOf(HintText), "")
    Public Property HintText As String
        Get
            Return GetValue(HintTextProperty)
        End Get
        Set(value As String)
            SetValue(HintTextProperty, value)
        End Set
    End Property

    '颜色

    Private Sub RefreshColor()
        Try
            If TemplatedParent IsNot Nothing AndAlso TypeOf TemplatedParent Is MyComboBox Then Return
            Dim ForeColorName As String, BackColorName As String
            Dim AnimationTime As Integer
            If IsEnabled Then
                If IsValidated OrElse Not IsTextChanged Then
                    If IsFocused Then
                        ForeColorName = "ColorBrush3"
                        BackColorName = "ColorBrush7"
                        AnimationTime = 10
                    ElseIf IsPointerOver Then
                        ForeColorName = "ColorBrush4"
                        BackColorName = "ColorBrush7"
                        AnimationTime = 100
                    Else
                        ForeColorName = "ColorBrushBg0"
                        BackColorName = "ColorBrushHalfWhite"
                        AnimationTime = 100
                    End If
                Else
                    ForeColorName = "ColorBrushRedLight"
                    BackColorName = "ColorBrushRedBack"
                    AnimationTime = 200
                End If
            Else
                ForeColorName = "ColorBrushGray5"
                BackColorName = "ColorBrushGray6"
                AnimationTime = 200
            End If
            If Not HasBackground Then BackColorName = "ColorBrushTransparent"
            If IsControlLoaded AndAlso AniControlEnabled = 0 Then
                AniStart({
                         AaColor(Me, Border.BorderBrushProperty, ForeColorName, AnimationTime),
                         AaColor(Me, Border.BackgroundProperty, BackColorName, AnimationTime)
                     }, "MyTextBox Color " & Uuid)
            Else
                AniStop("MyTextBox Color " & Uuid)
                SetResourceReference(Border.BorderBrushProperty, ForeColorName)
                SetResourceReference(Border.BackgroundProperty, BackColorName)
            End If
        Catch ex As Exception
            Logger.Warn(ex, "文本框颜色改变出错")
        End Try
    End Sub

    Private Sub RefreshTextColor()
        Dim NewColor As MyColor = If(IsEnabled, ColorGray1, ColorGray4)
        If Foreground Is Nothing Then Return
        If CType(Foreground, SolidColorBrush).Color.R = NewColor.R Then Return
        If IsControlLoaded AndAlso AniControlEnabled = 0 AndAlso Text <> "" Then
            AniStart({AaColor(Me, ForegroundProperty, If(IsEnabled, "ColorBrushGray1", "ColorBrushGray4"), 200)}, "MyTextBox TextColor " & Uuid)
        Else
            AniStop("MyTextBox TextColor " & Uuid)
            Foreground = CType(NewColor, Brush)
        End If
    End Sub

    '初始化

    Private IsControlLoaded As Boolean = False

    Public Sub New()
        MyBase.AddHandler(Me.TextChangedEvent, AddressOf MyTextBox_TextChanged)
        MyBase.AddHandler(Me.KeyUpEvent, AddressOf MyTextBox_KeyUp)
    End Sub

    Protected Overrides Sub OnApplyTemplate(e As TemplateAppliedEventArgs)
        MyBase.OnApplyTemplate(e)
        _labWrong = e.NameScope.Find(Of TextBlock)("labWrong")
        If _labWrong IsNot Nothing Then _labWrong.Text = ValidateResult
        _labHint = e.NameScope.Find(Of TextBlock)("labHint")
        RefreshHintVisibility()
    End Sub

    Private _labHint As TextBlock

    Private Sub RefreshHintVisibility()
        If _labHint Is Nothing Then Return
        _labHint.IsVisible = Text = "" AndAlso Not String.IsNullOrEmpty(HintText)
    End Sub

    Protected Overrides Sub OnAttachedToVisualTree(e As VisualTreeAttachmentEventArgs)
        MyBase.OnAttachedToVisualTree(e)
        IsControlLoaded = True
        RefreshColor()
        RefreshTextColor()
        RefreshHintVisibility()
        Validate()
    End Sub

    Protected Overrides Sub OnPropertyChanged(change As AvaloniaPropertyChangedEventArgs)
        MyBase.OnPropertyChanged(change)
        If change.Property Is ValidateResultProperty Then
            _IsValidated = String.IsNullOrEmpty(ValidateResult)
        ElseIf change.Property Is HintTextProperty Then
            PlaceholderText = If(HintText, "")
            RefreshHintVisibility()
        ElseIf change.Property Is IsEnabledProperty Then
            RefreshColor()
            RefreshTextColor()
        End If
    End Sub

    Protected Overrides Sub OnGotFocus(e As FocusChangedEventArgs)
        MyBase.OnGotFocus(e)
        RefreshColor()
    End Sub

    Protected Overrides Sub OnLostFocus(e As FocusChangedEventArgs)
        MyBase.OnLostFocus(e)
        RefreshColor()
        '离开输入框时兜底保存，避免页面切换期间漏写设置
        SettingService.SaveSetting(Me)
    End Sub

    Protected Overrides Sub OnPointerEntered(e As PointerEventArgs)
        MyBase.OnPointerEntered(e)
        RefreshColor()
    End Sub

    Protected Overrides Sub OnPointerExited(e As PointerEventArgs)
        MyBase.OnPointerExited(e)
        RefreshColor()
    End Sub

    Private Sub MyTextBox_TextChanged(sender As Object, e As TextChangedEventArgs)
        Try
            IsTextChanged = IsControlLoaded
            RefreshHintVisibility()
            Validate()
            If Not IsValidated Then Return
            RaiseEvent ValidatedTextChanged(Me, e)
        Catch ex As Exception
            Logger.Error(ex, "进行输入验证时出错")
        End Try
    End Sub

    Private Sub MyTextBox_KeyUp(sender As Object, e As KeyEventArgs)
        If e.Key = Key.Enter Then RaiseCustomEvent()
    End Sub

#Region "设置"

    Private Sub RefreshSetting(NewValue As String) Implements ISettingControl.RefreshSetting
        Text = NewValue
    End Sub

    Private Function GetCurrentSetting() As String Implements ISettingControl.GetCurrentSetting
        Return Text
    End Function

    Private Sub SaveSetting() Handles Me.ValidatedTextChanged
        SettingService.SaveSetting(Me)
    End Sub

#End Region

End Class
