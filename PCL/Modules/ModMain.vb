Imports Avalonia.Layout
Imports Avalonia.Media
Imports System.Collections.Concurrent
Imports System.Collections.ObjectModel
Imports System.Runtime.InteropServices

Public Module ModMain

#Region "全局引用"

    ''' <summary>
    ''' 主窗体。在 FormMain 构造时赋值。
    ''' </summary>
    Public FrmMain As FormMain

    ''' <summary>
    ''' 启动时显示的 PCL 图标（对应原版的 SplashScreen）。在启动器加载完成后关闭。
    ''' </summary>
    Public FrmStart As Window

    ''' <summary>
    ''' 版本 Mod 管理页面。在页面创建时赋值。
    ''' </summary>
    Public FrmInstanceMod As PageInstanceMod

    ''' <summary>
    ''' 版本设置页面。在页面创建时赋值。
    ''' </summary>
    Public FrmInstanceSetup As PageInstanceSetup

    ''' <summary>
    ''' 版本概览页面。在页面创建时赋值。
    ''' </summary>
    Public FrmInstanceOverall As PageInstanceOverall

    ''' <summary>
    ''' 不可安装 Mod 的版本的 Mod 管理占位页面。在页面创建时赋值。
    ''' </summary>
    Public FrmInstanceModDisabled As PageInstanceModDisabled

    ''' <summary>
    ''' 版本导出页面。在页面创建时赋值。
    ''' </summary>
    Public FrmInstanceExport As PageInstanceExport

    ''' <summary>
    ''' 版本修改页面。在页面创建时赋值。
    ''' </summary>
    Public FrmDownloadModify As PageDownloadModify

    ''' <summary>
    ''' 任务管理页面。在页面创建时赋值。
    ''' </summary>
    Public FrmSpeedLeft As PageSpeedLeft
    Public FrmSpeedRight As PageSpeedRight

    ''' <summary>
    ''' 启动页面的左右部分。在页面创建时赋值。
    ''' </summary>
    Public FrmLaunchLeft As PageLaunchLeft
    Public FrmLaunchRight As PageLaunchRight

    ''' <summary>
    ''' 版本选择页面的左右部分。在页面创建时赋值。
    ''' </summary>
    Public FrmSelectLeft As PageSelectLeft
    Public FrmSelectRight As PageSelectRight

    ''' <summary>
    ''' 版本设置页面的左侧导航。在页面创建时赋值。
    ''' </summary>
    Public FrmInstanceLeft As PageInstanceLeft

    ''' <summary>
    ''' 联机页面。在页面创建时赋值。
    ''' </summary>
    Public FrmLinkMain As PageLinkMain

    ''' <summary>
    ''' 各登录方式的子页面。在页面创建时赋值。
    ''' </summary>
    Public FrmLoginMs As PageLoginMs
    Public FrmLoginMsSkin As PageLoginMsSkin
    Public FrmLoginLegacy As PageLoginLegacy
    Public FrmLoginNide As PageLoginNide
    Public FrmLoginNideSkin As PageLoginNideSkin
    Public FrmLoginAuth As PageLoginAuth
    Public FrmLoginAuthSkin As PageLoginAuthSkin

#End Region

#Region "愚人节"

    Private _IsAprilEnabled As Boolean? = Nothing

    ''' <summary>
    ''' 当前是否处于愚人节模式。
    ''' </summary>
    Public ReadOnly Property IsAprilEnabled As Boolean
        Get
            If _IsAprilEnabled Is Nothing Then
                _IsAprilEnabled =
                    Date.Now.Month = 4 AndAlso Date.Now.Day = 1 AndAlso
                    Settings.Get(Of Integer)("AprilYear") <> Date.Now.Year
            End If
            Return _IsAprilEnabled.Value
        End Get
    End Property

    ''' <summary>
    ''' 是否已经放弃愚人节彩蛋。
    ''' </summary>
    Public IsAprilGiveup As Boolean = False

