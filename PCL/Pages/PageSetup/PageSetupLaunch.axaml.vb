Imports System.Collections.ObjectModel
Imports SkiaSharp

Public Class PageSetupLaunch
    Inherits MyPageRight

    Private IsLoadedOnce As Boolean = False
    Private RamTimer As DispatcherTimer
    Private PendingJavaSelection As String

    Public Sub New()
        InitControls()
        '输入验证
        TextArgumentInfo.ValidateRules.Add(New ValidateExcept With {
            .Excepts = New Collection(Of String) From {"""", "“", "”"}})
        TextArgumentWindowWidth.ValidateRules.Add(New ValidateInteger With {.Min = 1, .Max = 99999})
        TextArgumentWindowHeight.ValidateRules.Add(New ValidateInteger With {.Min = 1, .Max = 99999})
        AddHandler ModJava.JavaListChanged, AddressOf JavaListChanged
    End Sub

    Private Sub PageSetupLaunch_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        '重复加载部分
        PanBack.ScrollToHome()
        ModJava.JavaInit()
        RefreshRam(False)
        BtnSwitch.IsVisible = McInstanceSelected IsNot Nothing
        AniControlEnabled += 1
        Refresh()
        AniControlEnabled -= 1

        '非重复加载部分
        If Not IsLoadedOnce Then
            IsLoadedOnce = True
            SliderLoad()
            '内存自动刷新
            RamTimer = New DispatcherTimer With {.Interval = TimeSpan.FromSeconds(1)}
            AddHandler RamTimer.Tick, Sub() RefreshRam(False)
        End If
        RamTimer.Start()
    End Sub

    Private Sub PageSetupLaunch_Unloaded(sender As Object, e As RoutedEventArgs) Handles Me.Unloaded
        If RamTimer IsNot Nothing Then RamTimer.Stop()
    End Sub

    Public Sub Refresh()
        Try
            SettingService.RefreshSettings(Me)
            UpdateSkinTypeUI()
            UpdateRamType()
            WindowTypeUIRefresh()
            RefreshAdvanceRunWait()
            RefreshJvmResetButton()
            UpdateJavaList()
            RefreshRam(False)
        Catch ex As NullReferenceException
            Logger.Error(ex, "启动设置项存在异常，已被自动重置", LogBehavior.Alert)
            Reset()
        Catch ex As Exception
            Logger.Error(ex, "重载启动设置时出错")
        End Try
    End Sub

    Public Sub Reset()
        Try
            SettingService.ResetSettings(Me)
            Settings.Set("LaunchArgumentIndieV2", Settings.GetDefault("LaunchArgumentIndieV2"))
            ModJava.JavaListReset()
            If FrmMain IsNot Nothing Then ModJava.JavaListRefresh()
            Hint("已初始化启动设置！", HintType.Green)
        Catch ex As Exception
            Logger.Error(ex, "初始化启动设置失败", LogBehavior.Alert)
        End Try
        Refresh()
    End Sub

#Region "离线皮肤"

    Private Sub BtnSkinChange_Click(sender As Object, e As PointerReleasedEventArgs) Handles BtnSkinChange.Click
        Dim SkinInfo As McSkinInfo = McSkinSelect()
        If Not SkinInfo.IsVaild Then Return
        ChangeSkin(SkinInfo)
    End Sub

    Private Sub RadioSkinType4_Check(sender As Object, e As RouteEventArgs) Handles RadioSkinType4.PreviewCheck
        If Not (AniControlEnabled = 0 AndAlso e.RaiseByMouse) Then Return
        '已有图片则不再选择
        If FileUtils.Exists(Paths.AppDataThenName & "CustomSkin.png") Then Return
        '没有图片则要求选择
        Dim SkinInfo As McSkinInfo = McSkinSelect()
        If Not SkinInfo.IsVaild Then
            e.Handled = True
            Return
        End If
        '正式改变
        If Not ChangeSkin(SkinInfo) Then e.Handled = True
    End Sub

    '返回是否成功改变
    Private Function ChangeSkin(SkinInfo As McSkinInfo) As Boolean
        Try
            '复制文件
            FileUtils.Copy(SkinInfo.LocalFile, Paths.AppDataThenName & "CustomSkin.png")
            '将单层皮肤扩展到双层
            Dim Bitmap As New MyBitmap(Paths.AppDataThenName & "CustomSkin.png")
            If Bitmap.Pic.Width = 64 AndAlso Bitmap.Pic.Height = 32 Then
                Using NewPic As New SKBitmap(64, 64)
                    Using Canvas As New SKCanvas(NewPic)
                        Canvas.DrawBitmap(Bitmap.Pic, 0, 0)
                    End Using
                    FileUtils.Delete(Paths.AppDataThenName & "CustomSkin.png")
                    Dim NewBitmap As New MyBitmap(NewPic)
                    NewBitmap.Save(Paths.AppDataThenName & "CustomSkin.png")
                End Using
            End If
            '更新设置
            Settings.Set("LaunchSkinSlim", SkinInfo.IsSlim)
            Return True
        Catch ex As Exception
            Logger.Error(ex, "改变离线皮肤失败", LogBehavior.Alert)
            Return False
        Finally
            '设置当前显示
            PageLaunchLeft.SkinLegacy.Start(IsForceRestart:=True)
        End Try
    End Function

    Private Sub BtnSkinDelete_Click(sender As Object, e As PointerReleasedEventArgs) Handles BtnSkinDelete.Click
        Try
            FileUtils.Delete(Paths.AppDataThenName & "CustomSkin.png")
            RadioSkinType0.SetChecked(True, True)
            Hint("离线皮肤已清空！", HintType.Green)
        Catch ex As Exception
            Logger.Error(ex, "清空离线皮肤失败", LogBehavior.Alert)
        End Try
    End Sub

    Private Sub BtnSkinSave_Click(sender As Object, e As PointerReleasedEventArgs) Handles BtnSkinSave.Click
        MySkin.Save(PageLaunchLeft.SkinLegacy)
    End Sub

    Private Sub BtnSkinCache_Click(sender As Object, e As PointerReleasedEventArgs) Handles BtnSkinCache.Click
        MySkin.RefreshCache(Nothing)
    End Sub

    Public Shared Sub UpdateSkinType()
        PageLaunchLeft.SkinLegacy.Start()
        FrmMain?.FrmSetupLeft?.FrmSetupLaunch?.UpdateSkinTypeUI()
    End Sub

    ''' <summary>
    ''' 根据当前皮肤类型切换卡片内的附加设置项。
    ''' </summary>
    Private Sub UpdateSkinTypeUI()
        Select Case Settings.Get(Of Integer)("LaunchSkinType")
            Case 0, 1, 2 '默认
                PanSkinID.IsVisible = False
                PanSkinChange.IsVisible = False
            Case 3 '正版
                PanSkinID.IsVisible = True
                PanSkinChange.IsVisible = False
            Case 4 '自定义
                PanSkinID.IsVisible = False
                PanSkinChange.IsVisible = True
        End Select
        CardSkin.TriggerForceResize()
    End Sub

#End Region

#Region "内存分配"

    Private Sub SliderLoad()
        SliderRamCustom.GetHintText = Function(v) FormatRam(GetRamByIndex(CInt(v))) & " GB"
    End Sub

    ''' <summary>
    ''' 按自定义滑条的位置换算分配的内存（GB）。
    ''' </summary>
    Public Shared Function GetRamByIndex(Value As Integer) As Double
        If Value <= 12 Then Return Value * 0.1 + 0.3
        If Value <= 25 Then Return (Value - 12) * 0.5 + 1.5
        If Value <= 33 Then Return (Value - 25) + 8
        Return (Value - 33) * 2 + 16
    End Function

    ''' <summary>
    ''' 获取当前设置下分配给游戏的内存，单位为 GB。
    ''' </summary>
    Public Shared Function GetRam() As Double
        Dim RamGive As Double
        If Settings.Get(Of Integer)("LaunchRamType") = 0 Then
            '自动配置：根据电脑剩余内存分阶段分配
            Dim RamAvailable As Double = Math.Round(SystemInfo.AvailablePhysicalMemory / 1024.0 / 1024.0 / 1024.0, 1)
            Const RamMininum As Double = 0.5
            Const RamTarget1 As Double = 1.5
            Const RamTarget2 As Double = 2.5
            Const RamTarget3 As Double = 4.0
            Dim RamDelta As Double = RamTarget1
            RamGive += Math.Min(RamAvailable, RamDelta)
            RamAvailable -= RamDelta
            If RamAvailable >= 0.1 Then
                RamDelta = RamTarget2 - RamTarget1
                RamGive += Math.Min(RamAvailable * 0.7, RamDelta)
                RamAvailable -= RamDelta / 0.7
            End If
            If RamAvailable >= 0.1 Then
                RamDelta = RamTarget3 - RamTarget2
                RamGive += Math.Min(RamAvailable * 0.4, RamDelta)
                RamAvailable -= RamDelta / 0.4
            End If
            If RamAvailable >= 0.1 Then
                RamGive += Math.Min(RamAvailable * 0.15, RamTarget3)
            End If
            RamGive = Math.Round(Math.Max(RamGive, RamMininum), 1)
        Else
            RamGive = GetRamByIndex(Settings.Get(Of Integer)("LaunchRamCustom"))
        End If
        Return RamGive
    End Function

    ''' <summary>
    ''' 获取指定版本下分配的全局内存，单位为 GB。
    ''' </summary>
    Public Shared Function GetRam(Instance As McInstance, UseVersionJavaSetup As Boolean) As Double
        Dim RamGive As Double
        If Settings.Get(Of Integer)("LaunchRamType") = 0 Then
            '自动配置：根据电脑剩余内存与版本需求分阶段分配
            Dim RamAvailable As Double = Math.Round(SystemInfo.AvailablePhysicalMemory / 1024.0 / 1024.0 / 1024.0 * 10) / 10
            Dim RamMininum As Double
            Dim RamTarget1 As Double
            Dim RamTarget2 As Double
            Dim RamTarget3 As Double
            If Instance IsNot Nothing AndAlso Not Instance.IsLoaded Then Instance.Load()
            If Instance IsNot Nothing AndAlso Instance.Modable Then
                Dim ModDir = DirectoryUtils.GetInfo(Instance.PathIndie & "mods\")
                Dim ModCount As Integer = If(ModDir.Exists, ModDir.GetFiles.Count(Function(f) {".jar", ".zip", ".litemod"}.Contains(f.Extension.Lower)), 0)
                RamMininum = 0.5 + ModCount / 150
                RamTarget1 = 1.5 + ModCount / 90
                RamTarget2 = 2.7 + ModCount / 50
                RamTarget3 = 4.5 + ModCount / 25
            ElseIf Instance IsNot Nothing AndAlso Instance.Version.HasOptiFine Then
                RamMininum = 0.5
                RamTarget1 = 1.5
                RamTarget2 = 3
                RamTarget3 = 5
            Else
                RamMininum = 0.5
                RamTarget1 = 1.5
                RamTarget2 = 2.5
                RamTarget3 = 4
            End If
            Dim RamDelta As Double
            RamDelta = RamTarget1
            RamGive += Math.Min(RamAvailable, RamDelta)
            RamAvailable -= RamDelta
            If RamAvailable >= 0.1 Then
                RamDelta = RamTarget2 - RamTarget1
                RamGive += Math.Min(RamAvailable * 0.7, RamDelta)
                RamAvailable -= RamDelta / 0.7
            End If
            If RamAvailable >= 0.1 Then
                RamDelta = RamTarget3 - RamTarget2
                RamGive += Math.Min(RamAvailable * 0.4, RamDelta)
                RamAvailable -= RamDelta / 0.4
            End If
            If RamAvailable >= 0.1 Then
                RamGive += Math.Min(RamAvailable * 0.15, RamTarget3)
            End If
            RamGive = Math.Round(Math.Max(RamGive, RamMininum), 1)
        Else
            RamGive = GetRamByIndex(Settings.Get(Of Integer)("LaunchRamCustom"))
        End If
        Return RamGive
    End Function

    Private Shared Function FormatRam(Value As Double) As String
        If Value = Math.Floor(Value) Then Return Value.ToString("0.0")
        Return Value.ToString("0.0#")
    End Function

    ''' <summary>
    ''' 刷新 UI 上的内存占用显示。
    ''' </summary>
    Public Sub RefreshRam(ShowAnim As Boolean)
        If LabRamGame Is Nothing OrElse LabRamUsed Is Nothing Then Return
        If FrmMain IsNot Nothing AndAlso
           (FrmMain.FrmSetupLeft Is Nothing OrElse FrmMain.FrmSetupLeft.PageID <> PageSetupLeft.SetupPageType.Launch OrElse
            FrmMain.PanMainRight.Child IsNot Me) Then Return
        Try
            '获取内存情况
            Dim RamGame As Double = Math.Round(GetRam(), 5)
            Dim RamTotal As Double = Math.Round(SystemInfo.TotalPhysicalMemory / 1024.0 / 1024.0 / 1024.0, 1)
            Dim RamAvailable As Double = Math.Round(SystemInfo.AvailablePhysicalMemory / 1024.0 / 1024.0 / 1024.0, 1)
            Dim RamGameActual As Double = Math.Round(Math.Min(RamGame, RamAvailable), 5)
            Dim RamUsed As Double = Math.Round(RamTotal - RamAvailable, 5)
            Dim RamEmpty As Double = Math.Round(Math.Max(0, Math.Min(1000, RamTotal - RamUsed - RamGame)), 1)
            '设置最大可用内存
            If RamTotal <= 1.5 Then
                SliderRamCustom.MaxValue = Math.Max(CInt(Math.Floor((RamTotal - 0.3) / 0.1)), 1)
            ElseIf RamTotal <= 8 Then
                SliderRamCustom.MaxValue = CInt(Math.Floor((RamTotal - 1.5) / 0.5)) + 12
            ElseIf RamTotal <= 16 Then
                SliderRamCustom.MaxValue = CInt(Math.Floor(RamTotal - 8)) + 25
            Else
                SliderRamCustom.MaxValue = CInt(Math.Floor((RamTotal - 16) / 2)) + 33
            End If
            '设置文本
            LabRamGame.Text = FormatRam(RamGame) & " GB" &
                              If(RamGame <> RamGameActual, "（可用 " & FormatRam(RamGameActual) & " GB）", "")
            LabRamUsed.Text = FormatRam(RamUsed) & " GB"
            LabRamTotal.Text = " / " & FormatRam(RamTotal) & " GB"
            '设置色条宽度
            Dim TargetWidths As Double() = {Math.Max(RamUsed, 0.001), Math.Max(RamGameActual, 0.001), Math.Max(RamEmpty, 0.001)}
            If ShowAnim Then
                Dim AnimList As New List(Of AniData)
                For i = 0 To 2
                    AnimList.Add(AaGridLengthWidth(PanRamDisplay.ColumnDefinitions(i),
                        TargetWidths(i) - PanRamDisplay.ColumnDefinitions(i).Width.Value,
                        800,, New AniEaseOutFluent(AniEasePower.Strong)))
                Next
                AniStart(AnimList, "Ram Display")
            Else
                For i = 0 To 2
                    PanRamDisplay.ColumnDefinitions(i).Width = New GridLength(TargetWidths(i), GridUnitType.Star)
                Next
            End If
        Catch ex As Exception
            Logger.Error(ex, "刷新内存显示失败")
        End Try
    End Sub

    Private Sub UpdateRamType()
        If SliderRamCustom Is Nothing Then Return
        SliderRamCustom.IsEnabled = Settings.Get(Of Integer)("LaunchRamType") = 1
    End Sub

    Private Sub SliderRamCustom_Change(sender As Object, user As Boolean) Handles SliderRamCustom.Change
        RefreshRam(True)
    End Sub

    Private Sub RadioRamType_Changed(sender As Object, e As RouteEventArgs) Handles RadioRamType0.Changed, RadioRamType1.Changed
        UpdateRamType()
        RefreshRam(True)
    End Sub

    '开启自动内存优化的警告
    Private Sub CheckArgumentRam_Change(sender As Object, user As Boolean) Handles CheckArgumentRam.Change
        If AniControlEnabled <> 0 Then Return
        If Not CheckArgumentRam.Checked Then Return
        Dim AdminHint As String = If(WindowsUtils.HasAdminRole(), "",
            $"{vbCrLf}{vbCrLf}每次启动游戏，PCL 都需要申请管理员权限以进行内存优化。{vbCrLf}若想自动授予权限，可以右键 PCL，打开 属性 → 兼容性 → 以管理员身份运行此程序。")
        If MyMsgBox("内存优化会显著延长启动耗时，建议仅在内存不足时开启。" & vbCrLf &
                    "如果你在使用机械硬盘，这还可能导致一小段时间的严重卡顿。" & AdminHint,
                    "提醒", "确定", "取消") = 2 Then
            CheckArgumentRam.Checked = False
            SettingService.SaveSetting(CheckArgumentRam)
        End If
    End Sub

#End Region

#Region "其他选项"

    Private Sub WindowTypeUIRefresh()
        If TextArgumentWindowWidth Is Nothing Then Return
        Dim IsCustom As Boolean = ComboArgumentWindowType.SelectedIndex = 3
        TextArgumentWindowWidth.IsVisible = IsCustom
        LabArgumentWindowMiddle.IsVisible = IsCustom
        TextArgumentWindowHeight.IsVisible = IsCustom
    End Sub

    Private Sub ComboArgumentWindowType_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboArgumentWindowType.SelectionChanged
        WindowTypeUIRefresh()
    End Sub

    '可见性选择直接关闭的警告
    Private Sub ComboArgumentVisibie_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboArgumentVisibie.SelectionChanged
        If AniControlEnabled <> 0 Then Return
        If ComboArgumentVisibie.SelectedIndex <> 0 Then Return
        If MyMsgBox("若在游戏启动后立即关闭启动器，崩溃检测、更改游戏标题等功能将失效。" & vbCrLf &
                    "如果想保留这些功能，可以选择让启动器在游戏启动后隐藏，游戏退出后自动关闭。", "提醒", "继续", "取消") = 2 Then
            If e.RemovedItems IsNot Nothing AndAlso e.RemovedItems.Count > 0 Then
                ComboArgumentVisibie.SelectedItem = e.RemovedItems(0)
            End If
        End If
    End Sub

    '版本隔离提示
    Private Sub ComboArgumentIndieV2_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboArgumentIndieV2.SelectionChanged
        If AniControlEnabled <> 0 Then Return
        MyMsgBox("本设置仅会对之后新安装的版本生效。" & vbCrLf &
                 "如果要修改已安装的版本的隔离方式，请在它的版本独立设置中调整。")
    End Sub

#End Region

#Region "高级设置"

    Private Sub RefreshAdvanceRunWait()
        If CheckAdvanceRunWait Is Nothing Then Return
        CheckAdvanceRunWait.IsVisible = Not String.IsNullOrEmpty(TextAdvanceRun.Text)
    End Sub

    Private Sub TextAdvanceRun_TextChanged(sender As Object, e As TextChangedEventArgs) Handles TextAdvanceRun.TextChanged
        RefreshAdvanceRunWait()
    End Sub

    'JVM 参数重设
    Private Sub RefreshJvmResetButton()
        If BtnAdvanceJvmReset Is Nothing Then Return
        BtnAdvanceJvmReset.IsVisible = TextAdvanceJvm.Text <> Settings.GetDefault("LaunchAdvanceJvm")
    End Sub

    Private Sub TextAdvanceJvm_ValidatedTextChanged(sender As Object, e As TextChangedEventArgs) Handles TextAdvanceJvm.ValidatedTextChanged
        RefreshJvmResetButton()
    End Sub

    Private Sub BtnAdvanceJvmReset_Click(sender As Object, e As EventArgs) Handles BtnAdvanceJvmReset.Click
        Settings.Reset("LaunchAdvanceJvm")
        Refresh()
    End Sub

    '去除参数中的回车
    Private Sub ReplaceEnter(sender As MyTextBox, e As TextChangedEventArgs) Handles TextAdvanceJvm.TextChanged, TextAdvanceGame.TextChanged
        If sender.Text Is Nothing Then Return
        Dim NewText As String = sender.Text.ReplaceLineEndings(" ", True)
        If NewText = sender.Text Then Return
        Dim CaretIndex As Integer = sender.CaretIndex
        sender.Text = NewText
        sender.CaretIndex = Math.Max(0, CaretIndex - 1)
    End Sub

#End Region

#Region "Java 列表"

    Private Sub JavaListChanged(sender As Object, e As EventArgs)
        RunInUi(Sub() UpdateJavaList())
    End Sub

    ''' <summary>
    ''' 刷新 Java 下拉框的显示。
    ''' </summary>
    Public Sub UpdateJavaList()
        If ComboAdvanceJava Is Nothing Then Return
        Dim SelectedFolder As String = PendingJavaSelection
        If SelectedFolder Is Nothing Then SelectedFolder = GetSelectedJava()?.Folder
        PendingJavaSelection = Nothing
        ComboAdvanceJava.Items.Clear()
        If ModJava.IsRefreshing Then
            BtnAdvanceJavaSearch.IsEnabled = False
            BtnAdvanceJavaImport.IsEnabled = False
            ComboAdvanceJava.IsEnabled = False
            ComboAdvanceJava.PlaceholderText = "正在搜索 Java …"
            RefreshJavaButtons()
            Return
        End If
        BtnAdvanceJavaSearch.IsEnabled = True
        BtnAdvanceJavaImport.IsEnabled = True
        ComboAdvanceJava.IsEnabled = True
        Dim Entries = ModJava.JavaList
        For Each Entry In Entries
            Dim Item As New MyComboBoxItem With {.Content = Entry.ToString()}
            ToolTip.SetTip(Item, Entry.ToString())
            ComboAdvanceJava.Items.Add(Item)
        Next
        ComboAdvanceJava.PlaceholderText = If(Entries.Count = 0,
            "未找到 Java，请点击右侧按钮搜索或导入", "")
        If SelectedFolder IsNot Nothing Then
            Dim Index As Integer = Entries.ToList().FindIndex(Function(j) PathUtils.IsSame(j.Folder, SelectedFolder))
            If Index >= 0 Then ComboAdvanceJava.SelectedIndex = Index
        End If
        RefreshJavaButtons()
    End Sub

    Private Function GetSelectedJava() As Java
        Dim Entries = ModJava.JavaList
        Dim Index As Integer = ComboAdvanceJava.SelectedIndex
        If Index < 0 OrElse Index >= Entries.Count Then Return Nothing
        Return Entries(Index)
    End Function

    Private Sub RefreshJavaButtons()
        Dim Entry = GetSelectedJava()
        Dim Entries = ModJava.JavaList
        Dim Index As Integer = ComboAdvanceJava.SelectedIndex
        Dim IsAvailable As Boolean = Entry IsNot Nothing AndAlso Not ModJava.IsRefreshing
        BtnAdvanceJavaUp.IsEnabled = IsAvailable AndAlso Index > 0
        BtnAdvanceJavaDown.IsEnabled = IsAvailable AndAlso Index < Entries.Count - 1
        BtnAdvanceJavaRemove.IsEnabled = IsAvailable
        BtnAdvanceJavaOpen.IsEnabled = Entry IsNot Nothing
    End Sub

    Private Sub ComboAdvanceJava_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboAdvanceJava.SelectionChanged
        RefreshJavaButtons()
        If AniControlEnabled <> 0 OrElse ModJava.IsRefreshing Then Return
        If ComboAdvanceJava.SelectedIndex < 0 Then Return
        Hint("使用下方的箭头按钮可以调整 Java 的优先级，PCL 会优先选择列表中靠前且兼容的 Java。", HintType.Blue, False)
    End Sub

    Private Sub BtnAdvanceJavaSearch_Click(sender As Object, e As EventArgs) Handles BtnAdvanceJavaSearch.Click
        ModJava.JavaListRefresh()
    End Sub

    Private Sub BtnAdvanceJavaImport_Click(sender As Object, e As EventArgs) Handles BtnAdvanceJavaImport.Click
        ModJava.JavaManuallyImport()
    End Sub

    Private Sub BtnAdvanceJavaUp_Click(sender As Object, e As EventArgs) Handles BtnAdvanceJavaUp.Click
        Dim Entry = GetSelectedJava()
        If Entry Is Nothing Then Return
        PendingJavaSelection = Entry.Folder
        ModJava.JavaMove(Entry, -1)
    End Sub

    Private Sub BtnAdvanceJavaDown_Click(sender As Object, e As EventArgs) Handles BtnAdvanceJavaDown.Click
        Dim Entry = GetSelectedJava()
        If Entry Is Nothing Then Return
        PendingJavaSelection = Entry.Folder
        ModJava.JavaMove(Entry, 1)
    End Sub

    Private Sub BtnAdvanceJavaRemove_Click(sender As Object, e As EventArgs) Handles BtnAdvanceJavaRemove.Click
        Dim Entry = GetSelectedJava()
        If Entry Is Nothing Then Return
        PendingJavaSelection = ""
        ModJava.JavaRemove(Entry)
    End Sub

    Private Sub BtnAdvanceJavaOpen_Click(sender As Object, e As EventArgs) Handles BtnAdvanceJavaOpen.Click
        Dim Entry = GetSelectedJava()
        If Entry IsNot Nothing Then OpenExplorer(Entry.JavaExePath)
    End Sub

#End Region

    '切换到版本独立设置
    Private Sub BtnSwitch_Click(sender As Object, e As EventArgs) Handles BtnSwitch.Click
        McInstanceSelected.Load()
        PageInstanceLeft.Instance = McInstanceSelected
        FrmMain.PageChange(FormMain.PageType.InstanceSetup, FormMain.PageSubType.InstanceSetup)
    End Sub

End Class
