''' <summary>
''' 帮助页面：按分类列出内置与自定义帮助条目，并提供搜索。
''' </summary>
Public Class PageOtherHelp
    Inherits MyPageRight

    Private Shadows IsLoaded As Boolean = False

    Public Sub New()
        InitControls()
        PageLoaderInit(Load, PanLoad, PanBack, Nothing, HelpLoader, AddressOf HelpListLoad)
    End Sub

    Private Sub PageOtherHelp_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        PanBack.ScrollToHome()
        If IsLoaded Then Return
        IsLoaded = True
    End Sub

    ''' <summary>
    ''' 将帮助列表对象实例化为主页 UI。
    ''' </summary>
    Private Sub HelpListLoad(Loader As LoaderTask(Of Integer, List(Of HelpEntry)))
        Try
            PanList.Children.Clear()
            PanBack.ScrollToHome()
            Dim HelpItems = Loader.Output
            '获取全部分类
            Dim Types As New List(Of String)
            For Each Item As HelpEntry In HelpItems
                If Not IsVisibleEntry(Item) Then Continue For
                For Each TypeName As String In Item.Types
                    If Not Types.Contains(TypeName) Then Types.Add(TypeName)
                Next
            Next
            '将指南页面置顶
            If Types.Contains("指南") Then
                Types.Remove("指南")
                Types.Insert(0, "指南")
            End If
            '转化为 UI
            For Each TypeName As String In Types
                Dim TypeItems As New List(Of HelpEntry)
                For Each Item As HelpEntry In HelpItems
                    If Not IsVisibleEntry(Item) Then Continue For
                    If Item.Types.Contains(TypeName) Then TypeItems.Add(Item)
                Next
                Dim NewCard As New MyCard With {.Title = TypeName, .Margin = New Thickness(0, 0, 0, 15)}
                Dim NewStack As New StackPanel With {
                    .Margin = New Thickness(20, MyCard.SwapedHeight, 18, 0),
                    .VerticalAlignment = VerticalAlignment.Top,
                    .RenderTransform = New TranslateTransform(0, 0)}
                NewCard.Children.Add(NewStack)
                NewCard.SwapControl = NewStack
                NewCard.SetContentBuilder(TypeItems, Function(Entry) BuildListItem(Entry))
                If TypeName = "指南" Then
                    NewCard.BuildContent()
                Else
                    NewCard.IsSwapped = True
                End If
                PanList.Children.Add(NewCard)
            Next
        Catch ex As Exception
            Logger.Error(ex, "加载帮助列表 UI 失败")
        End Try
    End Sub

    Private Shared Function IsVisibleEntry(Entry As HelpEntry) As Boolean
        If BuildType = BuildTypes.Release Then Return Entry.ShowInPublic
        Return Entry.ShowInSnapshot
    End Function

    Private Shared Function BuildListItem(Entry As HelpEntry) As MyListItem
        Return Entry.ToListItem()
    End Function

    ''' <summary>
    ''' 帮助项目的点击事件。
    ''' </summary>
    Public Shared Sub OnItemClick(Entry As HelpEntry)
        Try
            If Entry Is Nothing Then Return
            If Entry.IsEvent Then
                CustomEvent.Raise(Entry.EventType, Entry.EventData)
            Else
                EnterHelpPage(Entry)
            End If
        Catch ex As Exception
            Logger.Error(ex, "处理帮助项目点击时发生意外错误")
        End Try
    End Sub

    Public Shared Sub EnterHelpPage(Location As String)
        RunInThread(
        Sub()
            If HelpLoader.State <> LoadState.Finished Then HelpLoader.WaitForExit(GetUuid())
            Dim Entry As New HelpEntry(Location)
            RunInUi(
            Sub()
                Dim FrmHelpDetail As New PageOtherHelpDetail
                If FrmHelpDetail.Init(Entry) Then
                    FrmMain.PageChangeHelpDetail(Entry, FrmHelpDetail)
                Else
                    Logger.Warn("已取消进入帮助项目，这一般是由于 xaml 初始化失败，且用户在弹窗中手动放弃")
                End If
            End Sub)
        End Sub)
    End Sub

    Public Shared Sub EnterHelpPage(Entry As HelpEntry)
        RunInThread(
        Sub()
            If HelpLoader.State <> LoadState.Finished Then HelpLoader.WaitForExit(GetUuid())
            RunInUi(
            Sub()
                Dim FrmHelpDetail As New PageOtherHelpDetail
                If FrmHelpDetail.Init(Entry) Then
                    FrmMain.PageChangeHelpDetail(Entry, FrmHelpDetail)
                Else
                    Logger.Warn("已取消进入帮助项目，这一般是由于 xaml 初始化失败，且用户在弹窗中手动放弃")
                End If
            End Sub)
        End Sub)
    End Sub

    ''' <summary>
    ''' 搜索帮助。
    ''' </summary>
    Private Sub SearchRun() Handles SearchBox.TextChanged
        If String.IsNullOrWhiteSpace(SearchBox.Text) Then
            '隐藏
            AniStart({
                AaOpacity(PanSearch, -PanSearch.Opacity, 100),
                AaCode(
                Sub()
                    PanSearch.Height = 0
                    PanSearch.IsVisible = False
                    PanList.IsVisible = True
                End Sub,, True),
                AaOpacity(PanList, 1 - PanList.Opacity, 150, 30)
            }, "FrmOtherHelp Search Switch")
        Else
            '构造请求
            Dim QueryList As New List(Of SearchEntry(Of HelpEntry))
            For Each Entry As HelpEntry In HelpLoader.Output
                If Not Entry.ShowInSearch Then Continue For
                If BuildType = BuildTypes.Release AndAlso Not Entry.ShowInPublic Then Continue For
                If BuildType <> BuildTypes.Release AndAlso Not Entry.ShowInSnapshot Then Continue For
                QueryList.Add(New SearchEntry(Of HelpEntry) With {
                    .Item = Entry,
                    .SearchSource = New List(Of SearchSource) From {
                        New SearchSource(Entry.Title, 1),
                        New SearchSource(Entry.Desc, 0.5),
                        New SearchSource(Entry.Search, 1.5)
                    }
                })
            Next
            '进行搜索，构造列表
            Dim SearchResult As List(Of SearchEntry(Of HelpEntry)) = Search(QueryList, SearchBox.Text, MaxBlurCount:=5, MinBlurSimilarity:=0.08)
            PanSearchList.Children.Clear()
            If Not SearchResult.Any() Then
                PanSearch.Title = "无搜索结果"
                PanSearchList.IsVisible = False
            Else
                PanSearch.Title = "搜索结果"
                For Each Result As SearchEntry(Of HelpEntry) In SearchResult
                    Dim Item = BuildListItem(Result.Item)
                    If ModeDebug Then Item.Info = If(Result.AbsoluteRight, "完全匹配，", "") & "相似度：" & Math.Round(Result.Similarity, 3) & "，" & Item.Info
                    PanSearchList.Children.Add(Item)
                Next
                PanSearchList.IsVisible = True
            End If
            '显示
            AniStart({
                AaOpacity(PanList, -PanList.Opacity, 100),
                AaCode(
                Sub()
                    PanList.IsVisible = False
                    PanSearch.IsVisible = True
                    PanSearch.TriggerForceResize()
                End Sub,, True),
                AaOpacity(PanSearch, 1 - PanSearch.Opacity, 150, 30)
            }, "FrmOtherHelp Search Switch")
        End If
    End Sub

End Class
