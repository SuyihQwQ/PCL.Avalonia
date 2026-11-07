''' <summary>
''' 微软正版登录页面。
''' </summary>
Public Class PageLoginMs
    Inherits StackPanel
    Implements ILoginPage

    Public Sub New()
        InitControls()
    End Sub

    ''' <summary>
    ''' 刷新页面显示的所有信息。
    ''' </summary>
    Public Sub Reload(KeepInput As Boolean) Implements ILoginPage.Reload
        Dim IndexBefore = ComboAccounts.SelectedIndex
        '刷新下拉框列表
        ComboAccounts.Items.Clear()
        ComboAccounts.Items.Add(New MyComboBoxItem With {.Content = "添加新账号"})
        Try
            Dim MsJson As JObject = Settings.Get(Of String)("LoginMsJson").DeserializeJson()
            For Each Account In MsJson
                Dim Item As New MyListItem With {
                    .Title = Account.Key,
                    .FontSize = 13,
                    .MaxHeight = 26,
                    .IsScaleAnimationEnabled = False,
                    .Margin = New Thickness(-3, 0, 0, 0),
                    .Tag = Account.Value.ToString
                }
                Dim BtnDelete As New MyIconButton With {
                    .Height = 20,
                    .Width = 20,
                    .VerticalAlignment = VerticalAlignment.Center,
                    .Tag = Account.Key,
                    .Logo = "F1 M 26.9166,22.1667L 37.9999,33.25L 49.0832,22.1668L 53.8332,26.9168L 42.7499,38L 53.8332,49.0834L 49.0833,53.8334L 37.9999,42.75L 26.9166,53.8334L 22.1666,49.0833L 33.25,38L 22.1667,26.9167L 26.9166,22.1667 Z"
                }
                AddHandler BtnDelete.Click, Sub()
                                                Try
                                                    Dim CurrentJson As JObject = Settings.Get(Of String)("LoginMsJson").DeserializeJson()
                                                    CurrentJson.Remove(Account.Key)
                                                    Settings.Set("LoginMsJson", CurrentJson.ToString(Newtonsoft.Json.Formatting.None))
                                                    If ComboAccounts.SelectedItem Is Item Then ComboAccounts.SelectedIndex = 0
                                                    ComboAccounts.Items.Remove(Item)
                                                Catch ex As Exception
                                                    Logger.Error(ex, "删除微软登录账号失败", LogBehavior.Toast)
                                                End Try
                                            End Sub
                Item.Buttons = {BtnDelete}
                ComboAccounts.Items.Add(Item)
            Next
        Catch ex As Exception
            Logger.Error(ex, "微软登录信息出错，登录信息已被重置", LogBehavior.Toast)
            Settings.Set("LoginMsJson", "{}")
        End Try
        '如果不保留输入，刷新列表后自动选择第一项
        ComboAccounts.SelectedIndex = If(KeepInput, Math.Max(0, IndexBefore), 0)
    End Sub

    Private Sub ComboAccounts_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboAccounts.SelectionChanged
        If AniControlEnabled <> 0 OrElse ComboAccounts.SelectedItem Is Nothing OrElse ComboAccounts.ContentPresenter Is Nothing Then Return
        If TypeOf ComboAccounts.SelectedItem Is MyListItem Then
            ComboAccounts.ContentPresenter.Content = CType(ComboAccounts.SelectedItem, MyListItem).Title
        ElseIf TypeOf ComboAccounts.SelectedItem Is MyComboBoxItem Then
            ComboAccounts.ContentPresenter.Content = CType(ComboAccounts.SelectedItem, MyComboBoxItem).Content
        End If
    End Sub

    ''' <summary>
    ''' 是否至少登录过一个微软正版账号。
    ''' </summary>
    Public Shared Function HasLoggedInAccount() As Boolean
        If Not String.IsNullOrEmpty(Settings.Get(Of String)("CacheMsV2OAuthRefresh")) Then Return True
        Try
            Dim MsJson As JObject = Settings.Get(Of String)("LoginMsJson").DeserializeJson()
            Return MsJson IsNot Nothing AndAlso MsJson.HasValues
        Catch ex As Exception
            Logger.Warn(ex, "读取微软登录账号列表失败")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' 获取当前页面的登录信息。
    ''' </summary>
    Public Shared Function GetLoginData() As McLoginMs
        If FrmLoginMs Is Nothing Then Return New McLoginMs With {.OAuthRefreshToken = Settings.Get(Of String)("CacheMsV2OAuthRefresh"), .UserName = Settings.Get(Of String)("CacheMsV2Name")}
        Dim Result As McLoginMs = Nothing
        RunInUiWait(
        Sub()
            If FrmLoginMs.ComboAccounts.SelectedIndex = 0 Then
                Result = New McLoginMs
            Else
                Dim Item As MyListItem = TryCast(FrmLoginMs.ComboAccounts.SelectedItem, MyListItem)
                If Item Is Nothing Then
                    Result = New McLoginMs
                Else
                    Result = New McLoginMs With {.OAuthRefreshToken = Item.Tag, .UserName = Item.Title}
                End If
            End If
        End Sub)
        Return Result
    End Function

    ''' <summary>
    ''' 当前页面的登录信息是否有效。
    ''' </summary>
    Public Shared Function IsVaild(LoginData As McLoginMs) As String
        If LoginData.OAuthRefreshToken = "" Then
            Return "请在登录账号后再启动游戏！"
        Else
            Return ""
        End If
    End Function

    Public Function IsVaild() As String
        Return IsVaild(GetLoginData())
    End Function

    Private Sub BtnLogin_Click(sender As Object, e As PointerReleasedEventArgs) Handles BtnLogin.Click
        ComboAccounts.IsEnabled = False
        BtnLogin.IsEnabled = False
        BtnLogin.Text = "0%"
        RunInNewThread(
        Sub()
            Try
                McLoginMsLoader.Start(GetLoginData(), IsForceRestart:=True)
                Do While McLoginMsLoader.State = LoadState.Loading
                    RunInUi(Sub() BtnLogin.Text = Math.Round(McLoginMsLoader.Progress * 100) & "%")
                    Thread.Sleep(50)
                Loop
                If McLoginMsLoader.State = LoadState.Finished Then
                    RunInUi(Sub() FrmLaunchLeft.RefreshPage(False, True))
                ElseIf McLoginMsLoader.State = LoadState.Canceled Then
                    Throw New OperationCanceledException
                ElseIf McLoginMsLoader.Error Is Nothing Then
                    Throw New Exception("未知错误！")
                Else
                    Throw New Exception(McLoginMsLoader.Error.Message, McLoginMsLoader.Error)
                End If
            Catch ex As Exception
                If ex.IsCanceled Then
                ElseIf ex.Message.StartsWithF("$") Then
                    Hint(ex.Message.TrimStart("$"), HintType.Red)
                ElseIf TypeOf ex Is Security.Authentication.AuthenticationException AndAlso ex.Message.Contains("SSL/TLS") Then
                    Logger.Error(ex, $"正版登录验证失败，请考虑在 [设置 → 其他] 中关闭 [在正版登录时验证 SSL 证书]，然后再试。{vbCrLf}{vbCrLf}原始错误信息：", LogBehavior.Alert)
                Else
                    Logger.Error(ex, "正版登录尝试失败", LogBehavior.Alert)
                End If
            Finally
                RunInUi(
                Sub()
                    ComboAccounts.IsEnabled = True
                    BtnLogin.IsEnabled = True
                    BtnLogin.Text = "登录"
                End Sub)
            End Try
        End Sub, "Ms Login")
    End Sub

End Class
