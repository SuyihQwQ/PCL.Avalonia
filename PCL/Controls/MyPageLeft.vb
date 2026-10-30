''' <summary>
''' 左侧页面容器，负责页面切换时的逐个进入、退出动画。
''' </summary>
Public Class MyPageLeft
    Inherits Grid
    Private Uuid As Integer = GetUuid()

    ''' <summary>
    ''' 执行逐个进入动画的控件。
    ''' </summary>
    Public Shared ReadOnly AnimatedControlProperty As StyledProperty(Of String) =
        AvaloniaProperty.Register(Of MyPageLeft, String)("AnimatedControl", Nothing)
    Public Property AnimatedControl As String
        Get
            Return GetValue(AnimatedControlProperty)
        End Get
        Set(value As String)
            SetValue(AnimatedControlProperty, value)
        End Set
    End Property

    Public Sub TriggerShowAnimation()
        If AnimatedControl Is Nothing Then
            '缩放动画
            If TypeOf RenderTransform IsNot ScaleTransform Then
                RenderTransform = New ScaleTransform(0.96, 0.96)
                RenderTransformOrigin = RelativePoint.Center
            End If
            Opacity = 0
            AniStart({
                AaScaleTransform(Me, 1 - AniScaleX(Me), 400,, New AniEaseOutBack(2)),
                AaOpacity(Me, 1, 100)
            }, "PageLeft PageChange " & Uuid)
        Else
            '逐个进入动画
            Dim AniList As New List(Of AniData)
            Dim Id As Integer = 0, Delay As Integer = 0
            For Each Element As Control In GetAllAnimControls(True)
                If Not Element.IsVisible Then
                    '还原之前的隐藏动画可能导致的改变
                    Element.Opacity = 1
                    Element.RenderTransform = New TranslateTransform(0, 0)
                    If TypeOf Element Is MyListItem Then CType(Element, MyListItem).IsMouseOverAnimationEnabled = True
                Else
                    Element.Opacity = 0
                    Element.RenderTransform = New TranslateTransform(-25, 0)
                    If TypeOf Element Is MyListItem Then CType(Element, MyListItem).IsMouseOverAnimationEnabled = False
                    AniList.Add(AaOpacity(Element, If(TypeOf Element Is TextBlock, 0.6, 1), 100, Delay, New AniEaseOutFluent(AniEasePower.Weak)))
                    AniList.Add(AaTranslateX(Element, 5, 200, Delay, New AniEaseOutFluent))
                    AniList.Add(AaTranslateX(Element, 20, 300, Delay, New AniEaseOutBack(AniEasePower.Weak)))
                    If TypeOf Element Is MyListItem Then
                        Dim ListItem As MyListItem = Element
                        AniList.Add(AaCode(
                        Sub()
                            ListItem.IsMouseOverAnimationEnabled = True
                            ListItem.RefreshColor(Me, EventArgs.Empty)
                        End Sub, Delay + 280))
                    End If
                    Delay += Math.Max(15 - Id, 7) * 2
                    Id += 1
                End If
            Next
            AniStart(AniList, "PageLeft PageChange " & Uuid)
        End If
    End Sub

    Public Sub TriggerHideAnimation()
        If AnimatedControl Is Nothing Then
            '缩放动画
            If TypeOf RenderTransform IsNot ScaleTransform Then
                RenderTransform = New ScaleTransform(1, 1)
                RenderTransformOrigin = RelativePoint.Center
            End If
            Dim Scale = DirectCast(RenderTransform, ScaleTransform)
            AniStart({
                AaScaleTransform(Me, 0.95 - Scale.ScaleX, 110,, New AniEaseInFluent(AniEasePower.Weak)),
                AaOpacity(Me, -Opacity, 80, 30)
            }, "PageLeft PageChange " & Uuid)
        Else
            '逐个退出动画
            Dim AniList As New List(Of AniData)
            Dim Id As Integer = 0
            Dim Controls = GetAllAnimControls()
            For Each Element As Control In Controls
                Dim ItemDelay As Integer = CInt(70 / Controls.Count * Id)
                AniList.Add(AaOpacity(Element, -Element.Opacity, 50, ItemDelay))
                AniList.Add(AaTranslateX(Element, -6, 50, ItemDelay))
                Id += 1
            Next
            AniStart(AniList, "PageLeft PageChange " & Uuid)
        End If
    End Sub

    '遍历获取所有需要生成动画的控件
    Private Function GetAllAnimControls(Optional IgnoreInvisibility As Boolean = False) As List(Of Control)
        Dim AllControls As New List(Of Control)
        Dim Root = NameScopeExtensions.Find(Of Control)(Me, AnimatedControl)
        If Root IsNot Nothing Then GetAllAnimControls(Root, AllControls, IgnoreInvisibility)
        Return AllControls
    End Function
    Private Sub GetAllAnimControls(Element As Control, ByRef AllControls As List(Of Control), IgnoreInvisibility As Boolean)
        If Element Is Nothing Then Return
        If Not IgnoreInvisibility AndAlso Not Element.IsVisible Then Return
        If TypeOf Element Is MyTextButton Then
            AllControls.Add(Element)
        ElseIf TypeOf Element Is MyListItem Then
            AllControls.Add(Element)
        ElseIf TypeOf Element Is ContentControl Then
            GetAllAnimControls(CType(Element, ContentControl).Content, AllControls, IgnoreInvisibility)
        ElseIf TypeOf Element Is Panel Then
            For Each Element2 As Control In CType(Element, Panel).Children
                GetAllAnimControls(Element2, AllControls, IgnoreInvisibility)
            Next
        Else
            AllControls.Add(Element)
        End If
    End Sub

End Class
