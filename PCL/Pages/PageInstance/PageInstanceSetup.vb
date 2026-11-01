Imports System.Collections.ObjectModel

''' <summary>
''' 版本独立设置页面。
''' </summary>
Public Class PageInstanceSetup
    Inherits MyPageRight

    Public Sub New()
        InitControls()
        '校验规则（XAML 中无法使用 System:String 提供字符串项）
        TextArgumentInfo.ValidateRules.Add(New ValidateExcept With {
            .Excepts = New Collection(Of String) From {"""", "“", "”"}})
        TextServerEnter.ValidateRules.Add(New ValidateExcept With {
            .Excepts = New Collection(Of String) From {"""", "“", "”", "http://", "https://"}})
    End Sub

    Private LoadedFirst As Boolean = False
    Private RamTimer As DispatcherTimer
    Private Sub PageSetupSystem_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        '重复加载部分
        PanBack.ScrollToHome()
        RefreshRam(False)

        '由于各个版本不同，每次都需要重新加载
        AniControlEnabled += 1
        Reload()
        AniControlEnabled -= 1

        '非重复加载部分
        If LoadedFirst Then Return
        LoadedFirst = True

        'Java 加载器同步
        AddHandler PageInstanceLeft.CurrentJavaWorker.Started, AddressOf UpdateJavaRelatedUi
        AddHandler PageInstanceLeft.CurrentJavaWorker.Stopped, AddressOf UpdateJavaRelatedUi

        '版本范围校验
        TextArgumentJavaRange.ValidateRules.Add(New ValidateFunc(
        Function(Str As String)
            Try
                If String.IsNullOrWhiteSpace(Str) Then Return "不能为空"
                Dim Range = ValueRange(Of Version).FromString(Str, Function(s) StringUtils.ParseVersionWithDefaults(s))
                If Range.IsEmpty Then Return "范围下限的版本号比上限更高，导致该范围无法匹配到任何版本"
                If Range.HasLower AndAlso Range.Lower.Major <= 1 AndAlso (Range.Lower.Minor > 0 OrElse Range.Lower.Build > 0 OrElse Range.Lower.Revision > 0) Then Return "不应使用 1.x 格式进行匹配（例如，若想匹配 Java 17，应填写 17 而非 1.17）"
                If Range.HasUpper AndAlso Range.Upper.Major <= 1 AndAlso (Range.Upper.Minor > 0 OrElse Range.Upper.Build > 0 OrElse Range.Upper.Revision > 0) Then Return "不应使用 1.x 格式进行匹配（例如，若想匹配 Java 17，应填写 17 而非 1.17）"
                If Range.HasUpper AndAlso Range.Upper.Major <= 4 Then Return "该范围所要求的 Java 版本过低"
                If Range.HasUpper AndAlso Range.IsUpperInclusive AndAlso Range.Upper.Minor <= 0 AndAlso Range.Upper.Build <= 0 Then Return $"右侧闭区间的意图并不明确。如果不想允许 Java {Range.Upper.Major}，请改为 {vbLQ}{Range.Upper.Major}){vbRQ}。如果想允许 Java {Range.Upper.Major}，请改为 {vbLQ}{Range.Upper.Major + 1}){vbRQ}。"
                If Range.Intersect(ValueRange(Of Version).OpenClosed(New Version(5, 0), New Version(99, 0))).IsEmpty() Then Return "该范围无法匹配到任何常见的 Java"
                Return ""
            Catch ex As Exception
                Return ex.Message
            End Try
        End Function))

        '内存自动刷新
        RamTimer = New DispatcherTimer With {.Interval = New TimeSpan(0, 0, 0, 1)}
        AddHandler RamTimer.Tick, Sub() RefreshRam()
        RamTimer.Start()

    End Sub
    Public Sub Reload()
        Try

            '启动参数
            Dim _unused = PageInstanceLeft.Instance.PathIndie '触发自动判定
            ComboArgumentIndieV2.SelectedIndex = If(Settings.Get(Of Boolean)("VersionArgumentIndieV2", Instance:=PageInstanceLeft.Instance), 0, 1)
            UpdateJavaList()

            '服务器
            '微软登录暂未开放，下拉框中前两项（正版相关）直接移除
            If Not MsLoginEnabled AndAlso ComboServerLogin.ItemCount > 3 Then
                ComboServerLogin.Items.RemoveAt(1)
                ComboServerLogin.Items.RemoveAt(0)
            End If
            Dim LoginStored As Integer = Settings.Get(Of Integer)("VersionServerLogin", Instance:=PageInstanceLeft.Instance)
            If Not MsLoginEnabled AndAlso LoginStored <= 1 Then LoginStored = 2
            ComboServerLoginLast = LoginStored
            ComboServerLogin.SelectedIndex = ServerLoginStoredToIndex(LoginStored)
            UpdateServerLoginUI()

            '高级设置
            If Settings.Get(Of Integer)("VersionAdvanceAssets", Instance:=PageInstanceLeft.Instance) = 2 Then
                Logger.Info("已迁移老版本的关闭文件校验设置")
                Settings.Reset("VersionAdvanceAssets", Instance:=PageInstanceLeft.Instance)
                Settings.Set("VersionAdvanceAssetsV2", True, Instance:=PageInstanceLeft.Instance)
            End If

            SettingService.RefreshSettings(Me)

            '游戏内存
            OnVersionRamTypeChanged(Settings.Get(Of Integer)("VersionRamType", Instance:=PageInstanceLeft.Instance))

        Catch ex As Exception
            Logger.Error(ex, "重载版本独立设置时出错")
        End Try
    End Sub

    '初始化
    Public Sub Reset()
        Try

            SettingService.ResetSettings(Me)

            Settings.Reset("VersionServerLogin", Instance:=PageInstanceLeft.Instance)
            Settings.Reset("VersionArgumentIndieV2", Instance:=PageInstanceLeft.Instance)
            Settings.Reset("VersionAdvanceAssets", Instance:=PageInstanceLeft.Instance)
            Configs.JavaForced.Reset(PageInstanceLeft.Instance.Config)
            JavaListRefreshWorker.Start()

            Logger.Info("已初始化版本独立设置")
            Hint("已初始化版本独立设置！", HintType.Green, False)
        Catch ex As Exception
            Logger.Error(ex, "初始化版本独立设置失败", LogBehavior.Alert)
        End Try

        Reload()
    End Sub

#Region "游戏内存"

    Public Shared Sub OnVersionRamTypeChanged(Type As Integer)
        If FrmInstanceSetup Is Nothing Then Return
        FrmInstanceSetup.RamType(Type)
    End Sub

    Public Sub RamType(Type As Integer)
        If SliderRamCustom Is Nothing Then Return
        SliderRamCustom.IsEnabled = (Type = 1)
    End Sub

    Private Shared Function FormatRam(Value As Double) As String
        If Value = Math.Floor(Value) Then Return Value.ToString("0.0")
        Return Value.ToString("0.0#")
    End Function

    ''' <summary>
    ''' 刷新 UI 上的 RAM 显示。
    ''' </summary>
    Public Sub RefreshRam(ShowAnim As Boolean)
        If LabRamGame Is Nothing OrElse LabRamUsed Is Nothing OrElse FrmMain.PageCurrent <> FormMain.PageType.InstanceSetup OrElse FrmInstanceLeft.PageID <> FormMain.PageSubType.InstanceSetup Then Return
        Try
            '获取内存情况
            Dim RamGame As Double = Math.Round(GetRam(PageInstanceLeft.Instance), 5)
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
                              If(RamGame <> RamGameActual, " (可用 " & FormatRam(RamGameActual) & " GB)", "")
            LabRamUsed.Text = FormatRam(RamUsed) & " GB"
            LabRamTotal.Text = " / " & FormatRam(RamTotal) & " GB"
            If ShowAnim Then
                '宽度动画
                AniStart({
                    AaGridLengthWidth(PanRamDisplay.ColumnDefinitions(0), RamUsed - PanRamDisplay.ColumnDefinitions(0).Width.Value, 800,, New AniEaseOutFluent(AniEasePower.Strong)),
                    AaGridLengthWidth(PanRamDisplay.ColumnDefinitions(1), RamGameActual - PanRamDisplay.ColumnDefinitions(1).Width.Value, 800,, New AniEaseOutFluent(AniEasePower.Strong)),
                    AaGridLengthWidth(PanRamDisplay.ColumnDefinitions(2), RamEmpty - PanRamDisplay.ColumnDefinitions(2).Width.Value, 800,, New AniEaseOutFluent(AniEasePower.Strong))
                }, "VersionSetup Ram Grid")
            Else
                '宽度设置
                PanRamDisplay.ColumnDefinitions(0).Width = New GridLength(Math.Max(RamUsed, 0.001), GridUnitType.Star)
                PanRamDisplay.ColumnDefinitions(1).Width = New GridLength(Math.Max(RamGameActual, 0.001), GridUnitType.Star)
                PanRamDisplay.ColumnDefinitions(2).Width = New GridLength(Math.Max(RamEmpty, 0.001), GridUnitType.Star)
            End If
        Catch ex As Exception
            Logger.Error(ex, "刷新内存显示失败")
        End Try
    End Sub
    Private Sub RefreshRam() Handles SliderRamCustom.Change, RadioRamType0.Check, RadioRamType1.Check, RadioRamType2.Check
        RefreshRam(True)
    End Sub

    Private RamTextLeft As Integer = 2, RamTextRight As Integer = 1
    ''' <summary>
    ''' 刷新 UI 上的文本位置。
    ''' </summary>
    Private Sub RefreshRamText() Handles RectRamGame.SizeChanged, RectRamEmpty.SizeChanged, LabRamGame.SizeChanged
        '获取宽度信息
        Dim RectUsedWidth = RectRamUsed.Bounds.Width
        Dim TotalWidth = PanRamDisplay.Bounds.Width
        Dim LabGameWidth = LabRamGame.Bounds.Width, LabUsedWidth = LabRamUsed.Bounds.Width, LabTotalWidth = LabRamTotal.Bounds.Width
        Dim LabGameTitleWidth = LabRamGameTitle.Bounds.Width, LabUsedTitleWidth = LabRamUsedTitle.Bounds.Width
        '左侧
        Dim Left As Integer
        If RectUsedWidth - 30 < LabUsedWidth OrElse RectUsedWidth - 30 < LabUsedTitleWidth Then
            '全写不下了
            Left = 0
        ElseIf RectUsedWidth - 25 < (LabUsedWidth + LabTotalWidth) Then
            '显示不下完整数据
            Left = 1
        Else
            '正常
            Left = 2
        End If
        If RamTextLeft <> Left Then
            RamTextLeft = Left
            Select Case Left
                Case 0
                    AniStart({
                            AaOpacity(LabRamUsed, -LabRamUsed.Opacity, 100),
                            AaOpacity(LabRamTotal, -LabRamTotal.Opacity, 100),
                            AaOpacity(LabRamUsedTitle, -LabRamUsedTitle.Opacity, 100)
                        }, "VersionSetup Ram TextLeft")
                Case 1
                    AniStart({
                            AaOpacity(LabRamUsed, 1 - LabRamUsed.Opacity, 100),
                            AaOpacity(LabRamTotal, -LabRamTotal.Opacity, 100),
                            AaOpacity(LabRamUsedTitle, 0.7 - LabRamUsedTitle.Opacity, 100)
                        }, "VersionSetup Ram TextLeft")
                Case 2
                    AniStart({
                            AaOpacity(LabRamUsed, 1 - LabRamUsed.Opacity, 100),
                            AaOpacity(LabRamTotal, 1 - LabRamTotal.Opacity, 100),
                            AaOpacity(LabRamUsedTitle, 0.7 - LabRamUsedTitle.Opacity, 100)
                        }, "VersionSetup Ram TextLeft")
            End Select
        End If
        '右侧
        Dim Right As Integer
        If TotalWidth < LabGameWidth + 2 + RectUsedWidth OrElse TotalWidth < LabGameTitleWidth + 2 + RectUsedWidth Then
            '挤到最右边
            Right = 0
        Else
            '正常情况
            Right = 1
        End If
        If Right = 0 Then
            If AniControlEnabled = 0 AndAlso (RamTextRight <> Right OrElse AniIsRun("VersionSetup Ram TextRight")) Then
                '需要动画
                AniStart({
                        AaX(LabRamGame, TotalWidth - LabGameWidth - LabRamGame.Margin.Left, 100,, New AniEaseOutFluent(AniEasePower.Weak)),
                        AaX(LabRamGameTitle, TotalWidth - LabGameTitleWidth - LabRamGameTitle.Margin.Left, 100,, New AniEaseOutFluent(AniEasePower.Weak))
                }, "VersionSetup Ram TextRight")
            Else
                '不需要动画
                LabRamGame.Margin = New Thickness(TotalWidth - LabGameWidth, 3, 0, 0)
                LabRamGameTitle.Margin = New Thickness(TotalWidth - LabGameTitleWidth, 0, 0, 5)
            End If
        Else
            If AniControlEnabled = 0 AndAlso (RamTextRight <> Right OrElse AniIsRun("VersionSetup Ram TextRight")) Then
                '需要动画
                AniStart({
                        AaX(LabRamGame, 2 + RectUsedWidth - LabRamGame.Margin.Left, 100,, New AniEaseOutFluent(AniEasePower.Weak)),
                        AaX(LabRamGameTitle, 2 + RectUsedWidth - LabRamGameTitle.Margin.Left, 100,, New AniEaseOutFluent(AniEasePower.Weak))
                }, "VersionSetup Ram TextRight")
            Else
                '不需要动画
                LabRamGame.Margin = New Thickness(2 + RectUsedWidth, 3, 0, 0)
                LabRamGameTitle.Margin = New Thickness(2 + RectUsedWidth, 0, 0, 5)
            End If
        End If
        RamTextRight = Right
    End Sub

    ''' <summary>
    ''' 获取当前版本设置下分配的 RAM 值，单位为 GB。
    ''' </summary>
    Public Shared Function GetRam(Instance As McInstance) As Double
        '跟随全局设置
        If Settings.Get(Of Integer)("VersionRamType", Instance:=Instance) = 2 Then
            Return PageSetupLaunch.GetRam(Instance, True)
        End If

        Dim RamGive As Double
        If Settings.Get(Of Integer)("VersionRamType", Instance:=Instance) = 0 Then
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
            RamGive = PageSetupLaunch.GetRamByIndex(Settings.Get(Of Integer)("VersionRamCustom", Instance:=Instance))
        End If
        Return RamGive
    End Function

