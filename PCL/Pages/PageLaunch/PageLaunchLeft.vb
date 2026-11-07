Imports Avalonia.Controls.Shapes
Imports Avalonia.Media

''' <summary>
''' 登录分页的统一接口。
''' </summary>
Public Interface ILoginPage
    ''' <summary>
    ''' 刷新页面显示的所有信息。
    ''' </summary>
    Sub Reload(KeepInput As Boolean)
End Interface

''' <summary>
''' 启动页面左侧部分：版本选择、登录方式切换、启动按钮与启动中界面。
''' </summary>
Public Class PageLaunchLeft
    Inherits MyPageLeft

    Public Sub New()
        InitControls()
    End Sub

#Region "基础状态"

    '加载当前版本
    Private IsLoad As Boolean = False
    Private IsLoadFinished As Boolean = False
    Private StartupLoadFailed As Boolean = False
    Private StartupStage As String = "初始化"
    Private StartupFailureMessage As String = ""
    Private StartupLoadWatchdog As System.Threading.Timer
    Private Const StartupLoaderTimeoutMs As Integer = 120000

    '愚人节动画变换
    Public ReadOnly Property AprilScaleTrans As New ScaleTransform()
    Public ReadOnly Property AprilPosTrans As New TranslateTransform()

    '进度条列
    Private ProgressLaunchingFinished As ColumnDefinition
    Private ProgressLaunchingUnfinished As ColumnDefinition

    Private Sub InitCustom()
        PanInput.RenderTransform = New ScaleTransform(1, 1)
        PanInput.RenderTransformOrigin = New RelativePoint(0.5, 0.5, RelativeUnit.Relative)
        PanLaunching.RenderTransform = New ScaleTransform(0.8, 0.8)
        PanLaunching.RenderTransformOrigin = New RelativePoint(0.5, 0.5, RelativeUnit.Relative)
        Dim AprilTrans As New TransformGroup()
        AprilTrans.Children.Add(AprilScaleTrans)
        AprilTrans.Children.Add(AprilPosTrans)
        PanLaunchBox.RenderTransform = AprilTrans
        PanLaunchBox.RenderTransformOrigin = New RelativePoint(0.5, 0.5, RelativeUnit.Relative)
        ProgressLaunchingFinished = PanProgress.ColumnDefinitions(0)
        ProgressLaunchingUnfinished = PanProgress.ColumnDefinitions(1)
        '初始化登录方式页面选择
        Select Case Settings.Get(Of McLoginType)("LoginType")
            Case McLoginType.Legacy
                RadioLoginType0.Checked = True
            Case McLoginType.Ms
                RadioLoginType5.Checked = True
        End Select
    End Sub

    Private Shared Sub WaitForStartupLoader(Loader As LoaderBase, Input As Object, TimeoutMessage As String, Optional IsForceRestart As Boolean = False)
        Try
            If IsForceRestart OrElse (Loader.State <> LoadState.Loading AndAlso Loader.State <> LoadState.Finished) Then
                Loader.Start(Input, IsForceRestart)
            End If

            Dim Remaining As Integer = StartupLoaderTimeoutMs
            While Loader.State = LoadState.Loading
                Thread.Sleep(10)
                Remaining -= 10
                If Remaining < 0 Then Throw New TimeoutException(TimeoutMessage)
            End While
            If Loader.State = LoadState.Finished Then Return
            If Loader.State = LoadState.Canceled Then Throw New OperationCanceledException("启动页加载器已取消：" & Loader.Name)
            If Loader.Error IsNot Nothing Then Throw Loader.Error
            Throw New Exception($"启动页加载器 {Loader.Name} 以状态 {Loader.State} 结束")
        Catch ex As TimeoutException
            Loader.Cancel()
            Throw
        End Try
    End Sub

    Private Sub SetStartupStage(Stage As String)
        StartupStage = Stage
        Logger.Info($"启动页初始化阶段：{Stage}")
        RunInUi(
            Sub()
                If Not IsLoadFinished AndAlso Not StartupLoadFailed Then LabVersion.Text = Stage
            End Sub)
    End Sub

    Private Sub FinishStartupLoad(Optional FailureMessage As String = Nothing)
        If IsLoadFinished Then Return
        StartupLoadWatchdog?.Dispose()
        StartupLoadWatchdog = Nothing
        StartupLoadFailed = FailureMessage IsNot Nothing
        StartupFailureMessage = If(FailureMessage, "")
        IsLoadFinished = True
        If StartupLoadFailed Then
            Logger.Error($"启动页初始化失败，阶段：{StartupStage}；原因：{StartupFailureMessage}", LogBehavior.None)
        End If
        RefreshButtonsUI()
        RefreshPage(False, False)
    End Sub

#End Region

#Region "页面加载"

    Private Sub PageLaunchLeft_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        If IsLoad Then
            '重新进入页面时 XAML 的 DynamicResource 会覆盖代码设置过的文字，需要强制刷新还原
            BtnLaunchState = -1
            RefreshPage(True, False, ForceReload:=True)
            RefreshButtonsUI()
        End If

        AprilPosTrans.X = 0
        AprilPosTrans.Y = 0

        If IsLoad Then Return
        '只在首次加载时初始化变换，重新进入页面时不能把启动中界面的缩放重置回 0.8
        InitCustom()
        IsLoad = True
        AniControlEnabled += 1

        '开始按钮
        AddHandler McInstanceListLoader.LoadingStateChanged, Sub(NewState, OldState) RefreshButtonsUI()
        AddHandler McFolderListLoader.LoadingStateChanged, Sub(NewState, OldState) RefreshButtonsUI()
        RefreshButtonsUI()

        StartupLoadWatchdog = New System.Threading.Timer(
            Sub()
                RunInUi(
                    Sub()
                        If IsLoadFinished Then Return
                        FinishStartupLoad($"初始化超时（{StartupStage}）。请查看启动器日志。")
                    End Sub)
            End Sub, Nothing, StartupLoaderTimeoutMs, Timeout.Infinite)

        '加载版本
        RunInNewThread(
        Sub()
            Try
            '自动整合包安装：准备
            SetStartupStage("检查待安装整合包")
            Dim PackInstallPath As String = Nothing
            If FileUtils.Exists(Paths.Base & "modpack.zip") Then PackInstallPath = Paths.Base & "modpack.zip"
            If FileUtils.Exists(Paths.Base & "modpack.mrpack") Then PackInstallPath = Paths.Base & "modpack.mrpack"
            If PackInstallPath IsNot Nothing Then
                Logger.Warn($"需自动安装整合包：{PackInstallPath}")
                PageSelectLeft.CreateMcFolderInCurrentPath()
                WaitForStartupLoader(McFolderListLoader, Nothing, "加载 Minecraft 文件夹列表超时")
            End If
            '确认 Minecraft 文件夹存在
            SetStartupStage("检查 Minecraft 文件夹")
            If McFolderSelected = "" OrElse Not DirectoryUtils.Exists(McFolderSelected) Then
                '无效的文件夹
                If McFolderSelected = "" Then
                    Logger.Info("没有已储存的 Minecraft 文件夹")
                Else
                    Logger.Warn($"Minecraft 文件夹无效，该文件夹已不存在：{McFolderSelected}")
                End If
                WaitForStartupLoader(McFolderListLoader, Nothing, "加载 Minecraft 文件夹列表超时", IsForceRestart:=True)
                If McFolderList.Any Then McFolderSelected = McFolderList.First.Location
            End If
            Dim StartupFolder As String = McFolderSelected
            If Settings.Get(Of Boolean)("SystemDebugDelay") Then Thread.Sleep(RandomInteger(500, 3000))
            '自动整合包安装
            If PackInstallPath IsNot Nothing Then
                Try
                    SetStartupStage("安装待处理的整合包")
                    Dim InstallLoader = ModpackInstall(PackInstallPath)
                    Logger.Info($"自动安装整合包已开始：{PackInstallPath}")
                    InstallLoader.WaitForExitTime(StartupLoaderTimeoutMs, TimeoutMessage:="自动安装整合包超时")
                    If InstallLoader.State = LoadState.Finished Then
                        Logger.Info($"自动安装整合包成功，清理安装包：{PackInstallPath}")
                        FileUtils.Delete(PackInstallPath)
                    End If
                Catch ex As Exception
                    If ex.IsCanceled Then
                        Logger.Info($"自动安装整合包已取消：{PackInstallPath}")
                    Else
                        Logger.Error(ex, $"自动安装整合包失败：{PackInstallPath}", LogBehavior.Alert)
                    End If
                End Try
            End If
            '确认 Minecraft 版本存在
            SetStartupStage("检查当前游戏版本")
            Dim Selection As String = ReadIni(StartupFolder & "PCL.ini", "Version")
            Dim Instance As McInstance = If(Selection = "", Nothing, New McInstance(Selection))
            If Instance Is Nothing OrElse Not PathUtils.IsParentOf(StartupFolder, Instance.PathVersion) OrElse Not Instance.Check() Then
                '无效的版本
                Logger.Info($"当前选择的 Minecraft 版本无效：{If(Instance Is Nothing, "null", Instance.PathVersion)}", If(IsNothing(Instance), LogBehavior.None, LogBehavior.ToastIfDebug))
                Dim InstanceListInput = TryCast(McInstanceListLoader.Input, String)
                Dim HasCurrentList = McInstanceListLoader.State = LoadState.Finished AndAlso
                                     InstanceListInput IsNot Nothing AndAlso PathUtils.IsSame(InstanceListInput, StartupFolder)
                If Not HasCurrentList Then
                    Dim IsCurrentListLoading = McInstanceListLoader.State = LoadState.Loading AndAlso
                                               InstanceListInput IsNot Nothing AndAlso PathUtils.IsSame(InstanceListInput, StartupFolder)
                    If Not IsCurrentListLoading Then
                        SetStartupStage("扫描 Minecraft 版本列表")
                        LoaderFolderRun(McInstanceListLoader, StartupFolder, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\")
                    End If
                    WaitForStartupLoader(McInstanceListLoader, StartupFolder, "加载 Minecraft 版本列表超时")
                End If
                Dim FirstInstance As McInstance = Nothing
                If McInstanceList.Any Then FirstInstance = McInstanceList.First.Value(0)
                If FirstInstance Is Nothing OrElse FirstInstance.Logo.Contains("RedstoneBlock") Then
                    Instance = Nothing
                    Logger.Info("无可用 Minecraft 版本")
                Else
                    Instance = FirstInstance
                    Logger.Info($"自动选择 Minecraft 版本：{Instance.PathVersion}")
                End If
            End If
            RunInUi(
            Sub()
                If IsLoadFinished Then Return
                McInstanceSelected = Instance '绕这一圈是为了避免 McVersionCheck 触发第二次版本改变
                FinishStartupLoad()
                '有可能选择的版本变化了，需要重新刷新
                If McLoginAble() = "" Then McLoginLoader.Start() '自动登录
                '用于自动化测试生成的程序是否可以正常运行
                If Environment.CommandLine.Contains("--test") Then
                    RunInThread(
                    Sub()
                        Thread.Sleep(500)
                        RunInUi(Sub() FrmMain.PageChange(FormMain.PageType.Download))
                        Thread.Sleep(500)
                        RunInUi(Sub() FrmMain.PageChange(FormMain.PageType.Download, FormMain.PageSubType.DownloadMod))
                        Thread.Sleep(500)
                        RunInUi(Sub() FrmMain.PageChange(FormMain.PageType.Setup))
                        Thread.Sleep(500)
                        RunInUi(Sub() FrmMain.PageChange(FormMain.PageType.Other))
                        Thread.Sleep(500)
                        RunInUi(Sub() BtnVersion_Click(Nothing, Nothing))
                        Thread.Sleep(500)
                        RunInUi(Sub() BtnMore_Click(Nothing, Nothing))
                        Thread.Sleep(500)
                        FormMain.EndProgramForce(123)
                    End Sub)
                End If
            End Sub)
            Catch ex As Exception
                If ex.IsCanceled Then
                    Logger.Info("启动页版本加载已取消")
                Else
                    Logger.Error(ex, $"启动页版本加载失败（阶段：{StartupStage}）", LogBehavior.Alert)
                End If
                RunInUi(
                Sub()
                    If IsLoadFinished Then Return
                    FinishStartupLoad(If(ex.IsCanceled, "启动页初始化已取消。", $"初始化失败（{StartupStage}），请查看启动器日志。"))
                End Sub)
            End Try
        End Sub, "Version Check", ThreadPriority.AboveNormal)

        '改变页面
        RefreshPage(False, False)

        AniControlEnabled -= 1
    End Sub

#End Region

#Region "切换大页面"

    ''' <summary>
    ''' 切换至启动中页面。
    ''' </summary>
    Public Sub PageChangeToLaunching()
        '修改登陆方式
        Select Case Settings.Get(Of McLoginType)("LoginType")
            Case McLoginType.Legacy
                LabLaunchingMethod.Text = "离线登录"
            Case McLoginType.Ms
                LabLaunchingMethod.Text = "正版登录"
            Case McLoginType.Nide
                LabLaunchingMethod.Text = "统一通行证"
            Case McLoginType.Auth
                LabLaunchingMethod.Text = "Authlib-Injector"
        End Select
        '初始化页面
        LabLaunchingName.Text = If(McInstanceSelected Is Nothing, "未知版本", McInstanceSelected.Name)
        LabLaunchingStage.Text = "初始化"
        LabLaunchingTitle.Text = If(CurrentLaunchOptions?.SaveBatch Is Nothing, "正在启动游戏", "正在导出启动脚本")
        LabLaunchingProgress.Text = "0.00 %"
        LabLaunchingProgress.Opacity = 1
        LabLaunchingProgress.IsVisible = True
        LabLaunchingProgressLeft.Opacity = 0.6
        LabLaunchingProgressLeft.IsVisible = True
        LabLaunchingDownload.Text = "0 B/s"
        LabLaunchingDownload.Opacity = 0
        LabLaunchingDownload.IsVisible = False
        LabLaunchingDownloadLeft.Opacity = 0
        LabLaunchingDownloadLeft.IsVisible = False
        ProgressLaunchingFinished.Width = New GridLength(0, GridUnitType.Star)
        ProgressLaunchingUnfinished.Width = New GridLength(1, GridUnitType.Star)
        PanLaunchingHint.Opacity = 0
        PanLaunchingHint.IsVisible = False
        ShowProgress = 0
        McLaunchProcess = Nothing
        McLaunchWatcher = Nothing
        '获取 “你知道吗” 提示
        LabLaunchingHint.Text = PageOtherTest.GetRandomHint()
        '初始化其他页面
        PanInput.IsHitTestVisible = False
        PanLaunching.IsHitTestVisible = False
        LoadLaunching.State.LoadingState = MyLoading.MyLoadingState.Run
        PanLaunching.IsVisible = True
        AniStart({
                AaOpacity(PanInput, 0, 50), '略作延迟，这样如果预检测失败，不会出现奇怪的弹一下的动画
                AaOpacity(PanInput, -PanInput.Opacity, 110,, New AniEaseInFluent, True),
                AaScaleTransform(PanInput, 1.2 - AniScaleX(PanInput), 160),
                AaOpacity(PanLaunching, 1 - PanLaunching.Opacity, 150, 100),
                AaScaleTransform(PanLaunching, 1 - AniScaleX(PanLaunching), 500, 100, New AniEaseOutBack(AniEasePower.Weak)),
                AaCode(Sub() PanLaunching.IsHitTestVisible = True, 150)
            }, "Launch State Page")
    End Sub
    ''' <summary>
    ''' 切换至登录页面。
    ''' </summary>
    Public Sub PageChangeToLogin()
        '退出启动界面时按当前缓存重新确认登录页，避免登录成功后仍停留在密码页
        RefreshPage(False, False, ForceReload:=True)
        PanInput.IsHitTestVisible = False
        PanLaunching.IsHitTestVisible = False
        LoadLaunching.State.LoadingState = MyLoading.MyLoadingState.Stop
        PanInput.IsVisible = True
        AniStart({
            AaOpacity(PanLaunching, -PanLaunching.Opacity, 150),
            AaScaleTransform(PanLaunching, 0.8 - AniScaleX(PanLaunching), 150,, New AniEaseOutFluent(AniEasePower.Weak)),
            AaOpacity(PanInput, 1 - PanInput.Opacity, 250, 50),
            AaScaleTransform(PanInput, 1 - AniScaleX(PanInput), 300, 50, New AniEaseOutBack(AniEasePower.Weak)),
            AaCode(Sub() PanInput.IsHitTestVisible = True, 200)
        }, "Launch State Page", True)
    End Sub

#End Region

#Region "切换登录页面"

    Private Enum PageType
        None
        Legacy
        Nide
        NideSkin
        Auth
        AuthSkin
        Ms
        MsSkin
    End Enum
    ''' <summary>
    ''' 当前页面的种类。
    ''' </summary>
    Private PageCurrent As PageType = PageType.None

    Private Function PageGet(Type As PageType) As Control
        Select Case Type
            Case PageType.Legacy
                If FrmLoginLegacy Is Nothing Then FrmLoginLegacy = New PageLoginLegacy
                Return FrmLoginLegacy
            Case PageType.Nide
                If FrmLoginNide Is Nothing Then FrmLoginNide = New PageLoginNide
                Return FrmLoginNide
            Case PageType.NideSkin
                If FrmLoginNideSkin Is Nothing Then FrmLoginNideSkin = New PageLoginNideSkin
                Return FrmLoginNideSkin
            Case PageType.Auth
                If FrmLoginAuth Is Nothing Then FrmLoginAuth = New PageLoginAuth
                Return FrmLoginAuth
            Case PageType.AuthSkin
                If FrmLoginAuthSkin Is Nothing Then FrmLoginAuthSkin = New PageLoginAuthSkin
                Return FrmLoginAuthSkin
            Case PageType.Ms
                If FrmLoginMs Is Nothing Then FrmLoginMs = New PageLoginMs
                Return FrmLoginMs
            Case PageType.MsSkin
                If FrmLoginMsSkin Is Nothing Then FrmLoginMsSkin = New PageLoginMsSkin
                Return FrmLoginMsSkin
            Case Else
                Throw New ArgumentOutOfRangeException("Type", "即将切换的登录分页编号越界")
        End Select
    End Function
    ''' <summary>
    ''' 切换现有登录页面种类，返回新页面的实例。
    ''' </summary>
    ''' <param name="Type">新页面的种类。</param>
    ''' <param name="Anim">是否显示动画。</param>
    Private Function PageChange(Type As PageType, Anim As Boolean) As Control
        Dim PageNew As Control = Nothing
        Try
            '确定更改的页面实例并实例化
            If PageCurrent = Type Then Return PageGet(Type)
            PageNew = PageGet(Type)

            '切换页面
            AniStop("FrmLogin PageChange")
            '清除页面关联性
            If TypeOf PageNew.Parent Is Panel Then CType(PageNew.Parent, Panel).Children.Remove(PageNew)
            If Anim Then
                AniStart({
                    AaOpacity(PanLogin, -PanLogin.Opacity, 100,, New AniEaseOutFluent),
                    AaCode(
                    Sub()
                        AniControlEnabled += 1
                        PanLogin.Children.Clear()
                        PanLogin.Children.Add(PageNew)
                        AniControlEnabled -= 1
                    End Sub, 100),
                    AaOpacity(PanLogin, 1, 100, 120, New AniEaseInFluent)
                }, "FrmLogin PageChange")
            Else
                AniControlEnabled += 1
                PanLogin.Children.Clear()
                PanLogin.Children.Add(PageNew)
                AniControlEnabled -= 1
            End If

            PageCurrent = Type
            Return PageNew
        Catch ex As Exception
            Logger.Error(ex, $"切换登录分页失败（{Type}）")
            Return PageNew
        End Try
    End Function

    ''' <summary>
    ''' 确认当前显示的子页面正确，并刷新该页面。
    ''' </summary>
    Public Sub RefreshPage(KeepInput As Boolean, Anim As Boolean, Optional ForceReload As Boolean = False, Optional IgnoreInstanceLoginType As Boolean = False)
        '获取页面的可用种类并回写缓存
        Dim Type As PageType
        Dim LoginPageType As Integer
        If McInstanceSelected IsNot Nothing AndAlso Not IgnoreInstanceLoginType Then
            LoginPageType = Settings.Get(Of Integer)("VersionServerLogin", Instance:=McInstanceSelected)
            '缓存当前版本的页面种类，下一次打开 McInstanceSelected 为空时才能加载出正确的页面
            Settings.Set("LoginPageType", LoginPageType)
        Else
            LoginPageType = Settings.Get(Of Integer)("LoginPageType")
        End If
        Select Case LoginPageType
            Case 0 '正版或离线
