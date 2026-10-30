Imports Avalonia.LogicalTree

''' <summary>
''' 帮助详情页，运行时加载帮助条目的 XAML 内容。
''' </summary>
Public Class PageOtherHelpDetail
    Inherits MyPageRight

    Public Entry As HelpEntry

    Private Shadows IsLoaded As Boolean = False

    Public Sub New()
        InitControls()
    End Sub

    Private Sub PageOtherHelpDetail_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        PanBack.ScrollToHome()
        If IsLoaded Then Return
        IsLoaded = True
    End Sub

    ''' <summary>
    ''' 根据特定帮助项初始化页面 UI，返回是否成功加载。
    ''' </summary>
    Public Function Init(Entry As HelpEntry) As Boolean
        Try
            If Entry Is Nothing Then Throw New ArgumentNullException(NameOf(Entry))
            Dim Content = If(Entry.XamlContent, "")
            If String.IsNullOrWhiteSpace(Content) Then Throw New Exception("帮助 xaml 文件为空")
            Me.Entry = Entry
            PanCustom.Children.Clear()
            Dim NewContent = ConvertHelpXaml(Content)
            ApplyImageInterpolation(NewContent)
            PanCustom.Children.Add(NewContent)
            Return True
        Catch ex As Exception
            Logger.Error(ex, "加载帮助 XAML 文件失败", LogBehavior.Alert)
            Return False
        End Try
    End Function

    Private Shared Sub ApplyImageInterpolation(Element As Control)
        If Element Is Nothing Then Return
        If TypeOf Element Is Image Then
            RenderOptions.SetBitmapInterpolationMode(CType(Element, Image), Media.Imaging.BitmapInterpolationMode.HighQuality)
        End If
        For Each Child As ILogical In Element.GetLogicalChildren()
            If TypeOf Child Is Control Then ApplyImageInterpolation(CType(Child, Control))
        Next
    End Sub

End Class
