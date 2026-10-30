''' <summary>
''' Authlib-Injector 登录页面。
''' </summary>
Public Class PageLoginAuth
    Inherits Grid
    Implements ILoginPage

    Public Sub New()
        InitControls()
    End Sub

    Private IsFirstLoad As Boolean = True

    ''' <summary>
    ''' 刷新页面显示的所有信息。
    ''' </summary>
    Public Sub Reload(KeepInput As Boolean) Implements ILoginPage.Reload
        '记住密码
        CheckRemember.Checked = Settings.Get(Of Boolean)("LoginRemember")
        If KeepInput AndAlso Not IsFirstLoad Then '避免第一次就以 KeepInput 的方式加载，导致文本框里没东西
            '保留输入，只刷新下拉框列表
            Dim Input As String = ComboName.Text
            ComboName.ItemsSource = If(Settings.Get(Of String)("LoginAuthEmail") = "", Nothing, Settings.Get(Of String)("LoginAuthEmail").Split("¨"))
            ComboName.Text = Input
        Else
            '不保留输入，刷新列表后自动选择第一项
            If Settings.Get(Of String)("LoginAuthEmail") = "" Then
                ComboName.ItemsSource = Nothing
            Else
                ComboName.ItemsSource = Settings.Get(Of String)("LoginAuthEmail").Split("¨")
                ComboName.Text = Settings.Get(Of String)("LoginAuthEmail").BeforeFirst("¨")
                If Settings.Get(Of Boolean)("LoginRemember") Then TextPass.Text = Settings.Get(Of String)("LoginAuthPass").BeforeFirst("¨").Trim
            End If
        End If
        IsFirstLoad = False
    End Sub

    ''' <summary>
    ''' 获取当前页面的登录信息。
    ''' </summary>
    Public Shared Function GetLoginData() As McLoginServer
        Dim Server As String = If(IsNothing(McInstanceSelected), Settings.Get(Of String)("CacheAuthServerServer"), Settings.Get(Of String)("VersionServerAuthServer", Instance:=McInstanceSelected)) & "/authserver"
        If FrmLoginAuth Is Nothing Then
            Return New McLoginServer(McLoginType.Auth) With {.Token = "Auth", .BaseUrl = Server, .UserName = "", .Password = "", .Description = "Authlib-Injector", .Type = McLoginType.Auth}
        Else
            Return New McLoginServer(McLoginType.Auth) With {.Token = "Auth", .BaseUrl = Server, .UserName = FrmLoginAuth.ComboName.Text.Replace("¨", "").Trim, .Password = FrmLoginAuth.TextPass.Text.Replace("¨", "").Trim, .Description = "Authlib-Injector", .Type = McLoginType.Auth}
        End If
    End Function

    ''' <summary>
    ''' 当前页面的登录信息是否有效。
    ''' </summary>
    Public Shared Function IsVaild(LoginData As McLoginServer) As String
        If LoginData.UserName = "" Then Return "账号不能为空！"
        If LoginData.Password = "" Then Return "密码不能为空！"
        Return ""
    End Function

    Public Function IsVaild() As String
        Return IsVaild(GetLoginData())
    End Function

    '保存输入信息
    Private Sub ComboName_TextChanged(sender As Object, e As EventArgs) Handles ComboName.TextChanged
        If ComboName.Text = "" Then TextPass.Text = ""
        If AniControlEnabled = 0 Then Settings.Set("CacheAuthAccess", "") '迫使其不进行 Validate
        BtnLink.Text = If(ComboName.Text = "", "注册账号", "找回密码")
    End Sub

    Private Sub TextPass_PasswordChanged(sender As Object, e As RoutedEventArgs) Handles TextPass.TextChanged
        If AniControlEnabled = 0 Then Settings.Set("CacheAuthAccess", "")
    End Sub

    Private Sub ComboName_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboName.SelectionChanged
        If ComboName.SelectedIndex = -1 OrElse Not Settings.Get(Of Boolean)("LoginRemember") Then
            TextPass.Text = ""
        Else
            Dim SavedPass As String() = Settings.Get(Of String)("LoginAuthPass").ToString.Split("¨")
            If ComboName.SelectedIndex < SavedPass.Length Then TextPass.Text = SavedPass(ComboName.SelectedIndex).Trim
        End If
    End Sub

    Private Sub CheckBoxChange(sender As Object, user As Boolean) Handles CheckRemember.Change
        If AniControlEnabled = 0 Then Settings.Set("LoginRemember", CheckRemember.Checked)
    End Sub

    '链接处理
    Private Sub Btn_Click(sender As Object, e As EventArgs) Handles BtnLink.Click
        If BtnLink.Text = "注册账号" Then
            OpenWebsite(If(McInstanceSelected IsNot Nothing, Settings.Get(Of String)("VersionServerAuthRegister", Instance:=McInstanceSelected), Settings.Get(Of String)("CacheAuthServerRegister")))
        Else
            Dim Website As String = If(McInstanceSelected IsNot Nothing, Settings.Get(Of String)("VersionServerAuthRegister", Instance:=McInstanceSelected), Settings.Get(Of String)("CacheAuthServerRegister"))
            OpenWebsite(Website.Replace("/auth/register", "/auth/forgot"))
        End If
    End Sub

    '切换注册按钮可见性
    Private Sub ReloadRegisterButton() Handles Me.Loaded
        Dim Address As String = If(McInstanceSelected IsNot Nothing, Settings.Get(Of String)("VersionServerAuthRegister", Instance:=McInstanceSelected), Settings.Get(Of String)("CacheAuthServerRegister"))
        BtnLink.IsVisible = String.IsNullOrEmpty(New ValidateHttp().Validate(Address))
    End Sub

End Class