UnknownType:
                If RadioLoginType5.Checked Then
                    If Settings.Get(Of String)("CacheMsV2OAuthRefresh") = "" Then
                        Type = PageType.Ms
                    Else
                        Type = PageType.MsSkin
                    End If
                    Settings.Set("LoginType", McLoginType.Ms)
                Else
                    Type = PageType.Legacy
                    Settings.Set("LoginType", McLoginType.Legacy)
                End If
                PanType.IsVisible = True
                PanTypeOne.IsVisible = False
                RadioLoginType5.IsVisible = True
                RadioLoginType0.IsVisible = True
            Case 1 '仅正版
                If Settings.Get(Of String)("CacheMsV2OAuthRefresh") = "" Then
                    Type = PageType.Ms
                Else
                    Type = PageType.MsSkin
                End If
                Settings.Set("LoginType", McLoginType.Ms)
                PanType.IsVisible = False
                PanTypeOne.IsVisible = True
                PathTypeOne.Data = Geometry.Parse(Logo.IconButtonShield)
                LabTypeOne.Text = "正版登录"
                RadioLoginType5.IsVisible = True
                RadioLoginType0.IsVisible = False
            Case 2 '仅离线
                Type = PageType.Legacy
                Settings.Set("LoginType", McLoginType.Legacy)
                PanType.IsVisible = False
                PanTypeOne.IsVisible = True
                PathTypeOne.Data = Geometry.Parse(Logo.IconButtonOffline)
                LabTypeOne.Text = "离线登录"
            Case 3 '统一通行证
                If Settings.Get(Of String)("CacheNideAccess") = "" AndAlso Settings.Get(Of String)("CacheNideName") = "" Then
                    Type = PageType.Nide
                Else
                    Type = PageType.NideSkin
                End If
                Settings.Set("LoginType", McLoginType.Nide)
                PanType.IsVisible = False
                PanTypeOne.IsVisible = True
                PathTypeOne.Data = Geometry.Parse(Logo.IconButtonCard)
                LabTypeOne.Text = "统一通行证登录"
            Case 4 'Authlib-Injector
                If Settings.Get(Of String)("CacheAuthAccess") = "" AndAlso Settings.Get(Of String)("CacheAuthName") = "" Then
                    Type = PageType.Auth
                Else
                    Type = PageType.AuthSkin
                End If
                Settings.Set("LoginType", McLoginType.Auth)
                PanType.IsVisible = False
                PanTypeOne.IsVisible = True
                PathTypeOne.Data = Geometry.Parse(Logo.IconButtonCard)
                LabTypeOne.Text = If(McInstanceSelected Is Nothing, Settings.Get(Of String)("CacheAuthServerName"), Settings.Get(Of String)("VersionServerAuthName", Instance:=McInstanceSelected))
                If LabTypeOne.Text = "" Then LabTypeOne.Text = "第三方登录"
            Case Else
                Logger.Error($"未知的登录页面：{LoginPageType}", LogBehavior.Toast)
                GoTo UnknownType
        End Select
        '刷新页面
        If PageCurrent <> Type Then
            DirectCast(PageChange(Type, Anim), ILoginPage).Reload(KeepInput)
        ElseIf ForceReload Then
            DirectCast(PageGet(Type), ILoginPage).Reload(KeepInput)
        End If
        Select Case Settings.Get(Of McLoginType)("LoginType")
            Case McLoginType.Ms
                RadioLoginType5.Checked = True
            Case McLoginType.Legacy
                RadioLoginType0.Checked = True
        End Select
    End Sub
    Private Sub RadioLoginType_Change(sender As Object, raiseByMouse As Boolean) Handles RadioLoginType0.Check, RadioLoginType5.Check
        If raiseByMouse Then RefreshPage(True, True)
    End Sub

