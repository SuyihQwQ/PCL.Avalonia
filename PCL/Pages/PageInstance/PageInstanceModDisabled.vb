''' <summary>
''' 不可安装 Mod 的版本的 Mod 管理占位页面。
''' </summary>
Public Class PageInstanceModDisabled
    Inherits MyPageRight

    Public Sub New()
        InitControls()
    End Sub

    Private Sub BtnDownload_Click(sender As Object, e As PointerReleasedEventArgs) Handles BtnDownload.Click
        FrmMain.PageChange(FormMain.PageType.Download, FormMain.PageSubType.DownloadInstall)
    End Sub
    Private Sub BtnVersion_Click(sender As Object, e As PointerReleasedEventArgs) Handles BtnVersion.Click
        FrmMain.PageChange(FormMain.PageType.Launch) '在版本选择页面选定版本的时候只会返回一层，因此如果不先锚定 Launch，在选择版本后会回退到版本设置的这个页面
        FrmMain.PageChange(FormMain.PageType.InstanceSelect)
    End Sub

    Public Sub BtnDownload_Loaded() Handles BtnDownload.Loaded
        Dim NewVisibility = Not ((Settings.Get(Of Boolean)("UiHiddenPageDownload") AndAlso Not PageSetupUI.HiddenForceShow) OrElse If(FrmSelectRight Is Nothing, False, FrmSelectRight.ShowHidden))
        If BtnDownload.IsVisible <> NewVisibility Then
            BtnDownload.IsVisible = NewVisibility
            PanMain.TriggerForceResize()
        End If
    End Sub

End Class