#End Region

#Region "服务器"

    '当第三方登录更改时，清空版本列表缓存以更新版本分类
    Public Shared Sub OnVersionServerLoginChanged(Type As Integer)
        If FrmInstanceSetup Is Nothing Then Return
        WriteIni(McFolderSelected & "PCL.ini", "InstanceCache", "")
        If PageInstanceLeft.Instance Is Nothing Then Return
        PageInstanceLeft.Instance = New McInstance(PageInstanceLeft.Instance.Name).Load()
        LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\")
    End Sub

    '自动替换标点
    Private Sub TextServerEnter_Change() Handles TextServerEnter.TextChanged
        Dim NewText = TextServerEnter.Text.Replace("：", ":").Replace("。", ".")
        If NewText = TextServerEnter.Text Then Return
        Dim CurrentPosition = TextServerEnter.SelectionStart '重设焦点
        TextServerEnter.Text = NewText
        TextServerEnter.SelectionStart = CurrentPosition
    End Sub

    '全局
    Private ComboServerLoginLast As Integer
    Private Shared Function ServerLoginStoredToIndex(Value As Integer) As Integer
        If MsLoginEnabled Then Return Value
        Return Math.Max(Value - 2, 0)
    End Function
    Private Shared Function ServerLoginIndexToStored(Index As Integer) As Integer
        If MsLoginEnabled Then Return Index
        Return Index + 2
    End Function
    Private Sub ComboServerLogin_Changed() Handles ComboServerLogin.SelectionChanged, TextServerNide.ValidatedTextChanged, TextServerAuthServer.ValidatedTextChanged, TextServerAuthRegister.ValidatedTextChanged
        If AniControlEnabled <> 0 Then Return
        If ComboServerLogin.SelectedIndex < 0 Then Return
        UpdateServerLoginUI()
        Dim Type As Integer = ServerLoginIndexToStored(ComboServerLogin.SelectedIndex)
        '检查是否输入正确，正确才触发设置改变
        If Type = 3 AndAlso Not TextServerNide.IsValidated Then Return
        If Type = 4 AndAlso Not TextServerAuthServer.IsValidated Then Return
        '检查结果是否发生改变，未改变则不触发设置改变
        If ComboServerLoginLast = Type Then Return
        '触发
        ComboServerLoginLast = Type
        Settings.Set("VersionServerLogin", Type, Instance:=PageInstanceLeft.Instance)
    End Sub
    Private Sub UpdateServerLoginUI()
        If LabServerNide Is Nothing OrElse ComboServerLogin.SelectedIndex < 0 Then Return
        Dim Type = ServerLoginIndexToStored(ComboServerLogin.SelectedIndex)
        LabServerNide.IsVisible = Type = 3
        TextServerNide.IsVisible = Type = 3
        PanServerNide.IsVisible = Type = 3
        LabServerAuthName.IsVisible = Type = 4
        TextServerAuthName.IsVisible = Type = 4
        LabServerAuthRegister.IsVisible = Type = 4
        TextServerAuthRegister.IsVisible = Type = 4
        LabServerAuthServer.IsVisible = Type = 4
        TextServerAuthServer.IsVisible = Type = 4
        BtnServerAuthLittle.IsVisible = Type = 4
        CardServer.TriggerForceResize()
    End Sub

    'LittleSkin
    Private Sub BtnServerAuthLittle_Click(sender As Object, e As EventArgs) Handles BtnServerAuthLittle.Click
        If TextServerAuthServer.Text <> "" AndAlso TextServerAuthServer.Text <> "https://littleskin.cn/api/yggdrasil" AndAlso
            MyMsgBox("即将把第三方登录设置覆盖为 LittleSkin 登录。" & vbCrLf & "除非你是服主，或者服主要求你这样做，否则请不要继续。" & vbCrLf & vbCrLf & "是否确实需要覆盖当前设置？",
                     "设置覆盖确认", "继续", "取消") = 2 Then Return
        TextServerAuthServer.Text = "https://littleskin.cn/api/yggdrasil"
        TextServerAuthRegister.Text = "https://littleskin.cn/auth/register"
        TextServerAuthName.Text = "LittleSkin 登录"
    End Sub

