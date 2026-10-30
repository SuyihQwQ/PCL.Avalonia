''' <summary>
''' 离线登录页面。
''' </summary>
Public Class PageLoginLegacy
    Inherits Grid
    Implements ILoginPage

    Public Sub New()
        InitControls()
        Skin.Loader = PageLaunchLeft.SkinLegacy
    End Sub

    Private Sub PageLoginLegacy_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Skin.Loader.Start()
    End Sub

    Public IsReloaded As Boolean = False

    ''' <summary>
    ''' 刷新页面显示的所有信息。
    ''' </summary>
    Public Sub Reload(KeepInput As Boolean) Implements ILoginPage.Reload
        If KeepInput AndAlso IsReloaded Then '避免第一次就以 KeepInput 的方式加载，导致文本框里没东西
            '保留输入，只刷新下拉框列表
            Dim Input As String = ComboName.Text?.Trim
            ComboName.ItemsSource = If(Settings.Get(Of String)("LoginLegacyName") = "", Nothing, Settings.Get(Of String)("LoginLegacyName").Split("¨"))
            ComboName.Text = Input
        Else
            '不保留输入，刷新列表后自动选择第一项
            If Settings.Get(Of String)("LoginLegacyName") = "" Then
                ComboName.ItemsSource = Nothing
            Else
                ComboName.ItemsSource = Settings.Get(Of String)("LoginLegacyName").ToString.Split("¨")
                ComboName.Text = Settings.Get(Of String)("LoginLegacyName").ToString.BeforeFirst("¨").Trim
            End If
        End If
        IsReloaded = True
    End Sub

    ''' <summary>
    ''' 获取当前页面的登录信息。
    ''' </summary>
    Public Shared Function GetLoginData() As McLoginData
        Dim UserName As String = If(FrmLoginLegacy Is Nothing, "", FrmLoginLegacy.ComboName.Text.Replace("¨", "").Trim)
        Return New McLoginLegacy With {.UserName = UserName, .SkinType = Settings.Get(Of Integer)("LaunchSkinType"), .SkinName = Settings.Get(Of String)("LaunchSkinID")}
    End Function

    ''' <summary>
    ''' 当前页面的登录信息是否有效。
    ''' </summary>
    Public Shared Function IsVaild(LoginData As McLoginLegacy) As String
        If LoginData.UserName.Trim = "" Then Return "玩家名不能为空！"
        If LoginData.UserName.Contains("""") Then Return "玩家名不能包含英文引号！"
        If McInstanceSelected IsNot Nothing AndAlso
           McInstanceSelected.Version.Valid AndAlso McInstanceSelected.Version.Vanilla >= New Version(20, 0, 3) AndAlso
           LoginData.UserName.Trim.Length > 16 Then
            Return "自 1.20.3 起，玩家名至多只能包含 16 个字符！"
        End If
        Return ""
    End Function

    Public Function IsVaild() As String
        Return IsVaild(GetLoginData())
    End Function

    Private Sub ComboName_KeyDown(sender As Object, e As KeyEventArgs) Handles ComboName.KeyDown
        If e.Key = Key.Space AndAlso (String.IsNullOrEmpty(ComboName.Text) OrElse ComboName.Text.EndsWithF(" ")) Then e.Handled = True
    End Sub

    Private Sub ComboLegacy_TextChanged(sender As Object, e As EventArgs) Handles ComboName.TextChanged
        If Settings.Get(Of Integer)("LaunchSkinType") = 0 Then PageLaunchLeft.SkinLegacy.Start(IsForceRestart:=True)
        HintChinese.IsVisible = Not ComboName.Text.RegexCheck("^[0-9A-Za-z_]*$")
        RunInUi(Sub() PanMain.InvalidateMeasure(), True)
    End Sub

    Private Sub Skin_Click() Handles Skin.Click
        If (Settings.Get(Of Boolean)("UiHiddenPageSetup") OrElse Settings.Get(Of Boolean)("UiHiddenSetupLaunch")) AndAlso Not PageSetupUI.HiddenForceShow Then
            Hint("启动设置已被禁用！", HintType.Red)
        Else
            FrmMain.PageChange(FormMain.PageType.Setup, FormMain.PageSubType.SetupLaunch) '切换到皮肤设置页面
        End If
    End Sub

End Class
