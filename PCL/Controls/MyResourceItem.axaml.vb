Public Class MyResourceItem
    Inherits Grid

#Region "基础属性"
    Public Uuid As Integer = GetUuid()

    '标题
    Public Property Title As String
        Get
            Return LabTitle.Text
        End Get
        Set(value As String)
            If LabTitle.Text = value Then Return
            LabTitle.Text = value
        End Set
    End Property

    '副标题
    Public Property SubTitle As String
        Get
            Return If(LabTitleRaw?.Text, "")
        End Get
        Set(value As String)
            If LabTitleRaw Is Nothing Then Return
            If LabTitleRaw.Text = value Then Return
            LabTitleRaw.Text = value
            LabTitleRaw.IsVisible = value <> ""
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
    '指向时扩展描述
    Private Sub LabInfo_PointerEntered(sender As Object, e As PointerEventArgs) Handles LabInfo.PointerEntered
        ToolTip.SetTip(LabInfo, If(IsTextTrimmed(LabInfo), LabInfo.Text, Nothing))
    End Sub

    'Tag
    Public WriteOnly Property Tags As List(Of String)
        Set(Tags As List(Of String))
            PanTags.Children.Clear()
            PanTags.IsVisible = Tags.Any()
            For Each TagText In Tags
                Dim BorderTag As New Border With {
                    .Background = New SolidColorBrush(New MyColor("#11000000")), .Padding = New Thickness(3, 1, 3, 1), .CornerRadius = New CornerRadius(3),
                    .Margin = New Thickness(0, 0, 3, 0), .UseLayoutRounding = False}
                Dim LabTag As New TextBlock With {
                    .Text = TagText, .Foreground = New SolidColorBrush(New MyColor("#868686")), .FontSize = 11}
                BorderTag.Child = LabTag
                PanTags.Children.Add(BorderTag)
            Next
        End Set
    End Property

    '下边栏列定义
    Public ReadOnly Property ColumnVersion1 As ColumnDefinition
        Get
            Return PanBottom.ColumnDefinitions(0)
        End Get
    End Property
    Public ReadOnly Property ColumnVersion2 As ColumnDefinition
        Get
            Return PanBottom.ColumnDefinitions(1)
        End Get
    End Property
    Public ReadOnly Property ColumnVersion3 As ColumnDefinition
        Get
            Return PanBottom.ColumnDefinitions(2)
        End Get
    End Property

#End Region

    Public Sub New()
        InitControls()
        MyBase.AddHandler(InputElement.PointerPressedEvent, AddressOf Button_MouseDown, RoutingStrategies.Tunnel)
        MyBase.AddHandler(InputElement.PointerReleasedEvent, AddressOf Button_MouseUp, RoutingStrategies.Tunnel)
        MyBase.AddHandler(Me.PointerEnteredEvent, AddressOf RefreshColor)
        MyBase.AddHandler(Me.PointerExitedEvent, AddressOf Button_MouseLeave)
    End Sub

#Region "点击"

    '触发点击事件
    Public Event Click(sender As Object, e As PointerReleasedEventArgs)
    Private Sub Button_MouseUp(sender As Object, e As PointerReleasedEventArgs)
        If Not IsMouseDown Then Return
        IsMouseDown = False
        '提前释放隐式指针捕获，避免 Click 中打开的模态弹窗被本控件持续捕获指针
        e.Pointer.Capture(Nothing)
        RaiseEvent Click(sender, e)
        If e.Handled Then Return
        Logger.Info($"按下资源工程列表项：{LabTitle.Text}")
    End Sub
    Private Sub MyResourceItem_Click(sender As MyResourceItem, e As EventArgs) Handles Me.Click
        '打开详情页
        Dim TargetType As ResourceTypes
        Dim TargetInstance As String = Nothing
        Dim TargetLoader As ModLoaders = ModLoaders.None
        If FrmMain.PageCurrent.Page = FormMain.PageType.Download Then
            '从下载页进入
            Select Case FrmMain.PageCurrentSub
                Case FormMain.PageSubType.DownloadMod
                    TargetType = ResourceTypes.Mod
                Case FormMain.PageSubType.DownloadPack
                    TargetType = ResourceTypes.ModPack
                Case FormMain.PageSubType.DownloadDataPack
                    TargetType = ResourceTypes.DataPack
                Case FormMain.PageSubType.DownloadResourcePack
                    TargetType = ResourceTypes.ResourcePack
                Case FormMain.PageSubType.DownloadShader
                    TargetType = ResourceTypes.Shader
                Case Else
                    TargetType = ResourceTypes.Any
            End Select
        Else
            '从详情页进入（查看前置）
            TargetType = ResourceTypes.Any '允许任意类别
            TargetInstance = FrmMain.PageCurrent.Additional(2)
            TargetLoader = FrmMain.PageCurrent.Additional(3)
        End If
        FrmMain.PageChange(New FormMain.PageStackData With {.Page = FormMain.PageType.ResourceDetail,
                           .Additional = {sender.Tag, New List(Of String), TargetInstance, TargetLoader, TargetType}})
    End Sub

    '鼠标点击判定
    Private IsMouseDown As Boolean = False
    Private Sub Button_MouseDown(sender As Object, e As PointerPressedEventArgs)
        If Not e.GetCurrentPoint(Me).Properties.IsLeftButtonPressed Then Return
        If IsPointerOver AndAlso CanInteraction Then
            IsMouseDown = True
            RefreshColor(sender, e)
        End If
    End Sub
    Private Sub Button_MouseLeave(sender As Object, e As PointerEventArgs)
        IsMouseDown = False
        RefreshColor(sender, e)
    End Sub

