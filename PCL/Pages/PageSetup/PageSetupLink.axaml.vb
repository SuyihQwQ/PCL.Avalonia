Public Class PageSetupLink
    Inherits MyPageRight

    Private IsLoadedOnce As Boolean = False

    Public Sub New()
        InitControls()
    End Sub

    Private Sub PageSetupLink_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        '重复加载部分
        PanBack.ScrollToHome()

        '非重复加载部分
        If IsLoadedOnce Then Return
        IsLoadedOnce = True

        AniControlEnabled += 1
        Reload()
        AniControlEnabled -= 1
    End Sub

    Public Sub Reload()
        SettingService.RefreshSettings(Me)
    End Sub

    '初始化
    Public Sub Reset()
        Try
            SettingService.ResetSettings(Me)
            Logger.Info("已初始化联机页设置")
            Hint("已初始化联机页设置！", HintType.Green, False)
        Catch ex As Exception
            Logger.Error(ex, "初始化联机页设置失败", LogBehavior.Alert)
        End Try
        Reload()
    End Sub

    Private Sub BtnLinkContribute_Click(sender As Object, e As PointerReleasedEventArgs) Handles BtnLinkContribute.Click
        OpenWebsite("https://shimo.im/docs/qKPttVvXKqPD8YDC#anchor-Dupo")
    End Sub

    Private Sub BtnLinkStatus_Click(sender As Object, e As PointerReleasedEventArgs) Handles BtnLinkStatus.Click
        OpenWebsite("https://uptime.easytier.cn/")
    End Sub

End Class