#End Region

#Region "Java 主类别和预览"

    Private Sub ReloadCurrentJava() Handles ComboArgumentJava.SelectionChanged, TextArgumentJavaRange.ValidatedTextChanged
        PageInstanceLeft.ReloadCurrentJava()
    End Sub

    Private Sub UpdateJavaRelatedUi() Handles ComboArgumentJava.SelectionChanged, TextArgumentJavaRange.TextChanged
        RunInUi(AddressOf _UpdateJavaRelatedUi)
    End Sub
    Private Sub _UpdateJavaRelatedUi()
        Dim JavaWorker = PageInstanceLeft.CurrentJavaWorker
        Select Case ComboArgumentJava.SelectedIndex
            Case 0, 2
                HintArgumentJava.IsVisible = True
                TextArgumentJavaRange.IsVisible = False
                PanArgumentJavaSelect.IsVisible = False
                '显示当前会选择的 Java
                If Not JavaWorker.LastSucceeded OrElse JavaWorker.Running Then
                    If Not JavaWorker.Running Then JavaWorker.Start()
                    HintArgumentJava.Text = $"正在查找 Java……"
                    HintArgumentJava.Theme = MyHint.Themes.Blue
                ElseIf JavaWorker.LastResult Is Nothing Then
                    If ComboArgumentJava.SelectedIndex = 0 Then
                        HintArgumentJava.Text = $"你的电脑上没有可供该版本使用的 Java，PCL 会在启动游戏时自动下载。"
                        HintArgumentJava.Theme = MyHint.Themes.Yellow
                    Else
                        HintArgumentJava.Text = $"该版本的版本文件夹中没有发现任何 Java！{vbCrLf}请点击此处打开版本文件夹，然后将 Java 文件夹复制进去。"
                        HintArgumentJava.Theme = MyHint.Themes.Red
                    End If
                Else
                    HintArgumentJava.Text = $"将会使用：{JavaWorker.LastResult}"
                    HintArgumentJava.Theme = MyHint.Themes.Blue
                End If
            Case 1
                HintArgumentJava.IsVisible = True
                TextArgumentJavaRange.IsVisible = True
                PanArgumentJavaSelect.IsVisible = False
                '首次设置
                If Not Settings.HasSaved("VersionArgumentJavaRange", PageInstanceLeft.Instance) Then
                    Dim DefaultRange As String = GetJavaRequirement(PageInstanceLeft.Instance).Range.ToString.Replace("-∞", "").Replace("+∞", "")
                    Settings.Set("VersionArgumentJavaRange", DefaultRange, PageInstanceLeft.Instance)
                    TextArgumentJavaRange.Text = DefaultRange
                End If
                '输入的范围有误
                If Not String.IsNullOrEmpty(TextArgumentJavaRange.ValidateResult) Then
                    HintArgumentJava.Text = $"Java 版本范围错误：{TextArgumentJavaRange.ValidateResult}"
                    HintArgumentJava.Theme = MyHint.Themes.Red
                    Return
                End If
                '显示当前会选择的 Java
                If Not JavaWorker.LastSucceeded OrElse JavaWorker.Running Then
                    If Not JavaWorker.Running Then JavaWorker.Start()
                    HintArgumentJava.Text = $"正在查找 Java……"
                    HintArgumentJava.Theme = MyHint.Themes.Blue
                ElseIf JavaWorker.LastResult Is Nothing Then
                    HintArgumentJava.Text = $"你的电脑上没有任何 Java 符合该版本范围！{vbCrLf}如果 Mojang 提供了对应版本的 Java，PCL 会在启动游戏时自动下载。"
                    HintArgumentJava.Theme = MyHint.Themes.Yellow
                Else
                    HintArgumentJava.Text = $"将会使用：{JavaWorker.LastResult}"
                    HintArgumentJava.Theme = MyHint.Themes.Blue
                End If
            Case 3
                HintArgumentJava.IsVisible = False
                TextArgumentJavaRange.IsVisible = False
                PanArgumentJavaSelect.IsVisible = True
        End Select
    End Sub
    Private Sub HintArgumentJava_Click(sender As Object, e As PointerReleasedEventArgs) Handles HintArgumentJava.Click
        If ComboArgumentJava.SelectedIndex = 2 Then PageInstanceOverall.OpenInstanceFolder(PageInstanceLeft.Instance)
    End Sub