#End Region

#Region "皮肤"

    '微软正版皮肤
    Public Shared SkinMs As New LoaderTask(Of (String, String), String)("Loader Skin Ms", AddressOf SkinMsLoad, AddressOf SkinMsInput, ThreadPriority.AboveNormal)
    Private Shared Function SkinMsInput() As (String, String)
        '获取名称
        Return (Settings.Get(Of String)("CacheMsV2Name"), Settings.Get(Of String)("CacheMsV2Uuid"))
    End Function
    Private Shared Sub SkinMsLoad(Data As LoaderTask(Of (String, String), String))
        '清空已有皮肤
        '如果在输入时清空皮肤，若输入内容一样则不会执行 Load 方法，导致皮肤不被加载
        RunInUi(Sub() If FrmLoginMsSkin IsNot Nothing AndAlso FrmLoginMsSkin.Skin IsNot Nothing Then FrmLoginMsSkin.Skin.Clear())
        '获取 Url
        Dim UserName As String = Data.Input.Item1
        Dim Uuid As String = Data.Input.Item2
        If UserName = "" Then
            Data.Output = PathImage & "Skins/" & McSkinSex(McLoginLegacyUuid(UserName)) & ".png"
            Logger.Info("获取微软正版皮肤失败，ID 为空")
            GoTo Finish
        End If
        Try
            Dim Result As String = McSkinGetAddress(Uuid, "Ms")
            If Data.IsCanceled Then Throw New OperationCanceledException("当前任务已取消：" & UserName)
            Result = McSkinDownload(Result)
            If Data.IsCanceled Then Throw New OperationCanceledException("当前任务已取消：" & UserName)
            Data.Output = Result
        Catch ex As Exception
            If ex.IsCanceled Then
                Data.Output = ""
                Return
            ElseIf ex.GetDisplay(False).Contains("(429)") Then
                Data.Output = PathImage & "Skins/" & McSkinSex(McLoginLegacyUuid(UserName)) & ".png"
                Logger.Error($"获取正版皮肤失败（{UserName}）：获取皮肤太过频繁，请 5 分钟后再试！", LogBehavior.Toast)
            ElseIf ex.GetDisplay(False).Contains("未设置自定义皮肤") Then
                Data.Output = PathImage & "Skins/" & McSkinSex(McLoginLegacyUuid(UserName)) & ".png"
                Logger.Info("用户未设置自定义皮肤，跳过皮肤加载")
            Else
                Data.Output = PathImage & "Skins/" & McSkinSex(McLoginLegacyUuid(UserName)) & ".png"
                Logger.Error(ex, $"获取微软正版皮肤失败（{UserName}）", LogBehavior.Toast)
            End If
        End Try
