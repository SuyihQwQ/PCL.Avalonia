Imports Avalonia.Controls.Documents

Public Class MyHint
    Inherits Border

    Public Uuid As Integer = GetUuid()

    '边框
    Public Property HasBorder As Boolean
        Get
            Return BorderThickness.Top > 0
        End Get
        Set(value As Boolean)
            If value Then
                BorderThickness = New Thickness(3, GetWPFSize(1), GetWPFSize(1), GetWPFSize(1))
            Else
                BorderThickness = New Thickness(3, 0, 0, 0)
            End If
        End Set
    End Property

    '配色
    Public Enum Themes
        Blue = 0
        Red = 1
        Yellow = 2
    End Enum

    Public Shared ReadOnly HintThemeProperty As StyledProperty(Of Themes) =
        AvaloniaProperty.Register(Of MyHint, Themes)("HintTheme", Themes.Red)
    Public Property HintTheme As Themes
        Get
            Return GetValue(HintThemeProperty)
        End Get
        Set(value As Themes)
            SetValue(HintThemeProperty, value)
        End Set
    End Property

    Private Sub UpdateUI()
        If LabText Is Nothing Then Return
        Select Case HintTheme
            Case Themes.Blue
                Background = CType(New MyColor("#d9ecff"), Brush)
                BorderBrush = CType(New MyColor("#1172D4"), Brush)
                LabText.Foreground = CType(New MyColor("#0F64B8"), Brush)
                BtnClose.Foreground = CType(New MyColor("#0F64B8"), Brush)
            Case Themes.Red
                Background = CType(New MyColor("#ffdddf"), Brush)
                BorderBrush = CType(New MyColor("#d82929"), Brush)
                LabText.Foreground = CType(New MyColor("#bf0b0b"), Brush)
                BtnClose.Foreground = CType(New MyColor("#bf0b0b"), Brush)
            Case Themes.Yellow
                Background = CType(New MyColor("#ffebd7"), Brush)
                BorderBrush = CType(New MyColor("#f57a00"), Brush)
                LabText.Foreground = CType(New MyColor("#d86c00"), Brush)
                BtnClose.Foreground = CType(New MyColor("#d86c00"), Brush)
        End Select
    End Sub

    ''' <summary>
    ''' 兼容旧版帮助 XAML 中的 Theme 属性。
    ''' </summary>
    Public Shadows Property Theme As Themes
        Get
            Return HintTheme
        End Get
        Set(value As Themes)
            HintTheme = value
        End Set
    End Property

    <Obsolete("IsWarn 已过时。请换用 HintTheme 属性。")>
    Public Property IsWarn As Boolean
        Get
            Return HintTheme = Themes.Red
        End Get
        Set(value As Boolean)
            HintTheme = If(value, Themes.Red, Themes.Blue)
        End Set
    End Property

    '文本
    Private _Inlines As InlineCollection
    Public ReadOnly Property Inlines As InlineCollection
        Get
            If LabText IsNot Nothing Then Return LabText.Inlines
            If _Inlines Is Nothing Then _Inlines = New InlineCollection
            Return _Inlines
        End Get
    End Property

    Public Shared ReadOnly TextProperty As StyledProperty(Of String) =
        AvaloniaProperty.Register(Of MyHint, String)("Text", "")
    Public Property Text As String
        Get
            Return GetValue(TextProperty)
        End Get
        Set(value As String)
            SetValue(TextProperty, value)
        End Set
    End Property

    '关闭按钮
    Public Property CanClose As Boolean
        Get
            Return BtnClose.IsVisible
        End Get
        Set(value As Boolean)
            BtnClose.IsVisible = value
        End Set
    End Property
    Public Property RelativeSetup As String = ""

    Public Sub New()
        InitControls()
        UpdateUI()
        AddHandler BtnClose.Click, AddressOf BtnClose_Click
        AddHandler PointerPressed, AddressOf MyHint_MouseDown
        AddHandler PointerReleased, AddressOf MyHint_MouseUp
        AddHandler PointerExited, AddressOf MyHint_MouseLeave
    End Sub

    Protected Overrides Sub OnAttachedToVisualTree(e As VisualTreeAttachmentEventArgs)
        MyBase.OnAttachedToVisualTree(e)
        UpdateUI()
        If CanClose AndAlso Not String.IsNullOrEmpty(RelativeSetup) Then
            Try
                If Settings.Get(Of Boolean)(RelativeSetup) Then IsVisible = False
            Catch ex As Exception
                Logger.Warn(ex, $"读取提示条设置失败（{RelativeSetup}）")
            End Try
        End If
    End Sub

    Protected Overrides Sub OnPropertyChanged(change As AvaloniaPropertyChangedEventArgs)
        MyBase.OnPropertyChanged(change)
        If change.Property Is HintThemeProperty Then
            UpdateUI()
        ElseIf change.Property Is TextProperty AndAlso LabText IsNot Nothing Then
            LabText.Text = Text
        End If
    End Sub

    Private Sub BtnClose_Click(sender As Object, e As EventArgs)
        If CanClose AndAlso Not String.IsNullOrEmpty(RelativeSetup) Then
            Try
                Settings.SetSafe(RelativeSetup, True)
            Catch ex As Exception
                Logger.Warn(ex, $"保存提示条设置失败（{RelativeSetup}）")
            End Try
        End If
        AniDispose(Me, False)
    End Sub

    '触发点击事件
    Public Event Click(sender As Object, e As PointerReleasedEventArgs)
    Private IsMouseDown As Boolean = False

    Private Sub MyHint_MouseUp(sender As Object, e As PointerReleasedEventArgs)
        If Not IsMouseDown Then Return
        IsMouseDown = False
        Logger.Info($"按下提示条{If(String.IsNullOrEmpty(Name), "", "：" & Name)}")
        RaiseEvent Click(sender, e)
        If e.Handled Then Return
        e.Handled = True
        RaiseCustomEvent()
    End Sub

    Private Sub MyHint_MouseDown(sender As Object, e As PointerPressedEventArgs)
        IsMouseDown = True
    End Sub

    Private Sub MyHint_MouseLeave(sender As Object, e As PointerEventArgs)
        IsMouseDown = False
    End Sub

End Class

Partial Public Module ModAnimation
    Public Sub AniDispose(Control As MyHint, RemoveFromChildren As Boolean, Optional CallBack As ParameterizedThreadStart = Nothing)
        If Not Control.IsHitTestVisible Then Return
        Control.IsHitTestVisible = False
        AniStart({
            AaScaleTransform(Control, -0.08, 200, , New AniEaseInFluent),
            AaOpacity(Control, -1, 200, , New AniEaseOutFluent),
            AaHeight(Control, -Control.Bounds.Height, 150, 100, New AniEaseOutFluent),
            AaCode(
            Sub()
                If RemoveFromChildren Then
                    Dim ParentPanel = TryCast(Control.Parent, Panel)
                    ParentPanel?.Children.Remove(Control)
                Else
                    Control.IsVisible = False
                End If
                If CallBack IsNot Nothing Then CallBack(Control)
            End Sub, , True)
        }, "MyHint Dispose " & Control.Uuid)
    End Sub
End Module
