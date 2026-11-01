Public Class PageSetupSystem
    Inherits MyPageRight

    Private IsLoadedOnce As Boolean = False

    Public Sub New()
        InitControls()
    End Sub

    Private Sub PageSetupSystem_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        '重复加载部分
        PanBack.ScrollToHome()

        '非重复加载部分
        If IsLoadedOnce Then Return
        IsLoadedOnce = True

        AniControlEnabled += 1
        Reload()
        SliderLoad()
        AniControlEnabled -= 1
    End Sub

    Public Sub Reload()
        SettingService.RefreshSettings(Me)
    End Sub

    Public Sub Reset()
        Try
            SettingService.ResetSettings(Me)
            Logger.Info("已初始化启动器页设置")
            Hint("已初始化启动器页设置！", HintType.Green, False)
        Catch ex As Exception
            Logger.Error(ex, "初始化启动器页设置失败", LogBehavior.Alert)
        End Try
        Reload()
    End Sub

    '滑动条

    Private Sub SliderLoad()
        SliderDownloadThread.GetHintText = Function(v) CStr(CInt(v) + 1)
        SliderDownloadSpeed.GetHintText =
        Function(v)
            Dim i = CInt(v)
            Select Case i
                Case Is <= 14
                    Return ((i + 1) * 0.1).ToString("0.0") & " M/s"
                Case Is <= 31
                    Return ((i - 11) * 0.5).ToString("0.0") & " M/s"
                Case Is <= 41
                    Return (i - 21).ToString() & " M/s"
                Case Else
                    Return "无限制"
            End Select
        End Function
        SliderDebugAnim.GetHintText = Function(v) If(CInt(v) > 29, "关闭", (CInt(v) / 10.0 + 0.1).ToString("0.0") & "x")
    End Sub

    Private Sub SliderDownloadThread_PreviewChange(sender As Object, e As RouteEventArgs) Handles SliderDownloadThread.PreviewChange
        If SliderDownloadThread.Value < 100 Then Return
        If Not Settings.Get(Of Boolean)("HintDownloadThread") Then
            Settings.Set("HintDownloadThread", True)
            MyMsgBox("如果设置过多的下载线程，可能会导致下载时出现非常严重的卡顿。" & vbCrLf &
                     "一般设置 64 线程即可满足大多数下载需求，除非你知道你在干什么，否则不建议设置更多的线程数！", "警告", "我知道了", IsWarn:=True)
        End If
    End Sub

    '调试模式

    Private Sub CheckDebugMode_Change(sender As Object, user As Boolean) Handles CheckDebugMode.Change
        If AniControlEnabled = 0 Then Hint("部分调试信息将在刷新或启动器重启后切换显示！", HintType.Blue, False)
    End Sub

    ''' <summary>
    ''' 启动器是否已经是最新版？
    ''' 若返回 Nothing，则代表无更新缓存文件或出错。
    ''' </summary>
    Public Shared Function IsLauncherNewest() As Boolean?
        Try
            '确认服务器公告是否正常
            Dim ServerContent As String = If(FileUtils.TryReadAsString(Path.Combine(PathTemp, "Cache", "Notice.cfg")), "")
            If ServerContent.Split("|").Count < 3 Then Return Nothing
            '确认是否为最新
            Return ServerContent.Split("|")(If(BuildType = BuildTypes.Release, 2, 1)) <= VersionCode
        Catch ex As Exception
            Logger.Error(ex, "确认启动器更新失败")
            Return Nothing
        End Try
    End Function

#Region "导出 / 导入设置"

    Private Sub BtnSystemSettingExp_Click(sender As Object, e As PointerReleasedEventArgs) Handles BtnSystemSettingExp.Click
        Hint("该功能尚在开发中！")
    End Sub

    Private Sub BtnSystemSettingImp_Click(sender As Object, e As PointerReleasedEventArgs) Handles BtnSystemSettingImp.Click
        Hint("该功能尚在开发中！")
    End Sub

#End Region

End Class