Finish:
        '刷新显示
        If FrmLoginMsSkin IsNot Nothing Then
            RunInUi(AddressOf FrmLoginMsSkin.Skin.Load)
        ElseIf Not Data.IsCanceled Then '如果已经中断，Input 也被清空，就不会再次刷新
            Data.Input = Nothing '清空输入，因为皮肤实际上没有被渲染，如果不清空切换到页面的 Start 会由于输入相同而不渲染
        End If
    End Sub

    '离线皮肤
    Public Shared SkinLegacy As New LoaderTask(Of (String, String), String)("Loader Skin Legacy", AddressOf SkinLegacyLoad, AddressOf SkinLegacyInput, ThreadPriority.AboveNormal)
    Private Shared Function SkinLegacyInput() As (String, String)
        '根据类型判断输入
        Dim Type As Integer = Settings.Get(Of Integer)("LaunchSkinType")
        Select Case Type
            Case 0
                If FrmLoginLegacy IsNot Nothing AndAlso FrmLoginLegacy.IsReloaded Then
                    Return ("0", If(FrmLoginLegacy.ComboName.Text.Trim, ""))
                ElseIf Settings.Get(Of String)("LoginLegacyName") = "" Then
                    Return ("0", "")
                Else
                    Return ("0", If(Settings.Get(Of String)("LoginLegacyName").ToString.BeforeFirst("¨"), ""))
                End If
            Case 3
                Return ("3", Settings.Get(Of String)("LaunchSkinID"))
            Case Else
                Return (Type.ToString, "")
        End Select
    End Function
    Private Shared Sub SkinLegacyLoad(Data As LoaderTask(Of (String, String), String))
        '清空已有皮肤
        RunInUi(Sub() If FrmLoginLegacy IsNot Nothing AndAlso FrmLoginLegacy.Skin IsNot Nothing Then FrmLoginLegacy.Skin.Clear())
        '获取 Url
        Select Case CInt(Data.Input.Item1)
            Case 0 '默认
                Data.Output = PathImage & "Skins/" & McSkinSex(McLoginLegacyUuid(Data.Input.Item2)) & ".png"
            Case 1 'Steve
