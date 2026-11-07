'由于包含加解密等安全信息，本文件中的部分代码已被删除

Imports Avalonia.Styling

Friend Module ModSecret

    '标注 PCL 的不同分支，仅用于替换标记
    Public Const VersionBranchMain As String = "OpenSource"
    '注册表位置
    Public Const RegFolder As String = "PCL.Avalonia"
    '用于微软登录的 ClientId。稳定版直接写入源码；快照版从环境变量读取。
#If STABLE Then
    Public ReadOnly OAuthClientId As String = "47959447-c8ad-4158-be87-83d64f9c205c"
#Else
    Public ReadOnly OAuthClientId As String = If(Environment.GetEnvironmentVariable("PCL_MS_CLIENT_ID"), "")
#End If
    'CurseForge API Key 仅在本地构建中通过不提交的 ModSecret.Private.vb 嵌入；
    '公开源码构建读取环境变量，未提供时只走 MCIM 镜像。
#If PCL_PRIVATE_SECRETS Then
    Public ReadOnly CurseForgeAPIKey As String = ModSecretPrivate.PrivateCurseForgeApiKey
#Else
    Public ReadOnly CurseForgeAPIKey As String = If(Environment.GetEnvironmentVariable("PCL_CURSEFORGE_API_KEY"), "")
#End If
    '用于匿名数据收集的腾讯云日志服务上报 URL，形如 https://{region}.cls.tencentcs.com/track?topic_id={topic_id}
    Public Const ClsBaseUrl As String = ""

#Region "网络鉴权"

    '将最新的正式版 PCL 下载到 PathTemp\Latest.exe，用于导出整合包。
    '注意：如果要自行实现这个功能，请换用另一个文件路径，以免与官方版本冲突。
    Friend Sub DownloadLatestPCL(Optional LoaderToSyncProgress As LoaderBase = Nothing)
    End Sub

#End Region