#End Region

#Region "弹出提示"

    ''' <summary>
    ''' 提示信息的种类。
    ''' 该枚举在自定义事件中使用，是公开 API 的一部分。
    ''' </summary>
    Public Enum HintType
        Blue
        Green
        Red
    End Enum

    Private Class HintMessage
        Public Text As String
        Public Type As HintType
        Public Log As Boolean
    End Class

    Private Class HintControlInfo
        Public Uuid As Integer
        Public Reusable As Boolean = True
    End Class

    ''' <summary>
    ''' 等待弹出的提示列表。
    ''' </summary>
    Private HintWaiting As New ConcurrentQueue(Of HintMessage)

    ''' <summary>
    ''' 在窗口左下角弹出提示文本。
    ''' </summary>
    Public Sub Hint(Text As String, Optional Type As HintType = HintType.Blue, Optional Log As Boolean = True)
        HintWaiting.Enqueue(New HintMessage With {.Text = Localization.TranslateText(Text), .Type = Type, .Log = Log})
        RunInUi(AddressOf HintTick)
    End Sub

    Private Sub HintTick()
        Try
            Dim CurrentHint As HintMessage = Nothing
            Do While HintWaiting.TryDequeue(CurrentHint)
                CurrentHint.Text = If(CurrentHint.Text, "").ReplaceLineEndings(" ", mergeMultiple:=True)
                If FrmMain Is Nothing OrElse FrmMain.PanHint Is Nothing Then Continue Do
                If FrmMain.PanHint.Children.Count >= 20 Then Continue Do
                '查找可以复用的重复提示
                Dim DoubleStack As Border = Nothing
                Dim DoubleInfo As HintControlInfo = Nothing
                For Each Stack In FrmMain.PanHint.Children.OfType(Of Border)()
                    Dim Info = TryCast(Stack.Tag, HintControlInfo)
                    Dim HintText = TryCast(Stack.Child, TextBlock)
                    If Info IsNot Nothing AndAlso Info.Reusable AndAlso HintText IsNot Nothing AndAlso HintText.Text = CurrentHint.Text Then
                        DoubleStack = Stack
                        DoubleInfo = Info
                    End If
                Next
                '渐变颜色
                Dim TargetColor0, TargetColor1 As MyColor
                Dim Percent As Double = 0.3
                Select Case CurrentHint.Type
                    Case HintType.Blue
                        TargetColor0 = New MyColor(215, 37, 155, 252)
                        TargetColor1 = New MyColor(215, 10, 142, 252)
                    Case HintType.Green
                        TargetColor0 = New MyColor(215, 33, 177, 33)
                        TargetColor1 = New MyColor(215, 29, 160, 29)
                    Case Else 'HintType.Red
                        TargetColor0 = New MyColor(215, 255, 53, 11)
                        TargetColor1 = New MyColor(215, 255, 43, 0)
                End Select
                If DoubleStack IsNot Nothing Then
                    '有重复提示，且该提示的进入动画已播放
                    If Not AniIsRun("Hint Show " & DoubleInfo.Uuid) Then
                        AniStop("Hint Hide " & DoubleInfo.Uuid)
                        Dim Delay As Double = (800 + CurrentHint.Text.Length.Clamp(5, 23) * 180) * AniSpeed
                        AniStart({
                            AaX(DoubleStack, -12 - DoubleStack.Margin.Left, 50, , New AniEaseOutFluent),
                            AaX(DoubleStack, -8, 50, 50, New AniEaseInFluent),
                            AaX(DoubleStack, 8, 50, 100, New AniEaseOutFluent),
                            AaX(DoubleStack, -8, 50, 150, New AniEaseInFluent),
                            AaDouble(
                                Sub(i As Object)
                                    Percent += CDbl(i)
                                    Dim Gradient = DirectCast(DoubleStack.Background, LinearGradientBrush)
                                    Gradient.GradientStops(0).Color = TargetColor0 * Percent + New MyColor(255, 255, 255) * (1 - Percent)
                                    Gradient.GradientStops(1).Color = TargetColor1 * Percent + New MyColor(255, 255, 255) * (1 - Percent)
                                End Sub, 0.7, 250),
                            AaX(DoubleStack, -50, 200, Delay, New AniEaseInFluent),
                            AaOpacity(DoubleStack, -1, 150, Delay),
                            AaCode(Sub() DoubleInfo.Reusable = False, Delay),
                            AaHeight(DoubleStack, -26, 100, , New AniEaseOutFluent, True),
                            AaCode(Sub() FrmMain.PanHint.Children.Remove(DoubleStack), , True)
                        }, "Hint Hide " & DoubleInfo.Uuid)
                    End If
                Else
                    '准备控件
                    Dim Info As New HintControlInfo With {.Uuid = GetUuid()}
                    Dim NewHintControl As New Border With {
                        .Tag = Info,
                        .Margin = New Thickness(-70, 0, 20, 0),
                        .Opacity = 0,
                        .Height = 0,
                        .HorizontalAlignment = HorizontalAlignment.Left,
                        .CornerRadius = New CornerRadius(0, 6, 6, 0)
                    }
                    Dim Gradient As New LinearGradientBrush With {
                        .StartPoint = New RelativePoint(0, 0.5, RelativeUnit.Relative),
                        .EndPoint = New RelativePoint(1, 0.5, RelativeUnit.Relative)
                    }
                    Gradient.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = TargetColor0 * Percent + New MyColor(255, 255, 255) * (1 - Percent)})
                    Gradient.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = TargetColor1 * Percent + New MyColor(255, 255, 255) * (1 - Percent)})
                    NewHintControl.Background = Gradient
                    NewHintControl.Child = New TextBlock With {
                        .TextTrimming = TextTrimming.CharacterEllipsis,
                        .FontSize = 13,
                        .Text = CurrentHint.Text,
                        .Foreground = Brushes.White,
                        .Margin = New Thickness(33, 5, 8, 5)
                    }
                    FrmMain.PanHint.Children.Add(NewHintControl)
                    '控件动画
                    Dim Animations As New List(Of AniData)
                    If FrmMain.PanHint.Children.Count > 1 Then
                        Animations.Add(AaHeight(NewHintControl, 26, 150, , New AniEaseOutFluent))
                    Else
                        NewHintControl.Height = 26
                    End If
                    Animations.AddRange({
                        AaX(NewHintControl, 30, 400, , New AniEaseOutElastic(AniEasePower.Weak)),
                        AaX(NewHintControl, 20, 200, , New AniEaseOutFluent),
                        AaOpacity(NewHintControl, 1, 100),
                        AaDouble(
                            Sub(i As Object)
                                Percent += CDbl(i)
                                Gradient.GradientStops(0).Color = TargetColor0 * Percent + New MyColor(255, 255, 255) * (1 - Percent)
                                Gradient.GradientStops(1).Color = TargetColor1 * Percent + New MyColor(255, 255, 255) * (1 - Percent)
                            End Sub, 0.7, 250, 100)
                    })
                    AniStart(Animations, "Hint Show " & Info.Uuid)
                    '结束动画
                    Dim Delay As Double = (800 + CurrentHint.Text.Length.Clamp(5, 23) * 180) * AniSpeed
                    AniStart({
                        AaX(NewHintControl, -50, 200, Delay, New AniEaseInFluent),
                        AaOpacity(NewHintControl, -1, 150, Delay),
                        AaCode(Sub() Info.Reusable = False, Delay),
                        AaHeight(NewHintControl, -26, 100, , New AniEaseOutFluent, True),
                        AaCode(Sub() FrmMain.PanHint.Children.Remove(NewHintControl), , True)
                    }, "Hint Hide " & Info.Uuid)
                End If
                If CurrentHint.Log Then Logger.Info($"弹出提示：{CurrentHint.Text}")
            Loop
        Catch ex As Exception
            Logger.Warn(ex, "显示弹出提示失败")
        End Try
    End Sub