UseDefault:
                Data.Output = PathImage & "Skins/Steve.png"
            Case 2 'Alex
                Data.Output = PathImage & "Skins/Alex.png"
            Case 3 '正版
                Dim ID As String = Data.Input.Item2
                Try
                    If ID.Count < 2 Then
                        Data.Output = PathImage & "Skins/Steve.png"
                    Else
                        Dim Result As String = McLoginMojangUuid(ID, True)
                        If Data.IsCanceled Then Throw New OperationCanceledException("当前任务已取消：" & ID)
                        Result = McSkinGetAddress(Result, "Mojang")
                        If Data.IsCanceled Then Throw New OperationCanceledException("当前任务已取消：" & ID)
                        Result = McSkinDownload(Result)
                        If Data.IsCanceled Then Throw New OperationCanceledException("当前任务已取消：" & ID)
                        Data.Output = Result
                    End If
                Catch ex As Exception
                    If ex.IsCanceled Then
                        Data.Output = ""
                        Return
                    ElseIf ex.GetDisplay(False).Contains("(429)") Then
                        Data.Output = PathImage & "Skins/" & McSkinSex(McLoginLegacyUuid(ID)) & ".png"
                        Logger.Info($"获取离线登录使用的正版皮肤失败（{ID}）：获取皮肤太过频繁，请 5 分钟后再试！")
                    Else
                        Data.Output = PathImage & "Skins/" & McSkinSex(McLoginLegacyUuid(ID)) & ".png"
                        Logger.Warn(ex, $"获取离线登录使用的正版皮肤失败（{ID}）")
                    End If
                End Try
            Case 4 '自定义
                If Not FileUtils.Exists(Paths.AppDataThenName & "CustomSkin.png") Then
                    Hint("未找到离线皮肤自定义文件，可能它已被删除。PCL 将使用默认的 Steve 皮肤！")
                    Settings.Set("LaunchSkinType", 1)
                    GoTo UseDefault
                End If
                Data.Output = Paths.AppDataThenName & "CustomSkin.png"
        End Select
        '刷新显示
        If FrmLoginLegacy IsNot Nothing Then
            RunInUi(AddressOf FrmLoginLegacy.Skin.Load)
        ElseIf Not Data.IsCanceled Then '如果已经中断，Input 也被清空，就不会再次刷新
            Data.Input = Nothing '清空输入，因为皮肤实际上没有被渲染，如果不清空切换到页面的 Start 会由于输入相同而不渲染
        End If
    End Sub

    '统一通行证皮肤
    Public Shared SkinNide As New LoaderTask(Of (String, String), String)("Loader Skin Nide", AddressOf SkinNideLoad, AddressOf SkinNideInput, ThreadPriority.AboveNormal)
    Private Shared Function SkinNideInput() As (String, String)
        '获取名称
        Return (Settings.Get(Of String)("CacheNideName"), Settings.Get(Of String)("CacheNideUuid"))
    End Function
    Private Shared Sub SkinNideLoad(Data As LoaderTask(Of (String, String), String))
        '清空已有皮肤
        '如果在输入时清空皮肤，若输入内容一样则不会执行 Load 方法，导致皮肤不被加载
        RunInUi(Sub() If FrmLoginNideSkin IsNot Nothing AndAlso FrmLoginNideSkin.Skin IsNot Nothing Then FrmLoginNideSkin.Skin.Clear())
        '获取 Url
        Dim UserName As String = Data.Input.Item1
        Dim Uuid As String = Data.Input.Item2
        If UserName = "" Then
            Data.Output = PathImage & "Skins/" & McSkinSex(McLoginLegacyUuid(UserName)) & ".png"
            Logger.Info("获取统一通行证皮肤失败，ID 为空")
            GoTo Finish
        End If
        Try
            Dim Result As String = McSkinGetAddress(Uuid, "Nide")
            If Data.IsCanceled Then Throw New OperationCanceledException("当前任务已取消：" & UserName)
            Result = McSkinDownload(Result)
            If Data.IsCanceled Then Throw New OperationCanceledException("当前任务已取消：" & UserName)
            Data.Output = Result
        Catch ex As Exception
            If ex.IsCanceled Then
                Data.Output = ""
                Return
            ElseIf ex.GetDisplay(False).Contains("(429)") Then
                Data.Output = PathImage & "Skins/Steve.png"
                Logger.Error($"获取统一通行证皮肤失败（{UserName}）：获取皮肤太过频繁，请 5 分钟后再试！", LogBehavior.Toast)
            ElseIf ex.GetDisplay(False).Contains("未设置自定义皮肤") Then
                Data.Output = PathImage & "Skins/Steve.png"
                Logger.Info("用户未设置自定义皮肤，跳过皮肤加载")
            Else
                Data.Output = PathImage & "Skins/Steve.png"
                Logger.Error(ex, $"获取统一通行证皮肤失败（{UserName}）", LogBehavior.Toast)
            End If
        End Try
