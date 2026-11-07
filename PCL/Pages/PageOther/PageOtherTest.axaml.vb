Imports System.Globalization

''' <summary>
''' “更多”页面的百宝箱子页面。
''' </summary>
Public Class PageOtherTest
    Inherits MyPageRight

    Public Sub New()
        InitControls()
    End Sub

    Public Shared Sub StartCustomDownload(Url As String, FileName As String, Optional Folder As String = Nothing)
    End Sub
    Public Shared Sub Jrrp()
    End Sub
    Public Shared Sub RubbishClear()
    End Sub
    Public Shared Sub MemoryOptimize(ShowHint As Boolean)
    End Sub
    Public Shared Sub MemoryOptimizeInternal(ShowHint As Boolean)
    End Sub
    Public Shared Function GetRandomHint() As String
        Return RandomOne(New List(Of String) From {
            "你可以在 设置 → 启动 中调整游戏内存分配。",
            "在 设置 → 个性化 中可以更换主题颜色和背景图片。",
            "把背景图片放进 PCL 数据文件夹的 Pictures 文件夹，PCL 会在每次启动时随机加载一张。",
            "PCL.Avalonia 是原版 PCL 的跨平台移植版本。",
            "遇到问题时，可以在 更多 → 帮助 中查找常见问题的解决办法。"
        })
    End Function
    Public Shared Function GetRandomPresetHint() As String
        Return ""
    End Function

    '检测中国用户
    Private Sub BtnDetectChinaUser_Click(sender As Object, e As PointerReleasedEventArgs) Handles BtnDetectChinaUser.Click
        Hint("正在检测……", Log:=False)
        '字体枚举在 UI 线程执行，避免平台字体服务在后台线程出错
        Dim SongFont = FindSongFont()
        RunInNewThread(
        Sub()
            Try
                Dim Score As Integer = 0
                Dim Lines As New List(Of String)
                '系统时区
                Dim Zone = TimeZoneInfo.Local
                Dim Offset = Zone.GetUtcOffset(Date.Now)
                Dim IsChinaZone As Boolean = Offset = TimeSpan.FromHours(8)
                If IsChinaZone Then Score += 1
                Dim OffsetSign As String = If(Offset < TimeSpan.Zero, "-", "+")
                Lines.Add($"系统时区：{If(IsChinaZone, "+1", "+0")}（{Zone.Id}，UTC{OffsetSign}{Offset.Duration.ToString("hh\:mm")}）")
                '系统语言
                Dim Language = CultureInfo.CurrentUICulture.Name
                Dim IsChineseLanguage As Boolean = Language.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
                If IsChineseLanguage Then Score += 1
                Lines.Add($"系统语言：{If(IsChineseLanguage, "+1", "+0")}（{Language}）")
                '安装字体
                If SongFont IsNot Nothing Then Score += 1
                Lines.Add($"安装字体：{If(SongFont IsNot Nothing, "+1", "+0")}（{If(SongFont, "未找到宋体")}）")
                'IP 地区
                Dim Country = GetIpCountry()
                Dim IsChinaIp As Boolean = IsChinaCountry(Country)
                If IsChinaIp Then Score += 1
                Lines.Add($"IP 地区：{If(IsChinaIp, "+1", "+0")}（{If(String.IsNullOrEmpty(Country), "查询失败", Country)}）")
                Dim Result = String.Join(vbCrLf, Lines) & vbCrLf & vbCrLf &
                             GetChinaUserResult(Score) & vbCrLf & "（检测结果仅供参考）"
                RunInUi(Sub() MyMsgBox(Result, "中国用户检测"))
            Catch ex As Exception
                Logger.Error(ex, "检测中国用户失败")
                RunInUi(Sub() MyMsgBox("检测失败：" & ex.Message, "中国用户检测", IsWarn:=True))
            End Try
        End Sub, "Detect China User")
    End Sub

    Private Shared Function GetChinaUserResult(Score As Integer) As String
        Select Case Score
            Case 0
                Return "你应该不是中国用户"
            Case 1
                Return "你有可能是中国用户"
            Case 2
                Return "你很可能是中国用户"
            Case 3
                Return "你极可能是中国用户"
            Case Else
                Return "你就是中国用户😈😈"
        End Select
    End Function

    ''' <summary>
    ''' 查找系统中的宋体类字体，返回字体名；未找到时返回 Nothing。
    ''' </summary>
    Private Shared Function FindSongFont() As String
        Try
            For Each Family In FontManager.Current.SystemFonts
                Dim Name = Family.Name
                If String.IsNullOrEmpty(Name) Then Continue For
                If Name.Contains("SimSun") OrElse Name.Contains("宋体") OrElse Name.Contains("NSimSun") OrElse
                   Name.Contains("新宋体") OrElse Name.Contains("Songti") OrElse Name.Contains("STSong") Then
                    Return Name
                End If
            Next
        Catch ex As Exception
            Logger.Warn(ex, "枚举系统字体失败")
        End Try
        If OperatingSystem.IsWindows() Then
            Dim WindowsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Windows)
            If Not String.IsNullOrEmpty(WindowsFolder) Then
                For Each FileName In {"simsun.ttc", "simsun.ttf"}
                    If FileUtils.Exists(Path.Combine(WindowsFolder, "Fonts", FileName)) Then Return "SimSun"
                Next
            End If
        End If
        Return Nothing
    End Function

    ''' <summary>
    ''' 通过公开 API 查询当前 IP 的国家/地区。失败时返回 Nothing。
    ''' </summary>
    Private Shared Function GetIpCountry() As String
        Try
            Dim Text = NetRequestByLoader("https://myip.ipip.net", 8000, IsJson:=False, SimulateBrowserHeaders:=True)
            Dim Match = System.Text.RegularExpressions.Regex.Match(Text, "来自于[：:]\s*([^\s]+)")
            If Match.Success Then
                Dim Country = Match.Groups(1).Value
                If Not String.IsNullOrEmpty(Country) AndAlso Not Country.Contains(ChrW(&HFFFD)) Then Return Country
            End If
            If Text.Contains("中国") Then Return "中国"
        Catch ex As Exception
            Logger.Warn(ex, "查询 IP 地区失败（myip.ipip.net）")
        End Try
        Try
            Dim Country = NetRequestByLoader("https://api.country.is/", 8000, IsJson:=True, SimulateBrowserHeaders:=True).DeserializeJson(Of JObject)()("country")?.ToString
            If Not String.IsNullOrWhiteSpace(Country) Then Return Country
        Catch ex As Exception
            Logger.Warn(ex, "查询 IP 地区失败（api.country.is）")
        End Try
        Try
            Dim Json = NetRequestByLoader("https://ipwho.is/", 8000, IsJson:=True, SimulateBrowserHeaders:=True).DeserializeJson(Of JObject)
            Dim Country = If(Json("country_code")?.ToString, Json("country")?.ToString)
            If Not String.IsNullOrWhiteSpace(Country) Then Return Country
        Catch ex As Exception
            Logger.Warn(ex, "查询 IP 地区失败（ipwho.is）")
        End Try
        Try
            Dim Country = NetRequestByLoader("https://ipinfo.io/json", 8000, IsJson:=True, SimulateBrowserHeaders:=True).DeserializeJson(Of JObject)()("country")?.ToString
            If Not String.IsNullOrWhiteSpace(Country) Then Return Country
        Catch ex As Exception
            Logger.Warn(ex, "查询 IP 地区失败（ipinfo.io）")
        End Try
        Return Nothing
    End Function

    Private Shared Function IsChinaCountry(Country As String) As Boolean
        If String.IsNullOrWhiteSpace(Country) Then Return False
        Dim Value = Country.Trim
        Return Value.Equals("CN", StringComparison.OrdinalIgnoreCase) OrElse
               Value.Contains("中国") OrElse Value.Contains("China", StringComparison.OrdinalIgnoreCase)
    End Function

End Class