#End Region

#Region "弹窗"

    ''' <summary>
    ''' 存储弹窗信息的转换器。
    ''' </summary>
    Public Class MyMsgBoxConverter
        Public Type As MyMsgBoxType
        Public Title As String
        Public Text As String
        ''' <summary>
        ''' 输入模式：文本框的文本。
        ''' 选择模式：需要放进去的 IEnumerable(Of IMyRadio)。
        ''' </summary>
        Public Content As Object
        ''' <summary>
        ''' 输入模式：提示文本。
        ''' </summary>
        Public HintText As String = ""
        ''' <summary>
        ''' 输入模式：对输入内容的验证规则。
        ''' </summary>
        Public ValidateRules As New Collection(Of Validate)
        ''' <summary>
        ''' 有多个按钮时，是否给第一个按钮加高亮。
        ''' </summary>
        Public HighLight As Boolean
        Public Button1 As String = "确定"
        Public Button2 As String = ""
        Public Button3 As String = ""
        ''' <summary>
        ''' 点击第一个按钮将执行该方法，不关闭弹窗。
        ''' </summary>
        Public Button1Action As Action = Nothing
        ''' <summary>
        ''' 点击第二个按钮将执行该方法，不关闭弹窗。
        ''' </summary>
        Public Button2Action As Action = Nothing
        ''' <summary>
        ''' 点击第三个按钮将执行该方法，不关闭弹窗。
        ''' </summary>
        Public Button3Action As Action = Nothing
        Public IsWarn As Boolean = False
        Public ForceWait As Boolean = False
        ''' <summary>
        ''' 弹窗是否已经关闭。
        ''' </summary>
        Public IsExited As Boolean = False
        ''' <summary>
        ''' 输入模式：输入的文本。若点击了非第一个按钮，则为 Nothing。
        ''' 选择模式：点击的按钮编号，从 1 开始。
        ''' </summary>
        Public Result As Object
    End Class

    Public Enum MyMsgBoxType
        Text
        [Select]
        Input
        Login
    End Enum

    ''' <summary>
    ''' 显示弹窗，返回点击按钮的编号（从 1 开始）。
    ''' </summary>
    Public Function MyMsgBox(Caption As String, Optional Title As String = "提示",
                             Optional Button1 As String = "确定", Optional Button2 As String = "", Optional Button3 As String = "",
                             Optional IsWarn As Boolean = False, Optional HighLight As Boolean = True, Optional ForceWait As Boolean = False,
                             Optional Button1Action As Action = Nothing, Optional Button2Action As Action = Nothing, Optional Button3Action As Action = Nothing) As Integer
        Dim Converter As New MyMsgBoxConverter With {
            .Type = MyMsgBoxType.Text, .Button1 = Button1, .Button2 = Button2, .Button3 = Button3,
            .Text = Caption, .IsWarn = IsWarn, .Title = Title, .HighLight = HighLight,
            .Button1Action = Button1Action, .Button2Action = Button2Action, .Button3Action = Button3Action}
        LocalizeMessage(Converter)
        If Button2.Length > 0 OrElse ForceWait Then
            If RunInUi() Then
                Return MyMsgBoxCore(Converter)
            Else
                Return Dispatcher.UIThread.Invoke(Function() MyMsgBoxCore(Converter))
            End If
        Else
            WaitingMyMsgBox.Enqueue(Converter)
            MyMsgBoxTick()
            Return 1
        End If
    End Function

    Private Function MyMsgBoxCore(Converter As MyMsgBoxConverter) As Integer
        Dim Result As Object = MyMsgBoxWaitCore(Converter)
        Logger.Info($"普通弹框返回：{If(Result, "null")}")
        Return If(Result Is Nothing, 0, CInt(Result))
    End Function

    ''' <summary>
    ''' 显示输入框并返回输入的文本。若点击第二个按钮，则返回 Nothing。
    ''' </summary>
    Public Function MyMsgBoxInput(Title As String, Optional Text As String = Nothing, Optional DefaultInput As String = Nothing, Optional ValidateRules As Collection(Of Validate) = Nothing, Optional HintText As String = "", Optional Button1 As String = "确定", Optional Button2 As String = "取消", Optional IsWarn As Boolean = False) As String
        Dim Converter As New MyMsgBoxConverter With {
            .Type = MyMsgBoxType.Input, .Text = If(Text, ""), .HintText = If(HintText, ""),
            .ValidateRules = If(ValidateRules, New Collection(Of Validate)),
            .Button1 = Button1, .Button2 = Button2, .Content = DefaultInput, .IsWarn = IsWarn, .Title = Title}
        LocalizeMessage(Converter)
        Dim Result As Object = MyMsgBoxWait(Converter)
        Logger.Info($"输入弹框返回：{If(Result, "null")}")
        Return TryCast(Result, String)
    End Function

    ''' <summary>
    ''' 显示选择框并返回选择的第几项（从 0 开始）。若点击第二个按钮，则返回 Nothing。
    ''' </summary>
    Public Function MyMsgBoxSelect(Selections As IEnumerable(Of IMyRadio), Optional Title As String = "提示", Optional Button1 As String = "确定", Optional Button2 As String = "", Optional IsWarn As Boolean = False) As Integer?
        Dim Converter As New MyMsgBoxConverter With {
            .Type = MyMsgBoxType.Select, .Button1 = Button1, .Button2 = Button2,
            .Content = Selections, .IsWarn = IsWarn, .Title = Title}
        LocalizeMessage(Converter)
        Dim Result As Object = MyMsgBoxWait(Converter)
        Logger.Info($"选择弹框返回：{If(Result, "null")}")
        If Result Is Nothing Then Return Nothing
        Return CInt(Result)
    End Function

    Private Sub LocalizeMessage(Converter As MyMsgBoxConverter)
        Converter.Title = Localization.TranslateText(Converter.Title)
        Converter.Text = Localization.TranslateText(Converter.Text)
        Converter.HintText = Localization.TranslateText(Converter.HintText)
        Converter.Button1 = Localization.TranslateText(Converter.Button1)
        Converter.Button2 = Localization.TranslateText(Converter.Button2)
        Converter.Button3 = Localization.TranslateText(Converter.Button3)
    End Sub

    ''' <summary>
    ''' 将弹窗列入队列，并在 UI 线程上泵消息直到弹窗关闭，返回弹窗结果。
    ''' </summary>
    Private Function MyMsgBoxWait(Converter As MyMsgBoxConverter) As Object
        If RunInUi() Then Return MyMsgBoxWaitCore(Converter)
        Return Dispatcher.UIThread.Invoke(Function() MyMsgBoxWaitCore(Converter))
    End Function
    Private Function MyMsgBoxWaitCore(Converter As MyMsgBoxConverter) As Object
        WaitingMyMsgBox.Enqueue(Converter)
        Try
            MyMsgBoxTick()
            If Not Converter.IsExited Then
                If FrmMain IsNot Nothing AndAlso FrmMain.PanMsg IsNot Nothing Then
                    ModalityHost.PumpUntil(Function() Converter.IsExited, Integer.MaxValue)
                Else
                    Logger.Warn("主窗体尚未加载，弹窗无法显示")
                    Converter.IsExited = True
                End If
            End If
        Catch ex As Exception
            Logger.Error(ex, "等待弹窗结果失败")
            Converter.IsExited = True
            '弹窗可能已经加入界面但没能正常关闭（例如布局时出错），必须手动清理，否则会一直阻塞后续弹窗
            Dim Msg As MyMsgBoxBase = TryCast(ActiveMsgBoxControl, MyMsgBoxBase)
            If Msg IsNot Nothing AndAlso Msg.Converter Is Converter Then
                ActiveMsgBoxControl = Nothing
                If FrmMain IsNot Nothing AndAlso FrmMain.PanMsg IsNot Nothing Then FrmMain.PanMsg.Children.Remove(Msg)
                MyMsgBoxTick()
            End If
        End Try
        Return Converter.Result
    End Function

    ''' <summary>
    ''' 等待显示的弹窗。
    ''' </summary>
    Public WaitingMyMsgBox As New ConcurrentQueue(Of MyMsgBoxConverter)

    ''' <summary>
    ''' 当前正在显示的窗口内弹窗。同一时间只会显示一个弹窗。
    ''' </summary>
    Public ActiveMsgBoxControl As Control = Nothing

    Public Sub MyMsgBoxTick()
        Dim TargetMsgBox As MyMsgBoxConverter = Nothing
        Dim Msg As Control = Nothing
        Try
            If Not RunInUi() Then
                RunInUi(AddressOf MyMsgBoxTick)
                Return
            End If
            If FrmMain Is Nothing OrElse FrmMain.PanMsg Is Nothing Then Return
            If FrmMain.PanMsg.Children.Count > 0 Then
                FrmMain.PanMsg.IsVisible = True
                Return
            End If
            If Not WaitingMyMsgBox.TryDequeue(TargetMsgBox) Then
                FrmMain.PanMsg.IsVisible = False
                Return
            End If
            Select Case TargetMsgBox.Type
                Case MyMsgBoxType.Text
                    Msg = New MyMsgText(TargetMsgBox)
                Case MyMsgBoxType.Input
                    Msg = New MyMsgInput(TargetMsgBox)
                Case MyMsgBoxType.Select
                    Msg = New MyMsgSelect(TargetMsgBox)
                Case MyMsgBoxType.Login
                    Msg = New MyMsgLogin(TargetMsgBox)
                Case Else
                    Logger.Warn($"暂不支持的弹窗类型：{TargetMsgBox.Type}")
                    TargetMsgBox.IsExited = True
                    Return
            End Select
            ActiveMsgBoxControl = Msg
            FrmMain.PanMsg.IsVisible = True
            FrmMain.PanMsg.Children.Add(Msg)
            If TypeOf Msg Is MyMsgLogin Then DirectCast(Msg, MyMsgLogin).StartLogin()
        Catch ex As Exception
            Logger.Error(ex, "处理等待中的弹窗失败")
            If Msg IsNot Nothing Then
                If ActiveMsgBoxControl Is Msg Then ActiveMsgBoxControl = Nothing
                If FrmMain IsNot Nothing AndAlso FrmMain.PanMsg IsNot Nothing Then FrmMain.PanMsg.Children.Remove(Msg)
            End If
            If TargetMsgBox IsNot Nothing Then
                TargetMsgBox.IsExited = True
                MyMsgBoxTick()
            End If
        End Try
    End Sub