Finish:
        '刷新显示
        If FrmLoginNideSkin IsNot Nothing Then
            RunInUi(AddressOf FrmLoginNideSkin.Skin.Load)
        ElseIf Not Data.IsCanceled Then '如果已经中断，Input 也被清空，就不会再次刷新
            Data.Input = Nothing '清空输入，因为皮肤实际上没有被渲染，如果不清空切换到页面的 Start 会由于输入相同而不渲染
        End If
    End Sub

    'Authlib-Injector 皮肤
    Public Shared SkinAuth As New LoaderTask(Of (String, String), String)("Loader Skin Auth", AddressOf SkinAuthLoad, AddressOf SkinAuthInput, ThreadPriority.AboveNormal)
    Private Shared Function SkinAuthInput() As (String, String)
        '获取名称
        Return (Settings.Get(Of String)("CacheAuthName"), Settings.Get(Of String)("CacheAuthUuid"))
    End Function
    Private Shared Sub SkinAuthLoad(Data As LoaderTask(Of (String, String), String))
        '清空已有皮肤
        '如果在输入时清空皮肤，若输入内容一样则不会执行 Load 方法，导致皮肤不被加载
        RunInUi(Sub() If FrmLoginAuthSkin IsNot Nothing AndAlso FrmLoginAuthSkin.Skin IsNot Nothing Then FrmLoginAuthSkin.Skin.Clear())
        '获取 Url
        Dim UserName As String = Data.Input.Item1
        Dim UUID As String = Data.Input.Item2
        If UserName = "" Then
            Data.Output = PathImage & "Skins/Steve.png"
            Logger.Info("获取 Authlib-Injector 皮肤失败，ID 为空")
            GoTo Finish
        End If
        Try
            Dim Result As String = McSkinGetAddress(UUID, "Auth")
            If Data.IsCanceled Then Throw New OperationCanceledException("当前任务已取消：" & UserName)
            Result = McSkinDownload(Result)
            If Data.IsCanceled Then Throw New OperationCanceledException("当前任务已取消：" & UserName)
            Data.Output = Result
        Catch ex As Exception
            If ex.IsCanceled Then
                Data.Output = ""
                Return
            ElseIf ex.GetDisplay(False).Contains("(429)") Then
                Data.Output = PathImage & "Skins/Steve.png"
                Logger.Error($"获取 Authlib-Injector 皮肤失败（{UserName}）：获取皮肤太过频繁，请 5 分钟后再试！", LogBehavior.Toast)
            ElseIf ex.GetDisplay(False).Contains("未设置自定义皮肤") Then
                Data.Output = PathImage & "Skins/Steve.png"
                Logger.Info("用户未设置自定义皮肤，跳过皮肤加载")
            Else
                Data.Output = PathImage & "Skins/Steve.png"
                Logger.Error(ex, $"获取 Authlib-Injector 皮肤失败（{UserName}）", LogBehavior.Toast)
            End If
        End Try
Finish:
        '刷新显示
        If FrmLoginAuthSkin IsNot Nothing Then
            RunInUi(AddressOf FrmLoginAuthSkin.Skin.Load)
        ElseIf Not Data.IsCanceled Then '如果已经中断，Input 也被清空，就不会再次刷新
            Data.Input = Nothing '清空输入，因为皮肤实际上没有被渲染，如果不清空切换到页面的 Start 会由于输入相同而不渲染
        End If
    End Sub

    '全部皮肤加载器
    '需要放在其中元素的后面，否则会因为它提前被加载而莫名其妙变成 Nothing
    Public Shared SkinLoaders As New List(Of LoaderTask(Of (String, String), String)) From {SkinMs, SkinLegacy, SkinNide, SkinAuth}