#Region "联网配置"

    ''' <summary>
    ''' 联网获取的配置信息。
    ''' 若获取失败或仍在获取中，可能为 Nothing。
    ''' </summary>
    Public ServerConfig As JObject

    Public ServerLoader As New LoaderTask(Of Integer, Integer)("PCL 配置更新", Sub() Logger.Info("已跳过更新通知检查"), Priority:=ThreadPriority.BelowNormal) With
        {.ReloadTimeout = 1000 * 60 * 60} '超时 1 小时

    Friend Function SecretCdnSign(UrlWithMark As String) As String
        If Not UrlWithMark.EndsWithF("{CDN}") Then Return UrlWithMark
        Return UrlWithMark.Replace("{CDN}", "").Replace(" ", "%20")
    End Function
    ''' <summary>
    ''' 设置 Headers 的 UA、Referer。
    ''' </summary>
    Friend Sub SecretHeadersSign(Url As String, ByRef Req As HttpRequestMessage, Optional SimulateBrowserHeaders As Boolean = False)
        If Not Req.Headers.UserAgent.Any Then
            If Url.Contains("baidupcs.com") OrElse Url.Contains("baidu.com") Then
                Req.Headers.Add("User-Agent", "LogStatistic")  '#4951
            ElseIf SimulateBrowserHeaders Then
                Req.Headers.Add("User-Agent", $"PCL2/{VersionBaseName}.{CInt(BuildType)} Mozilla/5.0 AppleWebKit/537.36 Chrome/63.0.3239.132 Safari/537.36")
            Else
                Req.Headers.Add("User-Agent", $"PCL2/{VersionBaseName}.{CInt(BuildType)}")
            End If
        End If
        If Not SimulateBrowserHeaders Then Req.Headers.Add("Referer", $"http://{VersionCode}.open.pcl2.server/")
        If (Url.Contains("api.curseforge.com") OrElse Url.Contains("forgecdn.net")) AndAlso Not String.IsNullOrEmpty(CurseForgeAPIKey) Then
            Req.Headers.Add("x-api-key", CurseForgeAPIKey)
        End If
    End Sub

#End Region

#Region "主题"

    Public Color1 As New MyColor(52, 61, 74)
    Public Color2 As New MyColor(11, 91, 203)
    Public Color3 As New MyColor(19, 112, 243)
    Public Color4 As New MyColor(72, 144, 245)
    Public Color5 As New MyColor(150, 192, 249)
    Public Color6 As New MyColor(213, 230, 253)
    Public Color7 As New MyColor(222, 236, 253)
    Public Color8 As New MyColor(234, 242, 254)
    Public ColorBg0 As New MyColor(150, 192, 249)
    Public ColorBg1 As New MyColor(190, Color7)
    Public ColorGray1 As New MyColor(64, 64, 64)
    Public ColorGray2 As New MyColor(115, 115, 115)
    Public ColorGray3 As New MyColor(140, 140, 140)
    Public ColorGray4 As New MyColor(166, 166, 166)
    Public ColorGray5 As New MyColor(204, 204, 204)
    Public ColorGray6 As New MyColor(235, 235, 235)
    Public ColorGray7 As New MyColor(240, 240, 240)
    Public ColorGray8 As New MyColor(245, 245, 245)
    Public ColorSemiTransparent As New MyColor(1, Color8)
    Public ColorCard As New MyColor(245, 255, 255, 255)

    Public ThemeNow As Integer = -1
    Public IsDarkTheme As Boolean = False
    Public ColorHue As Integer = 210, ColorSat As Integer = 85, ColorLightAdjust As Integer = 0, ColorHueTopbarDelta As OneOf(Of Integer, Integer()) = 0
    Public ThemeDontClick As Integer = 0


    Public Sub ThemeRefresh(Optional NewTheme As Integer = -1)
        Try
            ApplyThemeVariant()
            Dim Theme As Integer = If(NewTheme >= 0, NewTheme, Settings.Get(Of Integer)("UiLauncherTheme"))
            ThemeNow = Theme
            Select Case Theme
                Case 1 '甜柠青
                    ColorHue = 180 : ColorSat = 70 : ColorLightAdjust = 0 : ColorHueTopbarDelta = 0
                Case 2 '小草绿
                    ColorHue = 130 : ColorSat = 60 : ColorLightAdjust = 0 : ColorHueTopbarDelta = 0
                Case 3 '菠萝黄
                    ColorHue = 45 : ColorSat = 90 : ColorLightAdjust = 0 : ColorHueTopbarDelta = 0
                Case 4 '橡木棕
                    ColorHue = 30 : ColorSat = 45 : ColorLightAdjust = -10 : ColorHueTopbarDelta = 0
                Case Else '龙猫蓝等默认主题
                    ColorHue = 210 : ColorSat = 85 : ColorLightAdjust = 0 : ColorHueTopbarDelta = 0
            End Select

            IsDarkTheme = ResolveDarkTheme()
            If IsDarkTheme Then
                '深色版：背景取同一色相的暗色，文字与灰色提亮
                Color1 = New MyColor().FromHSL2(ColorHue, ColorSat * 0.12, 92)
                Color2 = New MyColor().FromHSL2(ColorHue, ColorSat, 48)
                Color3 = New MyColor().FromHSL2(ColorHue, ColorSat, 58)
                Color4 = New MyColor().FromHSL2(ColorHue, ColorSat, 45)
                Color5 = New MyColor().FromHSL2(ColorHue, ColorSat * 0.7, 32)
                Color6 = New MyColor().FromHSL2(ColorHue, ColorSat * 0.45, 22)
                Color7 = New MyColor().FromHSL2(ColorHue, ColorSat * 0.3, 16)
                Color8 = New MyColor().FromHSL2(ColorHue, ColorSat * 0.2, 12)
                ColorBg0 = Color4 * 0.35 + Color5 * 0.35 + Color6 * 0.3
                ColorGray1 = New MyColor(232, 232, 232)
                ColorGray2 = New MyColor(180, 180, 180)
                ColorGray3 = New MyColor(148, 148, 148)
                ColorGray4 = New MyColor(118, 118, 118)
                ColorGray5 = New MyColor(80, 80, 80)
                ColorGray6 = New MyColor(58, 58, 58)
                ColorGray7 = New MyColor(44, 44, 44)
                ColorGray8 = New MyColor(30, 30, 30)
                ColorCard = New MyColor(245, Color7)
            Else
                Color1 = New MyColor().FromHSL2(ColorHue, ColorSat * 0.2, 25 + ColorLightAdjust * 0.3)
                Color2 = New MyColor().FromHSL2(ColorHue, ColorSat, 45 + ColorLightAdjust)
                Color3 = New MyColor().FromHSL2(ColorHue, ColorSat, 55 + ColorLightAdjust)
                Color4 = New MyColor().FromHSL2(ColorHue, ColorSat, 65 + ColorLightAdjust)
                Color5 = New MyColor().FromHSL2(ColorHue, ColorSat, 80 + ColorLightAdjust * 0.4)
                Color6 = New MyColor().FromHSL2(ColorHue, ColorSat, 91 + ColorLightAdjust * 0.1)
                Color7 = New MyColor().FromHSL2(ColorHue, ColorSat, 95)
                Color8 = New MyColor().FromHSL2(ColorHue, ColorSat, 97)
                ColorBg0 = Color4 * 0.4 + Color5 * 0.4 + ColorGray4 * 0.2
                ColorGray1 = New MyColor(64, 64, 64)
                ColorGray2 = New MyColor(115, 115, 115)
                ColorGray3 = New MyColor(140, 140, 140)
                ColorGray4 = New MyColor(166, 166, 166)
                ColorGray5 = New MyColor(204, 204, 204)
                ColorGray6 = New MyColor(235, 235, 235)
                ColorGray7 = New MyColor(240, 240, 240)
                ColorGray8 = New MyColor(245, 245, 245)
                ColorCard = New MyColor(245, 255, 255, 255)
            End If
            ColorBg1 = New MyColor(190, Color7)
            ColorSemiTransparent = New MyColor(1, Color8)

            Application.Current.Resources("ColorBrush1") = New SolidColorBrush(Color1)
            Application.Current.Resources("ColorBrush2") = New SolidColorBrush(Color2)
            Application.Current.Resources("ColorBrush3") = New SolidColorBrush(Color3)
            Application.Current.Resources("ColorBrush4") = New SolidColorBrush(Color4)
            Application.Current.Resources("ColorBrush5") = New SolidColorBrush(Color5)
            Application.Current.Resources("ColorBrush6") = New SolidColorBrush(Color6)
            Application.Current.Resources("ColorBrush7") = New SolidColorBrush(Color7)
            Application.Current.Resources("ColorBrush8") = New SolidColorBrush(Color8)
            Application.Current.Resources("ColorBrushBg0") = New SolidColorBrush(ColorBg0)
            Application.Current.Resources("ColorBrushBg1") = New SolidColorBrush(ColorBg1)
            Application.Current.Resources("ColorBrushGray1") = New SolidColorBrush(ColorGray1)
            Application.Current.Resources("ColorBrushGray2") = New SolidColorBrush(ColorGray2)
            Application.Current.Resources("ColorBrushGray3") = New SolidColorBrush(ColorGray3)
            Application.Current.Resources("ColorBrushGray4") = New SolidColorBrush(ColorGray4)
            Application.Current.Resources("ColorBrushGray5") = New SolidColorBrush(ColorGray5)
            Application.Current.Resources("ColorBrushGray6") = New SolidColorBrush(ColorGray6)
            Application.Current.Resources("ColorBrushGray7") = New SolidColorBrush(ColorGray7)
            Application.Current.Resources("ColorBrushGray8") = New SolidColorBrush(ColorGray8)
            Application.Current.Resources("ColorBrushCard") = New SolidColorBrush(ColorCard)
            Application.Current.Resources("ColorBrushSemiTransparent") = New SolidColorBrush(ColorSemiTransparent)
            '深色模式下半透明白会叠成灰色板；颜色动画以它为起点时，RGB 与页面背景不一致会在插值中途闪一下，
            '因此取背景同色相的 0 透明度
            If IsDarkTheme Then
                Application.Current.Resources("ColorBrushHalfWhite") = New SolidColorBrush(New MyColor(0, Color8))
            Else
                Application.Current.Resources("ColorBrushHalfWhite") = New SolidColorBrush(Color.Parse("#55FFFFFF"))
            End If
            '透明色取背景同色相的 0 透明度，避免颜色动画从透明白插值经过灰色时闪一下
            Application.Current.Resources("ColorBrushTransparent") = New SolidColorBrush(New MyColor(0, Color8))
            Application.Current.Resources("ColorBrushBackgroundTransparentSidebar") = New SolidColorBrush(New MyColor(241, Color8))
            Application.Current.Resources("ColorObject1") = CType(Color1, Color)
            Application.Current.Resources("ColorObject2") = CType(Color2, Color)
            Application.Current.Resources("ColorObject3") = CType(Color3, Color)
            Application.Current.Resources("ColorObject4") = CType(Color4, Color)
            Application.Current.Resources("ColorObject5") = CType(Color5, Color)
            Application.Current.Resources("ColorObject6") = CType(Color6, Color)
            Application.Current.Resources("ColorObject7") = CType(Color7, Color)
            Application.Current.Resources("ColorObject8") = CType(Color8, Color)
            Application.Current.Resources("ColorObjectBg0") = CType(ColorBg0, Color)
            Application.Current.Resources("ColorObjectBg1") = CType(ColorBg1, Color)
            Application.Current.Resources("ColorObjectGray1") = CType(ColorGray1, Color)
            Application.Current.Resources("ColorObjectGray2") = CType(ColorGray2, Color)
            Application.Current.Resources("ColorObjectGray3") = CType(ColorGray3, Color)
            Application.Current.Resources("ColorObjectGray4") = CType(ColorGray4, Color)
            Application.Current.Resources("ColorObjectGray5") = CType(ColorGray5, Color)
            Application.Current.Resources("ColorObjectGray6") = CType(ColorGray6, Color)
            Application.Current.Resources("ColorObjectGray7") = CType(ColorGray7, Color)
            Application.Current.Resources("ColorObjectGray8") = CType(ColorGray8, Color)
            ThemeRefreshMain()
        Catch ex As Exception
            Logger.Error(ex, "刷新主题颜色失败", LogBehavior.Toast)
        End Try
    End Sub

    ''' <summary>
    ''' 让 Fluent 提供的右键菜单、提示框等弹出层与应用内深浅色保持一致。
    ''' </summary>
    Private Sub ApplyThemeVariant()
        Try
            If Application.Current Is Nothing Then Return
            Select Case Settings.Get(Of Integer)("UiLauncherThemeMode")
                Case 1 : Application.Current.RequestedThemeVariant = ThemeVariant.Light
                Case 2 : Application.Current.RequestedThemeVariant = ThemeVariant.Dark
                Case Else : Application.Current.RequestedThemeVariant = ThemeVariant.Default
            End Select
        Catch ex As Exception
            Logger.Warn(ex, "应用主题变体失败")
        End Try
    End Sub

    Private Function ResolveDarkTheme() As Boolean
        Select Case Settings.Get(Of Integer)("UiLauncherThemeMode")
            Case 1 : Return False
            Case 2 : Return True
            Case Else
                Return Application.Current IsNot Nothing AndAlso Application.Current.ActualThemeVariant = ThemeVariant.Dark
        End Select
    End Function

    ''' <summary>
    ''' 系统深浅色切换时调用。仅在“跟随系统”模式下刷新。
    ''' </summary>
    Friend Sub ThemeVariantChanged()
        If Settings.Get(Of Integer)("UiLauncherThemeMode") = 0 Then ThemeRefresh()
    End Sub

    Public Sub ThemeRefreshMain()
        RunInUi(
        Sub()
            If FrmMain Is Nothing OrElse Not FrmMain.IsLoaded Then Return
            '顶部条背景
            Dim TopDelta As Integer() = ColorHueTopbarDelta.Switch(Function(d) New Integer() {-d, 0, d}, Function(d) d)
            Dim TopLight As Integer = If(IsDarkTheme, 40, 48)
            Dim TopBrush As New LinearGradientBrush With {.EndPoint = New RelativePoint(1, 0, RelativeUnit.Relative), .StartPoint = New RelativePoint(0, 0, RelativeUnit.Relative)}
            TopBrush.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor().FromHSL2(ColorHue + TopDelta(0), ColorSat, TopLight + ColorLightAdjust)})
            TopBrush.GradientStops.Add(New GradientStop With {.Offset = 0.5, .Color = New MyColor().FromHSL2(ColorHue + TopDelta(1), ColorSat, TopLight + 6 + ColorLightAdjust)})
            TopBrush.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor().FromHSL2(ColorHue + TopDelta(2), ColorSat, TopLight + ColorLightAdjust)})
            FrmMain.PanTitle.Background = TopBrush
            '重新应用标题栏按钮状态：勾选按钮的白底与强调色文字不随主题资源替换而丢失
            For Each Btn In FrmMain.PanTitleSelect.Children.OfType(Of MyRadioButton)()
                Btn.RefreshColor(False)
            Next
            '主页面背景
            If Settings.Get(Of Boolean)("UiBackgroundColorful") Then
                Dim FormBrush As New LinearGradientBrush With {.EndPoint = New RelativePoint(0.1, 1, RelativeUnit.Relative), .StartPoint = New RelativePoint(0.9, 0, RelativeUnit.Relative)}
                If IsDarkTheme Then
                    FormBrush.GradientStops.Add(New GradientStop With {.Offset = -0.1, .Color = New MyColor().FromHSL2(ColorHue - 15, ColorSat * 0.4, 12)})
                    FormBrush.GradientStops.Add(New GradientStop With {.Offset = 0.4, .Color = New MyColor().FromHSL2(ColorHue, ColorSat * 0.4, 13)})
                    FormBrush.GradientStops.Add(New GradientStop With {.Offset = 1.1, .Color = New MyColor().FromHSL2(ColorHue + 15, ColorSat * 0.4, 12)})
                Else
                    FormBrush.GradientStops.Add(New GradientStop With {.Offset = -0.1, .Color = New MyColor().FromHSL2(ColorHue - 15, ColorSat * 0.8, 91)})
                    FormBrush.GradientStops.Add(New GradientStop With {.Offset = 0.4, .Color = New MyColor().FromHSL2(ColorHue, ColorSat * 0.8, 91)})
                    FormBrush.GradientStops.Add(New GradientStop With {.Offset = 1.1, .Color = New MyColor().FromHSL2(ColorHue + 15, ColorSat * 0.8, 91)})
                End If
                FrmMain.PanForm.Background = FormBrush
            Else
                FrmMain.PanForm.Background = CType(ColorGray8, Brush)
            End If
        End Sub)
    End Sub
    Friend Sub ThemeCheckAll(EffectSetup As Boolean)
    End Sub
    Friend Function ThemeCheckOne(Id As Integer) As Boolean
        Return True
    End Function
    Friend Function ThemeUnlock(Id As Integer, Optional ShowDoubleHint As Boolean = True, Optional UnlockHint As String = Nothing) As Boolean
        Return False
    End Function

#End Region

#Region "更新"

    Friend Sub UpdateCheckByButton()
        Logger.Info("当前版本不支持启动器更新功能")
    End Sub

    Friend IsUpdateWaitingRestart As Boolean = False
    Public Sub UpdateRestart(TriggerRestartAndByEnd As Boolean)
    End Sub
    Public Sub UpdateReplace(ProcessId As Integer, OldFileName As String, NewFileName As String, TriggerRestart As Boolean)
    End Sub

#End Region

    ''' <summary>
    ''' 获取设备识别码。
    ''' </summary>
    Friend Function GetIdentify() As String
        Return "0000-0000-0000-0000"
    End Function

End Module
