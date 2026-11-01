Imports Avalonia.Controls.Primitives
Imports Newtonsoft.Json.Linq

''' <summary>
''' 微软账户设备代码登录弹窗，显示在 FormMain.PanMsg 内。
''' </summary>
Public Class MyMsgLogin
    Inherits MyMsgBoxBase

    Private ReadOnly Data As JObject
    Private ReadOnly UserCode As String
    Private ReadOnly DeviceCode As String
    Private ReadOnly Website As String
    Private IsLoginStarted As Boolean = False

    Public Sub New(Converter As MyMsgBoxConverter)
        MyBase.New(Converter)
        Data = CType(Converter.Content, JObject)
        UserCode = Data("user_code").ToString()
        DeviceCode = Data("device_code").ToString()
        Website = Data("verification_uri").ToString()

        LabTitle.Text = "登录 Minecraft"
        Btn1.Text = "重新打开网页"
        Btn1.IsVisible = True
        Btn2.Text = "复制代码"
        Btn2.IsVisible = True
        Btn3.Text = "取消"
        Btn3.IsVisible = True

        Dim PanCaption As New MyScrollViewer With {
            .Margin = New Thickness(0, 0, 0, 17),
            .Padding = New Thickness(7, 0, 15, 0),
            .HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            .VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            .DeltaMult = 0.7,
            .VerticalAlignment = VerticalAlignment.Top}
        Dim Caption As New TextBlock With {
            .FontSize = 15,
            .LineHeight = 18,
            .TextWrapping = TextWrapping.Wrap,
            .TextTrimming = TextTrimming.None,
            .Padding = New Thickness(1),
            .VerticalAlignment = VerticalAlignment.Top,
            .Text =
                $"登录网页将自动开启，请在网页中输入 {UserCode}（已自动复制）。" & vbCrLf & vbCrLf &
                $"如果网络环境不佳，网页可能一直加载不出来，届时请使用加速器或 VPN 改善网络环境。" & vbCrLf &
                $"你也可以用其他设备打开 {Website} 并输入上述代码。"}
        Caption.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushGray2")
        PanCaption.Content = Caption
        Grid.SetRow(PanCaption, 3)
        PanMain.Children.Add(PanCaption)
    End Sub

    Protected Overrides Sub RefreshHighlight()
        '登录弹窗的三个按钮都保持普通样式
    End Sub

    ''' <summary>
    ''' 弹窗显示后启动登录工作线程。
    ''' </summary>
    Public Sub StartLogin()
        If IsLoginStarted Then Return
        IsLoginStarted = True
        Logger.Info($"登录弹窗：登录 Minecraft{vbCrLf}{Website}")
        RunInNewThread(AddressOf WorkThread, "MyMsgLogin")
    End Sub

    Public Overrides Sub Btn1_Click()
        If Converter.IsExited Then Return
        OpenWebsite(Website)
    End Sub

    Public Overrides Sub Btn2_Click()
        If Converter.IsExited Then Return
        ClipboardSet(UserCode)
    End Sub

    Public Overrides Sub Btn3_Click()
        Finished(New OperationCanceledException)
    End Sub

    Private Sub Finished(Result As Object)
        If Converter.IsExited Then Return
        Converter.IsExited = True
        Converter.Result = Result
        RunInUi(AddressOf Close)
        Thread.Sleep(200)
        FrmMain?.ShowWindowToTop()
    End Sub

    Private Sub WorkThread()
        Thread.Sleep(2000)
        If Converter.IsExited Then Return
        OpenWebsite(Website)
        ClipboardSet(UserCode)
        Thread.Sleep((Data("interval").ToObject(Of Integer)() - 1) * 1000)
        '轮询
        Dim UnknownFailureCount As Integer = 0
        Do While Not Converter.IsExited
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