#End Region

    '版本选择按钮
    Private Sub BtnVersion_Click(sender As Object, e As PointerReleasedEventArgs) Handles BtnVersion.Click
        If McLaunchLoader.State = LoadState.Loading Then Return
        FrmMain.PageChange(FormMain.PageType.InstanceSelect)
    End Sub
    '启动按钮
    Public Sub LaunchButtonClick() Handles BtnLaunch.Click
        If McLaunchLoader.State = LoadState.Loading OrElse Not BtnLaunch.IsEnabled OrElse
            (FrmMain.PageRight IsNot Nothing AndAlso FrmMain.PageRight.PageState <> MyPageRight.PageStates.ContentStay AndAlso FrmMain.PageRight.PageState <> MyPageRight.PageStates.ContentEnter) Then Return
        '愚人节处理
        If IsAprilEnabled AndAlso Not IsAprilGiveup Then
            ThemeUnlock(12, False, "隐藏主题 滑稽彩 已解锁！")
            IsAprilGiveup = True
            Settings.Set("AprilYear", Date.Now.Year)
            FrmLaunchLeft.AprilScaleTrans.ScaleX = 1
            FrmLaunchLeft.AprilScaleTrans.ScaleY = 1
            FrmLaunchLeft.AprilPosTrans.X = 0
            FrmLaunchLeft.AprilPosTrans.Y = 0
            FrmMain.BtnExtraApril.ShowRefresh()
        End If
        '实际的启动
        If BtnLaunch.Text = "启动游戏" Then
            If Not FormMain.RequirePremiumLogin() Then Return
            McLaunchStart()
        ElseIf BtnLaunch.Text = "下载游戏" Then
            FrmMain.PageChange(FormMain.PageType.Download, FormMain.PageSubType.DownloadInstall)
        End If
    End Sub
    Private BtnLaunchState As Integer = -1
    Private BtnLaunchInstance As McInstance = Nothing
    Public Sub RefreshButtonsUI() Handles BtnLaunch.Loaded
        If BtnLaunch Is Nothing OrElse Not BtnLaunch.IsLoaded Then Return
        '获取当前状态
        Dim CurrentState As Integer
        If Not IsLoadFinished Then
            CurrentState = 0
        ElseIf StartupLoadFailed Then
            CurrentState = 4
        Else
            If McInstanceSelected Is Nothing Then
                If Settings.Get(Of Boolean)("UiHiddenPageDownload") AndAlso Not PageSetupUI.HiddenForceShow Then
                    CurrentState = 1
                Else
                    CurrentState = 2
                End If
            Else
                CurrentState = 3
            End If
        End If
        '更新状态
        If CurrentState = BtnLaunchState AndAlso
           If(McInstanceSelected Is Nothing, "", McInstanceSelected.PathVersion) = If(BtnLaunchInstance Is Nothing, "", BtnLaunchInstance.PathVersion) Then GoTo ExitRefresh
        BtnLaunchInstance = McInstanceSelected
        BtnLaunchState = CurrentState
        Select Case CurrentState
            Case 0
                Logger.Info("启动按钮：正在加载 Minecraft 版本")
                BtnLaunch.Text = "正在加载"
                BtnLaunch.IsEnabled = False
                LabVersion.Text = "正在加载中，请稍候"
                BtnVersion.IsEnabled = False
                BtnMore.IsVisible = False
            Case 1
                Logger.Info("启动按钮：无 Minecraft 版本，下载已禁用")
                BtnLaunch.Text = "启动游戏"
                BtnLaunch.IsEnabled = False
                LabVersion.Text = "未找到可用的游戏版本"
                BtnVersion.IsEnabled = True
                BtnMore.IsVisible = False
            Case 2
                Logger.Info("启动按钮：无 Minecraft 版本，要求下载")
                BtnLaunch.Text = "下载游戏"
                BtnLaunch.IsEnabled = True
                LabVersion.Text = "未找到可用的游戏版本"
                BtnVersion.IsEnabled = True
                BtnMore.IsVisible = False
            Case 3
                Logger.Info($"启动按钮：Minecraft 版本：{McInstanceSelected.PathVersion}")
                BtnLaunch.Text = "启动游戏"
                BtnVersion.IsEnabled = True
                BtnLaunch.IsEnabled = True
                LabVersion.Text = McInstanceSelected.Name
            Case 4
                BtnLaunch.Text = "加载失败"
                BtnLaunch.IsEnabled = False
                BtnVersion.IsEnabled = True
                BtnMore.IsVisible = False
                LabVersion.Text = StartupFailureMessage
        End Select
