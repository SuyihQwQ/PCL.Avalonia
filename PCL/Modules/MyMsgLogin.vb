Imports Avalonia.Layout
Imports Newtonsoft.Json.Linq

''' <summary>
''' 微软账户设备代码登录弹窗。与原版 MyMsgLogin.xaml 的行为一致，但以独立窗口形式显示。
''' </summary>
Public Class MyMsgLogin
    Inherits Window

    Private ReadOnly Data As JObject
    Private ReadOnly UserCode As String
    Private ReadOnly DeviceCode As String
    Private ReadOnly Website As String

    Private ReadOnly MyConverter As MyMsgBoxConverter
    Private ReadOnly LabCaption As TextBlock
    Private IsLoginStarted As Boolean = False

    Public Sub New(Converter As MyMsgBoxConverter)
        MyConverter = Converter
        Data = CType(Converter.Content, JObject)
        UserCode = Data("user_code").ToString()
        DeviceCode = Data("device_code").ToString()
        Website = Data("verification_uri").ToString()

        Title = "登录 Minecraft"
        Width = 460
        SizeToContent = SizeToContent.Height
        CanResize = False
        ShowInTaskbar = False
        WindowStartupLocation = WindowStartupLocation.CenterOwner
        Background = New SolidColorBrush(Color.Parse("#F7F9FC"))

        Dim TitleText As New TextBlock With {
            .Text = "登录 Minecraft",
            .FontSize = 17,
            .FontWeight = FontWeight.SemiBold,
            .Foreground = New SolidColorBrush(Color.Parse("#343D4A"))
        }
        LabCaption = New TextBlock With {
            .FontSize = 13,
            .TextWrapping = TextWrapping.Wrap,
            .Foreground = New SolidColorBrush(Color.Parse("#5C5C5C")),
            .Text =
                $"登录网页将自动开启，请在网页中输入 {UserCode}（已自动复制）。" & vbCrLf & vbCrLf &
                $"如果网络环境不佳，网页可能一直加载不出来，届时请使用加速器或 VPN 改善网络环境。" & vbCrLf &
                $"你也可以用其他设备打开 {Website} 并输入上述代码。"
        }
        Dim ButtonPanel As New StackPanel With {
            .Orientation = Orientation.Horizontal,
            .HorizontalAlignment = HorizontalAlignment.Right,
            .Spacing = 10
        }
        ButtonPanel.Children.Add(CreateButton("重新打开网页", Sub() OpenWebsite(Website)))
        ButtonPanel.Children.Add(CreateButton("复制代码", Sub() ClipboardSet(UserCode)))
        ButtonPanel.Children.Add(CreateButton("取消", Sub() Finished(New OperationCanceledException)))

        Dim Stack As New StackPanel With {.Spacing = 18}
        Stack.Children.Add(TitleText)
        Stack.Children.Add(LabCaption)
        Stack.Children.Add(ButtonPanel)
        Content = New Border With {
            .Padding = New Thickness(26, 22, 26, 22),
            .Child = Stack
        }

        AddHandler Closed, AddressOf OnDialogClosed
    End Sub

    Private Shared Function CreateButton(Text As String, Handler As Action) As Button
        Dim Button As New Button With {
            .Content = Text,
            .MinWidth = 88,
            .Padding = New Thickness(16, 6, 16, 6),
            .HorizontalContentAlignment = HorizontalAlignment.Center
        }
        AddHandler Button.Click, Sub(sender, e) Handler()
        Return Button
    End Function

    ''' <summary>
    ''' 在窗口显示后启动登录工作线程。
    ''' </summary>
    Public Sub StartLogin()
        If IsLoginStarted Then Return
        IsLoginStarted = True
        Logger.Info($"登录弹窗：登录 Minecraft{vbCrLf}{LabCaption.Text}")
        RunInNewThread(AddressOf WorkThread, "MyMsgLogin")
    End Sub

    Private Sub Finished(Result As Object)
        If MyConverter.IsExited Then Return
        MyConverter.IsExited = True
        MyConverter.Result = Result
        RunInUi(AddressOf Close)
        Thread.Sleep(200)
        FrmMain?.ShowWindowToTop()
    End Sub

    Private Sub OnDialogClosed(sender As Object, e As EventArgs)
        If Not MyConverter.IsExited Then
            MyConverter.IsExited = True
            MyConverter.Result = New OperationCanceledException
        End If
        If ActiveMsgBoxWindow Is Me Then ActiveMsgBoxWindow = Nothing
        MyMsgBoxTick()
    End Sub

    Private Sub WorkThread()
        Thread.Sleep(2000)
        If MyConverter.IsExited Then Return
        OpenWebsite(Website)
        ClipboardSet(UserCode)
        Thread.Sleep((Data("interval").ToObject(Of Integer)() - 1) * 1000)
        '轮询
        Dim UnknownFailureCount As Integer = 0
        Do While Not MyConverter.IsExited
            Try
                Dim Result = NetRequestByClient(
                    "https://login.microsoftonline.com/consumers/oauth2/v2.0/token", HttpMethod.Post,
                    Content:=
                        "grant_type=urn:ietf:params:oauth:grant-type:device_code" & "&" &
                        "client_id=" & OAuthClientId & "&" &
                        "device_code=" & DeviceCode & "&" &
                        "scope=XboxLive.signin%20offline_access",
                    ContentType:="application/x-www-form-urlencoded",
                    Timeout:=5000 + UnknownFailureCount * 5000,
                    MakeLog:=False, RequireJson:=True)
                '获取结果
                Dim ResultJson As JObject = Result.DeserializeJson()
                McLaunchLog($"令牌过期时间：{ResultJson("expires_in")} 秒")
                Hint("网页登录成功！", HintType.Green)
                Finished({ResultJson("access_token").ToString, ResultJson("refresh_token").ToString})
                Return
            Catch ex As Exception
                '修改错误列表时，同时修改 ModLaunch.MsLoginStep1Refresh 中的对应代码
                If TypeOf ex Is HttpRequestCodeException Then
                    Dim Response = CType(ex, HttpRequestCodeException).Response
                    If Response.Contains("authorization_declined") Then
                        Finished(New Exception("$你拒绝了 PCL 申请的权限……"))
                        Return
                    ElseIf Response.Contains("expired_token") Then
                        Finished(New Exception("$登录用时太长啦，重新试试吧！"))
                        Return
                    ElseIf Response.Contains("Account security interrupt") Then
                        Finished(New Exception("$该账号由于安全问题无法登陆，请前往微软账户页获取更多信息。"))
                        Return
                    ElseIf Response.Contains("service abuse") Then
                        Finished(New Exception("$非常抱歉，该账号已被微软封禁，无法登录。"))
                        Return
                    ElseIf Response.Contains("AADSTS70000") Then '可能不能判 “invalid_grant”，见 #269
                        Finished(New RestartException)
                        Return
                    ElseIf Response.Contains("authorization_pending") Then
                        Thread.Sleep(2000)
                        Continue Do
                    End If
                End If
                If UnknownFailureCount <= 2 Then
                    UnknownFailureCount += 1
                    Logger.Warn(ex, $"登录轮询第 {UnknownFailureCount} 次失败")
                    Thread.Sleep(2000)
                Else
                    Finished(New Exception("登录轮询失败", ex))
                    Return
                End If
            End Try
        Loop
    End Sub

End Class
