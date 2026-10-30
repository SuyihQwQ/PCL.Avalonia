Imports Avalonia.Controls.Shapes
Imports Path = Avalonia.Controls.Shapes.Path

''' <summary>
''' 任务管理页面：左侧显示总进度、速度与剩余文件，右侧显示各下载任务的卡片。
''' </summary>
Public Class PageSpeedLeft
    Inherits MyPageLeft

    Private Const WatcherInterval As Integer = 300

    Public Sub New()
        InitControls()
    End Sub

    '初始化
    Private IsLoad As Boolean = False
    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        '进入时就刷新一次显示
        Watcher()

        '如果在页面切换动画的 “上一页消失” 部分已经完成了下载，就直接尝试返回
        TryReturnToHome()

        If IsLoad Then Return
        IsLoad = True

        '监控定时器
        Dim Timer As New DispatcherTimer With {.Interval = TimeSpan.FromMilliseconds(WatcherInterval)}
        AddHandler Timer.Tick, AddressOf Watcher
        Timer.Start()

        '非调试模式隐藏线程数
        If Not ModeDebug Then
            RowDefinitions(12).Height = New GridLength(0)
            RowDefinitions(13).Height = New GridLength(0)
            RowDefinitions(14).Height = New GridLength(0)
            RowDefinitions(15).Height = New GridLength(0)
        End If

    End Sub

    '定时器任务
    Private Class TaskCard
        Public Card As MyCard
        Public TaskGrid As Grid
    End Class
    Private ReadOnly RightCards As New Dictionary(Of String, TaskCard)
    Private Sub Watcher()
        If Not FrmMain.PageCurrent = FormMain.PageType.TaskManager Then Return
        Try

#Region "更新左边栏"
            If Not LoaderTaskbar.Any() Then
                '无任务
                LabProgress.Text = "100 %"
                LabSpeed.Text = "0 B/s"
                LabFile.Text = "0"
                LabThread.Text = "0 / " & NetTaskThreadLimit
            Else
                '有任务，输出基本信息
                Dim Tasks = LoaderTaskbar.Where(Function(l) l.Show).ToList() '筛选掉启动 MC 的任务（#6270）
                Dim RawPercent As Double = If(Tasks.Any, Tasks.Select(Function(l) l.Progress).Average().Clamp(0, 1), 1)
                Dim PredictText As String = Math.Floor(RawPercent * 100) & "." & Math.Floor((RawPercent * 100 - Math.Floor(RawPercent * 100)) * 100).ToString.EnsureLength("0", 2) & " %"
                LabProgress.Text = If(RawPercent > 0.999999, "100 %", PredictText)
                LabSpeed.Text = StringUtils.FormatByteSize(NetManager.Speed) & "/s"
                LabFile.Text = If(NetManager.FileRemain < 0, "0*", NetManager.FileRemain)
                LabThread.Text = NetTaskThreadCount & " / " & NetTaskThreadLimit
            End If
#End Region

        Catch ex As Exception
            Logger.Error(ex, "任务管理左栏监视出错")
        End Try
        If FrmSpeedRight Is Nothing OrElse FrmSpeedRight.PanMain Is Nothing Then Return
        Try
            For Each Loader In LoaderTaskbar.ToList
                TaskRefresh(Loader)
            Next
        Catch ex As Exception
            Logger.Error(ex, "任务管理右栏监视出错")
        End Try
    End Sub
    Public Sub TaskRefresh(Loader As LoaderBase)
        If Loader Is Nothing OrElse Not Loader.Show Then Return
        Try
            '获取实际加载器列表
            Dim LoaderList As List(Of LoaderBase) = CType(Loader, Object).GetLoaderList()
            If RightCards.ContainsKey(Loader.Name) Then
                '已有此卡片
                Dim TaskCard As TaskCard = RightCards(Loader.Name)
                Dim Card As MyCard = TaskCard.Card
                Dim TaskGrid As Grid = TaskCard.TaskGrid
                Dim NewValue As Double = Loader.Progress + Loader.State
                If Val(Card.Tag) = NewValue Then Return
                Card.Tag = NewValue
                Try
                    Select Case Loader.State
                        Case LoadState.Failed
#Region "失败，更新卡片"
                            TaskGrid.RowDefinitions.Clear()
                            TaskGrid.Children.Clear()
                            Dim FailedIcon As Control = CreateFailedIcon()
                            Grid.SetColumn(FailedIcon, 0)
                            Grid.SetRow(FailedIcon, 0)
                            TaskGrid.Children.Add(FailedIcon)
                            Dim Tb As New TextBlock With {
                                .TextWrapping = TextWrapping.Wrap,
                                .HorizontalAlignment = HorizontalAlignment.Left,
                                .Margin = New Thickness(0, 0, 0, 5)}
                            ToolTip.SetTip(Tb, "单击复制错误详情")
                            Tb.Text = Loader.Error.GetDisplay(True)
                            AddHandler Tb.PointerPressed,
                            Sub(sender As Object, e As PointerPressedEventArgs)
                                ClipboardSet(DirectCast(sender, TextBlock).Text, False)
                                Hint("已复制错误详情！", HintType.Green)
                            End Sub
                            Grid.SetColumn(Tb, 1)
                            Grid.SetRow(Tb, 0)
                            TaskGrid.Children.Add(Tb)
#End Region
                        Case LoadState.Finished, LoadState.Canceled
#Region "完成或中断，销毁卡片并返回"
                            AniDispose(Card, True, Sub(State As Object) TryReturnToHome())
#End Region
                        Case LoadState.Loading, LoadState.Waiting
#Region "进度不同，更新卡片"
                            Try
                                If TaskGrid.Children.Count < LoaderList.Count * 2 Then
                                    Logger.Warn($"刷新任务管理卡片 {Loader.Name} 失败：卡片中仅有 {TaskGrid.Children.Count} 个子项，要求至少有 {LoaderList.Count * 2} 个子项")
                                    Exit Try
                                End If
                                Dim Row As Integer = 0
                                For Each SubTask In LoaderList
                                    Select Case SubTask.State
                                        Case LoadState.Waiting
                                            If CType(TaskGrid.Children(Row * 2), Control).Tag <> "Waiting" Then
                                                TaskGrid.Children.RemoveAt(Row * 2)
                                                Dim Icon As Control = CreateWaitingIcon()
                                                Grid.SetColumn(Icon, 0)
                                                Grid.SetRow(Icon, Row)
                                                TaskGrid.Children.Insert(Row * 2, Icon)
                                            End If
                                        Case LoadState.Loading
                                            If CType(TaskGrid.Children(Row * 2), Control).Tag <> "Loading" Then
                                                TaskGrid.Children.RemoveAt(Row * 2)
                                                Dim Icon As Control = CreateLoadingIcon(SubTask.Progress)
                                                Grid.SetColumn(Icon, 0)
                                                Grid.SetRow(Icon, Row)
                                                TaskGrid.Children.Insert(Row * 2, Icon)
                                            Else
                                                CType(TaskGrid.Children(Row * 2), TextBlock).Text = Math.Floor(SubTask.Progress * 100) & "%"
                                            End If
                                        Case LoadState.Finished
                                            If CType(TaskGrid.Children(Row * 2), Control).Tag <> "Finished" Then
                                                TaskGrid.Children.RemoveAt(Row * 2)
                                                Dim Icon As Control = CreateFinishedIcon()
                                                Grid.SetColumn(Icon, 0)
                                                Grid.SetRow(Icon, Row)
                                                TaskGrid.Children.Insert(Row * 2, Icon)
                                            End If
                                    End Select
                                    Row += 1
                                Next
                            Catch ex As Exception
                                Logger.Error(ex, $"刷新任务管理卡片 {Loader.Name} 失败")
                            End Try
#End Region
                    End Select
                Catch ex As Exception
                    Logger.Error(ex, $"更新任务管理显示失败（{Loader.State}）")
                End Try
            ElseIf Not (Loader.State = LoadState.Canceled OrElse Loader.State = LoadState.Finished) Then
                Try
#Region "没有卡片且未中断或完成，添加新的卡片"
                    Dim Card As New MyCard With {
                        .Title = Loader.Name,
                        .Margin = New Thickness(0, 0, 0, 15),
                        .Tag = Loader.Progress + Loader.State}
                    Dim TaskGrid As New Grid With {.Margin = New Thickness(14, 40, 15, 10)}
                    TaskGrid.ColumnDefinitions.Add(New ColumnDefinition With {.Width = New GridLength(50)})
                    TaskGrid.ColumnDefinitions.Add(New ColumnDefinition With {.Width = New GridLength(1, GridUnitType.Star)})
                    For Each SubTask In LoaderList
                        TaskGrid.RowDefinitions.Add(New RowDefinition With {.Height = New GridLength(26)})
                    Next
                    Dim Row As Integer = 0
                    For Each SubTask In LoaderList
                        Dim Icon As Control
                        Select Case SubTask.State
                            Case LoadState.Waiting
                                Icon = CreateWaitingIcon()
                            Case LoadState.Loading
                                Icon = CreateLoadingIcon(SubTask.Progress)
                            Case LoadState.Finished
                                Icon = CreateFinishedIcon()
                            Case Else
                                Icon = CreateFailedIcon()
                        End Select
                        Grid.SetColumn(Icon, 0)
                        Grid.SetRow(Icon, Row)
                        TaskGrid.Children.Add(Icon)
                        Dim LabSubTask As New TextBlock With {.Text = SubTask.Name, .HorizontalAlignment = HorizontalAlignment.Left}
                        Grid.SetColumn(LabSubTask, 1)
                        Grid.SetRow(LabSubTask, Row)
                        TaskGrid.Children.Add(LabSubTask)
                        Row += 1
                    Next
                    Card.Children.Add(TaskGrid)
                    FrmSpeedRight.PanMain.Children.Insert(0, Card)
                    RightCards.Add(Loader.Name, New TaskCard With {.Card = Card, .TaskGrid = TaskGrid})
                    Logger.Info($"新建任务管理卡片：{Loader.Name}")
                    '添加取消按钮
                    Dim Cancel As New MyIconButton With {
                        .Name = "BtnCancel",
                        .Logo = "F1 M2,0 L0,2 8,10 0,18 2,20 10,12 18,20 20,18 12,10 20,2 18,0 10,8 2,0Z",
                        .Height = 20,
                        .Margin = New Thickness(0, 10, 10, 0),
                        .LogoScale = 1.1,
                        .HorizontalAlignment = HorizontalAlignment.Right,
                        .VerticalAlignment = VerticalAlignment.Top}
                    Card.Children.Add(Cancel)
                    AddHandler Cancel.Click,
                    Sub(sender As MyIconButton, e As EventArgs)
                        AniDispose(sender, False)
                        AniDispose(Card, True, Sub(State As Object) If FrmSpeedRight.PanMain.Children.Count = 0 AndAlso FrmMain.PageCurrent = FormMain.PageType.TaskManager Then FrmMain.PageBack())
                        RightCards.Remove(Loader.Name)
                        LoaderTaskbar.Remove(Loader)
                        Logger.Info($"关闭任务管理卡片：{Loader.Name}，且移出任务列表")
                        RunInThread(Sub() Loader.Cancel())
                    End Sub
                    '如果已经失败，再刷新一次，修改成失败的控件
                    If Loader.State = LoadState.Failed Then
                        Card.Tag = Nothing '避免重复导致刷新无效
                        TaskRefresh(Loader)
                    End If
#End Region
                Catch ex As Exception
                    Logger.Error(ex, "添加任务管理卡片失败")
                End Try
            End If
        Catch ex As Exception
            Logger.Error(ex, "刷新任务管理显示失败")
        End Try
    End Sub
    Public Sub TaskRemove(Loader As Object)
        If Loader Is Nothing Then Return
        If RightCards.ContainsKey(Loader.Name) Then
            RunInUiWait(
            Sub()
                '移除已有的卡片
                Dim TaskCard As TaskCard = RightCards(Loader.Name)
                FrmSpeedRight.PanMain.Children.Remove(TaskCard.Card)
                RightCards.Remove(Loader.Name)
                Logger.Info($"移除任务管理卡片：{Loader.Name}")
            End Sub)
        End If
    End Sub

    '卡片状态图标
    Private Shared Function CreateWaitingIcon() As Control
        Dim Icon As New Path With {
            .Stretch = Stretch.Uniform,
            .Tag = "Waiting",
            .Data = Geometry.Parse("F1 M5,0 a5,5 360 1 0 0,0.0001 m15,0 a5,5 360 1 0 0,0.0001 m15,0 a5,5 360 1 0 0,0.0001 Z"),
            .Width = 18,
            .Height = 6,
            .HorizontalAlignment = HorizontalAlignment.Center,
            .Margin = New Thickness(0, 7, 0, 0),
            .VerticalAlignment = VerticalAlignment.Top}
        Icon.SetResourceReference(Shape.FillProperty, "ColorBrush3")
        Return Icon
    End Function
    Private Shared Function CreateLoadingIcon(Progress As Double) As Control
        Dim Icon As New TextBlock With {
            .Text = Math.Floor(Progress * 100) & "%",
            .Tag = "Loading",
            .HorizontalAlignment = HorizontalAlignment.Center}
        Icon.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrush3")
        Return Icon
    End Function
    Private Shared Function CreateFinishedIcon() As Control
        Dim Icon As New Path With {
            .Stretch = Stretch.Uniform,
            .Tag = "Finished",
            .Data = Geometry.Parse("F1 M 23.7501,33.25L 34.8334,44.3333L 52.2499,22.1668L 56.9999,26.9168L 34.8334,53.8333L 19.0001,38L 23.7501,33.25 Z"),
            .Width = 15,
            .Height = 16,
            .HorizontalAlignment = HorizontalAlignment.Center,
            .Margin = New Thickness(0, 3, 0, 0),
            .VerticalAlignment = VerticalAlignment.Top}
        Icon.SetResourceReference(Shape.FillProperty, "ColorBrush3")
        Return Icon
    End Function
    Private Shared Function CreateFailedIcon() As Control
        Dim Icon As New Path With {
            .Stretch = Stretch.Uniform,
            .Tag = "Failed",
            .Data = Geometry.Parse("F1 M2.5,0 L0,2.5 7.5,10 0,17.5 2.5,20 10,12.5 17.5,20 20,17.5 12.5,10 20,2.5 17.5,0 10,7.5 2.5,0Z"),
            .Width = 15,
            .Height = 15,
            .HorizontalAlignment = HorizontalAlignment.Center,
            .Margin = New Thickness(0, 1, 0, 0),
            .VerticalAlignment = VerticalAlignment.Top}
        Icon.SetResourceReference(Shape.FillProperty, "ColorBrush3")
        Return Icon
    End Function

    ''' <summary>
    ''' 若没有任务，尝试返回主页。
    ''' </summary>
    Private Sub TryReturnToHome()
        If FrmSpeedRight IsNot Nothing AndAlso FrmSpeedRight.PanMain.Children.Count = 0 AndAlso FrmMain.PageCurrent = FormMain.PageType.TaskManager Then
            FrmMain.PageBack()
        End If
    End Sub

End Class