#End Region

#Region "后加载指向背景"

    Private _RectBack As Border = Nothing
    Public ReadOnly Property RectBack As Border
        Get
            If _RectBack Is Nothing Then
                Dim Rect As New Border With {
                    .Name = "RectBack",
                    .CornerRadius = New CornerRadius(3),
                    .RenderTransform = New ScaleTransform(0.8, 0.8),
                    .RenderTransformOrigin = New RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                    .BorderThickness = New Thickness(GetWPFSize(1)),
                    .IsHitTestVisible = False,
                    .Opacity = 0}
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

#End Region

    Private StateLast As String
    ''' <summary>
    ''' 是否允许交互。目前仅用于 PageDownloadResourceDetail 的顶部栏展示：若关闭碰撞检测，则无法展开 Tooltip。
    ''' </summary>
    Public Property CanInteraction As Boolean = True
    Public Sub RefreshColor(sender As Object, e As EventArgs)
        If Not CanInteraction Then Return
        '判断当前颜色
        Dim StateNew As String, Time As Integer
        If IsPointerOver Then
            If IsMouseDown Then
                StateNew = "MouseDown"
                Time = 120
            Else
                StateNew = "MouseOver"
                Time = 120
            End If
        Else
            StateNew = "Idle"
            Time = 180
        End If
        If StateLast = StateNew Then Return
        StateLast = StateNew
        '触发颜色动画
        If IsLoaded AndAlso AniControlEnabled = 0 Then '防止默认属性变更触发动画
            '有动画
            Dim Ani As New List(Of AniData)
            If IsPointerOver Then
                Ani.AddRange({
                    AaColor(RectBack, Border.BackgroundProperty, If(IsMouseDown, "ColorBrush6", "ColorBrushBg1"), Time),
                    AaOpacity(RectBack, 1 - RectBack.Opacity, Time,, New AniEaseOutFluent)})
                If IsMouseDown Then
                    Ani.Add(AaScaleTransform(RectBack, 0.996 - AniScaleX(RectBack), CInt(Time * 1.2),, New AniEaseOutFluent))
                Else
                    Ani.Add(AaScaleTransform(RectBack, 1 - AniScaleX(RectBack), CInt(Time * 1.2),, New AniEaseOutFluent))
                End If
            Else
                Ani.AddRange({
                    AaOpacity(RectBack, -RectBack.Opacity, Time),
                    AaColor(RectBack, Border.BackgroundProperty, If(IsMouseDown, "ColorBrush6", "ColorBrush7"), Time),
                    AaScaleTransform(RectBack, 0.996 - AniScaleX(RectBack), Time,, New AniEaseOutFluent),
                    AaScaleTransform(RectBack, -0.196, 1,,, True)})
            End If
            AniStart(Ani, "MyResourceItem Color " & Uuid)
        Else
            '无动画
            AniStop("MyResourceItem Color " & Uuid)
            If _RectBack IsNot Nothing Then RectBack.Opacity = 0
        End If
    End Sub

End Class
