Imports Avalonia.VisualTree

''' <summary>
''' 版本 Mod 列表中的单个 Mod 项。
''' </summary>
Public Class MyLocalModItem
    Inherits Grid

#Region "基础属性"
    Public Uuid As Integer = GetUuid()

    '标题
    Private _Title As String
    Public Property Title As String
        Get
            Return _Title
        End Get
        Set(value As String)
            Dim RawValue = value
            LabTitle.TextDecorations = If(Entry.IsEnabled, Nothing, TextDecorations.Strikethrough)
            If LabTitle.Text = value Then Return
            LabTitle.Text = value
            _Title = RawValue
        End Set
    End Property

    '副标题
    Public Property SubTitle As String
        Get
            Return If(LabSubtitle?.Text, "")
        End Get
        Set(value As String)
            If LabSubtitle.Text = value Then Return
            LabSubtitle.Text = value
            LabSubtitle.IsVisible = value <> ""
        End Set
    End Property

    '描述
    Public Property Description As String
        Get
            Return LabInfo.Text
        End Get
        Set(value As String)
            If LabInfo.Text = value Then Return
            LabInfo.Text = value
        End Set
    End Property

    'Tag
    Public WriteOnly Property Tags As List(Of String)
        Set(Tags As List(Of String))
            PanTags.Children.Clear()
            PanTags.IsVisible = Tags.Any()
            For Each TagText In Tags
                Dim BorderTag As New Border With {
                    .Background = New SolidColorBrush(New MyColor("#0A000000")), .Padding = New Thickness(3, 1, 3, 1), .CornerRadius = New CornerRadius(3),
                    .Margin = New Thickness(0, 0, 3, 0), .UseLayoutRounding = False}
                Dim LabTag As New TextBlock With {
                    .Text = TagText, .Foreground = New SolidColorBrush(New MyColor("#B0B0B0")), .FontSize = 11}
                BorderTag.Child = LabTag
                PanTags.Children.Add(BorderTag)
            Next
        End Set
    End Property

    '相关联的 Mod
    Public Property Entry As LocalResourceFile
        Get
            Return Tag
        End Get
        Set(value As LocalResourceFile)
            Tag = value
        End Set
    End Property

#End Region

#Region "点击与勾选"

    '触发点击事件
    Public Event Click(sender As Object, e As PointerReleasedEventArgs)
    Private Sub Button_MouseUp(sender As Object, e As PointerReleasedEventArgs)
        If Not IsMouseDown Then Return
        '先触发点击，再重置按下状态，以保持与按下时一致的配色动画顺序
        RaiseEvent Click(sender, e)
        IsMouseDown = False
        If ButtonStack IsNot Nothing Then ButtonStack.IsHitTestVisible = True
        If Swiping Then Swiping = False
        If e.Handled Then Return
        Logger.Info($"按下本地 Mod 列表项：{LabTitle.Text}")
    End Sub

    '鼠标点击判定
    Private IsMouseDown As Boolean = False
    Private Sub Button_MouseDown(sender As Object, e As PointerPressedEventArgs)
        If Not e.GetCurrentPoint(Me).Properties.IsLeftButtonPressed Then Return
        If e.Source IsNot Me Then Return '仅在直接按下列表项本身时响应，对应 WPF 的 IsMouseDirectlyOver
        IsMouseDown = True
        If ButtonStack IsNot Nothing Then ButtonStack.IsHitTestVisible = False
        '开始滑动
        Dim Panel = TryCast(Parent, StackPanel)
        If Panel Is Nothing Then Return 'Mod 可能已被删除（#3824）
        Dim Index = Panel.Children.IndexOf(Me)
        SwipeStart = Index
        SwipeEnd = Index
        Swiping = True
        SwipToState = Not Checked
        EnsureSwipeReleaseHook()
    End Sub
    Private Sub Button_MouseLeave(sender As Object, e As PointerEventArgs)
        IsMouseDown = False
        If ButtonStack IsNot Nothing Then ButtonStack.IsHitTestVisible = True
        If Swiping AndAlso Not IsPointerOverAnyItem(e) Then Swiping = False
        RefreshColor(sender, e)
    End Sub

    '滑动选中
    Private Shared SwipeStart As Integer, SwipeEnd As Integer
    Private Shared _Swiping As Boolean = False
    Private Shared Property Swiping As Boolean
        Get
            Return _Swiping
        End Get
        Set(value As Boolean)
            _Swiping = value
            If FrmInstanceMod IsNot Nothing Then FrmInstanceMod.CardSelect.IsHitTestVisible = Not value
        End Set
    End Property
    Private Shared SwipToState As Boolean '被滑动到的目标应将 Checked 改为此值
    Private Shared SwipeHookTopLevel As TopLevel = Nothing
    Private Shared Sub EnsureSwipeReleaseHook()
        If FrmInstanceMod Is Nothing Then Return
        Dim Top = TopLevel.GetTopLevel(FrmInstanceMod)
        If Top Is Nothing OrElse Top Is SwipeHookTopLevel Then Return
        SwipeHookTopLevel = Top
        Top.AddHandler(InputElement.PointerReleasedEvent, Sub(s, e) Swiping = False, RoutingStrategies.Tunnel)
    End Sub
    Private Shared Function IsPointerOverAnyItem(e As PointerEventArgs) As Boolean
        If e.Pointer Is Nothing OrElse FrmInstanceMod Is Nothing Then Return False
        Dim Top = TopLevel.GetTopLevel(FrmInstanceMod)
        If Top Is Nothing Then Return False
        Dim HitVisual = TryCast(Top.InputHitTest(e.GetPosition(Top)), Visual)
        If HitVisual Is Nothing Then Return False
        Return TypeOf HitVisual Is MyLocalModItem OrElse HitVisual.FindAncestorOfType(Of MyLocalModItem)() IsNot Nothing
    End Function
    Private Sub Button_MouseSwipe(sender As Object, e As PointerEventArgs)
        If Not Swiping Then Return
        Dim Panel = TryCast(Parent, StackPanel)
        If Panel Is Nothing Then Return 'Mod 可能已被删除（#3824）
        '计算滑动范围
        Dim Elements = Panel.Children
        Dim Index As Integer = Elements.IndexOf(Me)
        SwipeStart = Math.Min(SwipeStart, Index).Clamp(0, Elements.Count - 1)
        SwipeEnd = Math.Max(SwipeEnd, Index).Clamp(0, Elements.Count - 1)
        '勾选所有范围中的项
        If SwipeStart = SwipeEnd Then Return
        For i = SwipeStart To SwipeEnd
            Dim Item As MyLocalModItem = Elements(i)
            Item.InitLate(Item, e)
            Item.Checked = SwipToState
        Next
    End Sub

    '勾选状态
    Public Event Check(sender As Object, e As RouteEventArgs)
    Public Event Changed(sender As Object, e As RouteEventArgs)
    Private _Checked As Boolean = False
    Public Property Checked As Boolean
        Get
            Return _Checked
        End Get
        Set(value As Boolean)
            Try
                '触发属性值修改
                Dim RawValue = _Checked
                If value = _Checked Then Return
                _Checked = value
                Dim ChangedEventArgs As New RouteEventArgs(False)
                If IsInitialized Then
                    RaiseEvent Changed(Me, ChangedEventArgs)
                    If ChangedEventArgs.Handled Then
                        _Checked = RawValue
                        Return
                    End If
                End If
                If value Then
                    Dim CheckEventArgs As New RouteEventArgs(False)
                    RaiseEvent Check(Me, CheckEventArgs)
                    If CheckEventArgs.Handled Then Return
                End If
                '更改动画
                If IsVisibleInForm() Then
                    Dim Anim As New List(Of AniData)
                    If Checked Then
                        '由无变有
                        Dim Delta = 32 - RectCheck.Bounds.Height
                        Anim.Add(AaHeight(RectCheck, Delta * 0.4, 200,, New AniEaseOutFluent(AniEasePower.Weak)))
                        Anim.Add(AaHeight(RectCheck, Delta * 0.6, 300,, New AniEaseOutBack(AniEasePower.Weak)))
                        Anim.Add(AaOpacity(RectCheck, 1 - RectCheck.Opacity, 30))
                        RectCheck.VerticalAlignment = VerticalAlignment.Center
                        RectCheck.Margin = New Thickness(-3, 0, 0, 0)
                        Anim.Add(AaColor(LabTitle, TextBlock.ForegroundProperty, If(Entry.IsEnabled, "ColorBrush2", "ColorBrush5"), 200))
                    Else
                        '由有变无
                        Anim.Add(AaHeight(RectCheck, -RectCheck.Bounds.Height, 120,, New AniEaseInFluent(AniEasePower.Weak)))
                        Anim.Add(AaOpacity(RectCheck, -RectCheck.Opacity, 70, 40))
                        RectCheck.VerticalAlignment = VerticalAlignment.Center
                        Anim.Add(AaColor(LabTitle, TextBlock.ForegroundProperty, If(LabTitle.TextDecorations Is Nothing, "ColorBrush1", "ColorBrushGray4"), 120))
                    End If
                    AniStart(Anim, "MyLocalModItem Checked " & Uuid)
                Else
                    '不在窗口上时直接设置
                    RectCheck.VerticalAlignment = VerticalAlignment.Center
                    RectCheck.Margin = New Thickness(-3, 0, 0, 0)
                    If Checked Then
                        RectCheck.Height = 32
                        RectCheck.Opacity = 1
                        LabTitle.SetResourceReference(TextBlock.ForegroundProperty, If(Entry.IsEnabled, "ColorBrush2", "ColorBrush5"))
                    Else
                        RectCheck.Height = 0
                        RectCheck.Opacity = 0
                        LabTitle.SetResourceReference(TextBlock.ForegroundProperty, If(Entry.IsEnabled, "ColorBrush1", "ColorBrushGray4"))
                    End If
                    AniStop("MyLocalModItem Checked " & Uuid)
                End If
            Catch ex As Exception
                Logger.Warn(ex, "设置 Checked 失败")
            End Try
        End Set
    End Property

#End Region

#Region "后加载内容"

    '右下角状态指示图标
    Private ImgState As Image

    '指向背景
    Private _RectBack As Border = Nothing
    Public ReadOnly Property RectBack As Border
        Get
            If _RectBack Is Nothing Then
                Dim Rect As New Border With {
                    .CornerRadius = New CornerRadius(3),
                    .RenderTransform = New ScaleTransform(0.8, 0.8),
                    .RenderTransformOrigin = New RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                    .BorderThickness = New Thickness(GetWPFSize(1)),
                    .IsHitTestVisible = False,
                    .Opacity = 0
                }
                Rect.SetResourceReference(Border.BackgroundProperty, "ColorBrush7")
                Rect.SetResourceReference(Border.BorderBrushProperty, "ColorBrush6")
                Grid.SetColumnSpan(Rect, 999)
                Grid.SetRowSpan(Rect, 999)
                Children.Insert(0, Rect)
                _RectBack = Rect
            End If
            Return _RectBack
        End Get
    End Property

    '按钮
    Public ButtonHandler As Action(Of MyLocalModItem, EventArgs)
    Public ButtonStack As Control
    Private _Buttons As IEnumerable(Of MyIconButton)
    Public Property Buttons As IEnumerable(Of MyIconButton)
        Get
            Return _Buttons
        End Get
        Set(value As IEnumerable(Of MyIconButton))
            _Buttons = value
            '移除原 Stack
            If ButtonStack IsNot Nothing Then
                Children.Remove(ButtonStack)
                ButtonStack = Nothing
            End If
            If Not value.Any() Then Return
            '添加新 Stack
            ButtonStack = New StackPanel With {.Opacity = 0, .Margin = New Thickness(0, 0, 5, 0), .Orientation = Orientation.Horizontal,
                .HorizontalAlignment = HorizontalAlignment.Right, .VerticalAlignment = VerticalAlignment.Center, .UseLayoutRounding = False}
            Grid.SetColumnSpan(ButtonStack, 10) : Grid.SetRowSpan(ButtonStack, 10)
            '构造按钮
            For Each Btn As MyIconButton In value
                If Double.IsNaN(Btn.Height) Then Btn.Height = 25
                If Double.IsNaN(Btn.Width) Then Btn.Width = 25
                CType(ButtonStack, StackPanel).Children.Add(Btn)
            Next
            Children.Add(ButtonStack)
        End Set
    End Property

    '勾选条
    Private _RectCheck As Border
    Public ReadOnly Property RectCheck As Border
        Get
            If _RectCheck Is Nothing Then
                _RectCheck = New Border With {.Width = 5, .Height = If(Checked, Double.NaN, 0), .CornerRadius = New CornerRadius(2, 2, 2, 2),
                    .VerticalAlignment = If(Checked, VerticalAlignment.Stretch, VerticalAlignment.Center),
                    .HorizontalAlignment = HorizontalAlignment.Left, .UseLayoutRounding = False,
                    .Margin = If(Checked, New Thickness(-3, 6, 0, 6), New Thickness(-3, 0, 0, 0))}
                _RectCheck.SetResourceReference(Border.BackgroundProperty, "ColorBrush3")
                Grid.SetRowSpan(_RectCheck, 10)
                Children.Add(_RectCheck)
            End If
            Return _RectCheck
        End Get
    End Property

#End Region

#Region "布局"

    'XAML 中的列定义无法按名称生成字段，改为按索引访问
    Private ReadOnly Property ColumnPaddingRight As ColumnDefinition
        Get
            Return ColumnDefinitions(5)
        End Get
    End Property
    Private ReadOnly Property ColumnTitle As ColumnDefinition
        Get
            Return PanTitle.ColumnDefinitions(0)
        End Get
    End Property
    Private ReadOnly Property ColumnSubtitle As ColumnDefinition
        Get
            Return PanTitle.ColumnDefinitions(1)
        End Get
    End Property
    Private ReadOnly Property ColumnExtend As ColumnDefinition
        Get
            Return PanTitle.ColumnDefinitions(3)
        End Get
    End Property

    '自适应（#4465）
    Private Sub PanTitle_SizeChangedHandler(sender As Object, e As SizeChangedEventArgs)
        '0：全部舒展：Auto - Auto - (Auto) - 1*
        '1：压缩 Subtitle：Auto - 1* - (Auto) - 0
        '2：继续压缩 Title：1* - 0 - (Auto) - 0
        Dim CurrentCompressLevel As Integer =
            If(ColumnExtend.Width.IsStar, 0, If(ColumnTitle.Width.IsStar, 2, 1)) 'Subtitle 可能是 Collapsed
        Dim NewCompressLevel As Integer
        Select Case CurrentCompressLevel
            Case 0
                If ColumnExtend.ActualWidth < 0.5 Then
                    NewCompressLevel = If(Not LabSubtitle.IsVisible, 2, 1)
                Else
                    Return
                End If
            Case 1
                If ColumnSubtitle.ActualWidth < 0.5 Then
                    NewCompressLevel = 2
                ElseIf Not LabSubtitle.IsTextTrimmed Then
                    NewCompressLevel = 0
                Else
                    Return
                End If
            Case Else
                If Not LabTitle.IsTextTrimmed Then
                    NewCompressLevel = If(Not LabSubtitle.IsVisible, 0, 1)
                Else
                    Return
                End If
        End Select
        Select Case NewCompressLevel
            Case 0
                '全部舒展：Auto - Auto - (Auto) - 1*
                ColumnTitle.Width = GridLength.Auto
                ColumnSubtitle.Width = GridLength.Auto
                ColumnExtend.Width = New GridLength(1, GridUnitType.Star)
            Case 1
                '压缩 Subtitle：Auto - 1* - (Auto) - 0
                ColumnTitle.Width = GridLength.Auto
                ColumnSubtitle.Width = New GridLength(1, GridUnitType.Star)
                ColumnExtend.Width = New GridLength(0, GridUnitType.Pixel)
            Case Else
                '继续压缩 Title：1* - 0 - (Auto) - 0
                ColumnTitle.Width = New GridLength(1, GridUnitType.Star)
                ColumnSubtitle.Width = New GridLength(0, GridUnitType.Pixel)
                ColumnExtend.Width = New GridLength(0, GridUnitType.Pixel)
        End Select
    End Sub

#End Region

    Private Function GetUpdateCompareDescription() As String
        Dim CurrentName = If(Entry.ProjectVersion.Version, Entry.ProjectVersion.Display).Replace(".jar", "")
        Dim NewestName = If(Entry.UpdateFile.Version, Entry.UpdateFile.Display).Replace(".jar", "")
        '简化名称对比
        Dim CurrentSegs = CurrentName.Split("-_+ ".ToCharArray()).ToList()
        Dim NewestSegs = NewestName.Split("-_+ ".ToCharArray()).ToList()
        Dim Shortened As Boolean = False
        For Each Seg In CurrentSegs.ToList()
            If Not NewestSegs.Contains(Seg) Then Continue For
            CurrentSegs.Remove(Seg)
            NewestSegs.Remove(Seg)
            Shortened = True
        Next
        If Shortened AndAlso CurrentSegs.Any() AndAlso NewestSegs.Any() Then
            CurrentName = CurrentSegs.Join("-")
            NewestName = NewestSegs.Join("-")
            Entry._Version = CurrentName '使用网络信息作为显示的版本号
        End If
        Return $"当前版本：{CurrentName}（{StringUtils.FormatTimeSpan(Entry.ProjectVersion.ReleaseDate - Date.Now, False)}）{vbCrLf}最新版本：{NewestName}（{StringUtils.FormatTimeSpan(Entry.UpdateFile.ReleaseDate - Date.Now, False)}）"
    End Function

    '懒加载
    Public Sub New()
        InitControls()
        LazyLoadBehavior.OnFirstEnterScrollViewerViewport(Me, AddressOf FirstEnterScrollViewerViewport)
        MyBase.AddHandler(InputElement.PointerPressedEvent, AddressOf Button_MouseDown, RoutingStrategies.Tunnel)
        MyBase.AddHandler(InputElement.PointerReleasedEvent, AddressOf Button_MouseUp, RoutingStrategies.Tunnel)
        MyBase.AddHandler(Me.PointerEnteredEvent, AddressOf Button_MouseEnter)
        MyBase.AddHandler(Me.PointerExitedEvent, AddressOf Button_MouseLeave)
        AddHandler PanTitle.SizeChanged, AddressOf PanTitle_SizeChangedHandler
        AddHandler Me.Changed, Sub(s, e) RefreshColor(s, e)
        AddHandler BtnUpdate.PointerPressed, AddressOf BtnUpdate_PointerPressed
    End Sub
    Private Sub Button_MouseEnter(sender As Object, e As PointerEventArgs)
        Button_MouseSwipe(sender, e)
        RefreshColor(sender, e)
    End Sub
    Private HasEnteredViewport As Boolean = False
    Private Sub FirstEnterScrollViewerViewport()
        HasEnteredViewport = True
        Refresh()
    End Sub
    Public Sub Refresh()
        If Not HasEnteredViewport Then Return '避免在未显示时因为资源更新而加载内容
        '更新
        If Entry.HasUpdate Then
            BtnUpdate.IsVisible = True
            BtnUpdate.ToolTip = $"{GetUpdateCompareDescription()}{vbCrLf}点击以更新，右键查看更新日志。"
        Else
            BtnUpdate.IsVisible = False
        End If
        '标题与描述
        Dim DescFileName As String = PathUtils.GetFileNameWithoutExtension(Entry.EnabledName)
        Dim NewDescription As String
        If Settings.Get(Of Integer)("ToolModLocalNameStyle") = 1 Then
            '标题显示文件名，详情显示译名
            '标题
            Title = DescFileName
            SubTitle = ""
            '描述
            If Entry.Project Is Nothing Then
                NewDescription = Entry.Display
            Else
                Dim Titles = Entry.Project.GetControlTitle(False)
                NewDescription = Titles.Title & Titles.SubTitle
            End If
            NewDescription = NewDescription.Replace("  |  ", " / ")
            If Entry.Version IsNot Nothing Then NewDescription &= $" ({Entry.Version})"
        Else
            '标题显示译名，详情显示文件名
            '标题
            If Entry.Project Is Nothing Then
                Title = Entry.Display
                SubTitle = If(Entry.Version Is Nothing, "", "  |  " & Entry.Version)
            Else
                Dim Titles = Entry.Project.GetControlTitle(False)
                Title = Titles.Title
                SubTitle = Titles.SubTitle & If(Entry.Version Is Nothing, "", "  |  " & Entry.Version)
            End If
            '描述
            NewDescription = DescFileName
        End If
        If Entry.Project IsNot Nothing Then
            NewDescription += ": " & Entry.Project.Description.ReplaceLineEndings("")
        ElseIf Entry.Description IsNot Nothing Then
            NewDescription += ": " & Entry.Description.ReplaceLineEndings("")
        End If
        Description = NewDescription
        If Checked Then
            LabTitle.SetResourceReference(TextBlock.ForegroundProperty, If(Entry.IsEnabled, "ColorBrush2", "ColorBrush5"))
        Else
            LabTitle.SetResourceReference(TextBlock.ForegroundProperty, If(Entry.IsEnabled, "ColorBrush1", "ColorBrushGray4"))
        End If
        '主 Logo
        If Entry.Project IsNot Nothing Then
            Entry.Project.ApplyLogoToMyImage(PathLogo)
        Else
            PathLogo.Source = PathImage & "Icons/NoIcon.png"
        End If
        '图标右下角的 Logo
        If Entry.IsEnabled Then
            If ImgState IsNot Nothing Then
                Children.Remove(ImgState)
                ImgState = Nothing
            End If
        Else
            If ImgState Is Nothing Then
                ImgState = New Image With {
                    .Width = 20, .Height = 20, .Margin = New Thickness(0, 0, -5, -3), .IsHitTestVisible = False,
                    .HorizontalAlignment = HorizontalAlignment.Right, .VerticalAlignment = VerticalAlignment.Bottom
                }
                RenderOptions.SetBitmapInterpolationMode(ImgState, Media.Imaging.BitmapInterpolationMode.HighQuality)
                Grid.SetColumn(ImgState, 1) : Grid.SetRow(ImgState, 1) : Grid.SetRowSpan(ImgState, 2)
                Children.Add(ImgState)
            End If
            ImgState.Source = New MyBitmap(PathImage & $"Icons/Disabled.png")
        End If
        '标签
        If Entry.Project IsNot Nothing Then Tags = Entry.Project.Tags
    End Sub

    Public Sub RefreshColor(sender As Object, e As EventArgs)
        InitLate(sender, e)
        '触发颜色动画
        Dim Time As Integer = If(IsPointerOver, 120, 180)
        Dim Ani As New List(Of AniData)
        'ButtonStack
        If ButtonStack IsNot Nothing Then
            If IsPointerOver Then
                Ani.Add(AaOpacity(ButtonStack, 1 - ButtonStack.Opacity, Time * 0.7, Time * 0.3))
                Ani.Add(AaDouble(Sub(i As Object)
                                     ColumnPaddingRight.Width = New GridLength(Math.Max(0, ColumnPaddingRight.Width.Value + CDbl(i)))
                                 End Sub, 5 + Buttons.Count * 25 - ColumnPaddingRight.Width.Value, Time * 0.3, Time * 0.7))
            Else
                Ani.Add(AaOpacity(ButtonStack, -ButtonStack.Opacity, Time * 0.4))
                Ani.Add(AaDouble(Sub(i As Object)
                                     ColumnPaddingRight.Width = New GridLength(Math.Max(0, ColumnPaddingRight.Width.Value + CDbl(i)))
                                 End Sub, 4 - ColumnPaddingRight.Width.Value, Time * 0.4))
            End If
        End If
        'RectBack
        If IsPointerOver OrElse Checked Then
            Ani.AddRange({
                AaColor(RectBack, Border.BackgroundProperty, If(IsMouseDown, "ColorBrush6", "ColorBrushBg1"), Time),
                AaOpacity(RectBack, 1 - RectBack.Opacity, Time,, New AniEaseOutFluent)
            })
            If IsMouseDown Then
                Ani.Add(AaScaleTransform(RectBack, 0.996 - CType(RectBack.RenderTransform, ScaleTransform).ScaleX, Time * 1.2,, New AniEaseOutFluent))
            Else
                Ani.Add(AaScaleTransform(RectBack, 1 - CType(RectBack.RenderTransform, ScaleTransform).ScaleX, Time * 1.2,, New AniEaseOutFluent))
            End If
        Else
            Ani.AddRange({
                AaOpacity(RectBack, -RectBack.Opacity, Time),
                AaScaleTransform(RectBack, 0.996 - CType(RectBack.RenderTransform, ScaleTransform).ScaleX, Time,, New AniEaseOutFluent),
                AaScaleTransform(RectBack, -0.196, 1,,, True)
            })
        End If
        AniStart(Ani, "LocalModItem Color " & Uuid)
    End Sub

    '触发虚拟化内容
    Private Sub InitLate(sender As Object, e As EventArgs)
        If ButtonHandler IsNot Nothing Then
            ButtonHandler(sender, e)
            ButtonHandler = Nothing
        End If
    End Sub

    '显示更新日志
    Private Sub BtnUpdate_PointerPressed(sender As Object, e As PointerPressedEventArgs)
        If Not e.GetCurrentPoint(BtnUpdate).Properties.IsRightButtonPressed Then Return
        e.Handled = True
        ShowUpdateLog()
    End Sub
    Private Sub ShowUpdateLog()
        Dim CurseForgeUrl As String = Entry.ChangelogUrls.FirstOrDefault(Function(x) x.Contains("curseforge.com"))
        Dim ModrinthUrl As String = Entry.ChangelogUrls.FirstOrDefault(Function(x) x.Contains("modrinth.com"))
        If CurseForgeUrl Is Nothing OrElse ModrinthUrl Is Nothing Then
            OpenWebsite(Entry.ChangelogUrls.First)
        Else
            Select Case MyMsgBox("要在哪个网站上查看更新日志？", "查看更新日志", "Modrinth", "CurseForge", "取消")
                Case 1
                    OpenWebsite(ModrinthUrl)
                Case 2
                    OpenWebsite(CurseForgeUrl)
            End Select
        End If
    End Sub

    '触发更新
    Private Sub BtnUpdate_Click(sender As Object, e As EventArgs) Handles BtnUpdate.Click
        Select Case MyMsgBox($"是否要更新 {Entry.Display}？{vbCrLf}{vbCrLf}{GetUpdateCompareDescription()}", "Mod 更新确认", "更新", "查看更新日志", "取消")
            Case 1 '更新
                FrmInstanceMod.UpdateMods({Entry})
            Case 2 '查看更新日志
                ShowUpdateLog()
            Case 3 '取消
        End Select
    End Sub

End Class