ExitRefresh:
        '功能隐藏
        BtnVersion.IsVisible = Not PageSetupUI.HiddenForceShow AndAlso Not Settings.Get(Of Boolean)("UiHiddenFunctionSelect")
        If CurrentState = 3 Then BtnMore.IsVisible = BtnVersion.IsVisible
    End Sub

    Public Sub RefreshAfterPageEnter()
        If Not IsLoadFinished Then Return
        BtnLaunchState = -1
        RefreshPage(True, False, ForceReload:=True)
        RefreshButtonsUI()
    End Sub

    '取消按钮
    Public Sub BtnCancel_Click() Handles BtnCancel.Click
        If McLaunchLoaderReal IsNot Nothing Then
            McLaunchLoaderReal.Cancel()
            McLaunchLog("已取消启动")
            Try
                If McLaunchWatcher IsNot Nothing Then
                    McLaunchWatcher.Kill()
                ElseIf McLaunchProcess IsNot Nothing Then
                    If Not McLaunchProcess.HasExited Then McLaunchProcess.Kill()
                End If
            Catch ex As Exception
                Logger.Error(ex, "取消启动结束进程失败", LogBehavior.Toast)
            End Try
        End If
    End Sub
    '版本设置按钮
    Private Sub BtnMore_Click(sender As Object, e As PointerReleasedEventArgs) Handles BtnMore.Click
        If McLaunchLoader.State = LoadState.Loading Then Return
        If McInstanceSelected Is Nothing Then Return
        If McInstanceLock.IsLocked(McInstanceSelected) Then
            Hint("该版本正在被修改，暂时无法进入版本设置！", HintType.Red)
            Return
        End If
        McInstanceSelected.Load()
        PageInstanceLeft.Instance = McInstanceSelected
        FrmMain.PageChange(FormMain.PageType.InstanceSetup, 0)
    End Sub
    ''' <summary>
    ''' 每 0.1s 执行一次，刷新启动的数据 UI 显示。
    ''' </summary>
    Public Sub LaunchingRefresh()
        Try
            If McLaunchLoaderReal Is Nothing OrElse McLaunchLoaderReal.State = LoadState.Canceled Then Return
            '阶段状态获取
            Dim IsLaunched As Boolean = False '是否已经启动游戏，只是在等待窗口
            Try
                For Each Loader In McLaunchLoaderReal.GetLoaderList(False)
                    If Loader.State = LoadState.Loading OrElse Loader.State = LoadState.Waiting Then
                        LabLaunchingStage.Text = Loader.Name
                        IsLaunched = Loader.Name = "等待游戏窗口出现" OrElse Loader.Name = "结束处理"
                        Exit Try
                    End If
                Next
                LabLaunchingStage.Text = "已完成"
            Catch ex As Exception
                Logger.Warn(ex, "获取是否启动完成失败，可能是由于启动状态改变导致集合已修改")
                Return
            End Try
            LabLaunchingTitle.Text = If(IsLaunched OrElse McLaunchLoaderReal.State = LoadState.Finished,
                "已启动游戏",
                If(CurrentLaunchOptions.SaveBatch Is Nothing, "正在启动游戏", "正在导出启动脚本"))
            If AniIsRun("Launch State Page") Then IsLaunched = False '等待页面切换动画完成
            '更新进度
            Dim ActualProgress = McLaunchLoaderReal.Progress
            If ActualProgress >= ShowProgress Then ShowProgress += (ActualProgress - ShowProgress) * 0.1 + 0.0025 '向实际进度靠一点
            If ActualProgress <= ShowProgress Then ShowProgress = ActualProgress '原来或处理后变得比实际进度高，直接回退
            If IsLaunched Then ShowProgress = 1 '如果已经完成了，就不卖关子了
            LabLaunchingProgress.Text = (ShowProgress * 100).ToString("0.00") & " %"
            '更新下载速度
            Dim HasLaunchDownloader As Boolean = False
            Try
                If NetManager.Speed = 0 AndAlso Not LabLaunchingDownload.IsVisible Then
                    '可能只是在检查文件，没有实际下载
                    HasLaunchDownloader = False
                Else
                    For Each Loader In NetManager.Tasks
                        If Loader.RealParent IsNot Nothing AndAlso Loader.RealParent.Name = "Minecraft 启动" AndAlso Loader.State = LoadState.Loading Then HasLaunchDownloader = True
                    Next
                End If
            Catch ex As Exception
                Logger.Warn(ex, "获取 Minecraft 启动下载器失败，可能是因为启动被取消")
                HasLaunchDownloader = False
            End Try
            LabLaunchingDownload.Text = StringUtils.FormatByteSize(NetManager.Speed) & "/s"
            '进度改变动画
            Dim AnimList As New List(Of AniData) From {
                 AaGridLengthWidth(ProgressLaunchingFinished, ShowProgress - ProgressLaunchingFinished.Width.Value, 130,, New AniEaseOutFluent),
                 AaGridLengthWidth(ProgressLaunchingUnfinished, 1 - ShowProgress - ProgressLaunchingUnfinished.Width.Value, 130,, New AniEaseOutFluent)
            }
            If HasLaunchDownloader = Not LabLaunchingDownload.IsVisible Then 'IsDownloadStateChanged
                LabLaunchingDownload.IsVisible = True
                LabLaunchingDownloadLeft.IsVisible = True
                AnimList.AddRange({
                    AaOpacity(LabLaunchingDownload, If(HasLaunchDownloader, 1, 0) - LabLaunchingDownload.Opacity, 90),
                    AaOpacity(LabLaunchingDownloadLeft, If(HasLaunchDownloader, 0.5, 0) - LabLaunchingDownloadLeft.Opacity, 90),
                    AaCode(
                    Sub()
                        If Not HasLaunchDownloader Then
                            LabLaunchingDownload.IsVisible = False
                            LabLaunchingDownloadLeft.IsVisible = False
                        End If
                    End Sub, 110)
                })
            End If
            If (Not IsLaunched) = Not LabLaunchingProgress.IsVisible Then 'IsProgressStateChanged
                LabLaunchingProgress.IsVisible = True
                LabLaunchingProgressLeft.IsVisible = True
                If IsLaunched Then PanLaunchingHint.IsVisible = True
                AnimList.AddRange({
                    AaOpacity(LabLaunchingProgress, If(Not IsLaunched, 1, 0) - LabLaunchingProgress.Opacity, 90),
                    AaOpacity(LabLaunchingProgressLeft, If(Not IsLaunched, 0.5, 0) - LabLaunchingProgressLeft.Opacity, 90),
                    AaOpacity(PanLaunchingHint, If(IsLaunched, 1, 0) - PanLaunchingHint.Opacity, 90)
                })
            End If
            AniStart(AnimList, "Launching Progress")
        Catch ex As Exception
            Logger.Error(ex, "刷新启动信息失败")
        End Try
    End Sub
    Private ShowProgress As Double = 0
    '尺寸改变动画
    Private IsWidthAnimating As Boolean = False
    Private Sub PanLaunchingInfo_SizeChangedW(sender As Object, e As SizeChangedEventArgs) Handles PanLaunchingInfo.SizeChanged
        Dim DeltaWidth As Double = e.NewSize.Width - e.PreviousSize.Width
        If e.PreviousSize.Width = 0 OrElse IsWidthAnimating OrElse Math.Abs(DeltaWidth) < 1 OrElse PanLaunchingInfo.Bounds.Width = 0 Then Return
        AniStart({
            AaWidth(PanLaunchingInfo, DeltaWidth, 180,, New AniEaseOutFluent),
            AaCode(Sub()
                       IsWidthAnimating = False
                       PanLaunchingInfo.Width = Double.NaN '恢复自动宽度
                   End Sub,, True)
        }, "Launching Info Width")
        IsWidthAnimating = True
        PanLaunchingInfo.Width = e.PreviousSize.Width
    End Sub
    Private IsHeightAnimating As Boolean = False
    Private Sub PanLaunchingInfo_SizeChangedH(sender As Object, e As SizeChangedEventArgs) Handles PanLaunchingInfo.SizeChanged
        Dim DeltaHeight As Double = e.NewSize.Height - e.PreviousSize.Height
        If e.PreviousSize.Height = 0 OrElse IsHeightAnimating OrElse Math.Abs(DeltaHeight) < 1 OrElse PanLaunchingInfo.Bounds.Height = 0 Then Return
        AniStart({
            AaHeight(PanLaunchingInfo, DeltaHeight, 180,, New AniEaseOutFluent),
            AaCode(Sub()
                       IsHeightAnimating = False
                       PanLaunchingInfo.Height = Double.NaN '恢复自动高度
                   End Sub,, True)
        }, "Launching Info Height")
        IsHeightAnimating = True
        PanLaunchingInfo.Height = e.PreviousSize.Height
    End Sub

End Class