#End Region

#Region "选择特定 Java 的下拉框"

    '刷新 Java 下拉框显示
    Public Sub UpdateJavaList()
        If ComboArgumentJavaSelect Is Nothing OrElse LabArgumentJava Is Nothing OrElse Not ComboArgumentJavaSelect.IsLoaded Then Return
        ComboArgumentJavaSelect.Items.Clear()
        '还需要等待搜索结束
        If JavaListRefreshWorker.Running Then
            BtnArgumentJavaSearch.IsEnabled = False
            ComboArgumentJavaSelect.IsEnabled = False
            LabArgumentJava.Text = "搜索中 …"
            Return
        End If
        '========================================== 显示结果 ==========================================
        BtnArgumentJavaSearch.IsEnabled = True
        ComboArgumentJavaSelect.IsEnabled = True
        Dim SelectedJava As Java = Configs.JavaForced.Get(PageInstanceLeft.Instance.Config)
        '更新下拉框文本
        If Not Configs.JavaList.Get().Any() Then
            LabArgumentJava.Text = "未找到 Java，点击以导入已有的 Java"
        ElseIf SelectedJava Is Nothing Then
            LabArgumentJava.Text = "点击选择 Java …"
        Else
            LabArgumentJava.Text = SelectedJava.ToString
        End If
        '更新列表
        Try
            For Each JavaEntry In Configs.JavaList.Get()
                Dim JavaItem As New MyListItem With {
                    .FontSize = 13, .Height = 24, .IsScaleAnimationEnabled = False, .Type = MyListItem.CheckType.Clickable,
                    .Tag = JavaEntry, .Title = JavaEntry.ToString}
                AddHandler JavaItem.PointerReleased, Sub(s, e) e.Handled = True
                AddHandler JavaItem.Click,
                Sub()
                    Configs.JavaForced.Set(JavaEntry, PageInstanceLeft.Instance.Config)
                    UpdateJavaList()
                    ComboArgumentJavaSelect.IsDropDownOpen = False
                End Sub
                ComboArgumentJavaSelect.Items.Add(JavaItem)
                Dim Buttons As New List(Of MyIconButton)
                '从列表中移除按钮
                Dim IsOfficial As Boolean = PathUtils.IsParentOf($"{Paths.AppData}.minecraft\runtime\", JavaEntry.Folder)
                Dim DeleteButton As New MyIconButton With {.Logo = Logo.IconButtonStop, .Height = 24, .Width = 24}
                DeleteButton.IsEnabled = Not IsOfficial
                DeleteButton.ToolTip = If(DeleteButton.IsEnabled, "从列表中移除", "无法移除官方 Java")
                AddHandler DeleteButton.Click, Sub() ManuallyRemoveJava(JavaEntry)
                ToolTip.SetShowOnDisabled(DeleteButton, True)
                Buttons.Add(DeleteButton)
                '打开文件夹按钮
                Dim OpenButton As New MyIconButton With {.Logo = Logo.IconButtonOpen, .LogoScale = 1.1, .Height = 24, .Width = 24}
                OpenButton.ToolTip = "打开文件夹"
                AddHandler OpenButton.Click, Sub() OpenExplorer(JavaEntry.JavaExePath)
                Buttons.Add(OpenButton)
                JavaItem.Buttons = Buttons
            Next
            Dim ImportItem As New MyListItem With {
                .FontSize = 13, .Height = 24, .IsScaleAnimationEnabled = False, .Title = " 导入电脑中已有的 Java…", .Type = MyListItem.CheckType.Clickable}
            AddHandler ImportItem.PointerReleased,
            Sub(s, e)
                e.Handled = True
                ManuallyImportJava()
            End Sub
            ComboArgumentJavaSelect.Items.Add(ImportItem)
        Catch ex As Exception
            Logger.Error(ex, "更新 Java 下拉框失败")
        End Try
    End Sub
    Private Sub BtnAdvanceJavaSearch_Click() Handles BtnArgumentJavaSearch.Click
        ManuallySearchJava()
    End Sub

#End Region

    '版本隔离
    Private Sub ComboArgumentIndieV2_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboArgumentIndieV2.SelectionChanged
        If AniControlEnabled <> 0 Then Return
        Static IsReverting As Boolean = False
        If IsReverting Then Return
        If MyMsgBox("调整版本隔离设置后，你需要游戏存档、Mod 等文件手动迁移到新的游戏文件夹中。" & vbCrLf &
                    "如果发现存档消失，把这项设置改回来就能恢复。" & vbCrLf &
                    "如果你不会迁移存档，不建议修改这项设置！",
                    "警告", "我知道我在做什么", "取消", IsWarn:=True) = 2 Then
            IsReverting = True
            If e.RemovedItems.Count > 0 Then ComboArgumentIndieV2.SelectedItem = e.RemovedItems(0)
            IsReverting = False
        End If
        '实际应用设置
        Settings.Set("VersionArgumentIndieV2", sender.SelectedIndex = 0, Instance:=PageInstanceLeft.Instance)
    End Sub

    '启动前执行命令
    Private Sub TextAdvanceRun_TextChanged(sender As Object, e As TextChangedEventArgs) Handles TextAdvanceRun.TextChanged
        CheckAdvanceRunWait.IsVisible = TextAdvanceRun.Text <> ""
    End Sub

    '切换到全局设置
    Private Sub BtnSwitch_Click(sender As Object, e As EventArgs) Handles BtnSwitch.Click
        FrmMain.PageChange(FormMain.PageType.Setup, FormMain.PageSubType.SetupLaunch)
    End Sub

    '去除参数中的回车
    Private Sub ReplaceEnter(sender As MyTextBox, e As TextChangedEventArgs) Handles TextAdvanceJvm.TextChanged, TextAdvanceGame.TextChanged
        Dim NewText = sender.Text.ReplaceLineEndings(" ", mergeMultiple:=True)
        If NewText = sender.Text Then Return
        Dim CaretIndex = sender.CaretIndex
        sender.Text = NewText
        sender.CaretIndex = Math.Max(0, CaretIndex - 1)
    End Sub

End Class
