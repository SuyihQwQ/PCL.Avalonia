Public Class PageOtherAbout
    Inherits MyPageRight

    Private Shadows IsLoaded As Boolean = False

    Public Sub New()
        InitControls()
    End Sub

    Private Sub PageOtherAbout_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        '重复加载部分
        PanBack.ScrollToHome()

        '非重复加载部分
        If IsLoaded Then Return
        IsLoaded = True

        ItemAboutPcl.Info = ItemAboutPcl.Info.Replace("%VERSION%", VersionDisplay).Replace("%VERSIONCODE%", VersionCode.ToString()).Replace("%BRANCH%", CInt(BuildType).ToString())
    End Sub

    Public Shared Sub CopyIdentify()
        ClipboardSet(Identify)
    End Sub

End Class
