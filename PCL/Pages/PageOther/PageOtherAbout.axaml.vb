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
        If BuildType = BuildTypes.Debug Then
            BtnDonateDonate.IsVisible = False
            BtnDonateOutput.IsVisible = True
        End If
    End Sub

    Public Shared Sub CopyIdentify()
        ClipboardSet(Identify)
    End Sub

    Private Sub BtnDonateCopy_Click(sender As Object, e As PointerReleasedEventArgs) Handles BtnDonateCopy.Click
        CopyIdentify()
    End Sub

    Private Sub BtnDonateInput_Click(sender As Object, e As PointerReleasedEventArgs) Handles BtnDonateInput.Click
        InputPotatoCode(False)
    End Sub

    Private Sub BtnDonateOutput_Click(sender As Object, e As PointerReleasedEventArgs) Handles BtnDonateOutput.Click
        GeneratePotatoCode()
    End Sub

    Private Sub BtnAboutUpdate_Click(sender As Object, e As PointerReleasedEventArgs) Handles BtnAboutUpdate.Click
        UpdateCheckByButton()
    End Sub

End Class
