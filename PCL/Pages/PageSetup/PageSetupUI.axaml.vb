Public Class PageSetupUI
    Inherits MyPageRight

    ''' <summary>
    ''' 当前显示的个性化设置页面。尚未创建时为 Nothing。
    ''' </summary>
    Public Shared Property Instance As PageSetupUI

    Private IsLoadedOnce As Boolean = False

    Private Shared ReadOnly Property PicturesPath As String
        Get
            Return Path.Combine(PathData, "Pictures")
        End Get
    End Property
    Private Shared ReadOnly Property MusicsPath As String
        Get
            Return Path.Combine(PathData, "Musics")
        End Get
    End Property
    Private Shared ReadOnly Property LogoPath As String
        Get
            Return Path.Combine(PathData, "Logo.png")
        End Get
    End Property
    Private Shared ReadOnly Property CustomPath As String
        Get
            Return Path.Combine(PathData, "Custom.xaml")
        End Get
    End Property

    Public Sub New()
        Instance = Me
        InitControls()
    End Sub

    Private Sub PageSetupUI_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        '重复加载部分
        PanBack.ScrollToHome()
        ThemeCheckAll(True)
        If ThemeDontClick <> 0 Then
            Dim NewText As String = If(ThemeDontClick = 1, "眼瞎白", "真·滑稽彩")
            For Each Control In PanLauncherTheme.Children.OfType(Of MyRadioBox)()
                If Control.IsEnabled Then Control.Text = NewText
            Next
        End If

        AniControlEnabled += 1
        Refresh()
        AniControlEnabled -= 1

        HiddenRefresh()

        '非重复加载部分
        If IsLoadedOnce Then Return
        IsLoadedOnce = True

        SliderLoad()
    End Sub

    Public Sub Refresh()
        Try
            SettingService.RefreshSettings(Me)
            BackgroundRefresh(False, False)

            '标题栏
            CheckLogoLeft.IsVisible = RadioLogoType0.Checked
            PanLogoText.IsVisible = RadioLogoType2.Checked
            PanLogoChange.IsVisible = RadioLogoType3.Checked
            CardLogo.TriggerForceResize()

            '背景音乐
            MusicRefreshUI()

            '主页
            OnMainPageTypeChanged()
        Catch ex As NullReferenceException
            Logger.Error(ex, "个性化设置项存在异常，已被自动重置", LogBehavior.Alert)
            Reset()
        Catch ex As Exception
            Logger.Error(ex, "重载个性化设置时出错")
        End Try
    End Sub

    Public Sub Reset()
        Try
            SettingService.ResetSettings(Me)
            Logger.Info("已初始化个性化设置！")
            Hint("已初始化个性化设置", HintType.Green, False)
        Catch ex As Exception
            Logger.Error(ex, "初始化个性化设置失败", LogBehavior.Alert)
        End Try
        Refresh()
    End Sub

    '背景图片

    Private Sub BtnBackgroundOpen_Click(sender As Object, e As PointerReleasedEventArgs) Handles BtnBackgroundOpen.Click
        OpenExplorer(PicturesPath)
    End Sub

    Private Sub BtnBackgroundRefresh_Click(sender As Object, e As PointerReleasedEventArgs) Handles BtnBackgroundRefresh.Click
        BackgroundRefresh(True, True)
    End Sub

    ''' <summary>
    ''' 根据背景图片数量更新设置页 UI。
    ''' </summary>
    Public Sub BackgroundRefreshUI(Show As Boolean, Count As Integer)
        If Instance Is Nothing Then Return
        If PanBackgroundOpacity Is Nothing Then Return
        PanBackgroundOpacity.IsVisible = Show
        PanBackgroundBlur.IsVisible = Show
        PanBackgroundSuit.IsVisible = Show
        BtnBackgroundClear.IsVisible = Show
        CardBackground.Title = If(Show, $"背景图片（{Count} 张）", "背景图片")
        CardBackground.TriggerForceResize()
    End Sub

    Private Sub BtnBackgroundClear_Click(sender As Object, e As PointerReleasedEventArgs) Handles BtnBackgroundClear.Click
        If MyMsgBox("即将删除背景图片文件夹中的所有文件。" & vbCrLf & "此操作不可撤销，是否确定？", "警告",, "取消", IsWarn:=True) = 1 Then
            DirectoryUtils.Delete(PicturesPath)
            BackgroundRefresh(False, True)
            Hint("背景图片已清空！", HintType.Green)
        End If
    End Sub

    ''' <summary>
    ''' 刷新背景图片及设置页 UI。
    ''' </summary>
    ''' <param name="IsHint">是否显示刷新提示。</param>
    ''' <param name="Refresh">是否刷新图片显示。</param>
    Public Shared Sub BackgroundRefresh(IsHint As Boolean, Refresh As Boolean)
        Try
            '获取可用的图片文件
            DirectoryUtils.Create(PicturesPath)
            Dim Pic As New List(Of String)
            For Each File In DirectoryUtils.EnumerateFiles(PicturesPath, True)
                Dim Extension As String = PathUtils.GetExtension(File)
                If Extension <> "ini" AndAlso Extension <> "db" Then Pic.Add(File) '文件夹可能会被加入 .ini 和 thumbs.db
            Next
            '加载
            If Not Pic.Any() Then
                If Refresh Then
                    If FrmMain IsNot Nothing AndAlso Not FrmMain.ImgBack.IsVisible Then
                        If IsHint Then Hint("未检测到可用背景图片！", HintType.Red)
                    Else
                        FrmMain?.ClearBackgroundImage()
                        If IsHint Then Hint("背景图片已清除！", HintType.Green)
                    End If
                End If
                Instance?.BackgroundRefreshUI(False, 0)
            Else
                If Refresh Then
                    Dim Address As String = RandomOne(Pic)
                    Try
                        Logger.Info($"加载背景图片：{Address}")
                        FrmMain?.SetBackgroundImage(New MyBitmap(Address))
                        If IsHint Then Hint("背景图片已刷新：" & PathUtils.GetLastPart(Address), HintType.Green, False)
                    Catch ex As Exception
                        If ex.Message.Contains("参数无效") Then
                            Logger.Error($"刷新背景图片失败，该图片文件可能并非标准格式。{vbCrLf}你可以尝试使用画图打开该文件并重新保存，这会让图片变为标准格式。{vbCrLf}文件：{Address}", LogBehavior.Alert)
                        Else
                            Logger.Error(ex, $"刷新背景图片失败（{Address}）", LogBehavior.Alert)
                        End If
                    End Try
                End If
                Instance?.BackgroundRefreshUI(True, Pic.Count)
            End If
        Catch ex As Exception
            Logger.Error(ex, "刷新背景图片时出现未知错误")
        End Try
    End Sub

    '顶部栏

    Private Sub BtnLogoChange_Click(sender As Object, e As PointerReleasedEventArgs) Handles BtnLogoChange.Click
        Dim FileName As String = Dialogs.SelectFile("选择图片", False, filter:={({"png", "jpg", "jpeg", "gif", "webp"}, "常用图片文件")}).FirstOrDefault()
        If String.IsNullOrEmpty(FileName) Then Return
        Try
            FileUtils.Copy(FileName, LogoPath)
            FrmMain.RefreshTitleLogo()
        Catch ex As Exception
            Logger.Error(ex, "设置标题栏图片失败", LogBehavior.Alert)
            FrmMain.RefreshTitleLogo()
        End Try
    End Sub

    Private Sub RadioLogoType3_PreviewCheck(sender As Object, e As RouteEventArgs) Handles RadioLogoType3.PreviewCheck
        If Not (AniControlEnabled = 0 AndAlso e.RaiseByMouse) Then Return
Refresh:
        '已有图片则不再选择
        If FileUtils.Exists(LogoPath) Then
            FrmMain.RefreshTitleLogo()
            Return
        End If
        '没有图片则要求选择
        Dim FileName As String = Dialogs.SelectFile("选择图片", False, filter:={({"png", "jpeg", "jpg", "gif", "webp"}, "常用图片文件")}).FirstOrDefault()
        If String.IsNullOrEmpty(FileName) Then
            e.Handled = True
        Else
            Try
                FileUtils.Copy(FileName, LogoPath)
                GoTo Refresh
            Catch ex As Exception
                Logger.Error(ex, "复制标题栏图片失败", LogBehavior.Alert)
            End Try
        End If
    End Sub

    Private Sub BtnLogoDelete_Click(sender As Object, e As PointerReleasedEventArgs) Handles BtnLogoDelete.Click
        Try
            FileUtils.Delete(LogoPath)
            RadioLogoType1.SetChecked(True, True)
            FrmMain.RefreshTitleLogo()
            Hint("标题栏图片已清空！", HintType.Green)
        Catch ex As Exception
            Logger.Error(ex, "清空标题栏图片失败", LogBehavior.Alert)
        End Try
    End Sub

    '背景音乐

    Private Sub BtnMusicOpen_Click(sender As Object, e As PointerReleasedEventArgs) Handles BtnMusicOpen.Click
        OpenExplorer(MusicsPath)
    End Sub

    Private Sub BtnMusicRefresh_Click(sender As Object, e As PointerReleasedEventArgs) Handles BtnMusicRefresh.Click
        MusicRefreshPlay(True)
    End Sub

    Public Sub MusicRefreshUI()
        If Instance Is Nothing Then Return
        If PanMusicVolume Is Nothing Then Return
        If MusicAllList IsNot Nothing AndAlso MusicAllList.Any() Then
            PanMusicVolume.IsVisible = True
            PanMusicDetail.IsVisible = True
            BtnMusicClear.IsVisible = True
            CardMusic.Title = "背景音乐（" &
                DirectoryUtils.EnumerateFiles(MusicsPath, True).Count(Function(f) Not {"ini", "jpg", "txt", "cfg", "lrc", "db", "png"}.Contains(PathUtils.GetExtension(f))) &
                " 首）"
        Else
            PanMusicVolume.IsVisible = False
            PanMusicDetail.IsVisible = False
            BtnMusicClear.IsVisible = False
            CardMusic.Title = "背景音乐"
        End If
        CardMusic.TriggerForceResize()
    End Sub

    Private Sub BtnMusicClear_Click(sender As Object, e As PointerReleasedEventArgs) Handles BtnMusicClear.Click
        If MyMsgBox("即将删除背景音乐文件夹中的所有文件。" & vbCrLf & "此操作不可撤销，是否确定？", "警告",, "取消", IsWarn:=True) = 1 Then
            RunInThread(
            Sub()
                Hint("正在删除背景音乐……")
                '停止播放音乐
                MusicNAudio = Nothing
                MusicWaitingList = New List(Of String)
                MusicAllList = New List(Of String)
                Thread.Sleep(200)
                '删除文件
                Try
                    DirectoryUtils.Delete(MusicsPath)
                    Hint("背景音乐已删除！", HintType.Green)
                Catch ex As Exception
                    Logger.Error(ex, "删除背景音乐失败", LogBehavior.Alert)
                End Try
                Try
                    DirectoryUtils.Create(MusicsPath)
                    RunInUi(Sub() MusicRefreshPlay(False))
                Catch ex As Exception
                    Logger.Error(ex, "重建背景音乐文件夹失败", LogBehavior.Alert)
                End Try
            End Sub)
        End If
    End Sub

    Private Sub CheckMusicStart_Change(sender As Object, user As Boolean) Handles CheckMusicStart.Change
        If AniControlEnabled <> 0 Then Return
        If CheckMusicStart.Checked Then CheckMusicStop.Checked = False
    End Sub

    Private Sub CheckMusicStop_Change(sender As Object, user As Boolean) Handles CheckMusicStop.Change
        If AniControlEnabled <> 0 Then Return
        If CheckMusicStop.Checked Then CheckMusicStart.Checked = False
    End Sub

    '主页

    Private Sub BtnCustomFile_Click(sender As Object, e As PointerReleasedEventArgs) Handles BtnCustomFile.Click
        Try
            If FileUtils.Exists(CustomPath) Then
                If MyMsgBox("当前已存在布局文件，继续生成教学文件将会覆盖现有布局文件！", "覆盖确认", "继续", "取消", IsWarn:=True) = 2 Then Return
            End If
            ExtractResources(CustomPath, "Custom.xaml.txt")
            Hint("教学文件已生成！", HintType.Green)
            OpenExplorer(CustomPath)
        Catch ex As Exception
            Logger.Error(ex, "生成教学文件失败")
        End Try
    End Sub

    Private Sub BtnCustomRefresh_Click(sender As Object, e As PointerReleasedEventArgs) Handles BtnCustomRefresh.Click
        FrmLaunchRight.ForceRefresh()
        Hint("已刷新主页！", HintType.Green)
    End Sub

    Private Sub BtnCustomTutorial_Click(sender As Object, e As PointerReleasedEventArgs) Handles BtnCustomTutorial.Click
        MyMsgBox("1. 点击 生成教学文件 按钮，这会在 PCL 文件夹下生成 Custom.xaml 布局文件。" & vbCrLf &
                 "2. 使用记事本等工具打开这个文件并进行修改，修改完记得保存。" & vbCrLf &
                 "3. 点击 刷新主页 按钮，查看主页现在长啥样了。" & vbCrLf &
                 vbCrLf &
                 "你可以在生成教学文件后直接刷新主页，对照着进行修改，更有助于理解。" & vbCrLf &
                 "直接将主页文件拖进 PCL 窗口也可以快捷加载。", "主页自定义教程")
    End Sub

    Public Shared Sub OnMainPageTypeChanged()
        If Instance Is Nothing Then Return
        Select Case CInt(Settings.Get(Of Integer)("UiCustomType"))
            Case 0 '无
                Instance.PanCustomPreset.IsVisible = False
                Instance.PanCustomLocal.IsVisible = False
                Instance.PanCustomNet.IsVisible = False
                Instance.HintCustom.IsVisible = False
                Instance.HintCustomWarn.IsVisible = False
            Case 1 '本地
                Instance.PanCustomPreset.IsVisible = False
                Instance.PanCustomLocal.IsVisible = True
                Instance.PanCustomNet.IsVisible = False
                Instance.HintCustom.IsVisible = True
                Instance.HintCustomWarn.IsVisible = Not Settings.Get(Of Boolean)("HintCustomWarn")
                Instance.HintCustom.Text = $"从 PCL 文件夹下的 Custom.xaml 读取主页内容。{vbCrLf}你可以手动编辑该文件，向主页添加文本、图片、常用网站、快捷启动等功能。"
                CustomEventService.SetEventType(Instance.HintCustom, CustomEvent.EventType.None)
                CustomEventService.SetEventData(Instance.HintCustom, "")
            Case 2 '联网
                Instance.PanCustomPreset.IsVisible = False
                Instance.PanCustomLocal.IsVisible = False
                Instance.PanCustomNet.IsVisible = True
                Instance.HintCustom.IsVisible = True
                Instance.HintCustomWarn.IsVisible = Not Settings.Get(Of Boolean)("HintCustomWarn")
                Instance.HintCustom.Text = $"从指定网址联网获取主页内容。服主也可以用于动态更新服务器公告。{vbCrLf}如果你制作了稳定运行的联网主页，可以点击这条提示投稿，若合格即可加入预设！"
                CustomEventService.SetEventType(Instance.HintCustom, CustomEvent.EventType.打开网页)
                CustomEventService.SetEventData(Instance.HintCustom, "https://github.com/Meloong-Git/PCL/discussions/2528")
            Case 3 '预设
                Instance.PanCustomPreset.IsVisible = True
                Instance.PanCustomLocal.IsVisible = False
                Instance.PanCustomNet.IsVisible = False
                Instance.HintCustom.IsVisible = False
                Instance.HintCustomWarn.IsVisible = False
        End Select
        Instance.CardCustom.TriggerForceResize()
    End Sub

    Private Sub BtnCustomFolder_Click(sender As Object, e As PointerReleasedEventArgs) Handles BtnCustomFolder.Click
        If FileUtils.Exists(CustomPath) Then
            OpenExplorer(CustomPath)
        Else
            OpenExplorer(PathData)
        End If
    End Sub

    '主题

#Region "功能隐藏"

    Private Shared _HiddenForceShow As Boolean = False
    ''' <summary>
    ''' 是否强制显示被禁用的功能。
    ''' </summary>
    Public Shared Property HiddenForceShow As Boolean
        Get
            Return _HiddenForceShow
        End Get
        Set(value As Boolean)
            _HiddenForceShow = value
            HiddenRefresh()
        End Set
    End Property

    ''' <summary>
    ''' 更新功能隐藏带来的显示变化。
    ''' </summary>
    Public Shared Sub HiddenRefresh()
        If FrmMain Is Nothing OrElse FrmMain.PanTitleSelect Is Nothing Then Return
        Try
            '顶部栏
            If Not HiddenForceShow AndAlso Settings.Get(Of Boolean)("UiHiddenPageDownload") AndAlso Settings.Get(Of Boolean)("UiHiddenPageLink") AndAlso Settings.Get(Of Boolean)("UiHiddenPageSetup") AndAlso Settings.Get(Of Boolean)("UiHiddenPageOther") Then
                '顶部栏已被全部隐藏
                FrmMain.PanTitleSelect.IsVisible = False
            Else
                '顶部栏未被全部隐藏
                FrmMain.PanTitleSelect.IsVisible = True
                FrmMain.BtnTitleSelect1.IsVisible = HiddenForceShow OrElse Not Settings.Get(Of Boolean)("UiHiddenPageDownload")
                FrmMain.BtnTitleSelect2.IsVisible = False
                FrmMain.BtnTitleSelect3.IsVisible = HiddenForceShow OrElse Not Settings.Get(Of Boolean)("UiHiddenPageSetup")
                FrmMain.BtnTitleSelect4.IsVisible = HiddenForceShow OrElse Not Settings.Get(Of Boolean)("UiHiddenPageOther")
            End If
            '功能
            If Instance IsNot Nothing Then
                Instance.CardSwitch.IsVisible = HiddenForceShow OrElse Not Settings.Get(Of Boolean)("UiHiddenFunctionHidden")
                Instance.CardSwitch.Title = If(HiddenForceShow, "功能隐藏（已暂时关闭，按 F12 以重新启用）", "功能隐藏")
            End If
            '设置子页面
            FrmMain.FrmSetupLeft?.RefreshHidden()
            '更多子页面
            Dim OtherAvaliableCount As Integer = 0
            If Not Settings.Get(Of Boolean)("UiHiddenOtherHelp") Then OtherAvaliableCount += 1
            If Not Settings.Get(Of Boolean)("UiHiddenOtherAbout") Then OtherAvaliableCount += 1
            If Not Settings.Get(Of Boolean)("UiHiddenOtherTest") Then OtherAvaliableCount += 1
            If Not Settings.Get(Of Boolean)("UiHiddenOtherFeedback") Then OtherAvaliableCount += 1
            If Not Settings.Get(Of Boolean)("UiHiddenOtherVote") Then OtherAvaliableCount += 1
            If FrmMain.FrmOtherLeft IsNot Nothing Then
                FrmMain.FrmOtherLeft.RefreshHidden()
            End If
            If OtherAvaliableCount = 1 AndAlso Not HiddenForceShow Then
                If Not Settings.Get(Of Boolean)("UiHiddenOtherHelp") Then
                    FrmMain.BtnTitleSelect4.Text = "帮助"
                ElseIf Not Settings.Get(Of Boolean)("UiHiddenOtherAbout") Then
                    FrmMain.BtnTitleSelect4.Text = "关于"
                Else
                    FrmMain.BtnTitleSelect4.Text = "百宝箱"
                End If
            Else
                FrmMain.BtnTitleSelect4.Text = "更多"
            End If
        Catch ex As Exception
            Logger.Error(ex, "刷新功能隐藏项目失败")
        End Try
    End Sub

    'UI 协同改变

    Private Sub HiddenSetupMain(sender As Object, user As Boolean) Handles CheckHiddenPageSetup.Change
        '设置主页面
        If CheckHiddenPageSetup.Checked Then
            '开启
            CheckHiddenSetupLaunch.Checked = True
            CheckHiddenSetupSystem.Checked = True
            CheckHiddenSetupLink.Checked = True
            CheckHiddenSetupUI.Checked = True
        Else
            '关闭
            If Settings.Get(Of Boolean)("UiHiddenSetupLaunch") AndAlso Settings.Get(Of Boolean)("UiHiddenSetupUi") AndAlso Settings.Get(Of Boolean)("UiHiddenSetupSystem") AndAlso Settings.Get(Of Boolean)("UiHiddenSetupLink") Then
                CheckHiddenSetupLaunch.Checked = False
                CheckHiddenSetupSystem.Checked = False
                CheckHiddenSetupLink.Checked = False
                CheckHiddenSetupUI.Checked = False
            End If
        End If
    End Sub

    Private Sub HiddenSetupSub(sender As Object, user As Boolean) Handles CheckHiddenSetupLaunch.Change, CheckHiddenSetupSystem.Change, CheckHiddenSetupLink.Change, CheckHiddenSetupUI.Change
        '设置子页面
        CheckHiddenPageSetup.Checked =
            Settings.Get(Of Boolean)("UiHiddenSetupLaunch") AndAlso Settings.Get(Of Boolean)("UiHiddenSetupUi") AndAlso
            Settings.Get(Of Boolean)("UiHiddenSetupSystem") AndAlso Settings.Get(Of Boolean)("UiHiddenSetupLink")
    End Sub

    Private Sub HiddenOtherMain(sender As Object, user As Boolean) Handles CheckHiddenPageOther.Change
        '更多主页面
        If CheckHiddenPageOther.Checked Then
            '开启
            CheckHiddenOtherAbout.Checked = True
            CheckHiddenOtherTest.Checked = True
            CheckHiddenOtherFeedback.Checked = True
            CheckHiddenOtherVote.Checked = True
            CheckHiddenOtherHelp.Checked = True
        Else
            '关闭
            If Settings.Get(Of Boolean)("UiHiddenOtherHelp") AndAlso Settings.Get(Of Boolean)("UiHiddenOtherAbout") AndAlso Settings.Get(Of Boolean)("UiHiddenOtherTest") AndAlso
                Settings.Get(Of Boolean)("UiHiddenOtherVote") AndAlso Settings.Get(Of Boolean)("UiHiddenOtherFeedback") Then
                CheckHiddenOtherAbout.Checked = False
                CheckHiddenOtherTest.Checked = False
                CheckHiddenOtherFeedback.Checked = False
                CheckHiddenOtherVote.Checked = False
                CheckHiddenOtherHelp.Checked = False
            End If
        End If
    End Sub

    Private Sub HiddenOtherSub(sender As Object, user As Boolean) Handles CheckHiddenOtherHelp.Change, CheckHiddenOtherAbout.Change, CheckHiddenOtherTest.Change
        '更多子页面（有具体内容的）
        CheckHiddenPageOther.Checked =
            Settings.Get(Of Boolean)("UiHiddenOtherHelp") AndAlso Settings.Get(Of Boolean)("UiHiddenOtherAbout") AndAlso Settings.Get(Of Boolean)("UiHiddenOtherTest")
        '修改无具体内容的项
        If Not user Then Return
        If CheckHiddenPageOther.Checked Then
            CheckHiddenOtherFeedback.Checked = True
            CheckHiddenOtherVote.Checked = True
        End If
    End Sub

    Private Sub HiddenOtherNet(sender As Object, user As Boolean) Handles CheckHiddenOtherFeedback.Change, CheckHiddenOtherVote.Change
        '更多子页面（无具体内容的）
        If Not user Then Return
        If Settings.Get(Of Boolean)("UiHiddenOtherHelp") AndAlso Settings.Get(Of Boolean)("UiHiddenOtherAbout") AndAlso Settings.Get(Of Boolean)("UiHiddenOtherTest") AndAlso
            (Not Settings.Get(Of Boolean)("UiHiddenOtherFeedback") OrElse Not Settings.Get(Of Boolean)("UiHiddenOtherVote")) Then
            CheckHiddenOtherAbout.Checked = False
            CheckHiddenOtherTest.Checked = False
            CheckHiddenOtherHelp.Checked = False
        End If
    End Sub

    '警告提示
    Private Sub HiddenHint(sender As Object, user As Boolean) Handles CheckHiddenFunctionHidden.Change, CheckHiddenPageSetup.Change, CheckHiddenSetupUI.Change
        If AniControlEnabled = 0 AndAlso DirectCast(sender, MyCheckBox).Checked Then Hint("按 F12 即可暂时关闭功能隐藏设置。千万别忘了，要不然设置就改不回来了……")
    End Sub

#End Region

    '滑动条
    Private Sub SliderLoad()
        SliderMusicVolume.GetHintText = Function(v As Integer) Math.Ceiling(v * 0.1) & "%"
        SliderLauncherTransparent.GetHintText = Function(v As Integer) Math.Round(40 + v * 0.1) & "%"
        SliderBackgroundOpacity.GetHintText = Function(v As Integer) Math.Round(v * 0.1) & "%"
        SliderBackgroundBlur.GetHintText = Function(v As Integer) v & " 像素"
    End Sub

End Class