#End Region

#Region "系统"

    ''' <summary>
    ''' 把某个 PCL 窗口拖到最前面。
    ''' </summary>
    Public Sub ShowWindowToTop(Handle As IntPtr)
        If Not OperatingSystem.IsWindows() Then Return
        Try
            PostMessage(Handle, 400 * 16 + 2, 0, 0)
            SetForegroundWindow(Handle) '不在这里放不行，神秘 WinAPI，建议别动
        Catch ex As Exception
            Logger.Error(ex, "设置窗口置顶失败", LogBehavior.Toast)
        End Try
    End Sub
    <DllImport("user32.dll", CharSet:=CharSet.Ansi, EntryPoint:="FindWindowA")>
    Public Function FindWindow(ClassName As String, WindowName As String) As IntPtr
    End Function
    <DllImport("user32.dll")>
    Public Function SetForegroundWindow(hWnd As IntPtr) As Integer
    End Function
    <DllImport("user32.dll", CharSet:=CharSet.Ansi, EntryPoint:="PostMessageA")>
    Private Function PostMessage(hWnd As IntPtr, msg As UInteger, wParam As Long, lParam As Long) As Boolean
    End Function

    ''' <summary>
    ''' 将特定程序设置为使用高性能显卡启动。
    ''' 如果失败，则抛出异常。
    ''' </summary>
    Public Sub SetGPUPreference(Executeable As String)
        If Not OperatingSystem.IsWindows() Then Return
        Const REG_KEY As String = "HKEY_CURRENT_USER\Software\Microsoft\DirectX\UserGpuPreferences"
        Const REG_VALUE As String = "GpuPreference=2;"
        '查看现有设置
        If RegistryUtils.Read(REG_KEY, Executeable).result?.ToString() = REG_VALUE Then
            Logger.Info($"无需调整显卡设置：{Executeable}")
            Return
        End If
        '写入新设置
        RegistryUtils.Write(REG_KEY, Executeable, REG_VALUE)
        Logger.Info($"已调整显卡设置：{Executeable}")
    End Sub

#End Region

#Region "反馈与遥测"

    '反馈
    Public Sub Feedback(Optional ShowMsgbox As Boolean = True, Optional ForceOpenLog As Boolean = False)
        Try
            FeedbackInfo()
            If ForceOpenLog OrElse (ShowMsgbox AndAlso MyMsgBox("若你在汇报一个 Bug，请点击 打开文件夹 按钮，并上传 Log(1~5).txt 中包含错误信息的文件。" & vbCrLf & "游戏崩溃一般与启动器无关，请不要因为游戏崩溃而提交反馈。", "反馈提交提醒", "打开文件夹", "不需要") = 1) Then
                OpenExplorer(PathData & "Log1.txt")
            End If
            OpenWebsite("https://github.com/Meloong-Git/PCL/issues/")
        Catch ex As Exception
            Logger.Warn(ex, "提交反馈失败")
        End Try
    End Sub
    ''' <summary>
    ''' 在日志中输出系统诊断信息。
    ''' </summary>
    Public Sub FeedbackInfo()
        Try
            Logger.Warn($"诊断信息：{vbCrLf}" &
                "操作系统：" & RuntimeInformation.OSDescription & vbCrLf &
                "剩余内存：" & Int(SystemInfo.AvailablePhysicalMemory / 1024 / 1024) & " M / " & Int(SystemInfo.TotalPhysicalMemory / 1024 / 1024) & " M" & vbCrLf &
                "DPI：" & DPI & "（" & Math.Round(DPI / 96, 2) * 100 & "%）" & vbCrLf &
                "MC 文件夹：" & If(McFolderSelected, "Nothing") & vbCrLf &
                "文件位置：" & Paths.Base)
        Catch ex As Exception
            Logger.Warn(ex, "输出诊断信息失败")
        End Try
    End Sub

    '遥测
    Public Sub Telemetry([Event] As String, ParamArray Datas As String())
        If BuildType = BuildTypes.Debug Then Return '开发版不上传遥测
        If Not Settings.Get(Of Boolean)("SystemSystemTelemetry") Then Return '用户关闭了遥测
        If Not ClsBaseUrl.StartsWithF("http") Then Return '开源版没有设置遥测地址
        RunInNewThread(
        Sub()
            Try
                Logger.Info($"匿名数据上报：{[Event]}")
                Dim Url As String = $"{ClsBaseUrl}&Event={StringUtils.UrlEscape([Event])}"
                For i = 0 To Datas.Length - 1 Step 2
                    Url &= "&" & StringUtils.UrlEscape(Datas(i)) & "=" & StringUtils.UrlEscape(Datas(i + 1).ReplaceLineEndings(vbLf, mergeMultiple:=True))
                Next
                NetRequestByClient(Url, MakeLog:=False)
            Catch ex As Exception
                Logger.Warn(ex, "匿名数据上报失败")
            End Try
        End Sub, "Telemetry", ThreadPriority.Lowest)
    End Sub

#End Region

End Module
